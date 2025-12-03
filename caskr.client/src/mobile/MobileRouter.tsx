import { lazy, Suspense } from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { MobileAppShell } from './MobileAppShell'
import styles from './MobileRouter.module.css'

// Lazy-loaded mobile pages
const MobileDashboard = lazy(() => import('./pages/MobileDashboard'))
const MobileScan = lazy(() => import('./pages/MobileScan'))
const MobileTasks = lazy(() => import('./pages/MobileTasks'))
const MobileBarrels = lazy(() => import('./pages/MobileBarrels'))
const MobileBarrelDetail = lazy(() => import('./pages/MobileBarrelDetail'))

/**
 * Fallback loading component for lazy-loaded pages
 * Accessible, supports dark mode and reduced motion
 */
const PageLoader = () => (
  <div className={styles.pageLoader} role="status" aria-label="Loading page">
    <div className={styles.spinner} aria-hidden="true" />
  </div>
)

/**
 * Mobile-specific router with lazy-loaded pages
 */
export function MobileRouter() {
  return (
    <Routes>
      <Route element={<MobileAppShell />}>
        <Route
          index
          element={
            <Suspense fallback={<PageLoader />}>
              <MobileDashboard />
            </Suspense>
          }
        />
        <Route
          path="scan"
          element={
            <Suspense fallback={<PageLoader />}>
              <MobileScan />
            </Suspense>
          }
        />
        <Route
          path="tasks"
          element={
            <Suspense fallback={<PageLoader />}>
              <MobileTasks />
            </Suspense>
          }
        />
        <Route
          path="barrels"
          element={
            <Suspense fallback={<PageLoader />}>
              <MobileBarrels />
            </Suspense>
          }
        />
        <Route
          path="barrels/:id"
          element={
            <Suspense fallback={<PageLoader />}>
              <MobileBarrelDetail />
            </Suspense>
          }
        />

        {/* Redirect any unmatched routes to mobile dashboard */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  )
}

export default MobileRouter
