import { useState } from 'react'
import { useAppDispatch, useAppSelector } from '../../../hooks'
import {
  setEditingFaq,
  saveFaq,
  deleteFaq
} from '../../../features/pricingAdminSlice'
import type { PricingFaq } from '../../../types/pricing'
import FaqEditorModal from './FaqEditorModal'

function FaqsTab() {
  const dispatch = useAppDispatch()
  const { faqs, editingFaq, isSaving } = useAppSelector(state => state.pricingAdmin)
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null)
  const [previewFaq, setPreviewFaq] = useState<PricingFaq | null>(null)
  const [draggedId, setDraggedId] = useState<number | null>(null)

  const sortedFaqs = [...faqs].sort((a, b) => a.sortOrder - b.sortOrder)

  const handleDelete = async (id: number) => {
    await dispatch(deleteFaq(id))
    setDeleteConfirm(null)
  }

  const handleToggleActive = async (faq: PricingFaq) => {
    await dispatch(saveFaq({ ...faq, isActive: !faq.isActive }))
  }

  const handleDragStart = (e: React.DragEvent, id: number) => {
    setDraggedId(id)
    e.dataTransfer.effectAllowed = 'move'
  }

  const handleDragOver = async (e: React.DragEvent, targetId: number) => {
    e.preventDefault()
    if (draggedId === null || draggedId === targetId) return

    const draggedIndex = sortedFaqs.findIndex(f => f.id === draggedId)
    const targetIndex = sortedFaqs.findIndex(f => f.id === targetId)

    if (draggedIndex !== targetIndex) {
      // Update sort order for dragged item
      const draggedFaq = sortedFaqs[draggedIndex]
      const targetFaq = sortedFaqs[targetIndex]

      await dispatch(saveFaq({ ...draggedFaq, sortOrder: targetFaq.sortOrder }))
      await dispatch(saveFaq({ ...targetFaq, sortOrder: draggedFaq.sortOrder }))
    }
  }

  const handleDragEnd = () => {
    setDraggedId(null)
  }

  const handleCreateNew = () => {
    dispatch(setEditingFaq({
      id: 0,
      question: '',
      answer: '',
      category: undefined,
      sortOrder: faqs.length,
      isActive: true,
      publishDate: undefined,
      unpublishDate: undefined,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    }))
  }

  const truncateText = (text: string, maxLength: number = 100) => {
    if (text.length <= maxLength) return text
    return text.substring(0, maxLength) + '...'
  }

  const isScheduled = (faq: PricingFaq): boolean => {
    if (!faq.publishDate) return false
    return new Date(faq.publishDate) > new Date()
  }

  const isExpired = (faq: PricingFaq): boolean => {
    if (!faq.unpublishDate) return false
    return new Date(faq.unpublishDate) < new Date()
  }

  return (
    <div className="faqs-tab">
      <div className="tab-header">
        <h2>FAQs</h2>
        <button
          type="button"
          className="btn btn-primary"
          onClick={handleCreateNew}
        >
          Add FAQ
        </button>
      </div>

      <div className="faqs-list">
        {sortedFaqs.map(faq => (
          <div
            key={faq.id}
            className={`faq-item ${!faq.isActive ? 'inactive' : ''} ${draggedId === faq.id ? 'dragging' : ''}`}
            draggable
            onDragStart={e => handleDragStart(e, faq.id)}
            onDragOver={e => handleDragOver(e, faq.id)}
            onDragEnd={handleDragEnd}
          >
            <div className="faq-drag-handle">
              <span className="drag-icon">&#x2630;</span>
            </div>

            <div className="faq-content">
              <div className="faq-question">
                <strong>{faq.question}</strong>
                <div className="faq-badges">
                  {!faq.isActive && (
                    <span className="badge badge-secondary">Inactive</span>
                  )}
                  {isScheduled(faq) && (
                    <span className="badge badge-info">Scheduled</span>
                  )}
                  {isExpired(faq) && (
                    <span className="badge badge-secondary">Expired</span>
                  )}
                  {faq.category && (
                    <span className="badge badge-primary">{faq.category}</span>
                  )}
                </div>
              </div>
              <div className="faq-answer-preview">
                {truncateText(faq.answer)}
              </div>
            </div>

            <div className="faq-actions">
              <button
                type="button"
                className="btn btn-small btn-secondary"
                onClick={() => setPreviewFaq(faq)}
              >
                Preview
              </button>
              <button
                type="button"
                className="btn btn-small btn-secondary"
                onClick={() => dispatch(setEditingFaq(faq))}
              >
                Edit
              </button>
              <button
                type="button"
                className={`btn btn-small ${faq.isActive ? 'btn-warning-outline' : 'btn-success-outline'}`}
                onClick={() => handleToggleActive(faq)}
                disabled={isSaving}
              >
                {faq.isActive ? 'Deactivate' : 'Activate'}
              </button>
              {deleteConfirm === faq.id ? (
                <div className="delete-confirm">
                  <button
                    type="button"
                    className="btn btn-small btn-danger"
                    onClick={() => handleDelete(faq.id)}
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
                  onClick={() => setDeleteConfirm(faq.id)}
                >
                  Delete
                </button>
              )}
            </div>
          </div>
        ))}

        {faqs.length === 0 && (
          <div className="empty-state">
            <p>No FAQs yet. Add your first FAQ to get started.</p>
          </div>
        )}
      </div>

      {/* Preview Modal */}
      {previewFaq && (
        <div className="modal-overlay" onClick={() => setPreviewFaq(null)}>
          <div className="modal-content preview-modal" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h2>FAQ Preview</h2>
              <button
                type="button"
                className="modal-close"
                onClick={() => setPreviewFaq(null)}
              >
                &times;
              </button>
            </div>
            <div className="faq-preview-content">
              <div className="preview-question">{previewFaq.question}</div>
              <div
                className="preview-answer"
                dangerouslySetInnerHTML={{ __html: previewFaq.answer }}
              />
            </div>
          </div>
        </div>
      )}

      {editingFaq && <FaqEditorModal />}
    </div>
  )
}

export default FaqsTab
