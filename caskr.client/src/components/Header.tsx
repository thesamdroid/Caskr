import { Link } from 'react-router-dom'
import '../caskr-ui-redesign.css'

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
            <li className="nav-item"><Link to="/" className="active">Dashboard</Link></li>
            <li className="nav-item"><Link to="/orders">Orders</Link></li>
            <li className="nav-item"><Link to="/barrels">Barrels</Link></li>
            <li className="nav-item"><a href="#">Forecasting</a></li>
            <li className="nav-item"><a href="#">Reports</a></li>
            <li className="nav-item"><a href="#">Settings</a></li>
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
