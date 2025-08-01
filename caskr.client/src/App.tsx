import './App.css'
import { Link, Route, Routes } from 'react-router-dom'
import OrdersPage from './pages/OrdersPage'
import ProductsPage from './pages/ProductsPage'
import StatusesPage from './pages/StatusesPage'
import UsersPage from './pages/UsersPage'
import UserTypesPage from './pages/UserTypesPage'

function App() {
  return (
    <div>
      <nav>
        <Link to="/">Orders</Link> |{' '}
        <Link to="/products">Products</Link> |{' '}
        <Link to="/statuses">Statuses</Link> |{' '}
        <Link to="/users">Users</Link> |{' '}
        <Link to="/usertypes">User Types</Link>
      </nav>
      <Routes>
        <Route path="/" element={<OrdersPage />} />
        <Route path="/products" element={<ProductsPage />} />
        <Route path="/statuses" element={<StatusesPage />} />
        <Route path="/users" element={<UsersPage />} />
        <Route path="/usertypes" element={<UserTypesPage />} />
      </Routes>
    </div>
  )
}

export default App
