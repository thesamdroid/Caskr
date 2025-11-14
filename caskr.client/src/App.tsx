import './App.css'
import { Route, Routes } from 'react-router-dom'
import Layout from './components/Layout'
import DashboardPage from './pages/DashboardPage'
import OrdersPage from './pages/OrdersPage'
import ProductsPage from './pages/ProductsPage'
import LoginPage from './pages/LoginPage'
import BarrelsPage from './pages/BarrelsPage'
import AccountingSettingsPage from './pages/AccountingSettingsPage'

function App() {
  return (
    <Routes>
      <Route path='/' element={<Layout />}>
        <Route index element={<DashboardPage />} />
        <Route path='orders' element={<OrdersPage />} />
        <Route path='barrels' element={<BarrelsPage />} />
        <Route path='products' element={<ProductsPage />} />
        <Route path='accounting' element={<AccountingSettingsPage />} />
        <Route path='login' element={<LoginPage />} />
      </Route>
    </Routes>
  )
}

export default App
