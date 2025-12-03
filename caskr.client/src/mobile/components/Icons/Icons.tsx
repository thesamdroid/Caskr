/**
 * Shared mobile icons
 *
 * Consolidated SVG icons used across mobile components.
 * All icons accept className and aria-label props for styling and accessibility.
 */

import type { CSSProperties } from 'react'

interface IconProps {
  className?: string
  style?: CSSProperties
  'aria-label'?: string
  'aria-hidden'?: boolean
}

/**
 * Refresh/Sync icon
 */
export function RefreshIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <path d="M17.65 6.35C16.2 4.9 14.21 4 12 4c-4.42 0-7.99 3.58-7.99 8s3.57 8 7.99 8c3.73 0 6.84-2.55 7.73-6h-2.08c-.82 2.33-3.04 4-5.65 4-3.31 0-6-2.69-6-6s2.69-6 6-6c1.66 0 3.14.69 4.22 1.78L13 11h7V4l-2.35 2.35z" />
    </svg>
  )
}

/**
 * Offline/No connection icon
 */
export function OfflineIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <path d="M23.64 7c-.45-.34-4.93-4-11.64-4-1.5 0-2.89.19-4.15.48L18.18 13.8 23.64 7zm-6.6 8.22L3.27 1.44 2 2.72l2.05 2.06C1.91 5.76.59 6.82.36 7L12 21.5l3.07-4.04 3.52 3.52 1.26-1.27-2.81-2.81-.18.18v.04-.04l-.02.02.02-.02z" />
    </svg>
  )
}

/**
 * Barcode scanner icon
 */
export function ScanIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <path d="M4 4h4V2H2v6h2V4zm0 12H2v6h6v-2H4v-4zm16 4h-4v2h6v-6h-2v4zm0-16V2h-6v2h4v4h2V4zM9 9h6v6H9V9zm-2 8h10V7H7v10z" />
    </svg>
  )
}

/**
 * Search/magnifying glass icon
 */
export function SearchIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <path d="M15.5 14h-.79l-.28-.27a6.5 6.5 0 0 0 1.48-5.34c-.47-2.78-2.79-5-5.59-5.34a6.505 6.505 0 0 0-7.27 7.27c.34 2.8 2.56 5.12 5.34 5.59a6.5 6.5 0 0 0 5.34-1.48l.27.28v.79l4.25 4.25c.41.41 1.08.41 1.49 0 .41-.41.41-1.08 0-1.49L15.5 14zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z" />
    </svg>
  )
}

/**
 * Plus/Add icon
 */
export function PlusIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2.5"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <line x1="12" y1="5" x2="12" y2="19" />
      <line x1="5" y1="12" x2="19" y2="12" />
    </svg>
  )
}

/**
 * Close/X icon
 */
export function CloseIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <line x1="18" y1="6" x2="6" y2="18" />
      <line x1="6" y1="6" x2="18" y2="18" />
    </svg>
  )
}

/**
 * Checkmark icon
 */
export function CheckIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2.5"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <polyline points="20 6 9 17 4 12" />
    </svg>
  )
}

/**
 * Trash/Delete icon
 */
export function TrashIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <path d="M3 6h18M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6" />
    </svg>
  )
}

/**
 * Home icon
 */
export function HomeIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <path d="M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z" />
    </svg>
  )
}

/**
 * Tasks/Checklist icon
 */
export function TasksIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-9 14l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z" />
    </svg>
  )
}

/**
 * Barrel icon
 */
export function BarrelIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <path d="M18 6h-2c0-2.21-1.79-4-4-4S8 3.79 8 6H6c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2zm-6-2c1.1 0 2 .9 2 2h-4c0-1.1.9-2 2-2zm6 16H6V8h2v2c0 .55.45 1 1 1s1-.45 1-1V8h4v2c0 .55.45 1 1 1s1-.45 1-1V8h2v12z" />
    </svg>
  )
}

/**
 * More/Ellipsis icon
 */
export function MoreIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <path d="M12 8c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm0 2c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2zm0 6c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2z" />
    </svg>
  )
}

/**
 * Back/Left arrow icon
 */
export function BackIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <path d="M20 11H7.83l5.59-5.59L12 4l-8 8 8 8 1.41-1.41L7.83 13H20v-2z" />
    </svg>
  )
}

/**
 * Settings/Gear icon
 */
export function SettingsIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <path d="M19.14 12.94c.04-.31.06-.63.06-.94 0-.31-.02-.63-.06-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.04.31-.06.63-.06.94s.02.63.06.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z" />
    </svg>
  )
}

/**
 * Orders/Document icon
 */
export function OrdersIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-5 14H7v-2h7v2zm3-4H7v-2h10v2zm0-4H7V7h10v2z" />
    </svg>
  )
}

/**
 * Warehouse icon
 */
export function WarehouseIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <path d="M20 4H4v2h16V4zm1 10v-2l-1-5H4l-1 5v2h1v6h10v-6h4v6h2v-6h1zm-9 4H6v-4h6v4z" />
    </svg>
  )
}

/**
 * Reports/Chart icon
 */
export function ReportsIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <path d="M9 17H7v-7h2v7zm4 0h-2V7h2v10zm4 0h-2v-4h2v4zm2.5 2.1h-15V5h15v14.1zm0-16.1h-15c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h15c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2z" />
    </svg>
  )
}

/**
 * Compliance/Shield check icon
 */
export function ComplianceIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-9 14l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z" />
    </svg>
  )
}

/**
 * Accounting/Dollar icon
 */
export function AccountingIcon({ className, style, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      className={className}
      style={style}
      aria-hidden={props['aria-hidden'] ?? !props['aria-label']}
      aria-label={props['aria-label']}
      role={props['aria-label'] ? 'img' : undefined}
    >
      <path d="M11.8 10.9c-2.27-.59-3-1.2-3-2.15 0-1.09 1.01-1.85 2.7-1.85 1.78 0 2.44.85 2.5 2.1h2.21c-.07-1.72-1.12-3.3-3.21-3.81V3h-3v2.16c-1.94.42-3.5 1.68-3.5 3.61 0 2.31 1.91 3.46 4.7 4.13 2.5.6 3 1.48 3 2.41 0 .69-.49 1.79-2.7 1.79-2.06 0-2.87-.92-2.98-2.1h-2.2c.12 2.19 1.76 3.42 3.68 3.83V21h3v-2.15c1.95-.37 3.5-1.5 3.5-3.55 0-2.84-2.43-3.81-4.7-4.4z" />
    </svg>
  )
}

export default {
  RefreshIcon,
  OfflineIcon,
  ScanIcon,
  SearchIcon,
  PlusIcon,
  CloseIcon,
  CheckIcon,
  TrashIcon,
  HomeIcon,
  TasksIcon,
  BarrelIcon,
  MoreIcon,
  BackIcon,
  SettingsIcon,
  OrdersIcon,
  WarehouseIcon,
  ReportsIcon,
  ComplianceIcon,
  AccountingIcon
}
