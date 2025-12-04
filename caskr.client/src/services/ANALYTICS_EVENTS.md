# Analytics Events Documentation

This document describes all analytics events tracked on the Pricing Page.

## Page Events

### `pricing_page_viewed`
Fired when user lands on the pricing page.

| Property | Type | Description |
|----------|------|-------------|
| `source` | string | Referrer URL |
| `utm_source` | string? | UTM source parameter |
| `utm_medium` | string? | UTM medium parameter |
| `utm_campaign` | string? | UTM campaign parameter |
| `promo_code` | string? | Promo code from URL |

## Interaction Events

### `billing_toggle_changed`
Fired when user toggles between monthly and annual billing.

| Property | Type | Description |
|----------|------|-------------|
| `from` | 'monthly' \| 'annual' | Previous billing period |
| `to` | 'monthly' \| 'annual' | New billing period |

### `tier_card_hovered`
Fired when user hovers over a pricing tier card for >500ms.

| Property | Type | Description |
|----------|------|-------------|
| `tier` | string | Tier slug |
| `duration_ms` | number | Hover duration in milliseconds |

### `tier_card_clicked`
Fired when user clicks on a pricing tier card.

| Property | Type | Description |
|----------|------|-------------|
| `tier` | string | Tier slug |
| `billing_period` | 'monthly' \| 'annual' | Current billing period |
| `price_displayed` | number | Price shown in cents |

### `feature_comparison_viewed`
Fired when feature comparison table scrolls into view.

| Property | Type | Description |
|----------|------|-------------|
| `visible` | boolean | Whether element is visible |
| `section` | string | Section identifier |

### `faq_item_opened`
Fired when user expands a FAQ item.

| Property | Type | Description |
|----------|------|-------------|
| `question_id` | string | FAQ question ID |
| `question_text` | string | Question text (truncated to 100 chars) |

## Promo Code Events

### `promo_input_opened`
Fired when user clicks "Have a promo code?" to expand input.

No additional properties.

### `promo_code_entered`
Fired when user submits a promo code for validation.

| Property | Type | Description |
|----------|------|-------------|
| `code` | string | Promo code entered |

### `promo_code_applied`
Fired when promo code validation succeeds.

| Property | Type | Description |
|----------|------|-------------|
| `code` | string | Promo code |
| `valid` | boolean | Always true |
| `discountType` | string | 'percentage' \| 'fixedAmount' \| 'freeMonths' |
| `discountValue` | number | Discount value |

### `promo_code_invalid`
Fired when promo code validation fails.

| Property | Type | Description |
|----------|------|-------------|
| `code` | string | Promo code attempted |
| `error` | string | Error message |

### `promo_code_error`
Fired when promo code validation has a network error.

| Property | Type | Description |
|----------|------|-------------|
| `code` | string | Promo code attempted |
| `error` | string | Error type ('network_error') |

### `promo_code_removed`
Fired when user removes an applied promo code.

| Property | Type | Description |
|----------|------|-------------|
| `code` | string | Promo code removed |

## CTA Events

### `cta_clicked`
Fired when user clicks a call-to-action button.

| Property | Type | Description |
|----------|------|-------------|
| `tier` | string | Tier slug |
| `cta_text` | string | Button text |
| `cta_url` | string | Destination URL |
| `billing_period` | 'monthly' \| 'annual' | Current billing period |
| `promo_applied` | boolean | Whether promo is active |
| `price_displayed` | number? | Price shown |

### `mobile_sticky_cta_clicked`
Fired when user clicks the mobile sticky "View Plans" button.

No additional properties.

## Engagement Events

### `scroll_depth`
Fired when user scrolls to milestone percentages.

| Property | Type | Description |
|----------|------|-------------|
| `page` | string | Page name |
| `depth` | number | Scroll depth (25, 50, 75, 100) |

### `time_on_page`
Fired after 30 seconds on the page.

| Property | Type | Description |
|----------|------|-------------|
| `page` | string | Page name |
| `duration_ms` | number | Time spent in milliseconds |
| `milestone` | string | '30s' |

### `time_on_page_final`
Fired when user leaves the page.

| Property | Type | Description |
|----------|------|-------------|
| `page` | string | Page name |
| `duration_ms` | number | Total time spent in milliseconds |

## Implementation Notes

### Consent
- Analytics events are only tracked after user consent is given
- Do Not Track header is respected by default
- In development mode, consent is not required for testing

### Privacy
- IP addresses are anonymized in GA4
- No PII is included in event properties
- Promo codes are tracked but not associated with user identity

### Providers
The analytics service supports multiple providers:
1. **Google Analytics 4** - Primary analytics
2. **Segment** - Optional, for customer data platform
3. **Custom Backend** - For internal analytics storage
4. **Console** - Development logging

### Testing
Events can be observed in development by checking the browser console.
All events are prefixed with `[Analytics]`.
