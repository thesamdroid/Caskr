import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '../../../hooks'
import {
  checkAccess,
  fetchUsers,
  clearError,
  clearOperationSuccess
} from '../../../features/userAdminSlice'
import UserListPanel from '../../../components/admin/users/UserListPanel'
import UserDetailPanel from '../../../components/admin/users/UserDetailPanel'
import ReAuthModal from '../../../components/admin/users/ReAuthModal'
import ImpersonationBanner from '../../../components/admin/users/ImpersonationBanner'

function UserAdminDashboard() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()

  const user = useAppSelector(state => state.auth.user)
  const {
    accessChecked,
    accessAllowed,
    showReAuthModal,
    selectedUser,
    isImpersonating,
    error,
    operationSuccess
  } = useAppSelector(state => state.userAdmin)

  // Check super admin access on mount
  useEffect(() => {
    if (!user) {
      navigate('/login')
      return
    }

    if (user.role !== 'SuperAdmin') {
      navigate('/403')
      return
    }

    dispatch(checkAccess())
  }, [dispatch, navigate, user])

  // Load users when access is granted
  useEffect(() => {
    if (accessAllowed) {
      dispatch(fetchUsers({}))
    }
  }, [dispatch, accessAllowed])

  // Auto-clear notifications
  useEffect(() => {
    if (operationSuccess) {
      const timer = setTimeout(() => {
        dispatch(clearOperationSuccess())
      }, 3000)
      return () => clearTimeout(timer)
    }
  }, [dispatch, operationSuccess])

  // Show loading while checking access
  if (!accessChecked) {
    return (
      <div className="user-admin-dashboard">
        <div className="access-check-loading">
          <div className="loading-spinner" />
          <p>Verifying super admin access...</p>
        </div>
      </div>
    )
  }

  // Access denied (e.g., IP not allowed)
  if (!accessAllowed && !showReAuthModal) {
    return (
      <div className="user-admin-dashboard">
        <div className="access-denied">
          <div className="access-denied-icon">&#x1F6AB;</div>
          <h2>Access Denied</h2>
          <p>
            Your access to the Super Admin portal has been denied.
            This may be due to IP restrictions or session expiration.
          </p>
          <p>
            Please contact your system administrator if you believe this is an error.
          </p>
          <button
            type="button"
            className="btn btn-primary"
            onClick={() => navigate('/dashboard')}
          >
            Return to Dashboard
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="user-admin-dashboard">
      {/* Impersonation Banner */}
      {isImpersonating && <ImpersonationBanner />}

      {/* Header */}
      <div className="admin-header">
        <div className="header-title">
          <h1>User Administration</h1>
          <span className="badge badge-danger">Super Admin</span>
        </div>
        <div className="header-actions">
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => navigate('/admin/pricing')}
          >
            Pricing Admin
          </button>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => navigate('/dashboard')}
          >
            Exit Admin
          </button>
        </div>
      </div>

      {/* Notifications */}
      {error && (
        <div className="notification notification-error">
          <span>{error}</span>
          <button
            type="button"
            className="notification-close"
            onClick={() => dispatch(clearError())}
          >
            &times;
          </button>
        </div>
      )}

      {operationSuccess && (
        <div className="notification notification-success">
          <span>{operationSuccess}</span>
        </div>
      )}

      {/* Main Content */}
      <div className="user-admin-content">
        <UserListPanel />
        {selectedUser && <UserDetailPanel />}
      </div>

      {/* Modals */}
      {showReAuthModal && <ReAuthModal />}
    </div>
  )
}

export default UserAdminDashboard
