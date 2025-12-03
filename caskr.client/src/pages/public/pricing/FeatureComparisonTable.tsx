import { useState, useMemo, useCallback } from 'react';
import { PricingTier, PricingFeatureCategory, PricingFeature } from '../../../types/pricing';
import './FeatureComparisonTable.css';

export interface FeatureComparisonTableProps {
  tiers: PricingTier[];
  featuresByCategory: PricingFeatureCategory[];
}

interface CollapsedCategories {
  [key: string]: boolean;
}

/**
 * Get feature value for a tier
 */
function getFeatureValue(tier: PricingTier, featureId: number): { included: boolean; value: string | null } {
  const feature = tier.features.find(f => f.featureId === featureId);
  if (!feature) {
    return { included: false, value: null };
  }
  return {
    included: feature.isIncluded,
    value: feature.limitValue || null,
  };
}

/**
 * Feature comparison table with sticky header.
 * Collapsible categories on mobile.
 */
export function FeatureComparisonTable({
  tiers,
  featuresByCategory,
}: FeatureComparisonTableProps) {
  const [collapsedCategories, setCollapsedCategories] = useState<CollapsedCategories>({});
  const [isMobileView, setIsMobileView] = useState(false);

  // Sort tiers by sortOrder
  const sortedTiers = useMemo(() => {
    return [...tiers].sort((a, b) => a.sortOrder - b.sortOrder);
  }, [tiers]);

  // Check for mobile view on mount
  useMemo(() => {
    if (typeof window !== 'undefined') {
      const checkMobile = () => setIsMobileView(window.innerWidth < 768);
      checkMobile();
      window.addEventListener('resize', checkMobile);
      return () => window.removeEventListener('resize', checkMobile);
    }
  }, []);

  const toggleCategory = useCallback((category: string) => {
    setCollapsedCategories(prev => ({
      ...prev,
      [category]: !prev[category],
    }));
  }, []);

  if (sortedTiers.length === 0 || featuresByCategory.length === 0) {
    return null;
  }

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

      <div className="feature-comparison-wrapper">
        <table className="feature-comparison-table" role="grid">
          <thead className="feature-comparison-thead">
            <tr>
              <th scope="col" className="feature-comparison-th feature-name-col">
                Feature
              </th>
              {sortedTiers.map(tier => (
                <th
                  key={tier.id}
                  scope="col"
                  className={`feature-comparison-th tier-col ${tier.isPopular ? 'popular' : ''}`}
                >
                  {tier.name}
                  {tier.isPopular && (
                    <span className="popular-indicator" aria-label="Most popular">
                      *
                    </span>
                  )}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {featuresByCategory.map(category => (
              <CategoryRows
                key={category.category}
                category={category}
                tiers={sortedTiers}
                isCollapsed={!!collapsedCategories[category.category]}
                onToggle={() => toggleCategory(category.category)}
                isMobile={isMobileView}
              />
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

interface CategoryRowsProps {
  category: PricingFeatureCategory;
  tiers: PricingTier[];
  isCollapsed: boolean;
  onToggle: () => void;
  isMobile: boolean;
}

function CategoryRows({ category, tiers, isCollapsed, onToggle, isMobile }: CategoryRowsProps) {
  return (
    <>
      {/* Category header row */}
      <tr className="category-header-row">
        <th
          scope="row"
          colSpan={tiers.length + 1}
          className="category-header"
        >
          <button
            type="button"
            className="category-toggle"
            onClick={onToggle}
            aria-expanded={!isCollapsed}
            aria-controls={`category-${category.category.replace(/\s+/g, '-')}`}
          >
            <span className="category-name">{category.category}</span>
            <span className={`category-chevron ${isCollapsed ? 'collapsed' : ''}`} aria-hidden="true">
              <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                <path
                  d="M4 6L8 10L12 6"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </span>
          </button>
        </th>
      </tr>

      {/* Feature rows */}
      {!isCollapsed && category.features.map(feature => (
        <FeatureRow
          key={feature.id}
          feature={feature}
          tiers={tiers}
          categoryId={category.category.replace(/\s+/g, '-')}
        />
      ))}
    </>
  );
}

interface FeatureRowProps {
  feature: PricingFeature;
  tiers: PricingTier[];
  categoryId: string;
}

function FeatureRow({ feature, tiers, categoryId }: FeatureRowProps) {
  return (
    <tr className="feature-row" id={`category-${categoryId}`}>
      <th scope="row" className="feature-name-cell">
        <span className="feature-name">{feature.name}</span>
        {feature.description && (
          <span className="feature-description">{feature.description}</span>
        )}
      </th>
      {tiers.map(tier => {
        const { included, value } = getFeatureValue(tier, feature.id);
        return (
          <td
            key={tier.id}
            className={`feature-value-cell ${tier.isPopular ? 'popular' : ''}`}
          >
            {value ? (
              <span className="feature-limit">{value}</span>
            ) : included ? (
              <span className="feature-check" aria-label="Included">
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
            ) : (
              <span className="feature-x" aria-label="Not included">
                <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
                  <path
                    d="M5 5L15 15M15 5L5 15"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeLinecap="round"
                  />
                </svg>
              </span>
            )}
          </td>
        );
      })}
    </tr>
  );
}

export default FeatureComparisonTable;
