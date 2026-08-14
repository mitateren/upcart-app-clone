import type { HeadersFunction, LoaderFunctionArgs } from "react-router";
import { useLoaderData } from "react-router";
import { boundary } from "@shopify/shopify-app-react-router/server";
import { authenticate } from "../shopify.server";
import { getAnalyticsSummary } from "../lib/cart.server";

export const loader = async ({ request }: LoaderFunctionArgs) => {
  const { session } = await authenticate.admin(request);
  const summary = await getAnalyticsSummary(session.shop, 30);
  return { summary };
};

export default function AnalyticsPage() {
  const { summary } = useLoaderData<typeof loader>();

  return (
    <s-page heading="Analytics">
      <s-section heading={`Last ${summary.days} days`}>
        <s-stack direction="inline" gap="large">
          <Metric label="Cart opens" value={summary.opens} />
          <Metric label="Upsells added" value={summary.upsells} />
          <Metric label="Upsell CTR" value={`${summary.upsellCtr}%`} />
          <Metric label="Checkout clicks" value={summary.checkouts} />
          <Metric label="Checkout rate" value={`${summary.checkoutRate}%`} />
          <Metric label="Reward tiers reached" value={summary.rewardReached} />
          <Metric label="Discounts applied" value={summary.discountApplied} />
        </s-stack>
      </s-section>
      <s-section heading="Notes">
        <s-paragraph>
          Events are captured from the storefront drawer (open, add upsell,
          checkout click, reward tier, discount apply). Use these metrics to
          optimize modules in Cart Editor.
        </s-paragraph>
      </s-section>
    </s-page>
  );
}

function Metric({ label, value }: { label: string; value: string | number }) {
  return (
    <s-box padding="base" borderWidth="base" borderRadius="base">
      <s-text type="strong">{value}</s-text>
      <s-paragraph>{label}</s-paragraph>
    </s-box>
  );
}

export const headers: HeadersFunction = (headersArgs) =>
  boundary.headers(headersArgs);
