import { useState, useCallback, createContext, useContext, useMemo } from 'react';
import { AccordionItem } from './AccordionItem';
import './Accordion.css';

export interface AccordionItemData {
  id: string;
  title: string;
  content: string | React.ReactNode;
  defaultOpen?: boolean;
}

export interface AccordionProps {
  items: AccordionItemData[];
  allowMultiple?: boolean;
  defaultOpenIndex?: number;
  className?: string;
  variant?: 'default' | 'bordered' | 'separated';
}

interface AccordionContextType {
  openItems: Set<string>;
  toggleItem: (id: string) => void;
  allowMultiple: boolean;
}

const AccordionContext = createContext<AccordionContextType | null>(null);

export function useAccordionContext() {
  const context = useContext(AccordionContext);
  if (!context) {
    throw new Error('AccordionItem must be used within an Accordion');
  }
  return context;
}

/**
 * Reusable accordion component.
 * Supports single or multiple open items.
 * Smooth height animations with reduced motion support.
 */
export function Accordion({
  items,
  allowMultiple = false,
  defaultOpenIndex,
  className = '',
  variant = 'default',
}: AccordionProps) {
  // Initialize open items based on defaultOpenIndex and defaultOpen props
  const [openItems, setOpenItems] = useState<Set<string>>(() => {
    const initial = new Set<string>();

    // Check defaultOpenIndex
    if (defaultOpenIndex !== undefined && items[defaultOpenIndex]) {
      initial.add(items[defaultOpenIndex].id);
    }

    // Check defaultOpen on individual items
    items.forEach(item => {
      if (item.defaultOpen) {
        if (allowMultiple) {
          initial.add(item.id);
        } else if (initial.size === 0) {
          initial.add(item.id);
        }
      }
    });

    return initial;
  });

  const toggleItem = useCallback((id: string) => {
    setOpenItems(prev => {
      const next = new Set(prev);

      if (next.has(id)) {
        next.delete(id);
      } else {
        if (!allowMultiple) {
          next.clear();
        }
        next.add(id);
      }

      return next;
    });
  }, [allowMultiple]);

  const contextValue = useMemo(() => ({
    openItems,
    toggleItem,
    allowMultiple,
  }), [openItems, toggleItem, allowMultiple]);

  if (items.length === 0) {
    return null;
  }

  return (
    <AccordionContext.Provider value={contextValue}>
      <div
        className={`accordion accordion-${variant} ${className}`}
        role="region"
        aria-label="Accordion"
      >
        {items.map((item, index) => (
          <AccordionItem
            key={item.id}
            id={item.id}
            title={item.title}
            content={item.content}
            isFirst={index === 0}
            isLast={index === items.length - 1}
          />
        ))}
      </div>
    </AccordionContext.Provider>
  );
}

export default Accordion;
