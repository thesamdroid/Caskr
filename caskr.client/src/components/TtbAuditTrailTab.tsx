import { useEffect, useState, useCallback } from 'react'
import { authorizedFetch } from '../api/authorizedFetch'

interface TtbAuditLogEntry {
  id: number
  companyId: number
  entityType: string
  entityId: number
  action: 'Create' | 'Update' | 'Delete'
  changedByUserId: number
  changedByUserName: string | null
  changeTimestamp: string
  oldValues: string | null
  newValues: string | null
  ipAddress: string | null
  changeDescription: string | null
}

interface TtbAuditLogListResponse {
  items: TtbAuditLogEntry[]
  totalCount: number
  page: number
  pageSize: number
}

interface TtbAuditTrailTabProps {
  companyId: number
}

const actionBadges: Record<string, string> = {
  Create: 'approved',
  Update: 'draft',
  Delete: 'rejected'
}

const entityTypeLabels: Record<string, string> = {
  TtbTransaction: 'Transaction',
  TtbMonthlyReport: 'Monthly Report',
  TtbGaugeRecord: 'Gauge Record',
  TtbTaxDetermination: 'Tax Determination'
}

const formatDateTime = (value: string) => {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    timeZoneName: 'short'
  }).format(new Date(value))
}

function TtbAuditTrailTab({ companyId }: TtbAuditTrailTabProps) {
  const [logs, setLogs] = useState<TtbAuditLogEntry[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize] = useState(20)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [isExporting, setIsExporting] = useState(false)

  // Filters
  const [startDate, setStartDate] = useState<string>('')
  const [endDate, setEndDate] = useState<string>('')
  const [entityTypeFilter, setEntityTypeFilter] = useState<string>('')
  const [actionFilter, setActionFilter] = useState<string>('')

  const fetchAuditLogs = useCallback(async () => {
    setIsLoading(true)
    setError(null)

    try {
      const params = new URLSearchParams({
        companyId: companyId.toString(),
        page: page.toString(),
        pageSize: pageSize.toString()
      })

      if (startDate) {
        params.append('startDate', new Date(startDate).toISOString())
      }
      if (endDate) {
        params.append('endDate', new Date(endDate).toISOString())
      }
      if (entityTypeFilter) {
        params.append('entityType', entityTypeFilter)
      }
      if (actionFilter) {
        params.append('action', actionFilter)
      }

      const response = await authorizedFetch(`/api/ttb/audit-trail?${params.toString()}`)

      if (!response.ok) {
        throw new Error('Failed to fetch audit logs')
      }

      const data: TtbAuditLogListResponse = await response.json()
      setLogs(data.items)
      setTotalCount(data.totalCount)
    } catch (err) {
      console.error('[TtbAuditTrailTab] Error fetching audit logs', err)
      setError('Unable to load audit trail. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }, [companyId, page, pageSize, startDate, endDate, entityTypeFilter, actionFilter])

  useEffect(() => {
    fetchAuditLogs()
  }, [fetchAuditLogs])

  const handleExportCsv = async () => {
    setIsExporting(true)
    setError(null)

    try {
      const params = new URLSearchParams({
        companyId: companyId.toString()
      })

      if (startDate) {
        params.append('startDate', new Date(startDate).toISOString())
      }
      if (endDate) {
        params.append('endDate', new Date(endDate).toISOString())
      }

      const response = await authorizedFetch(`/api/ttb/audit-trail/export?${params.toString()}`)

      if (!response.ok) {
        throw new Error('Failed to export audit logs')
      }

      const blob = await response.blob()
      const url = URL.createObjectURL(blob)
      const anchor = document.createElement('a')
      anchor.href = url
      anchor.download = `TTB_Audit_Trail_${companyId}_${new Date().toISOString().slice(0, 10)}.csv`
      anchor.rel = 'noopener'
      document.body.appendChild(anchor)
      anchor.click()
      document.body.removeChild(anchor)
      URL.revokeObjectURL(url)
    } catch (err) {
      console.error('[TtbAuditTrailTab] Error exporting audit logs', err)
      setError('Unable to export audit trail. Please try again.')
    } finally {
      setIsExporting(false)
    }
  }

  const handleApplyFilters = () => {
    setPage(1)
    fetchAuditLogs()
  }

  const handleClearFilters = () => {
    setStartDate('')
    setEndDate('')
    setEntityTypeFilter('')
    setActionFilter('')
    setPage(1)
  }

  const totalPages = Math.ceil(totalCount / pageSize)

  return (
    <div className='audit-trail-tab'>
      <div className='filter-panel' aria-label='Audit trail filters'>
        <label>
          <span>Start Date</span>
          <input
            type='date'
            value={startDate}
            onChange={e => setStartDate(e.target.value)}
          />
        </label>

        <label>
          <span>End Date</span>
          <input
            type='date'
            value={endDate}
            onChange={e => setEndDate(e.target.value)}
          />
        </label>

        <label>
          <span>Entity Type</span>
          <select value={entityTypeFilter} onChange={e => setEntityTypeFilter(e.target.value)}>
            <option value=''>All Types</option>
            <option value='TtbTransaction'>Transaction</option>
            <option value='TtbMonthlyReport'>Monthly Report</option>
            <option value='TtbGaugeRecord'>Gauge Record</option>
            <option value='TtbTaxDetermination'>Tax Determination</option>
          </select>
        </label>

        <label>
          <span>Action</span>
          <select value={actionFilter} onChange={e => setActionFilter(e.target.value)}>
            <option value=''>All Actions</option>
            <option value='Create'>Create</option>
            <option value='Update'>Update</option>
            <option value='Delete'>Delete</option>
          </select>
        </label>

        <div className='filter-actions'>
          <button type='button' className='button-secondary' onClick={handleApplyFilters}>
            Apply Filters
          </button>
          <button type='button' className='button-ghost' onClick={handleClearFilters}>
            Clear
          </button>
        </div>
      </div>

      <div className='section-header' style={{ marginTop: '1rem' }}>
        <div>
          <p className='section-subtitle'>
            Showing {logs.length} of {totalCount} audit entries
          </p>
        </div>
        <div className='section-actions'>
          <button
            type='button'
            className='button-primary'
            onClick={handleExportCsv}
            disabled={isExporting}
            aria-label='Export audit trail to CSV for TTB inspections'
          >
            {isExporting ? 'Exporting...' : 'Export to CSV'}
          </button>
        </div>
      </div>

      {error && (
        <div className='alert alert-error' role='alert'>
          {error}
        </div>
      )}

      {isLoading ? (
        <div className='empty-state'>
          <div className='empty-state-icon'>Loading...</div>
          <h3 className='empty-state-title'>Loading audit trail</h3>
          <p className='empty-state-text'>Fetching compliance activity records...</p>
        </div>
      ) : logs.length === 0 ? (
        <div className='empty-state'>
          <div className='empty-state-icon'>No entries</div>
          <h3 className='empty-state-title'>No audit entries found</h3>
          <p className='empty-state-text'>
            Audit entries will appear here when TTB data is created, updated, or deleted.
          </p>
        </div>
      ) : (
        <>
          <div className='table-container'>
            <table className='table' role='table' aria-label='TTB audit trail entries'>
              <thead>
                <tr>
                  <th scope='col'>Timestamp (UTC)</th>
                  <th scope='col'>User</th>
                  <th scope='col'>Entity</th>
                  <th scope='col'>Action</th>
                  <th scope='col'>Description</th>
                  <th scope='col'>IP Address</th>
                </tr>
              </thead>
              <tbody>
                {logs.map(log => (
                  <tr key={log.id}>
                    <td>{formatDateTime(log.changeTimestamp)}</td>
                    <td>{log.changedByUserName ?? `User #${log.changedByUserId}`}</td>
                    <td>
                      {entityTypeLabels[log.entityType] ?? log.entityType} #{log.entityId}
                    </td>
                    <td>
                      <span className={`status-badge ${actionBadges[log.action] ?? 'draft'}`}>
                        {log.action}
                      </span>
                    </td>
                    <td title={log.changeDescription ?? ''}>
                      {log.changeDescription
                        ? log.changeDescription.length > 80
                          ? `${log.changeDescription.slice(0, 80)}...`
                          : log.changeDescription
                        : '—'}
                    </td>
                    <td>{log.ipAddress ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {totalPages > 1 && (
            <div className='pagination' role='navigation' aria-label='Audit trail pagination'>
              <button
                type='button'
                className='button-ghost'
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
              >
                Previous
              </button>
              <span className='pagination-info'>
                Page {page} of {totalPages}
              </span>
              <button
                type='button'
                className='button-ghost'
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
              >
                Next
              </button>
            </div>
          )}
        </>
      )}
    </div>
  )
}

export default TtbAuditTrailTab
