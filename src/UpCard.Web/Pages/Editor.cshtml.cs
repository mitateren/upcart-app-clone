using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UpCard.Web.Models;
using UpCard.Web.Services;

namespace UpCard.Web.Pages;

public class EditorModel : PageModel
{
    private readonly CartService _carts;
    public EditorModel(CartService carts) => _carts = carts;

    [BindProperty] public string CartId { get; set; } = "";
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public CartEditorForm Form { get; set; } = new();
    public string Status { get; set; } = "";
    public string? Message { get; set; }

    private string Shop =>
        User.FindFirstValue("shop") ?? Request.Query["shop"].ToString();

    public async Task<IActionResult> OnGetAsync(string? cartId)
    {
        if (string.IsNullOrWhiteSpace(Shop)) return RedirectToPage("/Index");
        var (_, carts) = await _carts.ListCartsAsync(Shop);
        var cart = !string.IsNullOrEmpty(cartId)
            ? await _carts.GetCartAsync(cartId)
            : carts.FirstOrDefault(c => c.Status == "live") ?? carts.First();
        if (cart == null) return NotFound();
        LoadCart(cart);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Shop)) return RedirectToPage("/Index");
        var existing = await _carts.GetCartAsync(CartId);
        if (existing == null) return NotFound();
        var json = ConfigFormMapper.ApplyToJson(existing.ConfigJson, Form);
        await _carts.UpdateCartConfigAsync(CartId, json, Name);
        existing = await _carts.GetCartAsync(CartId);
        LoadCart(existing!);
        Message = "Cart kaydedildi. Storefront birkaç saniye içinde güncellenir.";
        return Page();
    }

    public async Task<IActionResult> OnPostResetAsync()
    {
        if (string.IsNullOrWhiteSpace(Shop)) return RedirectToPage("/Index");
        var defaults = CartConfigDefaults.CreateDefaultJson();
        await _carts.UpdateCartConfigAsync(CartId, defaults, Name);
        var cart = await _carts.GetCartAsync(CartId);
        LoadCart(cart!);
        Message = "Varsayılan Upcart tarzı ayarlara dönüldü.";
        return Page();
    }

    private void LoadCart(Models.CartRecord cart)
    {
        CartId = cart.Id;
        Name = cart.Name;
        Status = cart.Status;
        Form = ConfigFormMapper.FromJson(cart.ConfigJson);
    }
}
