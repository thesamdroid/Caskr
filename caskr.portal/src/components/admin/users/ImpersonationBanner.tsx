import { useAppDispatch, useAppSelector } from '../../../hooks'
import { endImpersonation } from '../../../features/userAdminSlice'

function ImpersonationBanner() {
  const dispatch = useAppDispatch()
  const { impersonationSession } = useAppSelector(state => state.userAdmin)

  if (!impersonationSession) return null

  const handleEndImpersonation = () => {
    dispatch(endImpersonation())
  }

  const expiresAt = new Date(impersonationSession.expiresAt)
  const remainingMinutes = Math.max(
    0,
    Math.ceil((expiresAt.getTime() - Date.now()) / 60000)
  )

  return (
    <div className="impersonation-banner">
      <div className="banner-content">
        <span className="banner-icon">&#x1F464;</span>
        <span className="banner-text">
          You are impersonating <strong>{impersonationSession.targetUserEmail}</strong>
        </span>
        <span className="banner-time">
          {remainingMinutes} minute{remainingMinutes !== 1 ? 's' : ''} remaining
        </span>
      </div>
      <button
        type="button"
        className="btn btn-warning btn-small"
        onClick={handleEndImpersonation}
      >
        End Impersonation
      </button>
    </div>
  )
}

export default ImpersonationBanner
