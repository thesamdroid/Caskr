import { Outlet, useLocation } from 'react-router-dom'
import { useAppSelector } from '../hooks'
import { BottomNav, defaultNavItems } from './components/BottomNav'
import { MobileHeader } from './components/MobileHeader'
import { DrawerMenu, DrawerMenuSection } from './components/DrawerMenu'
import { useDrawer, useSafeArea, useBottomNavHeight } from './hooks'
import styles from './MobileAppShell.module.css'

// Page title mapping
const pageTitles: Record<string, string> = {
  '/': 'Dashboard',
  '/scan': 'Scan Barrel',
  '/tasks': 'Today\'s Tasks',
  '/barrels': 'Barrels',
  '/orders': 'Orders',
  '/products': 'Products',
  '/warehouses': 'Warehouses',
  '/purchase-orders': 'Purchase Orders',
  '/reports': 'Reports',
  '/report-builder': 'Report Builder',
  '/ttb-reports': 'TTB Compliance',
  '/accounting': 'Accounting',
  '/settings': 'Settings'
}

// Icons for drawer menu
const OrdersIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" style={{ width: 24, height: 24 }}>
    <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-5 14H7v-2h7v2zm3-4H7v-2h10v2zm0-4H7V7h10v2z"/>
  </svg>
)

const ProductsIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" style={{ width: 24, height: 24 }}>
    <path d="M18 6h-2c0-2.21-1.79-4-4-4S8 3.79 8 6H6c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2zm-6-2c1.1 0 2 .9 2 2h-4c0-1.1.9-2 2-2zm6 16H6V8h2v2c0 .55.45 1 1 1s1-.45 1-1V8h4v2c0 .55.45 1 1 1s1-.45 1-1V8h2v12z"/>
  </svg>
)

const WarehouseIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" style={{ width: 24, height: 24 }}>
    <path d="M20 4H4v2h16V4zm1 10v-2l-1-5H4l-1 5v2h1v6h10v-6h4v6h2v-6h1zm-9 4H6v-4h6v4z"/>
  </svg>
)

const ComplianceIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" style={{ width: 24, height: 24 }}>
    <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-9 14l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/>
  </svg>
)

const ReportsIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" style={{ width: 24, height: 24 }}>
    <path d="M9 17H7v-7h2v7zm4 0h-2V7h2v10zm4 0h-2v-4h2v4zm2.5 2.1h-15V5h15v14.1zm0-16.1h-15c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h15c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2z"/>
  </svg>
)

const AccountingIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" style={{ width: 24, height: 24 }}>
    <path d="M11.8 10.9c-2.27-.59-3-1.2-3-2.15 0-1.09 1.01-1.85 2.7-1.85 1.78 0 2.44.85 2.5 2.1h2.21c-.07-1.72-1.12-3.3-3.21-3.81V3h-3v2.16c-1.94.42-3.5 1.68-3.5 3.61 0 2.31 1.91 3.46 4.7 4.13 2.5.6 3 1.48 3 2.41 0 .69-.49 1.79-2.7 1.79-2.06 0-2.87-.92-2.98-2.1h-2.2c.12 2.19 1.76 3.42 3.68 3.83V21h3v-2.15c1.95-.37 3.5-1.5 3.5-3.55 0-2.84-2.43-3.81-4.7-4.4z"/>
  </svg>
)

const SettingsIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" style={{ width: 24, height: 24 }}>
    <path d="M19.14 12.94c.04-.31.06-.63.06-.94 0-.31-.02-.63-.06-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.04.31-.06.63-.06.94s.02.63.06.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z"/>
  </svg>
)

// Drawer menu sections
const drawerMenuSections: DrawerMenuSection[] = [
  {
    id: 'operations',
    title: 'Operations',
    items: [
      { id: 'orders', label: 'Orders', path: '/orders', icon: <OrdersIcon /> },
      { id: 'products', label: 'Products', path: '/products', icon: <ProductsIcon /> },
      { id: 'warehouses', label: 'Warehouses', path: '/warehouses', icon: <WarehouseIcon /> },
      { id: 'purchase-orders', label: 'Purchase Orders', path: '/purchase-orders', icon: <OrdersIcon /> }
    ]
  },
  {
    id: 'compliance',
    title: 'Compliance',
    items: [
      { id: 'ttb-reports', label: 'TTB Reports', path: '/ttb-reports', icon: <ComplianceIcon /> }
    ]
  },
  {
    id: 'reports',
    title: 'Reports',
    items: [
      { id: 'reports', label: 'View Reports', path: '/reports', icon: <ReportsIcon /> },
      { id: 'report-builder', label: 'Report Builder', path: '/report-builder', icon: <ReportsIcon /> }
    ]
  },
  {
    id: 'settings',
    title: 'Settings',
    items: [
      { id: 'accounting', label: 'Accounting', path: '/accounting', icon: <AccountingIcon /> },
      { id: 'settings', label: 'Settings', path: '/settings', icon: <SettingsIcon /> }
    ]
  }
]

/**
 * Mobile application shell with bottom navigation, header, and drawer menu
 */
export function MobileAppShell() {
  const location = useLocation()
  const authUser = useAppSelector(state => state.auth.user)
  const safeArea = useSafeArea()
  const { totalHeight: bottomNavHeight } = useBottomNavHeight()

  const {
    isOpen: isDrawerOpen,
    open: openDrawer,
    close: closeDrawer,
    drawerRef,
    backdropRef
  } = useDrawer()

  // Get page title based on current route
  const pageTitle = pageTitles[location.pathname] || 'Caskr'

  // Determine if we should show back button
  const showBackButton = location.pathname !== '/'

  // User profile for drawer
  const userProfile = authUser ? {
    name: authUser.name,
    email: authUser.email,
    company: authUser.companyName
  } : undefined

  const handleLogout = () => {
    // TODO: Implement logout logic
    console.log('Logout clicked')
  }

  return (
    <div className={styles.appShell}>
      {/* Header */}
      <MobileHeader
        title={pageTitle}
        showBackButton={showBackButton}
        enableCollapse={true}
      />

      {/* Main content area */}
      <main
        className={styles.mainContent}
        style={{
          paddingTop: 96 + safeArea.top, // Expanded header height + safe area
          paddingBottom: bottomNavHeight
        }}
      >
        <Outlet />
      </main>

      {/* Bottom navigation */}
      <BottomNav
        items={defaultNavItems}
        onMoreClick={openDrawer}
      />

      {/* Drawer menu */}
      <DrawerMenu
        isOpen={isDrawerOpen}
        onClose={closeDrawer}
        drawerRef={drawerRef}
        backdropRef={backdropRef}
        sections={drawerMenuSections}
        userProfile={userProfile}
        onLogout={handleLogout}
      />

      {/* Toast container for notifications */}
      <div
        className={styles.toastContainer}
        style={{ bottom: bottomNavHeight + 16 }}
        aria-live="polite"
      />
    </div>
  )
}

export default MobileAppShell
