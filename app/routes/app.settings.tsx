import { useEffect, useState } from "react";
import type {
  ActionFunctionArgs,
  HeadersFunction,
  LoaderFunctionArgs,
} from "react-router";
import { useActionData, useLoaderData, useNavigation, useSubmit } from "react-router";
import { useAppBridge } from "@shopify/app-bridge-react";
import { boundary } from "@shopify/shopify-app-react-router/server";
import { authenticate } from "../shopify.server";
import { parseCartConfig, type CartConfig } from "../lib/cart-config";
import { ensureDefaultCart, updateCartConfig } from "../lib/cart.server";

export const loader = async ({ request }: LoaderFunctionArgs) => {
  const { session } = await authenticate.admin(request);
  const { cart } = await ensureDefaultCart(session.shop);
  return {
    cartId: cart.id,
    config: parseCartConfig(cart.config),
  };
};

export const action = async ({ request }: ActionFunctionArgs) => {
  await authenticate.admin(request);
  const form = await request.formData();
  const cartId = String(form.get("cartId") || "");
  const config = parseCartConfig(String(form.get("config") || ""));
  await updateCartConfig(cartId, config);
  return { ok: true };
};

export default function SettingsPage() {
  const data = useLoaderData<typeof loader>();
  const actionData = useActionData<typeof action>();
  const navigation = useNavigation();
  const submit = useSubmit();
  const shopify = useAppBridge();
  const [config, setConfig] = useState<CartConfig>(data.config);

  useEffect(() => {
    if (actionData?.ok) shopify.toast.show("Settings saved");
  }, [actionData, shopify]);

  function save() {
    const fd = new FormData();
    fd.set("cartId", data.cartId);
    fd.set("config", JSON.stringify(config));
    submit(fd, { method: "post" });
  }

  return (
    <s-page heading="Settings">
      <s-button
        slot="primary-action"
        onClick={save}
        {...(navigation.state !== "idle" ? { loading: true } : {})}
      >
        Save
      </s-button>

      <s-section heading="Custom CSS">
        <textarea
          style={{
            width: "100%",
            minHeight: 160,
            fontFamily: "monospace",
            fontSize: 12,
          }}
          value={config.customCss}
          onChange={(e) =>
            setConfig((prev) => ({ ...prev, customCss: e.target.value }))
          }
          placeholder={".upcard-btn { border-radius: 999px !important; }"}
        />
      </s-section>

      <s-section heading="Custom HTML slots">
        <s-stack direction="block" gap="base">
          {(
            [
              ["beforeAnnouncements", "Before announcements"],
              ["betweenItems", "Between / after items"],
              ["aboveCheckout", "Above checkout"],
              ["scripts", "Scripts"],
            ] as const
          ).map(([key, label]) => (
            <div key={key}>
              <s-paragraph>{label}</s-paragraph>
              <textarea
                style={{
                  width: "100%",
                  minHeight: 100,
                  fontFamily: "monospace",
                  fontSize: 12,
                }}
                value={config.customHtml[key]}
                onChange={(e) =>
                  setConfig((prev) => ({
                    ...prev,
                    customHtml: {
                      ...prev.customHtml,
                      [key]: e.target.value,
                    },
                  }))
                }
              />
            </div>
          ))}
        </s-stack>
      </s-section>

      <s-section heading="Translations">
        <s-paragraph>
          Edit label translations as JSON (emptyCart, subtotal, savings,
          checkout, remove, quantity).
        </s-paragraph>
        <textarea
          style={{
            width: "100%",
            minHeight: 140,
            fontFamily: "monospace",
            fontSize: 12,
          }}
          value={JSON.stringify(config.translations, null, 2)}
          onChange={(e) => {
            try {
              const translations = JSON.parse(e.target.value);
              setConfig((prev) => ({ ...prev, translations }));
            } catch {
              /* ignore */
            }
          }}
        />
      </s-section>
    </s-page>
  );
}

export const headers: HeadersFunction = (headersArgs) =>
  boundary.headers(headersArgs);
