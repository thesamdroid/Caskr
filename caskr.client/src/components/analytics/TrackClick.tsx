/**
 * TrackClick Component
 *
 * Wraps children and tracks click events for analytics.
 */

import { cloneElement, ReactElement, useCallback } from 'react';
import { useAnalytics } from '../../hooks/useAnalytics';
import { EventProperties } from '../../services/analytics';

/**
 * Props for TrackClick component
 */
export interface TrackClickProps {
  /** The event name to track */
  event: string;
  /** Additional properties for the event */
  properties?: EventProperties;
  /** Child element to wrap (must accept onClick) */
  children: ReactElement;
  /** Whether to prevent the default event action */
  preventDefault?: boolean;
  /** Whether to stop event propagation */
  stopPropagation?: boolean;
}

/**
 * Component that tracks click events on its child element
 */
export function TrackClick({
  event,
  properties = {},
  children,
  preventDefault = false,
  stopPropagation = false,
}: TrackClickProps) {
  const { track } = useAnalytics();

  const handleClick = useCallback(
    (e: React.MouseEvent) => {
      if (preventDefault) {
        e.preventDefault();
      }
      if (stopPropagation) {
        e.stopPropagation();
      }

      // Track the click event
      track(event, {
        ...properties,
        clickTimestamp: new Date().toISOString(),
      });

      // Call the original onClick handler if present
      const originalOnClick = children.props.onClick;
      if (typeof originalOnClick === 'function') {
        originalOnClick(e);
      }
    },
    [event, properties, track, children.props.onClick, preventDefault, stopPropagation]
  );

  return cloneElement(children, {
    onClick: handleClick,
  });
}

/**
 * Props for TrackCTAClick component (specialized for CTA buttons)
 */
export interface TrackCTAClickProps {
  /** The tier slug */
  tier: string;
  /** The CTA text */
  ctaText: string;
  /** The CTA URL */
  ctaUrl: string;
  /** Billing period */
  billingPeriod: 'monthly' | 'annual';
  /** Whether a promo is applied */
  promoApplied?: boolean;
  /** Price displayed */
  priceDisplayed?: number;
  /** Child element */
  children: ReactElement;
}

/**
 * Specialized component for tracking CTA button clicks
 */
export function TrackCTAClick({
  tier,
  ctaText,
  ctaUrl,
  billingPeriod,
  promoApplied = false,
  priceDisplayed,
  children,
}: TrackCTAClickProps) {
  return (
    <TrackClick
      event="cta_clicked"
      properties={{
        tier,
        cta_text: ctaText,
        cta_url: ctaUrl,
        billing_period: billingPeriod,
        promo_applied: promoApplied,
        price_displayed: priceDisplayed,
      }}
    >
      {children}
    </TrackClick>
  );
}

export default TrackClick;
