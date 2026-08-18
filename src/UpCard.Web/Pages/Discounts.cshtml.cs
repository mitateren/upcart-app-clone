using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UpCard.Web.Data;
using UpCard.Web.Models;
using UpCard.Web.Services;

namespace UpCard.Web.Pages;

public class DiscountsModel : PageModel
{
    private readonly CartService _carts;
    private readonly AppDbContext _db;
    public DiscountsModel(CartService carts, AppDbContext db)
    {
        _carts = carts;
        _db = db;
    }

    public List<DiscountRuleRecord> Rules { get; set; } = new();
    public string? Message { get; set; }
    private string Shop => User.FindFirstValue("shop") ?? Request.Query["shop"].ToString();

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Shop)) return RedirectToPage("/Index");
        var shop = await _carts.EnsureShopAsync(Shop);
        Rules = await _db.DiscountRules.Where(r => r.ShopId == shop.Id).OrderByDescending(r => r.UpdatedAt).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(string name, string ruleType, decimal minCartTotal, decimal discountPercent)
    {
        var shop = await _carts.EnsureShopAsync(Shop);
        _db.DiscountRules.Add(new DiscountRuleRecord
        {
            ShopId = shop.Id,
            Name = string.IsNullOrWhiteSpace(name) ? "New rule" : name,
            RuleType = string.IsNullOrWhiteSpace(ruleType) ? "discount" : ruleType,
            ConditionsJson = JsonSerializer.Serialize(new { minCartTotal }),
            ActionsJson = JsonSerializer.Serialize(new { discountPercent })
        });
        await _db.SaveChangesAsync();
        Message = "Rule created";
        return await OnGetAsync();
    }

    public async Task<IActionResult> OnPostToggleAsync(string id)
    {
        var rule = await _db.DiscountRules.FirstAsync(r => r.Id == id);
        rule.Enabled = !rule.Enabled;
        rule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return await OnGetAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var rule = await _db.DiscountRules.FirstAsync(r => r.Id == id);
        _db.DiscountRules.Remove(rule);
        await _db.SaveChangesAsync();
        return await OnGetAsync();
    }
}
