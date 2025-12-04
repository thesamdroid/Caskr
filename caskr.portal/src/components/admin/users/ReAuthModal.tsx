import { useState } from 'react'
import { useAppDispatch, useAppSelector } from '../../../hooks'
import { reAuthenticate, setShowReAuthModal } from '../../../features/userAdminSlice'

function ReAuthModal() {
  const dispatch = useAppDispatch()
  const { error } = useAppSelector(state => state.userAdmin)
  const [password, setPassword] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!password.trim()) return

    setIsSubmitting(true)
    try {
      await dispatch(reAuthenticate(password)).unwrap()
    } catch {
      // Error handled by slice
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleClose = () => {
    dispatch(setShowReAuthModal(false))
  }

  return (
    <div className="modal-overlay">
      <div className="modal re-auth-modal">
        <div className="modal-header">
          <h2>Re-Authentication Required</h2>
        </div>

        <div className="modal-body">
          <div className="re-auth-warning">
            <div className="warning-icon">&#x1F510;</div>
            <p>
              For security purposes, you must re-enter your password to access
              the Super Admin portal.
            </p>
          </div>

          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label htmlFor="reauth-password">Your Password</label>
              <input
                id="reauth-password"
                type="password"
                value={password}
                onChange={e => setPassword(e.target.value)}
                placeholder="Enter your password"
                autoFocus
                autoComplete="current-password"
              />
            </div>

            {error && (
              <div className="form-error">{error}</div>
            )}

            <div className="modal-actions">
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
                disabled={isSubmitting || !password.trim()}
              >
                {isSubmitting ? 'Verifying...' : 'Verify Identity'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}

export default ReAuthModal
