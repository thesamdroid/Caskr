import { test, expect } from '@playwright/test';

test('barrels forecasting modal is only available on barrels page', async ({ page }) => {
  await page.route('**/api/barrels/company/1', async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
  });

  await page.goto('/');
  await expect(page.getByRole('button', { name: 'Forecasting' })).toHaveCount(0);

  await page.goto('/barrels');
  const forecastingButton = page.getByRole('button', { name: 'Forecasting' });
  await expect(forecastingButton).toBeVisible();

  await forecastingButton.click();
  await expect(page.getByRole('heading', { name: 'Forecast Barrels' })).toBeVisible();

  await page.getByRole('button', { name: 'Cancel' }).click();
  await expect(page.getByRole('heading', { name: 'Forecast Barrels' })).toHaveCount(0);
});
