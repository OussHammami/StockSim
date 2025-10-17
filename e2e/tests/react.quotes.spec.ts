import { test, expect } from '@playwright/test';

test.describe('React quotes', () => {
  test('charts page loads', async ({ page }) => {
    const resp = await page.goto('http://localhost:8082/charts');
    expect(resp?.ok()).toBeTruthy();
    // Basic smoke: title or header exists (adjust text to your app)
    await expect(page).toHaveTitle(/stocksim|react|charts/i);
  });
});