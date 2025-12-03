import { useState, useEffect, useRef, useMemo } from 'react';
import './AnimatedPrice.css';

export interface AnimatedPriceProps {
  /** Price amount in cents */
  amount: number;
  /** Currency code (default: "USD") */
  currency?: string;
  /** Period text (e.g., "/month", "/year") */
  period?: string;
  /** Previous amount for animation (in cents) */
  previousAmount?: number;
  /** Show original price struck through */
  showOriginal?: boolean;
  /** Original amount to show struck through (in cents) */
  originalAmount?: number;
  /** Additional CSS class */
  className?: string;
  /** Animation duration in ms (default: 500) */
  animationDuration?: number;
}

/**
 * Formats cents to currency string
 */
function formatCurrency(cents: number, currency: string): string {
  const amount = cents / 100;
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount);
}

/**
 * Get currency symbol
 */
function getCurrencySymbol(currency: string): string {
  const symbols: Record<string, string> = {
    USD: '$',
    EUR: '\u20AC',
    GBP: '\u00A3',
    JPY: '\u00A5',
    CAD: 'C$',
    AUD: 'A$',
  };
  return symbols[currency] || currency;
}

/**
 * Ease-out cubic function for smooth deceleration
 */
function easeOutCubic(t: number): number {
  return 1 - Math.pow(1 - t, 3);
}

/**
 * Check if user prefers reduced motion
 */
function prefersReducedMotion(): boolean {
  if (typeof window === 'undefined') return false;
  return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

/**
 * AnimatedPrice component that smoothly animates between price values.
 * Uses requestAnimationFrame for optimal performance.
 * Respects reduced motion preferences.
 *
 * Animation approach: requestAnimationFrame
 * - Chosen for smooth 60fps animation with full control
 * - No external dependencies (no bundle size increase)
 * - Works across all modern browsers
 * - Respects reduced motion preferences
 */
export function AnimatedPrice({
  amount,
  currency = 'USD',
  period,
  previousAmount,
  showOriginal = false,
  originalAmount,
  className = '',
  animationDuration = 500,
}: AnimatedPriceProps) {
  const [displayAmount, setDisplayAmount] = useState(amount);
  const animationRef = useRef<number | null>(null);
  const startTimeRef = useRef<number | null>(null);
  const startAmountRef = useRef<number>(amount);

  const currencySymbol = useMemo(() => getCurrencySymbol(currency), [currency]);

  // Animate when amount changes
  useEffect(() => {
    // Skip animation if reduced motion is preferred
    if (prefersReducedMotion()) {
      setDisplayAmount(amount);
      return;
    }

    // Skip animation on initial render or if no previous amount
    if (previousAmount === undefined || previousAmount === amount) {
      setDisplayAmount(amount);
      return;
    }

    startAmountRef.current = previousAmount;
    startTimeRef.current = null;

    const animate = (timestamp: number) => {
      if (startTimeRef.current === null) {
        startTimeRef.current = timestamp;
      }

      const elapsed = timestamp - startTimeRef.current;
      const progress = Math.min(elapsed / animationDuration, 1);
      const easedProgress = easeOutCubic(progress);

      const currentAmount = Math.round(
        startAmountRef.current + (amount - startAmountRef.current) * easedProgress
      );

      setDisplayAmount(currentAmount);

      if (progress < 1) {
        animationRef.current = requestAnimationFrame(animate);
      }
    };

    animationRef.current = requestAnimationFrame(animate);

    return () => {
      if (animationRef.current !== null) {
        cancelAnimationFrame(animationRef.current);
      }
    };
  }, [amount, previousAmount, animationDuration]);

  const formattedAmount = useMemo(() => {
    // Get just the number part for animation
    const num = Math.round(displayAmount / 100);
    return num.toLocaleString('en-US');
  }, [displayAmount]);

  const formattedOriginal = useMemo(() => {
    if (!originalAmount) return null;
    return formatCurrency(originalAmount, currency);
  }, [originalAmount, currency]);

  return (
    <div className={`animated-price ${className}`}>
      {showOriginal && formattedOriginal && (
        <span className="animated-price-original" aria-label={`Originally ${formattedOriginal}`}>
          {formattedOriginal}
        </span>
      )}
      <span className="animated-price-current">
        <span className="animated-price-currency" aria-hidden="true">
          {currencySymbol}
        </span>
        <span
          className="animated-price-amount"
          aria-label={`${formatCurrency(displayAmount, currency)}${period ? ` ${period}` : ''}`}
        >
          {formattedAmount}
        </span>
      </span>
      {period && (
        <span className="animated-price-period" aria-hidden="true">
          {period}
        </span>
      )}
    </div>
  );
}

export default AnimatedPrice;
