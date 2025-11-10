--
-- Migration: Add description field to user_type table
-- Date: 2025-11-10
-- Description: Adds description field to user_type table for better documentation of user roles
--

-- Add description field
ALTER TABLE public.user_type
ADD COLUMN IF NOT EXISTS description VARCHAR(500);

-- Add comment to document the field
COMMENT ON COLUMN public.user_type.description IS 'Detailed description of the user type and its permissions';
