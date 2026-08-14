import type { ActionFunctionArgs, LoaderFunctionArgs } from "react-router";
import { authenticate } from "../shopify.server";
import { trackEvent } from "../lib/cart.server";

export const loader = async ({ request }: LoaderFunctionArgs) => {
  return Response.json({ ok: true });
};

/**
 * App Proxy: POST /apps/upcard/analytics
 */
export const action = async ({ request }: ActionFunctionArgs) => {
  let shopDomain = "";
  try {
    const ctx = await authenticate.public.appProxy(request);
    shopDomain = ctx.session?.shop || "";
  } catch {
    // continue with body shop
  }

  const body = await request.json().catch(() => ({}));
  shopDomain = shopDomain || body.shop || "";
  if (!shopDomain || !body.eventType) {
    return Response.json({ error: "Invalid payload" }, { status: 400 });
  }

  await trackEvent(shopDomain, body.eventType, body.cartId, body.meta);
  return Response.json({ ok: true });
};
