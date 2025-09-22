import { test, expect } from '@playwright/test';

test.describe('global loading spinner', () => {
  test('appears while fetch requests are in flight', async ({ page }) => {
    await page.route('**/api/status', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([{ id: 1, name: 'New', statusTasks: [] }])
      })
    );

    await page.route('**/api/orders', async route => {
      if (route.request().method() === 'GET') {
        await new Promise(resolve => setTimeout(resolve, 500));
        await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
      } else {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
      }
    });

    await page.goto('/orders');

    const spinner = page.getByRole('status', { name: 'Loading' });
    await expect(spinner).toBeVisible();
    await expect(spinner).toBeHidden();
  });
});

