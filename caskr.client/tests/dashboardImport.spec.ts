import { test, expect } from '@playwright/test';

test('dashboard shows import barrells button and removes it from barrels page', async ({ page }) => {
  await page.goto('/');
  const importButton = page.getByRole('button', { name: 'Import Barrells' });
  await expect(importButton).toBeVisible();

  await importButton.click();
  await expect(page.getByRole('heading', { name: 'Import Barrels' })).toBeVisible();
  await page.getByRole('button', { name: 'Cancel' }).click();
  await expect(page.getByRole('heading', { name: 'Import Barrels' })).toHaveCount(0);

  await page.goto('/barrels');
  await expect(page.getByRole('button', { name: 'Import Barrells' })).toHaveCount(0);
});
