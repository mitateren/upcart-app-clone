# UpCard — Cart Drawer Cart Upsell

Shopify app that replaces the theme cart experience with a conversion-focused slide cart (Upcart-style): rewards, upsells, add-ons, announcements, discount codes, sticky cart, multicart / A/B traffic, and analytics.

## Stack

- React Router + Shopify App Bridge / Polaris web components
- Prisma (SQLite locally)
- Theme App Extension (`extensions/upcard-cart`) — **UpCard Bridge** app embed

## Setup

```bash
npm install
npm run setup
shopify app dev
```

1. Install the app on your development store.
2. Online Store → Themes → Customize → **App embeds** → enable **UpCard Bridge**.
3. Open UpCard → Cart Editor, configure modules, Save.
4. Manage Carts → Publish.
5. Add a product to cart on the storefront — the drawer should open.

## App proxy

Storefront loads config from `/apps/upcard/config` (proxied to `/api/proxy/config`).

## Modules

Design, Header, Announcements (+ timer), Rewards (up to 4 tiers), Upsells (Shopify recommendations + smart variant matching), Recommendations (empty cart), Add-ons, Discount codes, Express payments, Trust badges, Additional notes, Subscription upgrades (UI), Sticky cart, Custom CSS/HTML, Translations, Multicart + traffic allocation, Discount rules, Analytics events.
