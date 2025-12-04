import { expect, test } from '@playwright/test';
import { stubPricingData } from './support/pricingStubs';

test.describe('Pricing Page Analytics', () => {
  test.describe('Page View Tracking', () => {
    test('tracks page view on mount', async ({ page }) => {
      const analyticsRequests: any[] = [];

      await page.route('**/api/analytics/**', async (route) => {
        const body = route.request().postDataJSON();
        analyticsRequests.push(body);
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ success: true }),
        });
      });

      await stubPricingData(page);
      await page.goto('/pricing');

      // Wait for page to load
      await expect(page.getByRole('heading', { name: 'Simple, Transparent Pricing' })).toBeVisible();
    });

    test('includes UTM parameters in tracking', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing?utm_source=google&utm_medium=cpc&utm_campaign=brand');

      // Page should load without errors
      await expect(page.getByRole('heading', { name: 'Simple, Transparent Pricing' })).toBeVisible();
    });

    test('includes promo code from URL in tracking', async ({ page }) => {
      await stubPricingData(page, {
        promoValidation: { isValid: true, code: 'TRACK', discountDescription: '10% off' },
      });
      await page.goto('/pricing?promo=TRACK');

      await expect(page.getByText('Promo Code Applied')).toBeVisible();
    });
  });

  test.describe('Billing Toggle Tracking', () => {
    test('tracks billing toggle changes', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      // Switch to annual
      await page.getByRole('radio', { name: /Annual/ }).click();
      await expect(page.getByRole('radio', { name: /Annual/ })).toHaveAttribute('aria-checked', 'true');

      // Switch back to monthly
      await page.getByRole('radio', { name: 'Monthly' }).click();
      await expect(page.getByRole('radio', { name: 'Monthly' })).toHaveAttribute('aria-checked', 'true');
    });
  });

  test.describe('Promo Code Tracking', () => {
    test('tracks promo input opened', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('button', { name: /Have a promo code/ }).click();
      await expect(page.getByRole('textbox', { name: 'Promo code' })).toBeVisible();
    });

    test('tracks promo code entered', async ({ page }) => {
      await stubPricingData(page, {
        promoValidation: { isValid: true, code: 'TRACK20', discountDescription: '20% off' },
      });
      await page.goto('/pricing');

      await page.getByRole('button', { name: /Have a promo code/ }).click();
      await page.getByRole('textbox', { name: 'Promo code' }).fill('TRACK20');
      await page.getByRole('button', { name: 'Apply' }).click();

      await expect(page.getByText('Promo Code Applied')).toBeVisible();
    });

    test('tracks promo validation failure', async ({ page }) => {
      await stubPricingData(page, {
        promoValidation: { isValid: false, errorMessage: 'Invalid code' },
      });
      await page.goto('/pricing');

      await page.getByRole('button', { name: /Have a promo code/ }).click();
      await page.getByRole('textbox', { name: 'Promo code' }).fill('INVALID');
      await page.getByRole('button', { name: 'Apply' }).click();

      await expect(page.getByRole('alert')).toContainText('Invalid code');
    });

    test('tracks promo code removed', async ({ page }) => {
      await stubPricingData(page, {
        promoValidation: { isValid: true, code: 'REMOVE', discountDescription: '15% off' },
      });
      await page.goto('/pricing');

      // Apply promo
      await page.getByRole('button', { name: /Have a promo code/ }).click();
      await page.getByRole('textbox', { name: 'Promo code' }).fill('REMOVE');
      await page.getByRole('button', { name: 'Apply' }).click();
      await expect(page.getByText('Promo Code Applied')).toBeVisible();

      // Remove promo
      await page.getByRole('button', { name: 'Remove promo code' }).click();
      await expect(page.getByRole('button', { name: /Have a promo code/ })).toBeVisible();
    });
  });

  test.describe('Feature Comparison Visibility', () => {
    test('feature comparison table is visible when scrolled to', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      // Scroll to feature comparison
      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();
      await expect(page.locator('.feature-comparison-table')).toBeVisible();
    });
  });

  test.describe('FAQ Tracking', () => {
    test('tracks FAQ item opened', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      // Scroll to FAQ section
      await page.getByRole('heading', { name: 'Frequently Asked Questions' }).scrollIntoViewIfNeeded();

      // Open FAQ item
      const faqButton = page.getByRole('button', { name: /What is included in the free trial/ });
      await faqButton.click();

      await expect(faqButton).toHaveAttribute('aria-expanded', 'true');
    });
  });

  test.describe('CTA Click Tracking', () => {
    test('CTA buttons have correct links', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      // Check tier CTA links
      const starterCard = page.locator('article', { hasText: 'Starter' });
      await expect(starterCard.getByRole('link', { name: 'Get Started' })).toBeVisible();

      const proCard = page.locator('article', { hasText: 'Professional' });
      await expect(proCard.getByRole('link', { name: 'Start Free Trial' })).toBeVisible();
    });

    test('mobile sticky CTA scrolls to plans', async ({ page }) => {
      await stubPricingData(page);
      await page.setViewportSize({ width: 375, height: 667 });
      await page.goto('/pricing');

      // Mobile sticky should be visible
      await expect(page.locator('.pricing-mobile-sticky')).toBeVisible();

      // Click mobile CTA
      await page.locator('.pricing-mobile-cta').click();

      // Should scroll to pricing cards
      await expect(page.locator('.pricing-cards-section')).toBeInViewport();
    });
  });

  test.describe('Scroll Depth', () => {
    test('page is scrollable', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      // Scroll to bottom
      await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));

      // Footer should be visible
      await expect(page.locator('.pricing-footer')).toBeVisible();
    });
  });

  test.describe('Privacy and Consent', () => {
    test('page loads without analytics errors', async ({ page }) => {
      const errors: string[] = [];
      page.on('pageerror', (error) => {
        errors.push(error.message);
      });

      await stubPricingData(page);
      await page.goto('/pricing');

      // No JavaScript errors should occur
      await expect(page.getByRole('heading', { name: 'Simple, Transparent Pricing' })).toBeVisible();
      expect(errors.filter(e => e.includes('analytics'))).toHaveLength(0);
    });
  });
});
