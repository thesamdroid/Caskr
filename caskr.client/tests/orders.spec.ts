import { test, expect } from '@playwright/test';

test.describe('orders page', () => {
  test('can add a new order', async ({ page }) => {
    await page.route('**/api/status', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([{ id: 1, name: 'New', statusTasks: [] }])
      })
    );
    await page.route('**/api/orders', async route => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
      } else if (route.request().method() === 'POST') {
        const newOrder = {
          id: 1,
          name: 'Test Order',
          statusId: 1,
          ownerId: 1,
          spiritTypeId: 1,
          quantity: 1,
          mashBillId: 1
        };
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(newOrder)
        });
      } else {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
      }
    });

    await page.goto('/orders');
    await page.fill('input[placeholder="Name"]', 'Test Order');
    await page.selectOption('select', '1');
    await page.click('button[type="submit"]');
    await expect(page.getByText('Test Order')).toBeVisible();
  });
});
