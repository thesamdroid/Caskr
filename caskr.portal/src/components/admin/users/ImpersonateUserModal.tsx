import { useState } from 'react'
import { useAppDispatch, useAppSelector } from '../../../hooks'
import { startImpersonation, setShowImpersonateModal } from '../../../features/userAdminSlice'
import type { ImpersonateUserFormData } from '../../../types/userAdmin'
import { defaultImpersonateFormData } from '../../../types/userAdmin'

function ImpersonateUserModal() {
  const dispatch = useAppDispatch()
  const { selectedUser } = useAppSelector(state => state.userAdmin)

  const [formData, setFormData] = useState<ImpersonateUserFormData>(defaultImpersonateFormData)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState('')

  const handleChange = (field: keyof ImpersonateUserFormData, value: string | number | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedUser) return

    if (!formData.reason.trim()) {
      setError('Reason is required for impersonation')
      return
    }

    if (!formData.acknowledgement) {
      setError('You must acknowledge the impersonation policy')
      return
    }

    setIsSubmitting(true)
    setError('')

    try {
      await dispatch(startImpersonation({
        userId: selectedUser.id,
        data: formData
      })).unwrap()

      // Redirect to dashboard as the impersonated user
      window.location.href = '/dashboard'
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to start impersonation')
      setIsSubmitting(false)
    }
  }

  const handleClose = () => {
    dispatch(setShowImpersonateModal(false))
  }

  if (!selectedUser) return null

  return (
    <div className="modal-overlay">
      <div className="modal impersonate-user-modal">
        <div className="modal-header">
          <h2>Impersonate User</h2>
          <button type="button" className="modal-close" onClick={handleClose}>
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            <div className="impersonate-info">
              <p>
                You are about to impersonate <strong>{selectedUser.email}</strong>.
                This will allow you to view the application as this user sees it.
              </p>
              <p className="impersonate-warning">
                All actions taken while impersonating will be logged and attributed
                to your admin account.
              </p>
            </div>

            <div className="form-group">
              <label htmlFor="impersonate-reason">Reason for Impersonation *</label>
              <textarea
                id="impersonate-reason"
                value={formData.reason}
                onChange={e => handleChange('reason', e.target.value)}
                placeholder="e.g., Investigating support ticket #1234..."
                rows={3}
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="impersonate-duration">Session Duration</label>
              <select
                id="impersonate-duration"
                value={formData.duration}
                onChange={e => handleChange('duration', parseInt(e.target.value))}
              >
                <option value={15}>15 minutes</option>
                <option value={30}>30 minutes</option>
                <option value={60}>1 hour</option>
                <option value={120}>2 hours</option>
              </select>
              <small className="form-hint">
                You will be automatically logged out after this duration.
              </small>
            </div>

            <div className="form-group checkbox-group">
              <label className="policy-checkbox">
                <input
                  type="checkbox"
                  checked={formData.acknowledgement}
                  onChange={e => handleChange('acknowledgement', e.target.checked)}
                />
                <span>
                  I acknowledge that this impersonation session will be logged and
                  that I am only using this feature for legitimate support or
                  administrative purposes.
                </span>
              </label>
            </div>

            {error && <div className="form-error">{error}</div>}
          </div>

          <div className="modal-footer">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={handleClose}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={isSubmitting || !formData.acknowledgement}
            >
              {isSubmitting ? 'Starting...' : 'Start Impersonation'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

export default ImpersonateUserModal
