--
-- Migration: Mobile Detection User Preferences
-- Date: 2025-12-03
-- Task ID: MOBILE-001
-- Description: Adds table for storing user site preferences (desktop/mobile)
--              for the mobile redirect system.
--

-- ============================================================================
-- 1. Create ENUM Type for Site Preference
-- ============================================================================

DO $$ BEGIN
    CREATE TYPE site_preference AS ENUM ('Auto', 'Desktop', 'Mobile');
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

-- ============================================================================
-- 2. User Site Preferences Table
-- ============================================================================
-- Stores user preferences for site version (desktop vs mobile)
CREATE TABLE IF NOT EXISTS public.user_site_preferences (
    id BIGSERIAL PRIMARY KEY,
    user_id INTEGER,
    session_id VARCHAR(255),
    preferred_site site_preference NOT NULL DEFAULT 'Auto',
    last_detected_device VARCHAR(500),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_user_site_preferences_user
        FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE,
    CONSTRAINT chk_user_or_session
        CHECK (user_id IS NOT NULL OR session_id IS NOT NULL)
);

-- ============================================================================
-- 3. Indexes for User Site Preferences
-- ============================================================================

-- Index on user_id for authenticated user lookups
CREATE INDEX IF NOT EXISTS idx_user_site_preferences_user_id
    ON public.user_site_preferences(user_id)
    WHERE user_id IS NOT NULL;

-- Index on session_id for anonymous user lookups
CREATE INDEX IF NOT EXISTS idx_user_site_preferences_session_id
    ON public.user_site_preferences(session_id)
    WHERE session_id IS NOT NULL;

-- Unique constraint to ensure one preference per user
CREATE UNIQUE INDEX IF NOT EXISTS idx_user_site_preferences_unique_user
    ON public.user_site_preferences(user_id)
    WHERE user_id IS NOT NULL;

-- Unique constraint to ensure one preference per session
CREATE UNIQUE INDEX IF NOT EXISTS idx_user_site_preferences_unique_session
    ON public.user_site_preferences(session_id)
    WHERE session_id IS NOT NULL AND user_id IS NULL;

-- ============================================================================
-- 4. Trigger for Updated Timestamp
-- ============================================================================

CREATE OR REPLACE FUNCTION update_user_site_preferences_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_user_site_preferences_updated_at ON public.user_site_preferences;
CREATE TRIGGER trg_user_site_preferences_updated_at
    BEFORE UPDATE ON public.user_site_preferences
    FOR EACH ROW
    EXECUTE FUNCTION update_user_site_preferences_updated_at();

-- ============================================================================
-- 5. Comments
-- ============================================================================

COMMENT ON TABLE public.user_site_preferences IS 'User preferences for site version (desktop/mobile) routing. Supports both authenticated users (via user_id) and anonymous users (via session_id).';
COMMENT ON COLUMN public.user_site_preferences.user_id IS 'Foreign key to users table for authenticated users. NULL for anonymous users.';
COMMENT ON COLUMN public.user_site_preferences.session_id IS 'Session identifier for anonymous users. Stored in a cookie on the client.';
COMMENT ON COLUMN public.user_site_preferences.preferred_site IS 'User preference: Auto (detect automatically), Desktop (always desktop), Mobile (always mobile)';
COMMENT ON COLUMN public.user_site_preferences.last_detected_device IS 'Last detected device type from User-Agent parsing (e.g., iPhone, Android Tablet, Desktop)';

-- ============================================================================
-- 6. Grant Permissions
-- ============================================================================

GRANT ALL ON TABLE public.user_site_preferences TO postgres;
GRANT USAGE, SELECT ON SEQUENCE public.user_site_preferences_id_seq TO postgres;

-- ============================================================================
-- Migration Complete
-- ============================================================================
