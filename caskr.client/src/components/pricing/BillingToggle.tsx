import { useCallback, useRef, useEffect } from 'react';
import { BillingPeriod } from '../../types/pricing';
import './BillingToggle.css';

export interface BillingToggleProps {
  value: BillingPeriod;
  onChange: (value: BillingPeriod) => void;
  annualDiscountPercent: number;
  className?: string;
}

/**
 * Pill-shaped toggle for switching between monthly and annual billing.
 * Accessible via keyboard navigation (arrow keys, space, enter).
 * Respects reduced motion preferences.
 */
export function BillingToggle({
  value,
  onChange,
  annualDiscountPercent,
  className = '',
}: BillingToggleProps) {
  const toggleRef = useRef<HTMLDivElement>(null);
  const monthlyRef = useRef<HTMLButtonElement>(null);
  const annualRef = useRef<HTMLButtonElement>(null);

  const handleMonthlyClick = useCallback(() => {
    onChange('monthly');
  }, [onChange]);

  const handleAnnualClick = useCallback(() => {
    onChange('annual');
  }, [onChange]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      switch (e.key) {
        case 'ArrowLeft':
        case 'ArrowUp':
          e.preventDefault();
          onChange('monthly');
          monthlyRef.current?.focus();
          break;
        case 'ArrowRight':
        case 'ArrowDown':
          e.preventDefault();
          onChange('annual');
          annualRef.current?.focus();
          break;
        case ' ':
        case 'Enter':
          e.preventDefault();
          onChange(value === 'monthly' ? 'annual' : 'monthly');
          break;
      }
    },
    [onChange, value]
  );

  // Announce period changes to screen readers
  useEffect(() => {
    const announcement = document.getElementById('billing-toggle-announcement');
    if (announcement) {
      announcement.textContent = `Billing period changed to ${value}`;
    }
  }, [value]);

  return (
    <div className={`billing-toggle-container ${className}`}>
      <div
        ref={toggleRef}
        className="billing-toggle"
        role="radiogroup"
        aria-label="Billing period"
        onKeyDown={handleKeyDown}
      >
        <div
          className="billing-toggle-slider"
          style={{
            transform: value === 'annual' ? 'translateX(100%)' : 'translateX(0)',
          }}
          aria-hidden="true"
        />
        <button
          ref={monthlyRef}
          type="button"
          role="radio"
          aria-checked={value === 'monthly'}
          className={`billing-toggle-option ${value === 'monthly' ? 'active' : ''}`}
          onClick={handleMonthlyClick}
          tabIndex={value === 'monthly' ? 0 : -1}
        >
          Monthly
        </button>
        <button
          ref={annualRef}
          type="button"
          role="radio"
          aria-checked={value === 'annual'}
          className={`billing-toggle-option ${value === 'annual' ? 'active' : ''}`}
          onClick={handleAnnualClick}
          tabIndex={value === 'annual' ? 0 : -1}
        >
          Annual
          {annualDiscountPercent > 0 && (
            <span className="billing-toggle-badge" aria-label={`Save ${annualDiscountPercent}%`}>
              Save {annualDiscountPercent}%
            </span>
          )}
        </button>
      </div>
      {/* Screen reader announcement */}
      <div
        id="billing-toggle-announcement"
        className="visually-hidden"
        aria-live="polite"
        aria-atomic="true"
      />
    </div>
  );
}

export default BillingToggle;
