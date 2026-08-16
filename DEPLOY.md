# UpCard — Plesk (Windows) kurulum

RDP gerekmez. Plesk panel + HTTPS subdomain yeterli.
Hosting planında **Node.js** desteği açık olmalı (Plesk → Extensions → Node.js).

## 0) Kontrol

Plesk’te domain/subdomain altında **Node.js** sekmesi var mı?

- **Yoksa** → plan Node desteklemiyor; bu app kurulamaz (ASP.NET yetmez).
- **Varsa** → aşağıdaki adımlara devam.

## 1) Subdomain

Örnek: `upcard.tisortgetir.com`

- DNS A kaydı hosting IP’ne
- Plesk’te subdomain oluştur
- SSL (Let’s Encrypt) aç

## 2) Dosyaları yükle

Git veya FTP ile proje kökünü subdomain document root’una koy:

Örnek path: `C:\Inetpub\vhosts\...\upcard.tisortgetir.com\httpdocs\`  
veya Plesk’in gösterdiği Application root.

Gerekli klasörler: `app`, `build` (build sonrası), `prisma`, `public`, `node_modules` (npm sonrası), `package.json`, `server.js`, `shopify.app.toml`, `.env`

## 3) `.env` (Plesk Node.js → Custom environment variables veya dosya)

```env
NODE_ENV=production
SHOPIFY_API_KEY=bf5472c84b033938e4f3cf3e9abd7eaa
SHOPIFY_API_SECRET=PARTNER_CLIENT_SECRET
SHOPIFY_APP_URL=https://upcard.SENIN-DOMAIN.com
SCOPES=read_discounts,read_locales,read_orders,read_products,read_themes,write_discounts,write_files,write_products,write_translations
DATABASE_URL=file:./prisma/prod.sqlite
```

Secret: Partner Dashboard → UpCard → Client credentials.

## 4) Plesk Node.js ayarları

Domain → **Node.js** → Enable:

| Ayar | Değer |
|------|--------|
| Node.js version | 20.x veya 22.x |
| Package manager | npm |
| Application mode | production |
| Application root | proje kökü (package.json’un olduğu yer) |
| Application startup file | `server.js` |
| Application URL | `/` |

Sonra Plesk’te:

1. **NPM Install**
2. SSH veya “Run script” varsa:
   ```bash
   npm run build
   npx prisma generate
   npx prisma migrate deploy
   ```
3. Node.js uygulamasını **Restart / Enable**

SSH yoksa: bilgisayarında `npm run build` yapıp `build/` klasörünü FTP ile yükle; sunucuda sadece `npm ci --omit=dev`, `npx prisma generate`, `npx prisma migrate deploy` çalıştır (Plesk bazen “Run npm script” sunar).

## 5) Shopify’a bağla

Bilgisayarında (CLI):

1. `shopify.app.toml` içinde tüm URL’leri `https://upcard.SENIN-DOMAIN.com` yap
2. ```bash
   shopify app deploy --allow-updates
   ```

## 6) Mağaza

https://admin.shopify.com/store/tisort-getir/apps/upcard  

Example Domain gitmeli.  
Themes → App embeds → **UpCard Bridge** aç.

## Sık hatalar

| Belirti | Çözüm |
|---------|--------|
| Node.js sekmesi yok | Hosting’e Node eklet veya Railway/Fly kullan |
| 502 / app açılmıyor | `build/` yüklü mü, Node Restart, loglara bak |
| Example Domain | `SHOPIFY_APP_URL` + Partner App URL yanlış/eski |
| Prisma hata | `DATABASE_URL` yazılabilir klasöre işaret etmeli |
| Package engine hatası | Plesk Node 20+ seç |

## Not

`web.config` (IIS HttpPlatform) Plesk Node.js modunda genelde gerekmez; Plesk kendi process manager’ını kullanır. Startup file: **`server.js`**.
