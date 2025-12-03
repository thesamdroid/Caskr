import { useState, useRef, useEffect, useCallback, useMemo } from 'react';
import { PricingTier, PricingFeatureCategory, PricingFeature } from '../../types/pricing';
import './FeatureAccordion.css';

export interface FeatureAccordionProps {
  tiers: PricingTier[];
  featuresByCategory: PricingFeatureCategory[];
  mode?: 'by-tier' | 'by-category';
  defaultTierId?: number;
}

/**
 * Mobile-optimized feature display.
 * Two modes:
 * - by-tier: Select a tier, see all its features
 * - by-category: Expand categories, see features for all tiers
 */
export function FeatureAccordion({
  tiers,
  featuresByCategory,
  mode = 'by-tier',
  defaultTierId,
}: FeatureAccordionProps) {
  const sortedTiers = useMemo(() => {
    return [...tiers].sort((a, b) => a.sortOrder - b.sortOrder);
  }, [tiers]);

  const [selectedTierId, setSelectedTierId] = useState<number>(
    defaultTierId ?? sortedTiers[0]?.id ?? 0
  );

  const [expandedCategories, setExpandedCategories] = useState<Set<string>>(new Set());

  const toggleCategory = useCallback((category: string) => {
    setExpandedCategories(prev => {
      const next = new Set(prev);
      if (next.has(category)) {
        next.delete(category);
      } else {
        next.add(category);
      }
      return next;
    });
  }, []);

  const expandAll = useCallback(() => {
    setExpandedCategories(new Set(featuresByCategory.map(c => c.category)));
  }, [featuresByCategory]);

  const collapseAll = useCallback(() => {
    setExpandedCategories(new Set());
  }, []);

  if (mode === 'by-tier') {
    return (
      <ByTierMode
        tiers={sortedTiers}
        featuresByCategory={featuresByCategory}
        selectedTierId={selectedTierId}
        onTierChange={setSelectedTierId}
      />
    );
  }

  return (
    <ByCategoryMode
      tiers={sortedTiers}
      featuresByCategory={featuresByCategory}
      expandedCategories={expandedCategories}
      onToggleCategory={toggleCategory}
      onExpandAll={expandAll}
      onCollapseAll={collapseAll}
    />
  );
}

// ====================================
// BY TIER MODE
// ====================================

interface ByTierModeProps {
  tiers: PricingTier[];
  featuresByCategory: PricingFeatureCategory[];
  selectedTierId: number;
  onTierChange: (tierId: number) => void;
}

function ByTierMode({
  tiers,
  featuresByCategory,
  selectedTierId,
  onTierChange,
}: ByTierModeProps) {
  const selectedTier = tiers.find(t => t.id === selectedTierId);

  return (
    <div className="feature-accordion by-tier">
      {/* Tier selector tabs */}
      <div className="tier-selector" role="tablist" aria-label="Select pricing tier">
        {tiers.map(tier => (
          <button
            key={tier.id}
            type="button"
            role="tab"
            aria-selected={selectedTierId === tier.id}
            aria-controls={`tier-panel-${tier.id}`}
            className={`tier-tab ${selectedTierId === tier.id ? 'active' : ''} ${tier.isPopular ? 'popular' : ''}`}
            onClick={() => onTierChange(tier.id)}
          >
            {tier.name}
            {tier.isPopular && <span className="tier-tab-badge">Popular</span>}
          </button>
        ))}
      </div>

      {/* Feature list for selected tier */}
      {selectedTier && (
        <div
          id={`tier-panel-${selectedTier.id}`}
          role="tabpanel"
          aria-labelledby={`tier-tab-${selectedTier.id}`}
          className="tier-features-panel"
        >
          {featuresByCategory.map(category => (
            <TierCategorySection
              key={category.category}
              category={category}
              tier={selectedTier}
            />
          ))}
        </div>
      )}
    </div>
  );
}

interface TierCategorySectionProps {
  category: PricingFeatureCategory;
  tier: PricingTier;
}

function TierCategorySection({ category, tier }: TierCategorySectionProps) {
  const [isExpanded, setIsExpanded] = useState(true);
  const contentRef = useRef<HTMLDivElement>(null);
  const [height, setHeight] = useState<number | undefined>(undefined);

  useEffect(() => {
    if (contentRef.current) {
      setHeight(isExpanded ? contentRef.current.scrollHeight : 0);
    }
  }, [isExpanded]);

  const getFeatureValue = (featureId: number) => {
    return tier.features.find(f => f.featureId === featureId);
  };

  return (
    <div className="tier-category">
      <button
        type="button"
        className="tier-category-header"
        onClick={() => setIsExpanded(!isExpanded)}
        aria-expanded={isExpanded}
      >
        <span className="tier-category-name">{category.category}</span>
        <span className={`tier-category-chevron ${isExpanded ? '' : 'collapsed'}`}>
          <ChevronIcon />
        </span>
      </button>
      <div
        className="tier-category-content"
        style={{ height }}
        aria-hidden={!isExpanded}
      >
        <div ref={contentRef}>
          {category.features.map(feature => {
            const value = getFeatureValue(feature.id);
            return (
              <TierFeatureItem
                key={feature.id}
                feature={feature}
                value={value}
              />
            );
          })}
        </div>
      </div>
    </div>
  );
}

interface TierFeatureItemProps {
  feature: PricingFeature;
  value?: {
    isIncluded: boolean;
    limitValue?: string;
  };
}

function TierFeatureItem({ feature, value }: TierFeatureItemProps) {
  return (
    <div className="tier-feature-item">
      <div className="tier-feature-info">
        <span className="tier-feature-name">{feature.name}</span>
        {feature.description && (
          <span className="tier-feature-description">{feature.description}</span>
        )}
      </div>
      <div className="tier-feature-value">
        {value?.limitValue ? (
          <span className="tier-feature-limit">{value.limitValue}</span>
        ) : value?.isIncluded ? (
          <span className="tier-feature-check" aria-label="Included">
            <CheckIcon />
          </span>
        ) : (
          <span className="tier-feature-x" aria-label="Not included">
            <XIcon />
          </span>
        )}
      </div>
    </div>
  );
}

// ====================================
// BY CATEGORY MODE
// ====================================

interface ByCategoryModeProps {
  tiers: PricingTier[];
  featuresByCategory: PricingFeatureCategory[];
  expandedCategories: Set<string>;
  onToggleCategory: (category: string) => void;
  onExpandAll: () => void;
  onCollapseAll: () => void;
}

function ByCategoryMode({
  tiers,
  featuresByCategory,
  expandedCategories,
  onToggleCategory,
  onExpandAll,
  onCollapseAll,
}: ByCategoryModeProps) {
  const allExpanded = expandedCategories.size === featuresByCategory.length;

  return (
    <div className="feature-accordion by-category">
      {/* Expand/Collapse all buttons */}
      <div className="accordion-controls">
        <button
          type="button"
          className="accordion-control-btn"
          onClick={allExpanded ? onCollapseAll : onExpandAll}
        >
          {allExpanded ? 'Collapse All' : 'Expand All'}
        </button>
      </div>

      {/* Category accordions */}
      <div className="category-accordion-list">
        {featuresByCategory.map(category => (
          <CategoryAccordionItem
            key={category.category}
            category={category}
            tiers={tiers}
            isExpanded={expandedCategories.has(category.category)}
            onToggle={() => onToggleCategory(category.category)}
          />
        ))}
      </div>
    </div>
  );
}

interface CategoryAccordionItemProps {
  category: PricingFeatureCategory;
  tiers: PricingTier[];
  isExpanded: boolean;
  onToggle: () => void;
}

function CategoryAccordionItem({
  category,
  tiers,
  isExpanded,
  onToggle,
}: CategoryAccordionItemProps) {
  const contentRef = useRef<HTMLDivElement>(null);
  const [height, setHeight] = useState<number | undefined>(undefined);

  useEffect(() => {
    if (contentRef.current) {
      setHeight(isExpanded ? contentRef.current.scrollHeight : 0);
    }
  }, [isExpanded]);

  return (
    <div className={`category-accordion-item ${isExpanded ? 'expanded' : ''}`}>
      <button
        type="button"
        className="category-accordion-header"
        onClick={onToggle}
        aria-expanded={isExpanded}
      >
        <span className="category-accordion-name">{category.category}</span>
        <span className={`category-accordion-chevron ${isExpanded ? '' : 'collapsed'}`}>
          <ChevronIcon />
        </span>
      </button>
      <div
        className="category-accordion-content"
        style={{ height }}
        aria-hidden={!isExpanded}
      >
        <div ref={contentRef}>
          {category.features.map(feature => (
            <CategoryFeatureRow
              key={feature.id}
              feature={feature}
              tiers={tiers}
            />
          ))}
        </div>
      </div>
    </div>
  );
}

interface CategoryFeatureRowProps {
  feature: PricingFeature;
  tiers: PricingTier[];
}

function CategoryFeatureRow({ feature, tiers }: CategoryFeatureRowProps) {
  const getFeatureValue = (tier: PricingTier) => {
    return tier.features.find(f => f.featureId === feature.id);
  };

  return (
    <div className="category-feature-row">
      <div className="category-feature-name-col">
        <span className="category-feature-name">{feature.name}</span>
        {feature.description && (
          <span className="category-feature-description">{feature.description}</span>
        )}
      </div>
      <div className="category-feature-values">
        {tiers.map(tier => {
          const value = getFeatureValue(tier);
          return (
            <div key={tier.id} className="category-feature-value-item">
              <span className="category-feature-tier-name">{tier.name}</span>
              {value?.limitValue ? (
                <span className="category-feature-limit">{value.limitValue}</span>
              ) : value?.isIncluded ? (
                <span className="category-feature-check" aria-label={`${tier.name}: Included`}>
                  <CheckIcon />
                </span>
              ) : (
                <span className="category-feature-x" aria-label={`${tier.name}: Not included`}>
                  <XIcon />
                </span>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}

// ====================================
// ICONS
// ====================================

function ChevronIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
      <path
        d="M4 6L8 10L12 6"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function CheckIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 18 18" fill="none" aria-hidden="true">
      <path
        d="M15 4.5L6.75 12.75L3 9"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function XIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 18 18" fill="none" aria-hidden="true">
      <path
        d="M4.5 4.5L13.5 13.5M13.5 4.5L4.5 13.5"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
      />
    </svg>
  );
}

export default FeatureAccordion;
