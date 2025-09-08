import './App.css'
import { Route, Routes } from 'react-router-dom'
import Dashboard from './pages/Dashboard'
import OrdersPage from './pages/OrdersPage'
import ProductsPage from './pages/ProductsPage'
import StatusesPage from './pages/StatusesPage'
import UsersPage from './pages/UsersPage'
import UserTypesPage from './pages/UserTypesPage'
import LoginPage from './pages/LoginPage'
import BarrelsPage from './pages/BarrelsPage'
import Header from './components/Header'

function App() {
  return (
    <>
      <Header />
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/orders" element={<OrdersPage />} />
        <Route path="/barrels" element={<BarrelsPage />} />
        <Route path="/products" element={<ProductsPage />} />
        <Route path="/statuses" element={<StatusesPage />} />
        <Route path="/users" element={<UsersPage />} />
        <Route path="/usertypes" element={<UserTypesPage />} />
        <Route path="/login" element={<LoginPage />} />
      </Routes>
    </>
  )
}

export default App
