import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { authorizedFetch } from '../api/authorizedFetch'

export type TtbReportStatus = 'Draft' | 'Submitted' | 'Approved' | 'Rejected'

export enum TtbFormType {
  Form5110_28 = 0,
  Form5110_40 = 1
}

export interface TtbReport {
  id: number
  reportMonth: number
  reportYear: number
  formType: TtbFormType
  status: TtbReportStatus
  generatedAt?: string | null
}

interface FetchParams {
  companyId: number
  year: number
  status?: TtbReportStatus | 'All'
  formType?: TtbFormType | 'All'
}

interface TtbReportsState {
  items: TtbReport[]
  isLoading: boolean
  error: string | null
}

const normalizeFormType = (value: any): TtbFormType => {
  if (value === TtbFormType.Form5110_40 || value === 1 || value === '1') return TtbFormType.Form5110_40

  if (typeof value === 'string') {
    const normalized = value.trim().toLowerCase()
    if (normalized === 'form5110_40' || normalized === '5110_40' || normalized === '40') {
      return TtbFormType.Form5110_40
    }
  }

  return TtbFormType.Form5110_28
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
  formType: normalizeFormType(payload.formType ?? payload.form_type ?? payload.FormType ?? TtbFormType.Form5110_28),
  status: toStatus(payload.status ?? payload.Status),
  generatedAt: payload.generatedAt ?? payload.generated_at ?? payload.GeneratedAt ?? null
})

export const fetchTtbReports = createAsyncThunk(
  'ttbReports/fetch',
  async ({ companyId, year, status, formType }: FetchParams) => {
    const params = new URLSearchParams({
      companyId: companyId.toString(),
      year: year.toString()
    })

    if (status && status !== 'All') {
      params.append('status', status)
    }

    if (formType !== undefined && formType !== 'All') {
      params.append('formType', formType.toString())
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
