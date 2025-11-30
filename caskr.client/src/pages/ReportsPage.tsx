import { useEffect, useMemo, useState, useCallback } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  fetchReportTemplates,
  fetchSavedReports,
  executeReport,
  createSavedReport,
  deleteSavedReport,
  runSavedReport,
  exportToCsv,
  selectTemplate,
  setFilters,
  clearError,
  ReportTemplate,
  SavedReport,
  ReportColumn
} from '../features/reportsSlice'
import { authorizedFetch } from '../api/authorizedFetch'

type ReportsTab = 'templates' | 'saved'

interface ReportCategory {
  name: string
  icon: string
  reports: ReportTemplate[]
}

// Format date for display
const formatDate = (value: unknown): string => {
  if (!value) return '—'
  const date = new Date(value as string)
  if (isNaN(date.getTime())) return String(value)
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  }).format(date)
}

// Format datetime for display
const formatDateTime = (value: unknown): string => {
  if (!value) return '—'
  const date = new Date(value as string)
  if (isNaN(date.getTime())) return String(value)
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit'
  }).format(date)
}

// Format numbers with commas
const formatNumber = (value: unknown): string => {
  if (value === null || value === undefined) return '—'
  const num = Number(value)
  if (isNaN(num)) return String(value)
  return new Intl.NumberFormat('en-US').format(num)
}

// Format decimal values
const formatDecimal = (value: unknown): string => {
  if (value === null || value === undefined) return '—'
  const num = Number(value)
  if (isNaN(num)) return String(value)
  return new Intl.NumberFormat('en-US', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(num)
}

// Format cell value based on data type
const formatCellValue = (value: unknown, dataType: string): string => {
  if (value === null || value === undefined) return '—'

  switch (dataType) {
    case 'datetime':
      return formatDateTime(value)
    case 'date':
      return formatDate(value)
    case 'decimal':
      return formatDecimal(value)
    case 'number':
      return formatNumber(value)
    case 'boolean':
      return value ? 'Yes' : 'No'
    default:
      return String(value)
  }
}

// Download blob helper
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

function ReportsPage() {
  const dispatch = useAppDispatch()
  const authUser = useAppSelector(state => state.auth.user)
  const {
    templates,
    savedReports,
    currentResult,
    selectedTemplateId,
    currentFilters,
    loading,
    executing,
    exporting,
    error
  } = useAppSelector(state => state.reports)

  const [activeTab, setActiveTab] = useState<ReportsTab>('templates')
  const [searchQuery, setSearchQuery] = useState('')
  const [expandedCategories, setExpandedCategories] = useState<Set<string>>(
    new Set(['Financial', 'Inventory', 'Production', 'Compliance'])
  )
  const [showSaveModal, setShowSaveModal] = useState(false)
  const [saveName, setSaveName] = useState('')
  const [saveDescription, setSaveDescription] = useState('')
  const [saveAsFavorite, setSaveAsFavorite] = useState(false)
  const [sortColumn, setSortColumn] = useState<string | null>(null)
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc')
  const [currentPage, setCurrentPage] = useState(1)
  const [isExporting, setIsExporting] = useState(false)

  const companyId = authUser?.companyId ?? 1

  // Fetch data on mount
  useEffect(() => {
    dispatch(fetchReportTemplates(companyId))
    dispatch(fetchSavedReports(companyId))
  }, [dispatch, companyId])

  // Clear error on template change
  useEffect(() => {
    dispatch(clearError())
  }, [selectedTemplateId, dispatch])

  // Group templates by category
  const categorizedTemplates = useMemo((): ReportCategory[] => {
    const categories: Record<string, ReportTemplate[]> = {
      Financial: [],
      Inventory: [],
      Production: [],
      Compliance: [],
      General: []
    }

    const filteredTemplates = templates.filter(
      t =>
        !searchQuery ||
        t.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        t.description?.toLowerCase().includes(searchQuery.toLowerCase())
    )

    filteredTemplates.forEach(template => {
      const category = template.category || 'General'
      if (!categories[category]) categories[category] = []
      categories[category].push(template)
    })

    return [
      { name: 'Financial', icon: '$', reports: categories.Financial },
      { name: 'Inventory', icon: 'B', reports: categories.Inventory },
      { name: 'Production', icon: 'P', reports: categories.Production },
      { name: 'Compliance', icon: 'C', reports: categories.Compliance },
      { name: 'General', icon: 'G', reports: categories.General }
    ].filter(cat => cat.reports.length > 0)
  }, [templates, searchQuery])

  // Get selected template
  const selectedTemplate = useMemo(
    () => templates.find(t => t.id === selectedTemplateId),
    [templates, selectedTemplateId]
  )

  // Parse filter definition from template
  const filterDefinition = useMemo(() => {
    if (!selectedTemplate?.filterDefinition) return null
    try {
      return JSON.parse(selectedTemplate.filterDefinition) as {
        filter: string
        defaultParameters: Record<string, unknown>
      }
    } catch {
      return null
    }
  }, [selectedTemplate])

  // Extract filter parameters from filter definition
  const filterParams = useMemo(() => {
    if (!filterDefinition) return []
    const paramMatches = filterDefinition.filter.match(/@(\w+)/g) || []
    return paramMatches.map(p => p.substring(1))
  }, [filterDefinition])

  // Toggle category expansion
  const toggleCategory = (category: string) => {
    const newExpanded = new Set(expandedCategories)
    if (newExpanded.has(category)) {
      newExpanded.delete(category)
    } else {
      newExpanded.add(category)
    }
    setExpandedCategories(newExpanded)
  }

  // Handle template selection
  const handleSelectTemplate = (templateId: number) => {
    dispatch(selectTemplate(templateId))
    setCurrentPage(1)
    setSortColumn(null)
    setSortDirection('asc')

    // Load default filter values
    const template = templates.find(t => t.id === templateId)
    if (template?.filterDefinition) {
      try {
        const filterDef = JSON.parse(template.filterDefinition)
        if (filterDef.defaultParameters) {
          dispatch(setFilters(filterDef.defaultParameters))
        }
      } catch {
        dispatch(setFilters({}))
      }
    } else {
      dispatch(setFilters({}))
    }
  }

  // Handle filter change
  const handleFilterChange = (key: string, value: unknown) => {
    dispatch(setFilters({ ...currentFilters, [key]: value }))
  }

  // Run report
  const handleRunReport = useCallback(() => {
    if (!selectedTemplateId) return

    dispatch(
      executeReport({
        companyId,
        request: {
          reportTemplateId: selectedTemplateId,
          filters: currentFilters,
          page: currentPage,
          pageSize: 50,
          sortOverride: sortColumn
            ? [{ column: sortColumn, direction: sortDirection }]
            : undefined
        }
      })
    )
  }, [dispatch, companyId, selectedTemplateId, currentFilters, currentPage, sortColumn, sortDirection])

  // Handle sort
  const handleSort = (column: ReportColumn) => {
    if (sortColumn === column.name) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc')
    } else {
      setSortColumn(column.name)
      setSortDirection('asc')
    }
  }

  // Re-run report when sort changes
  useEffect(() => {
    if (currentResult && sortColumn) {
      handleRunReport()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sortColumn, sortDirection])

  // Handle pagination
  const handlePageChange = (page: number) => {
    setCurrentPage(page)
  }

  // Re-run report when page changes
  useEffect(() => {
    if (currentResult && selectedTemplateId) {
      handleRunReport()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentPage])

  // Export to CSV
  const handleExportCsv = async () => {
    if (!selectedTemplateId) return

    dispatch(
      exportToCsv({
        companyId,
        request: {
          reportTemplateId: selectedTemplateId,
          filters: currentFilters
        }
      })
    )
  }

  // Export to Excel (using JSON endpoint + client-side generation)
  const handleExportExcel = async () => {
    if (!selectedTemplateId || !currentResult) return
    setIsExporting(true)

    try {
      const response = await authorizedFetch(`api/reports/export/json/company/${companyId}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          reportTemplateId: selectedTemplateId,
          filters: currentFilters
        })
      })

      if (!response.ok) throw new Error('Export failed')

      const data = await response.json()

      // Generate simple Excel-compatible CSV with BOM for Excel
      const BOM = '\uFEFF'
      let csv = BOM
      csv += data.columns.map((c: ReportColumn) => `"${c.displayName}"`).join(',') + '\n'
      data.rows.forEach((row: Record<string, unknown>) => {
        csv +=
          data.columns
            .map((c: ReportColumn) => {
              const value = row[c.name]
              const formatted = formatCellValue(value, c.dataType)
              return `"${formatted.replace(/"/g, '""')}"`
            })
            .join(',') + '\n'
      })

      const blob = new Blob([csv], { type: 'application/vnd.ms-excel;charset=utf-8' })
      const filename = `${data.templateName.replace(/\s+/g, '_')}_${new Date().toISOString().split('T')[0]}.xls`
      downloadBlob(blob, filename)
    } catch (err) {
      console.error('[ReportsPage] Excel export failed:', err)
    } finally {
      setIsExporting(false)
    }
  }

  // Export to PDF (simple print-based approach)
  const handleExportPdf = () => {
    window.print()
  }

  // Save report
  const handleSaveReport = async () => {
    if (!selectedTemplateId || !saveName.trim()) return

    await dispatch(
      createSavedReport({
        companyId,
        request: {
          reportTemplateId: selectedTemplateId,
          name: saveName.trim(),
          description: saveDescription.trim() || undefined,
          filterValues: currentFilters,
          isFavorite: saveAsFavorite
        }
      })
    )

    setShowSaveModal(false)
    setSaveName('')
    setSaveDescription('')
    setSaveAsFavorite(false)
  }

  // Run saved report
  const handleRunSavedReport = (savedReport: SavedReport) => {
    // Select the template first
    dispatch(selectTemplate(savedReport.reportTemplateId))

    // Load saved filter values
    if (savedReport.filterValues) {
      try {
        const filters = JSON.parse(savedReport.filterValues)
        dispatch(setFilters(filters))
      } catch {
        dispatch(setFilters({}))
      }
    }

    // Run the saved report
    dispatch(runSavedReport({ companyId, savedReportId: savedReport.id }))
    setActiveTab('templates')
  }

  // Delete saved report
  const handleDeleteSavedReport = (savedReportId: number) => {
    if (window.confirm('Are you sure you want to delete this saved report?')) {
      dispatch(deleteSavedReport({ companyId, savedReportId }))
    }
  }

  // Render filter input based on parameter name
  const renderFilterInput = (paramName: string) => {
    const value = currentFilters[paramName] ?? ''

    // Date filters
    if (paramName.toLowerCase().includes('date') || paramName.toLowerCase().includes('start') || paramName.toLowerCase().includes('end')) {
      return (
        <input
          type="date"
          id={`filter-${paramName}`}
          value={value as string}
          onChange={e => handleFilterChange(paramName, e.target.value)}
          className="form-input"
        />
      )
    }

    // Status filters
    if (paramName.toLowerCase().includes('status')) {
      return (
        <select
          id={`filter-${paramName}`}
          value={value as string}
          onChange={e => handleFilterChange(paramName, e.target.value)}
          className="form-select"
        >
          <option value="">All</option>
          <option value="Active">Active</option>
          <option value="Aging">Aging</option>
          <option value="Complete">Complete</option>
          <option value="Draft">Draft</option>
          <option value="Pending">Pending</option>
        </select>
      )
    }

    // Numeric filters
    if (paramName.toLowerCase().includes('min') || paramName.toLowerCase().includes('max') || paramName.toLowerCase().includes('count') || paramName.toLowerCase().includes('quantity')) {
      return (
        <input
          type="number"
          id={`filter-${paramName}`}
          value={value as string}
          onChange={e => handleFilterChange(paramName, e.target.value ? Number(e.target.value) : '')}
          className="form-input"
          placeholder="Enter value"
        />
      )
    }

    // Default text input
    return (
      <input
        type="text"
        id={`filter-${paramName}`}
        value={value as string}
        onChange={e => handleFilterChange(paramName, e.target.value)}
        className="form-input"
        placeholder={`Enter ${paramName.replace(/_/g, ' ')}`}
      />
    )
  }

  // Format parameter name for display
  const formatParamName = (paramName: string): string => {
    return paramName
      .replace(/_/g, ' ')
      .replace(/([A-Z])/g, ' $1')
      .replace(/^\s/, '')
      .split(' ')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
      .join(' ')
  }

  return (
    <div className="reports-page">
      {/* Page Header */}
      <section className="content-section" aria-labelledby="reports-title">
        <div className="section-header">
          <div>
            <h1 id="reports-title" className="section-title">Report Builder</h1>
            <p className="section-subtitle">
              Select, configure, and run reports across your distillery data
            </p>
          </div>
        </div>

        {/* Error Alert */}
        {error && (
          <div className="alert alert-error" role="alert">
            {error}
            <button
              className="alert-close"
              onClick={() => dispatch(clearError())}
              aria-label="Dismiss error"
            >
              &times;
            </button>
          </div>
        )}

        {/* Main Layout */}
        <div className="reports-layout">
          {/* Left Sidebar */}
          <aside className="reports-sidebar" aria-label="Report templates">
            {/* Tab Buttons */}
            <div className="sidebar-tabs" role="tablist">
              <button
                role="tab"
                aria-selected={activeTab === 'templates'}
                className={`sidebar-tab ${activeTab === 'templates' ? 'active' : ''}`}
                onClick={() => setActiveTab('templates')}
              >
                Templates
              </button>
              <button
                role="tab"
                aria-selected={activeTab === 'saved'}
                className={`sidebar-tab ${activeTab === 'saved' ? 'active' : ''}`}
                onClick={() => setActiveTab('saved')}
              >
                Saved ({savedReports.length})
              </button>
            </div>

            {activeTab === 'templates' ? (
              <>
                {/* Search Box */}
                <div className="sidebar-search">
                  <input
                    type="search"
                    placeholder="Search reports..."
                    value={searchQuery}
                    onChange={e => setSearchQuery(e.target.value)}
                    className="search-input"
                    aria-label="Search report templates"
                  />
                </div>

                {/* Categories */}
                {loading ? (
                  <div className="sidebar-loading">Loading templates...</div>
                ) : (
                  <nav className="report-categories">
                    {categorizedTemplates.map(category => (
                      <div key={category.name} className="report-category">
                        <button
                          className="category-header"
                          onClick={() => toggleCategory(category.name)}
                          aria-expanded={expandedCategories.has(category.name)}
                        >
                          <span className="category-icon">{category.icon}</span>
                          <span className="category-name">{category.name}</span>
                          <span className="category-count">({category.reports.length})</span>
                          <span className="category-chevron">
                            {expandedCategories.has(category.name) ? '▼' : '▶'}
                          </span>
                        </button>

                        {expandedCategories.has(category.name) && (
                          <ul className="report-list" role="listbox">
                            {category.reports.map(report => (
                              <li key={report.id}>
                                <button
                                  className={`report-item ${selectedTemplateId === report.id ? 'selected' : ''}`}
                                  onClick={() => handleSelectTemplate(report.id)}
                                  aria-selected={selectedTemplateId === report.id}
                                  role="option"
                                >
                                  <span className="report-name">{report.name}</span>
                                  {report.isSystemTemplate && (
                                    <span className="system-badge" title="System template">S</span>
                                  )}
                                </button>
                              </li>
                            ))}
                          </ul>
                        )}
                      </div>
                    ))}
                  </nav>
                )}
              </>
            ) : (
              /* Saved Reports Tab */
              <div className="saved-reports-list">
                {savedReports.length === 0 ? (
                  <div className="sidebar-empty">
                    <p>No saved reports yet</p>
                    <p className="text-secondary">Run a report and save it for quick access</p>
                  </div>
                ) : (
                  <ul className="saved-reports" role="listbox">
                    {savedReports.map(saved => (
                      <li key={saved.id} className="saved-report-item">
                        <div className="saved-report-info">
                          <span className="saved-report-name">
                            {saved.isFavorite && <span className="favorite-star">*</span>}
                            {saved.name}
                          </span>
                          <span className="saved-report-template">{saved.reportTemplateName}</span>
                          {saved.lastRunAt && (
                            <span className="saved-report-date">
                              Last run: {formatDate(saved.lastRunAt)}
                            </span>
                          )}
                        </div>
                        <div className="saved-report-actions">
                          <button
                            className="button-small button-primary"
                            onClick={() => handleRunSavedReport(saved)}
                            title="Run this saved report"
                          >
                            Run
                          </button>
                          <button
                            className="button-small button-ghost"
                            onClick={() => handleDeleteSavedReport(saved.id)}
                            title="Delete this saved report"
                          >
                            X
                          </button>
                        </div>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            )}
          </aside>

          {/* Main Content Area */}
          <main className="reports-main">
            {!selectedTemplate ? (
              <div className="reports-placeholder">
                <div className="placeholder-icon">R</div>
                <h2>Select a Report</h2>
                <p>Choose a report template from the sidebar to get started</p>
              </div>
            ) : (
              <>
                {/* Report Header */}
                <div className="report-header">
                  <div className="report-title-section">
                    <h2 className="report-title">{selectedTemplate.name}</h2>
                    {selectedTemplate.description && (
                      <p className="report-description">{selectedTemplate.description}</p>
                    )}
                  </div>
                </div>

                {/* Filters Panel */}
                {filterParams.length > 0 && (
                  <div className="filters-panel">
                    <h3 className="filters-title">Filters</h3>
                    <div className="filters-grid">
                      {filterParams.map(param => (
                        <div key={param} className="filter-field">
                          <label htmlFor={`filter-${param}`} className="filter-label">
                            {formatParamName(param)}
                          </label>
                          {renderFilterInput(param)}
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {/* Action Buttons */}
                <div className="report-actions">
                  <button
                    className="button-primary"
                    onClick={handleRunReport}
                    disabled={executing}
                  >
                    {executing ? 'Running...' : 'Run Report'}
                  </button>

                  {currentResult && (
                    <>
                      <div className="export-buttons">
                        <button
                          className="button-secondary"
                          onClick={handleExportCsv}
                          disabled={exporting}
                          title="Export to CSV"
                        >
                          CSV
                        </button>
                        <button
                          className="button-secondary"
                          onClick={handleExportExcel}
                          disabled={isExporting}
                          title="Export to Excel"
                        >
                          Excel
                        </button>
                        <button
                          className="button-secondary"
                          onClick={handleExportPdf}
                          title="Print/Export to PDF"
                        >
                          PDF
                        </button>
                      </div>

                      <button
                        className="button-ghost"
                        onClick={() => setShowSaveModal(true)}
                        title="Save this report configuration"
                      >
                        Save as Favorite
                      </button>
                    </>
                  )}
                </div>

                {/* Results Area */}
                {executing ? (
                  <div className="results-loading">
                    <div className="loading-spinner"></div>
                    <p>Executing report...</p>
                  </div>
                ) : currentResult ? (
                  <div className="results-section">
                    {/* Results Metadata */}
                    <div className="results-meta">
                      <span className="results-count">
                        {currentResult.totalRows.toLocaleString()} row{currentResult.totalRows !== 1 ? 's' : ''}
                      </span>
                      <span className="results-time">
                        {currentResult.executionTimeMs}ms
                        {currentResult.fromCache && ' (cached)'}
                      </span>
                    </div>

                    {/* Results Table */}
                    {currentResult.rows.length === 0 ? (
                      <div className="empty-state">
                        <div className="empty-state-icon">O</div>
                        <h3 className="empty-state-title">No results found</h3>
                        <p className="empty-state-text">
                          Try adjusting your filters to see more results
                        </p>
                      </div>
                    ) : (
                      <>
                        <div className="table-container">
                          <table className="table results-table" role="table" aria-label="Report results">
                            <thead>
                              <tr>
                                {currentResult.columns.map(column => (
                                  <th
                                    key={column.name}
                                    scope="col"
                                    className={`sortable-header ${sortColumn === column.name ? 'sorted' : ''}`}
                                    onClick={() => handleSort(column)}
                                    role="columnheader"
                                    aria-sort={
                                      sortColumn === column.name
                                        ? sortDirection === 'asc'
                                          ? 'ascending'
                                          : 'descending'
                                        : 'none'
                                    }
                                  >
                                    {column.displayName}
                                    {sortColumn === column.name && (
                                      <span className="sort-indicator">
                                        {sortDirection === 'asc' ? ' ^' : ' v'}
                                      </span>
                                    )}
                                  </th>
                                ))}
                              </tr>
                            </thead>
                            <tbody>
                              {currentResult.rows.map((row, rowIndex) => (
                                <tr key={rowIndex}>
                                  {currentResult.columns.map(column => (
                                    <td key={column.name}>
                                      {formatCellValue(row[column.name], column.dataType)}
                                    </td>
                                  ))}
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>

                        {/* Pagination */}
                        {currentResult.totalPages > 1 && (
                          <div className="pagination" role="navigation" aria-label="Pagination">
                            <button
                              className="pagination-button"
                              onClick={() => handlePageChange(1)}
                              disabled={currentPage === 1}
                              aria-label="First page"
                            >
                              First
                            </button>
                            <button
                              className="pagination-button"
                              onClick={() => handlePageChange(currentPage - 1)}
                              disabled={currentPage === 1}
                              aria-label="Previous page"
                            >
                              Prev
                            </button>
                            <span className="pagination-info">
                              Page {currentPage} of {currentResult.totalPages}
                            </span>
                            <button
                              className="pagination-button"
                              onClick={() => handlePageChange(currentPage + 1)}
                              disabled={currentPage >= currentResult.totalPages}
                              aria-label="Next page"
                            >
                              Next
                            </button>
                            <button
                              className="pagination-button"
                              onClick={() => handlePageChange(currentResult.totalPages)}
                              disabled={currentPage >= currentResult.totalPages}
                              aria-label="Last page"
                            >
                              Last
                            </button>
                          </div>
                        )}
                      </>
                    )}
                  </div>
                ) : (
                  <div className="results-placeholder">
                    <p className="text-secondary">
                      Configure your filters and click "Run Report" to see results
                    </p>
                  </div>
                )}
              </>
            )}
          </main>
        </div>
      </section>

      {/* Save Report Modal */}
      {showSaveModal && (
        <div className="modal-overlay" onClick={() => setShowSaveModal(false)}>
          <div
            className="modal-content modal-small"
            onClick={e => e.stopPropagation()}
            role="dialog"
            aria-labelledby="save-modal-title"
            aria-modal="true"
          >
            <div className="modal-header">
              <h3 id="save-modal-title" className="modal-title">Save Report</h3>
              <p className="modal-subtitle">Save your current filter configuration for quick access</p>
            </div>

            <div className="modal-body">
              <div className="form-field">
                <label htmlFor="save-name" className="form-label">
                  Report Name <span className="required">*</span>
                </label>
                <input
                  id="save-name"
                  type="text"
                  className="form-input"
                  value={saveName}
                  onChange={e => setSaveName(e.target.value)}
                  placeholder="Enter a name for this report"
                  autoFocus
                />
              </div>

              <div className="form-field">
                <label htmlFor="save-description" className="form-label">
                  Description
                </label>
                <textarea
                  id="save-description"
                  className="form-textarea"
                  value={saveDescription}
                  onChange={e => setSaveDescription(e.target.value)}
                  placeholder="Optional description"
                  rows={3}
                />
              </div>

              <div className="form-field checkbox-field">
                <label className="checkbox-label">
                  <input
                    type="checkbox"
                    checked={saveAsFavorite}
                    onChange={e => setSaveAsFavorite(e.target.checked)}
                  />
                  <span>Mark as favorite</span>
                </label>
              </div>
            </div>

            <div className="modal-actions">
              <button className="button-primary" onClick={handleSaveReport} disabled={!saveName.trim()}>
                Save Report
              </button>
              <button className="button-ghost" onClick={() => setShowSaveModal(false)}>
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default ReportsPage
