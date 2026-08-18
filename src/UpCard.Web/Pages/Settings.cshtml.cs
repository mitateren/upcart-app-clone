using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UpCard.Web.Services;

namespace UpCard.Web.Pages;

public class SettingsModel : PageModel
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
    private string Shop => User.FindFirstValue("shop") ?? Request.Query["shop"].ToString();

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Shop)) return RedirectToPage("/Index");
        var (_, carts) = await _carts.ListCartsAsync(Shop);
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
        Message = "Settings saved";
        return Page();
    }
}
