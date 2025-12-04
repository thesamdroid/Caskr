import { useState, useEffect } from 'react'
import { useAppSelector } from '../../../hooks'
import { subscriptionApi } from '../../../api/userAdminApi'
import { getSubscriptionBadgeClass } from '../../../types/userAdmin'

interface SubscriptionDetails {
  subscriptionId: string | null
  status: string
  tier: string | null
  startDate: string | null
  endDate: string | null
  cancelAtPeriodEnd: boolean
  invoices: Array<{
    id: string
    date: string
    amount: number
    status: string
  }>
}

function UserSubscriptionTab() {
  const { selectedUser } = useAppSelector(state => state.userAdmin)
  const [details, setDetails] = useState<SubscriptionDetails | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [showExtendModal, setShowExtendModal] = useState(false)
  const [showChangeTierModal, setShowChangeTierModal] = useState(false)

  useEffect(() => {
    if (selectedUser) {
      loadSubscriptionDetails()
    }
  }, [selectedUser])

  const loadSubscriptionDetails = async () => {
    if (!selectedUser) return
    setIsLoading(true)
    try {
      const data = await subscriptionApi.getDetails(selectedUser.id)
      setDetails(data)
    } catch (error) {
      console.error('Failed to load subscription:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleCancelSubscription = async (immediate: boolean) => {
    if (!selectedUser) return
    const reason = prompt(`Reason for ${immediate ? 'immediate' : ''} cancellation:`)
    if (!reason) return

    try {
      await subscriptionApi.cancel(selectedUser.id, reason, immediate)
      loadSubscriptionDetails()
    } catch (error) {
      console.error('Failed to cancel subscription:', error)
    }
  }

  const handleExtendSubscription = async () => {
    if (!selectedUser) return
    const daysStr = prompt('Number of days to extend:')
    if (!daysStr) return
    const days = parseInt(daysStr)
    if (isNaN(days) || days <= 0) {
      alert('Please enter a valid number of days')
      return
    }

    const reason = prompt('Reason for extension:')
    if (!reason) return

    try {
      await subscriptionApi.extend(selectedUser.id, days, reason)
      loadSubscriptionDetails()
      setShowExtendModal(false)
    } catch (error) {
      console.error('Failed to extend subscription:', error)
    }
  }

  const handleGrantFree = async () => {
    if (!selectedUser) return
    const tierIdStr = prompt('Tier ID to grant:')
    if (!tierIdStr) return
    const tierId = parseInt(tierIdStr)

    const monthsStr = prompt('Duration in months:')
    if (!monthsStr) return
    const months = parseInt(monthsStr)

    const reason = prompt('Reason for granting free subscription:')
    if (!reason) return

    try {
      await subscriptionApi.grantFree(selectedUser.id, tierId, months, reason)
      loadSubscriptionDetails()
    } catch (error) {
      console.error('Failed to grant subscription:', error)
    }
  }

  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount / 100)
  }

  if (isLoading) {
    return (
      <div className="user-subscription-tab">
        <div className="loading-state">
          <div className="loading-spinner" />
          <p>Loading subscription details...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="user-subscription-tab">
      <div className="tab-header">
        <h3>Subscription</h3>
      </div>

      {!details?.subscriptionId ? (
        <div className="no-subscription">
          <div className="no-subscription-icon">&#x1F6AB;</div>
          <p>This user does not have an active subscription.</p>
          <button
            type="button"
            className="btn btn-primary"
            onClick={handleGrantFree}
          >
            Grant Free Subscription
          </button>
        </div>
      ) : (
        <>
          {/* Current Subscription */}
          <div className="subscription-card">
            <div className="subscription-header">
              <div className="subscription-tier">
                <h4>{details.tier || 'Unknown Tier'}</h4>
                <span className={`badge ${getSubscriptionBadgeClass(selectedUser?.subscriptionStatus || 'None')}`}>
                  {details.status}
                </span>
              </div>
              {details.cancelAtPeriodEnd && (
                <div className="cancel-notice">
                  Will cancel at end of period
                </div>
              )}
            </div>

            <div className="subscription-details">
              <div className="detail-row">
                <span className="label">Subscription ID</span>
                <span className="value mono">{details.subscriptionId}</span>
              </div>
              <div className="detail-row">
                <span className="label">Start Date</span>
                <span className="value">
                  {details.startDate ? new Date(details.startDate).toLocaleDateString() : '-'}
                </span>
              </div>
              <div className="detail-row">
                <span className="label">End/Renewal Date</span>
                <span className="value">
                  {details.endDate ? new Date(details.endDate).toLocaleDateString() : '-'}
                </span>
              </div>
            </div>

            <div className="subscription-actions">
              <button
                type="button"
                className="btn btn-secondary btn-small"
                onClick={() => setShowChangeTierModal(true)}
              >
                Change Tier
              </button>
              <button
                type="button"
                className="btn btn-secondary btn-small"
                onClick={handleExtendSubscription}
              >
                Extend
              </button>
              {!details.cancelAtPeriodEnd && (
                <button
                  type="button"
                  className="btn btn-warning btn-small"
                  onClick={() => handleCancelSubscription(false)}
                >
                  Cancel at Period End
                </button>
              )}
              <button
                type="button"
                className="btn btn-danger btn-small"
                onClick={() => handleCancelSubscription(true)}
              >
                Cancel Immediately
              </button>
            </div>
          </div>

          {/* Invoices */}
          <div className="invoices-section">
            <h4>Recent Invoices</h4>
            {details.invoices.length === 0 ? (
              <p className="no-invoices">No invoices found.</p>
            ) : (
              <table className="invoices-table">
                <thead>
                  <tr>
                    <th>Date</th>
                    <th>Amount</th>
                    <th>Status</th>
                    <th>Invoice ID</th>
                  </tr>
                </thead>
                <tbody>
                  {details.invoices.map(invoice => (
                    <tr key={invoice.id}>
                      <td>{new Date(invoice.date).toLocaleDateString()}</td>
                      <td>{formatCurrency(invoice.amount)}</td>
                      <td>
                        <span className={`badge ${
                          invoice.status === 'paid' ? 'badge-success' :
                          invoice.status === 'open' ? 'badge-warning' :
                          'badge-secondary'
                        }`}>
                          {invoice.status}
                        </span>
                      </td>
                      <td className="mono">{invoice.id}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </>
      )}
    </div>
  )
}

export default UserSubscriptionTab
