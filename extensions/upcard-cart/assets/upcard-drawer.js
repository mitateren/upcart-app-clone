(function () {
  "use strict";

  const boot = window.UpCardBootstrap || {};
  if (!boot.shop) return;

  const state = {
    open: false,
    cart: null,
    config: null,
    cartId: null,
    discountRules: [],
    timerEndsAt: null,
    notes: "",
    discountCode: "",
    discountMessage: "",
    upsells: [],
    loading: false,
  };

  const els = {};

  function money(cents) {
    const amount = (Number(cents) || 0) / 100;
    const format = boot.moneyFormat || "${{amount}}";
    try {
      return format
        .replace(/\{\{\s*amount\s*\}\}/g, amount.toFixed(2))
        .replace(/\{\{\s*amount_no_decimals\s*\}\}/g, String(Math.round(amount)))
        .replace(
          /\{\{\s*amount_with_comma_separator\s*\}\}/g,
          amount.toFixed(2).replace(".", ","),
        );
    } catch {
      return "$" + amount.toFixed(2);
    }
  }

  async function fetchJson(url, options) {
    const res = await fetch(url, {
      credentials: "same-origin",
      headers: {
        Accept: "application/json",
        ...(options && options.body instanceof FormData
          ? {}
          : { "Content-Type": "application/json" }),
      },
      ...options,
    });
    if (!res.ok) throw new Error("Request failed: " + res.status);
    return res.json();
  }

  async function loadConfig() {
    try {
      const data = await fetchJson(boot.configUrl + "?shop=" + encodeURIComponent(boot.shop));
      state.config = data.config;
      state.cartId = data.cartId;
      state.discountRules = data.discountRules || [];
      if (data.carts && data.carts.length > 1) {
        const pick = pickTrafficCart(data.carts);
        if (pick) {
          state.config = pick.config;
          state.cartId = pick.id;
        }
      }
    } catch (e) {
      console.warn("[UpCard] config load failed, using defaults", e);
      state.config = defaultConfig();
    }
    applyDesign();
  }

  function pickTrafficCart(carts) {
    const total = carts.reduce((s, c) => s + (c.trafficAllocation || 0), 0);
    if (!total) return carts[0];
    let r = Math.random() * total;
    for (const c of carts) {
      r -= c.trafficAllocation || 0;
      if (r <= 0) return c;
    }
    return carts[0];
  }

  function defaultConfig() {
    return {
      design: {
        backgroundColor: "#fff",
        textColor: "#111",
        buttonBackground: "#111",
        buttonTextColor: "#fff",
        accentColor: "#0a7c5e",
        borderRadius: 8,
        showStrikethroughPrices: true,
        showSavingsBelowPrices: true,
        fontFamily: "inherit",
      },
      header: { enabled: true, title: "Your cart", showItemCount: true, closeButtonStyle: "x" },
      announcements: { enabled: false },
      rewards: { enabled: false, tiers: [] },
      upsells: { enabled: false },
      recommendations: { enabled: false },
      addons: { enabled: false },
      discountCodes: { enabled: true, placeholder: "Discount code", buttonLabel: "Apply" },
      expressPayments: { enabled: false },
      trustBadges: { enabled: false },
      additionalNotes: { enabled: false },
      subscriptionUpgrades: { enabled: false },
      stickyCart: { enabled: true, position: "bottom-right", backgroundColor: "#111", iconColor: "#fff", showCount: true },
      behavior: {
        openOnAddToCart: true,
        position: "right",
        openCartSelectors: "a[href='/cart'], a[href$='/cart']",
        addToCartSelectors: "form[action='/cart/add'] [type='submit'], [name='add']",
        continueShopping: true,
        continueShoppingLabel: "Continue shopping",
        goToCartOnCheckout: false,
        disableFixedFooter: false,
        shadowDom: false,
      },
      customCss: "",
      customHtml: {},
      translations: {
        emptyCart: "Your cart is empty",
        subtotal: "Subtotal",
        savings: "You're saving",
        checkout: "Checkout",
        remove: "Remove",
      },
    };
  }

  function applyDesign() {
    const d = state.config.design || {};
    const root = document.documentElement;
    root.style.setProperty("--upcard-bg", d.backgroundColor || "#fff");
    root.style.setProperty("--upcard-text", d.textColor || "#111");
    root.style.setProperty("--upcard-btn-bg", d.buttonBackground || "#111");
    root.style.setProperty("--upcard-btn-text", d.buttonTextColor || "#fff");
    root.style.setProperty("--upcard-accent", d.accentColor || "#0a7c5e");
    root.style.setProperty("--upcard-radius", (d.borderRadius || 8) + "px");
    root.style.setProperty("--upcard-font", d.fontFamily || "inherit");

    let styleEl = document.getElementById("upcard-custom-css");
    if (!styleEl) {
      styleEl = document.createElement("style");
      styleEl.id = "upcard-custom-css";
      document.head.appendChild(styleEl);
    }
    styleEl.textContent = state.config.customCss || "";
  }

  async function refreshCart() {
    state.cart = await fetchJson(boot.cartUrl + ".js");
    updateStickyCount();
    if (state.open) render();
    maybeLoadUpsells();
  }

  function updateStickyCount() {
    if (!els.stickyCount) return;
    const n = state.cart ? state.cart.item_count : boot.cartItemCount || 0;
    els.stickyCount.textContent = String(n);
    els.stickyCount.hidden = !n;
  }

  function track(eventType, meta) {
    if (!boot.analyticsUrl) return;
    fetch(boot.analyticsUrl, {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        shop: boot.shop,
        cartId: state.cartId,
        eventType,
        meta,
      }),
      keepalive: true,
    }).catch(function () {});
  }

  function openDrawer() {
    state.open = true;
    if (!state.timerEndsAt && state.config.announcements && state.config.announcements.showTimer) {
      const mins = state.config.announcements.timerMinutes || 15;
      state.timerEndsAt = Date.now() + mins * 60 * 1000;
    }
    els.overlay.hidden = false;
    els.drawer.hidden = false;
    els.overlay.classList.add("is-open");
    els.drawer.classList.add("is-open");
    requestAnimationFrame(function () {
      els.overlay.classList.add("is-open");
      els.drawer.classList.add("is-open");
    });
    document.documentElement.style.overflow = "hidden";
    track("open");
    refreshCart().then(function () {
      render();
      els.overlay.classList.add("is-open");
      els.drawer.classList.add("is-open");
    });
  }

  function closeDrawer() {
    state.open = false;
    els.overlay.classList.remove("is-open");
    els.drawer.classList.remove("is-open");
    document.documentElement.style.overflow = "";
    setTimeout(function () {
      if (!state.open) {
        els.overlay.hidden = true;
        els.drawer.hidden = true;
      }
    }, 280);
  }

  function cartSubtotal() {
    return state.cart ? state.cart.items_subtotal_price || state.cart.total_price : 0;
  }

  function cartCompareSavings() {
    if (!state.cart) return 0;
    return state.cart.items.reduce(function (sum, item) {
      const compare = item.original_line_price || item.line_price;
      const current = item.final_line_price || item.line_price;
      return sum + Math.max(0, compare - current);
    }, 0);
  }

  function rewardsProgress() {
    const rewards = state.config.rewards;
    if (!rewards || !rewards.enabled || !rewards.tiers || !rewards.tiers.length) return null;
    const basis =
      rewards.basis === "item_count"
        ? state.cart
          ? state.cart.item_count
          : 0
        : cartSubtotal() / 100;
    const tiers = rewards.tiers.slice().sort(function (a, b) {
      return a.threshold - b.threshold;
    });
    let currentTier = null;
    let nextTier = tiers[0];
    let reached = 0;
    for (let i = 0; i < tiers.length; i++) {
      if (basis >= tiers[i].threshold) {
        currentTier = tiers[i];
        reached = i + 1;
        nextTier = tiers[i + 1] || null;
      }
    }
    const max = tiers[tiers.length - 1].threshold;
    const pct = Math.min(100, (basis / max) * 100);
    let text = rewards.completedText || "All rewards unlocked!";
    if (nextTier) {
      const remaining =
        rewards.basis === "item_count"
          ? Math.max(0, Math.ceil(nextTier.threshold - basis))
          : money(Math.max(0, nextTier.threshold * 100 - cartSubtotal()));
      text = (nextTier.textBefore || "Add {remaining} more").replace("{remaining}", remaining);
    } else if (currentTier) {
      text = currentTier.textAfter || text;
    }
    return { pct: pct, text: text, reached: reached, nextTier: nextTier, currentTier: currentTier };
  }

  async function maybeLoadUpsells() {
    const cfg = state.config.upsells;
    const rec = state.config.recommendations;
    const empty = !state.cart || !state.cart.items.length;

    if (empty && rec && rec.enabled) {
      state.upsells = await loadRecommendations(null, rec.maxItems || 4);
      if (state.open) render();
      return;
    }

    if (!cfg || !cfg.enabled || empty) {
      state.upsells = [];
      return;
    }

    const manual = (cfg.manualProductIds || [])
      .map(function (id) {
        const s = String(id);
        const m = s.match(/(\d+)\s*$/);
        return m ? Number(m[1]) : NaN;
      })
      .filter(function (n) {
        return !isNaN(n);
      });

    if (manual.length) {
      state.upsells = await loadManualProducts(manual, cfg.maxItems || 6);
    } else if (cfg.useAi) {
      const first = state.cart.items[0];
      state.upsells = await loadRecommendations(first.product_id, cfg.maxItems || 6);
    } else {
      state.upsells = [];
    }
    if (state.open) render();
  }

  async function loadManualProducts(productIds, limit) {
    try {
      const data = await fetchJson("/products.json?limit=250");
      const products = data.products || [];
      const wanted = new Set(productIds);
      const inCart = new Set(
        (state.cart && state.cart.items ? state.cart.items : []).map(function (i) {
          return i.product_id;
        }),
      );
      return products
        .filter(function (p) {
          return wanted.has(p.id) && p.available && !inCart.has(p.id);
        })
        .slice(0, limit || 6)
        .map(function (p) {
          const variant = pickVariant(p);
          return {
            id: p.id,
            title: p.title,
            handle: p.handle,
            image: p.featured_image || (p.images && p.images[0]),
            price: variant ? variant.price : p.price,
            variantId: variant ? variant.id : null,
          };
        });
    } catch (e) {
      console.warn("[UpCard] manual upsells failed", e);
      return [];
    }
  }

  async function loadRecommendations(productId, limit) {
    try {
      let url =
        "/recommendations/products.json?limit=" +
        encodeURIComponent(limit || 6) +
        "&intent=related";
      if (productId) url += "&product_id=" + encodeURIComponent(productId);
      const data = await fetchJson(url);
      const products = data.products || [];
      const inCart = new Set(
        (state.cart && state.cart.items ? state.cart.items : []).map(function (i) {
          return i.product_id;
        }),
      );
      return products
        .filter(function (p) {
          return p.available && !inCart.has(p.id);
        })
        .slice(0, limit || 6)
        .map(function (p) {
          const variant = pickVariant(p);
          return {
            id: p.id,
            title: p.title,
            handle: p.handle,
            image: p.featured_image || (p.images && p.images[0]),
            price: variant ? variant.price : p.price,
            variantId: variant ? variant.id : null,
          };
        });
    } catch (e) {
      console.warn("[UpCard] recommendations failed", e);
      return [];
    }
  }

  function pickVariant(product) {
    if (!product.variants || !product.variants.length) return null;
    const cfg = state.config.upsells;
    if (cfg && cfg.smartVariantMatching && state.cart && state.cart.items.length) {
      const cartOpts = {};
      state.cart.items.forEach(function (item) {
        (item.options_with_values || []).forEach(function (o) {
          cartOpts[o.name] = o.value;
        });
      });
      const match = product.variants.find(function (v) {
        if (!v.available) return false;
        const opts = v.options || [];
        return opts.every(function (val, idx) {
          const name = product.options[idx];
          return !cartOpts[name] || cartOpts[name] === val;
        });
      });
      if (match) return match;
    }
    return product.variants.find(function (v) {
      return v.available;
    }) || product.variants[0];
  }

  async function changeQty(key, quantity) {
    state.loading = true;
    render();
    try {
      await fetchJson(boot.cartChangeUrl + ".js", {
        method: "POST",
        body: JSON.stringify({ id: key, quantity: quantity }),
      });
      await refreshCart();
    } finally {
      state.loading = false;
      render();
    }
  }

  async function addVariant(variantId, properties) {
    state.loading = true;
    render();
    try {
      const body = { items: [{ id: Number(variantId), quantity: 1, properties: properties || {} }] };
      await fetchJson(boot.cartAddUrl + ".js", {
        method: "POST",
        body: JSON.stringify(body),
      });
      track("add_upsell", { variantId: variantId });
      await refreshCart();
      if (!state.open && state.config.behavior.openOnAddToCart) openDrawer();
      else render();
    } finally {
      state.loading = false;
      render();
    }
  }

  async function applyDiscount() {
    if (!state.discountCode) return;
    try {
      await fetch("/discount/" + encodeURIComponent(state.discountCode), {
        credentials: "same-origin",
      });
      state.discountMessage = "Discount applied";
      track("discount_applied", { code: state.discountCode });
      await refreshCart();
    } catch {
      state.discountMessage = "Could not apply code";
    }
    render();
  }

  async function saveNotes() {
    if (!state.config.additionalNotes || !state.config.additionalNotes.enabled) return;
    await fetchJson(boot.cartUpdateUrl + ".js", {
      method: "POST",
      body: JSON.stringify({ note: state.notes }),
    });
  }

  function formatTimer() {
    if (!state.timerEndsAt) return "00:00";
    const ms = Math.max(0, state.timerEndsAt - Date.now());
    const total = Math.floor(ms / 1000);
    const m = String(Math.floor(total / 60)).padStart(2, "0");
    const s = String(total % 60).padStart(2, "0");
    return m + ":" + s;
  }

  function htmlEscape(str) {
    return String(str || "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function render() {
    const cfg = state.config;
    const t = cfg.translations || {};
    const cart = state.cart;
    const design = cfg.design || {};

    els.drawer.setAttribute("data-position", (cfg.behavior && cfg.behavior.position) || "right");
    if (state.open) {
      els.drawer.hidden = false;
      els.overlay.hidden = false;
      els.drawer.classList.add("is-open");
      els.overlay.classList.add("is-open");
    }

    // Header
    let headerHtml = "";
    if (cfg.header.enabled) {
      const count =
        cfg.header.showItemCount && cart
          ? '<span class="upcard-header__count">(' + cart.item_count + ")</span>"
          : "";
      const logo =
        cfg.header.showLogo && cfg.header.logoUrl
          ? '<img src="' + htmlEscape(cfg.header.logoUrl) + '" alt="" height="28" />'
          : "";
      headerHtml =
        '<div class="upcard-header">' +
        "<div>" +
        logo +
        '<p class="upcard-header__title">' +
        htmlEscape(cfg.header.title) +
        count +
        "</p></div>" +
        '<button type="button" class="upcard-close" data-upcard-close aria-label="Close">' +
        (cfg.header.closeButtonStyle === "text" ? "Close" : "×") +
        "</button></div>";
    }

    // Announcement
    let announcementHtml = "";
    if (cfg.announcements && cfg.announcements.enabled) {
      let text = cfg.announcements.text || "";
      if (cfg.announcements.showTimer) text = text.replace("{TIMER}", formatTimer());
      announcementHtml =
        '<div class="upcard-announcement" style="background:' +
        htmlEscape(cfg.announcements.backgroundColor) +
        ";color:" +
        htmlEscape(cfg.announcements.textColor) +
        '">' +
        htmlEscape(text) +
        "</div>";
    }

    // Custom HTML before announcements
    const customBefore = (cfg.customHtml && cfg.customHtml.beforeAnnouncements) || "";

    // Rewards
    let rewardsHtml = "";
    const progress = rewardsProgress();
    if (progress && cfg.rewards.enabled) {
      rewardsHtml =
        '<div class="upcard-rewards">' +
        '<div class="upcard-rewards__text">' +
        htmlEscape(progress.text) +
        "</div>" +
        '<div class="upcard-rewards__track" style="background:' +
        htmlEscape(cfg.rewards.backgroundColor) +
        '">' +
        '<div class="upcard-rewards__fill" style="width:' +
        progress.pct +
        "%;background:" +
        htmlEscape(cfg.rewards.barColor) +
        '"></div></div></div>';
      if (progress.reached > 0) {
        track("reward_tier_reached", { reached: progress.reached });
      }
    }

    // Trust top
    let trustTop = "";
    if (cfg.trustBadges && cfg.trustBadges.enabled && cfg.trustBadges.position === "top" && cfg.trustBadges.imageUrl) {
      trustTop =
        '<div class="upcard-trust"><img src="' +
        htmlEscape(cfg.trustBadges.imageUrl) +
        '" alt="' +
        htmlEscape(cfg.trustBadges.alt || "") +
        '"/></div>';
    }

    // Items
    let itemsHtml = "";
    if (!cart || !cart.items.length) {
      itemsHtml = '<div class="upcard-empty">' + htmlEscape(t.emptyCart || "Your cart is empty") + "</div>";
    } else {
      itemsHtml = cart.items
        .map(function (item) {
          const compare =
            design.showStrikethroughPrices && item.original_line_price > item.final_line_price
              ? '<span class="upcard-price__compare">' + money(item.original_line_price) + "</span>"
              : "";
          const saveAmt = item.original_line_price - item.final_line_price;
          const savings =
            design.showSavingsBelowPrices && saveAmt > 0
              ? '<div class="upcard-savings">Save ' + money(saveAmt) + "</div>"
              : "";
          const img = item.image
            ? '<img class="upcard-item__image" src="' + htmlEscape(item.image) + '" alt=""/>'
            : '<div class="upcard-item__image"></div>';
          return (
            '<div class="upcard-item" data-key="' +
            htmlEscape(item.key) +
            '">' +
            img +
            "<div>" +
            '<p class="upcard-item__title">' +
            htmlEscape(item.product_title) +
            "</p>" +
            (item.variant_title && item.variant_title !== "Default Title"
              ? '<div class="upcard-item__variant">' + htmlEscape(item.variant_title) + "</div>"
              : "") +
            '<div class="upcard-item__row">' +
            '<div class="upcard-qty">' +
            '<button type="button" data-qty-dec="' +
            htmlEscape(item.key) +
            '">−</button>' +
            "<span>" +
            item.quantity +
            "</span>" +
            '<button type="button" data-qty-inc="' +
            htmlEscape(item.key) +
            '">+</button></div>' +
            '<div class="upcard-price">' +
            compare +
            money(item.final_line_price || item.line_price) +
            "</div></div>" +
            savings +
            '<button type="button" class="upcard-remove" data-remove="' +
            htmlEscape(item.key) +
            '">' +
            htmlEscape(t.remove || "Remove") +
            "</button></div></div>"
          );
        })
        .join("");
      if (cfg.customHtml && cfg.customHtml.betweenItems) {
        itemsHtml += cfg.customHtml.betweenItems;
      }
    }

    // Upsells / recommendations
    let upsellHtml = "";
    const showRecEmpty =
      cfg.recommendations &&
      cfg.recommendations.enabled &&
      (!cart || !cart.items.length) &&
      state.upsells.length;
    const showUpsell =
      cfg.upsells && cfg.upsells.enabled && cart && cart.items.length && state.upsells.length;
    if (showRecEmpty || showUpsell) {
      const title = showRecEmpty
        ? cfg.recommendations.title
        : cfg.upsells.title || "You may also like";
      const btn = (cfg.upsells && cfg.upsells.addButtonLabel) || "Add";
      upsellHtml =
        '<div class="upcard-section-title">' +
        htmlEscape(title) +
        '</div><div class="upcard-upsells">' +
        state.upsells
          .map(function (u) {
            return (
              '<div class="upcard-upsell-card">' +
              (u.image ? '<img src="' + htmlEscape(u.image) + '" alt=""/>' : "") +
              '<div class="upcard-upsell-card__title">' +
              htmlEscape(u.title) +
              "</div>" +
              '<div class="upcard-upsell-card__price">' +
              money(u.price) +
              "</div>" +
              (u.variantId
                ? '<button type="button" class="upcard-btn upcard-btn--small" data-add-variant="' +
                  u.variantId +
                  '">' +
                  htmlEscape(btn) +
                  "</button>"
                : "") +
              "</div>"
            );
          })
          .join("") +
        "</div>";
    }

    // Notes
    let notesHtml = "";
    if (cfg.additionalNotes && cfg.additionalNotes.enabled) {
      notesHtml =
        '<div class="upcard-notes"><label>' +
        htmlEscape(cfg.additionalNotes.label) +
        '</label><textarea rows="2" data-upcard-notes placeholder="' +
        htmlEscape(cfg.additionalNotes.placeholder || "") +
        '">' +
        htmlEscape(state.notes || (cart && cart.note) || "") +
        "</textarea></div>";
    }

    // Discount
    let discountHtml = "";
    if (cfg.discountCodes && cfg.discountCodes.enabled) {
      discountHtml =
        '<div class="upcard-discount"><label>' +
        htmlEscape(cfg.discountCodes.placeholder) +
        '</label><div class="upcard-discount__row">' +
        '<input type="text" data-upcard-discount value="' +
        htmlEscape(state.discountCode) +
        '" placeholder="' +
        htmlEscape(cfg.discountCodes.placeholder) +
        '"/>' +
        '<button type="button" class="upcard-btn upcard-btn--small" data-apply-discount style="width:auto">' +
        htmlEscape(cfg.discountCodes.buttonLabel) +
        "</button></div>" +
        (state.discountMessage
          ? '<div style="font-size:0.75rem;margin-top:6px">' +
            htmlEscape(state.discountMessage) +
            "</div>"
          : "") +
        "</div>";
    }

    // Add-ons
    let addonHtml = "";
    if (cfg.addons && cfg.addons.enabled && cfg.addons.productVariantId) {
      const inCart =
        cart &&
        cart.items.some(function (i) {
          return String(i.variant_id) === String(cfg.addons.productVariantId);
        });
      addonHtml =
        '<div class="upcard-addon"><div class="upcard-addon__meta"><strong>' +
        htmlEscape(cfg.addons.title) +
        '</strong><div class="upcard-addon__desc">' +
        htmlEscape(cfg.addons.description) +
        '</div></div><label class="upcard-switch"><input type="checkbox" data-addon-toggle ' +
        (inCart ? "checked" : "") +
        "/><span></span></label></div>";
    }

    // Trust bottom
    let trustBottom = "";
    if (
      cfg.trustBadges &&
      cfg.trustBadges.enabled &&
      cfg.trustBadges.position === "bottom" &&
      cfg.trustBadges.imageUrl
    ) {
      trustBottom =
        '<div class="upcard-trust"><img src="' +
        htmlEscape(cfg.trustBadges.imageUrl) +
        '" alt="' +
        htmlEscape(cfg.trustBadges.alt || "") +
        '"/></div>';
    }

    // Footer
    const savings = cartCompareSavings();
    const customAbove = (cfg.customHtml && cfg.customHtml.aboveCheckout) || "";
    const footerClass =
      "upcard-footer" + (cfg.behavior.disableFixedFooter ? "" : " is-fixed");
    const checkoutHref = cfg.behavior.goToCartOnCheckout ? boot.cartUrl : "/checkout";
    let footerHtml =
      '<div class="' +
      footerClass +
      '">' +
      '<div class="upcard-footer__row"><span>' +
      htmlEscape(t.subtotal || "Subtotal") +
      "</span><strong>" +
      money(cartSubtotal()) +
      "</strong></div>" +
      (savings > 0
        ? '<div class="upcard-footer__savings">' +
          htmlEscape(t.savings || "You're saving") +
          " " +
          money(savings) +
          "</div>"
        : "") +
      customAbove +
      '<a class="upcard-btn" href="' +
      checkoutHref +
      '" data-upcard-checkout>' +
      htmlEscape(t.checkout || "Checkout") +
      "</a>";

    if (cfg.expressPayments && cfg.expressPayments.enabled) {
      footerHtml +=
        '<div class="upcard-express" data-align="' +
        htmlEscape(cfg.expressPayments.alignment || "stretch") +
        '">' +
        '<div data-shopify="payment-button" class="shopify-payment-button"></div></div>';
    }

    if (cfg.behavior.continueShopping) {
      footerHtml +=
        '<button type="button" class="upcard-btn upcard-btn--secondary" data-upcard-close>' +
        htmlEscape(cfg.behavior.continueShoppingLabel || "Continue shopping") +
        "</button>";
    }
    footerHtml += "</div>";

    els.drawer.innerHTML =
      headerHtml +
      customBefore +
      announcementHtml +
      rewardsHtml +
      trustTop +
      '<div class="upcard-body">' +
      itemsHtml +
      upsellHtml +
      notesHtml +
      discountHtml +
      addonHtml +
      trustBottom +
      "</div>" +
      footerHtml;

    // Sticky visibility
    if (els.sticky) {
      els.sticky.hidden = !(cfg.stickyCart && cfg.stickyCart.enabled);
      if (cfg.stickyCart) {
        els.sticky.setAttribute("data-position", cfg.stickyCart.position || "bottom-right");
        els.sticky.style.background = cfg.stickyCart.backgroundColor || "#111";
        els.sticky.style.color = cfg.stickyCart.iconColor || "#fff";
      }
    }

    bindDrawerEvents();
  }

  function bindDrawerEvents() {
    els.drawer.querySelectorAll("[data-upcard-close]").forEach(function (btn) {
      btn.addEventListener("click", closeDrawer);
    });
    els.drawer.querySelectorAll("[data-qty-inc]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        const key = btn.getAttribute("data-qty-inc");
        const item = state.cart.items.find(function (i) {
          return i.key === key;
        });
        if (item) changeQty(key, item.quantity + 1);
      });
    });
    els.drawer.querySelectorAll("[data-qty-dec]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        const key = btn.getAttribute("data-qty-dec");
        const item = state.cart.items.find(function (i) {
          return i.key === key;
        });
        if (item) changeQty(key, Math.max(0, item.quantity - 1));
      });
    });
    els.drawer.querySelectorAll("[data-remove]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        changeQty(btn.getAttribute("data-remove"), 0);
      });
    });
    els.drawer.querySelectorAll("[data-add-variant]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        addVariant(btn.getAttribute("data-add-variant"));
      });
    });
    const discountInput = els.drawer.querySelector("[data-upcard-discount]");
    if (discountInput) {
      discountInput.addEventListener("input", function (e) {
        state.discountCode = e.target.value;
      });
    }
    const applyBtn = els.drawer.querySelector("[data-apply-discount]");
    if (applyBtn) applyBtn.addEventListener("click", applyDiscount);

    const notes = els.drawer.querySelector("[data-upcard-notes]");
    if (notes) {
      notes.addEventListener("change", function (e) {
        state.notes = e.target.value;
        saveNotes();
      });
    }

    const addon = els.drawer.querySelector("[data-addon-toggle]");
    if (addon) {
      addon.addEventListener("change", function (e) {
        const vid = state.config.addons.productVariantId;
        if (e.target.checked) addVariant(vid, { _upcard_addon: "true" });
        else {
          const item = state.cart.items.find(function (i) {
            return String(i.variant_id) === String(vid);
          });
          if (item) changeQty(item.key, 0);
        }
      });
    }

    const checkout = els.drawer.querySelector("[data-upcard-checkout]");
    if (checkout) {
      checkout.addEventListener("click", function () {
        track("checkout_click");
      });
    }
  }

  function interceptClicks() {
    const cfg = state.config.behavior || {};
    document.addEventListener(
      "click",
      function (e) {
        const openSel = cfg.openCartSelectors || "";
        const addSel = cfg.addToCartSelectors || "";
        const openEl = openSel && e.target.closest(openSel);
        if (openEl) {
          e.preventDefault();
          e.stopPropagation();
          openDrawer();
          return;
        }
        if (cfg.openOnAddToCart && addSel) {
          const addEl = e.target.closest(addSel);
          if (addEl) {
            setTimeout(function () {
              refreshCart().then(function () {
                openDrawer();
              });
            }, 400);
          }
        }
      },
      true,
    );
  }

  function watchAjaxCart() {
    const origFetch = window.fetch;
    window.fetch = function () {
      const args = arguments;
      return origFetch.apply(this, args).then(function (res) {
        try {
          const url = String(args[0] || "");
          if (/\/cart\/(add|change|update|clear)/.test(url)) {
            res
              .clone()
              .json()
              .then(function () {
                refreshCart();
                if (state.config.behavior.openOnAddToCart && /\/cart\/add/.test(url)) {
                  openDrawer();
                }
              })
              .catch(function () {});
          }
        } catch (e) {}
        return res;
      });
    };
  }

  function buildDom() {
    const overlay = document.createElement("div");
    overlay.className = "upcard-overlay";
    overlay.hidden = true;
    overlay.addEventListener("click", closeDrawer);

    const drawer = document.createElement("div");
    drawer.className = "upcard-drawer";
    drawer.hidden = true;
    drawer.setAttribute("role", "dialog");
    drawer.setAttribute("aria-modal", "true");
    drawer.setAttribute("aria-label", "Cart");

    const sticky = document.createElement("button");
    sticky.type = "button";
    sticky.className = "upcard-sticky";
    sticky.innerHTML =
      '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M6 6h15l-1.5 9h-12z"/><circle cx="9" cy="20" r="1"/><circle cx="18" cy="20" r="1"/><path d="M6 6L5 2H2"/></svg><span class="upcard-sticky__count" hidden>0</span>';
    sticky.addEventListener("click", openDrawer);

    document.body.appendChild(overlay);
    document.body.appendChild(drawer);
    document.body.appendChild(sticky);

    els.overlay = overlay;
    els.drawer = drawer;
    els.sticky = sticky;
    els.stickyCount = sticky.querySelector(".upcard-sticky__count");

    // Inject custom scripts once
    if (state.config.customHtml && state.config.customHtml.scripts) {
      const wrap = document.createElement("div");
      wrap.innerHTML = state.config.customHtml.scripts;
      Array.from(wrap.querySelectorAll("script")).forEach(function (s) {
        const ns = document.createElement("script");
        if (s.src) ns.src = s.src;
        else ns.textContent = s.textContent;
        document.body.appendChild(ns);
      });
    }
  }

  function startTimerTick() {
    setInterval(function () {
      if (state.open && state.config.announcements && state.config.announcements.showTimer) {
        const el = els.drawer.querySelector(".upcard-announcement");
        if (el && state.config.announcements.text) {
          el.textContent = state.config.announcements.text.replace("{TIMER}", formatTimer());
        }
      }
    }, 1000);
  }

  async function init() {
    await loadConfig();
    buildDom();
    interceptClicks();
    watchAjaxCart();
    startTimerTick();
    try {
      await refreshCart();
    } catch (e) {
      updateStickyCount();
    }
    window.UpCard = {
      open: openDrawer,
      close: closeDrawer,
      refresh: refreshCart,
      getConfig: function () {
        return state.config;
      },
    };
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
