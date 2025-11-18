import { useEffect, useMemo, useState } from 'react'
import {
  connectQuickBooks,
  disconnectQuickBooks,
  fetchAccountMappings,
  fetchQboAccounts,
  fetchQuickBooksStatus,
  saveAccountMappings,
  QuickBooksAccountMapping,
  clearAccountingError
} from '../features/accountingSlice'
import AccountingSyncPreferences from '../components/accounting/AccountingSyncPreferences'
import { ToastState } from '../types/toast'
import { useAppDispatch, useAppSelector } from '../hooks'

const CASKR_ACCOUNT_TYPES = [
  { value: 'Cogs', label: 'COGS', description: 'Cost of goods sold for finished products.' },
  { value: 'WorkInProgress', label: 'Work In Progress', description: 'Track spirit still aging or in production.' },
  { value: 'FinishedGoods', label: 'Finished Goods', description: 'Ready-to-ship inventory awaiting sale.' },
  { value: 'RawMaterials', label: 'Raw Materials', description: 'Ingredients and supplies before production.' },
  { value: 'Barrels', label: 'Barrels', description: 'Barrel assets used throughout maturation.' },
  { value: 'Ingredients', label: 'Ingredients', description: 'Flavorings, yeast, and other inputs.' },
  { value: 'Labor', label: 'Labor', description: 'Direct labor expenses tied to production.' },
  { value: 'Overhead', label: 'Overhead', description: 'Utilities and indirect production costs.' }
] as const

type AccountTypeValue = (typeof CASKR_ACCOUNT_TYPES)[number]['value']

type MappingSelection = Record<AccountTypeValue, QuickBooksAccountMapping | undefined>

const createEmptyMappings = (): MappingSelection => {
  return CASKR_ACCOUNT_TYPES.reduce((acc, accountType) => {
    acc[accountType.value] = undefined
    return acc
  }, {} as MappingSelection)
}

const useToast = () => {
  const [toast, setToast] = useState<ToastState | null>(null)

  useEffect(() => {
    if (!toast) return
    const timer = window.setTimeout(() => setToast(null), 4000)
    return () => window.clearTimeout(timer)
  }, [toast])

  const showToast = (next: ToastState) => setToast(next)

  return { toast, showToast }
}

const formatConnectedDate = (date?: string) => {
  if (!date) return '—'
  const parsed = new Date(date)
  if (Number.isNaN(parsed.getTime())) return '—'
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(parsed)
}

function AccountingSettingsPage() {
  const dispatch = useAppDispatch()
  const {
    status,
    statusLoading,
    accounts,
    accountsLoading,
    mappings,
    mappingsLoading,
    savingMappings,
    connecting,
    disconnecting,
    error
  } = useAppSelector(state => state.accounting)
  const companyId = 1
  const [localMappings, setLocalMappings] = useState<MappingSelection>(() => createEmptyMappings())
  const [searchTerms, setSearchTerms] = useState<Partial<Record<AccountTypeValue, string>>>({})
  const { toast, showToast } = useToast()

  const isConnected = Boolean(status?.connected)

  useEffect(() => {
    dispatch(fetchQuickBooksStatus(companyId))
  }, [dispatch, companyId])

  useEffect(() => {
    if (isConnected) {
      dispatch(fetchQboAccounts(companyId))
      dispatch(fetchAccountMappings(companyId))
    }
  }, [dispatch, companyId, isConnected])

  useEffect(() => {
    if (mappings.length === 0) {
      setLocalMappings(createEmptyMappings())
      return
    }

    const next = createEmptyMappings()
    for (const mapping of mappings) {
      if (mapping.caskrAccountType in next) {
        next[mapping.caskrAccountType as AccountTypeValue] = mapping
      }
    }
    setLocalMappings(next)
  }, [mappings])

  useEffect(() => {
    if (!error) return
    showToast({ type: 'error', message: error })
    dispatch(clearAccountingError())
  }, [dispatch, error, showToast])

  const connectionInfo = useMemo(
    () => ({
      label: isConnected ? 'Connected' : 'Disconnected',
      date: formatConnectedDate(status?.connectedAt),
      realmId: status?.realmId ?? '—'
    }),
    [isConnected, status?.connectedAt, status?.realmId]
  )

  const handleConnect = async () => {
    try {
      const { authUrl } = await dispatch(connectQuickBooks(companyId)).unwrap()
      showToast({ type: 'success', message: 'Redirecting to QuickBooks…' })
      window.location.href = authUrl
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to start QuickBooks connection'
      showToast({ type: 'error', message })
    }
  }

  const handleDisconnect = async () => {
    const confirmed = window.confirm('Disconnect QuickBooks? You will need to reconnect to sync data again.')
    if (!confirmed) return

    try {
      await dispatch(disconnectQuickBooks(companyId)).unwrap()
      setLocalMappings(createEmptyMappings())
      showToast({ type: 'success', message: 'QuickBooks connection removed.' })
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to disconnect QuickBooks'
      showToast({ type: 'error', message })
    }
  }

  const handleAccountChange = (accountType: AccountTypeValue, accountId: string) => {
    const selectedAccount = accounts.find(account => account.id === accountId)
    setLocalMappings(prev => ({
      ...prev,
      [accountType]: accountId
        ? {
            caskrAccountType: accountType,
            qboAccountId: accountId,
            qboAccountName: selectedAccount?.name ?? 'Unknown account'
          }
        : undefined
    }))
  }

  const handleClearSelection = (accountType: AccountTypeValue) => {
    setLocalMappings(prev => ({
      ...prev,
      [accountType]: undefined
    }))
  }

  const handleSave = async () => {
    const missingAccountType = CASKR_ACCOUNT_TYPES.find(accountType => !localMappings[accountType.value])
    if (missingAccountType) {
      showToast({ type: 'error', message: `${missingAccountType.label} must be mapped before saving.` })
      return
    }

    const payload = CASKR_ACCOUNT_TYPES.map(accountType => {
      const mapping = localMappings[accountType.value]!
      return {
        caskrAccountType: accountType.value,
        qboAccountId: mapping.qboAccountId,
        qboAccountName: mapping.qboAccountName
      }
    })

    try {
      await dispatch(saveAccountMappings({ companyId, mappings: payload })).unwrap()
      showToast({ type: 'success', message: 'Chart of accounts mappings saved.' })
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unable to save mappings'
      showToast({ type: 'error', message })
    }
  }

  const disabledMappings = !isConnected || accountsLoading || mappingsLoading

  return (
    <section className='content-section accounting-settings'>
      {toast && (
        <div className='toast-container' role='status' aria-live='assertive'>
          <div className={`toast ${toast.type === 'success' ? 'toast-success' : 'toast-error'}`}>
            {toast.message}
          </div>
        </div>
      )}
      <div className='section-header accounting-header'>
        <div>
          <h2 className='section-title'>Accounting Settings</h2>
          <p className='section-subtitle'>Connect QuickBooks Online and map CASKr accounts to your chart of accounts.</p>
        </div>
        <div className='status-group'>
          <span className={`status-pill ${isConnected ? 'connected' : 'disconnected'}`}>{connectionInfo.label}</span>
          <div className='status-meta'>
            <span>Realm ID: {connectionInfo.realmId}</span>
            <span>Connected: {connectionInfo.date}</span>
          </div>
        </div>
      </div>
      {statusLoading && <p className='helper-text info'>Checking QuickBooks connection…</p>}

      <div className='connection-card'>
        <div>
          <h3>QuickBooks Online</h3>
          <p className='helper-text'>Manage authentication for syncing invoices, journal entries, and inventory.</p>
        </div>
        <div className='section-actions'>
          {isConnected ? (
            <button
              type='button'
              className='button-secondary'
              onClick={handleDisconnect}
              disabled={disconnecting}
            >
              {disconnecting ? 'Disconnecting…' : 'Disconnect'}
            </button>
          ) : (
            <button type='button' onClick={handleConnect} disabled={connecting}>
              {connecting ? 'Connecting…' : 'Connect QuickBooks'}
            </button>
          )}
        </div>
      </div>

      <div className='mapping-section'>
        <div className='section-header'>
          <div>
            <h3 className='section-title'>Chart of Accounts Mapping</h3>
            <p className='helper-text'>Select the QuickBooks accounts that correspond to each CASKr account type.</p>
          </div>
          <div className='section-actions'>
            <button
              type='button'
              className='button-secondary'
              onClick={() => {
                if (!isConnected) return
                dispatch(fetchQboAccounts(companyId))
              }}
              disabled={!isConnected || accountsLoading}
            >
              {accountsLoading ? 'Refreshing…' : 'Refresh Accounts'}
            </button>
          </div>
        </div>
        {!isConnected && (
          <p className='helper-text warning'>Connect QuickBooks to edit chart of accounts mappings.</p>
        )}
        {(accountsLoading || mappingsLoading) && (
          <p className='helper-text'>Loading QuickBooks data…</p>
        )}
        <div className='table-container'>
          <table className='table accounting-table'>
            <thead>
              <tr>
                <th>Caskr Account Type</th>
                <th>QuickBooks Account</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {CASKR_ACCOUNT_TYPES.map(accountType => {
                const searchValue = searchTerms[accountType.value] ?? ''
                const searchValueLower = searchValue.toLowerCase()
                const filteredAccounts = searchValueLower
                  ? accounts.filter(account =>
                      `${account.name} ${account.accountType}`.toLowerCase().includes(searchValueLower)
                    )
                  : accounts
                const selected = localMappings[accountType.value]
                return (
                  <tr key={accountType.value}>
                    <td>
                      <div className='account-type'>
                        <strong>{accountType.label}</strong>
                        <p>{accountType.description}</p>
                      </div>
                    </td>
                    <td>
                      <div className='account-selector'>
                        <input
                          type='text'
                          placeholder='Search accounts'
                          value={searchValue}
                          onChange={event =>
                            setSearchTerms(prev => ({ ...prev, [accountType.value]: event.target.value }))
                          }
                          disabled={disabledMappings}
                        />
                        <select
                          value={selected?.qboAccountId ?? ''}
                          onChange={event => handleAccountChange(accountType.value, event.target.value)}
                          disabled={disabledMappings}
                        >
                          <option value=''>Select QuickBooks account</option>
                          {filteredAccounts.map(account => (
                            <option key={account.id} value={account.id}>
                              {account.name} ({account.accountType})
                            </option>
                          ))}
                        </select>
                        {!disabledMappings && filteredAccounts.length === 0 && (
                          <small className='helper-text warning'>No QuickBooks accounts match this search.</small>
                        )}
                      </div>
                    </td>
                    <td>
                      <button
                        type='button'
                        className='button-secondary'
                        onClick={() => handleClearSelection(accountType.value)}
                        disabled={!selected}
                      >
                        Clear
                      </button>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
        <div className='mapping-footer'>
          <button
            type='button'
            onClick={handleSave}
            disabled={disabledMappings || savingMappings}
          >
            {savingMappings ? 'Saving…' : 'Save Mappings'}
          </button>
        </div>
      </div>
      <AccountingSyncPreferences companyId={companyId} isConnected={isConnected} onToast={showToast} />
    </section>
  )
}

export default AccountingSettingsPage
