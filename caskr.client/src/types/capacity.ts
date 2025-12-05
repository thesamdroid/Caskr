// Capacity Planning Types

export interface EquipmentCapacitySummary {
  equipmentId: number
  equipmentName: string
  equipmentType: string
  totalCapacityHours: number
  allocatedHours: number
  availableHours: number
  utilizationPercent: number
}

export interface CapacityAlert {
  severity: 'Info' | 'Warning' | 'Critical'
  title: string
  description: string
  equipmentId?: number
  equipmentName?: string
}

export interface CapacityOverview {
  totalEquipmentCount: number
  totalCapacityHours: number
  allocatedHours: number
  availableHours: number
  overallUtilizationPercent: number
  equipment: EquipmentCapacitySummary[]
  alerts: CapacityAlert[]
  periodStart: string
  periodEnd: string
  // Extended UI properties
  averageUtilization?: number
}

export interface EquipmentUtilization {
  equipmentId: number
  equipmentName: string
  equipmentType: string
  utilizationPercent: number
  hoursAvailable: number
  hoursAllocated: number
  hoursInMaintenance: number
  // Extended UI properties
  totalHours?: number
  usedHours?: number
  availableHours?: number
}

export interface Bottleneck {
  equipmentId: number
  equipmentName: string
  equipmentType: string
  severity: 'Low' | 'Medium' | 'High' | 'Critical'
  utilizationPercent: number
  affectedProductionRuns: number
  averageWaitTime: string
  description: string
  // Extended UI properties
  impactPercent?: number
  currentUtilization?: number
  recommendations?: string[]
}

export interface BottleneckResolution {
  type: string
  description: string
  estimatedCostImpact: number
  estimatedCapacityGain: number
  estimatedImplementationTime: string
  prerequisites: string[]
  effectivenessScore: number
}

export interface WeeklyForecast {
  weekStart: string
  weekEnd: string
  predictedUtilization: number
  lowerBound: number
  upperBound: number
  predictedHoursUsed: number
  availableHours: number
  // Extended UI properties
  weekStartDate?: string
  predictedDemand?: number
  availableCapacity?: number
}

export interface CapacityForecast {
  generatedAt: string
  method: string
  confidenceLevel: number
  weeks: WeeklyForecast[]
  trend: string
  assumptions: string[]
  // Extended UI properties
  predictedDemandHours?: number
  availableCapacityHours?: number
  weeklyForecasts?: WeeklyForecast[]
  recommendations?: string[]
}

export interface CapacityPlanSummary {
  id: number
  name: string
  description?: string
  planPeriodStart: string
  planPeriodEnd: string
  planType: string
  status: string
  targetProofGallons?: number
  targetBottles?: number
  targetBatches?: number
  allocationCount: number
  createdAt: string
  // Alias properties for UI convenience
  startDate?: string
  endDate?: string
}

export interface CapacityAllocation {
  id: number
  equipmentId: number
  equipmentName: string
  allocationType: string
  startDate: string
  endDate: string
  hoursAllocated: number
  productionType?: string
  notes?: string
}

export interface CapacityPlanDetail extends CapacityPlanSummary {
  notes?: string
  createdByUserName?: string
  allocations: CapacityAllocation[]
}

export interface CapacityConstraint {
  id: number
  equipmentId?: number
  equipmentName?: string
  constraintType: string
  constraintValue: number
  effectiveFrom: string
  effectiveTo?: string
  reason?: string
  isActive: boolean
}

export interface ScenarioChange {
  type: 'AddEquipment' | 'RemoveEquipment' | 'ChangeCapacity' | 'AddConstraint' | 'RemoveConstraint' | 'ChangeOperatingHours' | 'AddProductionRun'
  equipmentId?: number
  parameters: Record<string, unknown>
}

export interface WhatIfScenario {
  name?: string
  changes?: ScenarioChange[]
  evaluationPeriodStart?: string
  evaluationPeriodEnd?: string
  // Extended UI properties for what-if analysis
  demandChangePercent?: number
  capacityChangePercent?: number
  newEquipmentIds?: number[]
  removedEquipmentIds?: number[]
  efficiencyFactors?: Record<string, number>
}

export interface ScenarioResult {
  scenarioId: number
  scenarioName: string
  projectedCapacity: CapacityOverview
  projectedBottlenecks: Bottleneck[]
  capacityChangePercent: number
  costImpact: number
  summary: string
  // Extended scenario properties for UI
  projectedUtilization?: number
  capacityGap?: number
  recommendations?: string[]
  bottlenecks?: Bottleneck[]
  isFeasible?: boolean
}

export interface GapAnalysis {
  periodStart: string
  periodEnd: string
  capacityHours: number
  demandHours: number
  gapHours: number
  gapPercent: number
  hasCapacityShortfall: boolean
  weeklyBreakdown: WeeklyGap[]
  recommendations: string[]
  // Extended UI properties
  requiredCapacity?: number
  availableCapacity?: number
}

export interface WeeklyGap {
  weekStart: string
  capacityHours: number
  demandHours: number
  gapHours: number
  gapPercent: number
}

export interface TrendDataPoint {
  date: string
  value: number
  label?: string
  // Extended UI properties
  utilizationPercent?: number
  period?: string
}

export interface UtilizationTrend {
  periodStart: string
  periodEnd: string
  monthlyData: MonthlyUtilization[]
  trendDirection: number
  trendDescription: string
  // Extended UI properties
  averageUtilization?: number
  peakUtilization?: number
  dataPoints?: TrendDataPoint[]
}

export interface MonthlyUtilization {
  year: number
  month: number
  utilizationPercent: number
  hoursAvailable: number
  hoursUsed: number
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

// Request DTOs
export interface CreateCapacityPlanRequest {
  name: string
  description?: string
  planPeriodStart: string
  planPeriodEnd: string
  planType: string
  targetProofGallons?: number
  targetBottles?: number
  targetBatches?: number
  notes?: string
  allocations?: CreateAllocationRequest[]
}

export interface CreateAllocationRequest {
  equipmentId: number
  allocationType: string
  startDate: string
  endDate: string
  hoursAllocated: number
  productionType?: string
  notes?: string
}

export interface CreateConstraintRequest {
  equipmentId?: number
  constraintType: string
  constraintValue: number
  effectiveFrom: string
  effectiveTo?: string
  reason?: string
}

export type ForecastMethod = 'MovingAverage' | 'ExponentialSmoothing' | 'LinearRegression' | 'SeasonalAdjusted'
