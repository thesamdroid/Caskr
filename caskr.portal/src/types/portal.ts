// Portal User Types
export interface PortalUser {
  id: number
  email: string
  firstName: string
  lastName: string
  companyId: number
  companyName: string
}

export interface PortalAuthState {
  user: PortalUser | null
  token: string | null
  expiresAt: string | null
  isAuthenticated: boolean
  isLoading: boolean
  error: string | null
}

// Auth Request/Response Types
export interface PortalLoginRequest {
  email: string
  password: string
}

export interface PortalLoginResponse {
  accessToken: string
  expiresAt: string
  user: PortalUser
}

export interface PortalRegistrationRequest {
  email: string
  password: string
  firstName: string
  lastName: string
  phone?: string
  companyId: number
}

export interface PortalRegistrationResponse {
  message: string
  userId: number
  email: string
}

export interface PortalForgotPasswordRequest {
  email: string
}

export interface PortalResetPasswordRequest {
  token: string
  newPassword: string
}

// Barrel/Cask Types
export interface CaskOwnership {
  id: number
  portalUserId: number
  barrelId: number
  purchaseDate: string
  purchasePrice?: number
  ownershipPercentage: number
  certificateNumber?: string
  status: CaskOwnershipStatus
  notes?: string
  createdAt: string
  barrel: Barrel
  documents: PortalDocument[]
}

export type CaskOwnershipStatus = 'Active' | 'Matured' | 'Bottled' | 'Sold'

export interface Barrel {
  id: number
  sku: string
  batchId: number
  companyId: number
  rickhouseId: number
  rickhouse?: Rickhouse
  order?: Order
  batch?: Batch
}

export interface Rickhouse {
  id: number
  name: string
  address?: string
}

export interface Order {
  id: number
  name: string
  createdAt: string
  spiritType?: SpiritType
}

export interface Batch {
  id: number
  mashBill?: MashBill
}

export interface MashBill {
  id: number
  name: string
}

export interface SpiritType {
  id: number
  name: string
}

// Document Types
export interface PortalDocument {
  id: number
  caskOwnershipId: number
  documentType: PortalDocumentType
  fileName: string
  filePath: string
  fileSizeBytes?: number
  mimeType?: string
  uploadedAt: string
}

export type PortalDocumentType =
  | 'Ownership_Certificate'
  | 'Insurance_Document'
  | 'Maturation_Report'
  | 'Photo'
  | 'Invoice'
  | 'Other'

// Notification Types
export interface PortalNotification {
  id: number
  portalUserId: number
  notificationType: PortalNotificationType
  title: string
  message: string
  relatedBarrelId?: number
  isRead: boolean
  sentAt: string
  readAt?: string
}

export type PortalNotificationType =
  | 'Barrel_Milestone'
  | 'Maturation_Update'
  | 'Ready_For_Bottling'
  | 'Document_Available'
  | 'New_Photo'
  | 'Account_Update'
  | 'System_Message'

// Company Types
export interface Company {
  id: number
  companyName: string
}
