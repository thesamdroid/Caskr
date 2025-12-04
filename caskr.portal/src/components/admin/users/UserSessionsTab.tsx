import { useAppDispatch, useAppSelector } from '../../../hooks'
import {
  terminateSession,
  terminateAllSessions
} from '../../../features/userAdminSlice'
import { formatRelativeTime } from '../../../types/userAdmin'

function UserSessionsTab() {
  const dispatch = useAppDispatch()
  const { selectedUser, selectedUserSessions } = useAppSelector(state => state.userAdmin)

  const handleTerminateSession = (sessionId: string) => {
    if (!selectedUser) return
    if (confirm('Terminate this session?')) {
      dispatch(terminateSession({ userId: selectedUser.id, sessionId }))
    }
  }

  const handleTerminateAll = () => {
    if (!selectedUser) return
    if (confirm('Terminate all sessions for this user? They will be logged out everywhere.')) {
      dispatch(terminateAllSessions(selectedUser.id))
    }
  }

  const parseUserAgent = (ua: string): { browser: string; os: string } => {
    let browser = 'Unknown'
    let os = 'Unknown'

    // Simple UA parsing
    if (ua.includes('Chrome')) browser = 'Chrome'
    else if (ua.includes('Firefox')) browser = 'Firefox'
    else if (ua.includes('Safari')) browser = 'Safari'
    else if (ua.includes('Edge')) browser = 'Edge'

    if (ua.includes('Windows')) os = 'Windows'
    else if (ua.includes('Mac')) os = 'macOS'
    else if (ua.includes('Linux')) os = 'Linux'
    else if (ua.includes('iPhone') || ua.includes('iPad')) os = 'iOS'
    else if (ua.includes('Android')) os = 'Android'

    return { browser, os }
  }

  return (
    <div className="user-sessions-tab">
      <div className="tab-header">
        <h3>Active Sessions</h3>
        {selectedUserSessions.length > 0 && (
          <button
            type="button"
            className="btn btn-danger btn-small"
            onClick={handleTerminateAll}
          >
            Terminate All
          </button>
        )}
      </div>

      {selectedUserSessions.length === 0 ? (
        <div className="empty-state">
          <p>No active sessions for this user.</p>
        </div>
      ) : (
        <div className="sessions-list">
          {selectedUserSessions.map(session => {
            const { browser, os } = parseUserAgent(session.userAgent)
            const isExpired = new Date(session.expiresAt) < new Date()

            return (
              <div
                key={session.id}
                className={`session-item ${session.isCurrent ? 'current' : ''} ${isExpired ? 'expired' : ''}`}
              >
                <div className="session-icon">
                  {os === 'Windows' && '\u{1F5A5}'}
                  {os === 'macOS' && '\u{1F4BB}'}
                  {os === 'Linux' && '\u{1F427}'}
                  {os === 'iOS' && '\u{1F4F1}'}
                  {os === 'Android' && '\u{1F4F1}'}
                  {os === 'Unknown' && '\u{1F310}'}
                </div>

                <div className="session-info">
                  <div className="session-header">
                    <span className="session-browser">{browser} on {os}</span>
                    {session.isCurrent && (
                      <span className="badge badge-success">Current</span>
                    )}
                    {isExpired && (
                      <span className="badge badge-secondary">Expired</span>
                    )}
                  </div>
                  <div className="session-details">
                    <span className="session-ip">IP: {session.ipAddress}</span>
                    <span className="session-time">
                      Last active: {formatRelativeTime(session.lastActivityAt)}
                    </span>
                  </div>
                  <div className="session-meta">
                    <span>Created: {new Date(session.createdAt).toLocaleString()}</span>
                    <span>Expires: {new Date(session.expiresAt).toLocaleString()}</span>
                  </div>
                </div>

                <div className="session-actions">
                  <button
                    type="button"
                    className="btn btn-danger btn-small"
                    onClick={() => handleTerminateSession(session.id)}
                    disabled={session.isCurrent}
                    title={session.isCurrent ? 'Cannot terminate current session' : 'Terminate session'}
                  >
                    Terminate
                  </button>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}

export default UserSessionsTab
