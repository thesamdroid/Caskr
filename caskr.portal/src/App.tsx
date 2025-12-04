import { Routes, Route, Navigate } from 'react-router-dom'
import { useAppSelector } from './hooks'
import Layout from './components/Layout'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import VerifyEmailPage from './pages/VerifyEmailPage'
import ForgotPasswordPage from './pages/ForgotPasswordPage'
import ResetPasswordPage from './pages/ResetPasswordPage'
import DashboardPage from './pages/DashboardPage'
import BarrelDetailPage from './pages/BarrelDetailPage'
import ForbiddenPage from './pages/ForbiddenPage'
import PricingAdminDashboard from './pages/admin/PricingAdminDashboard'
import UserAdminDashboard from './pages/admin/users/UserAdminDashboard'
import SignupPage from './pages/SignupPage'
import OnboardingWizard from './components/onboarding/OnboardingWizard'

function PrivateRoute({ children }: { children: React.ReactNode }) {
  const isAuthenticated = useAppSelector(state => state.auth.isAuthenticated)
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" />
}

function PublicRoute({ children }: { children: React.ReactNode }) {
  const isAuthenticated = useAppSelector(state => state.auth.isAuthenticated)
  return !isAuthenticated ? <>{children}</> : <Navigate to="/dashboard" />
}

function App() {
  return (
    <>
      <OnboardingWizard />
      <Routes>
      {/* Public routes */}
      <Route
        path="/login"
        element={
          <PublicRoute>
            <LoginPage />
          </PublicRoute>
        }
      />
      <Route
        path="/register"
        element={
          <PublicRoute>
            <RegisterPage />
          </PublicRoute>
        }
      />
      <Route path="/verify" element={<VerifyEmailPage />} />
      <Route path="/signup" element={<SignupPage />} />
      <Route
        path="/forgot-password"
        element={
          <PublicRoute>
            <ForgotPasswordPage />
          </PublicRoute>
        }
      />
      <Route
        path="/reset-password"
        element={
          <PublicRoute>
            <ResetPasswordPage />
          </PublicRoute>
        }
      />

      {/* Protected routes */}
      <Route
        path="/"
        element={
          <PrivateRoute>
            <Layout />
          </PrivateRoute>
        }
      >
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={<DashboardPage />} />
        <Route path="barrels/:id" element={<BarrelDetailPage />} />
        <Route path="admin/pricing" element={<PricingAdminDashboard />} />
        <Route path="admin/users" element={<UserAdminDashboard />} />
      </Route>

      {/* 403 Forbidden page */}
      <Route path="/403" element={<ForbiddenPage />} />

      {/* Catch-all redirect */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
    </>
  )
}

export default App
