import { useEffect } from "react";
import type {
  ActionFunctionArgs,
  HeadersFunction,
  LoaderFunctionArgs,
} from "react-router";
import { Form, useActionData, useLoaderData, useNavigation } from "react-router";
import { useAppBridge } from "@shopify/app-bridge-react";
import { boundary } from "@shopify/shopify-app-react-router/server";
import { authenticate } from "../shopify.server";
import {
  createDiscountRule,
  deleteDiscountRule,
  listDiscountRules,
  updateDiscountRule,
} from "../lib/cart.server";

export const loader = async ({ request }: LoaderFunctionArgs) => {
  const { session } = await authenticate.admin(request);
  const rules = await listDiscountRules(session.shop);
  return {
    rules: rules.map((r) => ({
      id: r.id,
      name: r.name,
      enabled: r.enabled,
      ruleType: r.ruleType,
      conditions: r.conditions,
      actions: r.actions,
    })),
  };
};

export const action = async ({ request }: ActionFunctionArgs) => {
  const { session } = await authenticate.admin(request);
  const form = await request.formData();
  const intent = String(form.get("intent") || "");

  if (intent === "create") {
    await createDiscountRule(session.shop, {
      name: String(form.get("name") || "New rule"),
      ruleType: String(form.get("ruleType") || "discount"),
      conditions: {
        minCartTotal: Number(form.get("minCartTotal") || 0),
      },
      actions: {
        discountPercent: Number(form.get("discountPercent") || 10),
        freeGiftVariantId: String(form.get("freeGiftVariantId") || ""),
      },
      enabled: true,
    });
    return { ok: true, message: "Rule created" };
  }

  if (intent === "toggle") {
    const id = String(form.get("id") || "");
    const enabled = String(form.get("enabled") || "") === "true";
    await updateDiscountRule(id, { enabled: !enabled });
    return { ok: true, message: "Rule updated" };
  }

  if (intent === "delete") {
    await deleteDiscountRule(String(form.get("id") || ""));
    return { ok: true, message: "Rule deleted" };
  }

  return { ok: false, message: "Unknown action" };
};

export default function DiscountsPage() {
  const { rules } = useLoaderData<typeof loader>();
  const actionData = useActionData<typeof action>();
  const navigation = useNavigation();
  const shopify = useAppBridge();

  useEffect(() => {
    if (actionData?.message) shopify.toast.show(actionData.message);
  }, [actionData, shopify]);

  return (
    <s-page heading="Discounts">
      <s-section heading="Create automatic rule">
        <s-paragraph>
          Rules apply based on cart conditions (mirrored to storefront config).
          For checkout-level discounts, also create matching Shopify discounts.
        </s-paragraph>
        <Form method="post">
          <input type="hidden" name="intent" value="create" />
          <s-stack direction="block" gap="base">
            <label>
              Name
              <input name="name" defaultValue="Spend & save" style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }} />
            </label>
            <label>
              Type
              <select name="ruleType" defaultValue="discount" style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}>
                <option value="discount">Discount %</option>
                <option value="free_gift">Free gift</option>
                <option value="free_shipping">Free shipping</option>
              </select>
            </label>
            <label>
              Min cart total
              <input name="minCartTotal" defaultValue="50" style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }} />
            </label>
            <label>
              Discount %
              <input name="discountPercent" defaultValue="10" style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }} />
            </label>
            <label>
              Free gift variant ID (optional)
              <input name="freeGiftVariantId" defaultValue="" style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }} />
            </label>
            <s-button
              type="submit"
              {...(navigation.state !== "idle" ? { loading: true } : {})}
            >
              Create rule
            </s-button>
          </s-stack>
        </Form>
      </s-section>

      <s-section heading="Rules">
        <s-stack direction="block" gap="base">
          {rules.length === 0 && (
            <s-paragraph>No discount rules yet.</s-paragraph>
          )}
          {rules.map((rule) => (
            <s-box
              key={rule.id}
              padding="base"
              borderWidth="base"
              borderRadius="base"
            >
              <s-stack direction="inline" gap="base">
                <div style={{ flex: 1 }}>
                  <s-text type="strong">{rule.name}</s-text>
                  <s-paragraph>
                    {rule.ruleType} ·{" "}
                    <s-badge tone={rule.enabled ? "success" : "neutral"}>
                      {rule.enabled ? "enabled" : "disabled"}
                    </s-badge>
                  </s-paragraph>
                  <pre style={{ fontSize: 11, margin: 0, whiteSpace: "pre-wrap" }}>
                    {JSON.stringify(
                      {
                        conditions: JSON.parse(rule.conditions),
                        actions: JSON.parse(rule.actions),
                      },
                      null,
                      2,
                    )}
                  </pre>
                </div>
                <Form method="post">
                  <input type="hidden" name="intent" value="toggle" />
                  <input type="hidden" name="id" value={rule.id} />
                  <input
                    type="hidden"
                    name="enabled"
                    value={String(rule.enabled)}
                  />
                  <s-button type="submit" variant="secondary">
                    {rule.enabled ? "Disable" : "Enable"}
                  </s-button>
                </Form>
                <Form method="post">
                  <input type="hidden" name="intent" value="delete" />
                  <input type="hidden" name="id" value={rule.id} />
                  <s-button type="submit" tone="critical" variant="secondary">
                    Delete
                  </s-button>
                </Form>
              </s-stack>
            </s-box>
          ))}
        </s-stack>
      </s-section>
    </s-page>
  );
}

export const headers: HeadersFunction = (headersArgs) =>
  boundary.headers(headersArgs);
