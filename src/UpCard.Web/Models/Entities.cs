using System.Text.Json;
using System.Text.Json.Serialization;

namespace UpCard.Web.Models;

public class ShopRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Domain { get; set; } = "";
    public string? AccessToken { get; set; }
    public string? Scope { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<CartRecord> Carts { get; set; } = new();
    public List<DiscountRuleRecord> DiscountRules { get; set; } = new();
    public List<AnalyticsEventRecord> Events { get; set; } = new();
}

public class CartRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ShopId { get; set; } = "";
    public ShopRecord? Shop { get; set; }
    public string Name { get; set; } = "Default cart";
    public string Status { get; set; } = "draft"; // draft | live
    public int TrafficAllocation { get; set; } = 100;
    public string ConfigJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class DiscountRuleRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ShopId { get; set; } = "";
    public ShopRecord? Shop { get; set; }
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public string RuleType { get; set; } = "discount";
    public string ConditionsJson { get; set; } = "{}";
    public string ActionsJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class AnalyticsEventRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ShopId { get; set; } = "";
    public ShopRecord? Shop { get; set; }
    public string? CartId { get; set; }
    public string EventType { get; set; } = "";
    public string? MetaJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class CartConfigDefaults
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static string CreateDefaultJson() =>
        JsonSerializer.Serialize(CreateDefault(), JsonOptions);

    public static Dictionary<string, object?> CreateDefault() => new()
    {
        ["design"] = new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["backgroundColor"] = "#ffffff",
            ["textColor"] = "#111111",
            ["buttonBackground"] = "#111111",
            ["buttonTextColor"] = "#ffffff",
            ["accentColor"] = "#0a7c5e",
            ["borderRadius"] = 8,
            ["showStrikethroughPrices"] = true,
            ["showSavingsBelowPrices"] = true,
            ["fontFamily"] = "inherit"
        },
        ["header"] = new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["title"] = "Your cart",
            ["showItemCount"] = true,
            ["showLogo"] = false,
            ["logoUrl"] = "",
            ["closeButtonStyle"] = "x"
        },
        ["announcements"] = new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["text"] = "Free shipping on orders over $75 — offer ends in {TIMER}",
            ["backgroundColor"] = "#111111",
            ["textColor"] = "#ffffff",
            ["timerMinutes"] = 15,
            ["showTimer"] = true
        },
        ["rewards"] = new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["basis"] = "cart_total",
            ["barColor"] = "#0a7c5e",
            ["backgroundColor"] = "#e8f5f0",
            ["completedText"] = "You've unlocked all rewards!",
            ["tiers"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["id"] = "tier-1",
                    ["threshold"] = 50,
                    ["type"] = "shipping",
                    ["textBefore"] = "Add {remaining} more for free shipping",
                    ["textAfter"] = "Free shipping unlocked!"
                },
                new Dictionary<string, object?>
                {
                    ["id"] = "tier-2",
                    ["threshold"] = 100,
                    ["type"] = "discount",
                    ["textBefore"] = "Add {remaining} more for 10% off",
                    ["textAfter"] = "10% discount unlocked!",
                    ["discountPercent"] = 10
                }
            }
        },
        ["upsells"] = new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["title"] = "You may also like",
            ["addButtonLabel"] = "Add",
            ["useAi"] = true,
            ["algorithm"] = "related",
            ["smartVariantMatching"] = true,
            ["manualProductIds"] = Array.Empty<string>(),
            ["maxItems"] = 6
        },
        ["recommendations"] = new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["title"] = "Popular products",
            ["emptyCartOnly"] = true,
            ["maxItems"] = 4
        },
        ["addons"] = new Dictionary<string, object?>
        {
            ["enabled"] = false,
            ["mode"] = "product",
            ["title"] = "Add shipping protection",
            ["description"] = "Protect your order against loss or damage",
            ["productVariantId"] = "",
            ["productTitle"] = "",
            ["shippingTiers"] = Array.Empty<object>()
        },
        ["discountCodes"] = new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["placeholder"] = "Discount code",
            ["buttonLabel"] = "Apply"
        },
        ["expressPayments"] = new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["alignment"] = "stretch"
        },
        ["trustBadges"] = new Dictionary<string, object?>
        {
            ["enabled"] = false,
            ["position"] = "bottom",
            ["imageUrl"] = "",
            ["alt"] = "Trusted checkout"
        },
        ["additionalNotes"] = new Dictionary<string, object?>
        {
            ["enabled"] = false,
            ["label"] = "Order notes",
            ["placeholder"] = "Special instructions…",
            ["required"] = false
        },
        ["subscriptionUpgrades"] = new Dictionary<string, object?>
        {
            ["enabled"] = false,
            ["title"] = "Subscribe & save",
            ["oneTimeLabel"] = "One-time",
            ["subscribeLabel"] = "Subscribe"
        },
        ["stickyCart"] = new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["position"] = "bottom-right",
            ["backgroundColor"] = "#111111",
            ["iconColor"] = "#ffffff",
            ["showCount"] = true
        },
        ["behavior"] = new Dictionary<string, object?>
        {
            ["openOnAddToCart"] = true,
            ["position"] = "right",
            ["openCartSelectors"] = "a[href='/cart'], a[href$='/cart']",
            ["addToCartSelectors"] = "form[action='/cart/add'] [type='submit'], [name='add']",
            ["shadowDom"] = false,
            ["continueShopping"] = true,
            ["continueShoppingLabel"] = "Continue shopping",
            ["goToCartOnCheckout"] = false,
            ["disableFixedFooter"] = false
        },
        ["customCss"] = "",
        ["customHtml"] = new Dictionary<string, object?>
        {
            ["beforeAnnouncements"] = "",
            ["betweenItems"] = "",
            ["aboveCheckout"] = "",
            ["scripts"] = ""
        },
        ["translations"] = new Dictionary<string, object?>
        {
            ["emptyCart"] = "Your cart is empty",
            ["subtotal"] = "Subtotal",
            ["savings"] = "You're saving",
            ["checkout"] = "Checkout",
            ["remove"] = "Remove",
            ["quantity"] = "Qty"
        }
    };
}
