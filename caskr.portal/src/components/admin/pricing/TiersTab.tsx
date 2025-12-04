import { useState } from 'react'
import { useAppDispatch, useAppSelector } from '../../../hooks'
import {
  setEditingTier,
  saveTier,
  deleteTier,
  reorderTiersLocally
} from '../../../features/pricingAdminSlice'
import type { PricingTier } from '../../../types/pricing'
import TierEditorModal from './TierEditorModal'

function TiersTab() {
  const dispatch = useAppDispatch()
  const { tiers, editingTier, isSaving } = useAppSelector(state => state.pricingAdmin)
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null)
  const [draggedId, setDraggedId] = useState<number | null>(null)

  const sortedTiers = [...tiers].sort((a, b) => a.sortOrder - b.sortOrder)

  const formatPrice = (cents?: number) => {
    if (cents === undefined || cents === null) return 'Custom'
    return `$${(cents / 100).toFixed(2)}`
  }

  const handleSetPopular = async (tierId: number) => {
    const tier = tiers.find(t => t.id === tierId)
    if (!tier) return

    // Update all tiers to remove popular flag
    for (const t of tiers) {
      if (t.isPopular && t.id !== tierId) {
        await dispatch(saveTier({ ...t, isPopular: false }))
      }
    }

    // Set the selected tier as popular
    await dispatch(saveTier({ ...tier, isPopular: true }))
  }

  const handleToggleActive = async (tier: PricingTier) => {
    await dispatch(saveTier({ ...tier, isActive: !tier.isActive }))
  }

  const handleDelete = async (id: number) => {
    await dispatch(deleteTier(id))
    setDeleteConfirm(null)
  }

  const handleDragStart = (e: React.DragEvent, id: number) => {
    setDraggedId(id)
    e.dataTransfer.effectAllowed = 'move'
  }

  const handleDragOver = (e: React.DragEvent, targetId: number) => {
    e.preventDefault()
    if (draggedId === null || draggedId === targetId) return

    const draggedIndex = sortedTiers.findIndex(t => t.id === draggedId)
    const targetIndex = sortedTiers.findIndex(t => t.id === targetId)

    if (draggedIndex !== targetIndex) {
      const newOrder = [...sortedTiers]
      const [removed] = newOrder.splice(draggedIndex, 1)
      newOrder.splice(targetIndex, 0, removed)
      dispatch(reorderTiersLocally(newOrder.map(t => t.id)))
    }
  }

  const handleDragEnd = () => {
    setDraggedId(null)
  }

  const handleCreateNew = () => {
    dispatch(setEditingTier({
      id: 0,
      name: '',
      slug: '',
      tagline: '',
      monthlyPriceCents: undefined,
      annualPriceCents: undefined,
      annualDiscountPercent: 0,
      isPopular: false,
      isCustomPricing: false,
      ctaText: 'Get Started',
      ctaUrl: '',
      sortOrder: tiers.length,
      isActive: true,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    }))
  }

  return (
    <div className="tiers-tab">
      <div className="tab-header">
        <h2>Pricing Tiers</h2>
        <button
          type="button"
          className="btn btn-primary"
          onClick={handleCreateNew}
        >
          Add New Tier
        </button>
      </div>

      <div className="admin-table-container">
        <table className="admin-table">
          <thead>
            <tr>
              <th style={{ width: '40px' }}></th>
              <th>Name</th>
              <th>Monthly</th>
              <th>Annual</th>
              <th>Status</th>
              <th>Popular</th>
              <th>Subscribers</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {sortedTiers.map(tier => (
              <tr
                key={tier.id}
                className={`${!tier.isActive ? 'inactive' : ''} ${draggedId === tier.id ? 'dragging' : ''}`}
                draggable
                onDragStart={e => handleDragStart(e, tier.id)}
                onDragOver={e => handleDragOver(e, tier.id)}
                onDragEnd={handleDragEnd}
              >
                <td className="drag-handle">
                  <span className="drag-icon">&#x2630;</span>
                </td>
                <td>
                  <div className="tier-name-cell">
                    <strong>{tier.name}</strong>
                    <span className="tier-slug">/{tier.slug}</span>
                  </div>
                </td>
                <td className="price-cell">
                  {tier.isCustomPricing ? (
                    <span className="custom-price">Custom</span>
                  ) : (
                    formatPrice(tier.monthlyPriceCents)
                  )}
                </td>
                <td className="price-cell">
                  {tier.isCustomPricing ? (
                    <span className="custom-price">Custom</span>
                  ) : (
                    <>
                      {formatPrice(tier.annualPriceCents)}
                      {tier.annualDiscountPercent > 0 && (
                        <span className="discount-badge">
                          -{tier.annualDiscountPercent}%
                        </span>
                      )}
                    </>
                  )}
                </td>
                <td>
                  <button
                    type="button"
                    className={`status-toggle ${tier.isActive ? 'active' : 'inactive'}`}
                    onClick={() => handleToggleActive(tier)}
                    disabled={isSaving}
                  >
                    {tier.isActive ? 'Active' : 'Inactive'}
                  </button>
                </td>
                <td>
                  <button
                    type="button"
                    className={`popular-toggle ${tier.isPopular ? 'popular' : ''}`}
                    onClick={() => handleSetPopular(tier.id)}
                    disabled={isSaving || tier.isPopular}
                  >
                    {tier.isPopular ? 'Popular' : 'Set Popular'}
                  </button>
                </td>
                <td className="subscribers-cell">
                  <span className="subscriber-count">--</span>
                </td>
                <td className="actions-cell">
                  <button
                    type="button"
                    className="btn btn-small btn-secondary"
                    onClick={() => dispatch(setEditingTier(tier))}
                  >
                    Edit
                  </button>
                  {deleteConfirm === tier.id ? (
                    <div className="delete-confirm">
                      <span>Delete?</span>
                      <button
                        type="button"
                        className="btn btn-small btn-danger"
                        onClick={() => handleDelete(tier.id)}
                        disabled={isSaving}
                      >
                        Yes
                      </button>
                      <button
                        type="button"
                        className="btn btn-small btn-secondary"
                        onClick={() => setDeleteConfirm(null)}
                      >
                        No
                      </button>
                    </div>
                  ) : (
                    <button
                      type="button"
                      className="btn btn-small btn-danger-outline"
                      onClick={() => setDeleteConfirm(tier.id)}
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

      {editingTier && <TierEditorModal />}
    </div>
  )
}

export default TiersTab
