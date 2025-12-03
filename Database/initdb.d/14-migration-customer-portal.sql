--
-- Migration: Customer Portal Schema
-- Date: 2025-12-03
-- Task ID: PORTAL-001
-- Description: Adds tables for customer/investor portals to view cask investments
--              Separate from main app users for security isolation
--

-- ============================================================================
-- 1. Portal Users Table
-- ============================================================================
-- Separate user table for customer portal (security isolation from main app)
CREATE TABLE IF NOT EXISTS public.portal_users (
    id BIGSERIAL PRIMARY KEY,
    email VARCHAR(200) NOT NULL UNIQUE,
    password_hash VARCHAR(500) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    phone VARCHAR(50),
    company_id INTEGER NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    email_verified BOOLEAN NOT NULL DEFAULT FALSE,
    verification_token VARCHAR(100),
    password_reset_token VARCHAR(100),
    password_reset_token_expires_at TIMESTAMPTZ,
    failed_login_attempts INTEGER NOT NULL DEFAULT 0,
    lockout_until TIMESTAMPTZ,
    last_login_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_portal_users_company
        FOREIGN KEY (company_id) REFERENCES public.company(id)
);

-- Indexes for portal_users
CREATE INDEX IF NOT EXISTS idx_portal_users_company_id_email
    ON public.portal_users(company_id, email);

CREATE INDEX IF NOT EXISTS idx_portal_users_verification_token
    ON public.portal_users(verification_token)
    WHERE verification_token IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_portal_users_password_reset_token
    ON public.portal_users(password_reset_token)
    WHERE password_reset_token IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_portal_users_is_active
    ON public.portal_users(is_active);

-- ============================================================================
-- 2. Cask Ownerships Table
-- ============================================================================
-- Links portal customers to their barrel investments
CREATE TABLE IF NOT EXISTS public.cask_ownerships (
    id BIGSERIAL PRIMARY KEY,
    portal_user_id BIGINT NOT NULL,
    barrel_id INTEGER NOT NULL,
    purchase_date DATE NOT NULL,
    purchase_price DECIMAL(10, 2),
    ownership_percentage DECIMAL(5, 2) NOT NULL DEFAULT 100.00,
    certificate_number VARCHAR(100),
    status VARCHAR(20) NOT NULL DEFAULT 'Active',
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_cask_ownerships_portal_user
        FOREIGN KEY (portal_user_id) REFERENCES public.portal_users(id) ON DELETE CASCADE,
    CONSTRAINT fk_cask_ownerships_barrel
        FOREIGN KEY (barrel_id) REFERENCES public.barrel(id),
    CONSTRAINT chk_cask_ownerships_status
        CHECK (status IN ('Active', 'Matured', 'Bottled', 'Sold')),
    CONSTRAINT chk_cask_ownerships_percentage
        CHECK (ownership_percentage > 0 AND ownership_percentage <= 100),
    CONSTRAINT uq_cask_ownerships_portal_user_barrel
        UNIQUE (portal_user_id, barrel_id)
);

-- Indexes for cask_ownerships
CREATE INDEX IF NOT EXISTS idx_cask_ownerships_portal_user_id
    ON public.cask_ownerships(portal_user_id);

CREATE INDEX IF NOT EXISTS idx_cask_ownerships_barrel_id
    ON public.cask_ownerships(barrel_id);

CREATE INDEX IF NOT EXISTS idx_cask_ownerships_portal_user_barrel
    ON public.cask_ownerships(portal_user_id, barrel_id);

CREATE INDEX IF NOT EXISTS idx_cask_ownerships_status
    ON public.cask_ownerships(status);

CREATE INDEX IF NOT EXISTS idx_cask_ownerships_certificate_number
    ON public.cask_ownerships(certificate_number)
    WHERE certificate_number IS NOT NULL;

-- ============================================================================
-- 3. Portal Access Logs Table
-- ============================================================================
-- Audit trail for security and compliance
CREATE TABLE IF NOT EXISTS public.portal_access_logs (
    id BIGSERIAL PRIMARY KEY,
    portal_user_id BIGINT NOT NULL,
    action VARCHAR(100) NOT NULL,
    resource_type VARCHAR(50),
    resource_id BIGINT,
    ip_address VARCHAR(45),
    user_agent TEXT,
    accessed_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_portal_access_logs_portal_user
        FOREIGN KEY (portal_user_id) REFERENCES public.portal_users(id) ON DELETE CASCADE,
    CONSTRAINT chk_portal_access_logs_action
        CHECK (action IN ('Login', 'Login_Failed', 'Logout', 'View_Barrel', 'View_Dashboard',
                          'Download_Certificate', 'Download_Document', 'View_Document',
                          'Password_Reset_Request', 'Password_Reset_Complete', 'Profile_Update'))
);

-- Indexes for portal_access_logs
CREATE INDEX IF NOT EXISTS idx_portal_access_logs_portal_user_id
    ON public.portal_access_logs(portal_user_id);

CREATE INDEX IF NOT EXISTS idx_portal_access_logs_accessed_at
    ON public.portal_access_logs(accessed_at);

CREATE INDEX IF NOT EXISTS idx_portal_access_logs_action
    ON public.portal_access_logs(action);

-- Composite index for audit queries
CREATE INDEX IF NOT EXISTS idx_portal_access_logs_user_action_time
    ON public.portal_access_logs(portal_user_id, action, accessed_at DESC);

-- ============================================================================
-- 4. Portal Documents Table
-- ============================================================================
-- Stores documents accessible to customers for their cask ownership
CREATE TABLE IF NOT EXISTS public.portal_documents (
    id BIGSERIAL PRIMARY KEY,
    cask_ownership_id BIGINT NOT NULL,
    document_type VARCHAR(50) NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    file_path VARCHAR(500) NOT NULL,
    file_size_bytes BIGINT,
    mime_type VARCHAR(100),
    uploaded_by_user_id INTEGER NOT NULL,
    uploaded_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_portal_documents_cask_ownership
        FOREIGN KEY (cask_ownership_id) REFERENCES public.cask_ownerships(id) ON DELETE CASCADE,
    CONSTRAINT fk_portal_documents_uploaded_by
        FOREIGN KEY (uploaded_by_user_id) REFERENCES public.users(id),
    CONSTRAINT chk_portal_documents_type
        CHECK (document_type IN ('Ownership_Certificate', 'Insurance_Document',
                                  'Maturation_Report', 'Photo', 'Invoice', 'Other'))
);

-- Indexes for portal_documents
CREATE INDEX IF NOT EXISTS idx_portal_documents_cask_ownership_id
    ON public.portal_documents(cask_ownership_id);

CREATE INDEX IF NOT EXISTS idx_portal_documents_document_type
    ON public.portal_documents(document_type);

CREATE INDEX IF NOT EXISTS idx_portal_documents_uploaded_at
    ON public.portal_documents(uploaded_at);

-- ============================================================================
-- 5. Portal Notifications Table
-- ============================================================================
-- In-app notifications for portal users
CREATE TABLE IF NOT EXISTS public.portal_notifications (
    id BIGSERIAL PRIMARY KEY,
    portal_user_id BIGINT NOT NULL,
    notification_type VARCHAR(50) NOT NULL,
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    related_barrel_id INTEGER,
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    sent_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    read_at TIMESTAMPTZ,
    CONSTRAINT fk_portal_notifications_portal_user
        FOREIGN KEY (portal_user_id) REFERENCES public.portal_users(id) ON DELETE CASCADE,
    CONSTRAINT fk_portal_notifications_barrel
        FOREIGN KEY (related_barrel_id) REFERENCES public.barrel(id),
    CONSTRAINT chk_portal_notifications_type
        CHECK (notification_type IN ('Barrel_Milestone', 'Maturation_Update', 'Ready_For_Bottling',
                                      'Document_Available', 'New_Photo', 'Account_Update',
                                      'System_Message'))
);

-- Indexes for portal_notifications
CREATE INDEX IF NOT EXISTS idx_portal_notifications_portal_user_id
    ON public.portal_notifications(portal_user_id);

CREATE INDEX IF NOT EXISTS idx_portal_notifications_is_read
    ON public.portal_notifications(portal_user_id, is_read)
    WHERE is_read = FALSE;

CREATE INDEX IF NOT EXISTS idx_portal_notifications_sent_at
    ON public.portal_notifications(sent_at DESC);

CREATE INDEX IF NOT EXISTS idx_portal_notifications_type
    ON public.portal_notifications(notification_type);

-- ============================================================================
-- Table Comments for Documentation
-- ============================================================================
COMMENT ON TABLE public.portal_users IS 'Customer portal users - separate from main app users for security isolation. Allows external customers to view their cask investments.';
COMMENT ON COLUMN public.portal_users.company_id IS 'Which distillery (company) this customer belongs to';
COMMENT ON COLUMN public.portal_users.password_hash IS 'BCrypt hashed password';
COMMENT ON COLUMN public.portal_users.verification_token IS 'Token for email verification';
COMMENT ON COLUMN public.portal_users.password_reset_token IS 'Token for password reset flow';
COMMENT ON COLUMN public.portal_users.failed_login_attempts IS 'Counter for rate limiting brute force attacks';
COMMENT ON COLUMN public.portal_users.lockout_until IS 'Account locked until this time if too many failed attempts';

COMMENT ON TABLE public.cask_ownerships IS 'Links portal customers to their barrel investments. Supports fractional ownership via ownership_percentage.';
COMMENT ON COLUMN public.cask_ownerships.ownership_percentage IS 'Percentage of barrel owned (allows fractional ownership, e.g., 25.00 = 25%)';
COMMENT ON COLUMN public.cask_ownerships.certificate_number IS 'Unique certificate number for this ownership record';
COMMENT ON COLUMN public.cask_ownerships.status IS 'Active: aging, Matured: ready, Bottled: bottled, Sold: transferred';

COMMENT ON TABLE public.portal_access_logs IS 'Audit trail for all portal user actions - security and compliance logging';
COMMENT ON COLUMN public.portal_access_logs.action IS 'Type of action performed (Login, View_Barrel, Download_Certificate, etc.)';
COMMENT ON COLUMN public.portal_access_logs.resource_type IS 'Type of resource accessed (barrel, document, etc.)';
COMMENT ON COLUMN public.portal_access_logs.resource_id IS 'ID of the specific resource accessed';

COMMENT ON TABLE public.portal_documents IS 'Documents uploaded by distillery staff for customer access';
COMMENT ON COLUMN public.portal_documents.document_type IS 'Category of document (Ownership_Certificate, Insurance_Document, Maturation_Report, Photo)';
COMMENT ON COLUMN public.portal_documents.uploaded_by_user_id IS 'Distillery staff member who uploaded this document';

COMMENT ON TABLE public.portal_notifications IS 'In-app notifications for portal users about their barrel investments';
COMMENT ON COLUMN public.portal_notifications.notification_type IS 'Category of notification (Barrel_Milestone, Maturation_Update, Ready_For_Bottling, etc.)';
