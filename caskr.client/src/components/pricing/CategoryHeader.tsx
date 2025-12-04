import './CategoryHeader.css';

export interface CategoryHeaderProps {
  category: string;
  icon?: React.ReactNode;
  isCollapsed: boolean;
  onToggle: () => void;
  colSpan: number;
  categoryId: string;
}

// Category icon mapping
const categoryIcons: Record<string, () => JSX.Element> = {
  compliance: ComplianceIcon,
  inventory: InventoryIcon,
  production: ProductionIcon,
  reporting: ReportingIcon,
  integrations: IntegrationsIcon,
  support: SupportIcon,
  security: SecurityIcon,
  'core features': CoreIcon,
  default: DefaultIcon,
};

/**
 * Category header row in the comparison table.
 * Full-width row with expand/collapse functionality.
 */
export function CategoryHeader({
  category,
  icon,
  isCollapsed,
  onToggle,
  colSpan,
  categoryId,
}: CategoryHeaderProps) {
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      onToggle();
    }
  };

  // Get the appropriate icon for the category
  const getCategoryIcon = () => {
    if (icon) return icon;

    const categoryLower = category.toLowerCase();
    const IconComponent = Object.entries(categoryIcons).find(
      ([key]) => categoryLower.includes(key)
    )?.[1] || categoryIcons.default;

    return <IconComponent />;
  };

  return (
    <tr className="category-header-row">
      <th
        scope="row"
        colSpan={colSpan}
        className="category-header-cell"
      >
        <button
          type="button"
          className="category-toggle"
          onClick={onToggle}
          onKeyDown={handleKeyDown}
          aria-expanded={!isCollapsed}
          aria-controls={`category-content-${categoryId}`}
        >
          <span className="category-content">
            <span className="category-icon" aria-hidden="true">
              {getCategoryIcon()}
            </span>
            <span className="category-name">{category}</span>
          </span>
          <span className={`category-chevron ${isCollapsed ? 'collapsed' : ''}`} aria-hidden="true">
            <ChevronIcon />
          </span>
        </button>
      </th>
    </tr>
  );
}

// Icon components for categories
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

function ComplianceIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 18 18" fill="none" aria-hidden="true">
      <path
        d="M9 1L2 4V8.5C2 12.64 5.04 16.5 9 17.5C12.96 16.5 16 12.64 16 8.5V4L9 1Z"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M6 9L8 11L12 7"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function InventoryIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 18 18" fill="none" aria-hidden="true">
      <rect x="2" y="4" width="14" height="12" rx="2" stroke="currentColor" strokeWidth="1.5" />
      <path d="M6 1V4M12 1V4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
      <path d="M2 8H16" stroke="currentColor" strokeWidth="1.5" />
      <circle cx="6" cy="12" r="1" fill="currentColor" />
      <circle cx="9" cy="12" r="1" fill="currentColor" />
      <circle cx="12" cy="12" r="1" fill="currentColor" />
    </svg>
  );
}

function ProductionIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 18 18" fill="none" aria-hidden="true">
      <path
        d="M2 13H16V16H2V13Z"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M3 13V8L6 3H12L15 8V13"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path d="M6 8H12" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
    </svg>
  );
}

function ReportingIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 18 18" fill="none" aria-hidden="true">
      <rect x="2" y="2" width="14" height="14" rx="2" stroke="currentColor" strokeWidth="1.5" />
      <path d="M5 12V9" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
      <path d="M9 12V6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
      <path d="M13 12V8" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
    </svg>
  );
}

function IntegrationsIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 18 18" fill="none" aria-hidden="true">
      <circle cx="9" cy="9" r="3" stroke="currentColor" strokeWidth="1.5" />
      <path d="M9 2V6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
      <path d="M9 12V16" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
      <path d="M2 9H6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
      <path d="M12 9H16" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
    </svg>
  );
}

function SupportIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 18 18" fill="none" aria-hidden="true">
      <circle cx="9" cy="9" r="7" stroke="currentColor" strokeWidth="1.5" />
      <circle cx="9" cy="9" r="3" stroke="currentColor" strokeWidth="1.5" />
      <path d="M9 2V6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
      <path d="M9 12V16" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
      <path d="M2 9H6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
      <path d="M12 9H16" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
    </svg>
  );
}

function SecurityIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 18 18" fill="none" aria-hidden="true">
      <rect x="3" y="8" width="12" height="8" rx="2" stroke="currentColor" strokeWidth="1.5" />
      <path
        d="M5 8V5C5 2.79086 6.79086 1 9 1C11.2091 1 13 2.79086 13 5V8"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
      <circle cx="9" cy="12" r="1" fill="currentColor" />
    </svg>
  );
}

function CoreIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 18 18" fill="none" aria-hidden="true">
      <rect x="2" y="2" width="5" height="5" rx="1" stroke="currentColor" strokeWidth="1.5" />
      <rect x="11" y="2" width="5" height="5" rx="1" stroke="currentColor" strokeWidth="1.5" />
      <rect x="2" y="11" width="5" height="5" rx="1" stroke="currentColor" strokeWidth="1.5" />
      <rect x="11" y="11" width="5" height="5" rx="1" stroke="currentColor" strokeWidth="1.5" />
    </svg>
  );
}

function DefaultIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 18 18" fill="none" aria-hidden="true">
      <circle cx="9" cy="9" r="7" stroke="currentColor" strokeWidth="1.5" />
      <path d="M6 9H12" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
      <path d="M9 6V12" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
    </svg>
  );
}

export default CategoryHeader;
