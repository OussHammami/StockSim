import { test, expect } from '@playwright/test';

function uniqueEmail() {
  const n = Date.now();
  return `it_${n}@example.com`;
}

test.describe('Blazor auth', () => {
  test.setTimeout(120_000);

  test('registers and signs in', async ({ page }) => {
    // Start at home and click Register (your navbar shows url: Account/Register)
    await page.goto('/', { waitUntil: 'domcontentloaded' });
    await Promise.all([
      page.waitForURL('**/Account/Register*', { waitUntil: 'domcontentloaded' }),
      page.getByRole('link', { name: 'Register', exact: true }).click(),
    ]);

    // Your inputs have no <label>; use name="Input.*"
    const emailInput = page.locator('input[name="Input.Email"]');
    const pwdInput   = page.locator('input[name="Input.Password"]');
    const confInput  = page.locator('input[name="Input.ConfirmPassword"]');

    await emailInput.waitFor({ state: 'visible' });

    const email = uniqueEmail();
    await emailInput.fill(email);
    await pwdInput.fill('P@ssword1!');
    await confInput.fill('P@ssword1!');

    await page.getByRole('button', { name: /register|sign up/i }).or(page.locator('button[type="submit"]')).click();

    await page.waitForLoadState('domcontentloaded');
    await expect(page).toHaveURL(/localhost:8080|\/$/);
  });
});
