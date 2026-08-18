# UpCard — ASP.NET Core 9 (Plesk / IIS)

Backend: `src/UpCard.Web` (**net9.0**).  
Storefront: `extensions/upcard-cart`.

## Domain

`https://upcart.tisortgetir.com`

## 1) Publish

Varsayılan: **self-contained** (sunucuda .NET 9 kurulu olmasa da çalışır):

```powershell
.\scripts\publish-dotnet.ps1
```

Sunucuda ASP.NET Core 9 Hosting Bundle varsa framework-dependent:

```powershell
.\scripts\publish-dotnet.ps1 -FrameworkDependent
```

`appsettings.Production.json`:

```json
{
  "Shopify": {
    "ApiKey": "bf5472c84b033938e4f3cf3e9abd7eaa",
    "ApiSecret": "CLIENT_SECRET",
    "AppUrl": "https://upcart.tisortgetir.com"
  }
}
```

## 2) Plesk’e yükle

- `publish/` içeriğinin **tamamını** site root’a yükle
- Self-contained: `web.config` → `processPath=".\UpCard.Web.exe"`
- FDD: `dotnet .\UpCard.Web.dll` + sunucuda **ASP.NET Core 9 Hosting Bundle**
- `App_Data` ve `logs` yazılabilir
- SSL açık olsun

## 3) Shopify

```powershell
shopify app deploy --allow-updates
```

## 4) Mağaza

1. Admin → UpCard app  
2. Themes → App embeds → **UpCard Bridge**
