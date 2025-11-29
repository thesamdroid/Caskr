import './App.css'
import { Route, Routes } from 'react-router-dom'
import Layout from './components/Layout'
import DashboardPage from './pages/DashboardPage'
import OrdersPage from './pages/OrdersPage'
import ProductsPage from './pages/ProductsPage'
import LoginPage from './pages/LoginPage'
import BarrelsPage from './pages/BarrelsPage'
import AccountingSettingsPage from './pages/AccountingSettingsPage'
import AccountingSyncHistoryPage from './pages/AccountingSyncHistoryPage'
import TtbReportsPage from './pages/TtbReportsPage'
import PermissionGuard from './components/PermissionGuard'
import { TTB_COMPLIANCE_PERMISSION } from './features/authSlice'

function App() {
  return (
    <Routes>
        <Route path='/' element={<Layout />}>
          <Route index element={<DashboardPage />} />
          <Route path='orders' element={<OrdersPage />} />
          <Route path='barrels' element={<BarrelsPage />} />
          <Route
            path='ttb-reports'
            element={
              <PermissionGuard requiredPermission={TTB_COMPLIANCE_PERMISSION}>
                <TtbReportsPage />
              </PermissionGuard>
            }
          />
          <Route path='products' element={<ProductsPage />} />
          <Route path='accounting' element={<AccountingSettingsPage />} />
        <Route path='accounting/sync-history' element={<AccountingSyncHistoryPage />} />
        <Route path='login' element={<LoginPage />} />
      </Route>
    </Routes>
  )
}

export default App
