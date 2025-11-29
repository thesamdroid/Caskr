import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { authorizedFetch } from '../api/authorizedFetch'

export type TtbReportStatus = 'Draft' | 'Submitted' | 'Approved' | 'Rejected'

export interface TtbReport {
  id: number
  reportMonth: number
  reportYear: number
  status: TtbReportStatus
  generatedAt?: string | null
}

interface FetchParams {
  companyId: number
  year: number
  status?: TtbReportStatus | 'All'
}

interface TtbReportsState {
  items: TtbReport[]
  isLoading: boolean
  error: string | null
}

const statusLookup: Record<number, TtbReportStatus> = {
  0: 'Draft',
  1: 'Submitted',
  2: 'Approved',
  3: 'Rejected'
}

const toStatus = (value: TtbReportStatus | number | string): TtbReportStatus => {
  if (typeof value === 'number') {
    return statusLookup[value] ?? 'Draft'
  }

  const normalized = value.toString().trim()
  if (normalized.length === 0) return 'Draft'

  const upper = normalized.toLowerCase()
  if (upper === 'submitted') return 'Submitted'
  if (upper === 'approved') return 'Approved'
  if (upper === 'rejected') return 'Rejected'
  return 'Draft'
}

const normalizeReport = (payload: any): TtbReport => ({
  id: payload.id,
  reportMonth: payload.reportMonth ?? payload.month ?? payload.report_month ?? payload.ReportMonth,
  reportYear: payload.reportYear ?? payload.year ?? payload.report_year ?? payload.ReportYear,
  status: toStatus(payload.status ?? payload.Status),
  generatedAt: payload.generatedAt ?? payload.generated_at ?? payload.GeneratedAt ?? null
})

export const fetchTtbReports = createAsyncThunk(
  'ttbReports/fetch',
  async ({ companyId, year, status }: FetchParams) => {
    const params = new URLSearchParams({
      companyId: companyId.toString(),
      year: year.toString()
    })

    if (status && status !== 'All') {
      params.append('status', status)
    }

    const response = await authorizedFetch(`/api/ttb/reports?${params.toString()}`)
    if (!response.ok) {
      console.error('[ttbReportsSlice] Failed to fetch reports', { status: response.status })
      throw new Error('Unable to load TTB reports')
    }

    const content = await response.json()
    if (!Array.isArray(content)) {
      console.warn('[ttbReportsSlice] Unexpected response shape while fetching reports', content)
      return [] as TtbReport[]
    }

    return content.map(normalizeReport)
  }
)

const initialState: TtbReportsState = {
  items: [],
  isLoading: false,
  error: null
}

const ttbReportsSlice = createSlice({
  name: 'ttbReports',
  initialState,
  reducers: {},
  extraReducers: builder => {
    builder
      .addCase(fetchTtbReports.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(fetchTtbReports.fulfilled, (state, action) => {
        state.items = action.payload
        state.isLoading = false
      })
      .addCase(fetchTtbReports.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.error.message ?? 'Failed to load TTB reports'
      })
  }
})

export default ttbReportsSlice.reducer
