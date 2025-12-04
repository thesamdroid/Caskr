import { useState } from 'react'
import { useAppDispatch, useAppSelector } from '../../../hooks'
import { fetchAuditLogs } from '../../../features/pricingAdminSlice'
import { auditLogsApi } from '../../../api/pricingAdminApi'
import type { PricingAuditLog } from '../../../types/pricing'

const ENTITY_TYPES = ['PricingTier', 'PricingFeature', 'PricingTierFeature', 'PricingFaq', 'PricingPromotion']

function AuditLogTab() {
  const dispatch = useAppDispatch()
  const { auditLogs, isLoading } = useAppSelector(state => state.pricingAdmin)

  const [filters, setFilters] = useState({
    entityType: '',
    startDate: '',
    endDate: '',
    limit: 100
  })
  const [expandedLog, setExpandedLog] = useState<number | null>(null)

  const handleFilterChange = (key: string, value: string | number) => {
    setFilters(prev => ({ ...prev, [key]: value }))
  }

  const handleApplyFilters = () => {
    dispatch(fetchAuditLogs({
      entityType: filters.entityType || undefined,
      startDate: filters.startDate || undefined,
      endDate: filters.endDate || undefined,
      limit: filters.limit
    }))
  }

  const handleExportCsv = async () => {
    try {
      const result = await auditLogsApi.exportCsv({
        startDate: filters.startDate || undefined,
        endDate: filters.endDate || undefined
      })
      const blob = new Blob([result.csv], { type: 'text/csv' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `pricing-audit-log-${new Date().toISOString().slice(0, 10)}.csv`
      a.click()
      URL.revokeObjectURL(url)
    } catch (error) {
      console.error('Failed to export:', error)
    }
  }

  const formatAction = (action: string): string => {
    return action.replace(/([A-Z])/g, ' $1').trim()
  }

  const formatEntityType = (entityType: string): string => {
    return entityType.replace('Pricing', '').replace(/([A-Z])/g, ' $1').trim()
  }

  const formatTimestamp = (timestamp: string): string => {
    const date = new Date(timestamp)
    return date.toLocaleString()
  }

  const parseJsonValue = (value: string | undefined): object | null => {
    if (!value) return null
    try {
      return JSON.parse(value)
    } catch {
      return null
    }
  }

  const renderDiff = (log: PricingAuditLog) => {
    const oldValue = parseJsonValue(log.oldValue)
    const newValue = parseJsonValue(log.newValue)

    if (!oldValue && !newValue) return null

    const allKeys = new Set([
      ...(oldValue ? Object.keys(oldValue) : []),
      ...(newValue ? Object.keys(newValue) : [])
    ])

    const changes: { key: string; old: unknown; new: unknown }[] = []
    allKeys.forEach(key => {
      const oldVal = oldValue?.[key as keyof typeof oldValue]
      const newVal = newValue?.[key as keyof typeof newValue]
      if (JSON.stringify(oldVal) !== JSON.stringify(newVal)) {
        changes.push({ key, old: oldVal, new: newVal })
      }
    })

    if (changes.length === 0) return null

    return (
      <div className="diff-view">
        <table className="diff-table">
          <thead>
            <tr>
              <th>Field</th>
              <th>Old Value</th>
              <th>New Value</th>
            </tr>
          </thead>
          <tbody>
            {changes.map(change => (
              <tr key={change.key}>
                <td className="field-name">{change.key}</td>
                <td className="old-value">
                  {change.old !== undefined ? String(change.old) : '-'}
                </td>
                <td className="new-value">
                  {change.new !== undefined ? String(change.new) : '-'}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    )
  }

  const getActionBadgeClass = (action: string): string => {
    if (action.toLowerCase().includes('create')) return 'badge-success'
    if (action.toLowerCase().includes('delete')) return 'badge-danger'
    if (action.toLowerCase().includes('update')) return 'badge-info'
    return 'badge-secondary'
  }

  return (
    <div className="audit-log-tab">
      <div className="tab-header">
        <h2>Audit Log</h2>
        <button
          type="button"
          className="btn btn-secondary"
          onClick={handleExportCsv}
        >
          Export to CSV
        </button>
      </div>

      {/* Filters */}
      <div className="audit-filters">
        <div className="filter-row">
          <div className="filter-group">
            <label htmlFor="filter-entity">Entity Type</label>
            <select
              id="filter-entity"
              value={filters.entityType}
              onChange={e => handleFilterChange('entityType', e.target.value)}
            >
              <option value="">All Types</option>
              {ENTITY_TYPES.map(type => (
                <option key={type} value={type}>
                  {formatEntityType(type)}
                </option>
              ))}
            </select>
          </div>

          <div className="filter-group">
            <label htmlFor="filter-start">Start Date</label>
            <input
              id="filter-start"
              type="date"
              value={filters.startDate}
              onChange={e => handleFilterChange('startDate', e.target.value)}
            />
          </div>

          <div className="filter-group">
            <label htmlFor="filter-end">End Date</label>
            <input
              id="filter-end"
              type="date"
              value={filters.endDate}
              onChange={e => handleFilterChange('endDate', e.target.value)}
            />
          </div>

          <div className="filter-group">
            <label htmlFor="filter-limit">Limit</label>
            <select
              id="filter-limit"
              value={filters.limit}
              onChange={e => handleFilterChange('limit', parseInt(e.target.value))}
            >
              <option value={50}>50</option>
              <option value={100}>100</option>
              <option value={250}>250</option>
              <option value={500}>500</option>
            </select>
          </div>

          <button
            type="button"
            className="btn btn-primary"
            onClick={handleApplyFilters}
          >
            Apply Filters
          </button>
        </div>
      </div>

      {/* Audit Log Table */}
      {isLoading ? (
        <div className="loading-state">
          <div className="loading-spinner" />
          <p>Loading audit logs...</p>
        </div>
      ) : (
        <div className="audit-log-list">
          {auditLogs.map(log => (
            <div
              key={log.id}
              className={`audit-log-item ${expandedLog === log.id ? 'expanded' : ''}`}
            >
              <div
                className="audit-log-header"
                onClick={() => setExpandedLog(expandedLog === log.id ? null : log.id)}
              >
                <div className="log-timestamp">
                  {formatTimestamp(log.createdAt)}
                </div>

                <div className="log-user">
                  {log.userName || `User #${log.userId}`}
                </div>

                <div className="log-action">
                  <span className={`badge ${getActionBadgeClass(log.action)}`}>
                    {formatAction(log.action)}
                  </span>
                </div>

                <div className="log-entity">
                  <span className="entity-type">{formatEntityType(log.entityType)}</span>
                  {log.entityId && <span className="entity-id">#{log.entityId}</span>}
                </div>

                <div className="log-expand">
                  {expandedLog === log.id ? '\u25BC' : '\u25B6'}
                </div>
              </div>

              {expandedLog === log.id && (
                <div className="audit-log-details">
                  {log.reason && (
                    <div className="log-reason">
                      <strong>Reason:</strong> {log.reason}
                    </div>
                  )}

                  {log.ipAddress && (
                    <div className="log-ip">
                      <strong>IP Address:</strong> {log.ipAddress}
                    </div>
                  )}

                  {renderDiff(log)}

                  {!parseJsonValue(log.oldValue) && !parseJsonValue(log.newValue) && (
                    <div className="log-raw">
                      {log.oldValue && (
                        <div>
                          <strong>Old Value:</strong>
                          <pre>{log.oldValue}</pre>
                        </div>
                      )}
                      {log.newValue && (
                        <div>
                          <strong>New Value:</strong>
                          <pre>{log.newValue}</pre>
                        </div>
                      )}
                    </div>
                  )}
                </div>
              )}
            </div>
          ))}

          {auditLogs.length === 0 && (
            <div className="empty-state">
              <p>No audit logs found matching the filters.</p>
            </div>
          )}
        </div>
      )}
    </div>
  )
}

export default AuditLogTab
