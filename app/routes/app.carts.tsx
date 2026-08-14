import { useEffect, useState } from "react";
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
  createCart,
  listCarts,
  publishCart,
  setTrafficAllocation,
} from "../lib/cart.server";

export const loader = async ({ request }: LoaderFunctionArgs) => {
  const { session } = await authenticate.admin(request);
  const { carts } = await listCarts(session.shop);
  return {
    carts: carts.map((c) => ({
      id: c.id,
      name: c.name,
      status: c.status,
      trafficAllocation: c.trafficAllocation,
      updatedAt: c.updatedAt,
    })),
  };
};

export const action = async ({ request }: ActionFunctionArgs) => {
  const { session } = await authenticate.admin(request);
  const form = await request.formData();
  const intent = String(form.get("intent") || "");

  if (intent === "create") {
    const name = String(form.get("name") || "New cart");
    await createCart(session.shop, name);
    return { ok: true, message: "Cart created" };
  }

  if (intent === "publish") {
    const cartId = String(form.get("cartId") || "");
    await publishCart(session.shop, cartId);
    return { ok: true, message: "Cart published" };
  }

  if (intent === "traffic") {
    const raw = String(form.get("allocations") || "[]");
    const allocations = JSON.parse(raw) as {
      id: string;
      trafficAllocation: number;
    }[];
    await setTrafficAllocation(session.shop, allocations);
    return { ok: true, message: "Traffic allocation saved" };
  }

  return { ok: false, message: "Unknown action" };
};

export default function ManageCartsPage() {
  const { carts } = useLoaderData<typeof loader>();
  const actionData = useActionData<typeof action>();
  const navigation = useNavigation();
  const shopify = useAppBridge();
  const [alloc, setAlloc] = useState(
    Object.fromEntries(carts.map((c) => [c.id, c.trafficAllocation])),
  );

  useEffect(() => {
    if (actionData?.message) shopify.toast.show(actionData.message);
  }, [actionData, shopify]);

  useEffect(() => {
    setAlloc(
      Object.fromEntries(carts.map((c) => [c.id, c.trafficAllocation])),
    );
  }, [carts]);

  return (
    <s-page heading="Manage Carts">
      <s-section heading="Create cart">
        <Form method="post">
          <input type="hidden" name="intent" value="create" />
          <s-stack direction="inline" gap="base">
            <label>
              Name
              <input
                name="name"
                defaultValue="New cart"
                style={{ display: "block", marginTop: 4, padding: 8 }}
              />
            </label>
            <s-button
              type="submit"
              {...(navigation.state !== "idle" ? { loading: true } : {})}
            >
              Create
            </s-button>
          </s-stack>
        </Form>
      </s-section>

      <s-section heading="Carts">
        <s-stack direction="block" gap="base">
          {carts.map((cart) => (
            <s-box
              key={cart.id}
              padding="base"
              borderWidth="base"
              borderRadius="base"
            >
              <s-stack direction="inline" gap="base">
                <div style={{ flex: 1 }}>
                  <s-text type="strong">{cart.name}</s-text>
                  <s-paragraph>
                    <s-badge
                      tone={cart.status === "live" ? "success" : "neutral"}
                    >
                      {cart.status}
                    </s-badge>{" "}
                    Updated {new Date(cart.updatedAt).toLocaleString()}
                  </s-paragraph>
                </div>
                <s-text-field
                  label="Traffic %"
                  value={String(alloc[cart.id] ?? 0)}
                  onChange={(e: Event) =>
                    setAlloc((prev) => ({
                      ...prev,
                      [cart.id]: Number(
                        (e.target as HTMLInputElement).value || 0,
                      ),
                    }))
                  }
                />
                <s-button href={`/app/editor?cartId=${cart.id}`}>Edit</s-button>
                <Form method="post">
                  <input type="hidden" name="intent" value="publish" />
                  <input type="hidden" name="cartId" value={cart.id} />
                  <s-button type="submit" variant="primary">
                    Publish
                  </s-button>
                </Form>
              </s-stack>
            </s-box>
          ))}
        </s-stack>
      </s-section>

      <s-section heading="A/B traffic allocation">
        <s-paragraph>
          Split traffic across carts for A/B testing. Percentages should total
          ~100 for visitors seeing UpCard.
        </s-paragraph>
        <Form method="post">
          <input type="hidden" name="intent" value="traffic" />
          <input
            type="hidden"
            name="allocations"
            value={JSON.stringify(
              Object.entries(alloc).map(([id, trafficAllocation]) => ({
                id,
                trafficAllocation,
              })),
            )}
          />
          <s-button type="submit">Save traffic allocation</s-button>
        </Form>
      </s-section>
    </s-page>
  );
}

export const headers: HeadersFunction = (headersArgs) =>
  boundary.headers(headersArgs);
