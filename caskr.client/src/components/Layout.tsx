import { Link, Outlet } from 'react-router-dom'

export default function Layout() {
  return (
    <>
      <header className='header'>
        <div className='header-content'>
          <Link to='/' className='logo'>
            <div className='barrel-icon' />
            CASKr
          </Link>
          <ul className='nav-menu'>
            <li className='nav-item'>
              <Link to='/'>Dashboard</Link>
            </li>
            <li className='nav-item'>
              <Link to='/orders'>Orders</Link>
            </li>
            <li className='nav-item'>
              <Link to='/barrels'>Barrels</Link>
            </li>
            <li className='nav-item'>
              <Link to='/products'>Products</Link>
            </li>
            <li className='nav-item'>
              <Link to='/login'>Login</Link>
            </li>
          </ul>
        </div>
      </header>
      <main className='main-content'>
        <Outlet />
      </main>
    </>
  )
}
