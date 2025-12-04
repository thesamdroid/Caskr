import { useState, useEffect } from 'react'
import { useAppDispatch, useAppSelector } from '../../../hooks'
import { setEditingTier, saveTier } from '../../../features/pricingAdminSlice'

function TierEditorModal() {
  const dispatch = useAppDispatch()
  const { editingTier, isSaving, tiers } = useAppSelector(state => state.pricingAdmin)

  const [formData, setFormData] = useState({
    name: '',
    slug: '',
    tagline: '',
    monthlyPriceCents: '',
    annualPriceCents: '',
    annualDiscountPercent: 0,
    isPopular: false,
    isCustomPricing: false,
    ctaText: 'Get Started',
    ctaUrl: '',
    sortOrder: 0,
    isActive: true
  })
  const [errors, setErrors] = useState<Record<string, string>>({})

  useEffect(() => {
    if (editingTier) {
      setFormData({
        name: editingTier.name || '',
        slug: editingTier.slug || '',
        tagline: editingTier.tagline || '',
        monthlyPriceCents: editingTier.monthlyPriceCents?.toString() || '',
        annualPriceCents: editingTier.annualPriceCents?.toString() || '',
        annualDiscountPercent: editingTier.annualDiscountPercent || 0,
        isPopular: editingTier.isPopular || false,
        isCustomPricing: editingTier.isCustomPricing || false,
        ctaText: editingTier.ctaText || 'Get Started',
        ctaUrl: editingTier.ctaUrl || '',
        sortOrder: editingTier.sortOrder || 0,
        isActive: editingTier.isActive ?? true
      })
      setErrors({})
    }
  }, [editingTier])

  const generateSlug = (name: string) => {
    return name
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-|-$/g, '')
  }

  const handleNameChange = (name: string) => {
    setFormData(prev => ({
      ...prev,
      name,
      slug: prev.slug || generateSlug(name)
    }))
  }

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.name.trim()) {
      newErrors.name = 'Name is required'
    }

    if (!formData.slug.trim()) {
      newErrors.slug = 'Slug is required'
    } else {
      // Check for duplicate slug
      const existingTier = tiers.find(
        t => t.slug === formData.slug && t.id !== editingTier?.id
      )
      if (existingTier) {
        newErrors.slug = 'A tier with this URL already exists'
      }
    }

    if (!formData.isCustomPricing) {
      const monthlyPrice = parseInt(formData.monthlyPriceCents)
      if (isNaN(monthlyPrice) || monthlyPrice < 0) {
        newErrors.monthlyPriceCents = 'Price must be a positive number'
      }

      const annualPrice = parseInt(formData.annualPriceCents)
      if (isNaN(annualPrice) || annualPrice < 0) {
        newErrors.annualPriceCents = 'Price must be a positive number'
      }
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    const tierData = {
      ...editingTier,
      name: formData.name,
      slug: formData.slug,
      tagline: formData.tagline || null,
      monthlyPriceCents: formData.isCustomPricing ? null : parseInt(formData.monthlyPriceCents) || 0,
      annualPriceCents: formData.isCustomPricing ? null : parseInt(formData.annualPriceCents) || 0,
      annualDiscountPercent: formData.annualDiscountPercent,
      isPopular: formData.isPopular,
      isCustomPricing: formData.isCustomPricing,
      ctaText: formData.ctaText || null,
      ctaUrl: formData.ctaUrl || null,
      sortOrder: formData.sortOrder,
      isActive: formData.isActive
    }

    await dispatch(saveTier(tierData))
  }

  const handleClose = () => {
    dispatch(setEditingTier(null))
  }

  if (!editingTier) return null

  const isNew = !editingTier.id

  return (
    <div className="modal-overlay" onClick={handleClose}>
      <div className="modal-content" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2>{isNew ? 'Create New Tier' : 'Edit Tier'}</h2>
          <button type="button" className="modal-close" onClick={handleClose}>
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit} className="modal-form">
          <div className="form-row">
            <div className="form-group">
              <label htmlFor="tier-name">Name *</label>
              <input
                id="tier-name"
                type="text"
                value={formData.name}
                onChange={e => handleNameChange(e.target.value)}
                placeholder="e.g., Professional"
              />
              {errors.name && <span className="form-error-text">{errors.name}</span>}
            </div>

            <div className="form-group">
              <label htmlFor="tier-slug">URL Slug *</label>
              <input
                id="tier-slug"
                type="text"
                value={formData.slug}
                onChange={e => setFormData(prev => ({ ...prev, slug: e.target.value }))}
                placeholder="e.g., professional"
              />
              {errors.slug && <span className="form-error-text">{errors.slug}</span>}
              <span className="form-hint">/pricing/{formData.slug || 'slug'}</span>
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="tier-tagline">Tagline</label>
            <input
              id="tier-tagline"
              type="text"
              value={formData.tagline}
              onChange={e => setFormData(prev => ({ ...prev, tagline: e.target.value }))}
              placeholder="e.g., Perfect for growing distilleries"
            />
          </div>

          <div className="form-group">
            <label className="checkbox-label">
              <input
                type="checkbox"
                checked={formData.isCustomPricing}
                onChange={e => setFormData(prev => ({ ...prev, isCustomPricing: e.target.checked }))}
              />
              Custom Pricing (Contact Sales)
            </label>
          </div>

          {!formData.isCustomPricing && (
            <>
              <div className="form-row">
                <div className="form-group">
                  <label htmlFor="tier-monthly">Monthly Price (cents) *</label>
                  <div className="input-with-prefix">
                    <span className="input-prefix">$</span>
                    <input
                      id="tier-monthly"
                      type="number"
                      min="0"
                      value={formData.monthlyPriceCents}
                      onChange={e => setFormData(prev => ({ ...prev, monthlyPriceCents: e.target.value }))}
                      placeholder="9900"
                    />
                  </div>
                  {errors.monthlyPriceCents && (
                    <span className="form-error-text">{errors.monthlyPriceCents}</span>
                  )}
                  <span className="form-hint">
                    Display: ${(parseInt(formData.monthlyPriceCents) / 100 || 0).toFixed(2)}/month
                  </span>
                </div>

                <div className="form-group">
                  <label htmlFor="tier-annual">Annual Price (cents) *</label>
                  <div className="input-with-prefix">
                    <span className="input-prefix">$</span>
                    <input
                      id="tier-annual"
                      type="number"
                      min="0"
                      value={formData.annualPriceCents}
                      onChange={e => setFormData(prev => ({ ...prev, annualPriceCents: e.target.value }))}
                      placeholder="95040"
                    />
                  </div>
                  {errors.annualPriceCents && (
                    <span className="form-error-text">{errors.annualPriceCents}</span>
                  )}
                  <span className="form-hint">
                    Display: ${(parseInt(formData.annualPriceCents) / 100 || 0).toFixed(2)}/year
                  </span>
                </div>
              </div>

              <div className="form-group">
                <label htmlFor="tier-discount">Annual Discount Percent</label>
                <input
                  id="tier-discount"
                  type="number"
                  min="0"
                  max="100"
                  value={formData.annualDiscountPercent}
                  onChange={e => setFormData(prev => ({ ...prev, annualDiscountPercent: parseInt(e.target.value) || 0 }))}
                />
                <span className="form-hint">
                  {formData.annualDiscountPercent > 0
                    ? `Shows "Save ${formData.annualDiscountPercent}%" badge`
                    : 'No discount badge shown'}
                </span>
              </div>
            </>
          )}

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="tier-cta-text">CTA Button Text</label>
              <input
                id="tier-cta-text"
                type="text"
                value={formData.ctaText}
                onChange={e => setFormData(prev => ({ ...prev, ctaText: e.target.value }))}
                placeholder="Get Started"
              />
            </div>

            <div className="form-group">
              <label htmlFor="tier-cta-url">CTA Button URL</label>
              <input
                id="tier-cta-url"
                type="text"
                value={formData.ctaUrl}
                onChange={e => setFormData(prev => ({ ...prev, ctaUrl: e.target.value }))}
                placeholder="/signup?plan=professional"
              />
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="tier-sort">Sort Order</label>
            <input
              id="tier-sort"
              type="number"
              min="0"
              value={formData.sortOrder}
              onChange={e => setFormData(prev => ({ ...prev, sortOrder: parseInt(e.target.value) || 0 }))}
            />
            <span className="form-hint">Lower numbers appear first</span>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={formData.isActive}
                  onChange={e => setFormData(prev => ({ ...prev, isActive: e.target.checked }))}
                />
                Active (visible on pricing page)
              </label>
            </div>

            <div className="form-group">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={formData.isPopular}
                  onChange={e => setFormData(prev => ({ ...prev, isPopular: e.target.checked }))}
                />
                Mark as "Most Popular"
              </label>
            </div>
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
              {isSaving ? 'Saving...' : isNew ? 'Create Tier' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

export default TierEditorModal
