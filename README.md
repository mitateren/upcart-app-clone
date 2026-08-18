# UpCard

Shopify cart drawer app (ASP.NET Core 9 + Theme App Extension).

## Yapı

| Yol | Açıklama |
|-----|----------|
| `src/UpCard.Web` | Admin + OAuth + app proxy API |
| `extensions/upcard-cart` | Storefront drawer (Liquid/JS) |
| `shopify.app.toml` | App URL, scopes, proxy |
| `scripts/publish-dotnet.ps1` | Release publish |

## Deploy

Bkz. [DEPLOY.md](./DEPLOY.md).

```powershell
.\scripts\publish-dotnet.ps1
shopify app deploy --allow-updates
```
