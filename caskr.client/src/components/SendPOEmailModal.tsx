import { useState, useEffect } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  sendPOEmail,
  fetchPurchaseOrders,
  PurchaseOrder
} from '../features/purchaseOrdersSlice'

interface SendPOEmailModalProps {
  isOpen: boolean
  onClose: () => void
  purchaseOrder: PurchaseOrder
}

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

function SendPOEmailModal({ isOpen, onClose, purchaseOrder }: SendPOEmailModalProps) {
  const dispatch = useAppDispatch()

  const [sending, setSending] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [toEmail, setToEmail] = useState('')
  const [subject, setSubject] = useState('')
  const [body, setBody] = useState('')

  // Initialize form when modal opens
  useEffect(() => {
    if (isOpen && purchaseOrder) {
      setToEmail(purchaseOrder.supplierEmail || '')
      setSubject(`Purchase Order ${purchaseOrder.poNumber}`)
      setBody(generateDefaultBody(purchaseOrder))
      setError(null)
    }
  }, [isOpen, purchaseOrder])

  const generateDefaultBody = (po: PurchaseOrder): string => {
    return `Dear ${po.supplierName},

Please find attached Purchase Order ${po.poNumber} dated ${formatDate(po.orderDate)}.

${po.expectedDeliveryDate ? `Expected delivery date: ${formatDate(po.expectedDeliveryDate)}` : ''}

Please confirm receipt of this order and provide any updates on availability and delivery.

Thank you for your business.

Best regards,
CASKr Distillery`
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)

    // Validation
    if (!toEmail.trim()) {
      setError('Recipient email is required')
      return
    }

    if (!subject.trim()) {
      setError('Subject is required')
      return
    }

    // Basic email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!emailRegex.test(toEmail.trim())) {
      setError('Please enter a valid email address')
      return
    }

    setSending(true)

    try {
      await dispatch(sendPOEmail({
        purchaseOrderId: purchaseOrder.id,
        toEmail: toEmail.trim(),
        subject: subject.trim(),
        body: body.trim()
      })).unwrap()

      // Refresh the PO list to update status
      dispatch(fetchPurchaseOrders({}))
      onClose()
    } catch (err: unknown) {
      const errorMessage = err && typeof err === 'object' && 'message' in err
        ? (err as { message: string }).message
        : 'Failed to send email'
      setError(errorMessage)
    } finally {
      setSending(false)
    }
  }

  if (!isOpen) return null

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal modal-large" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Send Purchase Order to Supplier</h2>
          <button onClick={onClose} className="modal-close" aria-label="Close">
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            {error && (
              <div className="form-error">{error}</div>
            )}

            <div className="email-preview">
              <div className="preview-header">
                <span className="preview-label">Sending PO:</span>
                <span className="preview-value">{purchaseOrder.poNumber}</span>
              </div>
              <div className="preview-note">
                A PDF of the purchase order will be attached to this email.
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="to-email">To *</label>
              <input
                id="to-email"
                type="email"
                value={toEmail}
                onChange={e => setToEmail(e.target.value)}
                placeholder="supplier@example.com"
                required
              />
              {!purchaseOrder.supplierEmail && (
                <span className="field-hint">No email on file for this supplier</span>
              )}
            </div>

            <div className="form-group">
              <label htmlFor="subject">Subject *</label>
              <input
                id="subject"
                type="text"
                value={subject}
                onChange={e => setSubject(e.target.value)}
                placeholder="Purchase Order..."
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="body">Message</label>
              <textarea
                id="body"
                value={body}
                onChange={e => setBody(e.target.value)}
                rows={10}
                placeholder="Enter your message..."
              />
            </div>

            <div className="attachment-info">
              <span className="attachment-icon">📎</span>
              <span>Attachment: {purchaseOrder.poNumber}.pdf</span>
            </div>
          </div>

          <div className="modal-footer">
            <button type="button" onClick={onClose} className="button-secondary" disabled={sending}>
              Cancel
            </button>
            <button type="submit" className="button-primary" disabled={sending}>
              {sending ? 'Sending...' : 'Send Email'}
            </button>
          </div>
        </form>

        <style>{`
          .modal-large {
            max-width: 600px;
          }

          .form-error {
            background: var(--color-error-bg, rgba(239, 68, 68, 0.1));
            color: var(--color-error);
            padding: 0.75rem;
            border-radius: 4px;
            margin-bottom: 1rem;
          }

          .email-preview {
            background: var(--color-surface);
            border: 1px solid var(--color-border);
            border-radius: 8px;
            padding: 1rem;
            margin-bottom: 1.5rem;
          }

          .preview-header {
            display: flex;
            align-items: center;
            gap: 0.5rem;
            margin-bottom: 0.5rem;
          }

          .preview-label {
            color: var(--color-text-secondary);
          }

          .preview-value {
            font-weight: 600;
            color: var(--gold-primary, #D4AF37);
          }

          .preview-note {
            font-size: 0.875rem;
            color: var(--color-text-secondary);
          }

          .form-group {
            margin-bottom: 1rem;
          }

          .form-group label {
            display: block;
            margin-bottom: 0.25rem;
            font-size: 0.875rem;
            color: var(--color-text-secondary);
          }

          .form-group input,
          .form-group textarea {
            width: 100%;
            padding: 0.5rem;
            border: 1px solid var(--color-border);
            border-radius: 4px;
            background: var(--color-surface);
            color: var(--color-text);
            font-size: 1rem;
            font-family: inherit;
          }

          .form-group input:focus,
          .form-group textarea:focus {
            outline: none;
            border-color: var(--gold-primary, #D4AF37);
          }

          .form-group textarea {
            resize: vertical;
          }

          .field-hint {
            display: block;
            margin-top: 0.25rem;
            font-size: 0.75rem;
            color: var(--color-warning, #F59E0B);
          }

          .attachment-info {
            display: flex;
            align-items: center;
            gap: 0.5rem;
            padding: 0.75rem;
            background: var(--color-surface);
            border: 1px solid var(--color-border);
            border-radius: 4px;
            font-size: 0.875rem;
          }

          .attachment-icon {
            font-size: 1.25rem;
          }

          .modal-overlay {
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: rgba(0, 0, 0, 0.5);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 1000;
          }

          .modal {
            background: var(--color-background);
            border-radius: 8px;
            width: 90%;
            max-height: 90vh;
            overflow-y: auto;
          }

          .modal-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 1rem;
            border-bottom: 1px solid var(--color-border);
          }

          .modal-header h2 {
            margin: 0;
            font-size: 1.25rem;
          }

          .modal-close {
            background: none;
            border: none;
            font-size: 1.5rem;
            cursor: pointer;
            color: var(--color-text-secondary);
          }

          .modal-body {
            padding: 1rem;
          }

          .modal-footer {
            display: flex;
            justify-content: flex-end;
            gap: 0.5rem;
            padding: 1rem;
            border-top: 1px solid var(--color-border);
          }

          .button-primary {
            background: var(--gold-primary, #D4AF37);
            color: var(--color-background);
            border: none;
            padding: 0.5rem 1rem;
            border-radius: 4px;
            cursor: pointer;
            font-size: 1rem;
          }

          .button-primary:disabled {
            opacity: 0.6;
            cursor: not-allowed;
          }

          .button-secondary {
            background: var(--color-surface);
            border: 1px solid var(--color-border);
            color: var(--color-text);
            padding: 0.5rem 1rem;
            border-radius: 4px;
            cursor: pointer;
            font-size: 1rem;
          }

          .button-secondary:disabled {
            opacity: 0.6;
            cursor: not-allowed;
          }
        `}</style>
      </div>
    </div>
  )
}

export default SendPOEmailModal
