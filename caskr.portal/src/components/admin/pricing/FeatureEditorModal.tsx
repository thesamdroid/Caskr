import { useState, useEffect } from 'react'
import { useAppDispatch, useAppSelector } from '../../../hooks'
import { setEditingFeature, saveFeature } from '../../../features/pricingAdminSlice'
import { FEATURE_CATEGORIES, FEATURE_ICONS } from '../../../types/pricing'

function FeatureEditorModal() {
  const dispatch = useAppDispatch()
  const { editingFeature, isSaving } = useAppSelector(state => state.pricingAdmin)

  const [formData, setFormData] = useState({
    name: '',
    description: '',
    category: 'General',
    icon: 'check',
    tooltipText: '',
    sortOrder: 0,
    isActive: true
  })
  const [errors, setErrors] = useState<Record<string, string>>({})

  useEffect(() => {
    if (editingFeature) {
      setFormData({
        name: editingFeature.name || '',
        description: editingFeature.description || '',
        category: editingFeature.category || 'General',
        icon: editingFeature.icon || 'check',
        tooltipText: editingFeature.tooltipText || '',
        sortOrder: editingFeature.sortOrder || 0,
        isActive: editingFeature.isActive ?? true
      })
      setErrors({})
    }
  }, [editingFeature])

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.name.trim()) {
      newErrors.name = 'Name is required'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    const featureData = {
      ...editingFeature,
      name: formData.name,
      description: formData.description || null,
      category: formData.category,
      icon: formData.icon,
      tooltipText: formData.tooltipText || null,
      sortOrder: formData.sortOrder,
      isActive: formData.isActive
    }

    await dispatch(saveFeature(featureData))
  }

  const handleClose = () => {
    dispatch(setEditingFeature(null))
  }

  if (!editingFeature) return null

  const isNew = !editingFeature.id

  return (
    <div className="modal-overlay" onClick={handleClose}>
      <div className="modal-content" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2>{isNew ? 'Create New Feature' : 'Edit Feature'}</h2>
          <button type="button" className="modal-close" onClick={handleClose}>
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit} className="modal-form">
          <div className="form-group">
            <label htmlFor="feature-name">Feature Name *</label>
            <input
              id="feature-name"
              type="text"
              value={formData.name}
              onChange={e => setFormData(prev => ({ ...prev, name: e.target.value }))}
              placeholder="e.g., Barrel Tracking"
            />
            {errors.name && <span className="form-error-text">{errors.name}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="feature-description">Description</label>
            <textarea
              id="feature-description"
              value={formData.description}
              onChange={e => setFormData(prev => ({ ...prev, description: e.target.value }))}
              placeholder="Brief description of this feature"
              rows={3}
            />
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="feature-category">Category</label>
              <select
                id="feature-category"
                value={formData.category}
                onChange={e => setFormData(prev => ({ ...prev, category: e.target.value }))}
              >
                {FEATURE_CATEGORIES.map(cat => (
                  <option key={cat} value={cat}>{cat}</option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label htmlFor="feature-icon">Icon</label>
              <select
                id="feature-icon"
                value={formData.icon}
                onChange={e => setFormData(prev => ({ ...prev, icon: e.target.value }))}
              >
                {FEATURE_ICONS.map(icon => (
                  <option key={icon} value={icon}>{icon}</option>
                ))}
              </select>
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="feature-tooltip">Tooltip Text</label>
            <input
              id="feature-tooltip"
              type="text"
              value={formData.tooltipText}
              onChange={e => setFormData(prev => ({ ...prev, tooltipText: e.target.value }))}
              placeholder="Additional info shown on hover"
            />
          </div>

          <div className="form-group">
            <label htmlFor="feature-sort">Sort Order</label>
            <input
              id="feature-sort"
              type="number"
              min="0"
              value={formData.sortOrder}
              onChange={e => setFormData(prev => ({ ...prev, sortOrder: parseInt(e.target.value) || 0 }))}
            />
            <span className="form-hint">Lower numbers appear first within the category</span>
          </div>

          <div className="form-group">
            <label className="checkbox-label">
              <input
                type="checkbox"
                checked={formData.isActive}
                onChange={e => setFormData(prev => ({ ...prev, isActive: e.target.checked }))}
              />
              Active (shown in feature lists)
            </label>
          </div>

          <div className="modal-actions">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={handleClose}
              disabled={isSaving}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={isSaving}
            >
              {isSaving ? 'Saving...' : isNew ? 'Create Feature' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

export default FeatureEditorModal
