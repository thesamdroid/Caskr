import { useMemo, useState, useCallback } from 'react';
import { PricingFaq as PricingFaqType } from '../../types/pricing';
import { Accordion, AccordionItemData } from '../common/Accordion';
import { createMarkdownHtml } from '../../utils/markdownRenderer';
import './PricingFaq.css';

export interface PricingFaqProps {
  faqs: PricingFaqType[];
  allowMultiple?: boolean;
  defaultOpenIndex?: number;
  showSearch?: boolean;
  /** Callback when a FAQ item is opened (for analytics) */
  onFaqOpen?: (questionId: string, questionText: string) => void;
}

/**
 * FAQ section for pricing page.
 * Uses the reusable Accordion component with markdown support.
 */
export function PricingFaq({
  faqs,
  allowMultiple = false,
  defaultOpenIndex,
  showSearch = false,
  onFaqOpen,
}: PricingFaqProps) {
  const [searchQuery, setSearchQuery] = useState('');

  // Sort FAQs by sortOrder
  const sortedFaqs = useMemo(() => {
    return [...faqs].sort((a, b) => a.sortOrder - b.sortOrder);
  }, [faqs]);

  // Filter FAQs based on search query
  const filteredFaqs = useMemo(() => {
    if (!searchQuery.trim()) {
      return sortedFaqs;
    }

    const query = searchQuery.toLowerCase();
    return sortedFaqs.filter(faq =>
      faq.question.toLowerCase().includes(query) ||
      faq.answer.toLowerCase().includes(query)
    );
  }, [sortedFaqs, searchQuery]);

  // Convert FAQs to AccordionItemData format
  const accordionItems: AccordionItemData[] = useMemo(() => {
    return filteredFaqs.map(faq => ({
      id: String(faq.id),
      title: highlightText(faq.question, searchQuery),
      content: <FaqAnswer answer={faq.answer} searchQuery={searchQuery} />,
    }));
  }, [filteredFaqs, searchQuery]);

  const handleSearchChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchQuery(e.target.value);
  }, []);

  const clearSearch = useCallback(() => {
    setSearchQuery('');
  }, []);

  if (faqs.length === 0) {
    return null;
  }

  return (
    <section className="pricing-faq-section" aria-labelledby="faq-title">
      <div className="pricing-faq-container">
        <header className="pricing-faq-header">
          <h2 id="faq-title" className="section-title">
            Frequently Asked Questions
          </h2>
          <p className="section-subtitle">
            Everything you need to know about our pricing
          </p>
        </header>

        {/* Search input (optional) */}
        {showSearch && (
          <div className="pricing-faq-search">
            <div className="faq-search-input-wrapper">
              <SearchIcon />
              <input
                type="search"
                className="faq-search-input"
                placeholder="Search FAQs..."
                value={searchQuery}
                onChange={handleSearchChange}
                aria-label="Search frequently asked questions"
              />
              {searchQuery && (
                <button
                  type="button"
                  className="faq-search-clear"
                  onClick={clearSearch}
                  aria-label="Clear search"
                >
                  <ClearIcon />
                </button>
              )}
            </div>
            {searchQuery && filteredFaqs.length === 0 && (
              <p className="faq-no-results">
                No questions match "{searchQuery}". Try a different search term.
              </p>
            )}
            {searchQuery && filteredFaqs.length > 0 && (
              <p className="faq-results-count">
                {filteredFaqs.length} {filteredFaqs.length === 1 ? 'result' : 'results'} found
              </p>
            )}
          </div>
        )}

        {/* FAQ Accordion */}
        {accordionItems.length > 0 && (
          <Accordion
            items={accordionItems}
            allowMultiple={allowMultiple}
            defaultOpenIndex={defaultOpenIndex}
            variant="separated"
            className="pricing-faq-accordion"
            onItemOpen={onFaqOpen}
          />
        )}

        {/* Contact CTA */}
        <div className="pricing-faq-contact">
          <p>
            Still have questions?{' '}
            <a href="/contact" className="pricing-faq-link">
              Contact our team
            </a>
          </p>
        </div>
      </div>
    </section>
  );
}

/**
 * FAQ answer component with markdown rendering
 */
interface FaqAnswerProps {
  answer: string;
  searchQuery?: string;
}

function FaqAnswer({ answer, searchQuery }: FaqAnswerProps) {
  const htmlContent = useMemo(() => {
    let html = createMarkdownHtml(answer).__html;

    // Highlight search matches in rendered content
    if (searchQuery && searchQuery.trim()) {
      const query = searchQuery.trim();
      const regex = new RegExp(`(${escapeRegex(query)})`, 'gi');
      html = html.replace(regex, '<mark class="faq-highlight">$1</mark>');
    }

    return { __html: html };
  }, [answer, searchQuery]);

  return (
    <div
      className="faq-answer-content"
      dangerouslySetInnerHTML={htmlContent}
    />
  );
}

/**
 * Highlight matching text in question (for search)
 */
function highlightText(text: string, query: string): string | React.ReactNode {
  if (!query || !query.trim()) {
    return text;
  }

  const regex = new RegExp(`(${escapeRegex(query.trim())})`, 'gi');
  const parts = text.split(regex);

  if (parts.length === 1) {
    return text;
  }

  return (
    <>
      {parts.map((part, index) =>
        regex.test(part) ? (
          <mark key={index} className="faq-highlight">
            {part}
          </mark>
        ) : (
          part
        )
      )}
    </>
  );
}

/**
 * Escape special regex characters
 */
function escapeRegex(string: string): string {
  return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

// Icons
function SearchIcon() {
  return (
    <svg
      width="18"
      height="18"
      viewBox="0 0 18 18"
      fill="none"
      aria-hidden="true"
      className="faq-search-icon"
    >
      <circle cx="8" cy="8" r="5.5" stroke="currentColor" strokeWidth="1.5" />
      <path
        d="M12 12L16 16"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

function ClearIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
      <path
        d="M4 4L12 12M12 4L4 12"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

export default PricingFaq;
