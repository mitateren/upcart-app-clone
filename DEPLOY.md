# UpCard production (canlı) kurulum

`example.com` / Example Domain = App URL henüz canlı host’a bağlanmamış demektir.

## 1) Render’da servis

1. https://dashboard.render.com → **New → Blueprint** (veya Web Service)
2. Repo: `https://github.com/tisortgetir/upcart-app-clone`
3. Branch: `main-cli`
4. `render.yaml` otomatik `upcard-app` servisini tanımlar
5. Environment variables (zorunlu):
   - `SHOPIFY_API_KEY` = `bf5472c84b033938e4f3cf3e9abd7eaa`
   - `SHOPIFY_API_SECRET` = Partner Dashboard → UpCard → **Client secret**
   - `SHOPIFY_APP_URL` = `https://upcard-app.onrender.com` (Render URL farklıysa onu yaz)
6. Deploy bitene kadar bekle (ilk build ~5–10 dk)

Client secret:
https://dev.shopify.com/dashboard/225511547/apps/bf5472c84b033938e4f3cf3e9abd7eaa/settings

## 2) Shopify App URL’lerini kaydet

Proje kökünde (Client secret gerekmez, sadece CLI login):

```bash
cd C:\wwwroot\shopifyApp_UpCard
shopify app deploy
```

Bu komut `shopify.app.toml` içindeki URL’leri Partner’a yazar + theme extension’ı yayınlar.

Render URL’n `upcard-app.onrender.com` değilse önce `shopify.app.toml` içindeki tüm URL’leri kendi Render domain’inle değiştir.

## 3) Mağazaya kur / yeniden aç

https://admin.shopify.com/store/tisort-getir/apps/upcard

Hâlâ Example Domain görürsen:
- Render deploy yeşil mi?
- Partner’da Application URL `https://upcard-app.onrender.com` mi?
- App’i kaldırıp tekrar kur (URL değişince gerekir)

## 4) Storefront drawer

**Online Store → Themes → Customize → App embeds → UpCard Bridge** aç → Save

## Not

`shopify app dev` sadece development (tunnel). Canlı mağaza (`tisort-getir`) için Render + `shopify app deploy` şart.
