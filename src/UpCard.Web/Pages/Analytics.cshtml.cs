using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UpCard.Web.Services;

namespace UpCard.Web.Pages;

public class AnalyticsModel : PageModel
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

    private string Shop => User.FindFirstValue("shop") ?? Request.Query["shop"].ToString();

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Shop)) return RedirectToPage("/Index");
        var summary = await _carts.GetAnalyticsAsync(Shop);
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
