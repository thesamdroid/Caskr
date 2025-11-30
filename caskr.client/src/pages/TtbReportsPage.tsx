import { useEffect, useMemo, useState } from 'react'
import PdfViewerModal from '../components/PdfViewerModal'
import TtbReportGenerationModal from '../components/TtbReportGenerationModal'
import TtbAuditTrailTab from '../components/TtbAuditTrailTab'
import { authorizedFetch } from '../api/authorizedFetch'
import { useAppDispatch, useAppSelector } from '../hooks'
import { fetchTtbReports, TtbFormType, TtbReport, TtbReportStatus } from '../features/ttbReportsSlice'

type TtbReportsTab = 'reports' | 'audit-trail'

// TTB Compliance: This page surfaces Form 5110.28 artifacts per docs/TTB_FORM_5110_28_MAPPING.md
// to ensure users can preview and download federally required reports without altering calculations.
const statusBadges: Record<TtbReportStatus, string> = {
  Draft: 'draft',
  Submitted: 'submitted',
  Approved: 'approved',
  Rejected: 'rejected',
  ValidationFailed: 'validation-failed',
  PendingReview: 'pending-review',
  Archived: 'archived'
}

const statusLabels: Record<TtbReportStatus, string> = {
  Draft: 'Draft',
  Submitted: 'Submitted',
  Approved: 'Approved',
  Rejected: 'Rejected',
  ValidationFailed: 'Validation Failed',
  PendingReview: 'Pending Review',
  Archived: 'Archived'
}

const statusOptions: Array<TtbReportStatus | 'All'> = [
  'All',
  'Draft',
  'PendingReview',
  'Approved',
  'Submitted',
  'Archived',
  'Rejected',
  'ValidationFailed'
]

const formTypeOptions: Array<TtbFormType | 'All'> = ['All', TtbFormType.Form5110_28, TtbFormType.Form5110_40]

const formatMonthYear = (month: number, year: number) =>
  new Intl.DateTimeFormat('en-US', { month: 'long', year: 'numeric' }).format(new Date(year, month - 1, 1))

const formatDate = (value?: string | null) => {
  if (!value) return '—'
  return new Intl.DateTimeFormat('en-US', { month: 'long', day: 'numeric', year: 'numeric' }).format(new Date(value))
}

const buildFileName = (formType: TtbFormType, month: number, year: number) =>
  `Form_${formType === TtbFormType.Form5110_40 ? '5110_40' : '5110_28'}_${month.toString().padStart(2, '0')}_${year}.pdf`

const describeFormType = (formType: TtbFormType) =>
  formType === TtbFormType.Form5110_40 ? 'Form 5110.40 (Storage)' : 'Form 5110.28 (Processing)'

function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  anchor.rel = 'noopener'
  document.body.appendChild(anchor)
  anchor.click()
  document.body.removeChild(anchor)
  URL.revokeObjectURL(url)
}

function TtbReportsPage() {
  const dispatch = useAppDispatch()
  const reports = useAppSelector(state => state.ttbReports.items)
  const isLoading = useAppSelector(state => state.ttbReports.isLoading)
  const fetchError = useAppSelector(state => state.ttbReports.error)
  const authUser = useAppSelector(state => state.auth.user)

  const [activeTab, setActiveTab] = useState<TtbReportsTab>('reports')
  const [yearFilter, setYearFilter] = useState<number>(new Date().getFullYear())
  const [statusFilter, setStatusFilter] = useState<TtbReportStatus | 'All'>('All')
  const [formTypeFilter, setFormTypeFilter] = useState<TtbFormType | 'All'>('All')
  const [isGenerateModalOpen, setIsGenerateModalOpen] = useState(false)
  const [pdfPreviewUrl, setPdfPreviewUrl] = useState<string | null>(null)
  const [pdfTitle, setPdfTitle] = useState('')
  const [actionError, setActionError] = useState<string | null>(null)
  const [actionSuccess, setActionSuccess] = useState<string | null>(null)
  const [isGenerating, setIsGenerating] = useState(false)
  const [isProcessingWorkflow, setIsProcessingWorkflow] = useState(false)
  const [submitToTtbReportId, setSubmitToTtbReportId] = useState<number | null>(null)
  const [confirmationNumber, setConfirmationNumber] = useState('')

  const companyId = authUser?.companyId ?? 1

  useEffect(() => {
    dispatch(fetchTtbReports({ companyId, year: yearFilter, status: statusFilter, formType: formTypeFilter }))
  }, [companyId, dispatch, formTypeFilter, statusFilter, yearFilter])

  const yearOptions = useMemo(() => {
    const currentYear = new Date().getFullYear()
    return Array.from({ length: 6 }, (_, index) => currentYear - index)
  }, [])

  const handleGenerateReport = async (month: number, year: number, formType: TtbFormType) => {
    setActionError(null)
    setActionSuccess(null)
    setIsGenerating(true)

    try {
      const response = await authorizedFetch('/api/ttb/reports/generate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ companyId, month, year, formType })
      })

      if (!response.ok) {
        throw new Error('Failed to generate report')
      }

      const blob = await response.blob()
      downloadBlob(blob, buildFileName(formType, month, year))
      setActionSuccess(`${describeFormType(formType)} generated. Download started for the new draft.`)
      setIsGenerateModalOpen(false)
      dispatch(fetchTtbReports({ companyId, year: yearFilter, status: statusFilter, formType: formTypeFilter }))
    } catch (error) {
      console.error('[TtbReportsPage] Error generating TTB report', { month, year, error })
      setActionError('Unable to generate this TTB report. Please review the reporting period and try again.')
    } finally {
      setIsGenerating(false)
    }
  }

  const handleDownload = async (report: TtbReport) => {
    setActionError(null)
    setActionSuccess(null)

    try {
      const response = await authorizedFetch(`/api/ttb/reports/${report.id}/download`)
      if (!response.ok) {
        throw new Error(`Download failed with status ${response.status}`)
      }

      const blob = await response.blob()
      downloadBlob(blob, buildFileName(report.formType, report.reportMonth, report.reportYear))
      setActionSuccess(`${describeFormType(report.formType)} download started for the selected period.`)
    } catch (error) {
      console.error('[TtbReportsPage] Error downloading TTB report', { reportId: report.id, error })
      setActionError('Unable to download the selected TTB report. Please try again.')
    }
  }

  const handleView = async (report: TtbReport) => {
    setActionError(null)
    setActionSuccess(null)

    try {
      // TTB 5110.28 PDF preview is pulled from the generated artifact to ensure compliance traceability.
      const response = await authorizedFetch(`/api/ttb/reports/${report.id}/download`)
      if (!response.ok) {
        throw new Error('View failed')
      }

      const blob = await response.blob()
      const url = URL.createObjectURL(blob)
      setPdfPreviewUrl(url)
      setPdfTitle(`${describeFormType(report.formType)} – ${formatMonthYear(report.reportMonth, report.reportYear)}`)
    } catch (error) {
      console.error('[TtbReportsPage] Error opening TTB report preview', { reportId: report.id, error })
      setActionError('Unable to open the TTB report preview. Please download the PDF instead.')
    }
  }

  const handleClosePdf = () => {
    if (pdfPreviewUrl) {
      URL.revokeObjectURL(pdfPreviewUrl)
    }
    setPdfPreviewUrl(null)
  }

  const refreshReports = () => {
    dispatch(fetchTtbReports({ companyId, year: yearFilter, status: statusFilter, formType: formTypeFilter }))
  }

  const handleSubmitForReview = async (report: TtbReport) => {
    setActionError(null)
    setActionSuccess(null)
    setIsProcessingWorkflow(true)

    try {
      const response = await authorizedFetch(`/api/ttb/reports/${report.id}/submit-for-review`, {
        method: 'POST'
      })

      if (!response.ok) {
        const error = await response.json()
        throw new Error(error.detail || 'Failed to submit for review')
      }

      setActionSuccess(`Report for ${formatMonthYear(report.reportMonth, report.reportYear)} submitted for review.`)
      refreshReports()
    } catch (error) {
      console.error('[TtbReportsPage] Error submitting for review', { reportId: report.id, error })
      setActionError(error instanceof Error ? error.message : 'Failed to submit for review.')
    } finally {
      setIsProcessingWorkflow(false)
    }
  }

  const handleApprove = async (report: TtbReport) => {
    setActionError(null)
    setActionSuccess(null)
    setIsProcessingWorkflow(true)

    try {
      const response = await authorizedFetch(`/api/ttb/reports/${report.id}/approve`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({})
      })

      if (!response.ok) {
        const error = await response.json()
        throw new Error(error.detail || 'Failed to approve report')
      }

      setActionSuccess(`Report for ${formatMonthYear(report.reportMonth, report.reportYear)} approved.`)
      refreshReports()
    } catch (error) {
      console.error('[TtbReportsPage] Error approving report', { reportId: report.id, error })
      setActionError(error instanceof Error ? error.message : 'Failed to approve report.')
    } finally {
      setIsProcessingWorkflow(false)
    }
  }

  const handleReject = async (report: TtbReport) => {
    setActionError(null)
    setActionSuccess(null)
    setIsProcessingWorkflow(true)

    try {
      const response = await authorizedFetch(`/api/ttb/reports/${report.id}/reject`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({})
      })

      if (!response.ok) {
        const error = await response.json()
        throw new Error(error.detail || 'Failed to reject report')
      }

      setActionSuccess(`Report for ${formatMonthYear(report.reportMonth, report.reportYear)} rejected and returned to draft.`)
      refreshReports()
    } catch (error) {
      console.error('[TtbReportsPage] Error rejecting report', { reportId: report.id, error })
      setActionError(error instanceof Error ? error.message : 'Failed to reject report.')
    } finally {
      setIsProcessingWorkflow(false)
    }
  }

  const handleSubmitToTtb = async () => {
    if (!submitToTtbReportId || !confirmationNumber.trim()) {
      setActionError('TTB confirmation number is required.')
      return
    }

    setActionError(null)
    setActionSuccess(null)
    setIsProcessingWorkflow(true)

    try {
      const response = await authorizedFetch(`/api/ttb/reports/${submitToTtbReportId}/submit-to-ttb`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ confirmationNumber: confirmationNumber.trim() })
      })

      if (!response.ok) {
        const error = await response.json()
        throw new Error(error.detail || 'Failed to submit to TTB')
      }

      setActionSuccess(`Report submitted to TTB with confirmation number: ${confirmationNumber.trim()}`)
      setSubmitToTtbReportId(null)
      setConfirmationNumber('')
      refreshReports()
    } catch (error) {
      console.error('[TtbReportsPage] Error submitting to TTB', { reportId: submitToTtbReportId, error })
      setActionError(error instanceof Error ? error.message : 'Failed to submit to TTB.')
    } finally {
      setIsProcessingWorkflow(false)
    }
  }

  const handleArchive = async (report: TtbReport) => {
    setActionError(null)
    setActionSuccess(null)
    setIsProcessingWorkflow(true)

    try {
      const response = await authorizedFetch(`/api/ttb/reports/${report.id}/archive`, {
        method: 'POST'
      })

      if (!response.ok) {
        const error = await response.json()
        throw new Error(error.detail || 'Failed to archive report')
      }

      setActionSuccess(`Report for ${formatMonthYear(report.reportMonth, report.reportYear)} archived.`)
      refreshReports()
    } catch (error) {
      console.error('[TtbReportsPage] Error archiving report', { reportId: report.id, error })
      setActionError(error instanceof Error ? error.message : 'Failed to archive report.')
    } finally {
      setIsProcessingWorkflow(false)
    }
  }

  const getWorkflowActions = (report: TtbReport) => {
    const actions: JSX.Element[] = []

    switch (report.status) {
      case 'Draft':
      case 'ValidationFailed':
        actions.push(
          <button
            key='submit-review'
            type='button'
            className='button-primary'
            onClick={() => handleSubmitForReview(report)}
            disabled={isProcessingWorkflow}
          >
            Submit for Review
          </button>
        )
        break
      case 'PendingReview':
        actions.push(
          <button
            key='approve'
            type='button'
            className='button-primary'
            onClick={() => handleApprove(report)}
            disabled={isProcessingWorkflow}
          >
            Approve
          </button>,
          <button
            key='reject'
            type='button'
            className='button-ghost'
            onClick={() => handleReject(report)}
            disabled={isProcessingWorkflow}
          >
            Reject
          </button>
        )
        break
      case 'Approved':
        actions.push(
          <button
            key='submit-ttb'
            type='button'
            className='button-primary'
            onClick={() => setSubmitToTtbReportId(report.id)}
            disabled={isProcessingWorkflow}
          >
            Submit to TTB
          </button>
        )
        break
      case 'Submitted':
        actions.push(
          <button
            key='archive'
            type='button'
            className='button-ghost'
            onClick={() => handleArchive(report)}
            disabled={isProcessingWorkflow}
          >
            Archive
          </button>
        )
        break
      case 'Archived':
        // No workflow actions for archived reports
        break
    }

    return actions
  }

  return (
    <section className='content-section' aria-labelledby='ttb-reports-title'>
      <div className='section-header'>
        <div>
          <h1 id='ttb-reports-title' className='section-title'>TTB Compliance</h1>
          <p className='section-subtitle'>Monitor generated reports, transactions, and audit trail for TTB compliance.</p>
        </div>
        {activeTab === 'reports' && (
          <div className='section-actions'>
            <button
              type='button'
              className='button-primary'
              onClick={() => setIsGenerateModalOpen(true)}
              aria-label='Generate a new TTB monthly report'
              data-testid='generate-ttb-report-button'
            >
              Generate New Report
            </button>
          </div>
        )}
      </div>

      <div className='tab-navigation' role='tablist' aria-label='TTB compliance tabs'>
        <button
          type='button'
          role='tab'
          aria-selected={activeTab === 'reports'}
          className={`tab-button ${activeTab === 'reports' ? 'active' : ''}`}
          onClick={() => setActiveTab('reports')}
        >
          Monthly Reports
        </button>
        <button
          type='button'
          role='tab'
          aria-selected={activeTab === 'audit-trail'}
          className={`tab-button ${activeTab === 'audit-trail' ? 'active' : ''}`}
          onClick={() => setActiveTab('audit-trail')}
        >
          Audit Trail
        </button>
      </div>

      {activeTab === 'audit-trail' ? (
        <TtbAuditTrailTab companyId={companyId} />
      ) : (
        <>
          <div className='filter-panel' aria-label='TTB report filters'>
        <label>
          <span>Year</span>
          <select value={yearFilter} onChange={event => setYearFilter(Number(event.target.value))}>
            {yearOptions.map(year => (
              <option key={year} value={year}>
                {year}
              </option>
            ))}
          </select>
        </label>

        <label>
          <span>Status</span>
          <select value={statusFilter} onChange={event => setStatusFilter(event.target.value as TtbReportStatus | 'All')}>
            {statusOptions.map(option => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </label>

        <label>
          <span>Form type</span>
          <select
            value={formTypeFilter}
            onChange={event => {
              const value = event.target.value
              setFormTypeFilter(value === 'All' ? 'All' : (Number(value) as TtbFormType))
            }}
          >
            {formTypeOptions.map(option => (
              <option key={option} value={option}>
                {option === 'All' ? 'All Forms' : describeFormType(option as TtbFormType)}
              </option>
            ))}
          </select>
        </label>
      </div>

      {(fetchError || actionError || actionSuccess) && (
        <div className='alert-stack' role='status' aria-live='polite'>
          {fetchError && <div className='alert alert-error'>{fetchError}</div>}
          {actionError && <div className='alert alert-error'>{actionError}</div>}
          {actionSuccess && <div className='alert alert-success'>{actionSuccess}</div>}
        </div>
      )}

      {isLoading ? (
        <div className='empty-state'>
          <div className='empty-state-icon'>📑</div>
          <h3 className='empty-state-title'>Loading TTB reports</h3>
          <p className='empty-state-text'>Fetching storage and processing activity for the selected filters…</p>
        </div>
      ) : reports.length === 0 ? (
        <div className='empty-state'>
          <div className='empty-state-icon'>🧾</div>
          <h3 className='empty-state-title'>No TTB reports yet</h3>
          <p className='empty-state-text'>Generate your first TTB report to begin monthly compliance tracking.</p>
        </div>
      ) : (
        <div className='table-container'>
          <table className='table' role='table' aria-label='Generated TTB reports'>
            <thead>
              <tr>
                <th scope='col'>Month/Year</th>
                <th scope='col'>Form</th>
                <th scope='col'>Generated Date</th>
                <th scope='col'>Status</th>
                <th scope='col'>Actions</th>
              </tr>
            </thead>
              <tbody>
                {reports.map(report => (
                  <tr key={report.id}>
                    <td>{formatMonthYear(report.reportMonth, report.reportYear)}</td>
                    <td>{describeFormType(report.formType)}</td>
                    <td>{formatDate(report.generatedAt)}</td>
                    <td>
                      <span className={`status-badge ${statusBadges[report.status]}`}>
                        {statusLabels[report.status]}
                      </span>
                  </td>
                  <td>
                    <div className='table-actions'>
                      <button
                        type='button'
                        className='button-tertiary'
                        onClick={() => handleDownload(report)}
                        aria-label={`Download PDF for ${formatMonthYear(report.reportMonth, report.reportYear)}`}
                      >
                        Download
                      </button>
                      <button
                        type='button'
                        className='button-secondary'
                        onClick={() => handleView(report)}
                        aria-label={`View report for ${formatMonthYear(report.reportMonth, report.reportYear)}`}
                      >
                        View
                      </button>
                      {getWorkflowActions(report)}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
        </>
      )}

      <TtbReportGenerationModal
        isOpen={isGenerateModalOpen}
        onClose={() => setIsGenerateModalOpen(false)}
        onSubmit={handleGenerateReport}
        defaultYear={yearFilter}
        defaultFormType={formTypeFilter === 'All' ? TtbFormType.Form5110_28 : formTypeFilter}
        isSubmitting={isGenerating}
        errorMessage={actionError}
      />

      <PdfViewerModal isOpen={Boolean(pdfPreviewUrl)} title={pdfTitle} pdfUrl={pdfPreviewUrl} onClose={handleClosePdf} />

      {/* Submit to TTB Modal */}
      {submitToTtbReportId && (
        <div className='modal-overlay' onClick={() => setSubmitToTtbReportId(null)}>
          <div className='modal-content' onClick={e => e.stopPropagation()}>
            <h2 className='modal-title'>Submit to TTB</h2>
            <p className='modal-description'>
              Enter the confirmation number received from the TTB after submitting this report through the TTB online system.
            </p>
            <div className='form-group'>
              <label htmlFor='confirmation-number'>TTB Confirmation Number</label>
              <input
                id='confirmation-number'
                type='text'
                value={confirmationNumber}
                onChange={e => setConfirmationNumber(e.target.value)}
                placeholder='e.g., TTB-2024-12345'
                className='form-control'
              />
            </div>
            <div className='modal-actions'>
              <button
                type='button'
                className='button-ghost'
                onClick={() => {
                  setSubmitToTtbReportId(null)
                  setConfirmationNumber('')
                }}
              >
                Cancel
              </button>
              <button
                type='button'
                className='button-primary'
                onClick={handleSubmitToTtb}
                disabled={isProcessingWorkflow || !confirmationNumber.trim()}
              >
                {isProcessingWorkflow ? 'Submitting...' : 'Confirm Submission'}
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}

export default TtbReportsPage
