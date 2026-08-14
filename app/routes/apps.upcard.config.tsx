import type { LoaderFunctionArgs } from "react-router";
import { authenticate } from "../shopify.server";
import {
  getEnabledDiscountRules,
  getLiveCartConfig,
} from "../lib/cart.server";

/**
 * App Proxy: GET /apps/upcard/config
 * Returns live cart config for the storefront drawer.
 */
export const loader = async ({ request }: LoaderFunctionArgs) => {
  const url = new URL(request.url);
  const shop =
    url.searchParams.get("shop") ||
    url.searchParams.get("shop_domain") ||
    "";

  // App proxy requests include shop; fall back to auth when available
  let shopDomain = shop;
  try {
    const ctx = await authenticate.public.appProxy(request);
    shopDomain = ctx.session?.shop || shopDomain;
  } catch {
    // allow public read via shop query when proxy auth unavailable in local tests
  }

  if (!shopDomain) {
    return Response.json({ error: "Missing shop" }, { status: 400 });
  }

  const live = await getLiveCartConfig(shopDomain);
  const discountRules = await getEnabledDiscountRules(shopDomain);

  return Response.json(
    {
      cartId: live.cartId,
      config: live.config,
      carts: live.carts,
      discountRules,
    },
    {
      headers: {
        "Cache-Control": "public, max-age=30",
        "Access-Control-Allow-Origin": "*",
      },
    },
  );
};
