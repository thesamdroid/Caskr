import { test, expect } from '@playwright/test';

test.describe('login', () => {
  test('shows error on failed login', async ({ page }) => {
    await page.route('**/api/auth/login', route =>
      route.fulfill({ status: 401, contentType: 'application/json', body: '{}' })
    );
    await page.goto('/login');
    await page.getByLabel('Email Address').fill('test@example.com');
    await page.getByLabel('Password').fill('hunter2');
    await page.click('button[type="submit"]');
    await expect(page.getByText('Login failed')).toBeVisible();
  });

  test('stores token and redirects on successful login', async ({ page }) => {
    await page.route('**/api/auth/login', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ token: 'fake-token' })
      })
    );
    await page.goto('/login');
    await page.getByLabel('Email Address').fill('test@example.com');
    await page.getByLabel('Password').fill('hunter2');
    await page.click('button[type="submit"]');
    await expect(page).toHaveURL('/');
    const token = await page.evaluate(() => localStorage.getItem('token'));
    expect(token).toBe('fake-token');
  });
});
