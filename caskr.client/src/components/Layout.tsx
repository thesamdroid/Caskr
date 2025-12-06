import { useEffect, useState, useRef } from 'react'
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom'
import LoadingOverlay from './LoadingOverlay'
import { useAppSelector, useAppDispatch } from '../hooks'
import { TTB_COMPLIANCE_PERMISSION, userHasPermission, logout } from '../features/authSlice'
import { fetchWarehouses, setSelectedWarehouseId, resetWarehousesState } from '../features/warehousesSlice'

interface NavigationItem {
  label: string
  path: string
  ariaLabel?: string
  hideWhenAuthenticated?: boolean
  showWhenAuthenticated?: boolean
  requiresPermission?: string
  matchExact?: boolean
  icon?: JSX.Element
  isLogout?: boolean
}

interface NavigationGroup {
  label: string
  items: NavigationItem[]
  icon?: JSX.Element
}

const ClipboardIcon = () => (
  <svg
    aria-hidden='true'
    focusable='false'
    className='nav-icon'
    viewBox='0 0 24 24'
    role='img'
  >
    <path
      d='M16 3h-1.18A3 3 0 0 0 12 2a3 3 0 0 0-2.82 2H8a2 2 0 0 0-2 2v12a3 3 0 0 0 3 3h6a3 3 0 0 0 3-3V5a2 2 0 0 0-2-2Zm-4-1a2 2 0 0 1 1.99 1.77c0 .07.01.14.01.21a1 1 0 0 1-1.01 1H11a1 1 0 0 1-1.01-1c0-.07.01-.14.01-.21A2 2 0 0 1 12 2Zm5 15a2 2 0 0 1-2 2H9a2 2 0 0 1-2-2V5a1 1 0 0 1 1-1h.17A3 3 0 0 0 11 6h2a3 3 0 0 0 2.83-2H16a1 1 0 0 1 1 1Z'
      fill='currentColor'
    />
  </svg>
)

export default function Layout() {
  const location = useLocation()
  const navigate = useNavigate()
  const dispatch = useAppDispatch()
  const authUser = useAppSelector(state => state.auth.user)
  const isAuthenticated = useAppSelector(state => state.auth.isAuthenticated)
  const warehouses = useAppSelector(state => state.warehouses.items)
  const selectedWarehouseId = useAppSelector(state => state.warehouses.selectedWarehouseId)

  const handleLogout = () => {
    dispatch(logout())
    dispatch(resetWarehousesState())
    navigate('/login')
  }

  // Fetch warehouses on mount
  useEffect(() => {
    const companyId = 1 // TODO: Get from auth context
    dispatch(fetchWarehouses({ companyId, includeInactive: false }))
  }, [dispatch])

  const handleWarehouseChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const value = e.target.value
    dispatch(setSelectedWarehouseId(value === 'all' ? null : parseInt(value, 10)))
  }

  // State for tracking open dropdown
  const [openDropdown, setOpenDropdown] = useState<string | null>(null)
  const dropdownRef = useRef<HTMLUListElement>(null)

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setOpenDropdown(null)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  // Standalone navigation items
  const standaloneItems: NavigationItem[] = [
    { label: 'Dashboard', path: '/', matchExact: true },
    { label: 'Login', path: '/login', hideWhenAuthenticated: true },
    { label: 'Log out', path: '#', showWhenAuthenticated: true, isLogout: true }
  ]

  // Grouped navigation items
  const navigationGroups: NavigationGroup[] = [
    {
      label: 'Orders',
      items: [
        { label: 'Work Orders', path: '/orders' },
        { label: 'Purchase Orders', path: '/purchase-orders', ariaLabel: 'Manage Purchase Orders' }
      ]
    },
    {
      label: 'Inventory',
      items: [
        { label: 'Barrels', path: '/barrels' },
        { label: 'Products', path: '/products' },
        { label: 'Warehouses', path: '/warehouses' }
      ]
    },
    {
      label: 'Reports',
      items: [
        {
          label: 'Compliance',
          path: '/ttb-reports',
          ariaLabel: 'TTB compliance reports - your completed federal reports',
          requiresPermission: TTB_COMPLIANCE_PERMISSION,
          icon: <ClipboardIcon />
        },
        { label: 'Reports', path: '/reports', ariaLabel: 'View Reports', matchExact: true },
        { label: 'Report Builder', path: '/report-builder', ariaLabel: 'Custom Report Builder' }
      ]
    },
    {
      label: 'Finance',
      items: [
        { label: 'Accounting', path: '/accounting', matchExact: true },
        { label: 'Sync History', path: '/accounting/sync-history' },
        { label: 'Pricing', path: '/pricing' }
      ]
    }
  ]

  const isActive = (item: NavigationItem) => {
    if (item.matchExact) {
      return location.pathname === item.path
    }
    return location.pathname.startsWith(item.path)
  }

  const isGroupActive = (group: NavigationGroup) => {
    return group.items.some(item => isActive(item))
  }

  const filterItems = (items: NavigationItem[]) => {
    return items.filter(item => {
      if (item.requiresPermission && !userHasPermission(authUser, item.requiresPermission)) {
        return false
      }
      if (item.hideWhenAuthenticated && isAuthenticated) {
        return false
      }
      if (item.showWhenAuthenticated && !isAuthenticated) {
        return false
      }
      return true
    })
  }

  const visibleStandaloneItems = filterItems(standaloneItems)

  const visibleGroups = navigationGroups.map(group => ({
    ...group,
    items: filterItems(group.items)
  })).filter(group => group.items.length > 0)

  const toggleDropdown = (label: string) => {
    setOpenDropdown(openDropdown === label ? null : label)
  }

  return (
    <>
      {/* Skip Navigation for Accessibility */}
      <a href="#main-content" className="skip-nav">
        Skip to main content
      </a>

      <LoadingOverlay />

      <header className="header" role="banner">
        <div className="header-content">
          <Link to="/" className="logo" aria-label="CASKr Home">
            <div className="barrel-icon" aria-hidden="true" />
            <span>CASKr</span>
          </Link>

          {/* Warehouse Selector - only show when authenticated */}
          {isAuthenticated && warehouses.length > 0 && (
            <div className="warehouse-selector">
              <label htmlFor="warehouse-select" className="warehouse-selector-label">
                Warehouse:
              </label>
              <select
                id="warehouse-select"
                value={selectedWarehouseId === null ? 'all' : selectedWarehouseId}
                onChange={handleWarehouseChange}
                className="warehouse-select"
                aria-label="Select warehouse to filter data"
              >
                <option value="all">All Warehouses</option>
                {warehouses.map(w => (
                  <option key={w.id} value={w.id}>
                    {w.name}
                  </option>
                ))}
              </select>
            </div>
          )}

          <nav role="navigation" aria-label="Main navigation">
            <ul className="nav-menu" ref={dropdownRef}>
              {/* Dashboard - standalone */}
              {visibleStandaloneItems.filter(item => item.label === 'Dashboard').map(item => (
                <li key={item.path} className="nav-item">
                  <Link
                    to={item.path}
                    className={isActive(item) ? 'active' : ''}
                    aria-current={isActive(item) ? 'page' : undefined}
                    aria-label={item.ariaLabel ?? item.label}
                  >
                    <span className="nav-label">{item.label}</span>
                  </Link>
                </li>
              ))}

              {/* Grouped navigation with dropdowns */}
              {visibleGroups.map(group => (
                <li key={group.label} className="nav-item nav-dropdown">
                  <button
                    className={`nav-dropdown-trigger ${isGroupActive(group) ? 'active' : ''}`}
                    onClick={() => toggleDropdown(group.label)}
                    aria-expanded={openDropdown === group.label}
                    aria-haspopup="true"
                  >
                    <span className="nav-label">{group.label}</span>
                    <svg className="dropdown-arrow" viewBox="0 0 12 12" aria-hidden="true">
                      <path d="M2 4l4 4 4-4" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                    </svg>
                  </button>
                  {openDropdown === group.label && (
                    <ul className="nav-dropdown-menu">
                      {group.items.map(item => (
                        <li key={item.path}>
                          <Link
                            to={item.path}
                            className={isActive(item) ? 'active' : ''}
                            aria-current={isActive(item) ? 'page' : undefined}
                            aria-label={item.ariaLabel ?? item.label}
                            onClick={() => setOpenDropdown(null)}
                          >
                            {item.icon && <span className="nav-icon-wrapper">{item.icon}</span>}
                            <span>{item.label}</span>
                          </Link>
                        </li>
                      ))}
                    </ul>
                  )}
                </li>
              ))}

              {/* Login/Logout - standalone */}
              {visibleStandaloneItems.filter(item => item.label === 'Login' || item.isLogout).map(item => (
                <li key={item.label} className="nav-item">
                  {item.isLogout ? (
                    <button
                      onClick={handleLogout}
                      className="nav-logout-button"
                      aria-label={item.ariaLabel ?? item.label}
                    >
                      <span className="nav-label">{item.label}</span>
                    </button>
                  ) : (
                    <Link
                      to={item.path}
                      className={isActive(item) ? 'active' : ''}
                      aria-current={isActive(item) ? 'page' : undefined}
                      aria-label={item.ariaLabel ?? item.label}
                    >
                      <span className="nav-label">{item.label}</span>
                    </Link>
                  )}
                </li>
              ))}
            </ul>
          </nav>
        </div>
      </header>

      <main id="main-content" className="main-content" role="main">
        <Outlet />
      </main>
    </>
  )
}
