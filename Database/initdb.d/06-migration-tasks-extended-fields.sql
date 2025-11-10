--
-- Migration: Add extended fields to tasks table
-- Date: 2025-11-10
-- Description: Adds assignee_id, is_complete, and due_date columns to tasks table for task management
--

-- Add assignee_id column (nullable, references users)
ALTER TABLE public.tasks
ADD COLUMN IF NOT EXISTS assignee_id INTEGER;

-- Add is_complete column (defaults to false)
ALTER TABLE public.tasks
ADD COLUMN IF NOT EXISTS is_complete BOOLEAN DEFAULT false NOT NULL;

-- Add due_date column (nullable)
ALTER TABLE public.tasks
ADD COLUMN IF NOT EXISTS due_date TIMESTAMP WITH TIME ZONE;

-- Add foreign key constraint for assignee_id
ALTER TABLE public.tasks
ADD CONSTRAINT IF NOT EXISTS fk_tasks_assigneeid
FOREIGN KEY (assignee_id) REFERENCES public.users(id);

-- Add comments to document the fields
COMMENT ON COLUMN public.tasks.assignee_id IS 'User assigned to this task';
COMMENT ON COLUMN public.tasks.is_complete IS 'Whether the task has been completed';
COMMENT ON COLUMN public.tasks.due_date IS 'Due date for the task (optional)';
