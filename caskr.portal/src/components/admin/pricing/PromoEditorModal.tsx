import { useState, useEffect } from 'react'
import { useAppDispatch, useAppSelector } from '../../../hooks'
import { setEditingPromotion, savePromotion } from '../../../features/pricingAdminSlice'
import type { DiscountType } from '../../../types/pricing'

function PromoEditorModal() {
  const dispatch = useAppDispatch()
  const { editingPromotion, isSaving, tiers } = useAppSelector(state => state.pricingAdmin)

  const [formData, setFormData] = useState({
    code: '',
    description: '',
    discountType: 'Percentage' as DiscountType,
    discountValue: 10,
    selectedTiers: [] as number[],
    validFrom: '',
    validUntil: '',
    maxRedemptions: '',
    minimumMonths: '',
    isActive: true
  })
  const [errors, setErrors] = useState<Record<string, string>>({})

  useEffect(() => {
    if (editingPromotion) {
      let selectedTiers: number[] = []
      if (editingPromotion.appliesToTiersJson) {
        try {
          selectedTiers = JSON.parse(editingPromotion.appliesToTiersJson)
        } catch {
          selectedTiers = []
        }
      }

      setFormData({
        code: editingPromotion.code || '',
        description: editingPromotion.description || '',
        discountType: editingPromotion.discountType || 'Percentage',
        discountValue: editingPromotion.discountValue || 10,
        selectedTiers,
        validFrom: editingPromotion.validFrom
          ? new Date(editingPromotion.validFrom).toISOString().slice(0, 16)
          : '',
        validUntil: editingPromotion.validUntil
          ? new Date(editingPromotion.validUntil).toISOString().slice(0, 16)
          : '',
        maxRedemptions: editingPromotion.maxRedemptions?.toString() || '',
        minimumMonths: editingPromotion.minimumMonths?.toString() || '',
        isActive: editingPromotion.isActive ?? true
      })
      setErrors({})
    }
  }, [editingPromotion])

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.code.trim()) {
      newErrors.code = 'Promo code is required'
    } else if (!/^[A-Z0-9_-]+$/i.test(formData.code)) {
      newErrors.code = 'Code can only contain letters, numbers, hyphens, and underscores'
    }

    if (formData.discountValue <= 0) {
      newErrors.discountValue = 'Discount value must be positive'
    }

    if (formData.discountType === 'Percentage' && formData.discountValue > 100) {
      newErrors.discountValue = 'Percentage discount cannot exceed 100%'
    }

    if (formData.validFrom && formData.validUntil) {
      if (new Date(formData.validFrom) >= new Date(formData.validUntil)) {
        newErrors.validUntil = 'End date must be after start date'
      }
    }

    if (formData.validUntil) {
      if (new Date(formData.validUntil) < new Date()) {
        newErrors.validUntil = 'End date must be in the future'
      }
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    const promoData = {
      ...editingPromotion,
      code: formData.code.toUpperCase(),
      description: formData.description || null,
      discountType: formData.discountType,
      discountValue: formData.discountValue,
      appliesToTiersJson:
        formData.selectedTiers.length > 0
          ? JSON.stringify(formData.selectedTiers)
          : null,
      validFrom: formData.validFrom ? new Date(formData.validFrom).toISOString() : null,
      validUntil: formData.validUntil ? new Date(formData.validUntil).toISOString() : null,
      maxRedemptions: formData.maxRedemptions ? parseInt(formData.maxRedemptions) : null,
      minimumMonths: formData.minimumMonths ? parseInt(formData.minimumMonths) : null,
      isActive: formData.isActive
    }

    await dispatch(savePromotion(promoData))
  }

  const handleClose = () => {
    dispatch(setEditingPromotion(null))
  }

  const handleTierToggle = (tierId: number) => {
    setFormData(prev => ({
      ...prev,
      selectedTiers: prev.selectedTiers.includes(tierId)
        ? prev.selectedTiers.filter(id => id !== tierId)
        : [...prev.selectedTiers, tierId]
    }))
  }

  const getDiscountPreview = (): string => {
    switch (formData.discountType) {
      case 'Percentage':
        return `${formData.discountValue}% off`
      case 'FixedAmount':
        return `$${(formData.discountValue / 100).toFixed(2)} off`
      case 'FreeMonths':
        return `${formData.discountValue} month${formData.discountValue !== 1 ? 's' : ''} free`
      default:
        return ''
    }
  }

  if (!editingPromotion) return null

  const isNew = !editingPromotion.id
  const sortedTiers = [...tiers].sort((a, b) => a.sortOrder - b.sortOrder)

  return (
    <div className="modal-overlay" onClick={handleClose}>
      <div className="modal-content modal-large" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2>{isNew ? 'Create New Promotion' : 'Edit Promotion'}</h2>
          <button type="button" className="modal-close" onClick={handleClose}>
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit} className="modal-form">
          <div className="form-row">
            <div className="form-group">
              <label htmlFor="promo-code">Promo Code *</label>
              <input
                id="promo-code"
                type="text"
                value={formData.code}
                onChange={e => setFormData(prev => ({ ...prev, code: e.target.value.toUpperCase() }))}
                placeholder="SUMMER2024"
              />
              {errors.code && <span className="form-error-text">{errors.code}</span>}
            </div>

            <div className="form-group">
              <label htmlFor="promo-description">Description</label>
              <input
                id="promo-description"
                type="text"
                value={formData.description}
                onChange={e => setFormData(prev => ({ ...prev, description: e.target.value }))}
                placeholder="Summer sale promotion"
              />
            </div>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="promo-type">Discount Type</label>
              <select
                id="promo-type"
                value={formData.discountType}
                onChange={e => setFormData(prev => ({
                  ...prev,
                  discountType: e.target.value as DiscountType
                }))}
              >
                <option value="Percentage">Percentage</option>
                <option value="FixedAmount">Fixed Amount (cents)</option>
                <option value="FreeMonths">Free Months</option>
              </select>
            </div>

            <div className="form-group">
              <label htmlFor="promo-value">Discount Value *</label>
              <input
                id="promo-value"
                type="number"
                min="1"
                value={formData.discountValue}
                onChange={e => setFormData(prev => ({
                  ...prev,
                  discountValue: parseInt(e.target.value) || 0
                }))}
              />
              {errors.discountValue && (
                <span className="form-error-text">{errors.discountValue}</span>
              )}
              <span className="form-hint">Preview: {getDiscountPreview()}</span>
            </div>
          </div>

          <div className="form-group">
            <label>Applies to Tiers</label>
            <div className="tier-checkboxes">
              {sortedTiers.map(tier => (
                <label key={tier.id} className="checkbox-label">
                  <input
                    type="checkbox"
                    checked={formData.selectedTiers.includes(tier.id)}
                    onChange={() => handleTierToggle(tier.id)}
                  />
                  {tier.name}
                </label>
              ))}
            </div>
            <span className="form-hint">
              {formData.selectedTiers.length === 0
                ? 'Applies to all tiers'
                : `Applies to ${formData.selectedTiers.length} tier(s)`}
            </span>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="promo-valid-from">Valid From</label>
              <input
                id="promo-valid-from"
                type="datetime-local"
                value={formData.validFrom}
                onChange={e => setFormData(prev => ({ ...prev, validFrom: e.target.value }))}
              />
            </div>

            <div className="form-group">
              <label htmlFor="promo-valid-until">Valid Until</label>
              <input
                id="promo-valid-until"
                type="datetime-local"
                value={formData.validUntil}
                onChange={e => setFormData(prev => ({ ...prev, validUntil: e.target.value }))}
              />
              {errors.validUntil && (
                <span className="form-error-text">{errors.validUntil}</span>
              )}
            </div>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="promo-max">Max Redemptions</label>
              <input
                id="promo-max"
                type="number"
                min="0"
                value={formData.maxRedemptions}
                onChange={e => setFormData(prev => ({ ...prev, maxRedemptions: e.target.value }))}
                placeholder="Unlimited"
              />
              {!isNew && editingPromotion.currentRedemptions > 0 && (
                <span className="form-hint">
                  Used {editingPromotion.currentRedemptions} times
                </span>
              )}
            </div>

            <div className="form-group">
              <label htmlFor="promo-min-months">Minimum Months</label>
              <input
                id="promo-min-months"
                type="number"
                min="0"
                value={formData.minimumMonths}
                onChange={e => setFormData(prev => ({ ...prev, minimumMonths: e.target.value }))}
                placeholder="No minimum"
              />
              <span className="form-hint">
                Require annual billing or minimum subscription length
              </span>
            </div>
          </div>

          <div className="form-group">
            <label className="checkbox-label">
              <input
                type="checkbox"
                checked={formData.isActive}
                onChange={e => setFormData(prev => ({ ...prev, isActive: e.target.checked }))}
              />
              Active (can be used by customers)
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
              {isSaving ? 'Saving...' : isNew ? 'Create Promotion' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

export default PromoEditorModal
