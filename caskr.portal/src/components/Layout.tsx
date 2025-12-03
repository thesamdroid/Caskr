import { Outlet, Link, useNavigate } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '../hooks'
import { logout } from '../features/authSlice'

function Layout() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const user = useAppSelector(state => state.auth.user)

  const handleLogout = () => {
    dispatch(logout())
    navigate('/login')
  }

  return (
    <div className="portal-layout">
      <header className="portal-header">
        <div className="header-content">
          <Link to="/dashboard" className="logo">
            <span className="logo-icon">&#x2615;</span>
            <span className="logo-text">Caskr Portal</span>
          </Link>

          <nav className="header-nav">
            <Link to="/dashboard" className="nav-link">
              My Barrels
            </Link>
          </nav>

          <div className="header-user">
            {user && (
              <span className="user-greeting">
                Welcome, {user.firstName}!
              </span>
            )}
            <button
              onClick={handleLogout}
              className="logout-button"
              aria-label="Logout"
            >
              Logout
            </button>
          </div>
        </div>
      </header>

      <main className="portal-main">
        <Outlet />
      </main>

      <footer className="portal-footer">
        <div className="footer-content">
          <p>&copy; {new Date().getFullYear()} Caskr. All rights reserved.</p>
          <nav className="footer-nav">
            <a href="/contact" className="footer-link">Contact Distillery</a>
            <a href="/privacy" className="footer-link">Privacy Policy</a>
            <a href="/terms" className="footer-link">Terms of Service</a>
          </nav>
        </div>
      </footer>
    </div>
  )
}

export default Layout
