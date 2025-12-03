import { useRef, useEffect, useState } from 'react';
import { useAccordionContext } from './Accordion';

export interface AccordionItemProps {
  id: string;
  title: string;
  content: string | React.ReactNode;
  isFirst?: boolean;
  isLast?: boolean;
}

/**
 * Individual accordion item with smooth height animation.
 * Supports keyboard navigation and accessibility.
 */
export function AccordionItem({
  id,
  title,
  content,
  isFirst = false,
  isLast = false,
}: AccordionItemProps) {
  const { openItems, toggleItem } = useAccordionContext();
  const isOpen = openItems.has(id);

  const contentRef = useRef<HTMLDivElement>(null);
  const [height, setHeight] = useState<number>(0);
  const [isAnimating, setIsAnimating] = useState(false);

  // Calculate content height for animation
  useEffect(() => {
    if (contentRef.current) {
      const contentHeight = contentRef.current.scrollHeight;

      if (isOpen) {
        setIsAnimating(true);
        setHeight(contentHeight);

        // After animation completes, set to auto for dynamic content
        const timer = setTimeout(() => {
          setIsAnimating(false);
        }, 300);

        return () => clearTimeout(timer);
      } else {
        setIsAnimating(true);
        setHeight(0);

        const timer = setTimeout(() => {
          setIsAnimating(false);
        }, 300);

        return () => clearTimeout(timer);
      }
    }
  }, [isOpen]);

  // Recalculate height if content changes while open
  useEffect(() => {
    if (isOpen && contentRef.current && !isAnimating) {
      setHeight(contentRef.current.scrollHeight);
    }
  }, [content, isOpen, isAnimating]);

  const handleClick = () => {
    toggleItem(id);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      toggleItem(id);
    }
  };

  return (
    <div
      className={`accordion-item ${isOpen ? 'open' : ''} ${isFirst ? 'first' : ''} ${isLast ? 'last' : ''}`}
    >
      <button
        type="button"
        className="accordion-header"
        onClick={handleClick}
        onKeyDown={handleKeyDown}
        aria-expanded={isOpen}
        aria-controls={`accordion-content-${id}`}
        id={`accordion-header-${id}`}
      >
        <span className="accordion-title">{title}</span>
        <span className={`accordion-icon ${isOpen ? 'open' : ''}`} aria-hidden="true">
          <ChevronIcon />
        </span>
      </button>

      <div
        id={`accordion-content-${id}`}
        role="region"
        aria-labelledby={`accordion-header-${id}`}
        className="accordion-content-wrapper"
        style={{
          height: isAnimating ? height : (isOpen ? 'auto' : 0),
          overflow: isAnimating ? 'hidden' : (isOpen ? 'visible' : 'hidden'),
        }}
      >
        <div ref={contentRef} className="accordion-content">
          {typeof content === 'string' ? (
            <p>{content}</p>
          ) : (
            content
          )}
        </div>
      </div>
    </div>
  );
}

function ChevronIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden="true">
      <path
        d="M5 7.5L10 12.5L15 7.5"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

export default AccordionItem;
