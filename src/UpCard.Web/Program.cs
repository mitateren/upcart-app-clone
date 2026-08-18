using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UpCard.Web.Data;
using UpCard.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var contentRoot = builder.Environment.ContentRootPath;
var appDataDir = Path.Combine(contentRoot, "App_Data");
var logsDir = Path.Combine(contentRoot, "logs");
var dpKeysDir = Path.Combine(appDataDir, "DataProtection-Keys");
Directory.CreateDirectory(appDataDir);
Directory.CreateDirectory(logsDir);
Directory.CreateDirectory(dpKeysDir);

var sqlitePath = Path.Combine(appDataDir, "upcard.db");
var sqliteCs = $"Data Source={sqlitePath}";

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysDir))
    .SetApplicationName("UpCard");

builder.Services.Configure<ShopifyOptions>(builder.Configuration.GetSection("Shopify"));
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(sqliteCs));
builder.Services.AddScoped<CartService>();
builder.Services.AddHttpClient();
builder.Services.AddRazorPages();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/auth/install";
        o.Cookie.Name = "upcard_session";
        o.Cookie.SameSite = SameSiteMode.None;
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
builder.Services.AddAuthorization();
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                         | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}
catch (Exception ex)
{
    var bootLog = Path.Combine(logsDir, "startup-error.txt");
    File.WriteAllText(bootLog, $"{DateTime.UtcNow:O}\n{ex}");
    throw;
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/auth/install", (HttpRequest req, IOptions<ShopifyOptions> opts) =>
{
    var shop = req.Query["shop"].ToString();
    if (string.IsNullOrWhiteSpace(shop)) return Results.BadRequest("Missing shop");
    shop = CartService.NormalizeShop(shop);
    var redirectUri = $"{opts.Value.AppUrl.TrimEnd('/')}/auth/callback";
    var url =
        $"https://{shop}/admin/oauth/authorize?client_id={Uri.EscapeDataString(opts.Value.ApiKey)}" +
        $"&scope={Uri.EscapeDataString(opts.Value.Scopes)}" +
        $"&redirect_uri={Uri.EscapeDataString(redirectUri)}";
    return Results.Redirect(url);
});

app.MapGet("/auth/callback", async (
    HttpRequest req,
    HttpContext ctx,
    CartService carts,
    IHttpClientFactory httpFactory,
    IOptions<ShopifyOptions> opts) =>
{
    if (!ShopifyCrypto.ValidateQueryHmac(req.Query, opts.Value.ApiSecret))
        return Results.Unauthorized();

    var shop = CartService.NormalizeShop(req.Query["shop"].ToString());
    var code = req.Query["code"].ToString();
    if (string.IsNullOrEmpty(code)) return Results.BadRequest("Missing code");

    var client = httpFactory.CreateClient();
    var payload = new
    {
        client_id = opts.Value.ApiKey,
        client_secret = opts.Value.ApiSecret,
        code
    };
    using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    var tokenRes = await client.PostAsync($"https://{shop}/admin/oauth/access_token", content);
    var tokenBody = await tokenRes.Content.ReadAsStringAsync();
    if (!tokenRes.IsSuccessStatusCode)
        return Results.Problem($"Token exchange failed: {tokenBody}");

    using var doc = JsonDocument.Parse(tokenBody);
    var accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
    var scope = doc.RootElement.TryGetProperty("scope", out var sc) ? sc.GetString() : opts.Value.Scopes;
    await carts.EnsureShopAsync(shop, accessToken, scope);

    var identity = new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.Name, shop),
        new Claim("shop", shop)
    }, CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity),
        new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) });

    var host = req.Query["host"].ToString();
    var redir = string.IsNullOrEmpty(host)
        ? "/"
        : $"/?shop={Uri.EscapeDataString(shop)}&host={Uri.EscapeDataString(host)}";
    return Results.Redirect(redir);
});

app.MapGet("/auth/shopify/callback", (HttpRequest req) =>
    Results.Redirect("/auth/callback" + req.QueryString));

app.MapGet("/api/auth/callback", (HttpRequest req) =>
    Results.Redirect("/auth/callback" + req.QueryString));

app.MapGet("/api/proxy/config", async (HttpRequest req, CartService carts, IOptions<ShopifyOptions> opts, IHostEnvironment env) =>
{
    var shop = req.Query["shop"].ToString();
    if (string.IsNullOrWhiteSpace(shop))
        return Results.Json(new { error = "Missing shop" }, statusCode: 400);

    var signed = ShopifyCrypto.ValidateAppProxy(req.Query, opts.Value.ApiSecret);
    if (!signed && !env.IsDevelopment())
        return Results.Unauthorized();

    return Results.Json(await carts.GetLiveCartPayloadAsync(shop));
});

app.MapGet("/apps/upcard/config", async (HttpRequest req, CartService carts, IOptions<ShopifyOptions> opts, IHostEnvironment env) =>
{
    var shop = req.Query["shop"].ToString();
    if (string.IsNullOrWhiteSpace(shop))
        return Results.Json(new { error = "Missing shop" }, statusCode: 400);
    var signed = ShopifyCrypto.ValidateAppProxy(req.Query, opts.Value.ApiSecret);
    if (!signed && !env.IsDevelopment())
        return Results.Unauthorized();
    return Results.Json(await carts.GetLiveCartPayloadAsync(shop));
});

app.MapPost("/api/proxy/analytics", async (HttpRequest req, CartService carts) =>
{
    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();
    using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
    var root = doc.RootElement;
    var shop = root.TryGetProperty("shop", out var sh) ? sh.GetString() : req.Query["shop"].ToString();
    var eventType = root.TryGetProperty("eventType", out var et) ? et.GetString() : null;
    var cartId = root.TryGetProperty("cartId", out var cid) ? cid.GetString() : null;
    if (string.IsNullOrWhiteSpace(shop) || string.IsNullOrWhiteSpace(eventType))
        return Results.BadRequest();
    await carts.TrackAsync(shop!, eventType!, cartId, root.TryGetProperty("meta", out var meta) ? meta : null);
    return Results.Json(new { ok = true });
});

app.MapPost("/apps/upcard/analytics", async (HttpRequest req, CartService carts) =>
{
    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();
    using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
    var root = doc.RootElement;
    var shop = root.TryGetProperty("shop", out var sh) ? sh.GetString() : req.Query["shop"].ToString();
    var eventType = root.TryGetProperty("eventType", out var et) ? et.GetString() : null;
    var cartId = root.TryGetProperty("cartId", out var cid) ? cid.GetString() : null;
    if (string.IsNullOrWhiteSpace(shop) || string.IsNullOrWhiteSpace(eventType))
        return Results.BadRequest();
    await carts.TrackAsync(shop!, eventType!, cartId);
    return Results.Json(new { ok = true });
});

app.MapPost("/webhooks/app/uninstalled", async (HttpRequest req, AppDbContext db, IOptions<ShopifyOptions> opts) =>
{
    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();
    if (!ShopifyCrypto.ValidateWebhookHmac(body, req.Headers["X-Shopify-Hmac-Sha256"], opts.Value.ApiSecret))
        return Results.Unauthorized();
    var shopDomain = req.Headers["X-Shopify-Shop-Domain"].ToString();
    if (!string.IsNullOrEmpty(shopDomain))
    {
        var shop = await db.Shops.FirstOrDefaultAsync(s => s.Domain == CartService.NormalizeShop(shopDomain));
        if (shop != null)
        {
            db.Shops.Remove(shop);
            await db.SaveChangesAsync();
        }
    }
    return Results.Ok();
});

app.MapPost("/webhooks/app/scopes_update", async (HttpRequest req, AppDbContext db, IOptions<ShopifyOptions> opts) =>
{
    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();
    if (!ShopifyCrypto.ValidateWebhookHmac(body, req.Headers["X-Shopify-Hmac-Sha256"], opts.Value.ApiSecret))
        return Results.Unauthorized();
    var shopDomain = req.Headers["X-Shopify-Shop-Domain"].ToString();
    if (string.IsNullOrEmpty(shopDomain)) return Results.Ok();
    using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
    var scope = doc.RootElement.TryGetProperty("current", out var cur)
        ? string.Join(",", cur.EnumerateArray().Select(e => e.GetString()))
        : null;
    var shop = await db.Shops.FirstOrDefaultAsync(s => s.Domain == CartService.NormalizeShop(shopDomain));
    if (shop != null && !string.IsNullOrEmpty(scope))
    {
        shop.Scope = scope;
        shop.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
    return Results.Ok();
});

app.MapRazorPages();
app.Run();
