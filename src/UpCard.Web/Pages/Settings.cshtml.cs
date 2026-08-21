using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using UpCard.Web.Services;

namespace UpCard.Web.Pages;

public class SettingsModel : UpCardPageModel
{
    private readonly CartService _carts;
    public SettingsModel(CartService carts) => _carts = carts;

    [BindProperty] public string CartId { get; set; } = "";
    [BindProperty] public string CustomCss { get; set; } = "";
    [BindProperty] public string BeforeAnnouncements { get; set; } = "";
    [BindProperty] public string BetweenItems { get; set; } = "";
    [BindProperty] public string AboveCheckout { get; set; } = "";
    [BindProperty] public string Scripts { get; set; } = "";
    [BindProperty] public string TranslationsJson { get; set; } = "{}";
    public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        var (_, carts) = await _carts.ListCartsAsync(ShopDomain);
        var cart = carts.FirstOrDefault(c => c.Status == "live") ?? carts.First();
        CartId = cart.Id;
        var node = JsonNode.Parse(cart.ConfigJson)!.AsObject();
        CustomCss = node["customCss"]?.GetValue<string>() ?? "";
        var html = node["customHtml"]?.AsObject();
        BeforeAnnouncements = html?["beforeAnnouncements"]?.GetValue<string>() ?? "";
        BetweenItems = html?["betweenItems"]?.GetValue<string>() ?? "";
        AboveCheckout = html?["aboveCheckout"]?.GetValue<string>() ?? "";
        Scripts = html?["scripts"]?.GetValue<string>() ?? "";
        TranslationsJson = JsonSerializer.Serialize(node["translations"], new JsonSerializerOptions { WriteIndented = true });
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        var cart = await _carts.GetCartAsync(CartId);
        if (cart == null) return NotFound();
        var node = JsonNode.Parse(cart.ConfigJson)!.AsObject();
        node["customCss"] = CustomCss;
        node["customHtml"] = new JsonObject
        {
            ["beforeAnnouncements"] = BeforeAnnouncements,
            ["betweenItems"] = BetweenItems,
            ["aboveCheckout"] = AboveCheckout,
            ["scripts"] = Scripts
        };
        try { node["translations"] = JsonNode.Parse(TranslationsJson); }
        catch { Message = "Translations JSON geçersiz"; return Page(); }
        await _carts.UpdateCartConfigAsync(CartId, node.ToJsonString());
        Message = "Ayarlar kaydedildi";
        return Page();
    }
}
