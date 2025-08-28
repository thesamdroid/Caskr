import './App.css'
import { Link, Route, Routes } from 'react-router-dom'
import LandingPage from './pages/LandingPage'
import OrdersPage from './pages/OrdersPage'
import ProductsPage from './pages/ProductsPage'
import StatusesPage from './pages/StatusesPage'
import UsersPage from './pages/UsersPage'
import UserTypesPage from './pages/UserTypesPage'
import LoginPage from './pages/LoginPage'
import BarrelsPage from './pages/BarrelsPage'

function App() {
  return (
    <div>
      <nav>
        <Link to="/landing">Home</Link> |{' '}
        <Link to="/orders">Orders</Link> |{' '}
        <Link to="/barrels">Barrels</Link> |{' '}
        <Link to="/products">Products</Link> |{' '}
        <Link to="/statuses">Statuses</Link> |{' '}
        <Link to="/users">Users</Link> |{' '}
        <Link to="/usertypes">User Types</Link> |{' '}
        <Link to="/login">Login</Link>
      </nav>
      <Routes>
        <Route path="/" element={<LoginPage />} />
        <Route path="/landing" element={<LandingPage />} />
        <Route path="/orders" element={<OrdersPage />} />
        <Route path="/barrels" element={<BarrelsPage />} />
        <Route path="/products" element={<ProductsPage />} />
        <Route path="/statuses" element={<StatusesPage />} />
        <Route path="/users" element={<UsersPage />} />
        <Route path="/usertypes" element={<UserTypesPage />} />
        <Route path="/login" element={<LoginPage />} />
      </Routes>
    </div>
  )
}

export default App
