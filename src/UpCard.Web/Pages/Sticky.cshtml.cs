using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using UpCard.Web.Services;

namespace UpCard.Web.Pages;

public class StickyModel : UpCardPageModel
{
    private readonly CartService _carts;
    public StickyModel(CartService carts) => _carts = carts;

    [BindProperty] public string CartId { get; set; } = "";
    [BindProperty] public bool Enabled { get; set; }
    [BindProperty] public string Position { get; set; } = "bottom-right";
    [BindProperty] public string BackgroundColor { get; set; } = "#000000";
    [BindProperty] public string IconColor { get; set; } = "#ffffff";
    [BindProperty] public string QtyBg { get; set; } = "#e42828";
    [BindProperty] public string QtyFg { get; set; } = "#ffffff";
    [BindProperty] public string CustomCss { get; set; } = "";
    [BindProperty] public bool ShowCount { get; set; } = true;
    public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        var (_, carts) = await _carts.ListCartsAsync(ShopDomain);
        var cart = carts.FirstOrDefault(c => c.Status == "live") ?? carts.First();
        CartId = cart.Id;
        var sticky = JsonNode.Parse(cart.ConfigJson)?["stickyCart"]?.AsObject();
        Enabled = sticky?["enabled"]?.GetValue<bool>() ?? false;
        Position = sticky?["position"]?.GetValue<string>() ?? "bottom-right";
        BackgroundColor = sticky?["backgroundColor"]?.GetValue<string>() ?? "#000000";
        IconColor = sticky?["iconColor"]?.GetValue<string>() ?? "#ffffff";
        ShowCount = sticky?["showCount"]?.GetValue<bool>() ?? true;
        CustomCss = JsonNode.Parse(cart.ConfigJson)?["customCss"]?.GetValue<string>() ?? "";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        var cart = await _carts.GetCartAsync(CartId);
        if (cart == null) return NotFound();
        var node = JsonNode.Parse(cart.ConfigJson)!.AsObject();
        node["stickyCart"] = new JsonObject
        {
            ["enabled"] = Enabled,
            ["position"] = Position,
            ["backgroundColor"] = BackgroundColor,
            ["iconColor"] = IconColor,
            ["showCount"] = ShowCount,
            ["qtyBackground"] = QtyBg,
            ["qtyColor"] = QtyFg
        };
        if (!string.IsNullOrWhiteSpace(CustomCss))
            node["customCss"] = CustomCss;
        await _carts.UpdateCartConfigAsync(CartId, node.ToJsonString());
        Message = "Sticky cart saved";
        return await OnGetAsync();
    }
}
