import { useEffect } from 'react'
import { Link } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '../hooks'
import { fetchMyBarrels } from '../features/barrelsSlice'
import { formatDistanceToNow, parseISO } from 'date-fns'

function DashboardPage() {
  const dispatch = useAppDispatch()
  const user = useAppSelector(state => state.auth.user)
  const { ownerships, isLoading, error } = useAppSelector(state => state.barrels)

  useEffect(() => {
    dispatch(fetchMyBarrels())
  }, [dispatch])

  const getStatusBadgeClass = (status: string) => {
    switch (status) {
      case 'Active':
        return 'badge-success'
      case 'Matured':
        return 'badge-info'
      case 'Bottled':
        return 'badge-primary'
      case 'Sold':
        return 'badge-secondary'
      default:
        return 'badge-default'
    }
  }

  const calculateAge = (purchaseDate: string) => {
    try {
      return formatDistanceToNow(parseISO(purchaseDate), { addSuffix: false })
    } catch {
      return 'Unknown'
    }
  }

  return (
    <div className="dashboard-page">
      <div className="page-header">
        <h1>Welcome back, {user?.firstName}!</h1>
        <p className="page-subtitle">Here's an overview of your barrel investments</p>
      </div>

      {isLoading && (
        <div className="loading-state">
          <div className="loading-spinner" />
          <p>Loading your barrels...</p>
        </div>
      )}

      {error && (
        <div className="error-state">
          <p>{error}</p>
          <button onClick={() => dispatch(fetchMyBarrels())} className="btn btn-secondary">
            Try Again
          </button>
        </div>
      )}

      {!isLoading && !error && ownerships.length === 0 && (
        <div className="empty-state">
          <div className="empty-icon">&#x1F6A2;</div>
          <h2>No Barrels Yet</h2>
          <p>Contact your distillery to register your barrel investments.</p>
        </div>
      )}

      {!isLoading && !error && ownerships.length > 0 && (
        <>
          <div className="stats-row">
            <div className="stat-card">
              <span className="stat-value">{ownerships.length}</span>
              <span className="stat-label">Total Barrels</span>
            </div>
            <div className="stat-card">
              <span className="stat-value">
                {ownerships.filter(o => o.status === 'Active').length}
              </span>
              <span className="stat-label">Aging</span>
            </div>
            <div className="stat-card">
              <span className="stat-value">
                {ownerships.filter(o => o.status === 'Matured').length}
              </span>
              <span className="stat-label">Matured</span>
            </div>
          </div>

          <div className="barrels-grid">
            {ownerships.map(ownership => (
              <Link
                key={ownership.id}
                to={`/barrels/${ownership.id}`}
                className="barrel-card"
              >
                <div className="barrel-card-header">
                  <span className="barrel-sku">{ownership.barrel?.sku || 'Barrel'}</span>
                  <span className={`badge ${getStatusBadgeClass(ownership.status)}`}>
                    {ownership.status}
                  </span>
                </div>

                <div className="barrel-card-body">
                  <div className="barrel-info">
                    <div className="info-row">
                      <span className="info-label">Purchase Date</span>
                      <span className="info-value">
                        {new Date(ownership.purchaseDate).toLocaleDateString()}
                      </span>
                    </div>
                    <div className="info-row">
                      <span className="info-label">Age</span>
                      <span className="info-value">{calculateAge(ownership.purchaseDate)}</span>
                    </div>
                    {ownership.ownershipPercentage < 100 && (
                      <div className="info-row">
                        <span className="info-label">Ownership</span>
                        <span className="info-value">{ownership.ownershipPercentage}%</span>
                      </div>
                    )}
                    {ownership.barrel?.rickhouse && (
                      <div className="info-row">
                        <span className="info-label">Location</span>
                        <span className="info-value">{ownership.barrel.rickhouse.name}</span>
                      </div>
                    )}
                  </div>
                </div>

                <div className="barrel-card-footer">
                  {ownership.documents && ownership.documents.length > 0 && (
                    <span className="doc-count">
                      {ownership.documents.length} document{ownership.documents.length !== 1 ? 's' : ''}
                    </span>
                  )}
                  <span className="view-link">View Details &rarr;</span>
                </div>
              </Link>
            ))}
          </div>
        </>
      )}
    </div>
  )
}

export default DashboardPage
