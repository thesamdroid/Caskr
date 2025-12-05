import { createAsyncThunk, createSlice, PayloadAction } from '@reduxjs/toolkit'
import { authorizedFetch } from '../api/authorizedFetch'
import type {
  CapacityOverview,
  EquipmentUtilization,
  Bottleneck,
  BottleneckResolution,
  CapacityForecast,
  CapacityPlanSummary,
  CapacityPlanDetail,
  CapacityConstraint,
  ScenarioResult,
  GapAnalysis,
  UtilizationTrend,
  WhatIfScenario,
  CreateCapacityPlanRequest,
  CreateConstraintRequest,
  ForecastMethod,
  PagedResult
} from '../types/capacity'

interface CapacityState {
  overview: CapacityOverview | null
  utilization: EquipmentUtilization[]
  bottlenecks: Bottleneck[]
  resolutions: BottleneckResolution[]
  forecast: CapacityForecast | null
  forecastMethod: ForecastMethod
  plans: CapacityPlanSummary[]
  currentPlan: CapacityPlanDetail | null
  constraints: CapacityConstraint[]
  scenarios: ScenarioResult[]
  gapAnalysis: GapAnalysis | null
  utilizationTrend: UtilizationTrend | null
  dateRange: { start: string; end: string }
  isLoading: boolean
  error: string | null
}

const getDefaultDateRange = () => {
  const end = new Date()
  const start = new Date()
  start.setDate(start.getDate() - 30)
  return {
    start: start.toISOString().split('T')[0],
    end: end.toISOString().split('T')[0]
  }
}

const initialState: CapacityState = {
  overview: null,
  utilization: [],
  bottlenecks: [],
  resolutions: [],
  forecast: null,
  forecastMethod: 'MovingAverage',
  plans: [],
  currentPlan: null,
  constraints: [],
  scenarios: [],
  gapAnalysis: null,
  utilizationTrend: null,
  dateRange: getDefaultDateRange(),
  isLoading: false,
  error: null
}

// Async Thunks
export const fetchCapacityOverview = createAsyncThunk(
  'capacity/fetchOverview',
  async ({ companyId, startDate, endDate }: { companyId: number; startDate: string; endDate: string }) => {
    const response = await authorizedFetch(
      `api/capacity/overview/company/${companyId}?startDate=${startDate}&endDate=${endDate}`
    )
    if (!response.ok) throw new Error('Failed to fetch capacity overview')
    return (await response.json()) as CapacityOverview
  }
)

export const fetchEquipmentUtilization = createAsyncThunk(
  'capacity/fetchUtilization',
  async ({ companyId, startDate, endDate }: { companyId: number; startDate: string; endDate: string }) => {
    const response = await authorizedFetch(
      `api/capacity/utilization/equipment/company/${companyId}?startDate=${startDate}&endDate=${endDate}`
    )
    if (!response.ok) throw new Error('Failed to fetch utilization')
    return (await response.json()) as EquipmentUtilization[]
  }
)

export const fetchBottlenecks = createAsyncThunk(
  'capacity/fetchBottlenecks',
  async ({ companyId, startDate, endDate, minSeverity }: { companyId: number; startDate: string; endDate: string; minSeverity?: string }) => {
    let url = `api/capacity/bottlenecks/company/${companyId}?startDate=${startDate}&endDate=${endDate}`
    if (minSeverity) url += `&minSeverity=${minSeverity}`
    const response = await authorizedFetch(url)
    if (!response.ok) throw new Error('Failed to fetch bottlenecks')
    return (await response.json()) as Bottleneck[]
  }
)

export const fetchBottleneckResolutions = createAsyncThunk(
  'capacity/fetchResolutions',
  async ({ equipmentId, startDate, endDate }: { equipmentId: number; startDate: string; endDate: string }) => {
    const response = await authorizedFetch(
      `api/capacity/bottlenecks/${equipmentId}/resolutions?startDate=${startDate}&endDate=${endDate}`
    )
    if (!response.ok) throw new Error('Failed to fetch resolutions')
    return (await response.json()) as BottleneckResolution[]
  }
)

export const fetchCapacityForecast = createAsyncThunk(
  'capacity/fetchForecast',
  async ({ companyId, weeksAhead, method }: { companyId: number; weeksAhead?: number; method?: ForecastMethod }) => {
    let url = `api/capacity/forecast/company/${companyId}`
    const params = new URLSearchParams()
    if (weeksAhead) params.append('weeksAhead', weeksAhead.toString())
    if (method) params.append('method', method)
    if (params.toString()) url += `?${params.toString()}`

    const response = await authorizedFetch(url)
    if (!response.ok) throw new Error('Failed to fetch forecast')
    return (await response.json()) as CapacityForecast
  }
)

export const fetchCapacityPlans = createAsyncThunk(
  'capacity/fetchPlans',
  async ({ companyId, status, planType }: { companyId: number; status?: string; planType?: string }) => {
    let url = `api/capacity/plans/company/${companyId}`
    const params = new URLSearchParams()
    if (status) params.append('status', status)
    if (planType) params.append('planType', planType)
    if (params.toString()) url += `?${params.toString()}`

    const response = await authorizedFetch(url)
    if (!response.ok) throw new Error('Failed to fetch plans')
    const result = (await response.json()) as PagedResult<CapacityPlanSummary>
    return result.items
  }
)

export const fetchCapacityPlan = createAsyncThunk(
  'capacity/fetchPlan',
  async ({ planId, companyId }: { planId: number; companyId: number }) => {
    const response = await authorizedFetch(`api/capacity/plans/${planId}/company/${companyId}`)
    if (!response.ok) throw new Error('Failed to fetch plan')
    return (await response.json()) as CapacityPlanDetail
  }
)

export const createCapacityPlan = createAsyncThunk(
  'capacity/createPlan',
  async ({ companyId, plan }: { companyId: number; plan: CreateCapacityPlanRequest }, { rejectWithValue }) => {
    const response = await authorizedFetch(`api/capacity/plans/company/${companyId}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(plan)
    })
    if (!response.ok) {
      const error = await response.json().catch(() => ({ error: 'Failed to create plan' }))
      return rejectWithValue(error)
    }
    return (await response.json()) as CapacityPlanDetail
  }
)

export const activatePlan = createAsyncThunk(
  'capacity/activatePlan',
  async ({ planId, companyId }: { planId: number; companyId: number }) => {
    const response = await authorizedFetch(`api/capacity/plans/${planId}/activate/company/${companyId}`, {
      method: 'POST'
    })
    if (!response.ok) throw new Error('Failed to activate plan')
    return (await response.json()) as CapacityPlanDetail
  }
)

export const fetchConstraints = createAsyncThunk(
  'capacity/fetchConstraints',
  async ({ companyId, equipmentId, activeOnly }: { companyId: number; equipmentId?: number; activeOnly?: boolean }) => {
    let url = `api/capacity/constraints/company/${companyId}`
    const params = new URLSearchParams()
    if (equipmentId) params.append('equipmentId', equipmentId.toString())
    if (activeOnly !== undefined) params.append('activeOnly', activeOnly.toString())
    if (params.toString()) url += `?${params.toString()}`

    const response = await authorizedFetch(url)
    if (!response.ok) throw new Error('Failed to fetch constraints')
    return (await response.json()) as CapacityConstraint[]
  }
)

export const createConstraint = createAsyncThunk(
  'capacity/createConstraint',
  async ({ companyId, constraint }: { companyId: number; constraint: CreateConstraintRequest }, { rejectWithValue }) => {
    const response = await authorizedFetch(`api/capacity/constraints/company/${companyId}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(constraint)
    })
    if (!response.ok) {
      const error = await response.json().catch(() => ({ error: 'Failed to create constraint' }))
      return rejectWithValue(error)
    }
    return (await response.json()) as CapacityConstraint
  }
)

export const runScenario = createAsyncThunk(
  'capacity/runScenario',
  async ({ companyId, scenario }: { companyId: number; scenario: WhatIfScenario }) => {
    const response = await authorizedFetch(`api/capacity/scenarios/company/${companyId}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(scenario)
    })
    if (!response.ok) throw new Error('Failed to run scenario')
    return (await response.json()) as ScenarioResult
  }
)

export const fetchGapAnalysis = createAsyncThunk(
  'capacity/fetchGapAnalysis',
  async ({ companyId, startDate, endDate }: { companyId: number; startDate: string; endDate: string }) => {
    const response = await authorizedFetch(
      `api/capacity/gap-analysis/company/${companyId}?startDate=${startDate}&endDate=${endDate}`
    )
    if (!response.ok) throw new Error('Failed to fetch gap analysis')
    return (await response.json()) as GapAnalysis
  }
)

export const fetchUtilizationTrend = createAsyncThunk(
  'capacity/fetchUtilizationTrend',
  async ({ companyId, months }: { companyId: number; months?: number }) => {
    let url = `api/capacity/utilization/trend/company/${companyId}`
    if (months) url += `?months=${months}`

    const response = await authorizedFetch(url)
    if (!response.ok) throw new Error('Failed to fetch utilization trend')
    return (await response.json()) as UtilizationTrend
  }
)

export const exportUtilization = createAsyncThunk(
  'capacity/exportUtilization',
  async ({ companyId, startDate, endDate, format }: { companyId: number; startDate: string; endDate: string; format: 'csv' | 'pdf' }) => {
    const response = await authorizedFetch(
      `api/capacity/export/utilization/company/${companyId}?startDate=${startDate}&endDate=${endDate}&format=${format}`
    )
    if (!response.ok) throw new Error('Failed to export utilization')
    const blob = await response.blob()
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `utilization-report.${format}`
    document.body.appendChild(a)
    a.click()
    window.URL.revokeObjectURL(url)
    a.remove()
    return true
  }
)

// Slice
const capacitySlice = createSlice({
  name: 'capacity',
  initialState,
  reducers: {
    setDateRange: (state, action: PayloadAction<{ start: string; end: string }>) => {
      state.dateRange = action.payload
    },
    setForecastMethod: (state, action: PayloadAction<ForecastMethod>) => {
      state.forecastMethod = action.payload
    },
    clearError: (state) => {
      state.error = null
    },
    addScenarioResult: (state, action: PayloadAction<ScenarioResult>) => {
      state.scenarios.push(action.payload)
    },
    clearScenarios: (state) => {
      state.scenarios = []
    }
  },
  extraReducers: (builder) => {
    // Capacity Overview
    builder.addCase(fetchCapacityOverview.pending, (state) => {
      state.isLoading = true
      state.error = null
    })
    builder.addCase(fetchCapacityOverview.fulfilled, (state, action) => {
      state.overview = action.payload
      state.isLoading = false
    })
    builder.addCase(fetchCapacityOverview.rejected, (state, action) => {
      state.isLoading = false
      state.error = action.error.message || 'Failed to fetch capacity overview'
    })

    // Equipment Utilization
    builder.addCase(fetchEquipmentUtilization.fulfilled, (state, action) => {
      state.utilization = action.payload
    })

    // Bottlenecks
    builder.addCase(fetchBottlenecks.fulfilled, (state, action) => {
      state.bottlenecks = action.payload
    })

    // Resolutions
    builder.addCase(fetchBottleneckResolutions.fulfilled, (state, action) => {
      state.resolutions = action.payload
    })

    // Forecast
    builder.addCase(fetchCapacityForecast.pending, (state) => {
      state.isLoading = true
    })
    builder.addCase(fetchCapacityForecast.fulfilled, (state, action) => {
      state.forecast = action.payload
      state.isLoading = false
    })
    builder.addCase(fetchCapacityForecast.rejected, (state, action) => {
      state.isLoading = false
      state.error = action.error.message || 'Failed to fetch forecast'
    })

    // Plans
    builder.addCase(fetchCapacityPlans.fulfilled, (state, action) => {
      state.plans = action.payload
    })
    builder.addCase(fetchCapacityPlan.fulfilled, (state, action) => {
      state.currentPlan = action.payload
    })
    builder.addCase(createCapacityPlan.fulfilled, (state, action) => {
      state.plans.unshift(action.payload)
      state.currentPlan = action.payload
    })
    builder.addCase(activatePlan.fulfilled, (state, action) => {
      state.currentPlan = action.payload
      const index = state.plans.findIndex(p => p.id === action.payload.id)
      if (index !== -1) {
        state.plans[index] = action.payload
      }
    })

    // Constraints
    builder.addCase(fetchConstraints.fulfilled, (state, action) => {
      state.constraints = action.payload
    })
    builder.addCase(createConstraint.fulfilled, (state, action) => {
      state.constraints.push(action.payload)
    })

    // Scenarios
    builder.addCase(runScenario.fulfilled, (state, action) => {
      state.scenarios.push(action.payload)
    })

    // Gap Analysis
    builder.addCase(fetchGapAnalysis.fulfilled, (state, action) => {
      state.gapAnalysis = action.payload
    })

    // Utilization Trend
    builder.addCase(fetchUtilizationTrend.fulfilled, (state, action) => {
      state.utilizationTrend = action.payload
    })
  }
})

export const { setDateRange, setForecastMethod, clearError, addScenarioResult, clearScenarios } = capacitySlice.actions
export default capacitySlice.reducer
