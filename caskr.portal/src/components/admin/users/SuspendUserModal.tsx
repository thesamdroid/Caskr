import { useState } from 'react'
import { useAppDispatch, useAppSelector } from '../../../hooks'
import { suspendUser, setShowSuspendModal } from '../../../features/userAdminSlice'
import type { SuspendUserFormData } from '../../../types/userAdmin'
import { defaultSuspendFormData } from '../../../types/userAdmin'

function SuspendUserModal() {
  const dispatch = useAppDispatch()
  const { selectedUser } = useAppSelector(state => state.userAdmin)

  const [formData, setFormData] = useState<SuspendUserFormData>(defaultSuspendFormData)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState('')

  const handleChange = (field: keyof SuspendUserFormData, value: string | number | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedUser) return

    if (!formData.reason.trim()) {
      setError('Reason is required for suspension')
      return
    }

    if (formData.duration === 'custom' && (!formData.customDays || formData.customDays <= 0)) {
      setError('Please specify a valid number of days')
      return
    }

    setIsSubmitting(true)
    setError('')

    try {
      await dispatch(suspendUser({
        userId: selectedUser.id,
        data: formData
      })).unwrap()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to suspend user')
      setIsSubmitting(false)
    }
  }

  const handleClose = () => {
    dispatch(setShowSuspendModal(false))
  }

  if (!selectedUser) return null

  return (
    <div className="modal-overlay">
      <div className="modal suspend-user-modal">
        <div className="modal-header">
          <h2>Suspend User</h2>
          <button type="button" className="modal-close" onClick={handleClose}>
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            <div className="suspend-warning">
              <p>
                You are about to suspend <strong>{selectedUser.email}</strong>.
                They will be unable to log in until the suspension is lifted.
              </p>
            </div>

            <div className="form-group">
              <label htmlFor="suspend-reason">Reason for Suspension *</label>
              <textarea
                id="suspend-reason"
                value={formData.reason}
                onChange={e => handleChange('reason', e.target.value)}
                placeholder="Explain why this user is being suspended..."
                rows={3}
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="suspend-duration">Duration</label>
              <select
                id="suspend-duration"
                value={formData.duration}
                onChange={e => handleChange('duration', e.target.value)}
              >
                <option value="indefinite">Indefinite</option>
                <option value="24h">24 Hours</option>
                <option value="7d">7 Days</option>
                <option value="30d">30 Days</option>
                <option value="custom">Custom</option>
              </select>
            </div>

            {formData.duration === 'custom' && (
              <div className="form-group">
                <label htmlFor="suspend-custom-days">Number of Days</label>
                <input
                  id="suspend-custom-days"
                  type="number"
                  min={1}
                  max={365}
                  value={formData.customDays || ''}
                  onChange={e => handleChange('customDays', parseInt(e.target.value) || 0)}
                />
              </div>
            )}

            <div className="form-group checkbox-group">
              <label>
                <input
                  type="checkbox"
                  checked={formData.notifyUser}
                  onChange={e => handleChange('notifyUser', e.target.checked)}
                />
                Send email notification to user
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
              className="btn btn-danger"
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Suspending...' : 'Suspend User'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

export default SuspendUserModal
