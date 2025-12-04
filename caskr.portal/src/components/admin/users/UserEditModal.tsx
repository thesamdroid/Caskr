import { useState, useEffect } from 'react'
import { useAppDispatch, useAppSelector } from '../../../hooks'
import { updateUser, setShowEditModal } from '../../../features/userAdminSlice'
import type { UserEditFormData, UserRole } from '../../../types/userAdmin'

function UserEditModal() {
  const dispatch = useAppDispatch()
  const { selectedUser } = useAppSelector(state => state.userAdmin)

  const [formData, setFormData] = useState<UserEditFormData>({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    role: 'User',
    timezone: ''
  })
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    if (selectedUser) {
      setFormData({
        firstName: selectedUser.firstName,
        lastName: selectedUser.lastName,
        email: selectedUser.email,
        phone: selectedUser.phone || '',
        role: selectedUser.role,
        timezone: selectedUser.timezone || ''
      })
    }
  }, [selectedUser])

  const handleChange = (field: keyof UserEditFormData, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedUser) return

    // Validate
    if (!formData.firstName.trim() || !formData.lastName.trim()) {
      setError('First and last name are required')
      return
    }

    if (!formData.email.trim() || !formData.email.includes('@')) {
      setError('Valid email is required')
      return
    }

    setIsSubmitting(true)
    setError('')

    try {
      await dispatch(updateUser({
        userId: selectedUser.id,
        data: formData
      })).unwrap()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update user')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleClose = () => {
    dispatch(setShowEditModal(false))
  }

  const timezones = [
    'America/New_York',
    'America/Chicago',
    'America/Denver',
    'America/Los_Angeles',
    'America/Anchorage',
    'Pacific/Honolulu',
    'UTC'
  ]

  return (
    <div className="modal-overlay">
      <div className="modal user-edit-modal">
        <div className="modal-header">
          <h2>Edit User</h2>
          <button type="button" className="modal-close" onClick={handleClose}>
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            <div className="form-row">
              <div className="form-group">
                <label htmlFor="edit-first-name">First Name</label>
                <input
                  id="edit-first-name"
                  type="text"
                  value={formData.firstName}
                  onChange={e => handleChange('firstName', e.target.value)}
                  required
                />
              </div>
              <div className="form-group">
                <label htmlFor="edit-last-name">Last Name</label>
                <input
                  id="edit-last-name"
                  type="text"
                  value={formData.lastName}
                  onChange={e => handleChange('lastName', e.target.value)}
                  required
                />
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="edit-email">Email</label>
              <input
                id="edit-email"
                type="email"
                value={formData.email}
                onChange={e => handleChange('email', e.target.value)}
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="edit-phone">Phone</label>
              <input
                id="edit-phone"
                type="tel"
                value={formData.phone}
                onChange={e => handleChange('phone', e.target.value)}
                placeholder="+1 (555) 123-4567"
              />
            </div>

            <div className="form-row">
              <div className="form-group">
                <label htmlFor="edit-role">Role</label>
                <select
                  id="edit-role"
                  value={formData.role}
                  onChange={e => handleChange('role', e.target.value as UserRole)}
                >
                  <option value="User">User</option>
                  <option value="Admin">Admin</option>
                  <option value="PricingManager">Pricing Manager</option>
                  <option value="SuperAdmin">Super Admin</option>
                </select>
              </div>
              <div className="form-group">
                <label htmlFor="edit-timezone">Timezone</label>
                <select
                  id="edit-timezone"
                  value={formData.timezone}
                  onChange={e => handleChange('timezone', e.target.value)}
                >
                  <option value="">Not set</option>
                  {timezones.map(tz => (
                    <option key={tz} value={tz}>{tz}</option>
                  ))}
                </select>
              </div>
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
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

export default UserEditModal
