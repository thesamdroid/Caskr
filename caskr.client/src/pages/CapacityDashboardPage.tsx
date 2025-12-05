import { useCallback, useEffect, useState } from 'react'
import './Dashboard.css'
import './Capacity.css'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  fetchCapacityOverview,
  fetchEquipmentUtilization,
  fetchBottlenecks,
  fetchCapacityForecast,
  fetchCapacityPlans,
  fetchGapAnalysis,
  fetchUtilizationTrend,
  setDateRange,
  setForecastMethod,
  runScenario,
  clearScenarios,
  exportUtilization
} from '../features/capacitySlice'
import type { ForecastMethod, WhatIfScenario } from '../types/capacity'

export default function CapacityDashboardPage() {
  const dispatch = useAppDispatch()
  const {
    overview,
    utilization,
    bottlenecks,
    forecast,
    forecastMethod,
    plans,
    gapAnalysis,
    utilizationTrend,
    scenarios,
    dateRange,
    isLoading,
    error
  } = useAppSelector(state => state.capacity)
  const authUser = useAppSelector(state => state.auth.user)
  const companyId = authUser?.companyId ?? 0

  const [activeTab, setActiveTab] = useState<'overview' | 'forecast' | 'plans' | 'scenarios'>('overview')
  const [showScenarioBuilder, setShowScenarioBuilder] = useState(false)
  const [scenarioForm, setScenarioForm] = useState<WhatIfScenario>({
    name: '',
    demandChangePercent: 0,
    capacityChangePercent: 0,
    newEquipmentIds: [],
    removedEquipmentIds: [],
    efficiencyFactors: {}
  })

  const loadDashboardData = useCallback(() => {
    if (!companyId) return

    dispatch(fetchCapacityOverview({ companyId, startDate: dateRange.start, endDate: dateRange.end }))
    dispatch(fetchEquipmentUtilization({ companyId, startDate: dateRange.start, endDate: dateRange.end }))
    dispatch(fetchBottlenecks({ companyId, startDate: dateRange.start, endDate: dateRange.end }))
    dispatch(fetchCapacityPlans({ companyId }))
    dispatch(fetchGapAnalysis({ companyId, startDate: dateRange.start, endDate: dateRange.end }))
    dispatch(fetchUtilizationTrend({ companyId }))
  }, [dispatch, companyId, dateRange])

  const loadForecast = useCallback(() => {
    if (!companyId) return
    dispatch(fetchCapacityForecast({ companyId, method: forecastMethod }))
  }, [dispatch, companyId, forecastMethod])

  useEffect(() => {
    loadDashboardData()
  }, [loadDashboardData])

  useEffect(() => {
    if (activeTab === 'forecast') {
      loadForecast()
    }
  }, [activeTab, loadForecast])

  const handleDateRangeChange = (start: string, end: string) => {
    dispatch(setDateRange({ start, end }))
  }

  const handleForecastMethodChange = (method: ForecastMethod) => {
    dispatch(setForecastMethod(method))
  }

  const handleRunScenario = async () => {
    if (!companyId || !scenarioForm.name) return
    await dispatch(runScenario({ companyId, scenario: scenarioForm }))
    setShowScenarioBuilder(false)
    setScenarioForm({
      name: '',
      demandChangePercent: 0,
      capacityChangePercent: 0,
      newEquipmentIds: [],
      removedEquipmentIds: [],
      efficiencyFactors: {}
    })
  }

  const handleExport = (format: 'csv' | 'pdf') => {
    if (!companyId) return
    dispatch(exportUtilization({ companyId, startDate: dateRange.start, endDate: dateRange.end, format }))
  }

  const formatPercent = (value: number | undefined) => {
    if (value === undefined) return 'N/A'
    return `${value.toFixed(1)}%`
  }

  const formatHours = (value: number | undefined) => {
    if (value === undefined) return 'N/A'
    return `${value.toFixed(1)}h`
  }

  const getUtilizationColor = (percent: number) => {
    if (percent >= 90) return 'critical'
    if (percent >= 75) return 'warning'
    return 'healthy'
  }

  const getSeverityColor = (severity: string) => {
    switch (severity.toLowerCase()) {
      case 'critical': return 'critical'
      case 'high': return 'warning'
      case 'medium': return 'caution'
      default: return 'healthy'
    }
  }

  return (
    <div className="dashboard-container">
      {/* Dashboard Header */}
      <div className="dashboard-header">
        <div className="header-content">
          <h1 className="page-title">Capacity Planning</h1>
          <p className="page-subtitle">Monitor equipment utilization, forecast demand, and optimize production capacity</p>
        </div>
        <div className="header-actions">
          <div className="date-range-picker">
            <label>
              From:
              <input
                type="date"
                value={dateRange.start}
                onChange={(e) => handleDateRangeChange(e.target.value, dateRange.end)}
              />
            </label>
            <label>
              To:
              <input
                type="date"
                value={dateRange.end}
                onChange={(e) => handleDateRangeChange(dateRange.start, e.target.value)}
              />
            </label>
          </div>
          <div className="export-buttons">
            <button className="button-secondary" onClick={() => handleExport('csv')}>
              Export CSV
            </button>
            <button className="button-secondary" onClick={() => handleExport('pdf')}>
              Export PDF
            </button>
          </div>
        </div>
      </div>

      {error && (
        <div className="dashboard-error" role="alert">
          <div className="dashboard-error-text">
            <p className="dashboard-error-title">Unable to load capacity data.</p>
            <p className="dashboard-error-details">{error}</p>
          </div>
          <button onClick={loadDashboardData} className="button-secondary">
            Try again
          </button>
        </div>
      )}

      {/* Tab Navigation */}
      <div className="tab-navigation">
        <button
          className={`tab-button ${activeTab === 'overview' ? 'active' : ''}`}
          onClick={() => setActiveTab('overview')}
        >
          Overview
        </button>
        <button
          className={`tab-button ${activeTab === 'forecast' ? 'active' : ''}`}
          onClick={() => setActiveTab('forecast')}
        >
          Forecast
        </button>
        <button
          className={`tab-button ${activeTab === 'plans' ? 'active' : ''}`}
          onClick={() => setActiveTab('plans')}
        >
          Capacity Plans
        </button>
        <button
          className={`tab-button ${activeTab === 'scenarios' ? 'active' : ''}`}
          onClick={() => setActiveTab('scenarios')}
        >
          What-If Scenarios
        </button>
      </div>

      {isLoading && (
        <div className="loading-overlay">
          <div className="loading-spinner" />
          <p>Loading capacity data...</p>
        </div>
      )}

      {/* Overview Tab */}
      {activeTab === 'overview' && (
        <div className="capacity-overview">
          {/* Stats Overview */}
          {overview && (
            <div className="stats-grid">
              <div className="stat-card">
                <div className={`stat-icon ${getUtilizationColor(overview.averageUtilization)}`}>
                  <span className="icon">%</span>
                </div>
                <div className="stat-content">
                  <p className="stat-label">Average Utilization</p>
                  <h3 className="stat-value">{formatPercent(overview.averageUtilization)}</h3>
                  <div className={`stat-change ${overview.averageUtilization >= 75 ? 'warning' : 'positive'}`}>
                    <span className="change-icon">{overview.averageUtilization >= 75 ? '!' : '='}</span>
                    <span>{overview.averageUtilization >= 90 ? 'Critical capacity' : overview.averageUtilization >= 75 ? 'High utilization' : 'Healthy range'}</span>
                  </div>
                </div>
              </div>

              <div className="stat-card">
                <div className="stat-icon">
                  <span className="icon">H</span>
                </div>
                <div className="stat-content">
                  <p className="stat-label">Total Capacity</p>
                  <h3 className="stat-value">{formatHours(overview.totalCapacityHours)}</h3>
                  <div className="stat-change neutral">
                    <span className="change-icon">=</span>
                    <span>Available hours</span>
                  </div>
                </div>
              </div>

              <div className="stat-card">
                <div className="stat-icon">
                  <span className="icon">A</span>
                </div>
                <div className="stat-content">
                  <p className="stat-label">Allocated Hours</p>
                  <h3 className="stat-value">{formatHours(overview.allocatedHours)}</h3>
                  <div className="stat-change neutral">
                    <span className="change-icon">=</span>
                    <span>In production</span>
                  </div>
                </div>
              </div>

              <div className="stat-card">
                <div className="stat-icon">
                  <span className="icon">R</span>
                </div>
                <div className="stat-content">
                  <p className="stat-label">Available Hours</p>
                  <h3 className="stat-value">{formatHours(overview.availableHours)}</h3>
                  <div className={`stat-change ${overview.availableHours <= 10 ? 'negative' : 'positive'}`}>
                    <span className="change-icon">{overview.availableHours <= 10 ? '!' : '='}</span>
                    <span>{overview.availableHours <= 10 ? 'Limited capacity' : 'Room to grow'}</span>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Equipment Utilization */}
          <div className="section-container">
            <div className="section-header">
              <h2 className="section-title">Equipment Utilization</h2>
              <p className="section-subtitle">Current utilization by equipment</p>
            </div>

            {utilization.length === 0 ? (
              <div className="empty-state">
                <span className="empty-icon">%</span>
                <h3>No utilization data</h3>
                <p>Equipment utilization data will appear here once available</p>
              </div>
            ) : (
              <div className="utilization-grid">
                {utilization.map(eq => (
                  <div key={eq.equipmentId} className="utilization-card">
                    <div className="utilization-header">
                      <h4 className="equipment-name">{eq.equipmentName}</h4>
                      <span className={`utilization-badge ${getUtilizationColor(eq.utilizationPercent)}`}>
                        {formatPercent(eq.utilizationPercent)}
                      </span>
                    </div>
                    <div className="utilization-bar-container">
                      <div
                        className={`utilization-bar ${getUtilizationColor(eq.utilizationPercent)}`}
                        style={{ width: `${Math.min(eq.utilizationPercent, 100)}%` }}
                      />
                    </div>
                    <div className="utilization-details">
                      <span>Total: {formatHours(eq.totalHours)}</span>
                      <span>Used: {formatHours(eq.usedHours)}</span>
                      <span>Available: {formatHours(eq.availableHours)}</span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Bottlenecks */}
          <div className="section-container">
            <div className="section-header">
              <h2 className="section-title">Bottlenecks</h2>
              <p className="section-subtitle">Identified capacity constraints</p>
            </div>

            {bottlenecks.length === 0 ? (
              <div className="empty-state success">
                <span className="empty-icon">+</span>
                <h3>No bottlenecks detected</h3>
                <p>All equipment is operating within healthy capacity limits</p>
              </div>
            ) : (
              <div className="bottleneck-list">
                {bottlenecks.map((bottleneck, index) => (
                  <div key={index} className={`bottleneck-card ${getSeverityColor(bottleneck.severity)}`}>
                    <div className="bottleneck-header">
                      <span className={`severity-badge ${getSeverityColor(bottleneck.severity)}`}>
                        {bottleneck.severity}
                      </span>
                      <h4 className="bottleneck-equipment">{bottleneck.equipmentName}</h4>
                    </div>
                    <p className="bottleneck-description">{bottleneck.description}</p>
                    <div className="bottleneck-metrics">
                      <span>Impact: {formatPercent(bottleneck.impactPercent)}</span>
                      <span>Current: {formatPercent(bottleneck.currentUtilization)}</span>
                    </div>
                    {bottleneck.recommendations && bottleneck.recommendations.length > 0 && (
                      <div className="bottleneck-recommendations">
                        <h5>Recommendations:</h5>
                        <ul>
                          {bottleneck.recommendations.map((rec, i) => (
                            <li key={i}>{rec}</li>
                          ))}
                        </ul>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Gap Analysis */}
          {gapAnalysis && (
            <div className="section-container">
              <div className="section-header">
                <h2 className="section-title">Gap Analysis</h2>
                <p className="section-subtitle">Capacity vs demand comparison</p>
              </div>

              <div className="gap-analysis-card">
                <div className="gap-summary">
                  <div className="gap-metric">
                    <span className="gap-label">Required Capacity</span>
                    <span className="gap-value">{formatHours(gapAnalysis.requiredCapacity)}</span>
                  </div>
                  <div className="gap-metric">
                    <span className="gap-label">Available Capacity</span>
                    <span className="gap-value">{formatHours(gapAnalysis.availableCapacity)}</span>
                  </div>
                  <div className={`gap-metric ${gapAnalysis.gapHours >= 0 ? 'surplus' : 'deficit'}`}>
                    <span className="gap-label">Gap</span>
                    <span className="gap-value">
                      {gapAnalysis.gapHours >= 0 ? '+' : ''}{formatHours(gapAnalysis.gapHours)}
                    </span>
                  </div>
                </div>
                {gapAnalysis.recommendations && gapAnalysis.recommendations.length > 0 && (
                  <div className="gap-recommendations">
                    <h5>Recommendations:</h5>
                    <ul>
                      {gapAnalysis.recommendations.map((rec, i) => (
                        <li key={i}>{rec}</li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Utilization Trend */}
          {utilizationTrend && (
            <div className="section-container">
              <div className="section-header">
                <h2 className="section-title">Utilization Trend</h2>
                <p className="section-subtitle">Historical utilization over time</p>
              </div>

              <div className="trend-chart">
                <div className="trend-bars">
                  {utilizationTrend.dataPoints.map((point, index) => (
                    <div key={index} className="trend-bar-container">
                      <div
                        className={`trend-bar ${getUtilizationColor(point.utilizationPercent)}`}
                        style={{ height: `${Math.min(point.utilizationPercent, 100)}%` }}
                        title={`${point.period}: ${formatPercent(point.utilizationPercent)}`}
                      />
                      <span className="trend-label">{point.period}</span>
                    </div>
                  ))}
                </div>
                <div className="trend-summary">
                  <span>Average: {formatPercent(utilizationTrend.averageUtilization)}</span>
                  <span>Peak: {formatPercent(utilizationTrend.peakUtilization)}</span>
                  <span>Trend: {utilizationTrend.trendDirection}</span>
                </div>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Forecast Tab */}
      {activeTab === 'forecast' && (
        <div className="capacity-forecast">
          <div className="forecast-controls">
            <label>
              Forecast Method:
              <select
                value={forecastMethod}
                onChange={(e) => handleForecastMethodChange(e.target.value as ForecastMethod)}
              >
                <option value="MovingAverage">Moving Average</option>
                <option value="ExponentialSmoothing">Exponential Smoothing</option>
                <option value="LinearRegression">Linear Regression</option>
                <option value="SeasonalAdjusted">Seasonal Adjusted</option>
              </select>
            </label>
            <button className="button-primary" onClick={loadForecast}>
              Refresh Forecast
            </button>
          </div>

          {forecast ? (
            <div className="forecast-content">
              <div className="forecast-summary">
                <div className="forecast-metric">
                  <span className="forecast-label">Predicted Demand</span>
                  <span className="forecast-value">{formatHours(forecast.predictedDemandHours)}</span>
                </div>
                <div className="forecast-metric">
                  <span className="forecast-label">Available Capacity</span>
                  <span className="forecast-value">{formatHours(forecast.availableCapacityHours)}</span>
                </div>
                <div className="forecast-metric">
                  <span className="forecast-label">Confidence</span>
                  <span className="forecast-value">{formatPercent(forecast.confidenceLevel)}</span>
                </div>
              </div>

              <div className="weekly-forecasts">
                <h3>Weekly Breakdown</h3>
                <div className="forecast-table">
                  <div className="forecast-header">
                    <span>Week</span>
                    <span>Predicted Demand</span>
                    <span>Available Capacity</span>
                    <span>Utilization</span>
                  </div>
                  {forecast.weeklyForecasts.map((week, index) => (
                    <div key={index} className="forecast-row">
                      <span>{week.weekStartDate}</span>
                      <span>{formatHours(week.predictedDemand)}</span>
                      <span>{formatHours(week.availableCapacity)}</span>
                      <span className={getUtilizationColor(week.predictedUtilization)}>
                        {formatPercent(week.predictedUtilization)}
                      </span>
                    </div>
                  ))}
                </div>
              </div>

              {forecast.recommendations && forecast.recommendations.length > 0 && (
                <div className="forecast-recommendations">
                  <h3>Recommendations</h3>
                  <ul>
                    {forecast.recommendations.map((rec, i) => (
                      <li key={i}>{rec}</li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          ) : (
            <div className="empty-state">
              <span className="empty-icon">F</span>
              <h3>No forecast data</h3>
              <p>Select a forecast method and click refresh to generate predictions</p>
            </div>
          )}
        </div>
      )}

      {/* Capacity Plans Tab */}
      {activeTab === 'plans' && (
        <div className="capacity-plans">
          <div className="plans-header">
            <h3>Capacity Plans</h3>
            <button className="button-primary">
              Create New Plan
            </button>
          </div>

          {plans.length === 0 ? (
            <div className="empty-state">
              <span className="empty-icon">P</span>
              <h3>No capacity plans</h3>
              <p>Create a capacity plan to start optimizing production</p>
            </div>
          ) : (
            <div className="plans-grid">
              {plans.map(plan => (
                <div key={plan.id} className="plan-card">
                  <div className="plan-header">
                    <h4 className="plan-name">{plan.name}</h4>
                    <span className={`plan-status ${plan.status.toLowerCase()}`}>
                      {plan.status}
                    </span>
                  </div>
                  <div className="plan-details">
                    <span>Type: {plan.planType}</span>
                    <span>Period: {plan.startDate} - {plan.endDate}</span>
                  </div>
                  <div className="plan-targets">
                    {plan.targetProofGallons && (
                      <span>Target: {plan.targetProofGallons.toLocaleString()} PG</span>
                    )}
                  </div>
                  <div className="plan-actions">
                    <button className="button-secondary">View Details</button>
                    {plan.status === 'Draft' && (
                      <button className="button-primary">Activate</button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* What-If Scenarios Tab */}
      {activeTab === 'scenarios' && (
        <div className="capacity-scenarios">
          <div className="scenarios-header">
            <h3>What-If Scenarios</h3>
            <button
              className="button-primary"
              onClick={() => setShowScenarioBuilder(!showScenarioBuilder)}
            >
              {showScenarioBuilder ? 'Cancel' : 'New Scenario'}
            </button>
            {scenarios.length > 0 && (
              <button className="button-secondary" onClick={() => dispatch(clearScenarios())}>
                Clear All
              </button>
            )}
          </div>

          {showScenarioBuilder && (
            <div className="scenario-builder">
              <h4>Build a Scenario</h4>
              <div className="scenario-form">
                <label>
                  Scenario Name:
                  <input
                    type="text"
                    value={scenarioForm.name}
                    onChange={(e) => setScenarioForm({ ...scenarioForm, name: e.target.value })}
                    placeholder="e.g., Holiday Rush Scenario"
                  />
                </label>
                <label>
                  Demand Change (%):
                  <input
                    type="number"
                    value={scenarioForm.demandChangePercent}
                    onChange={(e) => setScenarioForm({ ...scenarioForm, demandChangePercent: parseFloat(e.target.value) || 0 })}
                    placeholder="e.g., 20 for 20% increase"
                  />
                </label>
                <label>
                  Capacity Change (%):
                  <input
                    type="number"
                    value={scenarioForm.capacityChangePercent}
                    onChange={(e) => setScenarioForm({ ...scenarioForm, capacityChangePercent: parseFloat(e.target.value) || 0 })}
                    placeholder="e.g., -10 for 10% reduction"
                  />
                </label>
                <button
                  className="button-primary"
                  onClick={handleRunScenario}
                  disabled={!scenarioForm.name}
                >
                  Run Scenario
                </button>
              </div>
            </div>
          )}

          {scenarios.length === 0 ? (
            <div className="empty-state">
              <span className="empty-icon">?</span>
              <h3>No scenarios yet</h3>
              <p>Create a what-if scenario to explore different capacity situations</p>
            </div>
          ) : (
            <div className="scenarios-grid">
              {scenarios.map((scenario, index) => (
                <div key={index} className="scenario-card">
                  <div className="scenario-header">
                    <h4 className="scenario-name">{scenario.scenarioName}</h4>
                    <span className={`feasibility-badge ${scenario.isFeasible ? 'feasible' : 'not-feasible'}`}>
                      {scenario.isFeasible ? 'Feasible' : 'Not Feasible'}
                    </span>
                  </div>
                  <div className="scenario-results">
                    <div className="scenario-metric">
                      <span className="metric-label">Projected Utilization</span>
                      <span className={`metric-value ${getUtilizationColor(scenario.projectedUtilization)}`}>
                        {formatPercent(scenario.projectedUtilization)}
                      </span>
                    </div>
                    <div className="scenario-metric">
                      <span className="metric-label">Capacity Gap</span>
                      <span className={`metric-value ${scenario.capacityGap >= 0 ? 'surplus' : 'deficit'}`}>
                        {scenario.capacityGap >= 0 ? '+' : ''}{formatHours(scenario.capacityGap)}
                      </span>
                    </div>
                  </div>
                  {scenario.recommendations && scenario.recommendations.length > 0 && (
                    <div className="scenario-recommendations">
                      <h5>Recommendations:</h5>
                      <ul>
                        {scenario.recommendations.map((rec, i) => (
                          <li key={i}>{rec}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                  {scenario.bottlenecks && scenario.bottlenecks.length > 0 && (
                    <div className="scenario-bottlenecks">
                      <h5>Potential Bottlenecks:</h5>
                      <ul>
                        {scenario.bottlenecks.map((bn, i) => (
                          <li key={i}>{bn.equipmentName}: {bn.description}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  )
}
