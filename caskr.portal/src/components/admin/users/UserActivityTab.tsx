import { useAppSelector } from '../../../hooks'
import { userAuditApi } from '../../../api/userAdminApi'
import { formatRelativeTime } from '../../../types/userAdmin'

function UserActivityTab() {
  const { selectedUser, selectedUserAuditLogs } = useAppSelector(state => state.userAdmin)

  const handleExport = async () => {
    if (!selectedUser) return

    try {
      const result = await userAuditApi.export(selectedUser.id)
      const blob = new Blob([result.csv], { type: 'text/csv' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `user-${selectedUser.id}-audit-log.csv`
      a.click()
      URL.revokeObjectURL(url)
    } catch (error) {
      console.error('Export failed:', error)
    }
  }

  const getActionClass = (action: string): string => {
    const lowerAction = action.toLowerCase()
    if (lowerAction.includes('login') || lowerAction.includes('create')) return 'action-success'
    if (lowerAction.includes('delete') || lowerAction.includes('suspend')) return 'action-danger'
    if (lowerAction.includes('update') || lowerAction.includes('change')) return 'action-info'
    return 'action-default'
  }

  const formatAction = (action: string): string => {
    return action
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, str => str.toUpperCase())
      .trim()
  }

  return (
    <div className="user-activity-tab">
      <div className="tab-header">
        <h3>Activity Log</h3>
        <button
          type="button"
          className="btn btn-secondary btn-small"
          onClick={handleExport}
        >
          Export CSV
        </button>
      </div>

      {selectedUserAuditLogs.length === 0 ? (
        <div className="empty-state">
          <p>No activity recorded for this user.</p>
        </div>
      ) : (
        <div className="activity-timeline">
          {selectedUserAuditLogs.map(log => (
            <div key={log.id} className="activity-item">
              <div className="activity-marker">
                <div className={`marker-dot ${getActionClass(log.action)}`} />
              </div>
              <div className="activity-content">
                <div className="activity-header">
                  <span className={`activity-action ${getActionClass(log.action)}`}>
                    {formatAction(log.action)}
                  </span>
                  <span className="activity-time">
                    {formatRelativeTime(log.createdAt)}
                  </span>
                </div>
                {log.details && (
                  <p className="activity-details">{log.details}</p>
                )}
                <div className="activity-meta">
                  {log.performedByName && log.performedBy !== selectedUser?.id && (
                    <span className="meta-item">
                      By: {log.performedByName}
                    </span>
                  )}
                  {log.ipAddress && (
                    <span className="meta-item">
                      IP: {log.ipAddress}
                    </span>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

export default UserActivityTab
