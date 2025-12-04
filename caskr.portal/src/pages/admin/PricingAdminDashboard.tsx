import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '../../hooks'
import {
  checkAccess,
  fetchTiers,
  fetchFeatures,
  fetchFaqs,
  fetchPromotions,
  fetchAuditLogs,
  setActiveTab,
  clearError
} from '../../features/pricingAdminSlice'
import TiersTab from '../../components/admin/pricing/TiersTab'
import FeaturesTab from '../../components/admin/pricing/FeaturesTab'
import FaqsTab from '../../components/admin/pricing/FaqsTab'
import PromotionsTab from '../../components/admin/pricing/PromotionsTab'
import AuditLogTab from '../../components/admin/pricing/AuditLogTab'

type TabType = 'tiers' | 'features' | 'faqs' | 'promotions' | 'audit'

const tabs: { id: TabType; label: string }[] = [
  { id: 'tiers', label: 'Pricing Tiers' },
  { id: 'features', label: 'Features' },
  { id: 'faqs', label: 'FAQs' },
  { id: 'promotions', label: 'Promotions' },
  { id: 'audit', label: 'Audit Log' }
]

function PricingAdminDashboard() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const {
    activeTab,
    isLoading,
    error,
    hasAdminAccess,
    hasUnsavedChanges
  } = useAppSelector(state => state.pricingAdmin)
  const user = useAppSelector(state => state.auth.user)

  // Check admin access on mount
  useEffect(() => {
    dispatch(checkAccess())
  }, [dispatch])

  // Redirect if no access
  useEffect(() => {
    if (hasAdminAccess === false) {
      navigate('/403', { replace: true })
    }
  }, [hasAdminAccess, navigate])

  // Load data for active tab
  useEffect(() => {
    if (hasAdminAccess) {
      switch (activeTab) {
        case 'tiers':
          dispatch(fetchTiers())
          break
        case 'features':
          dispatch(fetchFeatures())
          dispatch(fetchTiers()) // Need tiers for feature matrix
          break
        case 'faqs':
          dispatch(fetchFaqs())
          break
        case 'promotions':
          dispatch(fetchPromotions())
          dispatch(fetchTiers()) // Need tiers for promotion restrictions
          break
        case 'audit':
          dispatch(fetchAuditLogs({ limit: 100 }))
          break
      }
    }
  }, [dispatch, activeTab, hasAdminAccess])

  // Warn before leaving with unsaved changes
  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      if (hasUnsavedChanges) {
        e.preventDefault()
        e.returnValue = ''
      }
    }
    window.addEventListener('beforeunload', handleBeforeUnload)
    return () => window.removeEventListener('beforeunload', handleBeforeUnload)
  }, [hasUnsavedChanges])

  const handleTabChange = (tabId: TabType) => {
    if (hasUnsavedChanges) {
      const confirmed = window.confirm(
        'You have unsaved changes. Are you sure you want to switch tabs?'
      )
      if (!confirmed) return
    }
    dispatch(setActiveTab(tabId))
  }

  const handlePreview = () => {
    window.open('/pricing?preview=true', '_blank')
  }

  // Show loading while checking access
  if (hasAdminAccess === null) {
    return (
      <div className="admin-page">
        <div className="loading-state">
          <div className="loading-spinner" />
          <p>Checking access...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="admin-page pricing-admin">
      <div className="admin-header">
        <div className="admin-header-content">
          <div>
            <h1>Pricing Administration</h1>
            <p className="admin-subtitle">
              Manage pricing tiers, features, FAQs, and promotional codes
            </p>
          </div>
          <div className="admin-header-actions">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={handlePreview}
            >
              Preview Changes
            </button>
          </div>
        </div>
        {hasUnsavedChanges && (
          <div className="unsaved-changes-banner">
            You have unsaved changes
          </div>
        )}
      </div>

      {error && (
        <div className="admin-error">
          <span>{error}</span>
          <button onClick={() => dispatch(clearError())} className="error-dismiss">
            Dismiss
          </button>
        </div>
      )}

      <div className="admin-tabs">
        {tabs.map(tab => (
          <button
            key={tab.id}
            type="button"
            className={`admin-tab ${activeTab === tab.id ? 'active' : ''}`}
            onClick={() => handleTabChange(tab.id)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      <div className="admin-content">
        {isLoading ? (
          <div className="loading-state">
            <div className="loading-spinner" />
            <p>Loading...</p>
          </div>
        ) : (
          <>
            {activeTab === 'tiers' && <TiersTab />}
            {activeTab === 'features' && <FeaturesTab />}
            {activeTab === 'faqs' && <FaqsTab />}
            {activeTab === 'promotions' && <PromotionsTab />}
            {activeTab === 'audit' && <AuditLogTab />}
          </>
        )}
      </div>

      <div className="admin-footer">
        <p className="audit-notice">
          All changes are logged. Current user: {user?.email}
        </p>
      </div>
    </div>
  )
}

export default PricingAdminDashboard
