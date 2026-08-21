using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UpCard.Web.Data;
using UpCard.Web.Models;
using UpCard.Web.Services;

namespace UpCard.Web.Pages;

public class DiscountsModel : UpCardPageModel
{
    private readonly CartService _carts;
    private readonly AppDbContext _db;
    public DiscountsModel(CartService carts, AppDbContext db)
    {
        _carts = carts;
        _db = db;
    }

    public List<DiscountRuleRecord> Live { get; set; } = new();
    public List<DiscountRuleRecord> Draft { get; set; } = new();
    public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        var shop = await _carts.EnsureShopAsync(ShopDomain);
        var all = await _db.DiscountRules.Where(r => r.ShopId == shop.Id).OrderByDescending(r => r.UpdatedAt).ToListAsync();
        Live = all.Where(r => r.Enabled).ToList();
        Draft = all.Where(r => !r.Enabled).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostPublishAsync(string id)
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        var rule = await _db.DiscountRules.FirstAsync(r => r.Id == id);
        rule.Enabled = true;
        rule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        Message = "İndirim yayınlandı";
        return await OnGetAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        var rule = await _db.DiscountRules.FirstAsync(r => r.Id == id);
        _db.DiscountRules.Remove(rule);
        await _db.SaveChangesAsync();
        return await OnGetAsync();
    }

    public static string TriggerSummary(DiscountRuleRecord r)
    {
        try
        {
            var node = JsonNode.Parse(r.ConditionsJson)?.AsObject();
            var triggers = node?["triggers"]?.AsArray();
            if (triggers == null || triggers.Count == 0)
            {
                if (node?["minCartTotal"] != null)
                    return $"Sepet tutarı ({node["minCartTotal"]}+)";
                return "—";
            }
            return string.Join(" · ", triggers.Select(t =>
            {
                var type = t?["type"]?.GetValue<string>() ?? "";
                return type switch
                {
                    "cart_total" => $"Sepet tutarı ({t?["min"]}+)",
                    "cart_quantity" => $"Sepet adedi ({t?["min"]}+)",
                    "products" => "Belirli ürünler",
                    "collections" => "Belirli koleksiyonlar",
                    "subscription" => "Abonelik",
                    "logged_in" => "Giriş yapmış",
                    _ => type
                };
            }));
        }
        catch { return "—"; }
    }

    public static string RewardSummary(DiscountRuleRecord r)
    {
        try
        {
            var a = JsonNode.Parse(r.ActionsJson)?.AsObject();
            var reward = a?["reward"]?.GetValue<string>() ?? r.RuleType;
            return reward switch
            {
                "free_shipping" => "Ücretsiz kargo",
                "percentage" => $"%{a?["value"] ?? a?["discountPercent"]} indirim",
                "fixed" => $"{a?["value"]} indirim",
                "free_gift" => "Hediye ürün",
                _ => r.RuleType
            };
        }
        catch { return r.RuleType; }
    }
}
