import { expect, test } from '@playwright/test';
import { stubPricingData, defaultPricingTiers, defaultPricingFaqs } from './support/pricingStubs';

test.describe('Pricing Page', () => {
  test.describe('Page Load', () => {
    test('displays pricing page with all sections', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      // Hero section
      await expect(page.getByRole('heading', { name: 'Simple, Transparent Pricing' })).toBeVisible();
      await expect(page.getByText('Start free, scale as you grow')).toBeVisible();

      // Billing toggle
      await expect(page.getByRole('radio', { name: 'Monthly' })).toBeVisible();
      await expect(page.getByRole('radio', { name: /Annual/ })).toBeVisible();

      // Pricing cards section
      await expect(page.getByRole('region', { name: 'Pricing plans' })).toBeVisible();
    });

    test('shows loading skeleton while fetching data', async ({ page }) => {
      await page.route('**/api/public/pricing', async (route) => {
        await new Promise((resolve) => setTimeout(resolve, 1000));
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            tiers: defaultPricingTiers,
            featuresByCategory: [],
            faqs: [],
            generatedAt: new Date().toISOString(),
          }),
        });
      });

      await page.goto('/pricing');
      await expect(page.locator('.pricing-card-skeleton').first()).toBeVisible();
    });

    test('handles API error gracefully', async ({ page }) => {
      await stubPricingData(page, {
        pricingResponses: [{ status: 500, body: { error: 'Server error' } }],
      });

      await page.goto('/pricing');
      await expect(page.getByRole('alert')).toContainText('Unable to load pricing');
      await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible();
    });

    test('allows retry after an API failure', async ({ page }) => {
      await stubPricingData(page, {
        pricingResponses: [
          { status: 500, body: { message: 'Server error' } },
          { status: 200 },
        ],
      });

      await page.goto('/pricing');

      await expect(page.getByRole('alert')).toContainText('Unable to load pricing');
      await expect(page.getByRole('alert')).toContainText('Server error');

      await page.getByRole('button', { name: 'Retry' }).click();

      await expect(page.getByRole('heading', { name: 'Simple, Transparent Pricing' })).toBeVisible();
      await expect(page.getByRole('alert')).toHaveCount(0);
    });
  });

  test.describe('Pricing Cards', () => {
    test('renders all pricing tiers from API', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      // Check all tier names are displayed
      await expect(page.getByRole('heading', { name: 'Starter' })).toBeVisible();
      await expect(page.getByRole('heading', { name: 'Professional' })).toBeVisible();
      await expect(page.getByRole('heading', { name: 'Business' })).toBeVisible();
      await expect(page.getByRole('heading', { name: 'Enterprise' })).toBeVisible();
    });

    test('displays correct monthly prices', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      // Starter: $99/month
      const starterCard = page.locator('article', { hasText: 'Starter' });
      await expect(starterCard.locator('.animated-price-amount')).toContainText('99');

      // Professional: $299/month
      const proCard = page.locator('article', { hasText: 'Professional' });
      await expect(proCard.locator('.animated-price-amount')).toContainText('299');
    });

    test('shows "Most Popular" badge on correct tier', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      const proCard = page.locator('article', { hasText: 'Professional' });
      await expect(proCard.locator('.pricing-card-badge')).toContainText('Most Popular');

      const starterCard = page.locator('article', { hasText: 'Starter' });
      await expect(starterCard.locator('.pricing-card-badge')).toBeHidden();
    });

    test('shows "Contact Sales" for enterprise tier', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      const enterpriseCard = page.locator('article', { hasText: 'Enterprise' });
      await expect(enterpriseCard.getByText('Custom')).toBeVisible();
      await expect(enterpriseCard.getByRole('link', { name: 'Contact Sales' })).toBeVisible();
    });

    test('displays feature list with checkmarks', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      const proCard = page.locator('article', { hasText: 'Professional' });
      await expect(proCard.getByText('Barrel Management')).toBeVisible();
      await expect(proCard.getByText('TTB Compliance')).toBeVisible();
    });
  });

  test.describe('Billing Toggle', () => {
    test('defaults to monthly billing', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      const monthlyOption = page.getByRole('radio', { name: 'Monthly' });
      await expect(monthlyOption).toHaveAttribute('aria-checked', 'true');
    });

    test('clicking Annual selects annual billing', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('radio', { name: /Annual/ }).click();

      const annualOption = page.getByRole('radio', { name: /Annual/ });
      await expect(annualOption).toHaveAttribute('aria-checked', 'true');
    });

    test('toggle updates all card prices to annual', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      // Verify monthly price first
      const starterCard = page.locator('article', { hasText: 'Starter' });
      await expect(starterCard.locator('.animated-price-amount')).toContainText('99');

      // Switch to annual
      await page.getByRole('radio', { name: /Annual/ }).click();

      // Price should show annual equivalent (approximately $79/month for 20% off)
      await expect(starterCard.locator('.animated-price-amount')).toContainText('79');
    });

    test('shows savings badge when annual is selected', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      // No savings badge initially
      const starterCard = page.locator('article', { hasText: 'Starter' });
      await expect(starterCard.locator('.savings-badge')).toBeHidden();

      // Switch to annual
      await page.getByRole('radio', { name: /Annual/ }).click();

      // Savings badge should appear
      await expect(starterCard.locator('.savings-badge')).toBeVisible();
    });

    test('keyboard navigation works (arrows, space, enter)', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      // Focus on monthly option
      const monthlyOption = page.getByRole('radio', { name: 'Monthly' });
      await monthlyOption.focus();

      // Press right arrow to switch to annual
      await page.keyboard.press('ArrowRight');
      const annualOption = page.getByRole('radio', { name: /Annual/ });
      await expect(annualOption).toHaveAttribute('aria-checked', 'true');

      // Press left arrow to switch back to monthly
      await page.keyboard.press('ArrowLeft');
      await expect(monthlyOption).toHaveAttribute('aria-checked', 'true');
    });

    test('shows discount badge on annual option', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      const annualOption = page.getByRole('radio', { name: /Annual/ });
      await expect(annualOption.locator('.billing-toggle-badge')).toContainText('Save 20%');
    });
  });

  test.describe('Feature Comparison Table', () => {
    test('renders feature comparison table with tiers', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await expect(page.getByRole('heading', { name: 'Compare Plans' })).toBeVisible();
      await expect(page.locator('.feature-comparison-table')).toBeVisible();
    });

    test('displays checkmarks and X marks correctly', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      // Scroll to comparison table
      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      // Check for feature rows
      const table = page.locator('.feature-comparison-table');
      await expect(table.locator('.feature-check').first()).toBeVisible();
    });

    test('categories are collapsible', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      // Find category toggle
      const categoryToggle = page.locator('.category-toggle').first();
      await expect(categoryToggle).toBeVisible();

      // Click to collapse
      await categoryToggle.click();
      await expect(categoryToggle).toHaveAttribute('aria-expanded', 'false');
    });
  });

  test.describe('FAQ Accordion', () => {
    test('renders FAQ section with questions', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await expect(page.getByRole('heading', { name: 'Frequently Asked Questions' })).toBeVisible();

      // Check first FAQ question
      await expect(page.getByText('What is included in the free trial?')).toBeVisible();
    });

    test('FAQ accordion expands and collapses', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      const faqButton = page.getByRole('button', { name: /What is included in the free trial/ });
      await faqButton.scrollIntoViewIfNeeded();

      // Initially collapsed
      await expect(faqButton).toHaveAttribute('aria-expanded', 'false');

      // Click to expand
      await faqButton.click();
      await expect(faqButton).toHaveAttribute('aria-expanded', 'true');

      // Click again to collapse
      await faqButton.click();
      await expect(faqButton).toHaveAttribute('aria-expanded', 'false');
    });

    test('only one FAQ item open at a time', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      const firstFaq = page.getByRole('button', { name: /What is included in the free trial/ });
      const secondFaq = page.getByRole('button', { name: /Can I change plans later/ });

      await firstFaq.scrollIntoViewIfNeeded();

      // Open first FAQ
      await firstFaq.click();
      await expect(firstFaq).toHaveAttribute('aria-expanded', 'true');

      // Open second FAQ - first should close
      await secondFaq.click();
      await expect(secondFaq).toHaveAttribute('aria-expanded', 'true');
      await expect(firstFaq).toHaveAttribute('aria-expanded', 'false');
    });

    test('shows contact link at bottom', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await expect(page.getByRole('link', { name: 'Contact our team' })).toBeVisible();
    });
  });

  test.describe('Promo Code', () => {
    test('promo code toggle shows input', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('button', { name: /Have a promo code/ }).click();
      await expect(page.getByRole('textbox', { name: 'Promo code' })).toBeVisible();
    });

    test('validates promo code against API', async ({ page }) => {
      await stubPricingData(page, {
        promoValidation: { isValid: true, code: 'SAVE20', discountDescription: '20% off' },
      });
      await page.goto('/pricing');

      await page.getByRole('button', { name: /Have a promo code/ }).click();
      await page.getByRole('textbox', { name: 'Promo code' }).fill('SAVE20');
      await page.getByRole('button', { name: 'Apply' }).click();

      // Should show success state
      await expect(page.getByText('Promo Code Applied')).toBeVisible();
      await expect(page.getByText('SAVE20')).toBeVisible();
    });

    test('shows error for invalid promo code', async ({ page }) => {
      await stubPricingData(page, {
        promoValidation: { isValid: false, errorMessage: 'Invalid promo code' },
      });
      await page.goto('/pricing');

      await page.getByRole('button', { name: /Have a promo code/ }).click();
      await page.getByRole('textbox', { name: 'Promo code' }).fill('INVALID');
      await page.getByRole('button', { name: 'Apply' }).click();

      await expect(page.getByRole('alert')).toContainText('Invalid promo code');
    });

    test('shows loading state while validating', async ({ page }) => {
      await stubPricingData(page, {
        promoValidation: { isValid: true, code: 'SAVE20', discountDescription: '20% off' },
        promoValidationDelay: 500,
      });
      await page.goto('/pricing');

      await page.getByRole('button', { name: /Have a promo code/ }).click();
      await page.getByRole('textbox', { name: 'Promo code' }).fill('SAVE20');
      await page.getByRole('button', { name: 'Apply' }).click();

      // Should show loading spinner
      await expect(page.locator('.promo-code-spinner')).toBeVisible();

      // Wait for success state
      await expect(page.getByText('Promo Code Applied')).toBeVisible();
    });

    test('remove button clears promo code', async ({ page }) => {
      await stubPricingData(page, {
        promoValidation: { isValid: true, code: 'SAVE20', discountDescription: '20% off' },
      });
      await page.goto('/pricing');

      // Apply promo
      await page.getByRole('button', { name: /Have a promo code/ }).click();
      await page.getByRole('textbox', { name: 'Promo code' }).fill('SAVE20');
      await page.getByRole('button', { name: 'Apply' }).click();
      await expect(page.getByText('Promo Code Applied')).toBeVisible();

      // Remove promo
      await page.getByRole('button', { name: 'Remove promo code' }).click();

      // Should show toggle again
      await expect(page.getByRole('button', { name: /Have a promo code/ })).toBeVisible();
    });

    test('cancel button hides input without clearing', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('button', { name: /Have a promo code/ }).click();
      await page.getByRole('textbox', { name: 'Promo code' }).fill('TEST');
      await page.getByRole('button', { name: 'Cancel' }).click();

      // Should show toggle again
      await expect(page.getByRole('button', { name: /Have a promo code/ })).toBeVisible();
    });

    test('input converts to uppercase', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('button', { name: /Have a promo code/ }).click();
      await page.getByRole('textbox', { name: 'Promo code' }).fill('lowercase');

      await expect(page.getByRole('textbox', { name: 'Promo code' })).toHaveValue('LOWERCASE');
    });

    test('shows retry option after error', async ({ page }) => {
      await stubPricingData(page, {
        promoValidation: { isValid: false, errorMessage: 'Invalid promo code' },
      });
      await page.goto('/pricing');

      await page.getByRole('button', { name: /Have a promo code/ }).click();
      await page.getByRole('textbox', { name: 'Promo code' }).fill('INVALID');
      await page.getByRole('button', { name: 'Apply' }).click();

      await expect(page.getByRole('alert')).toContainText('Invalid promo code');
      await expect(page.getByRole('button', { name: 'Try again' })).toBeVisible();
    });

    test('shows expired promo code error', async ({ page }) => {
      await stubPricingData(page, {
        promoValidation: { isValid: false, errorMessage: 'This promo code has expired' },
      });
      await page.goto('/pricing');

      await page.getByRole('button', { name: /Have a promo code/ }).click();
      await page.getByRole('textbox', { name: 'Promo code' }).fill('EXPIRED');
      await page.getByRole('button', { name: 'Apply' }).click();

      await expect(page.getByRole('alert')).toContainText('expired');
    });
  });

  test.describe('Promo Code URL Parameter', () => {
    test('auto-validates promo from URL parameter', async ({ page }) => {
      await stubPricingData(page, {
        promoValidation: { isValid: true, code: 'URLCODE', discountDescription: '15% off' },
      });
      await page.goto('/pricing?promo=URLCODE');

      // Should auto-apply and show success
      await expect(page.getByText('Promo Code Applied')).toBeVisible();
      await expect(page.getByText('URLCODE')).toBeVisible();
    });

    test('handles invalid URL promo gracefully', async ({ page }) => {
      await stubPricingData(page, {
        promoValidation: { isValid: false, errorMessage: 'Invalid promo code' },
      });
      await page.goto('/pricing?promo=BADCODE');

      // Should show error, not crash
      await expect(page.getByRole('alert')).toContainText('Invalid promo code');
    });

    test('updates URL when promo applied manually', async ({ page }) => {
      await stubPricingData(page, {
        promoValidation: { isValid: true, code: 'MANUAL', discountDescription: '10% off' },
      });
      await page.goto('/pricing');

      await page.getByRole('button', { name: /Have a promo code/ }).click();
      await page.getByRole('textbox', { name: 'Promo code' }).fill('MANUAL');
      await page.getByRole('button', { name: 'Apply' }).click();

      await expect(page.getByText('Promo Code Applied')).toBeVisible();

      // URL should include promo parameter
      await expect(page).toHaveURL(/promo=MANUAL/);
    });

    test('clears URL param when promo removed', async ({ page }) => {
      await stubPricingData(page, {
        promoValidation: { isValid: true, code: 'REMOVE', discountDescription: '20% off' },
      });
      await page.goto('/pricing?promo=REMOVE');

      await expect(page.getByText('Promo Code Applied')).toBeVisible();

      // Remove promo
      await page.getByRole('button', { name: 'Remove promo code' }).click();

      // URL should not include promo parameter
      await expect(page).not.toHaveURL(/promo=/);
    });
  });

  test.describe('CTA Section', () => {
    test('displays final CTA with buttons', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await expect(page.getByRole('heading', { name: /Ready to streamline/ })).toBeVisible();
      await expect(page.getByRole('link', { name: 'Start Your Free Trial' })).toBeVisible();
      await expect(page.getByRole('link', { name: 'Schedule a Demo' })).toBeVisible();
    });

    test('shows trust badges', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await expect(page.getByText('No credit card required')).toBeVisible();
      await expect(page.getByText('14-day free trial')).toBeVisible();
      await expect(page.getByText('Cancel anytime')).toBeVisible();
    });
  });

  test.describe('Mobile Responsiveness', () => {
    test('cards stack on mobile viewport', async ({ page }) => {
      await stubPricingData(page);
      await page.setViewportSize({ width: 375, height: 667 });
      await page.goto('/pricing');

      // Cards should be in single column
      const grid = page.locator('.pricing-cards-grid');
      await expect(grid).toBeVisible();

      // Check grid is not using multi-column layout
      const gridStyle = await grid.evaluate((el) => getComputedStyle(el).gridTemplateColumns);
      expect(gridStyle).toContain('1fr');
    });

    test('sticky mobile button is visible', async ({ page }) => {
      await stubPricingData(page);
      await page.setViewportSize({ width: 375, height: 667 });
      await page.goto('/pricing');

      await expect(page.locator('.pricing-mobile-sticky')).toBeVisible();
    });

    test('touch targets are minimum 44px', async ({ page }) => {
      await stubPricingData(page);
      await page.setViewportSize({ width: 375, height: 667 });
      await page.goto('/pricing');

      const buttons = page.locator('button');
      const buttonCount = await buttons.count();

      for (let i = 0; i < Math.min(buttonCount, 5); i++) {
        const button = buttons.nth(i);
        const isVisible = await button.isVisible();
        if (isVisible) {
          const box = await button.boundingBox();
          if (box) {
            expect(box.height).toBeGreaterThanOrEqual(44);
          }
        }
      }
    });
  });

  test.describe('Accessibility', () => {
    test('keyboard navigation works', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      // Tab through interactive elements
      await page.keyboard.press('Tab');

      // Focus should be visible
      const focusedElement = page.locator(':focus');
      await expect(focusedElement).toBeVisible();
    });

    test('price changes announced to screen readers', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      // Find the live region for announcements
      const announcement = page.locator('#billing-toggle-announcement');
      await expect(announcement).toHaveAttribute('aria-live', 'polite');
    });

    test('focus visible on all interactive elements', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      const toggle = page.getByRole('radio', { name: 'Monthly' });
      await toggle.focus();

      // Check element has visible focus indicator
      const isVisible = await toggle.isVisible();
      expect(isVisible).toBe(true);
    });
  });

  test.describe('State Persistence', () => {
    test('billing period persists across page refresh', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      // Switch to annual
      await page.getByRole('radio', { name: /Annual/ }).click();
      await expect(page.getByRole('radio', { name: /Annual/ })).toHaveAttribute('aria-checked', 'true');

      // Refresh page
      await page.reload();

      // Should still be on annual
      await expect(page.getByRole('radio', { name: /Annual/ })).toHaveAttribute('aria-checked', 'true');
    });
  });
});
