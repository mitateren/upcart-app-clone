using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UpCard.Web.Pages;

/// <summary>Keeps shop/host across embedded Shopify navigations (cookie often blocked in iframe).</summary>
public abstract class UpCardPageModel : PageModel
{
    public string ShopDomain { get; private set; } = "";
    public string HostParam { get; private set; } = "";

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        ShopDomain = User.FindFirstValue("shop")
                     ?? Request.Query["shop"].ToString()
                     ?? Request.Cookies["upcard_shop"]
                     ?? "";
        HostParam = Request.Query["host"].ToString()
                    ?? Request.Cookies["upcard_host"]
                    ?? "";

        if (!string.IsNullOrWhiteSpace(ShopDomain))
        {
            ShopDomain = Services.CartService.NormalizeShop(ShopDomain);
            var opt = new CookieOptions
            {
                IsEssential = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                MaxAge = TimeSpan.FromDays(30),
                Path = "/"
            };
            opt.Extensions.Add("Partitioned");
            Response.Cookies.Append("upcard_shop", ShopDomain, opt);
        }
        if (!string.IsNullOrWhiteSpace(HostParam))
        {
            var opt = new CookieOptions
            {
                IsEssential = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                MaxAge = TimeSpan.FromDays(30),
                Path = "/"
            };
            opt.Extensions.Add("Partitioned");
            Response.Cookies.Append("upcard_host", HostParam, opt);
        }

        ViewData["Shop"] = ShopDomain;
        ViewData["Host"] = HostParam;
        base.OnPageHandlerExecuting(context);
    }

    protected IActionResult? RequireShop()
    {
        if (!string.IsNullOrWhiteSpace(ShopDomain)) return null;
        return RedirectToPage("/Index");
    }

    protected RedirectToPageResult RedirectToAppPage(string pageName, object? routeValues = null)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (routeValues != null)
        {
            foreach (var p in routeValues.GetType().GetProperties())
                dict[p.Name] = p.GetValue(routeValues);
        }
        if (!string.IsNullOrWhiteSpace(ShopDomain)) dict["shop"] = ShopDomain;
        if (!string.IsNullOrWhiteSpace(HostParam)) dict["host"] = HostParam;
        return RedirectToPage(pageName, dict);
    }

    protected string AppPageUrl(string pageName, object? routeValues = null)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (routeValues != null)
        {
            foreach (var p in routeValues.GetType().GetProperties())
                dict[p.Name] = p.GetValue(routeValues);
        }
        if (!string.IsNullOrWhiteSpace(ShopDomain)) dict["shop"] = ShopDomain;
        if (!string.IsNullOrWhiteSpace(HostParam)) dict["host"] = HostParam;
        return Url.Page(pageName, dict) ?? pageName;
    }
}
