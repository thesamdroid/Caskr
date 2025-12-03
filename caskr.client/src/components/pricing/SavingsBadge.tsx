import { useMemo } from 'react';
import './SavingsBadge.css';

export interface SavingsBadgeProps {
  /** Savings amount in cents (null or 0 hides the badge) */
  savingsAmount: number | null;
  /** Currency code (default: "USD") */
  currency?: string;
  /** Show as percentage instead of amount */
  showAsPercent?: boolean;
  /** Percentage value if showing as percent */
  percent?: number;
  /** Whether the badge should be visible/animated */
  isVisible?: boolean;
  /** Additional CSS class */
  className?: string;
}

/**
 * Formats cents to currency string
 */
function formatSavings(cents: number, currency: string): string {
  const amount = cents / 100;
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount);
}

/**
 * SavingsBadge component that displays annual savings with a pop animation.
 * Shows "Save $X/year" or "Save X%" based on configuration.
 */
export function SavingsBadge({
  savingsAmount,
  currency = 'USD',
  showAsPercent = false,
  percent,
  isVisible = true,
  className = '',
}: SavingsBadgeProps) {
  const displayText = useMemo(() => {
    if (showAsPercent && percent !== undefined && percent > 0) {
      return `Save ${percent}%`;
    }
    if (savingsAmount && savingsAmount > 0) {
      return `Save ${formatSavings(savingsAmount, currency)}/year`;
    }
    return null;
  }, [savingsAmount, currency, showAsPercent, percent]);

  // Don't render if no savings or not visible
  if (!displayText || !isVisible) {
    return null;
  }

  return (
    <span
      className={`savings-badge ${className}`}
      role="status"
      aria-label={displayText}
    >
      {displayText}
    </span>
  );
}

export default SavingsBadge;
