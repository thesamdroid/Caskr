import { useState, useMemo } from 'react'
import { useAppDispatch, useAppSelector } from '../../../hooks'
import {
  setEditingPromotion,
  savePromotion,
  deletePromotion
} from '../../../features/pricingAdminSlice'
import { promotionsApi } from '../../../api/pricingAdminApi'
import type { PricingPromotion, PromoStatus } from '../../../types/pricing'
import { getPromoStatus } from '../../../types/pricing'
import PromoEditorModal from './PromoEditorModal'

function PromotionsTab() {
  const dispatch = useAppDispatch()
  const { promotions, tiers, editingPromotion, isSaving } = useAppSelector(state => state.pricingAdmin)
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null)
  const [statusFilter, setStatusFilter] = useState<PromoStatus | 'all'>('all')
  const [generatedCode, setGeneratedCode] = useState<string | null>(null)
  const [codePrefix, setCodePrefix] = useState('')
  const [importCsv, setImportCsv] = useState('')
  const [showImport, setShowImport] = useState(false)

  // Filter and sort promotions
  const filteredPromotions = useMemo(() => {
    let filtered = [...promotions]

    if (statusFilter !== 'all') {
      filtered = filtered.filter(p => getPromoStatus(p) === statusFilter)
    }

    return filtered.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
  }, [promotions, statusFilter])

  const statusCounts = useMemo(() => {
    return promotions.reduce(
      (acc, p) => {
        const status = getPromoStatus(p)
        acc[status]++
        return acc
      },
      { active: 0, scheduled: 0, expired: 0, inactive: 0 } as Record<PromoStatus, number>
    )
  }, [promotions])

  const formatDiscount = (promo: PricingPromotion): string => {
    switch (promo.discountType) {
      case 'Percentage':
        return `${promo.discountValue}% off`
      case 'FixedAmount':
        return `$${(promo.discountValue / 100).toFixed(2)} off`
      case 'FreeMonths':
        return `${promo.discountValue} month${promo.discountValue > 1 ? 's' : ''} free`
      default:
        return `${promo.discountValue}`
    }
  }

  const getApplicableTiers = (promo: PricingPromotion): string => {
    if (!promo.appliesToTiersJson) return 'All tiers'
    try {
      const tierIds = JSON.parse(promo.appliesToTiersJson) as number[]
      if (tierIds.length === 0) return 'All tiers'
      return tiers
        .filter(t => tierIds.includes(t.id))
        .map(t => t.name)
        .join(', ')
    } catch {
      return 'All tiers'
    }
  }

  const handleDelete = async (id: number) => {
    await dispatch(deletePromotion(id))
    setDeleteConfirm(null)
  }

  const handleDuplicate = (promo: PricingPromotion) => {
    dispatch(setEditingPromotion({
      ...promo,
      id: 0,
      code: promo.code + '-COPY',
      currentRedemptions: 0,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    }))
  }

  const handleGenerateCode = async () => {
    try {
      const result = await promotionsApi.generateCode(codePrefix || undefined)
      setGeneratedCode(result.code)
    } catch (error) {
      console.error('Failed to generate code:', error)
    }
  }

  const handleImportCsv = async () => {
    try {
      const result = await promotionsApi.importCsv(importCsv)
      alert(`Imported ${result.imported} promotions. ${result.errors.length} errors.`)
      setShowImport(false)
      setImportCsv('')
      window.location.reload()
    } catch (error) {
      console.error('Failed to import:', error)
    }
  }

  const handleExportCsv = async () => {
    try {
      const result = await promotionsApi.exportCsv()
      const blob = new Blob([result.csv], { type: 'text/csv' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = 'promotions.csv'
      a.click()
      URL.revokeObjectURL(url)
    } catch (error) {
      console.error('Failed to export:', error)
    }
  }

  const handleCreateNew = () => {
    dispatch(setEditingPromotion({
      id: 0,
      code: '',
      description: '',
      discountType: 'Percentage',
      discountValue: 10,
      appliesToTiersJson: undefined,
      validFrom: undefined,
      validUntil: undefined,
      maxRedemptions: undefined,
      currentRedemptions: 0,
      minimumMonths: undefined,
      isActive: true,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    }))
  }

  const getStatusBadgeClass = (status: PromoStatus): string => {
    switch (status) {
      case 'active':
        return 'badge-success'
      case 'scheduled':
        return 'badge-info'
      case 'expired':
        return 'badge-secondary'
      case 'inactive':
        return 'badge-secondary'
      default:
        return 'badge-secondary'
    }
  }

  return (
    <div className="promotions-tab">
      <div className="tab-header">
        <h2>Promotions</h2>
        <div className="tab-header-actions">
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => setShowImport(!showImport)}
          >
            Import CSV
          </button>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={handleExportCsv}
          >
            Export CSV
          </button>
          <button
            type="button"
            className="btn btn-primary"
            onClick={handleCreateNew}
          >
            Add Promotion
          </button>
        </div>
      </div>

      {/* Code Generator */}
      <div className="code-generator">
        <h3>Generate Promo Code</h3>
        <div className="code-generator-row">
          <input
            type="text"
            placeholder="Prefix (optional)"
            value={codePrefix}
            onChange={e => setCodePrefix(e.target.value.toUpperCase())}
          />
          <button
            type="button"
            className="btn btn-secondary"
            onClick={handleGenerateCode}
          >
            Generate
          </button>
          {generatedCode && (
            <span className="generated-code">
              <strong>{generatedCode}</strong>
              <button
                type="button"
                className="btn btn-small btn-secondary"
                onClick={() => navigator.clipboard.writeText(generatedCode)}
              >
                Copy
              </button>
            </span>
          )}
        </div>
      </div>

      {/* Import Modal */}
      {showImport && (
        <div className="import-section">
          <h3>Import Promotions from CSV</h3>
          <p className="form-hint">
            Format: code,discountType,discountValue,validFrom,validUntil,maxRedemptions
          </p>
          <textarea
            value={importCsv}
            onChange={e => setImportCsv(e.target.value)}
            rows={5}
            placeholder="SUMMER2024,Percentage,20,2024-06-01,2024-09-01,100"
          />
          <div className="import-actions">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => setShowImport(false)}
            >
              Cancel
            </button>
            <button
              type="button"
              className="btn btn-primary"
              onClick={handleImportCsv}
              disabled={!importCsv.trim()}
            >
              Import
            </button>
          </div>
        </div>
      )}

      {/* Status Filter Tabs */}
      <div className="status-tabs">
        <button
          type="button"
          className={`status-tab ${statusFilter === 'all' ? 'active' : ''}`}
          onClick={() => setStatusFilter('all')}
        >
          All ({promotions.length})
        </button>
        <button
          type="button"
          className={`status-tab ${statusFilter === 'active' ? 'active' : ''}`}
          onClick={() => setStatusFilter('active')}
        >
          Active ({statusCounts.active})
        </button>
        <button
          type="button"
          className={`status-tab ${statusFilter === 'scheduled' ? 'active' : ''}`}
          onClick={() => setStatusFilter('scheduled')}
        >
          Scheduled ({statusCounts.scheduled})
        </button>
        <button
          type="button"
          className={`status-tab ${statusFilter === 'expired' ? 'active' : ''}`}
          onClick={() => setStatusFilter('expired')}
        >
          Expired ({statusCounts.expired})
        </button>
      </div>

      <div className="admin-table-container">
        <table className="admin-table">
          <thead>
            <tr>
              <th>Code</th>
              <th>Discount</th>
              <th>Applies To</th>
              <th>Valid Period</th>
              <th>Usage</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {filteredPromotions.map(promo => {
              const status = getPromoStatus(promo)
              return (
                <tr key={promo.id} className={status === 'inactive' ? 'inactive' : ''}>
                  <td>
                    <strong className="promo-code">{promo.code}</strong>
                    {promo.description && (
                      <div className="promo-description">{promo.description}</div>
                    )}
                  </td>
                  <td>{formatDiscount(promo)}</td>
                  <td>{getApplicableTiers(promo)}</td>
                  <td className="date-cell">
                    {promo.validFrom && (
                      <div>From: {new Date(promo.validFrom).toLocaleDateString()}</div>
                    )}
                    {promo.validUntil && (
                      <div>Until: {new Date(promo.validUntil).toLocaleDateString()}</div>
                    )}
                    {!promo.validFrom && !promo.validUntil && <span>Always valid</span>}
                  </td>
                  <td className="usage-cell">
                    <span className="usage-count">
                      {promo.currentRedemptions}
                      {promo.maxRedemptions && ` / ${promo.maxRedemptions}`}
                    </span>
                  </td>
                  <td>
                    <span className={`badge ${getStatusBadgeClass(status)}`}>
                      {status.charAt(0).toUpperCase() + status.slice(1)}
                    </span>
                  </td>
                  <td className="actions-cell">
                    <button
                      type="button"
                      className="btn btn-small btn-secondary"
                      onClick={() => dispatch(setEditingPromotion(promo))}
                    >
                      Edit
                    </button>
                    <button
                      type="button"
                      className="btn btn-small btn-secondary"
                      onClick={() => handleDuplicate(promo)}
                    >
                      Duplicate
                    </button>
                    {deleteConfirm === promo.id ? (
                      <div className="delete-confirm">
                        <button
                          type="button"
                          className="btn btn-small btn-danger"
                          onClick={() => handleDelete(promo.id)}
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
                        onClick={() => setDeleteConfirm(promo.id)}
                      >
                        Delete
                      </button>
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      {filteredPromotions.length === 0 && (
        <div className="empty-state">
          <p>No promotions found.</p>
        </div>
      )}

      {editingPromotion && <PromoEditorModal />}
    </div>
  )
}

export default PromotionsTab
