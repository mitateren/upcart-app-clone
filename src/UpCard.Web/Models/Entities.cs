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
    public string Name { get; set; } = "Varsayılan sepet";
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
            ["title"] = "Sepetiniz",
            ["showItemCount"] = true,
            ["showLogo"] = false,
            ["logoUrl"] = "",
            ["closeButtonStyle"] = "x"
        },
        ["announcements"] = new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["text"] = "750₺ üzeri siparişlerde ücretsiz kargo — teklif {TIMER} içinde bitiyor",
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
            ["completedText"] = "Tüm ödülleri açtınız!",
            ["tiers"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["id"] = "tier-1",
                    ["threshold"] = 50,
                    ["type"] = "shipping",
                    ["textBefore"] = "Ücretsiz kargo için {remaining} daha ekleyin",
                    ["textAfter"] = "Ücretsiz kargo açıldı!"
                },
                new Dictionary<string, object?>
                {
                    ["id"] = "tier-2",
                    ["threshold"] = 100,
                    ["type"] = "discount",
                    ["textBefore"] = "%10 indirim için {remaining} daha ekleyin",
                    ["textAfter"] = "%10 indirim açıldı!",
                    ["discountPercent"] = 10
                }
            }
        },
        ["upsells"] = new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["title"] = "Bunları da beğenebilirsiniz",
            ["addButtonLabel"] = "Ekle",
            ["useAi"] = true,
            ["algorithm"] = "related",
            ["smartVariantMatching"] = true,
            ["manualProductIds"] = Array.Empty<string>(),
            ["maxItems"] = 6
        },
        ["recommendations"] = new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["title"] = "Popüler ürünler",
            ["emptyCartOnly"] = true,
            ["maxItems"] = 4
        },
        ["addons"] = new Dictionary<string, object?>
        {
            ["enabled"] = false,
            ["mode"] = "product",
            ["title"] = "Kargo koruması ekle",
            ["description"] = "Siparişinizi kayıp veya hasara karşı koruyun",
            ["productVariantId"] = "",
            ["productTitle"] = "",
            ["shippingTiers"] = Array.Empty<object>()
        },
        ["discountCodes"] = new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["placeholder"] = "İndirim kodu",
            ["buttonLabel"] = "Uygula"
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
            ["alt"] = "Güvenli ödeme"
        },
        ["additionalNotes"] = new Dictionary<string, object?>
        {
            ["enabled"] = false,
            ["label"] = "Sipariş notu",
            ["placeholder"] = "Özel talimatlarınız…",
            ["required"] = false
        },
        ["subscriptionUpgrades"] = new Dictionary<string, object?>
        {
            ["enabled"] = false,
            ["title"] = "Abone ol, tasarruf et",
            ["oneTimeLabel"] = "Tek seferlik",
            ["subscribeLabel"] = "Abone ol"
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
            ["openCartSelectors"] = "a[href='/cart'], a[href$='/cart'], a[href*='/cart'], #cart-icon-bubble, .header__icon--cart, [aria-controls='CartDrawer'], [aria-controls='cart-drawer']",
            ["addToCartSelectors"] = "form[action*='/cart/add'] [type='submit'], form[action*='/cart/add'] button, [name='add'], .product-form__submit",
            ["shadowDom"] = false,
            ["continueShopping"] = true,
            ["continueShoppingLabel"] = "Alışverişe devam et",
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
            ["emptyCart"] = "Sepetiniz boş",
            ["subtotal"] = "Ara toplam",
            ["savings"] = "Tasarrufunuz",
            ["checkout"] = "Ödemeye geç",
            ["remove"] = "Kaldır",
            ["quantity"] = "Adet"
        }
    };

    /// <summary>Mevcut İngilizce varsayılan metinleri Türkçe’ye çevirir (özel metinlere dokunmaz).</summary>
    public static string MigrateEnglishCopyToTurkish(string json)
    {
        if (string.IsNullOrEmpty(json)) return json;
        var pairs = new (string En, string Tr)[]
        {
            ("Your cart is empty", "Sepetiniz boş"),
            ("You're saving", "Tasarrufunuz"),
            ("You've unlocked all rewards!", "Tüm ödülleri açtınız!"),
            ("Add {remaining} more for free shipping", "Ücretsiz kargo için {remaining} daha ekleyin"),
            ("Free shipping unlocked!", "Ücretsiz kargo açıldı!"),
            ("Add {remaining} more for 10% off", "%10 indirim için {remaining} daha ekleyin"),
            ("10% discount unlocked!", "%10 indirim açıldı!"),
            ("You may also like", "Bunları da beğenebilirsiniz"),
            ("Popular products", "Popüler ürünler"),
            ("Add shipping protection", "Kargo koruması ekle"),
            ("Protect your order against loss or damage", "Siparişinizi kayıp veya hasara karşı koruyun"),
            ("Discount code", "İndirim kodu"),
            ("Trusted checkout", "Güvenli ödeme"),
            ("Order notes", "Sipariş notu"),
            ("Special instructions…", "Özel talimatlarınız…"),
            ("Special instructions...", "Özel talimatlarınız…"),
            ("Subscribe & save", "Abone ol, tasarruf et"),
            ("One-time", "Tek seferlik"),
            ("Continue shopping", "Alışverişe devam et"),
            ("Free shipping on orders over $75 — offer ends in {TIMER}", "750₺ üzeri siparişlerde ücretsiz kargo — teklif {TIMER} içinde bitiyor"),
            ("\"Your cart\"", "\"Sepetiniz\""),
            ("\"Subtotal\"", "\"Ara toplam\""),
            ("\"Checkout\"", "\"Ödemeye geç\""),
            ("\"Remove\"", "\"Kaldır\""),
            ("\"Apply\"", "\"Uygula\""),
            ("\"Add\"", "\"Ekle\""),
            ("\"Qty\"", "\"Adet\""),
            ("\"Subscribe\"", "\"Abone ol\""),
        };
        foreach (var (en, tr) in pairs)
            json = json.Replace(en, tr, StringComparison.Ordinal);
        return json;
    }
}
