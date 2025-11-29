import { test, expect } from '@playwright/test';
import { seedAuthenticatedUser } from './support/auth';

test.describe('navigation links', () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthenticatedUser(page);
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
    { link: 'Dashboard', heading: 'Dashboard', path: '/' },
    { link: 'Orders', heading: 'Orders', path: '/orders' },
    { link: 'Barrels', heading: 'Barrels', path: '/barrels' },
    { link: 'TTB Compliance', heading: 'TTB Monthly Reports (Form 5110.28)', path: '/ttb-reports' },
    { link: 'Products', heading: 'Products', path: '/products' },
    { link: 'Accounting', heading: 'Accounting Settings', path: '/accounting' },
    { link: 'Sync History', heading: 'Accounting Sync History', path: '/accounting/sync-history' },
    { link: 'Login', heading: 'Welcome to CASKr', path: '/login' }
  ];

  test('each navigation link leads to correct page', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    await page.waitForSelector('.nav-menu a');
    for (const p of pages) {
      const link = page.locator(`a[href="${p.path}"]`).first();
      await expect(link).toBeVisible();
      await link.click();
      await expect(
        page.getByRole('heading', { name: p.heading, exact: true })
      ).toBeVisible();
    }
  });

  test('navigation does not include restricted links', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('link', { name: 'Statuses' })).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Users' })).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'User Types' })).toHaveCount(0);
  });

  test('hides TTB compliance navigation when permission is missing', async ({ page }) => {
    await page.addInitScript(() => localStorage.clear());
    await seedAuthenticatedUser(page, { permissions: [] });
    await page.goto('/');
    await expect(page.getByRole('link', { name: 'TTB Compliance' })).toHaveCount(0);

    await page.goto('/ttb-reports');
    await expect(page).toHaveURL('/');
  });
});
