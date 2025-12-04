import { useState } from 'react'
import { useAppDispatch, useAppSelector } from '../../../hooks'
import { createUserNote, deleteUserNote } from '../../../features/userAdminSlice'
import type { UserNoteFormData } from '../../../types/userAdmin'
import { formatRelativeTime } from '../../../types/userAdmin'

function UserNotesTab() {
  const dispatch = useAppDispatch()
  const { selectedUser, selectedUserNotes } = useAppSelector(state => state.userAdmin)
  const currentUser = useAppSelector(state => state.auth.user)

  const [showAddNote, setShowAddNote] = useState(false)
  const [noteForm, setNoteForm] = useState<UserNoteFormData>({
    content: '',
    isImportant: false
  })

  const handleSubmitNote = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedUser || !noteForm.content.trim()) return

    try {
      await dispatch(createUserNote({
        userId: selectedUser.id,
        data: noteForm
      })).unwrap()
      setNoteForm({ content: '', isImportant: false })
      setShowAddNote(false)
    } catch (error) {
      console.error('Failed to add note:', error)
    }
  }

  const handleDeleteNote = async (noteId: number) => {
    if (!selectedUser) return
    if (confirm('Delete this note?')) {
      dispatch(deleteUserNote({ userId: selectedUser.id, noteId }))
    }
  }

  return (
    <div className="user-notes-tab">
      <div className="tab-header">
        <h3>Admin Notes</h3>
        <button
          type="button"
          className="btn btn-primary btn-small"
          onClick={() => setShowAddNote(!showAddNote)}
        >
          {showAddNote ? 'Cancel' : 'Add Note'}
        </button>
      </div>

      {/* Add Note Form */}
      {showAddNote && (
        <form className="add-note-form" onSubmit={handleSubmitNote}>
          <div className="form-group">
            <textarea
              value={noteForm.content}
              onChange={e => setNoteForm(prev => ({ ...prev, content: e.target.value }))}
              placeholder="Add a note about this user..."
              rows={3}
              autoFocus
            />
          </div>
          <div className="form-row">
            <label className="checkbox-label">
              <input
                type="checkbox"
                checked={noteForm.isImportant}
                onChange={e => setNoteForm(prev => ({ ...prev, isImportant: e.target.checked }))}
              />
              Mark as important
            </label>
            <button
              type="submit"
              className="btn btn-primary btn-small"
              disabled={!noteForm.content.trim()}
            >
              Save Note
            </button>
          </div>
        </form>
      )}

      {/* Notes List */}
      {selectedUserNotes.length === 0 ? (
        <div className="empty-state">
          <p>No admin notes for this user.</p>
        </div>
      ) : (
        <div className="notes-list">
          {selectedUserNotes.map(note => (
            <div
              key={note.id}
              className={`note-item ${note.isImportant ? 'important' : ''}`}
            >
              <div className="note-header">
                <div className="note-meta">
                  {note.isImportant && (
                    <span className="important-badge" title="Important">
                      &#x26A0;
                    </span>
                  )}
                  <span className="note-author">{note.createdByName}</span>
                  <span className="note-time">
                    {formatRelativeTime(note.createdAt)}
                  </span>
                </div>
                {note.createdBy === currentUser?.id && (
                  <button
                    type="button"
                    className="btn-icon danger"
                    onClick={() => handleDeleteNote(note.id)}
                    title="Delete note"
                  >
                    &times;
                  </button>
                )}
              </div>
              <div className="note-content">
                {note.content}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

export default UserNotesTab
