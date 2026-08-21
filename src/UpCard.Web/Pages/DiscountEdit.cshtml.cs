using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UpCard.Web.Data;
using UpCard.Web.Models;
using UpCard.Web.Services;

namespace UpCard.Web.Pages;

public class DiscountEditModel : UpCardPageModel
{
    private readonly CartService _carts;
    private readonly AppDbContext _db;
    public DiscountEditModel(CartService carts, AppDbContext db)
    {
        _carts = carts;
        _db = db;
    }

    [BindProperty] public string? Id { get; set; }
    [BindProperty] public string Title { get; set; } = "Free shipping";
    [BindProperty] public string Code { get; set; } = "FREESHIP";
    [BindProperty] public string Description { get; set; } = "Spend more to unlock free shipping on your order.";
    [BindProperty] public string TriggerType { get; set; } = "cart_total";
    [BindProperty] public decimal TriggerMin { get; set; } = 50;
    [BindProperty] public string? TriggerType2 { get; set; }
    [BindProperty] public decimal? TriggerMin2 { get; set; }
    [BindProperty] public string RewardType { get; set; } = "free_shipping";
    [BindProperty] public decimal RewardValue { get; set; } = 10;

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        if (string.IsNullOrEmpty(id)) return Page();
        var rule = await _db.DiscountRules.FirstOrDefaultAsync(r => r.Id == id);
        if (rule == null) return NotFound();
        Id = rule.Id;
        Title = rule.Name;
        var c = JsonNode.Parse(rule.ConditionsJson)?.AsObject();
        Code = c?["code"]?.GetValue<string>() ?? "";
        Description = c?["description"]?.GetValue<string>() ?? "";
        var triggers = c?["triggers"]?.AsArray();
        if (triggers is { Count: > 0 })
        {
            TriggerType = triggers[0]?["type"]?.GetValue<string>() ?? "cart_total";
            TriggerMin = triggers[0]?["min"]?.GetValue<decimal>() ?? 50;
            if (triggers.Count > 1)
            {
                TriggerType2 = triggers[1]?["type"]?.GetValue<string>();
                TriggerMin2 = triggers[1]?["min"]?.GetValue<decimal>();
            }
        }
        var a = JsonNode.Parse(rule.ActionsJson)?.AsObject();
        RewardType = a?["reward"]?.GetValue<string>() ?? rule.RuleType;
        RewardValue = a?["value"]?.GetValue<decimal>() ?? a?["discountPercent"]?.GetValue<decimal>() ?? 10;
        return Page();
    }

    public Task<IActionResult> OnPostSaveDraftAsync() => Save(false);
    public Task<IActionResult> OnPostPublishAsync() => Save(true);

    private async Task<IActionResult> Save(bool publish)
    {
        var gate = RequireShop();
        if (gate != null) return gate;
        var shop = await _carts.EnsureShopAsync(ShopDomain);
        var triggers = new JsonArray
        {
            new JsonObject { ["type"] = TriggerType, ["min"] = TriggerMin }
        };
        if (!string.IsNullOrWhiteSpace(TriggerType2) && TriggerMin2.HasValue)
            triggers.Add(new JsonObject { ["type"] = TriggerType2, ["min"] = TriggerMin2.Value });

        var conditions = new JsonObject
        {
            ["code"] = Code,
            ["description"] = Description,
            ["triggers"] = triggers
        };
        var actions = new JsonObject
        {
            ["reward"] = RewardType,
            ["value"] = RewardValue,
            ["discountPercent"] = RewardType == "percentage" ? RewardValue : null
        };

        DiscountRuleRecord rule;
        if (!string.IsNullOrEmpty(Id))
            rule = await _db.DiscountRules.FirstAsync(r => r.Id == Id);
        else
        {
            rule = new DiscountRuleRecord { ShopId = shop.Id };
            _db.DiscountRules.Add(rule);
        }

        rule.Name = string.IsNullOrWhiteSpace(Title) ? "Untitled discount" : Title;
        rule.RuleType = RewardType;
        rule.Enabled = publish;
        rule.ConditionsJson = conditions.ToJsonString();
        rule.ActionsJson = actions.ToJsonString();
        rule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToAppPage("/Discounts");
    }
}
