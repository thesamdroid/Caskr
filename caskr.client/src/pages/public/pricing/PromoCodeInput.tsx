import { useState, useCallback, useRef, useEffect } from 'react';
import { PromoCodeValidationResult } from '../../../types/pricing';
import { ValidatedPromo } from '../../../utils/calculatePromoPrice';
import './PromoCodeInput.css';

/**
 * Props for PromoCodeInput component
 */
export interface PromoCodeInputProps {
  /** Callback when promo is validated and applied */
  onApply: (promo: ValidatedPromo) => void;
  /** Callback when promo is removed */
  onRemove: () => void;
  /** Initial code from URL parameter */
  initialCode?: string;
  /** Alternative callback for validation (legacy support) */
  onValidate?: (code: string) => Promise<PromoCodeValidationResult>;
  /** Alternative callback for clearing (legacy support) */
  onClear?: () => void;
  /** Current validation result (for controlled mode) */
  currentValidation?: PromoCodeValidationResult | null;
  /** Additional CSS class */
  className?: string;
  /** Analytics callback for tracking events */
  onAnalyticsEvent?: (event: string, data?: Record<string, unknown>) => void;
}

/**
 * Promo code input field with validation.
 * Collapsible by default with "Have a promo code?" link.
 * Shows discount applied when valid.
 */
export function PromoCodeInput({
  onApply,
  onRemove,
  initialCode,
  onValidate,
  onClear,
  currentValidation,
  className = '',
  onAnalyticsEvent,
}: PromoCodeInputProps) {
  const [code, setCode] = useState(initialCode ?? '');
  const [isValidating, setIsValidating] = useState(false);
  const [showInput, setShowInput] = useState(!!initialCode);
  const [localValidation, setLocalValidation] = useState<PromoCodeValidationResult | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Use controlled validation if provided, otherwise use local state
  const validation = currentValidation ?? localValidation;

  // Auto-expand and validate if initial code is provided
  useEffect(() => {
    if (initialCode && !validation?.isValid) {
      setShowInput(true);
      setCode(initialCode);
      // Auto-validate will be handled by parent via usePricingData
    }
  }, [initialCode, validation?.isValid]);

  const handleSubmit = useCallback(async (e: React.FormEvent) => {
    e.preventDefault();
    if (!code.trim() || isValidating) return;

    const normalizedCode = code.trim().toUpperCase();
    setIsValidating(true);

    // Track analytics event
    onAnalyticsEvent?.('promo_code_entered', { code: normalizedCode });

    try {
      if (onValidate) {
        // Legacy mode: use onValidate callback
        const result = await onValidate(normalizedCode);
        setLocalValidation(result);

        if (result.isValid && result.code) {
          // Convert to ValidatedPromo and call onApply
          const promo: ValidatedPromo = {
            code: result.code,
            discountType: result.discountType ?? 'percentage',
            discountValue: result.discountValue ?? 0,
            description: result.discountDescription ?? result.description ?? '',
            appliesTo: result.applicableTierIds ?? null,
          };
          onApply(promo);
          onAnalyticsEvent?.('promo_code_applied', {
            code: normalizedCode,
            valid: true,
            discountType: promo.discountType,
            discountValue: promo.discountValue,
          });
        } else {
          onAnalyticsEvent?.('promo_code_invalid', {
            code: normalizedCode,
            error: result.errorMessage,
          });
        }
      } else {
        // Direct API call mode
        const response = await fetch('/api/public/pricing/validate-promo', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ code: normalizedCode }),
        });

        if (!response.ok) {
          throw new Error('Network error');
        }

        const result: PromoCodeValidationResult = await response.json();
        setLocalValidation(result);

        if (result.isValid && result.code) {
          const promo: ValidatedPromo = {
            code: result.code,
            discountType: result.discountType ?? 'percentage',
            discountValue: result.discountValue ?? 0,
            description: result.discountDescription ?? result.description ?? '',
            appliesTo: result.applicableTierIds ?? null,
          };
          onApply(promo);
          onAnalyticsEvent?.('promo_code_applied', {
            code: normalizedCode,
            valid: true,
            discountType: promo.discountType,
            discountValue: promo.discountValue,
          });
        } else {
          onAnalyticsEvent?.('promo_code_invalid', {
            code: normalizedCode,
            error: result.errorMessage,
          });
        }
      }
    } catch (error) {
      const errorResult: PromoCodeValidationResult = {
        isValid: false,
        errorMessage: 'Could not validate code. Please try again.',
      };
      setLocalValidation(errorResult);
      onAnalyticsEvent?.('promo_code_error', {
        code: normalizedCode,
        error: 'network_error',
      });
    } finally {
      setIsValidating(false);
    }
  }, [code, isValidating, onValidate, onApply, onAnalyticsEvent]);

  const handleClear = useCallback(() => {
    const previousCode = code;
    setCode('');
    setLocalValidation(null);
    setShowInput(false);

    // Call appropriate clear callback
    if (onClear) {
      onClear();
    }
    onRemove();

    onAnalyticsEvent?.('promo_code_removed', { code: previousCode });
  }, [code, onClear, onRemove, onAnalyticsEvent]);

  const handleShowInput = useCallback(() => {
    setShowInput(true);
    onAnalyticsEvent?.('promo_input_opened', {});
    // Focus input after render
    setTimeout(() => inputRef.current?.focus(), 100);
  }, [onAnalyticsEvent]);

  const handleRetry = useCallback(() => {
    setLocalValidation(null);
    inputRef.current?.focus();
  }, []);

  // If we have a valid promo code applied, show the success state
  if (validation?.isValid) {
    return (
      <div className={`promo-code-container promo-code-success ${className}`}>
        <div className="promo-code-applied" role="status" aria-live="polite">
          <span className="promo-code-success-icon" aria-hidden="true">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
              <circle cx="10" cy="10" r="9" stroke="currentColor" strokeWidth="2" />
              <path
                d="M6 10L9 13L14 7"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
          </span>
          <div className="promo-code-info">
            <span className="promo-code-label">Promo Code Applied</span>
            <span className="promo-code-value">
              <span className="promo-code-code">{validation.code}</span>
              {validation.discountDescription && (
                <>
                  <span className="promo-code-separator">:</span>
                  <span className="promo-code-discount">{validation.discountDescription}</span>
                </>
              )}
            </span>
          </div>
          <button
            type="button"
            className="promo-code-remove"
            onClick={handleClear}
            aria-label="Remove promo code"
          >
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
              <path
                d="M4 4L12 12M12 4L4 12"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
              />
            </svg>
            <span className="promo-code-remove-text">Remove</span>
          </button>
        </div>
      </div>
    );
  }

  // Show toggle to reveal input
  if (!showInput) {
    return (
      <div className={`promo-code-container ${className}`}>
        <button
          type="button"
          className="promo-code-toggle"
          onClick={handleShowInput}
        >
          <span className="promo-code-toggle-icon" aria-hidden="true">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
              <path
                d="M2 8H14M8 2V14"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
              />
            </svg>
          </span>
          Have a promo code?
        </button>
      </div>
    );
  }

  // Show input form
  return (
    <div className={`promo-code-container ${className}`}>
      <form onSubmit={handleSubmit} className="promo-code-form">
        <div className="promo-code-input-wrapper">
          <input
            ref={inputRef}
            type="text"
            value={code}
            onChange={(e) => setCode(e.target.value.toUpperCase())}
            placeholder="Enter code"
            className={`promo-code-input ${validation && !validation.isValid ? 'error' : ''}`}
            aria-label="Promo code"
            aria-invalid={validation ? !validation.isValid : undefined}
            aria-describedby={validation?.errorMessage ? 'promo-error' : undefined}
            disabled={isValidating}
            maxLength={20}
            autoComplete="off"
            spellCheck={false}
          />
          <button
            type="submit"
            className="promo-code-apply"
            disabled={!code.trim() || isValidating}
            aria-busy={isValidating}
          >
            {isValidating ? (
              <span className="promo-code-spinner" aria-label="Validating">
                <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                  <circle
                    cx="8"
                    cy="8"
                    r="6"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeDasharray="25"
                    strokeLinecap="round"
                  />
                </svg>
              </span>
            ) : (
              'Apply'
            )}
          </button>
        </div>

        {/* Error state */}
        {validation && !validation.isValid && (
          <div className="promo-code-error-container">
            <p id="promo-error" className="promo-code-error" role="alert">
              <svg className="promo-code-error-icon" width="14" height="14" viewBox="0 0 14 14" fill="none" aria-hidden="true">
                <circle cx="7" cy="7" r="6" stroke="currentColor" strokeWidth="1.5" />
                <path d="M7 4V7.5M7 9.5V10" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
              </svg>
              {validation.errorMessage}
            </p>
            <button
              type="button"
              className="promo-code-retry"
              onClick={handleRetry}
            >
              Try again
            </button>
          </div>
        )}

        <button
          type="button"
          className="promo-code-cancel"
          onClick={() => {
            setShowInput(false);
            setLocalValidation(null);
          }}
        >
          Cancel
        </button>
      </form>
    </div>
  );
}

export default PromoCodeInput;
