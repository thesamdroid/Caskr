import { NavLink, Link } from 'react-router-dom'

export default function Header() {
  return (
    <header className="header">
      <div className="header-content">
        <Link to="/" className="logo">
          <div className="barrel-icon" />
          <span>CASKr</span>
        </Link>
        <nav>
          <ul className="nav-menu">
            <li className="nav-item">
              <NavLink to="/" className={({ isActive }) => (isActive ? 'active' : '')}>
                Dashboard
              </NavLink>
            </li>
            <li className="nav-item">
              <NavLink to="/orders" className={({ isActive }) => (isActive ? 'active' : '')}>
                Orders
              </NavLink>
            </li>
            <li className="nav-item">
              <NavLink to="/barrels" className={({ isActive }) => (isActive ? 'active' : '')}>
                Barrels
              </NavLink>
            </li>
            <li className="nav-item">
              <NavLink to="/forecasting" className={({ isActive }) => (isActive ? 'active' : '')}>
                Forecasting
              </NavLink>
            </li>
            <li className="nav-item">
              <NavLink to="/reports" className={({ isActive }) => (isActive ? 'active' : '')}>
                Reports
              </NavLink>
            </li>
            <li className="nav-item">
              <NavLink to="/settings" className={({ isActive }) => (isActive ? 'active' : '')}>
                Settings
              </NavLink>
            </li>
          </ul>
        </nav>
        <div className="user-profile">
          <div className="user-avatar">JD</div>
          <span className="font-medium">John Distiller</span>
        </div>
      </div>
    </header>
  )
}
