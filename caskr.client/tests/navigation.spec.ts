import { test, expect } from '@playwright/test';

test.describe('navigation links', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/**', async route => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
      } else {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
      }
    });
  });

  const pages = [
    { link: 'Orders', heading: 'Orders' },
    { link: 'Barrels', heading: 'Barrels' },
    { link: 'Products', heading: 'Products' },
    { link: 'Statuses', heading: 'Statuses' },
    { link: 'Users', heading: 'Users' },
    { link: 'User Types', heading: 'User Types' },
    { link: 'Login', heading: 'Login' }
  ];

  test('each navigation link leads to correct page', async ({ page }) => {
    await page.goto('/orders');
    for (const p of pages) {
      await page.getByRole('link', { name: p.link }).click();
      await expect(page.getByRole('heading', { name: p.heading })).toBeVisible();
    }
  });
});
