import { Link, Outlet, useLocation } from 'react-router-dom'
import LoadingOverlay from './LoadingOverlay'
import { useAppSelector } from '../hooks'
import { TTB_COMPLIANCE_PERMISSION, userHasPermission } from '../features/authSlice'

interface NavigationItem {
  label: string
  path: string
  ariaLabel?: string
  hideWhenAuthenticated?: boolean
  requiresPermission?: string
  matchExact?: boolean
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
  const authUser = useAppSelector(state => state.auth.user)
  const isAuthenticated = useAppSelector(state => state.auth.isAuthenticated)

  const navigationItems: NavigationItem[] = [
    { label: 'Dashboard', path: '/', matchExact: true },
    { label: 'Orders', path: '/orders' },
    { label: 'Barrels', path: '/barrels' },
    {
      label: 'TTB Compliance',
      path: '/ttb-reports',
      ariaLabel: 'TTB compliance reports',
      requiresPermission: TTB_COMPLIANCE_PERMISSION,
      icon: <ClipboardIcon />
    },
    { label: 'Products', path: '/products' },
    { label: 'Reports', path: '/reports', ariaLabel: 'Report Builder' },
    { label: 'Accounting', path: '/accounting' },
    { label: 'Sync History', path: '/accounting/sync-history' },
    { label: 'Login', path: '/login' }
  ]

  const isActive = (item: NavigationItem) => {
    if (item.matchExact) {
      return location.pathname === item.path
    }
    return location.pathname.startsWith(item.path)
  }

  const visibleItems = navigationItems.filter(item => {
    if (item.requiresPermission && !userHasPermission(authUser, item.requiresPermission)) {
      return false
    }

    if (item.hideWhenAuthenticated && isAuthenticated) {
      return false
    }

    return true
  })

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

          <nav role="navigation" aria-label="Main navigation">
            <ul className="nav-menu">
              {visibleItems.map(item => (
                <li key={item.path} className="nav-item">
                  <Link
                    to={item.path}
                    className={isActive(item) ? 'active' : ''}
                    aria-current={isActive(item) ? 'page' : undefined}
                    aria-label={item.ariaLabel ?? item.label}
                  >
                    {item.icon && <span className="nav-icon-wrapper">{item.icon}</span>}
                    <span className="nav-label">{item.label}</span>
                  </Link>
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
