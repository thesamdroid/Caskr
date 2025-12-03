import './App.css'
import { Route, Routes } from 'react-router-dom'
import Layout from './components/Layout'
import DashboardPage from './pages/DashboardPage'
import DashboardPreviewPage from './pages/DashboardPreviewPage'
import OrdersPage from './pages/OrdersPage'
import ProductsPage from './pages/ProductsPage'
import LoginPage from './pages/LoginPage'
import BarrelsPage from './pages/BarrelsPage'
import WarehousesPage from './pages/WarehousesPage'
import AccountingSettingsPage from './pages/AccountingSettingsPage'
import AccountingSyncHistoryPage from './pages/AccountingSyncHistoryPage'
import TtbReportsPage from './pages/TtbReportsPage'
import TtbAutoReportPreviewPage from './pages/TtbAutoReportPreviewPage'
import TtbTransactionsPage from './pages/TtbTransactionsPage'
import TtbGaugeRecordsPage from './pages/TtbGaugeRecordsPage'
import ReportsPage from './pages/ReportsPage'
import CustomReportBuilderPage from './pages/CustomReportBuilderPage'
import PermissionGuard from './components/PermissionGuard'
import { TTB_COMPLIANCE_PERMISSION, TTB_EDIT_PERMISSION } from './features/authSlice'

function App() {
  return (
    <Routes>
        <Route path='/' element={<Layout />}>
          <Route index element={<DashboardPage />} />
          <Route path='dashboard-preview' element={<DashboardPreviewPage />} />
          <Route path='orders' element={<OrdersPage />} />
          <Route path='barrels' element={<BarrelsPage />} />
          <Route path='warehouses' element={<WarehousesPage />} />
          <Route
            path='ttb-reports'
            element={
              <PermissionGuard requiredPermission={TTB_COMPLIANCE_PERMISSION}>
                <TtbReportsPage />
              </PermissionGuard>
            }
          />
          <Route path='ttb-auto-report-preview' element={<TtbAutoReportPreviewPage />} />
          <Route
            path='ttb-transactions'
            element={
              <PermissionGuard requiredPermission={TTB_EDIT_PERMISSION}>
                <TtbTransactionsPage />
              </PermissionGuard>
            }
          />
          <Route
            path='ttb-gauge-records'
            element={
              <PermissionGuard requiredPermission={TTB_EDIT_PERMISSION}>
                <TtbGaugeRecordsPage />
              </PermissionGuard>
            }
          />
          <Route path='products' element={<ProductsPage />} />
          <Route path='reports' element={<ReportsPage />} />
          <Route path='report-builder' element={<CustomReportBuilderPage />} />
          <Route path='accounting' element={<AccountingSettingsPage />} />
        <Route path='accounting/sync-history' element={<AccountingSyncHistoryPage />} />
        <Route path='login' element={<LoginPage />} />
      </Route>
    </Routes>
  )
}

export default App
