using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UpCard.Web.Data;
using UpCard.Web.Models;

namespace UpCard.Web.Services;

public class ShopifyOptions
{
    public string ApiKey { get; set; } = "";
    public string ApiSecret { get; set; } = "";
    public string Scopes { get; set; } = "";
    public string AppUrl { get; set; } = "";
    public string HostName => new Uri(AppUrl.TrimEnd('/')).Host;
}

public class CartService
{
    private readonly AppDbContext _db;

    public CartService(AppDbContext db) => _db = db;

    public async Task<ShopRecord> EnsureShopAsync(string domain, string? accessToken = null, string? scope = null)
    {
        domain = NormalizeShop(domain);
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == domain);
        if (shop == null)
        {
            shop = new ShopRecord { Domain = domain, AccessToken = accessToken, Scope = scope };
            _db.Shops.Add(shop);
            await _db.SaveChangesAsync();
            await EnsureDefaultCartAsync(shop.Id);
            return shop;
        }

        if (!string.IsNullOrEmpty(accessToken))
        {
            shop.AccessToken = accessToken;
            shop.Scope = scope ?? shop.Scope;
            shop.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return shop;
    }

    public async Task<CartRecord> EnsureDefaultCartAsync(string shopId)
    {
        var existing = await _db.Carts.Where(c => c.ShopId == shopId).OrderBy(c => c.CreatedAt).FirstOrDefaultAsync();
        if (existing != null) return existing;

        var cart = new CartRecord
        {
            ShopId = shopId,
            Name = "Varsayılan sepet",
            Status = "live",
            TrafficAllocation = 100,
            ConfigJson = CartConfigDefaults.CreateDefaultJson()
        };
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync();
        return cart;
    }

    public async Task<(ShopRecord shop, List<CartRecord> carts)> ListCartsAsync(string domain)
    {
        var shop = await EnsureShopAsync(domain);
        await EnsureDefaultCartAsync(shop.Id);
        var carts = await _db.Carts.Where(c => c.ShopId == shop.Id).OrderByDescending(c => c.UpdatedAt).ToListAsync();
        return (shop, carts);
    }

    public Task<CartRecord?> GetCartAsync(string cartId) =>
        _db.Carts.FirstOrDefaultAsync(c => c.Id == cartId);

    public async Task<CartRecord> UpdateCartConfigAsync(string cartId, string configJson, string? name = null)
    {
        var cart = await _db.Carts.FirstAsync(c => c.Id == cartId);
        cart.ConfigJson = configJson;
        if (!string.IsNullOrWhiteSpace(name)) cart.Name = name!;
        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return cart;
    }

    public async Task<CartRecord> CreateCartAsync(string domain, string name)
    {
        var shop = await EnsureShopAsync(domain);
        var cart = new CartRecord
        {
            ShopId = shop.Id,
            Name = name,
            Status = "draft",
            TrafficAllocation = 0,
            ConfigJson = CartConfigDefaults.CreateDefaultJson()
        };
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync();
        return cart;
    }

    public async Task<CartRecord> PublishCartAsync(string domain, string cartId)
    {
        var shop = await EnsureShopAsync(domain);
        var carts = await _db.Carts.Where(c => c.ShopId == shop.Id).ToListAsync();
        foreach (var c in carts)
        {
            c.Status = c.Id == cartId ? "live" : "draft";
            if (c.Id == cartId) c.TrafficAllocation = 100;
            c.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return carts.First(c => c.Id == cartId);
    }

    public async Task<object> GetLiveCartPayloadAsync(string domain)
    {
        var shop = await EnsureShopAsync(domain);
        await EnsureDefaultCartAsync(shop.Id);
        var carts = await _db.Carts
            .Where(c => c.ShopId == shop.Id && (c.Status == "live" || c.TrafficAllocation > 0))
            .ToListAsync();
        var live = carts.FirstOrDefault(c => c.Status == "live") ?? carts.First();
        var rules = await _db.DiscountRules.Where(r => r.ShopId == shop.Id && r.Enabled).ToListAsync();

        return new
        {
            cartId = live.Id,
            config = JsonSerializer.Deserialize<object>(live.ConfigJson),
            carts = carts.Select(c => new
            {
                id = c.Id,
                trafficAllocation = c.TrafficAllocation,
                config = JsonSerializer.Deserialize<object>(c.ConfigJson)
            }),
            discountRules = rules.Select(r => new
            {
                id = r.Id,
                name = r.Name,
                ruleType = r.RuleType,
                conditions = JsonSerializer.Deserialize<object>(r.ConditionsJson),
                actions = JsonSerializer.Deserialize<object>(r.ActionsJson)
            })
        };
    }

    public async Task TrackAsync(string domain, string eventType, string? cartId = null, object? meta = null)
    {
        var shop = await EnsureShopAsync(domain);
        _db.AnalyticsEvents.Add(new AnalyticsEventRecord
        {
            ShopId = shop.Id,
            CartId = cartId,
            EventType = eventType,
            MetaJson = meta == null ? null : JsonSerializer.Serialize(meta)
        });
        await _db.SaveChangesAsync();
    }

    public async Task<object> GetAnalyticsAsync(string domain, int days = 30)
    {
        var shop = await EnsureShopAsync(domain);
        var since = DateTime.UtcNow.AddDays(-days);
        var grouped = await _db.AnalyticsEvents
            .Where(e => e.ShopId == shop.Id && e.CreatedAt >= since)
            .GroupBy(e => e.EventType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync();

        int Get(string t) => grouped.FirstOrDefault(x => x.Type == t)?.Count ?? 0;
        var opens = Get("open");
        var upsells = Get("add_upsell");
        var checkouts = Get("checkout_click");
        return new
        {
            opens,
            upsells,
            checkouts,
            rewardReached = Get("reward_tier_reached"),
            discountApplied = Get("discount_applied"),
            upsellCtr = opens == 0 ? 0 : Math.Round(upsells * 1000.0 / opens) / 10.0,
            checkoutRate = opens == 0 ? 0 : Math.Round(checkouts * 1000.0 / opens) / 10.0,
            days
        };
    }

    public static string NormalizeShop(string shop)
    {
        shop = shop.Trim().ToLowerInvariant();
        if (!shop.Contains('.')) shop += ".myshopify.com";
        return shop;
    }
}

public static class ShopifyCrypto
{
    public static bool ValidateQueryHmac(IQueryCollection query, string secret)
    {
        if (!query.TryGetValue("hmac", out var hmacVal)) return false;
        var hmac = hmacVal.ToString();
        var map = query
            .Where(k => !string.Equals(k.Key, "hmac", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(k.Key, "signature", StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k.Key, StringComparer.Ordinal)
            .Select(k => $"{k.Key}={k.Value}");
        var message = string.Join("&", map);
        using var hasher = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = BitConverter.ToString(hasher.ComputeHash(Encoding.UTF8.GetBytes(message))).Replace("-", "").ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hash),
            Encoding.UTF8.GetBytes(hmac.ToLowerInvariant()));
    }

    public static bool ValidateAppProxy(IQueryCollection query, string secret)
    {
        if (!query.TryGetValue("signature", out var sigVal)) return false;
        var signature = sigVal.ToString();
        var map = query
            .Where(k => !string.Equals(k.Key, "signature", StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k.Key, StringComparer.Ordinal)
            .Select(k => $"{k.Key}={k.Value}");
        var message = string.Join("", map);
        using var hasher = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = BitConverter.ToString(hasher.ComputeHash(Encoding.UTF8.GetBytes(message))).Replace("-", "").ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hash),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }

    public static bool ValidateWebhookHmac(string body, string? hmacHeader, string secret)
    {
        if (string.IsNullOrEmpty(hmacHeader)) return false;
        using var hasher = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToBase64String(hasher.ComputeHash(Encoding.UTF8.GetBytes(body)));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hash),
            Encoding.UTF8.GetBytes(hmacHeader));
    }
}
