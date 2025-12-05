import { useState, useCallback, useRef, useEffect } from 'react';
import { PricingFaq as PricingFaqType } from '../../../types/pricing';
import './PricingFaq.css';

export interface PricingFaqProps {
  faqs: PricingFaqType[];
  onFaqOpen?: (questionId: string, questionText: string) => void;
}

/**
 * Accordion component for FAQ items.
 * Only one item open at a time.
 * Smooth expand/collapse animation.
 */
export function PricingFaq({ faqs, onFaqOpen }: PricingFaqProps) {
  const [openId, setOpenId] = useState<number | null>(null);

  const handleToggle = useCallback((id: number, question: string) => {
    const isOpening = openId !== id;
    setOpenId(prev => (prev === id ? null : id));
    if (isOpening && onFaqOpen) {
      onFaqOpen(String(id), question);
    }
  }, [openId, onFaqOpen]);

  if (faqs.length === 0) {
    return null;
  }

  // Sort FAQs by sortOrder
  const sortedFaqs = [...faqs].sort((a, b) => a.sortOrder - b.sortOrder);

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

        <div className="pricing-faq-list" role="region" aria-label="FAQ accordion">
          {sortedFaqs.map(faq => (
            <FaqItem
              key={faq.id}
              faq={faq}
              isOpen={openId === faq.id}
              onToggle={() => handleToggle(faq.id, faq.question)}
            />
          ))}
        </div>

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

interface FaqItemProps {
  faq: PricingFaqType;
  isOpen: boolean;
  onToggle: () => void;
}

function FaqItem({ faq, isOpen, onToggle }: FaqItemProps) {
  const contentRef = useRef<HTMLDivElement>(null);
  const [height, setHeight] = useState<number | undefined>(undefined);

  // Calculate content height for smooth animation
  useEffect(() => {
    if (contentRef.current) {
      const contentHeight = contentRef.current.scrollHeight;
      setHeight(isOpen ? contentHeight : 0);
    }
  }, [isOpen]);

  // Handle keyboard navigation
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      onToggle();
    }
  };

  // Simple markdown-like rendering for answers
  const renderAnswer = (text: string) => {
    // Convert **bold** to <strong>
    let rendered = text.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
    // Convert *italic* to <em>
    rendered = rendered.replace(/\*(.*?)\*/g, '<em>$1</em>');
    // Convert line breaks to <br>
    rendered = rendered.replace(/\n/g, '<br />');
    return { __html: rendered };
  };

  return (
    <div className={`faq-item ${isOpen ? 'open' : ''}`}>
      <button
        type="button"
        className="faq-question"
        onClick={onToggle}
        onKeyDown={handleKeyDown}
        aria-expanded={isOpen}
        aria-controls={`faq-answer-${faq.id}`}
        id={`faq-question-${faq.id}`}
      >
        <span className="faq-question-text">{faq.question}</span>
        <span className={`faq-chevron ${isOpen ? 'open' : ''}`} aria-hidden="true">
          <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
            <path
              d="M5 7.5L10 12.5L15 7.5"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        </span>
      </button>
      <div
        id={`faq-answer-${faq.id}`}
        role="region"
        aria-labelledby={`faq-question-${faq.id}`}
        className="faq-answer-wrapper"
        style={{ height }}
      >
        <div ref={contentRef} className="faq-answer">
          <p dangerouslySetInnerHTML={renderAnswer(faq.answer)} />
        </div>
      </div>
    </div>
  );
}

export default PricingFaq;
