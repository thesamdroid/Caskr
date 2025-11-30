import { createAsyncThunk, createSlice, PayloadAction } from '@reduxjs/toolkit'
import { authorizedFetch } from '../api/authorizedFetch'

// Types for report templates
export interface ReportTemplate {
  id: number
  name: string
  description: string | null
  category: string
  isSystemTemplate: boolean
  filterDefinition: string | null
  createdAt: string
}

// Types for report results
export interface ReportColumn {
  name: string
  displayName: string
  dataType: string
  sourceColumn: string
}

export interface ReportResult {
  columns: ReportColumn[]
  rows: Record<string, unknown>[]
  totalRows: number
  page: number
  pageSize: number
  totalPages: number
  executionTimeMs: number
  fromCache: boolean
  templateId: number
  templateName: string
  generatedAt: string
}

// Types for saved reports
export interface SavedReport {
  id: number
  reportTemplateId: number
  reportTemplateName: string
  name: string
  description: string | null
  filterValues: string | null
  isFavorite: boolean
  lastRunAt: string | null
  runCount: number
  createdAt: string
}

// Types for filter configuration
export interface FilterDefinition {
  filter: string
  defaultParameters: Record<string, unknown>
}

// Request types
export interface ExecuteReportRequest {
  reportTemplateId: number
  filters?: Record<string, unknown>
  page?: number
  pageSize?: number
  sortOverride?: { column: string; direction: 'asc' | 'desc' }[]
}

export interface SaveReportRequest {
  reportTemplateId: number
  name: string
  description?: string
  filterValues?: Record<string, unknown>
  isFavorite?: boolean
}

// Async thunks

// Fetch all report templates for a company
export const fetchReportTemplates = createAsyncThunk(
  'reports/fetchTemplates',
  async (companyId: number) => {
    const response = await authorizedFetch(`api/reports/templates/company/${companyId}`)
    if (!response.ok) throw new Error('Failed to fetch report templates')
    return (await response.json()) as ReportTemplate[]
  }
)

// Execute a report
export const executeReport = createAsyncThunk(
  'reports/execute',
  async ({ companyId, request }: { companyId: number; request: ExecuteReportRequest }) => {
    const response = await authorizedFetch(`api/reports/execute/company/${companyId}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request)
    })
    if (!response.ok) {
      const error = await response.json()
      throw new Error(error.error || 'Failed to execute report')
    }
    return (await response.json()) as ReportResult
  }
)

// Fetch saved reports for a user
export const fetchSavedReports = createAsyncThunk(
  'reports/fetchSaved',
  async (companyId: number) => {
    const response = await authorizedFetch(`api/reports/saved/company/${companyId}`)
    if (!response.ok) throw new Error('Failed to fetch saved reports')
    return (await response.json()) as SavedReport[]
  }
)

// Create a saved report
export const createSavedReport = createAsyncThunk(
  'reports/createSaved',
  async ({ companyId, request }: { companyId: number; request: SaveReportRequest }) => {
    const response = await authorizedFetch(`api/reports/saved/company/${companyId}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request)
    })
    if (!response.ok) {
      const error = await response.json()
      throw new Error(error.error || 'Failed to save report')
    }
    return (await response.json()) as SavedReport
  }
)

// Update a saved report
export const updateSavedReport = createAsyncThunk(
  'reports/updateSaved',
  async ({
    companyId,
    savedReportId,
    updates
  }: {
    companyId: number
    savedReportId: number
    updates: Partial<SaveReportRequest>
  }) => {
    const response = await authorizedFetch(
      `api/reports/saved/${savedReportId}/company/${companyId}`,
      {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(updates)
      }
    )
    if (!response.ok) {
      const error = await response.json()
      throw new Error(error.error || 'Failed to update saved report')
    }
    return (await response.json()) as SavedReport
  }
)

// Delete a saved report
export const deleteSavedReport = createAsyncThunk(
  'reports/deleteSaved',
  async ({ companyId, savedReportId }: { companyId: number; savedReportId: number }) => {
    const response = await authorizedFetch(
      `api/reports/saved/${savedReportId}/company/${companyId}`,
      { method: 'DELETE' }
    )
    if (!response.ok) throw new Error('Failed to delete saved report')
    return savedReportId
  }
)

// Run a saved report
export const runSavedReport = createAsyncThunk(
  'reports/runSaved',
  async ({ companyId, savedReportId }: { companyId: number; savedReportId: number }) => {
    const response = await authorizedFetch(
      `api/reports/saved/${savedReportId}/run/company/${companyId}`,
      { method: 'POST' }
    )
    if (!response.ok) {
      const error = await response.json()
      throw new Error(error.error || 'Failed to run saved report')
    }
    return (await response.json()) as ReportResult
  }
)

// Export to CSV
export const exportToCsv = createAsyncThunk(
  'reports/exportCsv',
  async ({ companyId, request }: { companyId: number; request: ExecuteReportRequest }) => {
    const response = await authorizedFetch(`api/reports/export/csv/company/${companyId}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request)
    })
    if (!response.ok) {
      const error = await response.json()
      throw new Error(error.error || 'Failed to export report')
    }

    // Get filename from Content-Disposition header or generate one
    const contentDisposition = response.headers.get('Content-Disposition')
    let filename = 'report.csv'
    if (contentDisposition) {
      const match = contentDisposition.match(/filename="?(.+)"?/)
      if (match) filename = match[1]
    }

    // Download the file
    const blob = await response.blob()
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = filename
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    window.URL.revokeObjectURL(url)

    return filename
  }
)

// State interface
interface ReportsState {
  templates: ReportTemplate[]
  savedReports: SavedReport[]
  currentResult: ReportResult | null
  selectedTemplateId: number | null
  currentFilters: Record<string, unknown>
  loading: boolean
  executing: boolean
  exporting: boolean
  error: string | null
}

const initialState: ReportsState = {
  templates: [],
  savedReports: [],
  currentResult: null,
  selectedTemplateId: null,
  currentFilters: {},
  loading: false,
  executing: false,
  exporting: false,
  error: null
}

const reportsSlice = createSlice({
  name: 'reports',
  initialState,
  reducers: {
    selectTemplate: (state, action: PayloadAction<number | null>) => {
      state.selectedTemplateId = action.payload
      state.currentResult = null
      state.currentFilters = {}
      state.error = null
    },
    setFilters: (state, action: PayloadAction<Record<string, unknown>>) => {
      state.currentFilters = action.payload
    },
    updateFilter: (state, action: PayloadAction<{ key: string; value: unknown }>) => {
      state.currentFilters[action.payload.key] = action.payload.value
    },
    clearFilters: state => {
      state.currentFilters = {}
    },
    clearResult: state => {
      state.currentResult = null
    },
    clearError: state => {
      state.error = null
    }
  },
  extraReducers: builder => {
    // Fetch templates
    builder.addCase(fetchReportTemplates.pending, state => {
      state.loading = true
      state.error = null
    })
    builder.addCase(fetchReportTemplates.fulfilled, (state, action) => {
      state.templates = action.payload
      state.loading = false
    })
    builder.addCase(fetchReportTemplates.rejected, (state, action) => {
      state.loading = false
      state.error = action.error.message || 'Failed to fetch templates'
    })

    // Execute report
    builder.addCase(executeReport.pending, state => {
      state.executing = true
      state.error = null
    })
    builder.addCase(executeReport.fulfilled, (state, action) => {
      state.currentResult = action.payload
      state.executing = false
    })
    builder.addCase(executeReport.rejected, (state, action) => {
      state.executing = false
      state.error = action.error.message || 'Failed to execute report'
    })

    // Fetch saved reports
    builder.addCase(fetchSavedReports.pending, state => {
      state.loading = true
      state.error = null
    })
    builder.addCase(fetchSavedReports.fulfilled, (state, action) => {
      state.savedReports = action.payload
      state.loading = false
    })
    builder.addCase(fetchSavedReports.rejected, (state, action) => {
      state.loading = false
      state.error = action.error.message || 'Failed to fetch saved reports'
    })

    // Create saved report
    builder.addCase(createSavedReport.fulfilled, (state, action) => {
      state.savedReports.push(action.payload)
    })

    // Update saved report
    builder.addCase(updateSavedReport.fulfilled, (state, action) => {
      const index = state.savedReports.findIndex(r => r.id === action.payload.id)
      if (index !== -1) {
        state.savedReports[index] = action.payload
      }
    })

    // Delete saved report
    builder.addCase(deleteSavedReport.fulfilled, (state, action) => {
      state.savedReports = state.savedReports.filter(r => r.id !== action.payload)
    })

    // Run saved report
    builder.addCase(runSavedReport.pending, state => {
      state.executing = true
      state.error = null
    })
    builder.addCase(runSavedReport.fulfilled, (state, action) => {
      state.currentResult = action.payload
      state.executing = false
    })
    builder.addCase(runSavedReport.rejected, (state, action) => {
      state.executing = false
      state.error = action.error.message || 'Failed to run saved report'
    })

    // Export CSV
    builder.addCase(exportToCsv.pending, state => {
      state.exporting = true
      state.error = null
    })
    builder.addCase(exportToCsv.fulfilled, state => {
      state.exporting = false
    })
    builder.addCase(exportToCsv.rejected, (state, action) => {
      state.exporting = false
      state.error = action.error.message || 'Failed to export report'
    })
  }
})

export const {
  selectTemplate,
  setFilters,
  updateFilter,
  clearFilters,
  clearResult,
  clearError
} = reportsSlice.actions

export default reportsSlice.reducer
