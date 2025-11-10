--
-- Migration: Add Keycloak and audit fields to users table
-- Date: 2025-11-10
-- Description: Adds keycloak_user_id, is_active, created_at, and last_login_at columns to users table
--

-- Add keycloak_user_id column (nullable, for Keycloak integration)
ALTER TABLE public.users
ADD COLUMN IF NOT EXISTS keycloak_user_id VARCHAR(100);

-- Add is_active column (defaults to true)
ALTER TABLE public.users
ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT true NOT NULL;

-- Add created_at column (defaults to current timestamp)
ALTER TABLE public.users
ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL;

-- Add last_login_at column (nullable, tracks last login time)
ALTER TABLE public.users
ADD COLUMN IF NOT EXISTS last_login_at TIMESTAMP WITH TIME ZONE;

-- Create index on keycloak_user_id for faster lookups
CREATE INDEX IF NOT EXISTS idx_users_keycloak_user_id ON public.users(keycloak_user_id);

-- Create index on email for faster lookups (used in login queries)
CREATE INDEX IF NOT EXISTS idx_users_email ON public.users(email);

-- Create index on is_active for filtering active users
CREATE INDEX IF NOT EXISTS idx_users_is_active ON public.users(is_active);

-- Add comment to document the migration
COMMENT ON COLUMN public.users.keycloak_user_id IS 'External Keycloak user identifier for SSO integration';
COMMENT ON COLUMN public.users.is_active IS 'Indicates whether the user account is active';
COMMENT ON COLUMN public.users.created_at IS 'Timestamp when the user was created';
COMMENT ON COLUMN public.users.last_login_at IS 'Timestamp of the user''s last login';
