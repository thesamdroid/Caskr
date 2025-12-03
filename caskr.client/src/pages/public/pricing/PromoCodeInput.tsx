import { useState, useCallback, useRef } from 'react';
import { PromoCodeValidationResult } from '../../../types/pricing';
import './PromoCodeInput.css';

export interface PromoCodeInputProps {
  onValidate: (code: string) => Promise<PromoCodeValidationResult>;
  onClear: () => void;
  currentValidation: PromoCodeValidationResult | null;
  className?: string;
}

/**
 * Promo code input field with validation.
 * Shows discount applied when valid.
 */
export function PromoCodeInput({
  onValidate,
  onClear,
  currentValidation,
  className = '',
}: PromoCodeInputProps) {
  const [code, setCode] = useState('');
  const [isValidating, setIsValidating] = useState(false);
  const [showInput, setShowInput] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleSubmit = useCallback(async (e: React.FormEvent) => {
    e.preventDefault();
    if (!code.trim() || isValidating) return;

    setIsValidating(true);
    try {
      await onValidate(code.trim().toUpperCase());
    } finally {
      setIsValidating(false);
    }
  }, [code, isValidating, onValidate]);

  const handleClear = useCallback(() => {
    setCode('');
    onClear();
    setShowInput(false);
  }, [onClear]);

  const handleShowInput = useCallback(() => {
    setShowInput(true);
    // Focus input after render
    setTimeout(() => inputRef.current?.focus(), 100);
  }, []);

  // If we have a valid promo code applied, show the success state
  if (currentValidation?.isValid) {
    return (
      <div className={`promo-code-container ${className}`}>
        <div className="promo-code-applied">
          <span className="promo-code-success-icon" aria-hidden="true">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
              <path
                d="M16.5 5.5L7.5 14.5L3.5 10.5"
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
              {currentValidation.code}: {currentValidation.discountDescription}
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
                d="M8 4V12M4 8H12"
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
            className={`promo-code-input ${currentValidation && !currentValidation.isValid ? 'error' : ''}`}
            aria-label="Promo code"
            aria-invalid={currentValidation && !currentValidation.isValid}
            aria-describedby={currentValidation?.errorMessage ? 'promo-error' : undefined}
            disabled={isValidating}
            maxLength={20}
          />
          <button
            type="submit"
            className="promo-code-apply"
            disabled={!code.trim() || isValidating}
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
        {currentValidation && !currentValidation.isValid && (
          <p id="promo-error" className="promo-code-error" role="alert">
            {currentValidation.errorMessage}
          </p>
        )}
        <button
          type="button"
          className="promo-code-cancel"
          onClick={() => setShowInput(false)}
        >
          Cancel
        </button>
      </form>
    </div>
  );
}

export default PromoCodeInput;
