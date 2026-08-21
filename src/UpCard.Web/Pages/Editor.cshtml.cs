using Microsoft.AspNetCore.Mvc;
using UpCard.Web.Models;
using UpCard.Web.Services;

namespace UpCard.Web.Pages;

public class EditorModel : UpCardPageModel
{
    private readonly CartService _carts;
    public EditorModel(CartService carts) => _carts = carts;

    [BindProperty] public string CartId { get; set; } = "";
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public CartEditorForm Form { get; set; } = new();
    public string Status { get; set; } = "";
    public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync(string? cartId)
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        var (_, carts) = await _carts.ListCartsAsync(ShopDomain);
        var cart = !string.IsNullOrEmpty(cartId)
            ? await _carts.GetCartAsync(cartId)
            : carts.FirstOrDefault(c => c.Status == "live") ?? carts.First();
        if (cart == null) return NotFound();
        LoadCart(cart);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        var existing = await _carts.GetCartAsync(CartId);
        if (existing == null) return NotFound();
        var json = ConfigFormMapper.ApplyToJson(existing.ConfigJson, Form);
        await _carts.UpdateCartConfigAsync(CartId, json, Name);
        existing = await _carts.GetCartAsync(CartId);
        LoadCart(existing!);
        Message = "Cart kaydedildi.";
        var section = Request.Query["section"].ToString();
        if (!string.IsNullOrEmpty(section))
            return RedirectToAppPage("/Editor", new { cartId = CartId, section });
        return Page();
    }

    public async Task<IActionResult> OnPostResetAsync()
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        var defaults = CartConfigDefaults.CreateDefaultJson();
        await _carts.UpdateCartConfigAsync(CartId, defaults, Name);
        var cart = await _carts.GetCartAsync(CartId);
        LoadCart(cart!);
        Message = "Varsayılanlara dönüldü.";
        return Page();
    }

    private void LoadCart(CartRecord cart)
    {
        CartId = cart.Id;
        Name = cart.Name;
        Status = cart.Status;
        Form = ConfigFormMapper.FromJson(cart.ConfigJson);
    }
}
