/**
 * PromoCodeInput Storybook Stories
 *
 * Showcases the promo code input component in various states.
 */

import type { Meta, StoryObj } from '@storybook/react';
import { fn } from '@storybook/test';
import { PromoCodeInput, PromoCodeInputProps } from '../pages/public/pricing/PromoCodeInput';

const meta = {
  title: 'Pricing/PromoCodeInput',
  component: PromoCodeInput,
  parameters: {
    layout: 'centered',
    docs: {
      description: {
        component: 'Promo code input with validation, collapsible by default.',
      },
    },
  },
  tags: ['autodocs'],
  argTypes: {
    className: {
      control: 'text',
      description: 'Additional CSS class',
    },
    initialCode: {
      control: 'text',
      description: 'Initial promo code from URL parameter',
    },
  },
  args: {
    onApply: fn(),
    onRemove: fn(),
    onAnalyticsEvent: fn(),
  },
} satisfies Meta<typeof PromoCodeInput>;

export default meta;
type Story = StoryObj<typeof meta>;

/**
 * Default collapsed state with "Have a promo code?" link
 */
export const Default: Story = {
  args: {},
};

/**
 * Expanded input state (after clicking toggle)
 */
export const Expanded: Story = {
  args: {
    initialCode: '',
  },
  play: async ({ canvasElement }) => {
    const button = canvasElement.querySelector('.promo-code-toggle') as HTMLButtonElement;
    if (button) {
      button.click();
    }
  },
};

/**
 * With initial code from URL (auto-expanded)
 */
export const WithInitialCode: Story = {
  args: {
    initialCode: 'SAVE20',
  },
};

/**
 * Valid promo code applied (success state)
 */
export const ValidPromoApplied: Story = {
  args: {
    currentValidation: {
      isValid: true,
      code: 'SUMMER2024',
      discountDescription: '25% off your first year',
    },
  },
};

/**
 * Valid promo with percentage discount
 */
export const PercentageDiscount: Story = {
  args: {
    currentValidation: {
      isValid: true,
      code: 'PERCENT20',
      discountType: 'percentage',
      discountValue: 20,
      discountDescription: '20% off',
    },
  },
};

/**
 * Valid promo with fixed amount discount
 */
export const FixedDiscount: Story = {
  args: {
    currentValidation: {
      isValid: true,
      code: 'FLAT50',
      discountType: 'fixedAmount',
      discountValue: 5000,
      discountDescription: '$50 off',
    },
  },
};

/**
 * Valid promo with free months
 */
export const FreeMonths: Story = {
  args: {
    currentValidation: {
      isValid: true,
      code: 'FREE3',
      discountType: 'freeMonths',
      discountValue: 3,
      discountDescription: '3 months free',
    },
  },
};

/**
 * Invalid promo code error state
 */
export const InvalidCode: Story = {
  args: {
    currentValidation: {
      isValid: false,
      errorMessage: 'This promo code is not valid',
    },
  },
};

/**
 * Expired promo code error
 */
export const ExpiredCode: Story = {
  args: {
    currentValidation: {
      isValid: false,
      errorMessage: 'This promo code has expired',
    },
  },
};

/**
 * Max redemptions reached error
 */
export const MaxRedemptions: Story = {
  args: {
    currentValidation: {
      isValid: false,
      errorMessage: 'This promo code is no longer available',
    },
  },
};

/**
 * Network error state
 */
export const NetworkError: Story = {
  args: {
    currentValidation: {
      isValid: false,
      errorMessage: 'Could not validate code. Please try again.',
    },
  },
};

/**
 * Mobile view
 */
export const Mobile: Story = {
  args: {
    currentValidation: {
      isValid: true,
      code: 'MOBILE',
      discountDescription: '15% off',
    },
  },
  parameters: {
    viewport: {
      defaultViewport: 'mobile1',
    },
  },
};
