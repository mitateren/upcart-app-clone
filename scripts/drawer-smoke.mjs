export default async function (page, ui) {
  await page.goto("http://localhost:4173/scripts/drawer-smoke.html", {
    waitUntil: "networkidle",
    timeout: 60000,
  });
  await page.waitForSelector(".upcard-sticky", { timeout: 15000 });
  await page.waitForFunction(() => !!window.UpCard?.open, { timeout: 15000 });
  await page.evaluate(() => window.UpCard.open());
  await page.waitForSelector(".upcard-drawer.is-open", { timeout: 10000 });
  await page.waitForTimeout(500);

  const result = await page.evaluate(() => {
    const title = document.querySelector(".upcard-header__title");
    const reward = document.querySelector(".upcard-rewards");
    const item = document.querySelector(".upcard-item");
    const announcement = document.querySelector(".upcard-announcement");
    const sticky = document.querySelector(".upcard-sticky");
    const checkout = document.querySelector("[data-upcard-checkout]");
    return {
      api: !!window.UpCard?.open,
      sticky: !!sticky,
      open: !!document.querySelector(".upcard-drawer.is-open"),
      title: title?.textContent?.trim() || "",
      rewards: !!reward,
      item: !!item,
      announcement: !!announcement,
      checkout: !!checkout,
      itemText: item?.textContent?.slice(0, 80) || "",
    };
  });

  // Close via continue shopping / close
  await page.click("[data-upcard-close]");
  await page.waitForTimeout(400);
  result.closed = !(await page.locator(".upcard-drawer.is-open").count());

  // Re-open via sticky
  await page.click(".upcard-sticky");
  await page.waitForSelector(".upcard-drawer.is-open", { timeout: 5000 });
  result.reopenedViaSticky = true;

  await page.screenshot({
    path: "C:/wwwroot/shopifyApp_UpCard/scripts/drawer-smoke.png",
    fullPage: true,
  });

  return result;
}
