import type { HeadersFunction, LoaderFunctionArgs } from "react-router";
import { Link, useLoaderData } from "react-router";
import { boundary } from "@shopify/shopify-app-react-router/server";
import { authenticate } from "../shopify.server";
import { ensureDefaultCart, getAnalyticsSummary } from "../lib/cart.server";

export const loader = async ({ request }: LoaderFunctionArgs) => {
  const { session } = await authenticate.admin(request);
  const { cart } = await ensureDefaultCart(session.shop);
  const summary = await getAnalyticsSummary(session.shop, 30);
  return {
    shop: session.shop,
    cart: { id: cart.id, name: cart.name, status: cart.status },
    summary,
  };
};

export default function HomePage() {
  const { cart, summary, shop } = useLoaderData<typeof loader>();

  return (
    <s-page heading="UpCard">
      <s-button slot="primary-action" href="/app/editor">
        Open Cart Editor
      </s-button>

      <s-section heading="Cart status">
        <s-paragraph>
          Live cart: <s-text type="strong">{cart.name}</s-text> ({cart.status})
          on <s-text type="strong">{shop}</s-text>
        </s-paragraph>
        <s-stack direction="inline" gap="base">
          <s-button href="/app/editor" variant="primary">
            Edit cart
          </s-button>
          <s-button href="/app/carts" variant="secondary">
            Manage carts
          </s-button>
        </s-stack>
      </s-section>

      <s-section heading="Setup checklist">
        <s-unordered-list>
          <s-list-item>
            Open Online Store → Themes → Customize → App embeds → enable{" "}
            <s-text type="strong">UpCard Bridge</s-text>
          </s-list-item>
          <s-list-item>
            Configure modules in{" "}
            <Link to="/app/editor">Cart Editor</Link>
          </s-list-item>
          <s-list-item>
            Publish a cart from{" "}
            <Link to="/app/carts">Manage Carts</Link>
          </s-list-item>
          <s-list-item>
            Test add-to-cart on the storefront — UpCard drawer should open
          </s-list-item>
        </s-unordered-list>
      </s-section>

      <s-section heading="Last 30 days">
        <s-stack direction="inline" gap="large">
          <s-box padding="base" borderWidth="base" borderRadius="base">
            <s-text type="strong">{summary.opens}</s-text>
            <s-paragraph>Cart opens</s-paragraph>
          </s-box>
          <s-box padding="base" borderWidth="base" borderRadius="base">
            <s-text type="strong">{summary.upsellCtr}%</s-text>
            <s-paragraph>Upsell CTR</s-paragraph>
          </s-box>
          <s-box padding="base" borderWidth="base" borderRadius="base">
            <s-text type="strong">{summary.checkoutRate}%</s-text>
            <s-paragraph>Checkout rate</s-paragraph>
          </s-box>
        </s-stack>
      </s-section>
    </s-page>
  );
}

export const headers: HeadersFunction = (headersArgs) =>
  boundary.headers(headersArgs);
