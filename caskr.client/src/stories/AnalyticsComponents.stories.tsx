/**
 * Analytics Components Storybook Stories
 *
 * Showcases the analytics tracking components.
 */

import type { Meta, StoryObj } from '@storybook/react';
import { fn } from '@storybook/test';
import { TrackVisibility } from '../components/analytics/TrackVisibility';
import { TrackClick, TrackCTAClick } from '../components/analytics/TrackClick';

// TrackVisibility Stories
const trackVisibilityMeta = {
  title: 'Analytics/TrackVisibility',
  component: TrackVisibility,
  parameters: {
    layout: 'centered',
    docs: {
      description: {
        component: 'Tracks when an element enters the viewport using Intersection Observer.',
      },
    },
  },
  tags: ['autodocs'],
  argTypes: {
    event: {
      control: 'text',
      description: 'Event name to track',
    },
    threshold: {
      control: { type: 'range', min: 0, max: 1, step: 0.1 },
      description: 'Visibility threshold (0-1)',
    },
    once: {
      control: 'boolean',
      description: 'Only fire event once',
    },
  },
} satisfies Meta<typeof TrackVisibility>;

export default trackVisibilityMeta;
type TrackVisibilityStory = StoryObj<typeof trackVisibilityMeta>;

export const Default: TrackVisibilityStory = {
  args: {
    event: 'section_viewed',
    properties: { section: 'demo' },
    threshold: 0.5,
    once: true,
    children: (
      <div style={{
        padding: '40px',
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
        borderRadius: '8px',
        color: 'white',
        textAlign: 'center',
      }}>
        <h3>Tracked Section</h3>
        <p>This section fires an analytics event when 50% visible.</p>
      </div>
    ),
  },
};

export const LowThreshold: TrackVisibilityStory = {
  args: {
    event: 'section_glimpsed',
    properties: { section: 'low-threshold' },
    threshold: 0.1,
    once: true,
    children: (
      <div style={{
        padding: '40px',
        background: '#e0e0e0',
        borderRadius: '8px',
        textAlign: 'center',
      }}>
        <h3>Low Threshold Section</h3>
        <p>Fires when just 10% visible.</p>
      </div>
    ),
  },
};

export const RepeatedTracking: TrackVisibilityStory = {
  args: {
    event: 'section_visibility_changed',
    properties: { section: 'repeated' },
    threshold: 0.5,
    once: false,
    trackOnHide: true,
    children: (
      <div style={{
        padding: '40px',
        background: '#c8e6c9',
        borderRadius: '8px',
        textAlign: 'center',
      }}>
        <h3>Repeated Tracking Section</h3>
        <p>Fires every time visibility changes.</p>
      </div>
    ),
  },
};

// TrackClick Stories
export const TrackClickMeta = {
  title: 'Analytics/TrackClick',
  component: TrackClick,
  parameters: {
    layout: 'centered',
    docs: {
      description: {
        component: 'Wraps children and tracks click events for analytics.',
      },
    },
  },
  tags: ['autodocs'],
};

export const ClickTracking: StoryObj<typeof TrackClick> = {
  render: () => (
    <TrackClick
      event="button_clicked"
      properties={{ button_id: 'demo', location: 'story' }}
    >
      <button
        style={{
          padding: '12px 24px',
          fontSize: '16px',
          background: '#1976d2',
          color: 'white',
          border: 'none',
          borderRadius: '8px',
          cursor: 'pointer',
        }}
      >
        Click Me (Tracked)
      </button>
    </TrackClick>
  ),
};

export const CTATracking: StoryObj<typeof TrackCTAClick> = {
  render: () => (
    <TrackCTAClick
      tier="professional"
      ctaText="Start Free Trial"
      ctaUrl="/signup?tier=professional"
      billingPeriod="annual"
      promoApplied={true}
      priceDisplayed={23920}
    >
      <a
        href="/signup?tier=professional"
        style={{
          display: 'inline-block',
          padding: '16px 32px',
          fontSize: '18px',
          fontWeight: '600',
          background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
          color: 'white',
          textDecoration: 'none',
          borderRadius: '8px',
        }}
      >
        Start Free Trial
      </a>
    </TrackCTAClick>
  ),
};

export const LinkTracking: StoryObj<typeof TrackClick> = {
  render: () => (
    <TrackClick
      event="link_clicked"
      properties={{ link_text: 'Learn more', destination: '/about' }}
    >
      <a
        href="/about"
        style={{
          color: '#1976d2',
          textDecoration: 'underline',
          cursor: 'pointer',
        }}
      >
        Learn more about our features
      </a>
    </TrackClick>
  ),
};
