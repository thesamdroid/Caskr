import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '../hooks'
import { fetchBarrelDetail, clearSelectedOwnership } from '../features/barrelsSlice'
import { documentsApi } from '../api/portalApi'
import { parseISO, differenceInYears, differenceInMonths } from 'date-fns'

function BarrelDetailPage() {
  const { id } = useParams<{ id: string }>()
  const dispatch = useAppDispatch()
  const { selectedOwnership: ownership, isLoading, error } = useAppSelector(state => state.barrels)
  const [downloadingDoc, setDownloadingDoc] = useState<number | null>(null)

  useEffect(() => {
    if (id) {
      dispatch(fetchBarrelDetail(parseInt(id)))
    }
    return () => {
      dispatch(clearSelectedOwnership())
    }
  }, [id, dispatch])

  const handleDownloadDocument = async (documentId: number, fileName: string) => {
    try {
      setDownloadingDoc(documentId)
      const blob = await documentsApi.downloadDocument(documentId)
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = fileName
      document.body.appendChild(a)
      a.click()
      window.URL.revokeObjectURL(url)
      document.body.removeChild(a)
    } catch (err) {
      console.error('Download failed:', err)
      alert('Failed to download document. Please try again.')
    } finally {
      setDownloadingDoc(null)
    }
  }

  const getDocumentIcon = (type: string) => {
    switch (type) {
      case 'Ownership_Certificate':
        return '&#x1F4DC;'
      case 'Insurance_Document':
        return '&#x1F4CB;'
      case 'Maturation_Report':
        return '&#x1F4CA;'
      case 'Photo':
        return '&#x1F4F7;'
      case 'Invoice':
        return '&#x1F4B5;'
      default:
        return '&#x1F4C4;'
    }
  }

  const formatDocumentType = (type: string) => {
    return type.replace(/_/g, ' ')
  }

  const getMaturationProgress = () => {
    if (!ownership) return 0
    const purchaseDate = parseISO(ownership.purchaseDate)
    const now = new Date()
    const monthsAged = differenceInMonths(now, purchaseDate)
    const targetMonths = 60 // 5 years default
    return Math.min((monthsAged / targetMonths) * 100, 100)
  }

  const getAgeDisplay = () => {
    if (!ownership) return 'Unknown'
    const purchaseDate = parseISO(ownership.purchaseDate)
    const now = new Date()
    const years = differenceInYears(now, purchaseDate)
    const months = differenceInMonths(now, purchaseDate) % 12

    if (years > 0) {
      return `${years} year${years !== 1 ? 's' : ''}, ${months} month${months !== 1 ? 's' : ''}`
    }
    return `${months} month${months !== 1 ? 's' : ''}`
  }

  if (isLoading) {
    return (
      <div className="barrel-detail-page">
        <div className="loading-state">
          <div className="loading-spinner" />
          <p>Loading barrel details...</p>
        </div>
      </div>
    )
  }

  if (error || !ownership) {
    return (
      <div className="barrel-detail-page">
        <div className="error-state">
          <p>{error || 'Barrel not found'}</p>
          <Link to="/dashboard" className="btn btn-secondary">
            Back to Dashboard
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="barrel-detail-page">
      <div className="page-header">
        <Link to="/dashboard" className="back-link">
          &larr; Back to My Barrels
        </Link>
        <div className="header-content">
          <div>
            <h1>{ownership.barrel?.sku || 'Barrel Details'}</h1>
            {ownership.certificateNumber && (
              <p className="certificate-number">Certificate: {ownership.certificateNumber}</p>
            )}
          </div>
          <span
            className={`badge badge-lg ${
              ownership.status === 'Active'
                ? 'badge-success'
                : ownership.status === 'Matured'
                ? 'badge-info'
                : 'badge-primary'
            }`}
          >
            {ownership.status}
          </span>
        </div>
      </div>

      <div className="detail-grid">
        {/* Maturation Progress */}
        <div className="detail-card maturation-card">
          <h2>Maturation Progress</h2>
          <div className="maturation-info">
            <div className="age-display">
              <span className="age-value">{getAgeDisplay()}</span>
              <span className="age-label">in barrel</span>
            </div>
            <div className="progress-bar">
              <div
                className="progress-fill"
                style={{ width: `${getMaturationProgress()}%` }}
              />
            </div>
            <span className="progress-label">
              {Math.round(getMaturationProgress())}% towards 5-year maturation
            </span>
          </div>
        </div>

        {/* Barrel Details */}
        <div className="detail-card">
          <h2>Barrel Details</h2>
          <dl className="detail-list">
            <div className="detail-item">
              <dt>SKU</dt>
              <dd>{ownership.barrel?.sku || '-'}</dd>
            </div>
            <div className="detail-item">
              <dt>Purchase Date</dt>
              <dd>{new Date(ownership.purchaseDate).toLocaleDateString()}</dd>
            </div>
            {ownership.purchasePrice && (
              <div className="detail-item">
                <dt>Purchase Price</dt>
                <dd>${ownership.purchasePrice.toLocaleString()}</dd>
              </div>
            )}
            <div className="detail-item">
              <dt>Ownership</dt>
              <dd>{ownership.ownershipPercentage}%</dd>
            </div>
            {ownership.barrel?.rickhouse && (
              <div className="detail-item">
                <dt>Location</dt>
                <dd>{ownership.barrel.rickhouse.name}</dd>
              </div>
            )}
            {ownership.barrel?.batch?.mashBill && (
              <div className="detail-item">
                <dt>Mash Bill</dt>
                <dd>{ownership.barrel.batch.mashBill.name}</dd>
              </div>
            )}
          </dl>
        </div>

        {/* Notes */}
        {ownership.notes && (
          <div className="detail-card">
            <h2>Notes</h2>
            <p className="notes-content">{ownership.notes}</p>
          </div>
        )}

        {/* Documents */}
        <div className="detail-card documents-card">
          <h2>Documents</h2>
          {ownership.documents && ownership.documents.length > 0 ? (
            <ul className="documents-list">
              {ownership.documents.map(doc => (
                <li key={doc.id} className="document-item">
                  <div className="document-info">
                    <span
                      className="document-icon"
                      dangerouslySetInnerHTML={{ __html: getDocumentIcon(doc.documentType) }}
                    />
                    <div className="document-details">
                      <span className="document-name">{doc.fileName}</span>
                      <span className="document-type">{formatDocumentType(doc.documentType)}</span>
                    </div>
                  </div>
                  <button
                    onClick={() => handleDownloadDocument(doc.id, doc.fileName)}
                    disabled={downloadingDoc === doc.id}
                    className="btn btn-small btn-secondary"
                  >
                    {downloadingDoc === doc.id ? 'Downloading...' : 'Download'}
                  </button>
                </li>
              ))}
            </ul>
          ) : (
            <p className="no-documents">No documents available yet.</p>
          )}
        </div>
      </div>

      {/* Contact Form CTA */}
      <div className="contact-cta">
        <h2>Have Questions?</h2>
        <p>Contact your distillery for more information about your barrel.</p>
        <a href="mailto:info@example.com" className="btn btn-primary">
          Contact Distillery
        </a>
      </div>
    </div>
  )
}

export default BarrelDetailPage
