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
    await page.getByLabel('Order Name').fill('Test Order');
    await page.getByLabel('Order Status').selectOption('1');
    await page.click('button[type="submit"]');
    await expect(page.getByText('Test Order')).toBeVisible();
  });

  test('falls back to default badge when status name missing', async ({ page }) => {
    await page.route('**/api/status', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([])
      })
    );

    await page.route('**/api/orders', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 42,
            name: 'Order Without Status',
            statusId: 99,
            ownerId: 1,
            spiritTypeId: 1,
            quantity: 1,
            mashBillId: 1
          }
        ])
      })
    );

    await page.goto('/orders');
    const statusBadge = page.locator('.status-badge').first();
    await expect(statusBadge).toContainText('99');
    await expect(statusBadge).toHaveClass(/status-badge\s+default/);
  });
});

test.describe('order accessibility affordances', () => {
  test('rows expose descriptive labels for assistive technology', async ({ page }) => {
    await page.route('**/api/status', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([{ id: 1, name: 'New', statusTasks: [] }])
      })
    );

    await page.route('**/api/orders', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 7, name: 'Barrel Shipment', statusId: 1, ownerId: 1, spiritTypeId: 1, quantity: 1, mashBillId: 1 }
        ])
      })
    );

    await page.goto('/orders');

    await expect(
      page.getByRole('button', { name: 'View details for order Barrel Shipment' })
    ).toBeVisible();
  });
});
