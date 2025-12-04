/**
 * TrackVisibility Component
 *
 * Tracks when an element enters the viewport using Intersection Observer.
 * Fires an analytics event once when the element becomes visible.
 */

import { useEffect, useRef, ReactNode } from 'react';
import { useAnalytics } from '../../hooks/useAnalytics';
import { EventProperties } from '../../services/analytics';

/**
 * Props for TrackVisibility component
 */
export interface TrackVisibilityProps {
  /** The event name to track */
  event: string;
  /** Additional properties for the event */
  properties?: EventProperties;
  /** Child elements to wrap */
  children: ReactNode;
  /** Visibility threshold (0-1, default 0.5 = 50% visible) */
  threshold?: number;
  /** Only fire event once (default true) */
  once?: boolean;
  /** Root margin for intersection observer */
  rootMargin?: string;
  /** Optional className for wrapper */
  className?: string;
  /** Fire event when element becomes invisible */
  trackOnHide?: boolean;
}

/**
 * Component that tracks when its children become visible in the viewport
 */
export function TrackVisibility({
  event,
  properties = {},
  children,
  threshold = 0.5,
  once = true,
  rootMargin = '0px',
  className,
  trackOnHide = false,
}: TrackVisibilityProps) {
  const elementRef = useRef<HTMLDivElement>(null);
  const hasTrackedRef = useRef(false);
  const wasVisibleRef = useRef(false);
  const { track } = useAnalytics();

  useEffect(() => {
    const element = elementRef.current;
    if (!element) return;

    // Check if Intersection Observer is supported
    if (!('IntersectionObserver' in window)) {
      // Fallback: fire event immediately
      if (!hasTrackedRef.current) {
        track(event, { ...properties, visible: true });
        hasTrackedRef.current = true;
      }
      return;
    }

    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          const isVisible = entry.isIntersecting;

          if (isVisible && !hasTrackedRef.current) {
            // Element became visible
            track(event, {
              ...properties,
              visible: true,
              intersectionRatio: entry.intersectionRatio,
            });

            if (once) {
              hasTrackedRef.current = true;
              observer.disconnect();
            }
          } else if (!isVisible && wasVisibleRef.current && trackOnHide && !once) {
            // Element became invisible (if trackOnHide is enabled)
            track(event, {
              ...properties,
              visible: false,
            });
          }

          wasVisibleRef.current = isVisible;
        }
      },
      {
        threshold,
        rootMargin,
      }
    );

    observer.observe(element);

    return () => {
      observer.disconnect();
    };
  }, [event, properties, threshold, rootMargin, once, trackOnHide, track]);

  return (
    <div ref={elementRef} className={className}>
      {children}
    </div>
  );
}

export default TrackVisibility;
