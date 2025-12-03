import './PricingFooter.css';

/**
 * Standard public footer for pricing page.
 */
export function PricingFooter() {
  const currentYear = new Date().getFullYear();

  return (
    <footer className="pricing-footer" role="contentinfo">
      <div className="pricing-footer-container">
        <div className="pricing-footer-brand">
          <a href="/" className="pricing-footer-logo">
            <div className="barrel-icon" aria-hidden="true" />
            <span>CASKr</span>
          </a>
          <p className="pricing-footer-tagline">
            Modern distillery management software
          </p>
        </div>

        <nav className="pricing-footer-nav" aria-label="Footer navigation">
          <div className="pricing-footer-column">
            <h3 className="pricing-footer-heading">Product</h3>
            <ul className="pricing-footer-links">
              <li><a href="/features">Features</a></li>
              <li><a href="/pricing">Pricing</a></li>
              <li><a href="/integrations">Integrations</a></li>
              <li><a href="/updates">Updates</a></li>
            </ul>
          </div>

          <div className="pricing-footer-column">
            <h3 className="pricing-footer-heading">Company</h3>
            <ul className="pricing-footer-links">
              <li><a href="/about">About</a></li>
              <li><a href="/blog">Blog</a></li>
              <li><a href="/careers">Careers</a></li>
              <li><a href="/contact">Contact</a></li>
            </ul>
          </div>

          <div className="pricing-footer-column">
            <h3 className="pricing-footer-heading">Resources</h3>
            <ul className="pricing-footer-links">
              <li><a href="/help">Help Center</a></li>
              <li><a href="/docs">Documentation</a></li>
              <li><a href="/api">API</a></li>
              <li><a href="/status">Status</a></li>
            </ul>
          </div>

          <div className="pricing-footer-column">
            <h3 className="pricing-footer-heading">Legal</h3>
            <ul className="pricing-footer-links">
              <li><a href="/privacy">Privacy Policy</a></li>
              <li><a href="/terms">Terms of Service</a></li>
              <li><a href="/security">Security</a></li>
              <li><a href="/compliance">Compliance</a></li>
            </ul>
          </div>
        </nav>

        <div className="pricing-footer-bottom">
          <p className="pricing-footer-copyright">
            &copy; {currentYear} CASKr. All rights reserved.
          </p>
        </div>
      </div>
    </footer>
  );
}

export default PricingFooter;
