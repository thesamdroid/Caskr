import '../App.css'

const schedules = [
  { company: 'Riverbend DSP', cadence: 'Monthly', nextRun: 'Feb 01, 06:00 UTC', target: 'Jan 2024', status: 'Ready' },
  { company: 'High Desert Spirits', cadence: 'Weekly', nextRun: 'Jan 17, 06:00 UTC', target: 'Dec 2023', status: 'Queued' },
  { company: 'Harbor Gin Collective', cadence: 'Monthly', nextRun: 'Feb 01, 06:00 UTC', target: 'Jan 2024', status: 'Waiting' }
]

const drafts = [
  { company: 'Riverbend DSP', month: '12/2023', status: 'Draft', link: '/ttb-reports/preview/101' },
  { company: 'High Desert Spirits', month: '12/2023', status: 'Draft', link: '/ttb-reports/preview/202' }
]

export default function TtbAutoReportPreviewPage() {
  return (
    <div style={{ padding: '24px' }}>
      <p className="tag warning">Preview mode</p>
      <h1 style={{ marginTop: '8px' }}>TTB Auto-Report Scheduler Preview</h1>
      <p style={{ color: 'var(--text-secondary)' }}>
        Sample data that mirrors the new background generator. Use this view to explain monthly and weekly cadences to compliance officers.
      </p>

      <div className="preview-grid">
        {schedules.map(item => (
          <div key={item.company} className="preview-card">
            <h3>{item.company}</h3>
            <p style={{ color: 'var(--text-secondary)' }}>Cadence: {item.cadence}</p>
            <p className="tag success">Next run: {item.nextRun}</p>
            <p style={{ marginTop: '8px' }}>Target period: {item.target}</p>
            <p className="tag warning" style={{ marginTop: '8px' }}>{item.status}</p>
          </div>
        ))}
      </div>

      <div className="preview-card" style={{ marginTop: '24px' }}>
        <h3>Drafts waiting for review</h3>
        <p style={{ color: 'var(--text-secondary)', marginBottom: '12px' }}>
          Automatically generated Form 5110.28 drafts for the previous month.
        </p>
        <div style={{ display: 'grid', gap: '10px' }}>
          {drafts.map(draft => (
            <div
              key={draft.link}
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                padding: '12px',
                borderRadius: '12px',
                background: 'var(--bg-tertiary)',
                border: '1px solid var(--border-secondary)'
              }}
            >
              <div>
                <p style={{ fontWeight: 600 }}>{draft.company}</p>
                <p style={{ color: 'var(--text-secondary)' }}>Month: {draft.month}</p>
              </div>
              <div style={{ textAlign: 'right' }}>
                <p className="tag success" style={{ marginBottom: '6px' }}>{draft.status}</p>
                <p style={{ color: 'var(--text-secondary)' }}>{draft.link}</p>
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="alert-banner">
        <strong>Error simulation:</strong> Email notification to compliance@harborgin.com failed (mailbox full). The report was saved as Draft and will be retried during the next run.
      </div>
    </div>
  )
}
