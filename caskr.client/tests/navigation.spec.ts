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
    { link: 'Dashboard', heading: 'Forecasting', path: '/' },
    { link: 'Orders', heading: 'Orders', path: '/orders' },
    { link: 'Barrels', heading: 'Barrels', path: '/barrels' },
    { link: 'Products', heading: 'Products', path: '/products' },
    { link: 'Login', heading: 'Login', path: '/login' }
  ];

  test('each navigation link leads to correct page', async ({ page }) => {
    for (const p of pages) {
      await page.goto(p.path);
      await page.getByRole('link', { name: p.link }).click();
      await expect(page.getByRole('heading', { name: p.heading })).toBeVisible();
    }
  });

  test('navigation does not include restricted links', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('link', { name: 'Statuses' })).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Users' })).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'User Types' })).toHaveCount(0);
  });
});
