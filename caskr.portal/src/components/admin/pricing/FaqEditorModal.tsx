import { useState, useEffect } from 'react'
import { useAppDispatch, useAppSelector } from '../../../hooks'
import { setEditingFaq, saveFaq } from '../../../features/pricingAdminSlice'

const FAQ_CATEGORIES = ['General', 'Billing', 'Features', 'Technical', 'Enterprise']

function FaqEditorModal() {
  const dispatch = useAppDispatch()
  const { editingFaq, isSaving } = useAppSelector(state => state.pricingAdmin)

  const [formData, setFormData] = useState({
    question: '',
    answer: '',
    category: '',
    sortOrder: 0,
    isActive: true,
    publishDate: '',
    unpublishDate: ''
  })
  const [errors, setErrors] = useState<Record<string, string>>({})

  useEffect(() => {
    if (editingFaq) {
      setFormData({
        question: editingFaq.question || '',
        answer: editingFaq.answer || '',
        category: editingFaq.category || '',
        sortOrder: editingFaq.sortOrder || 0,
        isActive: editingFaq.isActive ?? true,
        publishDate: editingFaq.publishDate
          ? new Date(editingFaq.publishDate).toISOString().slice(0, 16)
          : '',
        unpublishDate: editingFaq.unpublishDate
          ? new Date(editingFaq.unpublishDate).toISOString().slice(0, 16)
          : ''
      })
      setErrors({})
    }
  }, [editingFaq])

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.question.trim()) {
      newErrors.question = 'Question is required'
    }

    if (!formData.answer.trim()) {
      newErrors.answer = 'Answer is required'
    }

    if (formData.publishDate && formData.unpublishDate) {
      if (new Date(formData.publishDate) >= new Date(formData.unpublishDate)) {
        newErrors.unpublishDate = 'Unpublish date must be after publish date'
      }
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    const faqData = {
      ...editingFaq,
      question: formData.question,
      answer: formData.answer,
      category: formData.category || null,
      sortOrder: formData.sortOrder,
      isActive: formData.isActive,
      publishDate: formData.publishDate ? new Date(formData.publishDate).toISOString() : null,
      unpublishDate: formData.unpublishDate ? new Date(formData.unpublishDate).toISOString() : null
    }

    await dispatch(saveFaq(faqData))
  }

  const handleClose = () => {
    dispatch(setEditingFaq(null))
  }

  // Simple rich text helpers
  const insertFormatting = (tag: string) => {
    const textarea = document.getElementById('faq-answer') as HTMLTextAreaElement
    if (!textarea) return

    const start = textarea.selectionStart
    const end = textarea.selectionEnd
    const selectedText = formData.answer.substring(start, end)

    let newText = ''
    switch (tag) {
      case 'bold':
        newText = `<strong>${selectedText}</strong>`
        break
      case 'link':
        const url = prompt('Enter URL:')
        if (url) {
          newText = `<a href="${url}">${selectedText || 'Link text'}</a>`
        } else {
          return
        }
        break
      case 'list':
        newText = `\n<ul>\n  <li>${selectedText || 'Item'}</li>\n</ul>\n`
        break
      default:
        return
    }

    const newAnswer = formData.answer.substring(0, start) + newText + formData.answer.substring(end)
    setFormData(prev => ({ ...prev, answer: newAnswer }))
  }

  if (!editingFaq) return null

  const isNew = !editingFaq.id

  return (
    <div className="modal-overlay" onClick={handleClose}>
      <div className="modal-content modal-large" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2>{isNew ? 'Create New FAQ' : 'Edit FAQ'}</h2>
          <button type="button" className="modal-close" onClick={handleClose}>
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit} className="modal-form">
          <div className="form-group">
            <label htmlFor="faq-question">Question *</label>
            <input
              id="faq-question"
              type="text"
              value={formData.question}
              onChange={e => setFormData(prev => ({ ...prev, question: e.target.value }))}
              placeholder="e.g., What payment methods do you accept?"
            />
            {errors.question && <span className="form-error-text">{errors.question}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="faq-answer">Answer *</label>
            <div className="rich-text-toolbar">
              <button
                type="button"
                className="toolbar-btn"
                onClick={() => insertFormatting('bold')}
                title="Bold"
              >
                <strong>B</strong>
              </button>
              <button
                type="button"
                className="toolbar-btn"
                onClick={() => insertFormatting('link')}
                title="Insert Link"
              >
                Link
              </button>
              <button
                type="button"
                className="toolbar-btn"
                onClick={() => insertFormatting('list')}
                title="Bullet List"
              >
                List
              </button>
            </div>
            <textarea
              id="faq-answer"
              value={formData.answer}
              onChange={e => setFormData(prev => ({ ...prev, answer: e.target.value }))}
              placeholder="Write your answer here. You can use HTML for formatting."
              rows={8}
            />
            {errors.answer && <span className="form-error-text">{errors.answer}</span>}
            <span className="form-hint">Supports HTML: &lt;strong&gt;, &lt;a href&gt;, &lt;ul&gt;&lt;li&gt;</span>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="faq-category">Category</label>
              <select
                id="faq-category"
                value={formData.category}
                onChange={e => setFormData(prev => ({ ...prev, category: e.target.value }))}
              >
                <option value="">No category</option>
                {FAQ_CATEGORIES.map(cat => (
                  <option key={cat} value={cat}>{cat}</option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label htmlFor="faq-sort">Sort Order</label>
              <input
                id="faq-sort"
                type="number"
                min="0"
                value={formData.sortOrder}
                onChange={e => setFormData(prev => ({ ...prev, sortOrder: parseInt(e.target.value) || 0 }))}
              />
            </div>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="faq-publish">Publish Date (Optional)</label>
              <input
                id="faq-publish"
                type="datetime-local"
                value={formData.publishDate}
                onChange={e => setFormData(prev => ({ ...prev, publishDate: e.target.value }))}
              />
              <span className="form-hint">Schedule when this FAQ becomes visible</span>
            </div>

            <div className="form-group">
              <label htmlFor="faq-unpublish">Unpublish Date (Optional)</label>
              <input
                id="faq-unpublish"
                type="datetime-local"
                value={formData.unpublishDate}
                onChange={e => setFormData(prev => ({ ...prev, unpublishDate: e.target.value }))}
              />
              {errors.unpublishDate && (
                <span className="form-error-text">{errors.unpublishDate}</span>
              )}
              <span className="form-hint">Schedule when this FAQ should be hidden</span>
            </div>
          </div>

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
              {isSaving ? 'Saving...' : isNew ? 'Create FAQ' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

export default FaqEditorModal
