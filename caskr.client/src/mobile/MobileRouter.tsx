import { lazy, Suspense } from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { MobileAppShell } from './MobileAppShell'

// Lazy-loaded mobile pages
const MobileDashboard = lazy(() => import('./pages/MobileDashboard'))
const MobileScan = lazy(() => import('./pages/MobileScan'))
const MobileTasks = lazy(() => import('./pages/MobileTasks'))
const MobileBarrels = lazy(() => import('./pages/MobileBarrels'))
const MobileBarrelDetail = lazy(() => import('./pages/MobileBarrelDetail'))

// Fallback loading component
const PageLoader = () => (
  <div style={{
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: '50vh'
  }}>
    <div style={{
      width: 40,
      height: 40,
      border: '3px solid #e5e7eb',
      borderTopColor: '#2563eb',
      borderRadius: '50%',
      animation: 'spin 0.8s linear infinite'
    }} />
    <style>{`
      @keyframes spin {
        to { transform: rotate(360deg); }
      }
    `}</style>
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
