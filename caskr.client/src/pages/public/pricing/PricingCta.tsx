import './PricingCta.css';

export interface PricingCtaProps {
  className?: string;
}

/**
 * Final call-to-action section with trust badges.
 */
export function PricingCta({ className = '' }: PricingCtaProps) {
  return (
    <section className={`pricing-cta-section ${className}`} aria-labelledby="cta-heading">
      <div className="pricing-cta-container">
        <h2 id="cta-heading" className="pricing-cta-headline">
          Ready to streamline your distillery operations?
        </h2>

        <div className="pricing-cta-buttons">
          <a href="/signup" className="button-primary button-lg pricing-cta-primary">
            Start Your Free Trial
          </a>
          <a href="/demo" className="button-secondary button-lg pricing-cta-secondary">
            Schedule a Demo
          </a>
        </div>

        <div className="pricing-cta-trust" role="list" aria-label="Trust badges">
          <div className="pricing-cta-badge" role="listitem">
            <span className="pricing-cta-badge-icon" aria-hidden="true">
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
            <span>No credit card required</span>
          </div>
          <div className="pricing-cta-badge" role="listitem">
            <span className="pricing-cta-badge-icon" aria-hidden="true">
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
            <span>14-day free trial</span>
          </div>
          <div className="pricing-cta-badge" role="listitem">
            <span className="pricing-cta-badge-icon" aria-hidden="true">
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
            <span>Cancel anytime</span>
          </div>
        </div>

        {/* Optional: Customer testimonial */}
        <div className="pricing-cta-testimonial">
          <blockquote className="testimonial-quote">
            "CASKr has transformed how we manage our barrel inventory. The TTB compliance features alone save us hours every month."
          </blockquote>
          <cite className="testimonial-author">
            <span className="testimonial-name">John Smith</span>
            <span className="testimonial-title">Head Distiller, Oakwood Distillery</span>
          </cite>
        </div>
      </div>
    </section>
  );
}

export default PricingCta;
