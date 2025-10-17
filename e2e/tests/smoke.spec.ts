import { test, expect } from '@playwright/test';

test.describe('Compose stack smoke', () => {
  test('Blazor app responds', async ({ page }) => {
    const resp = await page.goto('http://localhost:8080');
    expect(resp?.ok()).toBeTruthy();
  });

  test('React app responds and has title', async ({ page }) => {
    const resp = await page.goto('http://localhost:8082/charts');
    expect(resp?.ok()).toBeTruthy();
    await expect(page).toHaveTitle(/stocksim-react/i);
  });

  test('MarketFeed health', async ({ request }) => {
    const r = await request.get('http://localhost:8081/healthz');
    expect(r.ok()).toBeTruthy();
  });
});