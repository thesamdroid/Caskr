import { test, expect } from '@playwright/test';

test('home page loads dashboard content', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveTitle(/Caskr Orders/);
  await expect(page.getByRole('heading', { name: 'Forecasting' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Dashboard' })).toBeVisible();
});
