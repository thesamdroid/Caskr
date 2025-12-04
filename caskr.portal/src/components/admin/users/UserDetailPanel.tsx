import { useEffect } from 'react'
import { useAppDispatch, useAppSelector } from '../../../hooks'
import {
  setActiveTab,
  setShowSuspendModal,
  setShowDeleteModal,
  setShowImpersonateModal,
  setShowEditModal,
  clearSelectedUser,
  fetchUserAuditLogs,
  fetchUserSessions,
  fetchUserNotes,
  resetUserPassword,
  forceLogoutUser,
  unlockUserAccount,
  unsuspendUser,
  restoreUser
} from '../../../features/userAdminSlice'
import {
  formatUserRole,
  formatUserStatus,
  getStatusBadgeClass,
  getRoleBadgeClass,
  getSubscriptionBadgeClass,
  formatBytes,
  formatRelativeTime
} from '../../../types/userAdmin'
import UserEditModal from './UserEditModal'
import SuspendUserModal from './SuspendUserModal'
import DeleteUserModal from './DeleteUserModal'
import ImpersonateUserModal from './ImpersonateUserModal'
import UserActivityTab from './UserActivityTab'
import UserSessionsTab from './UserSessionsTab'
import UserSubscriptionTab from './UserSubscriptionTab'
import UserNotesTab from './UserNotesTab'

function UserDetailPanel() {
  const dispatch = useAppDispatch()
  const {
    selectedUser,
    isLoadingUser,
    activeTab,
    showEditModal,
    showSuspendModal,
    showDeleteModal,
    showImpersonateModal
  } = useAppSelector(state => state.userAdmin)

  // Load tab data when tab changes
  useEffect(() => {
    if (!selectedUser) return

    if (activeTab === 'activity') {
      dispatch(fetchUserAuditLogs({ userId: selectedUser.id, limit: 50 }))
    } else if (activeTab === 'sessions') {
      dispatch(fetchUserSessions(selectedUser.id))
    } else if (activeTab === 'notes') {
      dispatch(fetchUserNotes(selectedUser.id))
    }
  }, [dispatch, selectedUser, activeTab])

  if (!selectedUser) return null

  if (isLoadingUser) {
    return (
      <div className="user-detail-panel">
        <div className="loading-state">
          <div className="loading-spinner" />
          <p>Loading user details...</p>
        </div>
      </div>
    )
  }

  const handleClose = () => {
    dispatch(clearSelectedUser())
  }

  const handleResetPassword = () => {
    if (confirm('Send password reset email to this user?')) {
      dispatch(resetUserPassword(selectedUser.id))
    }
  }

  const handleForceLogout = () => {
    if (confirm('Log out this user from all sessions?')) {
      dispatch(forceLogoutUser(selectedUser.id))
    }
  }

  const handleUnlock = () => {
    dispatch(unlockUserAccount(selectedUser.id))
  }

  const handleUnsuspend = () => {
    const reason = prompt('Reason for reactivating this user:')
    if (reason) {
      dispatch(unsuspendUser({ userId: selectedUser.id, reason }))
    }
  }

  const handleRestore = () => {
    const reason = prompt('Reason for restoring this user:')
    if (reason) {
      dispatch(restoreUser({ userId: selectedUser.id, reason }))
    }
  }

  const tabs = [
    { id: 'details', label: 'Details' },
    { id: 'activity', label: 'Activity' },
    { id: 'sessions', label: 'Sessions' },
    { id: 'subscription', label: 'Subscription' },
    { id: 'notes', label: 'Notes' }
  ] as const

  return (
    <div className="user-detail-panel">
      {/* Header */}
      <div className="detail-header">
        <div className="user-identity">
          <div className="user-avatar">
            {selectedUser.avatarUrl ? (
              <img src={selectedUser.avatarUrl} alt="" />
            ) : (
              <span>{selectedUser.firstName?.[0]}{selectedUser.lastName?.[0]}</span>
            )}
          </div>
          <div className="user-basic">
            <h2>{selectedUser.firstName} {selectedUser.lastName}</h2>
            <p className="user-email">{selectedUser.email}</p>
            <div className="user-badges">
              <span className={`badge ${getRoleBadgeClass(selectedUser.role)}`}>
                {formatUserRole(selectedUser.role)}
              </span>
              <span className={`badge ${getStatusBadgeClass(selectedUser.status)}`}>
                {formatUserStatus(selectedUser.status)}
              </span>
              {selectedUser.subscriptionTier && (
                <span className={`badge ${getSubscriptionBadgeClass(selectedUser.subscriptionStatus)}`}>
                  {selectedUser.subscriptionTier}
                </span>
              )}
            </div>
          </div>
        </div>
        <button
          type="button"
          className="btn-close"
          onClick={handleClose}
        >
          &times;
        </button>
      </div>

      {/* Quick Actions */}
      <div className="quick-actions">
        <button
          type="button"
          className="btn btn-secondary btn-small"
          onClick={() => dispatch(setShowEditModal(true))}
        >
          Edit
        </button>
        <button
          type="button"
          className="btn btn-secondary btn-small"
          onClick={handleResetPassword}
        >
          Reset Password
        </button>
        <button
          type="button"
          className="btn btn-secondary btn-small"
          onClick={handleForceLogout}
        >
          Force Logout
        </button>
        {selectedUser.lockedUntil && (
          <button
            type="button"
            className="btn btn-warning btn-small"
            onClick={handleUnlock}
          >
            Unlock Account
          </button>
        )}
        {selectedUser.status === 'Active' && (
          <>
            <button
              type="button"
              className="btn btn-warning btn-small"
              onClick={() => dispatch(setShowSuspendModal(true))}
            >
              Suspend
            </button>
            <button
              type="button"
              className="btn btn-info btn-small"
              onClick={() => dispatch(setShowImpersonateModal(true))}
            >
              Impersonate
            </button>
          </>
        )}
        {selectedUser.status === 'Suspended' && (
          <button
            type="button"
            className="btn btn-success btn-small"
            onClick={handleUnsuspend}
          >
            Reactivate
          </button>
        )}
        {selectedUser.status !== 'Deleted' && (
          <button
            type="button"
            className="btn btn-danger btn-small"
            onClick={() => dispatch(setShowDeleteModal(true))}
          >
            Delete
          </button>
        )}
        {selectedUser.status === 'Deleted' && (
          <button
            type="button"
            className="btn btn-success btn-small"
            onClick={handleRestore}
          >
            Restore
          </button>
        )}
      </div>

      {/* Tabs */}
      <div className="detail-tabs">
        {tabs.map(tab => (
          <button
            key={tab.id}
            type="button"
            className={`tab-button ${activeTab === tab.id ? 'active' : ''}`}
            onClick={() => dispatch(setActiveTab(tab.id))}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Tab Content */}
      <div className="tab-content">
        {activeTab === 'details' && (
          <div className="details-tab">
            {/* Account Info */}
            <div className="info-section">
              <h3>Account Information</h3>
              <div className="info-grid">
                <div className="info-item">
                  <label>User ID</label>
                  <span className="mono">{selectedUser.id}</span>
                </div>
                <div className="info-item">
                  <label>Email Verified</label>
                  <span>
                    {selectedUser.emailVerifiedAt
                      ? new Date(selectedUser.emailVerifiedAt).toLocaleString()
                      : 'Not verified'}
                  </span>
                </div>
                <div className="info-item">
                  <label>Created</label>
                  <span>{new Date(selectedUser.createdAt).toLocaleString()}</span>
                </div>
                <div className="info-item">
                  <label>Last Login</label>
                  <span>{formatRelativeTime(selectedUser.lastLoginAt)}</span>
                </div>
                <div className="info-item">
                  <label>Phone</label>
                  <span>{selectedUser.phone || '-'}</span>
                </div>
                <div className="info-item">
                  <label>Timezone</label>
                  <span>{selectedUser.timezone || 'Not set'}</span>
                </div>
              </div>
            </div>

            {/* Security */}
            <div className="info-section">
              <h3>Security</h3>
              <div className="info-grid">
                <div className="info-item">
                  <label>Two-Factor Auth</label>
                  <span className={selectedUser.twoFactorEnabled ? 'text-success' : 'text-warning'}>
                    {selectedUser.twoFactorEnabled ? 'Enabled' : 'Disabled'}
                  </span>
                </div>
                <div className="info-item">
                  <label>Last Password Change</label>
                  <span>{formatRelativeTime(selectedUser.lastPasswordChangeAt)}</span>
                </div>
                <div className="info-item">
                  <label>Failed Login Attempts</label>
                  <span className={selectedUser.failedLoginAttempts > 0 ? 'text-warning' : ''}>
                    {selectedUser.failedLoginAttempts}
                  </span>
                </div>
                <div className="info-item">
                  <label>Account Locked</label>
                  <span className={selectedUser.lockedUntil ? 'text-danger' : 'text-success'}>
                    {selectedUser.lockedUntil
                      ? `Until ${new Date(selectedUser.lockedUntil).toLocaleString()}`
                      : 'No'}
                  </span>
                </div>
              </div>
            </div>

            {/* Distillery Info */}
            {selectedUser.distilleryName && (
              <div className="info-section">
                <h3>Distillery</h3>
                <div className="info-grid">
                  <div className="info-item">
                    <label>Name</label>
                    <span>{selectedUser.distilleryName}</span>
                  </div>
                  <div className="info-item">
                    <label>DSP Number</label>
                    <span>{selectedUser.dspNumber || '-'}</span>
                  </div>
                  <div className="info-item">
                    <label>TTB Permit</label>
                    <span>{selectedUser.ttbPermitId || '-'}</span>
                  </div>
                  <div className="info-item full-width">
                    <label>Address</label>
                    <span>
                      {[
                        selectedUser.distilleryAddress,
                        selectedUser.distilleryCity,
                        selectedUser.distilleryState,
                        selectedUser.distilleryPostalCode
                      ].filter(Boolean).join(', ') || '-'}
                    </span>
                  </div>
                </div>
              </div>
            )}

            {/* Usage Stats */}
            <div className="info-section">
              <h3>Usage Statistics</h3>
              <div className="stats-grid">
                <div className="stat-card">
                  <span className="stat-value">{selectedUser.totalBarrels}</span>
                  <span className="stat-label">Barrels</span>
                </div>
                <div className="stat-card">
                  <span className="stat-value">{selectedUser.totalBatches}</span>
                  <span className="stat-label">Batches</span>
                </div>
                <div className="stat-card">
                  <span className="stat-value">{selectedUser.teamMemberCount}</span>
                  <span className="stat-label">Team Members</span>
                </div>
                <div className="stat-card">
                  <span className="stat-value">{formatBytes(selectedUser.storageUsedMb * 1024 * 1024)}</span>
                  <span className="stat-label">Storage Used</span>
                </div>
                <div className="stat-card">
                  <span className="stat-value">{selectedUser.totalLogins}</span>
                  <span className="stat-label">Total Logins</span>
                </div>
              </div>
            </div>
          </div>
        )}

        {activeTab === 'activity' && <UserActivityTab />}
        {activeTab === 'sessions' && <UserSessionsTab />}
        {activeTab === 'subscription' && <UserSubscriptionTab />}
        {activeTab === 'notes' && <UserNotesTab />}
      </div>

      {/* Modals */}
      {showEditModal && <UserEditModal />}
      {showSuspendModal && <SuspendUserModal />}
      {showDeleteModal && <DeleteUserModal />}
      {showImpersonateModal && <ImpersonateUserModal />}
    </div>
  )
}

export default UserDetailPanel
