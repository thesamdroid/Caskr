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
    await expect(page.getByText('Invalid email or password')).toBeVisible();
  });

  test('stores token and redirects on successful login', async ({ page }) => {
    const loginResponse = {
      token: 'fake-token',
      refreshToken: 'fake-refresh',
      expiresAt: new Date().toISOString(),
      user: {
        id: 1,
        name: 'Test User',
        email: 'test@example.com',
        companyId: 10,
        companyName: 'Test Co',
        userTypeId: 2,
        role: 'Compliance Manager',
        permissions: ['TTB_COMPLIANCE']
      }
    };

    await page.route('**/api/auth/login', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(loginResponse)
      })
    );
    await page.goto('/login');
    await page.getByLabel('Email Address').fill('test@example.com');
    await page.getByLabel('Password').fill('hunter2');
    await page.click('button[type="submit"]');
    await expect(page).toHaveURL('/');
    const token = await page.evaluate(() => localStorage.getItem('token'));
    expect(token).toBe('fake-token');
    const user = await page.evaluate(() => localStorage.getItem('auth.user'));
    expect(user).toContain('TTB_COMPLIANCE');
  });
});
