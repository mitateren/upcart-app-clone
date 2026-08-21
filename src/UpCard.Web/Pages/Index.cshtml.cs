using Microsoft.AspNetCore.Mvc;
using UpCard.Web.Services;

namespace UpCard.Web.Pages;

public class IndexModel : UpCardPageModel
{
    private readonly CartService _carts;

    public IndexModel(CartService carts) => _carts = carts;

    public string Shop { get; set; } = "";
    public string LiveCartName { get; set; } = "-";
    public string LiveCartStatus { get; set; } = "-";
    public bool NeedsInstall { get; set; }
    public int Opens { get; set; }
    public int Upsells { get; set; }
    public int Checkouts { get; set; }
    public double UpsellCtr { get; set; }
    public double CheckoutRate { get; set; }
    public int RewardReached { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Shop = ShopDomain;

        if (string.IsNullOrWhiteSpace(Shop))
        {
            NeedsInstall = true;
            return Page();
        }

        if (User.Identity?.IsAuthenticated != true)
        {
            return Redirect($"/auth/install?shop={Uri.EscapeDataString(Shop)}&host={Uri.EscapeDataString(HostParam)}");
        }

        var (_, carts) = await _carts.ListCartsAsync(Shop);
        var live = carts.FirstOrDefault(c => c.Status == "live") ?? carts.FirstOrDefault();
        LiveCartName = live?.Name ?? "-";
        LiveCartStatus = live?.Status ?? "-";

        var summary = await _carts.GetAnalyticsAsync(Shop);
        var json = System.Text.Json.JsonSerializer.Serialize(summary);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        Opens = root.GetProperty("opens").GetInt32();
        Upsells = root.GetProperty("upsells").GetInt32();
        Checkouts = root.GetProperty("checkouts").GetInt32();
        UpsellCtr = root.GetProperty("upsellCtr").GetDouble();
        CheckoutRate = root.GetProperty("checkoutRate").GetDouble();
        RewardReached = root.GetProperty("rewardReached").GetInt32();
        return Page();
    }
}
