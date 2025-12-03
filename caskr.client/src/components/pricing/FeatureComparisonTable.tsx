import { useState, useMemo, useCallback, useRef, useEffect } from 'react';
import { PricingTier, PricingFeatureCategory, BillingPeriod } from '../../types/pricing';
import { ComparisonTableHeader } from './ComparisonTableHeader';
import { FeatureRow, FeatureValueData } from './FeatureRow';
import { CategoryHeader } from './CategoryHeader';
import { FeatureAccordion } from './FeatureAccordion';
import './FeatureComparisonTable.css';

export interface FeatureComparisonTableProps {
  tiers: PricingTier[];
  featuresByCategory: PricingFeatureCategory[];
  billingPeriod?: BillingPeriod;
  loading?: boolean;
  error?: string | null;
  onRetry?: () => void;
  mobileMode?: 'scroll' | 'accordion-tier' | 'accordion-category';
  showCompareMode?: boolean;
}

interface CollapsedCategories {
  [key: string]: boolean;
}

/**
 * Feature comparison table with all features:
 * - Sticky header and first column
 * - Collapsible categories
 * - Tier highlighting
 * - Mobile accordion modes
 * - Loading and error states
 * - Print optimization
 */
export function FeatureComparisonTable({
  tiers,
  featuresByCategory,
  billingPeriod = 'monthly',
  loading = false,
  error = null,
  onRetry,
  mobileMode = 'accordion-tier',
  showCompareMode = false,
}: FeatureComparisonTableProps) {
  const [collapsedCategories, setCollapsedCategories] = useState<CollapsedCategories>({});
  const [highlightedTierId, setHighlightedTierId] = useState<number | null>(null);
  const [isMobileView, setIsMobileView] = useState(false);
  const [isScrolledX, setIsScrolledX] = useState(false);
  const [compareMode, setCompareMode] = useState(false);
  const [selectedTiers, setSelectedTiers] = useState<Set<number>>(new Set());
  const wrapperRef = useRef<HTMLDivElement>(null);

  // Sort tiers by sortOrder
  const sortedTiers = useMemo(() => {
    return [...tiers].sort((a, b) => a.sortOrder - b.sortOrder);
  }, [tiers]);

  // Filter tiers for compare mode
  const displayTiers = useMemo(() => {
    if (compareMode && selectedTiers.size > 0) {
      return sortedTiers.filter(t => selectedTiers.has(t.id));
    }
    return sortedTiers;
  }, [sortedTiers, compareMode, selectedTiers]);

  // Check for mobile view
  useEffect(() => {
    const checkMobile = () => setIsMobileView(window.innerWidth < 768);
    checkMobile();
    window.addEventListener('resize', checkMobile);
    return () => window.removeEventListener('resize', checkMobile);
  }, []);

  // Track horizontal scroll for sticky column shadow
  useEffect(() => {
    const wrapper = wrapperRef.current;
    if (!wrapper) return;

    const handleScroll = () => {
      setIsScrolledX(wrapper.scrollLeft > 0);
    };

    wrapper.addEventListener('scroll', handleScroll);
    return () => wrapper.removeEventListener('scroll', handleScroll);
  }, []);

  const toggleCategory = useCallback((category: string) => {
    setCollapsedCategories(prev => ({
      ...prev,
      [category]: !prev[category],
    }));
  }, []);

  const expandAllCategories = useCallback(() => {
    const allCollapsed: CollapsedCategories = {};
    featuresByCategory.forEach(c => {
      allCollapsed[c.category] = false;
    });
    setCollapsedCategories(allCollapsed);
  }, [featuresByCategory]);

  const collapseAllCategories = useCallback(() => {
    const allCollapsed: CollapsedCategories = {};
    featuresByCategory.forEach(c => {
      allCollapsed[c.category] = true;
    });
    setCollapsedCategories(allCollapsed);
  }, [featuresByCategory]);

  const toggleCompareMode = useCallback(() => {
    setCompareMode(prev => !prev);
    if (compareMode) {
      setSelectedTiers(new Set());
    }
  }, [compareMode]);

  const toggleTierSelection = useCallback((tierId: number) => {
    setSelectedTiers(prev => {
      const next = new Set(prev);
      if (next.has(tierId)) {
        next.delete(tierId);
      } else if (next.size < 3) {
        // Limit to 3 tiers in compare mode
        next.add(tierId);
      }
      return next;
    });
  }, []);

  // Get feature values for a feature across all tiers
  const getFeatureValues = useCallback((featureId: number): FeatureValueData[] => {
    return displayTiers.map(tier => {
      const feature = tier.features.find(f => f.featureId === featureId);
      return {
        tierId: tier.id,
        isIncluded: feature?.isIncluded ?? false,
        limitValue: feature?.limitValue,
      };
    });
  }, [displayTiers]);

  // Loading state
  if (loading) {
    return (
      <section className="feature-comparison-section" aria-busy="true">
        <div className="feature-comparison-header">
          <h2 className="section-title">Compare Plans</h2>
          <p className="section-subtitle">See what's included in each plan</p>
        </div>
        <TableSkeleton tierCount={4} categoryCount={3} featuresPerCategory={3} />
      </section>
    );
  }

  // Error state
  if (error) {
    return (
      <section className="feature-comparison-section">
        <div className="feature-comparison-header">
          <h2 className="section-title">Compare Plans</h2>
        </div>
        <div className="feature-comparison-error" role="alert">
          <p>{error}</p>
          {onRetry && (
            <button type="button" className="button-secondary" onClick={onRetry}>
              Retry
            </button>
          )}
        </div>
      </section>
    );
  }

  // Empty state
  if (sortedTiers.length === 0 || featuresByCategory.length === 0) {
    return (
      <section className="feature-comparison-section">
        <div className="feature-comparison-header">
          <h2 className="section-title">Compare Plans</h2>
        </div>
        <div className="feature-comparison-empty">
          <p>No features available to compare.</p>
        </div>
      </section>
    );
  }

  // Mobile accordion view
  if (isMobileView && mobileMode !== 'scroll') {
    return (
      <section className="feature-comparison-section" aria-labelledby="feature-comparison-title">
        <div className="feature-comparison-header">
          <h2 id="feature-comparison-title" className="section-title">
            Compare Plans
          </h2>
          <p className="section-subtitle">
            See what's included in each plan
          </p>
        </div>
        <FeatureAccordion
          tiers={sortedTiers}
          featuresByCategory={featuresByCategory}
          mode={mobileMode === 'accordion-category' ? 'by-category' : 'by-tier'}
        />
      </section>
    );
  }

  // Desktop table view (or mobile scroll view)
  return (
    <section className="feature-comparison-section" aria-labelledby="feature-comparison-title">
      <div className="feature-comparison-header">
        <h2 id="feature-comparison-title" className="section-title">
          Compare Plans
        </h2>
        <p className="section-subtitle">
          See what's included in each plan
        </p>
      </div>

      {/* Table controls */}
      <div className="feature-comparison-controls">
        <div className="controls-left">
          <button
            type="button"
            className="control-btn"
            onClick={expandAllCategories}
          >
            Expand All
          </button>
          <button
            type="button"
            className="control-btn"
            onClick={collapseAllCategories}
          >
            Collapse All
          </button>
        </div>

        {showCompareMode && (
          <div className="controls-right">
            <button
              type="button"
              className={`control-btn ${compareMode ? 'active' : ''}`}
              onClick={toggleCompareMode}
            >
              {compareMode ? 'Exit Compare' : 'Compare Plans'}
            </button>
          </div>
        )}
      </div>

      {/* Compare mode tier selector */}
      {compareMode && (
        <div className="compare-tier-selector">
          <p className="compare-instruction">Select 2-3 plans to compare:</p>
          <div className="compare-tier-buttons">
            {sortedTiers.map(tier => (
              <button
                key={tier.id}
                type="button"
                className={`compare-tier-btn ${selectedTiers.has(tier.id) ? 'selected' : ''}`}
                onClick={() => toggleTierSelection(tier.id)}
                disabled={!selectedTiers.has(tier.id) && selectedTiers.size >= 3}
              >
                {tier.name}
                {selectedTiers.has(tier.id) && <CheckIcon />}
              </button>
            ))}
          </div>
        </div>
      )}

      <div
        ref={wrapperRef}
        className={`feature-comparison-wrapper ${isScrolledX ? 'scrolled-x' : ''}`}
      >
        <table className="feature-comparison-table" role="grid">
          <ComparisonTableHeader
            tiers={displayTiers}
            billingPeriod={billingPeriod}
            highlightedTierId={highlightedTierId}
            onTierHighlight={setHighlightedTierId}
          />
          <tbody>
            {featuresByCategory.map((category, categoryIndex) => {
              const isCollapsed = !!collapsedCategories[category.category];
              const categoryId = category.category.replace(/\s+/g, '-').toLowerCase();

              return (
                <CategorySection
                  key={category.category}
                  category={category}
                  categoryId={categoryId}
                  isCollapsed={isCollapsed}
                  onToggle={() => toggleCategory(category.category)}
                  tiers={displayTiers}
                  highlightedTierId={highlightedTierId}
                  getFeatureValues={getFeatureValues}
                  categoryIndex={categoryIndex}
                />
              );
            })}
          </tbody>
        </table>
      </div>
    </section>
  );
}

// ====================================
// CATEGORY SECTION
// ====================================

interface CategorySectionProps {
  category: PricingFeatureCategory;
  categoryId: string;
  isCollapsed: boolean;
  onToggle: () => void;
  tiers: PricingTier[];
  highlightedTierId: number | null;
  getFeatureValues: (featureId: number) => FeatureValueData[];
  categoryIndex: number;
}

function CategorySection({
  category,
  categoryId,
  isCollapsed,
  onToggle,
  tiers,
  highlightedTierId,
  getFeatureValues,
  categoryIndex,
}: CategorySectionProps) {
  return (
    <>
      <CategoryHeader
        category={category.category}
        isCollapsed={isCollapsed}
        onToggle={onToggle}
        colSpan={tiers.length + 1}
        categoryId={categoryId}
      />
      {!isCollapsed && (
        <>
          {category.features.map((feature, featureIndex) => (
            <FeatureRow
              key={feature.id}
              feature={{
                name: feature.name,
                description: feature.description,
                category: category.category,
              }}
              values={getFeatureValues(feature.id)}
              tiers={tiers}
              highlightedTierId={highlightedTierId}
              zebra={featureIndex % 2 === 1}
            />
          ))}
        </>
      )}
    </>
  );
}

// ====================================
// SKELETON LOADER
// ====================================

interface TableSkeletonProps {
  tierCount: number;
  categoryCount: number;
  featuresPerCategory: number;
}

function TableSkeleton({ tierCount, categoryCount, featuresPerCategory }: TableSkeletonProps) {
  return (
    <div className="feature-comparison-skeleton" aria-label="Loading comparison table">
      <div className="skeleton-header">
        <div className="skeleton-cell feature-col" />
        {Array.from({ length: tierCount }).map((_, i) => (
          <div key={i} className="skeleton-cell tier-col" />
        ))}
      </div>
      {Array.from({ length: categoryCount }).map((_, ci) => (
        <div key={ci} className="skeleton-category">
          <div className="skeleton-category-header" />
          {Array.from({ length: featuresPerCategory }).map((_, fi) => (
            <div key={fi} className="skeleton-row">
              <div className="skeleton-cell feature-col" />
              {Array.from({ length: tierCount }).map((_, ti) => (
                <div key={ti} className="skeleton-cell value-col" />
              ))}
            </div>
          ))}
        </div>
      ))}
    </div>
  );
}

// ====================================
// ICONS
// ====================================

function CheckIcon() {
  return (
    <svg width="14" height="14" viewBox="0 0 14 14" fill="none" aria-hidden="true">
      <path
        d="M11 4L5.5 9.5L3 7"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

export default FeatureComparisonTable;
