using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UpCard.Web.Models;
using UpCard.Web.Services;

namespace UpCard.Web.Pages;

public class CartsModel : PageModel
{
    private readonly CartService _carts;
    public CartsModel(CartService carts) => _carts = carts;

    public List<CartRecord> Carts { get; set; } = new();
    public string? Message { get; set; }
    private string Shop => User.FindFirstValue("shop") ?? Request.Query["shop"].ToString();

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Shop)) return RedirectToPage("/Index");
        (_, Carts) = await _carts.ListCartsAsync(Shop);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(string name)
    {
        await _carts.CreateCartAsync(Shop, string.IsNullOrWhiteSpace(name) ? "New cart" : name);
        Message = "Cart created";
        (_, Carts) = await _carts.ListCartsAsync(Shop);
        return Page();
    }

    public async Task<IActionResult> OnPostPublishAsync(string cartId)
    {
        await _carts.PublishCartAsync(Shop, cartId);
        Message = "Published";
        (_, Carts) = await _carts.ListCartsAsync(Shop);
        return Page();
    }
}
