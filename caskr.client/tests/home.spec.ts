import { test, expect } from '@playwright/test';

test('home page loads dashboard content', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveTitle(/Caskr Orders/);
  await expect(page.getByRole('heading', { level: 1, name: 'Dashboard' })).toBeVisible();
  await expect(page.getByRole('heading', { level: 2, name: 'Current Orders' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Dashboard' })).toBeVisible();
});
