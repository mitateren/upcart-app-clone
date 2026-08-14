export type RewardTierType = "shipping" | "discount" | "product";

export interface RewardTier {
  id: string;
  threshold: number;
  type: RewardTierType;
  textBefore: string;
  textAfter: string;
  discountPercent?: number;
  productVariantId?: string;
  productTitle?: string;
}

export interface CartConfig {
  design: {
    enabled: boolean;
    backgroundColor: string;
    textColor: string;
    buttonBackground: string;
    buttonTextColor: string;
    accentColor: string;
    borderRadius: number;
    showStrikethroughPrices: boolean;
    showSavingsBelowPrices: boolean;
    fontFamily: string;
  };
  header: {
    enabled: boolean;
    title: string;
    showItemCount: boolean;
    showLogo: boolean;
    logoUrl: string;
    closeButtonStyle: "x" | "text";
  };
  announcements: {
    enabled: boolean;
    text: string;
    backgroundColor: string;
    textColor: string;
    timerMinutes: number;
    showTimer: boolean;
  };
  rewards: {
    enabled: boolean;
    basis: "cart_total" | "item_count";
    barColor: string;
    backgroundColor: string;
    completedText: string;
    tiers: RewardTier[];
  };
  upsells: {
    enabled: boolean;
    title: string;
    addButtonLabel: string;
    useAi: boolean;
    algorithm: "related" | "complementary";
    smartVariantMatching: boolean;
    manualProductIds: string[];
    maxItems: number;
  };
  recommendations: {
    enabled: boolean;
    title: string;
    emptyCartOnly: boolean;
    maxItems: number;
  };
  addons: {
    enabled: boolean;
    mode: "shipping_protection" | "product";
    title: string;
    description: string;
    productVariantId: string;
    productTitle: string;
    shippingTiers: { maxCartValue: number; price: string }[];
  };
  discountCodes: {
    enabled: boolean;
    placeholder: string;
    buttonLabel: string;
  };
  expressPayments: {
    enabled: boolean;
    alignment: "left" | "center" | "right" | "stretch";
  };
  trustBadges: {
    enabled: boolean;
    position: "top" | "bottom";
    imageUrl: string;
    alt: string;
  };
  additionalNotes: {
    enabled: boolean;
    label: string;
    placeholder: string;
    required: boolean;
  };
  subscriptionUpgrades: {
    enabled: boolean;
    title: string;
    oneTimeLabel: string;
    subscribeLabel: string;
  };
  stickyCart: {
    enabled: boolean;
    position: "bottom-right" | "bottom-left";
    backgroundColor: string;
    iconColor: string;
    showCount: boolean;
  };
  behavior: {
    openOnAddToCart: boolean;
    position: "left" | "right";
    openCartSelectors: string;
    addToCartSelectors: string;
    shadowDom: boolean;
    continueShopping: boolean;
    continueShoppingLabel: string;
    goToCartOnCheckout: boolean;
    disableFixedFooter: boolean;
  };
  customCss: string;
  customHtml: {
    beforeAnnouncements: string;
    betweenItems: string;
    aboveCheckout: string;
    scripts: string;
  };
  translations: Record<string, string>;
}

export const DEFAULT_CART_CONFIG: CartConfig = {
  design: {
    enabled: true,
    backgroundColor: "#ffffff",
    textColor: "#111111",
    buttonBackground: "#111111",
    buttonTextColor: "#ffffff",
    accentColor: "#0a7c5e",
    borderRadius: 8,
    showStrikethroughPrices: true,
    showSavingsBelowPrices: true,
    fontFamily: "inherit",
  },
  header: {
    enabled: true,
    title: "Your cart",
    showItemCount: true,
    showLogo: false,
    logoUrl: "",
    closeButtonStyle: "x",
  },
  announcements: {
    enabled: true,
    text: "Free shipping on orders over $75 — offer ends in {TIMER}",
    backgroundColor: "#111111",
    textColor: "#ffffff",
    timerMinutes: 15,
    showTimer: true,
  },
  rewards: {
    enabled: true,
    basis: "cart_total",
    barColor: "#0a7c5e",
    backgroundColor: "#e8f5f0",
    completedText: "You've unlocked all rewards!",
    tiers: [
      {
        id: "tier-1",
        threshold: 50,
        type: "shipping",
        textBefore: "Add {remaining} more for free shipping",
        textAfter: "Free shipping unlocked!",
      },
      {
        id: "tier-2",
        threshold: 100,
        type: "discount",
        textBefore: "Add {remaining} more for 10% off",
        textAfter: "10% discount unlocked!",
        discountPercent: 10,
      },
    ],
  },
  upsells: {
    enabled: true,
    title: "You may also like",
    addButtonLabel: "Add",
    useAi: true,
    algorithm: "related",
    smartVariantMatching: true,
    manualProductIds: [],
    maxItems: 6,
  },
  recommendations: {
    enabled: true,
    title: "Popular products",
    emptyCartOnly: true,
    maxItems: 4,
  },
  addons: {
    enabled: false,
    mode: "product",
    title: "Add shipping protection",
    description: "Protect your order against loss or damage",
    productVariantId: "",
    productTitle: "",
    shippingTiers: [
      { maxCartValue: 50, price: "1.99" },
      { maxCartValue: 100, price: "2.99" },
      { maxCartValue: 999999, price: "4.99" },
    ],
  },
  discountCodes: {
    enabled: true,
    placeholder: "Discount code",
    buttonLabel: "Apply",
  },
  expressPayments: {
    enabled: true,
    alignment: "stretch",
  },
  trustBadges: {
    enabled: false,
    position: "bottom",
    imageUrl: "",
    alt: "Trusted checkout",
  },
  additionalNotes: {
    enabled: false,
    label: "Order notes",
    placeholder: "Special instructions…",
    required: false,
  },
  subscriptionUpgrades: {
    enabled: false,
    title: "Subscribe & save",
    oneTimeLabel: "One-time",
    subscribeLabel: "Subscribe",
  },
  stickyCart: {
    enabled: true,
    position: "bottom-right",
    backgroundColor: "#111111",
    iconColor: "#ffffff",
    showCount: true,
  },
  behavior: {
    openOnAddToCart: true,
    position: "right",
    openCartSelectors:
      "a[href='/cart'], a[href$='/cart'], [data-cart-icon], .header__icon--cart, #cart-icon-bubble",
    addToCartSelectors:
      "form[action='/cart/add'] [type='submit'], [name='add'], .product-form__submit",
    shadowDom: false,
    continueShopping: true,
    continueShoppingLabel: "Continue shopping",
    goToCartOnCheckout: false,
    disableFixedFooter: false,
  },
  customCss: "",
  customHtml: {
    beforeAnnouncements: "",
    betweenItems: "",
    aboveCheckout: "",
    scripts: "",
  },
  translations: {
    emptyCart: "Your cart is empty",
    subtotal: "Subtotal",
    savings: "You're saving",
    checkout: "Checkout",
    remove: "Remove",
    quantity: "Qty",
  },
};

export function parseCartConfig(raw: string | null | undefined): CartConfig {
  if (!raw) return structuredClone(DEFAULT_CART_CONFIG);
  try {
    const parsed = JSON.parse(raw) as Partial<CartConfig>;
    return deepMergeConfig(structuredClone(DEFAULT_CART_CONFIG), parsed);
  } catch {
    return structuredClone(DEFAULT_CART_CONFIG);
  }
}

function deepMergeConfig(
  base: CartConfig,
  override: Partial<CartConfig>,
): CartConfig {
  const result = { ...base };
  for (const key of Object.keys(override) as (keyof CartConfig)[]) {
    const value = override[key];
    const baseValue = base[key];
    if (
      value &&
      typeof value === "object" &&
      !Array.isArray(value) &&
      baseValue &&
      typeof baseValue === "object" &&
      !Array.isArray(baseValue)
    ) {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (result as any)[key] = { ...(baseValue as object), ...(value as object) };
    } else if (value !== undefined) {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (result as any)[key] = value;
    }
  }
  return result;
}
