using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using UpCard.Web.Services;

namespace UpCard.Web.Pages;

public class AnalyticsModel : UpCardPageModel
{
    private readonly CartService _carts;
    public AnalyticsModel(CartService carts) => _carts = carts;

    public int Opens { get; set; }
    public int Upsells { get; set; }
    public int Checkouts { get; set; }
    public int RewardReached { get; set; }
    public int DiscountApplied { get; set; }
    public double UpsellCtr { get; set; }
    public double CheckoutRate { get; set; }
    public int Days { get; set; } = 30;

    public async Task<IActionResult> OnGetAsync()
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        var summary = await _carts.GetAnalyticsAsync(ShopDomain);
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(summary));
        var root = doc.RootElement;
        Opens = root.GetProperty("opens").GetInt32();
        Upsells = root.GetProperty("upsells").GetInt32();
        Checkouts = root.GetProperty("checkouts").GetInt32();
        RewardReached = root.GetProperty("rewardReached").GetInt32();
        DiscountApplied = root.GetProperty("discountApplied").GetInt32();
        UpsellCtr = root.GetProperty("upsellCtr").GetDouble();
        CheckoutRate = root.GetProperty("checkoutRate").GetDouble();
        Days = root.GetProperty("days").GetInt32();
        return Page();
    }
}
