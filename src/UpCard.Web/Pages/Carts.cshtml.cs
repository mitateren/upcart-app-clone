using Microsoft.AspNetCore.Mvc;
using UpCard.Web.Models;
using UpCard.Web.Services;

namespace UpCard.Web.Pages;

public class CartsModel : UpCardPageModel
{
    private readonly CartService _carts;
    public CartsModel(CartService carts) => _carts = carts;

    public List<CartRecord> Carts { get; set; } = new();
    public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        (_, Carts) = await _carts.ListCartsAsync(ShopDomain);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(string name)
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        await _carts.CreateCartAsync(ShopDomain, string.IsNullOrWhiteSpace(name) ? "New cart" : name);
        Message = "Sepet oluşturuldu";
        (_, Carts) = await _carts.ListCartsAsync(ShopDomain);
        return Page();
    }

    public async Task<IActionResult> OnPostPublishAsync(string cartId)
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        await _carts.PublishCartAsync(ShopDomain, cartId);
        Message = "Yayınlandı";
        (_, Carts) = await _carts.ListCartsAsync(ShopDomain);
        return Page();
    }
}
