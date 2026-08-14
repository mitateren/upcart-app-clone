import prisma from "../db.server";
import {
  DEFAULT_CART_CONFIG,
  parseCartConfig,
  type CartConfig,
} from "./cart-config";

export async function ensureShop(domain: string) {
  return prisma.shop.upsert({
    where: { domain },
    create: { domain },
    update: {},
  });
}

export async function ensureDefaultCart(shopDomain: string) {
  const shop = await ensureShop(shopDomain);
  const existing = await prisma.cart.findFirst({
    where: { shopId: shop.id },
    orderBy: { createdAt: "asc" },
  });
  if (existing) {
    return { shop, cart: existing };
  }
  const cart = await prisma.cart.create({
    data: {
      shopId: shop.id,
      name: "Default cart",
      status: "live",
      trafficAllocation: 100,
      config: JSON.stringify(DEFAULT_CART_CONFIG),
    },
  });
  return { shop, cart };
}

export async function listCarts(shopDomain: string) {
  const { shop } = await ensureDefaultCart(shopDomain);
  const carts = await prisma.cart.findMany({
    where: { shopId: shop.id },
    orderBy: { updatedAt: "desc" },
  });
  return { shop, carts };
}

export async function getCart(cartId: string) {
  return prisma.cart.findUnique({ where: { id: cartId } });
}

export async function updateCartConfig(
  cartId: string,
  config: CartConfig,
  name?: string,
) {
  return prisma.cart.update({
    where: { id: cartId },
    data: {
      config: JSON.stringify(config),
      ...(name ? { name } : {}),
    },
  });
}

export async function createCart(
  shopDomain: string,
  name: string,
  config?: CartConfig,
) {
  const shop = await ensureShop(shopDomain);
  return prisma.cart.create({
    data: {
      shopId: shop.id,
      name,
      status: "draft",
      trafficAllocation: 0,
      config: JSON.stringify(config ?? DEFAULT_CART_CONFIG),
    },
  });
}

export async function publishCart(shopDomain: string, cartId: string) {
  const shop = await ensureShop(shopDomain);
  await prisma.cart.updateMany({
    where: { shopId: shop.id, status: "live" },
    data: { status: "draft" },
  });
  return prisma.cart.update({
    where: { id: cartId },
    data: { status: "live", trafficAllocation: 100 },
  });
}

export async function getLiveCartConfig(shopDomain: string): Promise<{
  cartId: string;
  config: CartConfig;
  carts: { id: string; trafficAllocation: number; config: CartConfig }[];
}> {
  const { shop } = await ensureDefaultCart(shopDomain);
  const carts = await prisma.cart.findMany({
    where: {
      shopId: shop.id,
      OR: [{ status: "live" }, { trafficAllocation: { gt: 0 } }],
    },
  });

  const live = carts.find((c) => c.status === "live") ?? carts[0];
  const mapped = carts.map((c) => ({
    id: c.id,
    trafficAllocation: c.trafficAllocation,
    config: parseCartConfig(c.config),
  }));

  return {
    cartId: live.id,
    config: parseCartConfig(live.config),
    carts: mapped,
  };
}

export async function setTrafficAllocation(
  shopDomain: string,
  allocations: { id: string; trafficAllocation: number }[],
) {
  const shop = await ensureShop(shopDomain);
  await Promise.all(
    allocations.map((a) =>
      prisma.cart.updateMany({
        where: { id: a.id, shopId: shop.id },
        data: { trafficAllocation: a.trafficAllocation },
      }),
    ),
  );
}

export async function trackEvent(
  shopDomain: string,
  eventType: string,
  cartId?: string,
  meta?: Record<string, unknown>,
) {
  const shop = await ensureShop(shopDomain);
  return prisma.analyticsEvent.create({
    data: {
      shopId: shop.id,
      cartId,
      eventType,
      meta: meta ? JSON.stringify(meta) : null,
    },
  });
}

export async function getAnalyticsSummary(shopDomain: string, days = 30) {
  const shop = await ensureShop(shopDomain);
  const since = new Date();
  since.setDate(since.getDate() - days);

  const events = await prisma.analyticsEvent.groupBy({
    by: ["eventType"],
    where: { shopId: shop.id, createdAt: { gte: since } },
    _count: { _all: true },
  });

  const counts: Record<string, number> = {};
  for (const e of events) {
    counts[e.eventType] = e._count._all;
  }

  const opens = counts.open ?? 0;
  const upsells = counts.add_upsell ?? 0;
  const checkouts = counts.checkout_click ?? 0;

  return {
    opens,
    upsells,
    checkouts,
    rewardReached: counts.reward_tier_reached ?? 0,
    discountApplied: counts.discount_applied ?? 0,
    upsellCtr: opens ? Math.round((upsells / opens) * 1000) / 10 : 0,
    checkoutRate: opens ? Math.round((checkouts / opens) * 1000) / 10 : 0,
    days,
  };
}

export async function listDiscountRules(shopDomain: string) {
  const shop = await ensureShop(shopDomain);
  return prisma.discountRule.findMany({
    where: { shopId: shop.id },
    orderBy: { updatedAt: "desc" },
  });
}

export async function createDiscountRule(
  shopDomain: string,
  data: {
    name: string;
    ruleType: string;
    conditions: unknown;
    actions: unknown;
    enabled?: boolean;
  },
) {
  const shop = await ensureShop(shopDomain);
  return prisma.discountRule.create({
    data: {
      shopId: shop.id,
      name: data.name,
      ruleType: data.ruleType,
      conditions: JSON.stringify(data.conditions),
      actions: JSON.stringify(data.actions),
      enabled: data.enabled ?? true,
    },
  });
}

export async function updateDiscountRule(
  id: string,
  data: Partial<{
    name: string;
    ruleType: string;
    conditions: unknown;
    actions: unknown;
    enabled: boolean;
  }>,
) {
  return prisma.discountRule.update({
    where: { id },
    data: {
      ...(data.name !== undefined ? { name: data.name } : {}),
      ...(data.ruleType !== undefined ? { ruleType: data.ruleType } : {}),
      ...(data.conditions !== undefined
        ? { conditions: JSON.stringify(data.conditions) }
        : {}),
      ...(data.actions !== undefined
        ? { actions: JSON.stringify(data.actions) }
        : {}),
      ...(data.enabled !== undefined ? { enabled: data.enabled } : {}),
    },
  });
}

export async function deleteDiscountRule(id: string) {
  return prisma.discountRule.delete({ where: { id } });
}

export async function getEnabledDiscountRules(shopDomain: string) {
  const shop = await ensureShop(shopDomain);
  const rules = await prisma.discountRule.findMany({
    where: { shopId: shop.id, enabled: true },
  });
  return rules.map((r) => ({
    id: r.id,
    name: r.name,
    ruleType: r.ruleType,
    conditions: JSON.parse(r.conditions),
    actions: JSON.parse(r.actions),
  }));
}
