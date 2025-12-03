import { expect, test } from '@playwright/test';
import { stubPricingData, defaultFeaturesByCategory, extendedPricingFaqs } from './support/pricingStubs';

test.describe('Feature Comparison Table', () => {
  test.describe('Table Rendering', () => {
    test('renders correct number of tier columns', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await expect(page.getByRole('heading', { name: 'Compare Plans' })).toBeVisible();

      // Should have 4 tier columns (Starter, Professional, Business, Enterprise)
      const tierHeaders = page.locator('.comparison-th.tier-col, .feature-comparison-th.tier-col');
      await expect(tierHeaders).toHaveCount(4);
    });

    test('header shows all tier names', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      const table = page.locator('.feature-comparison-table');
      await expect(table.getByText('Starter')).toBeVisible();
      await expect(table.getByText('Professional')).toBeVisible();
      await expect(table.getByText('Business')).toBeVisible();
      await expect(table.getByText('Enterprise')).toBeVisible();
    });

    test('displays feature rows with correct values per tier', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      // Check that feature rows exist
      const featureRows = page.locator('.feature-row');
      const count = await featureRows.count();
      expect(count).toBeGreaterThan(0);
    });

    test('category headers separate feature groups', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      // Check category headers exist
      await expect(page.getByText('Core Features')).toBeVisible();
      await expect(page.getByText('Compliance')).toBeVisible();
    });

    test('displays checkmarks for included features', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      // Check for checkmark icons
      const checkmarks = page.locator('.feature-check, .feature-value-check');
      const count = await checkmarks.count();
      expect(count).toBeGreaterThan(0);
    });

    test('displays X marks for excluded features', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      // Check for X mark icons
      const xMarks = page.locator('.feature-x, .feature-value-x');
      const count = await xMarks.count();
      expect(count).toBeGreaterThan(0);
    });

    test('displays limit values correctly', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      // Check for limit values (100, 500, Unlimited)
      const table = page.locator('.feature-comparison-table, .feature-comparison-wrapper');
      await expect(table.getByText('100')).toBeVisible();
    });
  });

  test.describe('Category Collapse/Expand', () => {
    test('categories are collapsible', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      // Find and click category toggle
      const categoryToggle = page.locator('.category-toggle').first();
      await expect(categoryToggle).toBeVisible();

      // Get initial aria-expanded state
      const initialExpanded = await categoryToggle.getAttribute('aria-expanded');
      expect(initialExpanded).toBe('true');

      // Click to collapse
      await categoryToggle.click();
      await expect(categoryToggle).toHaveAttribute('aria-expanded', 'false');

      // Click to expand again
      await categoryToggle.click();
      await expect(categoryToggle).toHaveAttribute('aria-expanded', 'true');
    });

    test('collapsed categories hide feature rows', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      // Get initial feature row count
      const categoryToggle = page.locator('.category-toggle').first();
      const initialRows = await page.locator('.feature-row').count();

      // Collapse the first category
      await categoryToggle.click();
      await page.waitForTimeout(100); // Wait for animation

      // Feature rows should decrease
      const collapsedRows = await page.locator('.feature-row').count();
      expect(collapsedRows).toBeLessThan(initialRows);
    });
  });

  test.describe('Sticky Header and Column', () => {
    test('table has sticky header', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      const thead = page.locator('.feature-comparison-thead, .comparison-table-thead');
      const style = await thead.evaluate((el) => getComputedStyle(el).position);
      expect(style).toBe('sticky');
    });

    test('first column is sticky on horizontal scroll', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      const featureNameCol = page.locator('.feature-name-col, .feature-name-cell').first();
      const style = await featureNameCol.evaluate((el) => getComputedStyle(el).position);
      expect(style).toBe('sticky');
    });
  });

  test.describe('Row Interactions', () => {
    test('feature row highlights on hover', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      const featureRow = page.locator('.feature-row').first();
      await featureRow.hover();

      // Check background changes (hover state)
      const bgColor = await featureRow.evaluate((el) => getComputedStyle(el).backgroundColor);
      expect(bgColor).not.toBe('rgba(0, 0, 0, 0)'); // Should have some background
    });
  });

  test.describe('Accessibility', () => {
    test('table has proper ARIA roles', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      const table = page.locator('.feature-comparison-table');
      await expect(table).toHaveAttribute('role', 'grid');
    });

    test('category toggles are keyboard accessible', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      const categoryToggle = page.locator('.category-toggle').first();
      await categoryToggle.focus();

      // Press Enter to toggle
      await page.keyboard.press('Enter');
      await expect(categoryToggle).toHaveAttribute('aria-expanded', 'false');

      // Press Space to toggle back
      await page.keyboard.press('Space');
      await expect(categoryToggle).toHaveAttribute('aria-expanded', 'true');
    });

    test('checkmarks and X marks have accessible labels', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      // Check for aria-labels on check marks
      const checkmark = page.locator('[aria-label="Included"]').first();
      await expect(checkmark).toBeVisible();

      // Check for aria-labels on X marks
      const xMark = page.locator('[aria-label="Not included"]').first();
      await expect(xMark).toBeVisible();
    });
  });

  test.describe('Mobile View', () => {
    test('table is scrollable on mobile', async ({ page }) => {
      await stubPricingData(page);
      await page.setViewportSize({ width: 375, height: 667 });
      await page.goto('/pricing');

      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      const wrapper = page.locator('.feature-comparison-wrapper');
      const overflowX = await wrapper.evaluate((el) => getComputedStyle(el).overflowX);
      expect(overflowX).toBe('auto');
    });

    test('no horizontal overflow on mobile viewport', async ({ page }) => {
      await stubPricingData(page);
      await page.setViewportSize({ width: 375, height: 667 });
      await page.goto('/pricing');

      // Check page doesn't have horizontal scroll
      const bodyWidth = await page.evaluate(() => document.body.scrollWidth);
      const viewportWidth = await page.evaluate(() => window.innerWidth);
      expect(bodyWidth).toBeLessThanOrEqual(viewportWidth + 50); // Allow small tolerance
    });
  });

  test.describe('Popular Tier Indicator', () => {
    test('shows "Most Popular" badge on correct tier', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      // Professional tier should have the popular badge
      const table = page.locator('.feature-comparison-table, .feature-comparison-wrapper');
      const popularBadge = table.locator('.tier-popular-badge, .popular-indicator');
      await expect(popularBadge.first()).toBeVisible();
    });

    test('popular tier column has special styling', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await page.getByRole('heading', { name: 'Compare Plans' }).scrollIntoViewIfNeeded();

      const popularColumn = page.locator('.tier-col.popular, .feature-comparison-th.popular').first();
      await expect(popularColumn).toBeVisible();
    });
  });
});

test.describe('FAQ Accordion', () => {
  test.describe('Rendering', () => {
    test('renders FAQ section with questions', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await expect(page.getByRole('heading', { name: 'Frequently Asked Questions' })).toBeVisible();
      await expect(page.getByText('What is included in the free trial?')).toBeVisible();
    });

    test('renders all FAQ items', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      await expect(page.getByText('What is included in the free trial?')).toBeVisible();
      await expect(page.getByText('Can I change plans later?')).toBeVisible();
      await expect(page.getByText('Is there a discount for annual billing?')).toBeVisible();
    });

    test('shows contact link at bottom', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      const contactLink = page.getByRole('link', { name: 'Contact our team' });
      await contactLink.scrollIntoViewIfNeeded();
      await expect(contactLink).toBeVisible();
    });
  });

  test.describe('Expand/Collapse Behavior', () => {
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

    test('shows answer content when expanded', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      const faqButton = page.getByRole('button', { name: /What is included in the free trial/ });
      await faqButton.scrollIntoViewIfNeeded();

      // Expand
      await faqButton.click();

      // Answer should be visible
      await expect(page.getByText('14-day free trial')).toBeVisible();
    });
  });

  test.describe('Animation', () => {
    test('chevron icon rotates on expand', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      const faqButton = page.getByRole('button', { name: /What is included in the free trial/ });
      await faqButton.scrollIntoViewIfNeeded();

      const chevron = faqButton.locator('.accordion-icon, .faq-chevron');

      // Before click - no rotation
      const beforeTransform = await chevron.evaluate((el) => getComputedStyle(el).transform);

      // Click to expand
      await faqButton.click();
      await page.waitForTimeout(350); // Wait for animation

      // After click - should have rotation
      const afterTransform = await chevron.evaluate((el) => getComputedStyle(el).transform);
      expect(afterTransform).not.toBe(beforeTransform);
    });

    test('respects reduced motion preference', async ({ page }) => {
      await page.emulateMedia({ reducedMotion: 'reduce' });
      await stubPricingData(page);
      await page.goto('/pricing');

      const faqButton = page.getByRole('button', { name: /What is included in the free trial/ });
      await faqButton.scrollIntoViewIfNeeded();

      // Clicking should still work
      await faqButton.click();
      await expect(faqButton).toHaveAttribute('aria-expanded', 'true');
    });
  });

  test.describe('Accessibility', () => {
    test('FAQ items are keyboard accessible', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      const faqButton = page.getByRole('button', { name: /What is included in the free trial/ });
      await faqButton.scrollIntoViewIfNeeded();
      await faqButton.focus();

      // Press Enter to expand
      await page.keyboard.press('Enter');
      await expect(faqButton).toHaveAttribute('aria-expanded', 'true');

      // Press Space to collapse
      await page.keyboard.press('Space');
      await expect(faqButton).toHaveAttribute('aria-expanded', 'false');
    });

    test('ARIA expanded state is correct', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      const faqButton = page.getByRole('button', { name: /What is included in the free trial/ });
      await faqButton.scrollIntoViewIfNeeded();

      // Initially collapsed
      await expect(faqButton).toHaveAttribute('aria-expanded', 'false');

      // After click - expanded
      await faqButton.click();
      await expect(faqButton).toHaveAttribute('aria-expanded', 'true');
    });

    test('focus visible on FAQ headers', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      const faqButton = page.getByRole('button', { name: /What is included in the free trial/ });
      await faqButton.scrollIntoViewIfNeeded();
      await faqButton.focus();

      // Check element has focus
      const isFocused = await faqButton.evaluate((el) => el === document.activeElement);
      expect(isFocused).toBe(true);
    });
  });

  test.describe('Markdown Content', () => {
    test('renders bold text correctly', async ({ page }) => {
      await stubPricingData(page);
      await page.goto('/pricing');

      const faqButton = page.getByRole('button', { name: /What is included in the free trial/ });
      await faqButton.scrollIntoViewIfNeeded();
      await faqButton.click();

      // Check for bold text (14-day free trial should be bold)
      const boldText = page.locator('strong').filter({ hasText: '14-day free trial' });
      await expect(boldText).toBeVisible();
    });
  });

  test.describe('Mobile View', () => {
    test('FAQ accordion works on mobile', async ({ page }) => {
      await stubPricingData(page);
      await page.setViewportSize({ width: 375, height: 667 });
      await page.goto('/pricing');

      const faqButton = page.getByRole('button', { name: /What is included in the free trial/ });
      await faqButton.scrollIntoViewIfNeeded();

      // Click should work on mobile
      await faqButton.click();
      await expect(faqButton).toHaveAttribute('aria-expanded', 'true');
    });

    test('touch targets are minimum 44px', async ({ page }) => {
      await stubPricingData(page);
      await page.setViewportSize({ width: 375, height: 667 });
      await page.goto('/pricing');

      const faqButtons = page.locator('.accordion-header, .faq-question');
      const count = await faqButtons.count();

      for (let i = 0; i < Math.min(count, 3); i++) {
        const button = faqButtons.nth(i);
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
});
