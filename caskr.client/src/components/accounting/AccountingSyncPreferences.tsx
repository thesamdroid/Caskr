import { useEffect, useMemo, useState } from 'react'
import {
  fetchAccountingSyncPreferences,
  QuickBooksSyncPreferences,
  QuickBooksSyncFrequency,
  saveAccountingSyncPreferences,
  testQuickBooksConnection
} from '../../features/accountingSlice'
import { ToastState } from '../../types/toast'
import { useAppDispatch, useAppSelector } from '../../hooks'

const SYNC_FREQUENCY_OPTIONS: QuickBooksSyncFrequency[] = ['Immediate', 'Hourly', 'Daily', 'Manual']

const createLocalPreferences = (companyId: number): QuickBooksSyncPreferences => ({
  companyId,
  autoSyncInvoices: false,
  autoSyncCogs: false,
  syncFrequency: 'Manual'
})

interface AccountingSyncPreferencesProps {
  companyId: number
  isConnected: boolean
  onToast: (toast: ToastState) => void
}

const AccountingSyncPreferences = ({ companyId, isConnected, onToast }: AccountingSyncPreferencesProps) => {
  const dispatch = useAppDispatch()
  const { preferences, preferencesLoading, savingPreferences, testingConnection } = useAppSelector(
    state => state.accounting
  )
  const [localPreferences, setLocalPreferences] = useState<QuickBooksSyncPreferences>(() =>
    createLocalPreferences(companyId)
  )

  useEffect(() => {
    dispatch(fetchAccountingSyncPreferences(companyId))
  }, [dispatch, companyId])

  useEffect(() => {
    if (preferences) {
      setLocalPreferences(preferences)
      return
    }

    setLocalPreferences(createLocalPreferences(companyId))
  }, [preferences, companyId])

  const isUpdating = preferencesLoading || savingPreferences

  const frequencyDescription = useMemo(() => {
    switch (localPreferences.syncFrequency) {
      case 'Immediate':
        return 'Runs right away whenever orders or costs change.'
      case 'Hourly':
        return 'Bundles updates and syncs every hour.'
      case 'Daily':
        return 'Processes QuickBooks sync once per day overnight.'
      default:
        return 'Requires a manual sync from the accounting dashboard.'
    }
  }, [localPreferences.syncFrequency])

  const handleToggle = (field: 'autoSyncInvoices' | 'autoSyncCogs') => {
    setLocalPreferences(prev => ({
      ...prev,
      [field]: !prev[field]
    }))
  }

  const handleFrequencyChange = (value: QuickBooksSyncFrequency) => {
    setLocalPreferences(prev => ({
      ...prev,
      syncFrequency: value
    }))
  }

  const handleTestConnection = async () => {
    try {
      const result = await dispatch(testQuickBooksConnection(companyId)).unwrap()
      onToast({ type: 'success', message: result.message })
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unable to verify QuickBooks connection'
      onToast({ type: 'error', message })
    }
  }

  const handleSavePreferences = async () => {
    try {
      await dispatch(saveAccountingSyncPreferences(localPreferences)).unwrap()
      onToast({ type: 'success', message: 'Accounting sync preferences saved.' })
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unable to save accounting sync preferences'
      onToast({ type: 'error', message })
    }
  }

  return (
    <div className='accounting-preferences'>
      <div className='section-header'>
        <div>
          <h3 className='section-title'>Sync Preferences</h3>
          <p className='section-subtitle'>Control how CASKr sends invoices and COGS entries to QuickBooks.</p>
        </div>
        <div className='section-actions preferences-actions-inline'>
          <button
            type='button'
            className='button-secondary'
            onClick={handleTestConnection}
            disabled={testingConnection || !isConnected}
          >
            {testingConnection ? 'Testing…' : 'Test Connection'}
          </button>
          <button
            type='button'
            onClick={handleSavePreferences}
            disabled={isUpdating}
          >
            {savingPreferences ? 'Saving…' : 'Save Preferences'}
          </button>
        </div>
      </div>
      {!isConnected && (
        <p className='helper-text warning' role='status'>
          Connect QuickBooks to run automatic syncs. Preferences are saved for later use.
        </p>
      )}
      {preferencesLoading && <p className='helper-text info'>Loading sync preferences…</p>}
      <div className='preference-grid' aria-live='polite'>
        <div className='preference-row'>
          <div className='preference-copy'>
            <label htmlFor='autoSyncInvoices'>Auto-sync invoices</label>
            <p className='helper-text'>Automatically push invoices to QuickBooks when orders are completed.</p>
          </div>
          <div className='preference-control'>
            <input
              id='autoSyncInvoices'
              type='checkbox'
              role='switch'
              aria-checked={localPreferences.autoSyncInvoices}
              checked={localPreferences.autoSyncInvoices}
              onChange={() => handleToggle('autoSyncInvoices')}
              disabled={isUpdating}
            />
            <span>{localPreferences.autoSyncInvoices ? 'Enabled' : 'Disabled'}</span>
          </div>
        </div>
        <div className='preference-row'>
          <div className='preference-copy'>
            <label htmlFor='autoSyncCogs'>Auto-sync COGS</label>
            <p className='helper-text'>Create COGS journal entries when batch processing completes.</p>
          </div>
          <div className='preference-control'>
            <input
              id='autoSyncCogs'
              type='checkbox'
              role='switch'
              aria-checked={localPreferences.autoSyncCogs}
              checked={localPreferences.autoSyncCogs}
              onChange={() => handleToggle('autoSyncCogs')}
              disabled={isUpdating}
            />
            <span>{localPreferences.autoSyncCogs ? 'Enabled' : 'Disabled'}</span>
          </div>
        </div>
        <div className='preference-row'>
          <div className='preference-copy'>
            <label htmlFor='syncFrequency'>Sync frequency</label>
            <p className='helper-text'>{frequencyDescription}</p>
          </div>
          <div className='preference-control'>
            <select
              id='syncFrequency'
              value={localPreferences.syncFrequency}
              onChange={event => handleFrequencyChange(event.target.value as QuickBooksSyncFrequency)}
              disabled={isUpdating}
            >
              {SYNC_FREQUENCY_OPTIONS.map(option => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>
      <div className='preferences-actions'>
        <button
          type='button'
          className='button-secondary'
          onClick={handleTestConnection}
          disabled={testingConnection || !isConnected}
        >
          {testingConnection ? 'Testing…' : 'Test Connection'}
        </button>
        <button type='button' onClick={handleSavePreferences} disabled={isUpdating}>
          {savingPreferences ? 'Saving…' : 'Save Preferences'}
        </button>
      </div>
    </div>
  )
}

export default AccountingSyncPreferences
