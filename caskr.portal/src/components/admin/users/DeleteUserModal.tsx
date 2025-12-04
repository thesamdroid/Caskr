import { useState } from 'react'
import { useAppDispatch, useAppSelector } from '../../../hooks'
import { deleteUser, setShowDeleteModal } from '../../../features/userAdminSlice'
import type { DeleteUserFormData } from '../../../types/userAdmin'
import { defaultDeleteFormData } from '../../../types/userAdmin'

function DeleteUserModal() {
  const dispatch = useAppDispatch()
  const { selectedUser } = useAppSelector(state => state.userAdmin)

  const [formData, setFormData] = useState<DeleteUserFormData>(defaultDeleteFormData)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState('')

  const handleChange = (field: keyof DeleteUserFormData, value: string | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedUser) return

    if (!formData.reason.trim()) {
      setError('Reason is required for deletion')
      return
    }

    if (formData.confirmEmail !== selectedUser.email) {
      setError('Email confirmation does not match')
      return
    }

    setIsSubmitting(true)
    setError('')

    try {
      await dispatch(deleteUser({
        userId: selectedUser.id,
        data: formData
      })).unwrap()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete user')
      setIsSubmitting(false)
    }
  }

  const handleClose = () => {
    dispatch(setShowDeleteModal(false))
  }

  if (!selectedUser) return null

  return (
    <div className="modal-overlay">
      <div className="modal delete-user-modal">
        <div className="modal-header danger">
          <h2>Delete User</h2>
          <button type="button" className="modal-close" onClick={handleClose}>
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            <div className="delete-warning">
              <div className="warning-icon">&#x26A0;</div>
              <p>
                <strong>Warning:</strong> You are about to delete the user account for{' '}
                <strong>{selectedUser.email}</strong>. This action cannot be easily undone.
              </p>
            </div>

            <div className="form-group">
              <label htmlFor="delete-reason">Reason for Deletion *</label>
              <textarea
                id="delete-reason"
                value={formData.reason}
                onChange={e => handleChange('reason', e.target.value)}
                placeholder="Explain why this user is being deleted..."
                rows={3}
                required
              />
            </div>

            <div className="form-group checkbox-group">
              <label className="danger-checkbox">
                <input
                  type="checkbox"
                  checked={formData.deleteData}
                  onChange={e => handleChange('deleteData', e.target.checked)}
                />
                <span>
                  Also delete all user data (barrels, batches, reports, etc.)
                  <br />
                  <small className="text-danger">
                    This is permanent and cannot be recovered!
                  </small>
                </span>
              </label>
            </div>

            <div className="form-group">
              <label htmlFor="delete-confirm">
                Type <strong>{selectedUser.email}</strong> to confirm
              </label>
              <input
                id="delete-confirm"
                type="text"
                value={formData.confirmEmail}
                onChange={e => handleChange('confirmEmail', e.target.value)}
                placeholder={selectedUser.email}
                autoComplete="off"
              />
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
              disabled={isSubmitting || formData.confirmEmail !== selectedUser.email}
            >
              {isSubmitting ? 'Deleting...' : 'Delete User Permanently'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

export default DeleteUserModal
