/**
 * Default FAQ content for the pricing page.
 * This can be used as seed data or fallback content.
 */

import type { PricingFaq } from '../types/pricing';

export const defaultPricingFaqs: PricingFaq[] = [
  // ====================================
  // BILLING QUESTIONS
  // ====================================
  {
    id: 1,
    question: 'Can I change plans later?',
    answer:
      'Yes, you can upgrade or downgrade your plan at any time. Changes take effect immediately, and any price differences will be **prorated** based on your billing cycle. Your data and settings are preserved when changing plans.',
    sortOrder: 1,
  },
  {
    id: 2,
    question: 'What payment methods do you accept?',
    answer:
      'We accept all major **credit cards** (Visa, Mastercard, American Express, Discover), **ACH bank transfers** for US customers, and **wire transfers** for Enterprise plans. All payments are processed securely through Stripe.',
    sortOrder: 2,
  },
  {
    id: 3,
    question: 'Is there a free trial?',
    answer:
      'Yes! We offer a **14-day free trial** with full access to all features. No credit card required to start. At the end of your trial, you can choose a plan that fits your needs.',
    sortOrder: 3,
  },
  {
    id: 4,
    question: 'What happens when my trial ends?',
    answer:
      "When your trial ends, your data is preserved but your account becomes **read-only**. You can view and export your data, but you won't be able to make changes until you subscribe. We'll send you reminders before your trial ends.",
    sortOrder: 4,
  },
  {
    id: 5,
    question: 'Is there a discount for annual billing?',
    answer:
      'Yes, annual billing saves you **20%** compared to monthly billing. This discount applies to all paid plans. You can switch between monthly and annual billing at any time.',
    sortOrder: 5,
  },

  // ====================================
  // FEATURE QUESTIONS
  // ====================================
  {
    id: 6,
    question: "What's included in TTB compliance?",
    answer:
      'Our TTB compliance features include:\n- **Automated form generation** for TTB-5110.40 (Operations Reports), TTB-5110.11 (Processing Records), and more\n- **Gauge records** with automatic calculations\n- **Audit trail** for all inventory movements\n- **Excise tax calculations** based on current rates\n- **DSP permit tracking** and renewal reminders',
    sortOrder: 6,
  },
  {
    id: 7,
    question: 'Can I import my existing data?',
    answer:
      'Absolutely! We support **CSV import** for barrels, inventory, and customer data. Our Growth and Business plans include **migration assistance** where our team helps you transfer data from spreadsheets or other systems. Enterprise customers get **dedicated migration support**.',
    sortOrder: 7,
  },
  {
    id: 8,
    question: 'Do you offer training?',
    answer:
      'Yes! All plans include access to our **comprehensive documentation**, video tutorials, and knowledge base. Growth plan customers get a **1-on-1 onboarding call**. Business and Enterprise customers receive **custom training sessions** and dedicated customer success support.',
    sortOrder: 8,
  },

  // ====================================
  // TECHNICAL QUESTIONS
  // ====================================
  {
    id: 9,
    question: 'Is my data secure?',
    answer:
      'Security is our top priority. We use:\n- **SOC 2 Type II** certified infrastructure\n- **256-bit AES encryption** at rest\n- **TLS 1.3** encryption in transit\n- **Daily backups** with point-in-time recovery\n- **Role-based access controls** for team members\n\nEnterprise customers also get SSO, audit logs, and custom security configurations.',
    sortOrder: 9,
  },
  {
    id: 10,
    question: "What's your uptime guarantee?",
    answer:
      'We offer tier-based SLAs:\n- **Starter**: 99.5% uptime\n- **Growth**: 99.9% uptime\n- **Business**: 99.95% uptime\n- **Enterprise**: 99.99% uptime with credits\n\nYou can view our current status at [status.caskr.com](https://status.caskr.com).',
    sortOrder: 10,
  },
  {
    id: 11,
    question: 'Can I export my data?',
    answer:
      "Yes, you own your data and can **export it anytime**. We support CSV, Excel, and JSON exports for all your data including barrels, production records, sales, and reports. There's **no lock-in** - your data is always accessible.",
    sortOrder: 11,
  },

  // ====================================
  // PRICING QUESTIONS
  // ====================================
  {
    id: 12,
    question: 'Why usage-based limits?',
    answer:
      "Usage-based pricing ensures you only pay for what you use. It's **fair and scalable** - small distilleries pay less, and as you grow, your plan grows with you. Limits are based on typical usage patterns for each tier.",
    sortOrder: 12,
  },
  {
    id: 13,
    question: "What counts as a 'case' for production limits?",
    answer:
      "A 'case' is calculated as a **standard 9-liter case equivalent** (9L = 2.38 gallons). This is the industry standard measurement. For example, if you produce 100 gallons, that counts as approximately 42 cases (100 ÷ 2.38).",
    sortOrder: 13,
  },
  {
    id: 14,
    question: 'Do you offer nonprofit or educational discounts?',
    answer:
      'Yes, we offer **special pricing** for nonprofit organizations, educational institutions, and craft distillery associations. [Contact our team](/contact) with details about your organization to learn more.',
    sortOrder: 14,
  },
  {
    id: 15,
    question: 'Can I pause my subscription?',
    answer:
      "We don't offer subscription pausing, but you can **downgrade** to a lower tier if you need to reduce costs temporarily. Your data is always preserved, and you can upgrade again whenever you're ready.",
    sortOrder: 15,
  },
];

/**
 * Get FAQs filtered by category keyword
 */
export function getFaqsByCategory(category: 'billing' | 'features' | 'technical' | 'pricing'): PricingFaq[] {
  const categoryRanges = {
    billing: [1, 5],
    features: [6, 8],
    technical: [9, 11],
    pricing: [12, 15],
  };

  const [start, end] = categoryRanges[category];
  return defaultPricingFaqs.filter(faq => faq.id >= start && faq.id <= end);
}

export default defaultPricingFaqs;
