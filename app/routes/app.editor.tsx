import { useEffect, useMemo, useState } from "react";
import type {
  ActionFunctionArgs,
  HeadersFunction,
  LoaderFunctionArgs,
} from "react-router";
import { Form, useActionData, useLoaderData, useNavigation, useSubmit } from "react-router";
import { useAppBridge } from "@shopify/app-bridge-react";
import { boundary } from "@shopify/shopify-app-react-router/server";
import { authenticate } from "../shopify.server";
import {
  DEFAULT_CART_CONFIG,
  parseCartConfig,
  type CartConfig,
} from "../lib/cart-config";
import { ensureDefaultCart, getCart, updateCartConfig } from "../lib/cart.server";

const MODULES: {
  key: keyof CartConfig;
  label: string;
  hasEnabled?: boolean;
}[] = [
  { key: "design", label: "Design", hasEnabled: true },
  { key: "header", label: "Header", hasEnabled: true },
  { key: "announcements", label: "Announcements", hasEnabled: true },
  { key: "rewards", label: "Rewards", hasEnabled: true },
  { key: "upsells", label: "Upsells", hasEnabled: true },
  { key: "recommendations", label: "Recommendations", hasEnabled: true },
  { key: "addons", label: "Add-ons", hasEnabled: true },
  { key: "discountCodes", label: "Discount codes", hasEnabled: true },
  { key: "expressPayments", label: "Express payments", hasEnabled: true },
  { key: "trustBadges", label: "Trust badges", hasEnabled: true },
  { key: "additionalNotes", label: "Additional notes", hasEnabled: true },
  { key: "subscriptionUpgrades", label: "Subscription upgrades", hasEnabled: true },
  { key: "stickyCart", label: "Sticky cart", hasEnabled: true },
  { key: "behavior", label: "Behavior" },
];

export const loader = async ({ request }: LoaderFunctionArgs) => {
  const { session } = await authenticate.admin(request);
  const url = new URL(request.url);
  const cartId = url.searchParams.get("cartId");
  const { cart: defaultCart } = await ensureDefaultCart(session.shop);
  const cart = cartId ? await getCart(cartId) : defaultCart;
  if (!cart) throw new Response("Cart not found", { status: 404 });
  return {
    cartId: cart.id,
    name: cart.name,
    status: cart.status,
    config: parseCartConfig(cart.config),
  };
};

export const action = async ({ request }: ActionFunctionArgs) => {
  await authenticate.admin(request);
  const form = await request.formData();
  const cartId = String(form.get("cartId") || "");
  const configRaw = String(form.get("config") || "");
  const name = String(form.get("name") || "");
  const config = parseCartConfig(configRaw);
  await updateCartConfig(cartId, config, name || undefined);
  return { ok: true };
};

export default function CartEditorPage() {
  const data = useLoaderData<typeof loader>();
  const actionData = useActionData<typeof action>();
  const navigation = useNavigation();
  const submit = useSubmit();
  const shopify = useAppBridge();
  const [config, setConfig] = useState<CartConfig>(data.config);
  const [active, setActive] = useState<keyof CartConfig>("design");
  const [name, setName] = useState(data.name);

  useEffect(() => {
    setConfig(data.config);
    setName(data.name);
  }, [data.cartId, data.config, data.name]);

  useEffect(() => {
    if (actionData?.ok) shopify.toast.show("Cart saved");
  }, [actionData, shopify]);

  const saving = navigation.state !== "idle";

  const moduleEnabled = useMemo(() => {
    const mod = config[active];
    if (mod && typeof mod === "object" && "enabled" in mod) {
      return Boolean((mod as { enabled: boolean }).enabled);
    }
    return true;
  }, [active, config]);

  function patch<K extends keyof CartConfig>(key: K, value: Partial<CartConfig[K]>) {
    setConfig((prev) => ({
      ...prev,
      [key]:
        typeof prev[key] === "object" && !Array.isArray(prev[key])
          ? { ...(prev[key] as object), ...(value as object) }
          : (value as CartConfig[K]),
    }));
  }

  function toggleModule() {
    const mod = config[active];
    if (mod && typeof mod === "object" && "enabled" in mod) {
      patch(active, { enabled: !(mod as { enabled: boolean }).enabled } as never);
    }
  }

  function save() {
    const fd = new FormData();
    fd.set("cartId", data.cartId);
    fd.set("name", name);
    fd.set("config", JSON.stringify(config));
    submit(fd, { method: "post" });
  }

  function resetDefaults() {
    setConfig(structuredClone(DEFAULT_CART_CONFIG));
  }

  return (
    <s-page heading="Cart Editor">
      <s-button slot="primary-action" onClick={save} {...(saving ? { loading: true } : {})}>
        Save
      </s-button>
      <s-button slot="secondary-actions" onClick={resetDefaults} variant="secondary">
        Reset defaults
      </s-button>

      <s-section>
        <s-stack direction="inline" gap="base">
          <s-text-field
            label="Cart name"
            value={name}
            onChange={(e: Event) =>
              setName((e.target as HTMLInputElement).value)
            }
          />
          <s-badge tone={data.status === "live" ? "success" : "neutral"}>
            {data.status}
          </s-badge>
        </s-stack>
      </s-section>

      <s-section>
        <div style={{ display: "grid", gridTemplateColumns: "220px 1fr", gap: 16 }}>
          <div>
            <s-box padding="base" borderWidth="base" borderRadius="base">
              <s-stack direction="block" gap="small">
                {MODULES.map((m) => {
                  const mod = config[m.key];
                  const enabled =
                    mod && typeof mod === "object" && "enabled" in mod
                      ? (mod as { enabled: boolean }).enabled
                      : true;
                  return (
                    <s-button
                      key={m.key}
                      variant={active === m.key ? "primary" : "tertiary"}
                      onClick={() => setActive(m.key)}
                    >
                      {enabled ? m.label : `${m.label} (off)`}
                    </s-button>
                  );
                })}
              </s-stack>
            </s-box>
          </div>

          <div>
            <s-box padding="base" borderWidth="base" borderRadius="base">
              <s-stack direction="block" gap="base">
                <s-stack direction="inline" gap="base">
                  <s-heading>{MODULES.find((m) => m.key === active)?.label}</s-heading>
                  {"enabled" in ((config[active] as object) || {}) && (
                    <s-button onClick={toggleModule} variant="secondary">
                      {moduleEnabled ? "Disable" : "Enable"}
                    </s-button>
                  )}
                </s-stack>

                {active === "design" && (
                  <>
                    <ColorField
                      label="Background"
                      value={config.design.backgroundColor}
                      onChange={(v) => patch("design", { backgroundColor: v })}
                    />
                    <ColorField
                      label="Text"
                      value={config.design.textColor}
                      onChange={(v) => patch("design", { textColor: v })}
                    />
                    <ColorField
                      label="Button background"
                      value={config.design.buttonBackground}
                      onChange={(v) => patch("design", { buttonBackground: v })}
                    />
                    <ColorField
                      label="Button text"
                      value={config.design.buttonTextColor}
                      onChange={(v) => patch("design", { buttonTextColor: v })}
                    />
                    <ColorField
                      label="Accent"
                      value={config.design.accentColor}
                      onChange={(v) => patch("design", { accentColor: v })}
                    />
                    <Toggle
                      label="Show strikethrough prices"
                      checked={config.design.showStrikethroughPrices}
                      onChange={(v) =>
                        patch("design", { showStrikethroughPrices: v })
                      }
                    />
                    <Toggle
                      label="Show savings below prices"
                      checked={config.design.showSavingsBelowPrices}
                      onChange={(v) =>
                        patch("design", { showSavingsBelowPrices: v })
                      }
                    />
                  </>
                )}

                {active === "header" && (
                  <>
                    <TextField
                      label="Title"
                      value={config.header.title}
                      onChange={(v) => patch("header", { title: v })}
                    />
                    <Toggle
                      label="Show item count"
                      checked={config.header.showItemCount}
                      onChange={(v) => patch("header", { showItemCount: v })}
                    />
                    <Toggle
                      label="Show logo"
                      checked={config.header.showLogo}
                      onChange={(v) => patch("header", { showLogo: v })}
                    />
                    <TextField
                      label="Logo URL"
                      value={config.header.logoUrl}
                      onChange={(v) => patch("header", { logoUrl: v })}
                    />
                  </>
                )}

                {active === "announcements" && (
                  <>
                    <TextField
                      label="Announcement text (use {TIMER})"
                      value={config.announcements.text}
                      onChange={(v) => patch("announcements", { text: v })}
                    />
                    <ColorField
                      label="Background"
                      value={config.announcements.backgroundColor}
                      onChange={(v) =>
                        patch("announcements", { backgroundColor: v })
                      }
                    />
                    <ColorField
                      label="Text color"
                      value={config.announcements.textColor}
                      onChange={(v) => patch("announcements", { textColor: v })}
                    />
                    <Toggle
                      label="Show countdown timer"
                      checked={config.announcements.showTimer}
                      onChange={(v) => patch("announcements", { showTimer: v })}
                    />
                    <NumberField
                      label="Timer minutes"
                      value={config.announcements.timerMinutes}
                      onChange={(v) =>
                        patch("announcements", { timerMinutes: v })
                      }
                    />
                  </>
                )}

                {active === "rewards" && (
                  <>
                    <s-select
                      label="Basis"
                      value={config.rewards.basis}
                      onChange={(e: Event) =>
                        patch("rewards", {
                          basis: (e.target as HTMLSelectElement).value as
                            | "cart_total"
                            | "item_count",
                        })
                      }
                    >
                      <option value="cart_total">Cart total</option>
                      <option value="item_count">Item count</option>
                    </s-select>
                    <ColorField
                      label="Bar color"
                      value={config.rewards.barColor}
                      onChange={(v) => patch("rewards", { barColor: v })}
                    />
                    <TextField
                      label="Completed text"
                      value={config.rewards.completedText}
                      onChange={(v) => patch("rewards", { completedText: v })}
                    />
                    <s-paragraph>
                      Tiers ({config.rewards.tiers.length}/4) — edit JSON below
                      for advanced thresholds.
                    </s-paragraph>
                    <textarea
                      style={{
                        width: "100%",
                        minHeight: 160,
                        fontFamily: "monospace",
                        fontSize: 12,
                      }}
                      value={JSON.stringify(config.rewards.tiers, null, 2)}
                      onChange={(e) => {
                        try {
                          const tiers = JSON.parse(e.target.value);
                          if (Array.isArray(tiers)) {
                            patch("rewards", { tiers: tiers.slice(0, 4) });
                          }
                        } catch {
                          /* ignore while typing */
                        }
                      }}
                    />
                  </>
                )}

                {active === "upsells" && (
                  <>
                    <TextField
                      label="Title"
                      value={config.upsells.title}
                      onChange={(v) => patch("upsells", { title: v })}
                    />
                    <TextField
                      label="Add button label"
                      value={config.upsells.addButtonLabel}
                      onChange={(v) => patch("upsells", { addButtonLabel: v })}
                    />
                    <Toggle
                      label="Use AI / Shopify recommendations"
                      checked={config.upsells.useAi}
                      onChange={(v) => patch("upsells", { useAi: v })}
                    />
                    <Toggle
                      label="Smart variant matching"
                      checked={config.upsells.smartVariantMatching}
                      onChange={(v) =>
                        patch("upsells", { smartVariantMatching: v })
                      }
                    />
                    <NumberField
                      label="Max items"
                      value={config.upsells.maxItems}
                      onChange={(v) => patch("upsells", { maxItems: v })}
                    />
                  </>
                )}

                {active === "recommendations" && (
                  <>
                    <TextField
                      label="Title"
                      value={config.recommendations.title}
                      onChange={(v) => patch("recommendations", { title: v })}
                    />
                    <Toggle
                      label="Empty cart only"
                      checked={config.recommendations.emptyCartOnly}
                      onChange={(v) =>
                        patch("recommendations", { emptyCartOnly: v })
                      }
                    />
                    <NumberField
                      label="Max items"
                      value={config.recommendations.maxItems}
                      onChange={(v) =>
                        patch("recommendations", { maxItems: v })
                      }
                    />
                  </>
                )}

                {active === "addons" && (
                  <>
                    <TextField
                      label="Title"
                      value={config.addons.title}
                      onChange={(v) => patch("addons", { title: v })}
                    />
                    <TextField
                      label="Description"
                      value={config.addons.description}
                      onChange={(v) => patch("addons", { description: v })}
                    />
                    <TextField
                      label="Product variant ID"
                      value={config.addons.productVariantId}
                      onChange={(v) =>
                        patch("addons", { productVariantId: v })
                      }
                    />
                    <s-select
                      label="Mode"
                      value={config.addons.mode}
                      onChange={(e: Event) =>
                        patch("addons", {
                          mode: (e.target as HTMLSelectElement).value as
                            | "shipping_protection"
                            | "product",
                        })
                      }
                    >
                      <option value="product">Product add-on</option>
                      <option value="shipping_protection">
                        Shipping protection
                      </option>
                    </s-select>
                  </>
                )}

                {active === "discountCodes" && (
                  <>
                    <TextField
                      label="Placeholder"
                      value={config.discountCodes.placeholder}
                      onChange={(v) =>
                        patch("discountCodes", { placeholder: v })
                      }
                    />
                    <TextField
                      label="Button label"
                      value={config.discountCodes.buttonLabel}
                      onChange={(v) =>
                        patch("discountCodes", { buttonLabel: v })
                      }
                    />
                  </>
                )}

                {active === "expressPayments" && (
                  <s-select
                    label="Alignment"
                    value={config.expressPayments.alignment}
                    onChange={(e: Event) =>
                      patch("expressPayments", {
                        alignment: (e.target as HTMLSelectElement).value as
                          | "left"
                          | "center"
                          | "right"
                          | "stretch",
                      })
                    }
                  >
                    <option value="stretch">Stretch</option>
                    <option value="left">Left</option>
                    <option value="center">Center</option>
                    <option value="right">Right</option>
                  </s-select>
                )}

                {active === "trustBadges" && (
                  <>
                    <TextField
                      label="Image URL"
                      value={config.trustBadges.imageUrl}
                      onChange={(v) => patch("trustBadges", { imageUrl: v })}
                    />
                    <TextField
                      label="Alt text"
                      value={config.trustBadges.alt}
                      onChange={(v) => patch("trustBadges", { alt: v })}
                    />
                    <s-select
                      label="Position"
                      value={config.trustBadges.position}
                      onChange={(e: Event) =>
                        patch("trustBadges", {
                          position: (e.target as HTMLSelectElement).value as
                            | "top"
                            | "bottom",
                        })
                      }
                    >
                      <option value="top">Top</option>
                      <option value="bottom">Bottom</option>
                    </s-select>
                  </>
                )}

                {active === "additionalNotes" && (
                  <>
                    <TextField
                      label="Label"
                      value={config.additionalNotes.label}
                      onChange={(v) => patch("additionalNotes", { label: v })}
                    />
                    <TextField
                      label="Placeholder"
                      value={config.additionalNotes.placeholder}
                      onChange={(v) =>
                        patch("additionalNotes", { placeholder: v })
                      }
                    />
                    <Toggle
                      label="Required"
                      checked={config.additionalNotes.required}
                      onChange={(v) =>
                        patch("additionalNotes", { required: v })
                      }
                    />
                  </>
                )}

                {active === "subscriptionUpgrades" && (
                  <>
                    <TextField
                      label="Title"
                      value={config.subscriptionUpgrades.title}
                      onChange={(v) =>
                        patch("subscriptionUpgrades", { title: v })
                      }
                    />
                    <TextField
                      label="One-time label"
                      value={config.subscriptionUpgrades.oneTimeLabel}
                      onChange={(v) =>
                        patch("subscriptionUpgrades", { oneTimeLabel: v })
                      }
                    />
                    <TextField
                      label="Subscribe label"
                      value={config.subscriptionUpgrades.subscribeLabel}
                      onChange={(v) =>
                        patch("subscriptionUpgrades", { subscribeLabel: v })
                      }
                    />
                  </>
                )}

                {active === "stickyCart" && (
                  <>
                    <s-select
                      label="Position"
                      value={config.stickyCart.position}
                      onChange={(e: Event) =>
                        patch("stickyCart", {
                          position: (e.target as HTMLSelectElement).value as
                            | "bottom-right"
                            | "bottom-left",
                        })
                      }
                    >
                      <option value="bottom-right">Bottom right</option>
                      <option value="bottom-left">Bottom left</option>
                    </s-select>
                    <ColorField
                      label="Background"
                      value={config.stickyCart.backgroundColor}
                      onChange={(v) =>
                        patch("stickyCart", { backgroundColor: v })
                      }
                    />
                    <Toggle
                      label="Show count badge"
                      checked={config.stickyCart.showCount}
                      onChange={(v) => patch("stickyCart", { showCount: v })}
                    />
                  </>
                )}

                {active === "behavior" && (
                  <>
                    <Toggle
                      label="Open on add to cart"
                      checked={config.behavior.openOnAddToCart}
                      onChange={(v) =>
                        patch("behavior", { openOnAddToCart: v })
                      }
                    />
                    <s-select
                      label="Drawer position"
                      value={config.behavior.position}
                      onChange={(e: Event) =>
                        patch("behavior", {
                          position: (e.target as HTMLSelectElement).value as
                            | "left"
                            | "right",
                        })
                      }
                    >
                      <option value="right">Right</option>
                      <option value="left">Left</option>
                    </s-select>
                    <TextField
                      label="Open cart button selectors"
                      value={config.behavior.openCartSelectors}
                      onChange={(v) =>
                        patch("behavior", { openCartSelectors: v })
                      }
                    />
                    <TextField
                      label="Add to cart button selectors"
                      value={config.behavior.addToCartSelectors}
                      onChange={(v) =>
                        patch("behavior", { addToCartSelectors: v })
                      }
                    />
                    <Toggle
                      label="Shadow DOM"
                      checked={config.behavior.shadowDom}
                      onChange={(v) => patch("behavior", { shadowDom: v })}
                    />
                    <Toggle
                      label="Continue shopping button"
                      checked={config.behavior.continueShopping}
                      onChange={(v) =>
                        patch("behavior", { continueShopping: v })
                      }
                    />
                    <Toggle
                      label="Go to cart page on checkout"
                      checked={config.behavior.goToCartOnCheckout}
                      onChange={(v) =>
                        patch("behavior", { goToCartOnCheckout: v })
                      }
                    />
                    <Toggle
                      label="Disable fixed footer"
                      checked={config.behavior.disableFixedFooter}
                      onChange={(v) =>
                        patch("behavior", { disableFixedFooter: v })
                      }
                    />
                  </>
                )}
              </s-stack>
            </s-box>
          </div>
        </div>
      </s-section>

      <Form method="post" style={{ display: "none" }}>
        <input type="hidden" name="cartId" value={data.cartId} />
      </Form>
    </s-page>
  );
}

function TextField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
}) {
  return (
    <s-text-field
      label={label}
      value={value}
      onChange={(e: Event) => onChange((e.target as HTMLInputElement).value)}
    />
  );
}

function NumberField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: number;
  onChange: (v: number) => void;
}) {
  return (
    <s-text-field
      label={label}
      value={String(value)}
      onChange={(e: Event) =>
        onChange(Number((e.target as HTMLInputElement).value) || 0)
      }
    />
  );
}

function ColorField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
}) {
  return (
    <s-stack direction="inline" gap="base">
      <s-text-field
        label={label}
        value={value}
        onChange={(e: Event) => onChange((e.target as HTMLInputElement).value)}
      />
      <input
        type="color"
        value={/^#[0-9a-fA-F]{6}$/.test(value) ? value : "#000000"}
        onChange={(e) => onChange(e.target.value)}
        aria-label={label}
      />
    </s-stack>
  );
}

function Toggle({
  label,
  checked,
  onChange,
}: {
  label: string;
  checked: boolean;
  onChange: (v: boolean) => void;
}) {
  return (
    <s-checkbox
      label={label}
      checked={checked}
      onChange={(e: Event) =>
        onChange((e.target as HTMLInputElement).checked)
      }
    />
  );
}

export const headers: HeadersFunction = (headersArgs) =>
  boundary.headers(headersArgs);
