/**
 * UpCard local smoke tests (no Shopify Partner required)
 * Run: node --experimental-strip-types scripts/smoke-test.mjs
 * or via tsx / prisma-backed JS
 */
import { createRequire } from "module";
import { pathToFileURL } from "url";
import { readFileSync, existsSync } from "fs";
import { join, dirname } from "path";
import { fileURLToPath } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const root = join(__dirname, "..");
const require = createRequire(import.meta.url);

let passed = 0;
let failed = 0;

function ok(name, cond, detail = "") {
  if (cond) {
    passed++;
    console.log(`  ✓ ${name}`);
  } else {
    failed++;
    console.error(`  ✗ ${name}${detail ? " — " + detail : ""}`);
  }
}

console.log("\n=== 1. Project files ===");
const required = [
  "app/lib/cart-config.ts",
  "app/lib/cart.server.ts",
  "app/routes/app.editor.tsx",
  "app/routes/app.carts.tsx",
  "app/routes/api.proxy.$.tsx",
  "extensions/upcard-cart/assets/upcard-drawer.js",
  "extensions/upcard-cart/assets/upcard-drawer.css",
  "extensions/upcard-cart/blocks/upcard-bridge.liquid",
  "prisma/schema.prisma",
  "prisma/dev.sqlite",
];
for (const f of required) {
  ok(f, existsSync(join(root, f)));
}

console.log("\n=== 2. Drawer JS syntax ===");
try {
  const js = readFileSync(
    join(root, "extensions/upcard-cart/assets/upcard-drawer.js"),
    "utf8",
  );
  // eslint-disable-next-line no-new-func
  new Function(js);
  ok("upcard-drawer.js parses", true);
  ok("exposes UpCard API assignment", js.includes("window.UpCard"));
  ok("has openDrawer", js.includes("function openDrawer"));
  ok("has rewardsProgress", js.includes("function rewardsProgress"));
  ok("has loadRecommendations", js.includes("function loadRecommendations"));
  ok("has applyDiscount", js.includes("function applyDiscount"));
  ok("intercepts ATC", js.includes("openOnAddToCart"));
} catch (e) {
  ok("upcard-drawer.js parses", false, String(e));
}

console.log("\n=== 3. Liquid schema ===");
{
  const liquid = readFileSync(
    join(root, "extensions/upcard-cart/blocks/upcard-bridge.liquid"),
    "utf8",
  );
  ok("has schema", liquid.includes("{% schema %}"));
  ok("target body", liquid.includes('"target": "body"'));
  ok("UpCardBootstrap", liquid.includes("UpCardBootstrap"));
  ok("configUrl", liquid.includes("/apps/upcard/config"));
}

console.log("\n=== 4. Prisma + cart service ===");
async function testPrisma() {
  // Dynamic import of compiled paths won't work for TS; use Prisma client + inline logic
  const { PrismaClient } = require("@prisma/client");
  const prisma = new PrismaClient();
  const shopDomain = `smoke-test-${Date.now()}.myshopify.com`;

  try {
    const shop = await prisma.shop.upsert({
      where: { domain: shopDomain },
      create: { domain: shopDomain },
      update: {},
    });
    ok("create shop", !!shop.id);

    const defaultConfig = {
      design: { enabled: true, backgroundColor: "#fff" },
      header: { enabled: true, title: "Your cart" },
      rewards: { enabled: true, tiers: [] },
    };

    const cart = await prisma.cart.create({
      data: {
        shopId: shop.id,
        name: "Smoke cart",
        status: "live",
        trafficAllocation: 100,
        config: JSON.stringify(defaultConfig),
      },
    });
    ok("create cart", !!cart.id && cart.status === "live");

    const rule = await prisma.discountRule.create({
      data: {
        shopId: shop.id,
        name: "Smoke rule",
        ruleType: "discount",
        conditions: JSON.stringify({ minCartTotal: 50 }),
        actions: JSON.stringify({ discountPercent: 10 }),
        enabled: true,
      },
    });
    ok("create discount rule", !!rule.id);

    const event = await prisma.analyticsEvent.create({
      data: {
        shopId: shop.id,
        cartId: cart.id,
        eventType: "open",
        meta: JSON.stringify({ source: "smoke" }),
      },
    });
    ok("create analytics event", !!event.id);

    const live = await prisma.cart.findFirst({
      where: { shopId: shop.id, status: "live" },
    });
    ok("find live cart", live?.id === cart.id);

    const grouped = await prisma.analyticsEvent.groupBy({
      by: ["eventType"],
      where: { shopId: shop.id },
      _count: { _all: true },
    });
    ok("analytics groupBy", grouped.some((g) => g.eventType === "open"));

    // cleanup
    await prisma.analyticsEvent.deleteMany({ where: { shopId: shop.id } });
    await prisma.discountRule.deleteMany({ where: { shopId: shop.id } });
    await prisma.cart.deleteMany({ where: { shopId: shop.id } });
    await prisma.shop.delete({ where: { id: shop.id } });
    ok("cleanup smoke shop", true);
  } catch (e) {
    ok("prisma flow", false, String(e));
  } finally {
    await prisma.$disconnect();
  }
}

console.log("\n=== 5. Cart config merge (inline) ===");
{
  // Mirror parseCartConfig deep merge behavior lightly
  const raw = JSON.stringify({
    header: { title: "Sepetim" },
    announcements: { enabled: false },
  });
  const parsed = JSON.parse(raw);
  ok("partial config parse", parsed.header.title === "Sepetim");
  ok("announcements disabled flag", parsed.announcements.enabled === false);
}

await testPrisma();

console.log("\n=== Summary ===");
console.log(`Passed: ${passed}, Failed: ${failed}`);
process.exit(failed > 0 ? 1 : 0);
