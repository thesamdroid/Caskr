--
-- Migration: Accounting sync preference last sync timestamp
-- Date: 2025-03-07
-- Description: Adds the last_sync_at column to accounting_sync_preferences
--
ALTER TABLE public.accounting_sync_preferences
    ADD COLUMN IF NOT EXISTS last_sync_at TIMESTAMPTZ NULL;
