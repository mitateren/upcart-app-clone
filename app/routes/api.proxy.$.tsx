import type { ActionFunctionArgs, LoaderFunctionArgs } from "react-router";
import { authenticate } from "../shopify.server";
import {
  getEnabledDiscountRules,
  getLiveCartConfig,
  trackEvent,
} from "../lib/cart.server";

/**
 * App Proxy: /apps/upcard/* → /api/proxy/*
 */
export const loader = async ({ request, params }: LoaderFunctionArgs) => {
  const path = params["*"] || "";
  const url = new URL(request.url);

  let shopDomain =
    url.searchParams.get("shop") || url.searchParams.get("shop_domain") || "";

  try {
    const ctx = await authenticate.public.appProxy(request);
    shopDomain = ctx.session?.shop || shopDomain;
  } catch {
    // ignore
  }

  if (!shopDomain) {
    return Response.json({ error: "Missing shop" }, { status: 400 });
  }

  if (path === "config" || path === "" || path.startsWith("config")) {
    const live = await getLiveCartConfig(shopDomain);
    const discountRules = await getEnabledDiscountRules(shopDomain);
    return Response.json({
      cartId: live.cartId,
      config: live.config,
      carts: live.carts,
      discountRules,
    });
  }

  return Response.json({ error: "Not found", path }, { status: 404 });
};

export const action = async ({ request, params }: ActionFunctionArgs) => {
  const path = params["*"] || "";

  let shopDomain = "";
  try {
    const ctx = await authenticate.public.appProxy(request);
    shopDomain = ctx.session?.shop || "";
  } catch {
    // ignore
  }

  const body = await request.json().catch(() => ({}));
  shopDomain = shopDomain || body.shop || "";

  if (path.includes("analytics")) {
    if (!shopDomain || !body.eventType) {
      return Response.json({ error: "Invalid payload" }, { status: 400 });
    }
    await trackEvent(shopDomain, body.eventType, body.cartId, body.meta);
    return Response.json({ ok: true });
  }

  return Response.json({ error: "Not found" }, { status: 404 });
};
