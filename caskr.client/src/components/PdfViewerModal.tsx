interface PdfViewerModalProps {
  isOpen: boolean
  title: string
  pdfUrl: string | null
  onClose: () => void
}

export default function PdfViewerModal({ isOpen, title, pdfUrl, onClose }: PdfViewerModalProps) {
  if (!isOpen || !pdfUrl) return null

  return (
    <div className='modal' role='dialog' aria-modal='true' aria-labelledby='pdf-viewer-heading'>
      <div className='modal-content wide'>
        <div className='modal-header'>
          <h3 id='pdf-viewer-heading'>{title}</h3>
          <p className='modal-subtitle'>Preview the generated Form 5110.28 PDF before submission.</p>
        </div>

        <div className='pdf-frame' role='document' aria-label='TTB report preview'>
          <iframe src={pdfUrl} title='TTB report PDF preview' className='pdf-iframe' />
        </div>

        <div className='modal-actions'>
          <button type='button' className='button-secondary' onClick={onClose} aria-label='Close report preview'>
            Close
          </button>
        </div>
      </div>
    </div>
  )
}
