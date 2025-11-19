import { test, expect, Page } from '@playwright/test';

function uniqueEmail() {
  const n = Date.now();
  return `dash_${n}@example.com`;
}

async function registerUser(page: Page) {
  await page.goto('/', { waitUntil: 'domcontentloaded' });
  await Promise.all([
    page.waitForURL('**/Account/Register*', { waitUntil: 'domcontentloaded' }),
    page.getByRole('link', { name: 'Register', exact: true }).click()
  ]);

  const email = uniqueEmail();
  await page.locator('input[name="Input.Email"]').fill(email);
  await page.locator('input[name="Input.Password"]').fill('P@ssword1!');
  await page.locator('input[name="Input.ConfirmPassword"]').fill('P@ssword1!');

  await page.getByRole('button', { name: /register|sign up/i }).first().click();
  await page.waitForLoadState('domcontentloaded');

  return email;
}

test.describe('Blazor dashboard trade', () => {
  test.setTimeout(120_000);

  test('new account can deposit cash and place a market order', async ({ page }) => {
    await registerUser(page);

    await page.goto('/dashboard', { waitUntil: 'networkidle' });
    await expect(page.getByText('Portfolio Dashboard')).toBeVisible();

    const depositInput = page.getByLabel('Deposit', { exact: true });
    await depositInput.fill('500');
    await page.getByRole('button', { name: /^Deposit$/i }).click();
    await expect(page.locator('.account-panel')).toContainText(/Cash:\s*500/, { timeout: 15000 });

    await page.getByLabel('Symbol').fill('MSFT');
    await page.getByLabel('Quantity').fill('1');
    await page.getByRole('button', { name: /Place Order/i }).click();

    await expect(page.locator('.orders-panel')).toContainText(/MSFT/, { timeout: 15000 });
    await expect(page.locator('.orders-panel')).toContainText(/Accepted|PartiallyFilled|Filled/i);
  });
});
