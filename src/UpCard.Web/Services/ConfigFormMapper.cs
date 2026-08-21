using System.Text.Json;
using System.Text.Json.Nodes;

namespace UpCard.Web.Services;

/// <summary>Maps cart config JSON ↔ admin form modules (Upcart-style).</summary>
public static class ConfigFormMapper
{
    public static CartEditorForm FromJson(string json)
    {
        var root = ParseObject(json);
        var form = new CartEditorForm();
        var design = Obj(root, "design");
        form.DesignEnabled = Bool(design, "enabled", true);
        form.BackgroundColor = Str(design, "backgroundColor", "#ffffff");
        form.TextColor = Str(design, "textColor", "#111111");
        form.ButtonBackground = Str(design, "buttonBackground", "#111111");
        form.ButtonTextColor = Str(design, "buttonTextColor", "#ffffff");
        form.AccentColor = Str(design, "accentColor", "#008060");
        form.BorderRadius = Int(design, "borderRadius", 8);
        form.ShowStrikethrough = Bool(design, "showStrikethroughPrices", true);
        form.ShowSavings = Bool(design, "showSavingsBelowPrices", true);
        form.FontFamily = Str(design, "fontFamily", "inherit");

        var header = Obj(root, "header");
        form.HeaderEnabled = Bool(header, "enabled", true);
        form.HeaderTitle = Str(header, "title", "Sepetiniz");
        form.ShowItemCount = Bool(header, "showItemCount", true);
        form.ShowLogo = Bool(header, "showLogo", false);
        form.LogoUrl = Str(header, "logoUrl", "");

        var ann = Obj(root, "announcements");
        form.AnnouncementsEnabled = Bool(ann, "enabled", true);
        form.AnnouncementText = Str(ann, "text", "");
        form.AnnouncementBg = Str(ann, "backgroundColor", "#111111");
        form.AnnouncementFg = Str(ann, "textColor", "#ffffff");
        form.ShowTimer = Bool(ann, "showTimer", true);
        form.TimerMinutes = Int(ann, "timerMinutes", 15);

        var rewards = Obj(root, "rewards");
        form.RewardsEnabled = Bool(rewards, "enabled", true);
        form.RewardBarColor = Str(rewards, "barColor", "#008060");
        form.RewardBg = Str(rewards, "backgroundColor", "#e8f5f0");
        form.RewardCompletedText = Str(rewards, "completedText", "Tüm ödülleri açtınız!");
        form.Tier1Threshold = TierNum(rewards, 0, "threshold", 50);
        form.Tier1TextBefore = TierStr(rewards, 0, "textBefore", "Ücretsiz kargo için {remaining} daha ekleyin");
        form.Tier1TextAfter = TierStr(rewards, 0, "textAfter", "Ücretsiz kargo açıldı!");
        form.Tier2Threshold = TierNum(rewards, 1, "threshold", 100);
        form.Tier2TextBefore = TierStr(rewards, 1, "textBefore", "%10 indirim için {remaining} daha ekleyin");
        form.Tier2TextAfter = TierStr(rewards, 1, "textAfter", "%10 indirim açıldı!");
        form.Tier2DiscountPercent = TierNum(rewards, 1, "discountPercent", 10);

        var upsells = Obj(root, "upsells");
        form.UpsellsEnabled = Bool(upsells, "enabled", true);
        form.UpsellsTitle = Str(upsells, "title", "Bunları da beğenebilirsiniz");
        form.UpsellsButton = Str(upsells, "addButtonLabel", "Ekle");
        form.UpsellsUseAi = Bool(upsells, "useAi", true);
        form.UpsellsMaxItems = Int(upsells, "maxItems", 6);
        form.ManualProductIds = string.Join(", ", ArrStr(upsells, "manualProductIds"));

        var rec = Obj(root, "recommendations");
        form.RecommendationsEnabled = Bool(rec, "enabled", true);
        form.RecommendationsTitle = Str(rec, "title", "Popüler ürünler");
        form.RecommendationsEmptyOnly = Bool(rec, "emptyCartOnly", true);
        form.RecommendationsMax = Int(rec, "maxItems", 4);

        var addons = Obj(root, "addons");
        form.AddonsEnabled = Bool(addons, "enabled", false);
        form.AddonsTitle = Str(addons, "title", "Kargo koruması ekle");
        form.AddonsDescription = Str(addons, "description", "");
        form.AddonsVariantId = Str(addons, "productVariantId", "");
        form.AddonsProductTitle = Str(addons, "productTitle", "");

        var disc = Obj(root, "discountCodes");
        form.DiscountCodesEnabled = Bool(disc, "enabled", true);
        form.DiscountPlaceholder = Str(disc, "placeholder", "İndirim kodu");
        form.DiscountButton = Str(disc, "buttonLabel", "Uygula");

        var express = Obj(root, "expressPayments");
        form.ExpressEnabled = Bool(express, "enabled", true);

        var trust = Obj(root, "trustBadges");
        form.TrustEnabled = Bool(trust, "enabled", false);
        form.TrustImageUrl = Str(trust, "imageUrl", "");
        form.TrustAlt = Str(trust, "alt", "Güvenli ödeme");

        var notes = Obj(root, "additionalNotes");
        form.NotesEnabled = Bool(notes, "enabled", false);
        form.NotesLabel = Str(notes, "label", "Sipariş notu");
        form.NotesPlaceholder = Str(notes, "placeholder", "Özel talimatlarınız…");

        var sticky = Obj(root, "stickyCart");
        form.StickyEnabled = Bool(sticky, "enabled", true);
        form.StickyPosition = Str(sticky, "position", "bottom-right");
        form.StickyBg = Str(sticky, "backgroundColor", "#111111");
        form.StickyIcon = Str(sticky, "iconColor", "#ffffff");
        form.StickyShowCount = Bool(sticky, "showCount", true);

        var behavior = Obj(root, "behavior");
        form.OpenOnAddToCart = Bool(behavior, "openOnAddToCart", true);
        form.DrawerPosition = Str(behavior, "position", "right");
        form.ContinueShopping = Bool(behavior, "continueShopping", true);
        form.ContinueShoppingLabel = Str(behavior, "continueShoppingLabel", "Alışverişe devam et");

        var sub = Obj(root, "subscriptionUpgrades");
        form.SubscriptionEnabled = Bool(sub, "enabled", false);
        form.SubscriptionTitle = Str(sub, "title", "Abone ol, tasarruf et");

        form.CustomCss = Str(root, "customCss", "");
        var html = Obj(root, "customHtml");
        form.HtmlBeforeAnnouncements = Str(html, "beforeAnnouncements", "");
        form.HtmlBetweenItems = Str(html, "betweenItems", "");
        form.HtmlAboveCheckout = Str(html, "aboveCheckout", "");
        form.CustomScripts = Str(html, "scripts", "");

        var tr = Obj(root, "translations");
        form.TrEmptyCart = Str(tr, "emptyCart", "Sepetiniz boş");
        form.TrSubtotal = Str(tr, "subtotal", "Ara toplam");
        form.TrCheckout = Str(tr, "checkout", "Ödemeye geç");
        form.TrRemove = Str(tr, "remove", "Kaldır");

        form.RawJsonBackup = json;
        return form;
    }

    public static string ApplyToJson(string existingJson, CartEditorForm form)
    {
        var root = ParseObject(string.IsNullOrWhiteSpace(existingJson) ? "{}" : existingJson);

        SetObj(root, "design", new JsonObject
        {
            ["enabled"] = form.DesignEnabled,
            ["backgroundColor"] = form.BackgroundColor,
            ["textColor"] = form.TextColor,
            ["buttonBackground"] = form.ButtonBackground,
            ["buttonTextColor"] = form.ButtonTextColor,
            ["accentColor"] = form.AccentColor,
            ["borderRadius"] = form.BorderRadius,
            ["showStrikethroughPrices"] = form.ShowStrikethrough,
            ["showSavingsBelowPrices"] = form.ShowSavings,
            ["fontFamily"] = form.FontFamily
        });

        SetObj(root, "header", new JsonObject
        {
            ["enabled"] = form.HeaderEnabled,
            ["title"] = form.HeaderTitle,
            ["showItemCount"] = form.ShowItemCount,
            ["showLogo"] = form.ShowLogo,
            ["logoUrl"] = form.LogoUrl,
            ["closeButtonStyle"] = "x"
        });

        SetObj(root, "announcements", new JsonObject
        {
            ["enabled"] = form.AnnouncementsEnabled,
            ["text"] = form.AnnouncementText,
            ["backgroundColor"] = form.AnnouncementBg,
            ["textColor"] = form.AnnouncementFg,
            ["timerMinutes"] = form.TimerMinutes,
            ["showTimer"] = form.ShowTimer
        });

        SetObj(root, "rewards", new JsonObject
        {
            ["enabled"] = form.RewardsEnabled,
            ["basis"] = "cart_total",
            ["barColor"] = form.RewardBarColor,
            ["backgroundColor"] = form.RewardBg,
            ["completedText"] = form.RewardCompletedText,
            ["tiers"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = "tier-1",
                    ["threshold"] = form.Tier1Threshold,
                    ["type"] = "shipping",
                    ["textBefore"] = form.Tier1TextBefore,
                    ["textAfter"] = form.Tier1TextAfter
                },
                new JsonObject
                {
                    ["id"] = "tier-2",
                    ["threshold"] = form.Tier2Threshold,
                    ["type"] = "discount",
                    ["textBefore"] = form.Tier2TextBefore,
                    ["textAfter"] = form.Tier2TextAfter,
                    ["discountPercent"] = form.Tier2DiscountPercent
                }
            }
        });

        var manualIds = form.ManualProductIds
            .Split(new[] { ',', ' ', '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        SetObj(root, "upsells", new JsonObject
        {
            ["enabled"] = form.UpsellsEnabled,
            ["title"] = form.UpsellsTitle,
            ["addButtonLabel"] = form.UpsellsButton,
            ["useAi"] = form.UpsellsUseAi,
            ["algorithm"] = "related",
            ["smartVariantMatching"] = true,
            ["manualProductIds"] = new JsonArray(manualIds.Select(id => JsonValue.Create(id)).ToArray()),
            ["maxItems"] = form.UpsellsMaxItems
        });

        SetObj(root, "recommendations", new JsonObject
        {
            ["enabled"] = form.RecommendationsEnabled,
            ["title"] = form.RecommendationsTitle,
            ["emptyCartOnly"] = form.RecommendationsEmptyOnly,
            ["maxItems"] = form.RecommendationsMax
        });

        SetObj(root, "addons", new JsonObject
        {
            ["enabled"] = form.AddonsEnabled,
            ["mode"] = "product",
            ["title"] = form.AddonsTitle,
            ["description"] = form.AddonsDescription,
            ["productVariantId"] = form.AddonsVariantId,
            ["productTitle"] = form.AddonsProductTitle,
            ["shippingTiers"] = new JsonArray()
        });

        SetObj(root, "discountCodes", new JsonObject
        {
            ["enabled"] = form.DiscountCodesEnabled,
            ["placeholder"] = form.DiscountPlaceholder,
            ["buttonLabel"] = form.DiscountButton
        });

        SetObj(root, "expressPayments", new JsonObject
        {
            ["enabled"] = form.ExpressEnabled,
            ["alignment"] = "stretch"
        });

        SetObj(root, "trustBadges", new JsonObject
        {
            ["enabled"] = form.TrustEnabled,
            ["position"] = "bottom",
            ["imageUrl"] = form.TrustImageUrl,
            ["alt"] = form.TrustAlt
        });

        SetObj(root, "additionalNotes", new JsonObject
        {
            ["enabled"] = form.NotesEnabled,
            ["label"] = form.NotesLabel,
            ["placeholder"] = form.NotesPlaceholder,
            ["required"] = false
        });

        SetObj(root, "subscriptionUpgrades", new JsonObject
        {
            ["enabled"] = form.SubscriptionEnabled,
            ["title"] = form.SubscriptionTitle,
            ["oneTimeLabel"] = "One-time",
            ["subscribeLabel"] = "Subscribe"
        });

        SetObj(root, "stickyCart", new JsonObject
        {
            ["enabled"] = form.StickyEnabled,
            ["position"] = form.StickyPosition,
            ["backgroundColor"] = form.StickyBg,
            ["iconColor"] = form.StickyIcon,
            ["showCount"] = form.StickyShowCount
        });

        var behavior = Obj(root, "behavior");
        behavior["openOnAddToCart"] = form.OpenOnAddToCart;
        behavior["position"] = form.DrawerPosition;
        behavior["continueShopping"] = form.ContinueShopping;
        behavior["continueShoppingLabel"] = form.ContinueShoppingLabel;
        if (!behavior.ContainsKey("openCartSelectors"))
            behavior["openCartSelectors"] = "a[href='/cart'], a[href$='/cart']";
        if (!behavior.ContainsKey("addToCartSelectors"))
            behavior["addToCartSelectors"] = "form[action='/cart/add'] [type='submit'], [name='add']";
        root["behavior"] = behavior;

        root["customCss"] = form.CustomCss ?? "";
        SetObj(root, "customHtml", new JsonObject
        {
            ["beforeAnnouncements"] = form.HtmlBeforeAnnouncements ?? "",
            ["betweenItems"] = form.HtmlBetweenItems ?? "",
            ["aboveCheckout"] = form.HtmlAboveCheckout ?? "",
            ["scripts"] = form.CustomScripts ?? ""
        });

        SetObj(root, "translations", new JsonObject
        {
            ["emptyCart"] = form.TrEmptyCart,
            ["subtotal"] = form.TrSubtotal,
            ["savings"] = "You're saving",
            ["checkout"] = form.TrCheckout,
            ["remove"] = form.TrRemove,
            ["quantity"] = "Qty"
        });

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static JsonObject ParseObject(string json)
    {
        try
        {
            return JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
        }
        catch
        {
            return new JsonObject();
        }
    }

    private static JsonObject Obj(JsonObject root, string key)
    {
        if (root[key] is JsonObject o) return o;
        var n = new JsonObject();
        root[key] = n;
        return n;
    }

    private static void SetObj(JsonObject root, string key, JsonObject value) => root[key] = value;

    private static string Str(JsonObject? o, string key, string fallback) =>
        o?[key]?.GetValue<string>() ?? fallback;

    private static bool Bool(JsonObject? o, string key, bool fallback) =>
        o?[key]?.GetValue<bool>() ?? fallback;

    private static int Int(JsonObject? o, string key, int fallback)
    {
        if (o?[key] is null) return fallback;
        try { return o[key]!.GetValue<int>(); }
        catch
        {
            try { return (int)o[key]!.GetValue<double>(); }
            catch { return fallback; }
        }
    }

    private static string[] ArrStr(JsonObject? o, string key)
    {
        if (o?[key] is not JsonArray arr) return Array.Empty<string>();
        return arr.Select(x => x?.GetValue<string>() ?? "").Where(s => s.Length > 0).ToArray();
    }

    private static int TierNum(JsonObject rewards, int index, string key, int fallback)
    {
        if (rewards["tiers"] is not JsonArray arr || arr.Count <= index || arr[index] is not JsonObject t)
            return fallback;
        return Int(t, key, fallback);
    }

    private static string TierStr(JsonObject rewards, int index, string key, string fallback)
    {
        if (rewards["tiers"] is not JsonArray arr || arr.Count <= index || arr[index] is not JsonObject t)
            return fallback;
        return Str(t, key, fallback);
    }
}

public class CartEditorForm
{
    public bool DesignEnabled { get; set; } = true;
    public string BackgroundColor { get; set; } = "#ffffff";
    public string TextColor { get; set; } = "#111111";
    public string ButtonBackground { get; set; } = "#111111";
    public string ButtonTextColor { get; set; } = "#ffffff";
    public string AccentColor { get; set; } = "#008060";
    public int BorderRadius { get; set; } = 8;
    public bool ShowStrikethrough { get; set; } = true;
    public bool ShowSavings { get; set; } = true;
    public string FontFamily { get; set; } = "inherit";

    public bool HeaderEnabled { get; set; } = true;
    public string HeaderTitle { get; set; } = "Sepetiniz";
    public bool ShowItemCount { get; set; } = true;
    public bool ShowLogo { get; set; }
    public string LogoUrl { get; set; } = "";

    public bool AnnouncementsEnabled { get; set; } = true;
    public string AnnouncementText { get; set; } = "";
    public string AnnouncementBg { get; set; } = "#111111";
    public string AnnouncementFg { get; set; } = "#ffffff";
    public bool ShowTimer { get; set; } = true;
    public int TimerMinutes { get; set; } = 15;

    public bool RewardsEnabled { get; set; } = true;
    public string RewardBarColor { get; set; } = "#008060";
    public string RewardBg { get; set; } = "#e8f5f0";
    public string RewardCompletedText { get; set; } = "";
    public int Tier1Threshold { get; set; } = 50;
    public string Tier1TextBefore { get; set; } = "";
    public string Tier1TextAfter { get; set; } = "";
    public int Tier2Threshold { get; set; } = 100;
    public string Tier2TextBefore { get; set; } = "";
    public string Tier2TextAfter { get; set; } = "";
    public int Tier2DiscountPercent { get; set; } = 10;

    public bool UpsellsEnabled { get; set; } = true;
    public string UpsellsTitle { get; set; } = "";
    public string UpsellsButton { get; set; } = "Ekle";
    public bool UpsellsUseAi { get; set; } = true;
    public int UpsellsMaxItems { get; set; } = 6;
    public string ManualProductIds { get; set; } = "";

    public bool RecommendationsEnabled { get; set; } = true;
    public string RecommendationsTitle { get; set; } = "";
    public bool RecommendationsEmptyOnly { get; set; } = true;
    public int RecommendationsMax { get; set; } = 4;

    public bool AddonsEnabled { get; set; }
    public string AddonsTitle { get; set; } = "";
    public string AddonsDescription { get; set; } = "";
    public string AddonsVariantId { get; set; } = "";
    public string AddonsProductTitle { get; set; } = "";

    public bool DiscountCodesEnabled { get; set; } = true;
    public string DiscountPlaceholder { get; set; } = "";
    public string DiscountButton { get; set; } = "Uygula";

    public bool ExpressEnabled { get; set; } = true;
    public bool TrustEnabled { get; set; }
    public string TrustImageUrl { get; set; } = "";
    public string TrustAlt { get; set; } = "";

    public bool NotesEnabled { get; set; }
    public string NotesLabel { get; set; } = "";
    public string NotesPlaceholder { get; set; } = "";

    public bool StickyEnabled { get; set; } = true;
    public string StickyPosition { get; set; } = "bottom-right";
    public string StickyBg { get; set; } = "#111111";
    public string StickyIcon { get; set; } = "#ffffff";
    public bool StickyShowCount { get; set; } = true;

    public bool OpenOnAddToCart { get; set; } = true;
    public string DrawerPosition { get; set; } = "right";
    public bool ContinueShopping { get; set; } = true;
    public string ContinueShoppingLabel { get; set; } = "Alışverişe devam et";

    public bool SubscriptionEnabled { get; set; }
    public string SubscriptionTitle { get; set; } = "";

    public string CustomCss { get; set; } = "";
    public string HtmlBeforeAnnouncements { get; set; } = "";
    public string HtmlBetweenItems { get; set; } = "";
    public string HtmlAboveCheckout { get; set; } = "";
    public string CustomScripts { get; set; } = "";

    public string TrEmptyCart { get; set; } = "";
    public string TrSubtotal { get; set; } = "";
    public string TrCheckout { get; set; } = "";
    public string TrRemove { get; set; } = "";

    public string RawJsonBackup { get; set; } = "{}";
}
