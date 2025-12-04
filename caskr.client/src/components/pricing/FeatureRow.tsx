import { useState, useRef, useEffect } from 'react';
import { PricingTier, PricingFeature } from '../../types/pricing';
import './FeatureRow.css';

export interface FeatureValueData {
  tierId: number;
  isIncluded: boolean;
  limitValue?: string;
  textValue?: string;
}

export interface FeatureRowProps {
  feature: {
    name: string;
    description?: string;
    category: string;
  };
  values: FeatureValueData[];
  tiers: PricingTier[];
  highlightedTierId?: number | null;
  zebra?: boolean;
}

/**
 * Individual feature row in the comparison table.
 * Supports boolean, limit, and text value types.
 * Shows tooltip on hover/focus for feature description.
 */
export function FeatureRow({
  feature,
  values,
  tiers,
  highlightedTierId,
  zebra = false,
}: FeatureRowProps) {
  const [showTooltip, setShowTooltip] = useState(false);
  const [tooltipPosition, setTooltipPosition] = useState<'above' | 'below'>('below');
  const featureNameRef = useRef<HTMLDivElement>(null);
  const tooltipRef = useRef<HTMLDivElement>(null);

  // Sort tiers by sortOrder
  const sortedTiers = [...tiers].sort((a, b) => a.sortOrder - b.sortOrder);

  // Get value for a specific tier
  const getValueForTier = (tierId: number): FeatureValueData | undefined => {
    return values.find(v => v.tierId === tierId);
  };

  // Calculate tooltip position
  useEffect(() => {
    if (showTooltip && featureNameRef.current && tooltipRef.current) {
      const featureRect = featureNameRef.current.getBoundingClientRect();
      const tooltipHeight = tooltipRef.current.offsetHeight;
      const spaceAbove = featureRect.top;
      const spaceBelow = window.innerHeight - featureRect.bottom;

      setTooltipPosition(spaceBelow < tooltipHeight + 20 && spaceAbove > tooltipHeight + 20 ? 'above' : 'below');
    }
  }, [showTooltip]);

  const handleMouseEnter = () => {
    if (feature.description) {
      setShowTooltip(true);
    }
  };

  const handleMouseLeave = () => {
    setShowTooltip(false);
  };

  const handleFocus = () => {
    if (feature.description) {
      setShowTooltip(true);
    }
  };

  const handleBlur = () => {
    setShowTooltip(false);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') {
      setShowTooltip(false);
    }
  };

  const renderCellValue = (value: FeatureValueData | undefined, tier: PricingTier) => {
    if (!value) {
      return (
        <span className="feature-value-na" aria-label="Not applicable">
          <DashIcon />
        </span>
      );
    }

    // Text value (like "Email only", "24/7 phone")
    if (value.textValue) {
      return (
        <span className="feature-value-text">
          {value.textValue}
        </span>
      );
    }

    // Limit value (like "5 users", "Unlimited")
    if (value.limitValue) {
      return (
        <span className="feature-value-limit">
          {value.limitValue}
        </span>
      );
    }

    // Boolean value
    if (value.isIncluded) {
      return (
        <span className="feature-value-check" aria-label="Included">
          <CheckIcon />
        </span>
      );
    }

    return (
      <span className="feature-value-x" aria-label="Not included">
        <XIcon />
      </span>
    );
  };

  return (
    <tr className={`feature-row ${zebra ? 'zebra' : ''}`}>
      <th scope="row" className="feature-name-cell">
        <div
          ref={featureNameRef}
          className={`feature-name-wrapper ${feature.description ? 'has-tooltip' : ''}`}
          onMouseEnter={handleMouseEnter}
          onMouseLeave={handleMouseLeave}
          onFocus={handleFocus}
          onBlur={handleBlur}
          onKeyDown={handleKeyDown}
          tabIndex={feature.description ? 0 : undefined}
          role={feature.description ? 'button' : undefined}
          aria-describedby={feature.description ? `tooltip-${feature.name.replace(/\s+/g, '-')}` : undefined}
        >
          <span className="feature-name">{feature.name}</span>
          {feature.description && (
            <span className="feature-info-icon" aria-hidden="true">
              <InfoIcon />
            </span>
          )}
        </div>

        {/* Tooltip */}
        {feature.description && showTooltip && (
          <div
            ref={tooltipRef}
            id={`tooltip-${feature.name.replace(/\s+/g, '-')}`}
            className={`feature-tooltip ${tooltipPosition}`}
            role="tooltip"
          >
            {feature.description}
            <span className="tooltip-arrow" aria-hidden="true" />
          </div>
        )}
      </th>

      {sortedTiers.map((tier) => {
        const value = getValueForTier(tier.id);
        const isHighlighted = highlightedTierId === tier.id;

        return (
          <td
            key={tier.id}
            className={`feature-value-cell ${tier.isPopular ? 'popular' : ''} ${isHighlighted ? 'highlighted' : ''}`}
          >
            {renderCellValue(value, tier)}
          </td>
        );
      })}
    </tr>
  );
}

// Icon components
function CheckIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden="true">
      <path
        d="M16.5 5.5L7.5 14.5L3.5 10.5"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function XIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden="true">
      <path
        d="M5 5L15 15M15 5L5 15"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
      />
    </svg>
  );
}

function DashIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden="true">
      <path
        d="M5 10H15"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
      />
    </svg>
  );
}

function InfoIcon() {
  return (
    <svg width="14" height="14" viewBox="0 0 14 14" fill="none" aria-hidden="true">
      <circle cx="7" cy="7" r="6" stroke="currentColor" strokeWidth="1.5" />
      <path d="M7 6V10" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
      <circle cx="7" cy="4" r="0.75" fill="currentColor" />
    </svg>
  );
}

export default FeatureRow;
