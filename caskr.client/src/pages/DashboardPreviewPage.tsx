import '../App.css'

const stats = [
  { label: 'Active Orders', value: 6, helper: '+2 week over week', tone: 'success' },
  { label: 'Bottling Queue', value: 3, helper: '2 scheduled today', tone: 'warning' },
  { label: 'Compliance Tasks', value: 4, helper: '1 requires review', tone: 'warning' }
]

const tasks = [
  { title: 'Prepare 5110.28 draft', owner: 'Alexis Reid', due: 'Fri, 9:00 AM', status: 'In progress' },
  { title: 'Validate barrel lot 24B', owner: 'Morgan Silva', due: 'Today, 3:00 PM', status: 'Blocked' },
  { title: 'Sync QuickBooks batches', owner: 'Taylor Fox', due: 'Tomorrow, 10:00 AM', status: 'Scheduled' }
]

export default function DashboardPreviewPage() {
  return (
    <div style={{ padding: '24px' }}>
      <header>
        <p className="tag warning">Preview mode</p>
        <h1 style={{ marginTop: '8px' }}>Dashboard Preview (Sample Data)</h1>
        <p style={{ color: 'var(--text-secondary)' }}>
          Static data to illustrate how the dashboard renders when compliance monitoring and production metrics are healthy.
        </p>
      </header>

      <div className="preview-grid">
        {stats.map(card => (
          <div key={card.label} className="preview-card">
            <h3>{card.label}</h3>
            <p style={{ fontSize: '2rem', fontWeight: 700 }}>{card.value}</p>
            <p className={`tag ${card.tone}`}>{card.helper}</p>
          </div>
        ))}
      </div>

      <div className="preview-card" style={{ marginTop: '24px' }}>
        <h3>High-priority tasks</h3>
        <p style={{ color: 'var(--text-secondary)', marginBottom: '12px' }}>
          Draft generation and production handoffs that are due in the next 24 hours.
        </p>
        <div style={{ display: 'grid', gap: '12px' }}>
          {tasks.map(task => (
            <div
              key={task.title}
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
                <p style={{ fontWeight: 600 }}>{task.title}</p>
                <p style={{ color: 'var(--text-secondary)' }}>Owner: {task.owner}</p>
              </div>
              <div style={{ textAlign: 'right' }}>
                <p className="tag warning" style={{ marginBottom: '6px' }}>{task.status}</p>
                <p style={{ color: 'var(--text-secondary)' }}>{task.due}</p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
