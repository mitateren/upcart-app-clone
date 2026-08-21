using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using UpCard.Web.Services;

namespace UpCard.Web.Pages;

public class OnboardingModel : UpCardPageModel
{
    private readonly CartService _carts;
    public OnboardingModel(CartService carts) => _carts = carts;

    [BindProperty] public int Step { get; set; } = 1;
    [BindProperty] public string Style { get; set; } = "default";
    [BindProperty] public bool ModUpsells { get; set; } = true;
    [BindProperty] public bool ModRewards { get; set; } = true;
    [BindProperty] public bool ModRecommendations { get; set; } = true;
    [BindProperty] public bool ModAddons { get; set; }
    [BindProperty] public bool ModAnnouncements { get; set; } = true;
    [BindProperty] public bool ModSubscriptions { get; set; }
    public string CartId { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(int step = 1)
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        Step = Math.Clamp(step, 1, 2);
        var (_, carts) = await _carts.ListCartsAsync(ShopDomain);
        var cart = carts.FirstOrDefault(c => c.Status == "live") ?? carts.First();
        CartId = cart.Id;
        return Page();
    }

    public async Task<IActionResult> OnPostNextAsync()
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        var (_, carts) = await _carts.ListCartsAsync(ShopDomain);
        var cart = carts.FirstOrDefault(c => c.Status == "live") ?? carts.First();
        ApplyStyle(cart.ConfigJson, out var json);
        await _carts.UpdateCartConfigAsync(cart.Id, json);
        return RedirectToAppPage("/Onboarding", new { step = 2 });
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        var (_, carts) = await _carts.ListCartsAsync(ShopDomain);
        var cart = carts.FirstOrDefault(c => c.Status == "live") ?? carts.First();
        var node = JsonNode.Parse(cart.ConfigJson)!.AsObject();
        SetEnabled(node, "upsells", ModUpsells);
        SetEnabled(node, "rewards", ModRewards);
        SetEnabled(node, "recommendations", ModRecommendations);
        SetEnabled(node, "addons", ModAddons);
        SetEnabled(node, "announcements", ModAnnouncements);
        SetEnabled(node, "subscriptionUpgrades", ModSubscriptions);
        await _carts.UpdateCartConfigAsync(cart.Id, node.ToJsonString(), cart.Name);
        return RedirectToAppPage("/Editor", new { cartId = cart.Id });
    }

    private void ApplyStyle(string existing, out string json)
    {
        var node = JsonNode.Parse(existing)!.AsObject();
        var design = node["design"]?.AsObject() ?? new JsonObject();
        switch ((Style ?? "default").ToLowerInvariant())
        {
            case "ivory":
                design["backgroundColor"] = "#fbfbf7";
                design["textColor"] = "#1a1a1a";
                design["buttonBackground"] = "#2c2c2c";
                design["buttonTextColor"] = "#ffffff";
                design["accentColor"] = "#6b6b6b";
                design["borderRadius"] = 0;
                break;
            case "electric":
                design["backgroundColor"] = "#ffffff";
                design["textColor"] = "#0b1b3a";
                design["buttonBackground"] = "#1d4ed8";
                design["buttonTextColor"] = "#ffffff";
                design["accentColor"] = "#2563eb";
                design["borderRadius"] = 12;
                break;
            case "natural":
                design["backgroundColor"] = "#fffaf3";
                design["textColor"] = "#3b2f2f";
                design["buttonBackground"] = "#c2410c";
                design["buttonTextColor"] = "#ffffff";
                design["accentColor"] = "#ea580c";
                design["borderRadius"] = 10;
                break;
            default:
                design["backgroundColor"] = "#ffffff";
                design["textColor"] = "#111111";
                design["buttonBackground"] = "#111111";
                design["buttonTextColor"] = "#ffffff";
                design["accentColor"] = "#008060";
                design["borderRadius"] = 8;
                break;
        }
        design["enabled"] = true;
        node["design"] = design;
        json = node.ToJsonString();
    }

    private static void SetEnabled(JsonObject root, string key, bool enabled)
    {
        var o = root[key]?.AsObject() ?? new JsonObject();
        o["enabled"] = enabled;
        root[key] = o;
    }
}
