import { test, expect } from '@playwright/test';

test('home page has CASKr header and orders link', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveTitle(/Caskr Orders/);
  await expect(page.getByRole('link', { name: 'Orders' })).toBeVisible();
});
