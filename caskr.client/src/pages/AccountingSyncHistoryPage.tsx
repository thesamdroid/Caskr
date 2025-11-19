import { useEffect, useMemo, useState } from 'react'
import { authorizedFetch } from '../api/authorizedFetch'
import { ToastState } from '../types/toast'

const STATUS_OPTIONS = ['All', 'Pending', 'InProgress', 'Success', 'Failed'] as const
const ENTITY_TYPES = ['All', 'Invoice', 'Batch', 'Payment'] as const
const PAGE_SIZES = [10, 25, 50] as const

type StatusFilter = (typeof STATUS_OPTIONS)[number]
type EntityFilter = (typeof ENTITY_TYPES)[number]

type QuickBooksSyncStatus = 'Pending' | 'InProgress' | 'Success' | 'Failed'
export type QuickBooksSyncEntityType = 'Invoice' | 'Batch' | 'Payment'

interface QuickBooksSyncLog {
  id: string | number
  syncedAt: string
  entityType: QuickBooksSyncEntityType
  entityId: number
  status: QuickBooksSyncStatus
  qboId?: string | null
  errorMessage?: string | null
}

interface SyncLogsResponse {
  data: QuickBooksSyncLog[]
  total: number
  page: number
  limit: number
}

const formatDateTime = (value: string) => {
  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) {
    return '—'
  }
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(parsed)
}

const getQuickBooksUrl = (entityType: QuickBooksSyncEntityType, qboId: string) => {
  const normalized = entityType.toLowerCase()
  return `https://app.qbo.intuit.com/app/${normalized}?txnId=${encodeURIComponent(qboId)}`
}

const getEntityLink = (entityType: QuickBooksSyncEntityType, entityId: number) => {
  switch (entityType) {
    case 'Invoice':
      return `/orders/${entityId}`
    case 'Batch':
      return `/barrels?batchId=${entityId}`
    case 'Payment':
      return `/payments/${entityId}`
    default:
      return '#'
  }
}

const getRetryEndpoint = (log: QuickBooksSyncLog) => {
  if (log.entityType === 'Invoice') {
    return {
      url: 'api/accounting/quickbooks/sync-invoice',
      payload: { invoiceId: log.entityId }
    }
  }

  if (log.entityType === 'Batch') {
    return {
      url: 'api/accounting/quickbooks/sync-batch',
      payload: { batchId: log.entityId }
    }
  }

  return null
}

const createToast = (type: ToastState['type'], message: string): ToastState => ({ type, message })

function AccountingSyncHistoryPage() {
  const companyId = 1
  const [logs, setLogs] = useState<QuickBooksSyncLog[]>([])
  const [total, setTotal] = useState(0)
  const [status, setStatus] = useState<StatusFilter>('All')
  const [entityType, setEntityType] = useState<EntityFilter>('All')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState<(typeof PAGE_SIZES)[number]>(25)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [toast, setToast] = useState<ToastState | null>(null)
  const [refreshKey, setRefreshKey] = useState(0)
  const [retrying, setRetrying] = useState<Record<string, boolean>>({})
  const [bulkRetrying, setBulkRetrying] = useState(false)

  useEffect(() => {
    if (!toast) return
    const timer = window.setTimeout(() => setToast(null), 4000)
    return () => window.clearTimeout(timer)
  }, [toast])

  useEffect(() => {
    const abortController = new AbortController()
    const loadLogs = async () => {
      setLoading(true)
      setError(null)
      const params = new URLSearchParams({
        companyId: String(companyId),
        page: String(page),
        limit: String(pageSize)
      })

      if (status !== 'All') {
        params.set('status', status)
      }

      if (entityType !== 'All') {
        params.set('entityType', entityType)
      }

      if (startDate) {
        params.set('startDate', startDate)
      }

      if (endDate) {
        params.set('endDate', endDate)
      }

      try {
        const response = await authorizedFetch(`api/accounting/quickbooks/sync-logs?${params.toString()}`, {
          signal: abortController.signal
        })
        if (!response.ok) {
          throw new Error('Unable to load sync history')
        }

        const payload = (await response.json()) as SyncLogsResponse
        setLogs(payload.data)
        setTotal(payload.total)
      } catch (err) {
        if (abortController.signal.aborted) return
        const message = err instanceof Error ? err.message : 'Unable to load sync history'
        setError(message)
        setLogs([])
      } finally {
        if (!abortController.signal.aborted) {
          setLoading(false)
        }
      }
    }

    void loadLogs()

    return () => abortController.abort()
  }, [companyId, status, entityType, startDate, endDate, page, pageSize, refreshKey])

  const pageCount = useMemo(() => {
    if (total === 0) return 1
    return Math.max(1, Math.ceil(total / pageSize))
  }, [total, pageSize])

  const failedLogs = useMemo(
    () => logs.filter(log => log.status === 'Failed' && Boolean(getRetryEndpoint(log))),
    [logs]
  )

  const handlePageChange = (nextPage: number) => {
    setPage(Math.min(Math.max(nextPage, 1), pageCount))
  }

  const handlePageSizeChange = (value: number) => {
    setPageSize(value as (typeof PAGE_SIZES)[number])
    setPage(1)
  }

  const retrySync = async (log: QuickBooksSyncLog, { suppressRefresh = false } = {}) => {
    const retryConfig = getRetryEndpoint(log)
    if (!retryConfig) {
      setToast(createToast('error', 'Retry is only available for invoices and batches.'))
      return
    }

    const logKey = `${log.id}`
    setRetrying(prev => ({ ...prev, [logKey]: true }))
    try {
      const response = await authorizedFetch(retryConfig.url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...retryConfig.payload, companyId })
      })

      if (!response.ok) {
        const payload = (await response.json().catch(() => ({}))) as { message?: string }
        throw new Error(payload.message ?? 'Retry failed')
      }

      if (!suppressRefresh) {
        setRefreshKey(prev => prev + 1)
      }
      setToast(createToast('success', `Retry started for ${log.entityType.toLowerCase()} ${log.entityId}.`))
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Retry failed'
      setToast(createToast('error', message))
    } finally {
      setRetrying(prev => {
        const next = { ...prev }
        delete next[logKey]
        return next
      })
    }
  }

  const handleBulkRetry = async () => {
    if (failedLogs.length === 0) return
    setBulkRetrying(true)
    for (const log of failedLogs) {
      await retrySync(log, { suppressRefresh: true })
    }
    setBulkRetrying(false)
    setRefreshKey(prev => prev + 1)
  }

  const startIndex = total === 0 ? 0 : (page - 1) * pageSize + 1
  const endIndex = total === 0 ? 0 : Math.min(total, page * pageSize)

  const clearFilters = () => {
    setStatus('All')
    setEntityType('All')
    setStartDate('')
    setEndDate('')
    setPage(1)
  }

  const handleExport = () => {
    if (logs.length === 0) {
      setToast(createToast('error', 'No rows to export.'))
      return
    }

    const headers = ['Synced At', 'Entity Type', 'Entity ID', 'Status', 'QBO ID', 'Error Message']
    const rows = logs.map(log => [
      formatDateTime(log.syncedAt),
      log.entityType,
      log.entityId,
      log.status,
      log.qboId ?? '',
      log.errorMessage ?? ''
    ])

    const csvContent = [headers, ...rows]
      .map(row => row.map(value => `"${String(value).replace(/"/g, '""')}"`).join(','))
      .join('\n')

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `quickbooks-sync-history-${Date.now()}.csv`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)
    setToast(createToast('success', 'CSV export started.'))
  }

  return (
    <section className="content-section accounting-sync-history" aria-labelledby="sync-history-title">
      <div className="section-header accounting-sync-header">
        <div>
          <h1 id="sync-history-title" className="section-title">Accounting Sync History</h1>
          <p className="section-subtitle">Review QuickBooks sync operations, filter by status, and retry failed attempts.</p>
        </div>
        <div className="section-actions">
          <button type="button" className="button-secondary" onClick={handleExport} aria-label="Export sync history to CSV">
            Export CSV
          </button>
          <button
            type="button"
            className="button-primary"
            onClick={handleBulkRetry}
            disabled={failedLogs.length === 0 || bulkRetrying}
            aria-label="Retry all failed sync operations"
          >
            {bulkRetrying ? 'Retrying…' : 'Retry Failed Syncs'}
          </button>
        </div>
      </div>

      {toast && (
        <div className='toast-container' role='status' aria-live='polite'>
          <div className={`toast ${toast.type === 'success' ? 'toast-success' : 'toast-error'}`}>{toast.message}</div>
        </div>
      )}

      <div className='filter-panel' aria-label='Accounting sync filters'>
        <label>
          <span>Status</span>
          <select value={status} onChange={event => { setStatus(event.target.value as StatusFilter); setPage(1) }}>
            {STATUS_OPTIONS.map(option => (
              <option key={option} value={option}>
                {option === 'InProgress' ? 'In Progress' : option}
              </option>
            ))}
          </select>
        </label>
        <label>
          <span>Entity type</span>
          <select value={entityType} onChange={event => { setEntityType(event.target.value as EntityFilter); setPage(1) }}>
            {ENTITY_TYPES.map(option => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </label>
        <label>
          <span>Start date</span>
          <input
            type='date'
            value={startDate}
            onChange={event => {
              setStartDate(event.target.value)
              setPage(1)
            }}
            max={endDate || undefined}
          />
        </label>
        <label>
          <span>End date</span>
          <input
            type='date'
            value={endDate}
            onChange={event => {
              setEndDate(event.target.value)
              setPage(1)
            }}
            min={startDate || undefined}
          />
        </label>
        <label>
          <span>Rows per page</span>
          <select value={pageSize} onChange={event => handlePageSizeChange(Number(event.target.value))}>
            {PAGE_SIZES.map(option => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </label>
        <button type='button' className='button-tertiary' onClick={clearFilters} aria-label='Clear all filters'>
          Clear filters
        </button>
      </div>

      {error && (
        <p className='helper-text warning' role='alert'>
          {error}
        </p>
      )}

      <div className='table-container'>
        <table className='table accounting-sync-table'>
          <thead>
            <tr>
              <th>Synced At</th>
              <th>Entity Type</th>
              <th>Entity ID</th>
              <th>Status</th>
              <th>QBO ID</th>
              <th>Error Message</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr>
                <td colSpan={7}>
                  <span className='helper-text info'>Loading sync history…</span>
                </td>
              </tr>
            )}
            {!loading && logs.length === 0 && (
              <tr>
                <td colSpan={7}>
                  <span className='helper-text'>No sync operations found for the selected filters.</span>
                </td>
              </tr>
            )}
            {!loading &&
              logs.map(log => {
                const quickBooksUrl = log.qboId ? getQuickBooksUrl(log.entityType, log.qboId) : null
                const retryConfig = getRetryEndpoint(log)
                const logKey = `${log.id}`
                return (
                  <tr key={`${log.id}-${log.syncedAt}`}>
                    <td>{formatDateTime(log.syncedAt)}</td>
                    <td>{log.entityType}</td>
                    <td>
                      <a href={getEntityLink(log.entityType, log.entityId)} target='_blank' rel='noreferrer' className='entity-link'>
                        #{log.entityId}
                      </a>
                    </td>
                    <td>
                      <span className={`sync-status-badge ${log.status.toLowerCase()}`}>
                        {log.status === 'InProgress' ? 'In Progress' : log.status}
                      </span>
                    </td>
                    <td>{log.qboId ?? '—'}</td>
                    <td>{log.errorMessage ?? '—'}</td>
                    <td>
                      <div className='sync-actions'>
                        {log.status === 'Failed' && retryConfig && (
                          <button
                            type='button'
                            className='button-secondary'
                            onClick={() => {
                              void retrySync(log)
                            }}
                            disabled={Boolean(retrying[logKey])}
                          >
                            {retrying[logKey] ? 'Retrying…' : 'Retry'}
                          </button>
                        )}
                        {quickBooksUrl && log.status === 'Success' && (
                          <a
                            href={quickBooksUrl}
                            target='_blank'
                            rel='noreferrer'
                            className='button-link'
                          >
                            View in QuickBooks
                          </a>
                        )}
                      </div>
                    </td>
                  </tr>
                )
              })}
          </tbody>
        </table>
      </div>

      <div className='pagination-bar'>
        <div className='pagination-summary'>
          Showing {startIndex}-{endIndex} of {total} results
        </div>
        <div className='pagination-controls'>
          <button type='button' onClick={() => handlePageChange(page - 1)} disabled={page === 1} className='button-secondary'>
            Previous
          </button>
          <span className='pagination-page' aria-live='polite'>
            Page {page} of {pageCount}
          </span>
          <button type='button' onClick={() => handlePageChange(page + 1)} disabled={page === pageCount}>
            Next
          </button>
        </div>
      </div>
    </section>
  )
}

export default AccountingSyncHistoryPage
