import { useState, useMemo } from 'react'
import { useAppDispatch, useAppSelector } from '../../../hooks'
import {
  setEditingFeature,
  saveFeature,
  deleteFeature
} from '../../../features/pricingAdminSlice'
import { tierFeaturesApi } from '../../../api/pricingAdminApi'
import type { PricingFeature, PricingTier } from '../../../types/pricing'
import { FEATURE_CATEGORIES } from '../../../types/pricing'
import FeatureEditorModal from './FeatureEditorModal'

function FeaturesTab() {
  const dispatch = useAppDispatch()
  const { features, tiers, editingFeature, isSaving } = useAppSelector(state => state.pricingAdmin)
  const [viewMode, setViewMode] = useState<'list' | 'matrix'>('list')
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null)
  const [selectedCategory, setSelectedCategory] = useState<string>('all')
  const [copyFromTier, setCopyFromTier] = useState<number | null>(null)

  // Group features by category
  const featuresByCategory = useMemo(() => {
    const filtered = selectedCategory === 'all'
      ? features
      : features.filter(f => f.category === selectedCategory)

    return filtered.reduce((acc, feature) => {
      const category = feature.category || 'Other'
      if (!acc[category]) acc[category] = []
      acc[category].push(feature)
      return acc
    }, {} as Record<string, PricingFeature[]>)
  }, [features, selectedCategory])

  // Build feature matrix
  const sortedTiers = [...tiers].sort((a, b) => a.sortOrder - b.sortOrder)

  const isFeatureIncluded = (tier: PricingTier, featureId: number): boolean => {
    return tier.tierFeatures?.some(tf => tf.featureId === featureId && tf.isIncluded) || false
  }

  const getFeatureValue = (tier: PricingTier, featureId: number): string | null => {
    const tf = tier.tierFeatures?.find(tf => tf.featureId === featureId)
    return tf?.limitDescription || null
  }

  const handleToggleFeature = async (tierId: number, featureId: number, currentlyIncluded: boolean) => {
    try {
      if (currentlyIncluded) {
        await tierFeaturesApi.removeFromTier(tierId, featureId)
      } else {
        await tierFeaturesApi.addToTier(tierId, { featureId, isIncluded: true })
      }
      // Refresh data
      window.location.reload()
    } catch (error) {
      console.error('Failed to toggle feature:', error)
    }
  }

  const handleCopyFeatures = async (fromTierId: number, toTierId: number) => {
    const fromTier = tiers.find(t => t.id === fromTierId)
    if (!fromTier?.tierFeatures) return

    try {
      for (const tf of fromTier.tierFeatures) {
        await tierFeaturesApi.addToTier(toTierId, {
          featureId: tf.featureId,
          isIncluded: tf.isIncluded,
          limitValue: tf.limitValue,
          limitDescription: tf.limitDescription
        })
      }
      setCopyFromTier(null)
      window.location.reload()
    } catch (error) {
      console.error('Failed to copy features:', error)
    }
  }

  const handleDelete = async (id: number) => {
    await dispatch(deleteFeature(id))
    setDeleteConfirm(null)
  }

  const handleCreateNew = () => {
    dispatch(setEditingFeature({
      id: 0,
      name: '',
      description: '',
      category: 'General',
      icon: 'check',
      tooltipText: '',
      sortOrder: features.length,
      isActive: true,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    }))
  }

  return (
    <div className="features-tab">
      <div className="tab-header">
        <h2>Features</h2>
        <div className="tab-header-actions">
          <div className="view-toggle">
            <button
              type="button"
              className={`toggle-btn ${viewMode === 'list' ? 'active' : ''}`}
              onClick={() => setViewMode('list')}
            >
              List
            </button>
            <button
              type="button"
              className={`toggle-btn ${viewMode === 'matrix' ? 'active' : ''}`}
              onClick={() => setViewMode('matrix')}
            >
              Matrix
            </button>
          </div>
          <button
            type="button"
            className="btn btn-primary"
            onClick={handleCreateNew}
          >
            Add Feature
          </button>
        </div>
      </div>

      {viewMode === 'list' && (
        <>
          <div className="filter-bar">
            <select
              value={selectedCategory}
              onChange={e => setSelectedCategory(e.target.value)}
            >
              <option value="all">All Categories</option>
              {FEATURE_CATEGORIES.map(cat => (
                <option key={cat} value={cat}>{cat}</option>
              ))}
            </select>
          </div>

          {Object.entries(featuresByCategory).map(([category, categoryFeatures]) => (
            <div key={category} className="feature-category">
              <h3 className="category-title">{category}</h3>
              <div className="admin-table-container">
                <table className="admin-table">
                  <thead>
                    <tr>
                      <th>Feature</th>
                      <th>Description</th>
                      <th>Status</th>
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {categoryFeatures.sort((a, b) => a.sortOrder - b.sortOrder).map(feature => (
                      <tr key={feature.id} className={!feature.isActive ? 'inactive' : ''}>
                        <td>
                          <strong>{feature.name}</strong>
                        </td>
                        <td className="description-cell">
                          {feature.description || '-'}
                        </td>
                        <td>
                          <span className={`badge ${feature.isActive ? 'badge-success' : 'badge-secondary'}`}>
                            {feature.isActive ? 'Active' : 'Inactive'}
                          </span>
                        </td>
                        <td className="actions-cell">
                          <button
                            type="button"
                            className="btn btn-small btn-secondary"
                            onClick={() => dispatch(setEditingFeature(feature))}
                          >
                            Edit
                          </button>
                          {deleteConfirm === feature.id ? (
                            <div className="delete-confirm">
                              <button
                                type="button"
                                className="btn btn-small btn-danger"
                                onClick={() => handleDelete(feature.id)}
                                disabled={isSaving}
                              >
                                Confirm
                              </button>
                              <button
                                type="button"
                                className="btn btn-small btn-secondary"
                                onClick={() => setDeleteConfirm(null)}
                              >
                                Cancel
                              </button>
                            </div>
                          ) : (
                            <button
                              type="button"
                              className="btn btn-small btn-danger-outline"
                              onClick={() => setDeleteConfirm(feature.id)}
                            >
                              Delete
                            </button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          ))}
        </>
      )}

      {viewMode === 'matrix' && (
        <div className="feature-matrix">
          <div className="copy-features-bar">
            <span>Copy features from:</span>
            <select
              value={copyFromTier || ''}
              onChange={e => setCopyFromTier(e.target.value ? parseInt(e.target.value) : null)}
            >
              <option value="">Select tier...</option>
              {sortedTiers.map(tier => (
                <option key={tier.id} value={tier.id}>{tier.name}</option>
              ))}
            </select>
            {copyFromTier && (
              <span>to:</span>
            )}
            {copyFromTier && (
              <select
                onChange={e => {
                  if (e.target.value) {
                    handleCopyFeatures(copyFromTier, parseInt(e.target.value))
                  }
                }}
              >
                <option value="">Select target tier...</option>
                {sortedTiers.filter(t => t.id !== copyFromTier).map(tier => (
                  <option key={tier.id} value={tier.id}>{tier.name}</option>
                ))}
              </select>
            )}
          </div>

          <div className="matrix-table-container">
            <table className="matrix-table">
              <thead>
                <tr>
                  <th className="feature-name-col">Feature</th>
                  {sortedTiers.map(tier => (
                    <th key={tier.id} className="tier-col">{tier.name}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {features.sort((a, b) => {
                  if (a.category !== b.category) return (a.category || '').localeCompare(b.category || '')
                  return a.sortOrder - b.sortOrder
                }).map((feature, idx, arr) => {
                  const showCategory = idx === 0 || feature.category !== arr[idx - 1].category
                  return (
                    <>
                      {showCategory && (
                        <tr key={`cat-${feature.category}`} className="category-row">
                          <td colSpan={sortedTiers.length + 1}>
                            <strong>{feature.category || 'Other'}</strong>
                          </td>
                        </tr>
                      )}
                      <tr key={feature.id}>
                        <td className="feature-name-col">
                          {feature.name}
                        </td>
                        {sortedTiers.map(tier => {
                          const included = isFeatureIncluded(tier, feature.id)
                          const value = getFeatureValue(tier, feature.id)
                          return (
                            <td key={tier.id} className="tier-col">
                              <button
                                type="button"
                                className={`matrix-toggle ${included ? 'included' : ''}`}
                                onClick={() => handleToggleFeature(tier.id, feature.id, included)}
                              >
                                {value || (included ? '\u2713' : '\u2717')}
                              </button>
                            </td>
                          )
                        })}
                      </tr>
                    </>
                  )
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {editingFeature && <FeatureEditorModal />}
    </div>
  )
}

export default FeaturesTab
