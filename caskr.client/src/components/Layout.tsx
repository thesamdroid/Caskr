import { Link, Outlet, useLocation } from 'react-router-dom'
import LoadingOverlay from './LoadingOverlay'

export default function Layout() {
  const location = useLocation()

  const isActive = (path: string) => {
    if (path === '/') {
      return location.pathname === '/'
    }
    return location.pathname.startsWith(path)
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

          <nav role="navigation" aria-label="Main navigation">
            <ul className="nav-menu">
              <li className="nav-item">
                <Link
                  to="/"
                  className={isActive('/') && location.pathname === '/' ? 'active' : ''}
                  aria-current={location.pathname === '/' ? 'page' : undefined}
                >
                  Dashboard
                </Link>
              </li>
              <li className="nav-item">
                <Link
                  to="/orders"
                  className={isActive('/orders') ? 'active' : ''}
                  aria-current={isActive('/orders') ? 'page' : undefined}
                >
                  Orders
                </Link>
              </li>
              <li className="nav-item">
                <Link
                  to="/barrels"
                  className={isActive('/barrels') ? 'active' : ''}
                  aria-current={isActive('/barrels') ? 'page' : undefined}
                >
                  Barrels
                </Link>
              </li>
              <li className="nav-item">
                <Link
                  to="/ttb-reports"
                  className={isActive('/ttb-reports') ? 'active' : ''}
                  aria-current={isActive('/ttb-reports') ? 'page' : undefined}
                >
                  TTB Reports
                </Link>
              </li>
              <li className="nav-item">
                <Link
                  to="/products"
                  className={isActive('/products') ? 'active' : ''}
                  aria-current={isActive('/products') ? 'page' : undefined}
                >
                  Products
                </Link>
              </li>
              <li className="nav-item">
                <Link
                  to="/accounting"
                  className={isActive('/accounting') && !location.pathname.includes('sync-history') ? 'active' : ''}
                  aria-current={location.pathname === '/accounting' ? 'page' : undefined}
                >
                  Accounting
                </Link>
              </li>
              <li className="nav-item">
                <Link
                  to="/accounting/sync-history"
                  className={location.pathname.includes('sync-history') ? 'active' : ''}
                  aria-current={location.pathname.includes('sync-history') ? 'page' : undefined}
                >
                  Sync History
                </Link>
              </li>
              <li className="nav-item">
                <Link
                  to="/login"
                  className={isActive('/login') ? 'active' : ''}
                  aria-current={isActive('/login') ? 'page' : undefined}
                >
                  Login
                </Link>
              </li>
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
