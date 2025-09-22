import { test, expect } from '@playwright/test';

test.describe('navigation links', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**', async route => {
      const url = new URL(route.request().url());
      if (!url.pathname.startsWith('/api/')) {
        await route.continue();
        return;
      }

      if (route.request().method() === 'GET') {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
      } else {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
      }
    });
  });

  const pages = [
    { link: 'Orders', heading: 'Orders', path: '/orders' },
    { link: 'Barrels', heading: 'Barrels', path: '/barrels' },
    { link: 'Products', heading: 'Products', path: '/products' },
    { link: 'Statuses', heading: 'Statuses', path: '/statuses' },
    { link: 'Users', heading: 'Users', path: '/users' },
    { link: 'User Types', heading: 'User Types', path: '/usertypes' },
    { link: 'Login', heading: 'Login', path: '/login' }
  ];

  test('each navigation link leads to correct page', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    await page.waitForSelector('.nav-menu a');
    for (const p of pages) {
      const link = page.locator(`a[href="${p.path}"]`).first();
      await expect(link).toBeVisible();
      await link.click();
      await expect(page.getByRole('heading', { name: p.heading })).toBeVisible();
    }
  });
});
