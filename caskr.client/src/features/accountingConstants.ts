/**
 * Centralized constants for QuickBooks integration on the frontend.
 * This ensures consistency and makes it easier to update API endpoints.
 */

export const API_ENDPOINTS = {
  STATUS: 'api/accounting/quickbooks/status',
  CONNECT: 'api/accounting/quickbooks/connect',
  DISCONNECT: 'api/accounting/quickbooks/disconnect',
  ACCOUNTS: 'api/accounting/quickbooks/accounts',
  MAPPINGS: 'api/accounting/quickbooks/mappings',
  PREFERENCES: 'api/accounting/quickbooks/preferences',
  TEST_CONNECTION: 'api/accounting/quickbooks/test',
  INVOICE_STATUS: 'api/accounting/quickbooks/invoice-status',
  SYNC_INVOICE: 'api/accounting/quickbooks/sync-invoice',
} as const

export const SYNC_FREQUENCIES = {
  IMMEDIATE: 'Immediate',
  HOURLY: 'Hourly',
  DAILY: 'Daily',
  MANUAL: 'Manual',
} as const

export const SYNC_STATUSES = {
  PENDING: 'Pending',
  IN_PROGRESS: 'InProgress',
  SUCCESS: 'Success',
  FAILED: 'Failed',
} as const

export const ERROR_MESSAGES = {
  CONNECTION_FAILED: 'Failed to connect to QuickBooks. Please try again.',
  DISCONNECTION_FAILED: 'Failed to disconnect QuickBooks. Please try again.',
  LOAD_ACCOUNTS_FAILED: 'Failed to load QuickBooks accounts. Please try again.',
  LOAD_MAPPINGS_FAILED: 'Failed to load account mappings. Please try again.',
  SAVE_MAPPINGS_FAILED: 'Unable to save account mappings. Please try again.',
  LOAD_PREFERENCES_FAILED: 'Failed to load sync preferences. Please try again.',
  SAVE_PREFERENCES_FAILED: 'Unable to save sync preferences. Please try again.',
  LOAD_STATUS_FAILED: 'Unable to load QuickBooks status. Please try again.',
  SYNC_INVOICE_FAILED: 'Failed to sync invoice to QuickBooks. Please try again.',
  LOAD_INVOICE_STATUS_FAILED: 'Failed to load invoice sync status. Please try again.',
  GENERIC_ERROR: 'An unexpected error occurred. Please try again.',
} as const

export const SUCCESS_MESSAGES = {
  CONNECTED: 'Redirecting to QuickBooks…',
  DISCONNECTED: 'QuickBooks connection removed.',
  MAPPINGS_SAVED: 'Chart of accounts mappings saved.',
  PREFERENCES_SAVED: 'Sync preferences saved successfully.',
  INVOICE_SYNCED: 'Invoice synced to QuickBooks successfully.',
} as const

export const ACCOUNT_TYPES = [
  { value: 'Cogs', label: 'COGS', description: 'Cost of goods sold for finished products.' },
  { value: 'WorkInProgress', label: 'Work In Progress', description: 'Track spirit still aging or in production.' },
  { value: 'FinishedGoods', label: 'Finished Goods', description: 'Ready-to-ship inventory awaiting sale.' },
  { value: 'RawMaterials', label: 'Raw Materials', description: 'Ingredients and supplies before production.' },
  { value: 'Barrels', label: 'Barrels', description: 'Barrel assets used throughout maturation.' },
  { value: 'Ingredients', label: 'Ingredients', description: 'Flavorings, yeast, and other inputs.' },
  { value: 'Labor', label: 'Labor', description: 'Direct labor expenses tied to production.' },
  { value: 'Overhead', label: 'Overhead', description: 'Utilities and indirect production costs.' },
] as const
