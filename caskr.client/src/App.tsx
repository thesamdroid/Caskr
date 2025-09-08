import './App.css'
import { Route, Routes } from 'react-router-dom'
import Layout from './components/Layout'
import DashboardPage from './pages/DashboardPage'
import OrdersPage from './pages/OrdersPage'
import ProductsPage from './pages/ProductsPage'
import StatusesPage from './pages/StatusesPage'
import UsersPage from './pages/UsersPage'
import UserTypesPage from './pages/UserTypesPage'
import LoginPage from './pages/LoginPage'
import BarrelsPage from './pages/BarrelsPage'

function App() {
  return (
    <Routes>
      <Route path='/' element={<Layout />}>
        <Route index element={<DashboardPage />} />
        <Route path='orders' element={<OrdersPage />} />
        <Route path='barrels' element={<BarrelsPage />} />
        <Route path='products' element={<ProductsPage />} />
        <Route path='statuses' element={<StatusesPage />} />
        <Route path='users' element={<UsersPage />} />
        <Route path='usertypes' element={<UserTypesPage />} />
      </Route>
      <Route path='/login' element={<LoginPage />} />
    </Routes>
  )
}

export default App
