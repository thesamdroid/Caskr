import { createSlice, createAsyncThunk, type PayloadAction } from '@reduxjs/toolkit'
import type {
  UserListItem,
  UserDetail,
  UserAuditLog,
  UserSession,
  UserNote,
  UserSearchFilters,
  SuperAdminAccessCheck,
  ImpersonationSession,
  UserEditFormData,
  SuspendUserFormData,
  DeleteUserFormData,
  ImpersonateUserFormData,
  UserNoteFormData
} from '../types/userAdmin'
import { defaultUserSearchFilters } from '../types/userAdmin'
import {
  accessApi,
  usersApi,
  accountOpsApi,
  impersonationApi,
  sessionsApi,
  userAuditApi,
  notesApi
} from '../api/userAdminApi'

interface UserAdminState {
  // Access control
  accessChecked: boolean
  accessAllowed: boolean
  requiresReAuth: boolean
  reAuthExpiresAt: string | null

  // User list
  users: UserListItem[]
  totalUsers: number
  totalPages: number
  filters: UserSearchFilters
  isLoadingList: boolean

  // Selected user
  selectedUser: UserDetail | null
  selectedUserAuditLogs: UserAuditLog[]
  selectedUserSessions: UserSession[]
  selectedUserNotes: UserNote[]
  isLoadingUser: boolean

  // Impersonation
  impersonationSession: ImpersonationSession | null
  isImpersonating: boolean

  // UI state
  activeTab: 'details' | 'activity' | 'sessions' | 'subscription' | 'notes'
  showReAuthModal: boolean
  showSuspendModal: boolean
  showDeleteModal: boolean
  showImpersonateModal: boolean
  showEditModal: boolean

  // Error handling
  error: string | null
  operationSuccess: string | null
}

const initialState: UserAdminState = {
  accessChecked: false,
  accessAllowed: false,
  requiresReAuth: false,
  reAuthExpiresAt: null,

  users: [],
  totalUsers: 0,
  totalPages: 0,
  filters: defaultUserSearchFilters,
  isLoadingList: false,

  selectedUser: null,
  selectedUserAuditLogs: [],
  selectedUserSessions: [],
  selectedUserNotes: [],
  isLoadingUser: false,

  impersonationSession: null,
  isImpersonating: false,

  activeTab: 'details',
  showReAuthModal: false,
  showSuspendModal: false,
  showDeleteModal: false,
  showImpersonateModal: false,
  showEditModal: false,

  error: null,
  operationSuccess: null
}

// Async thunks
export const checkAccess = createAsyncThunk(
  'userAdmin/checkAccess',
  async () => {
    const result = await accessApi.checkAccess()
    return result
  }
)

export const reAuthenticate = createAsyncThunk(
  'userAdmin/reAuthenticate',
  async (password: string) => {
    const result = await accessApi.reAuthenticate(password)
    sessionStorage.setItem('superAdminReAuthToken', result.token)
    return result
  }
)

export const fetchUsers = createAsyncThunk(
  'userAdmin/fetchUsers',
  async (filters: Partial<UserSearchFilters>) => {
    const result = await usersApi.list(filters)
    return result
  }
)

export const fetchUserById = createAsyncThunk(
  'userAdmin/fetchUserById',
  async (userId: string) => {
    const user = await usersApi.getById(userId)
    return user
  }
)

export const updateUser = createAsyncThunk(
  'userAdmin/updateUser',
  async ({ userId, data }: { userId: string; data: UserEditFormData }) => {
    const result = await usersApi.update(userId, data)
    return result
  }
)

export const resetUserPassword = createAsyncThunk(
  'userAdmin/resetUserPassword',
  async (userId: string) => {
    const result = await usersApi.resetPassword(userId)
    return result
  }
)

export const forceLogoutUser = createAsyncThunk(
  'userAdmin/forceLogoutUser',
  async (userId: string) => {
    await usersApi.forceLogout(userId)
    return userId
  }
)

export const unlockUserAccount = createAsyncThunk(
  'userAdmin/unlockUserAccount',
  async (userId: string) => {
    await usersApi.unlockAccount(userId)
    return userId
  }
)

export const suspendUser = createAsyncThunk(
  'userAdmin/suspendUser',
  async ({ userId, data }: { userId: string; data: SuspendUserFormData }) => {
    await accountOpsApi.suspend(userId, data)
    return userId
  }
)

export const unsuspendUser = createAsyncThunk(
  'userAdmin/unsuspendUser',
  async ({ userId, reason }: { userId: string; reason: string }) => {
    await accountOpsApi.unsuspend(userId, reason)
    return userId
  }
)

export const deleteUser = createAsyncThunk(
  'userAdmin/deleteUser',
  async ({ userId, data }: { userId: string; data: DeleteUserFormData }) => {
    await accountOpsApi.delete(userId, data)
    return userId
  }
)

export const restoreUser = createAsyncThunk(
  'userAdmin/restoreUser',
  async ({ userId, reason }: { userId: string; reason: string }) => {
    await accountOpsApi.restore(userId, reason)
    return userId
  }
)

export const startImpersonation = createAsyncThunk(
  'userAdmin/startImpersonation',
  async ({ userId, data }: { userId: string; data: ImpersonateUserFormData }) => {
    const session = await impersonationApi.start(userId, data)
    return session
  }
)

export const endImpersonation = createAsyncThunk(
  'userAdmin/endImpersonation',
  async () => {
    await impersonationApi.end()
  }
)

export const fetchUserSessions = createAsyncThunk(
  'userAdmin/fetchUserSessions',
  async (userId: string) => {
    const sessions = await sessionsApi.list(userId)
    return sessions
  }
)

export const terminateSession = createAsyncThunk(
  'userAdmin/terminateSession',
  async ({ userId, sessionId }: { userId: string; sessionId: string }) => {
    await sessionsApi.terminate(userId, sessionId)
    return sessionId
  }
)

export const terminateAllSessions = createAsyncThunk(
  'userAdmin/terminateAllSessions',
  async (userId: string) => {
    await sessionsApi.terminateAll(userId)
    return userId
  }
)

export const fetchUserAuditLogs = createAsyncThunk(
  'userAdmin/fetchUserAuditLogs',
  async ({ userId, limit }: { userId: string; limit?: number }) => {
    const logs = await userAuditApi.list(userId, limit)
    return logs
  }
)

export const fetchUserNotes = createAsyncThunk(
  'userAdmin/fetchUserNotes',
  async (userId: string) => {
    const notes = await notesApi.list(userId)
    return notes
  }
)

export const createUserNote = createAsyncThunk(
  'userAdmin/createUserNote',
  async ({ userId, data }: { userId: string; data: UserNoteFormData }) => {
    const note = await notesApi.create(userId, data)
    return note
  }
)

export const deleteUserNote = createAsyncThunk(
  'userAdmin/deleteUserNote',
  async ({ userId, noteId }: { userId: string; noteId: number }) => {
    await notesApi.delete(userId, noteId)
    return noteId
  }
)

const userAdminSlice = createSlice({
  name: 'userAdmin',
  initialState,
  reducers: {
    setFilters: (state, action: PayloadAction<Partial<UserSearchFilters>>) => {
      state.filters = { ...state.filters, ...action.payload }
    },
    resetFilters: state => {
      state.filters = defaultUserSearchFilters
    },
    setActiveTab: (state, action: PayloadAction<UserAdminState['activeTab']>) => {
      state.activeTab = action.payload
    },
    setShowReAuthModal: (state, action: PayloadAction<boolean>) => {
      state.showReAuthModal = action.payload
    },
    setShowSuspendModal: (state, action: PayloadAction<boolean>) => {
      state.showSuspendModal = action.payload
    },
    setShowDeleteModal: (state, action: PayloadAction<boolean>) => {
      state.showDeleteModal = action.payload
    },
    setShowImpersonateModal: (state, action: PayloadAction<boolean>) => {
      state.showImpersonateModal = action.payload
    },
    setShowEditModal: (state, action: PayloadAction<boolean>) => {
      state.showEditModal = action.payload
    },
    clearSelectedUser: state => {
      state.selectedUser = null
      state.selectedUserAuditLogs = []
      state.selectedUserSessions = []
      state.selectedUserNotes = []
    },
    clearError: state => {
      state.error = null
    },
    clearOperationSuccess: state => {
      state.operationSuccess = null
    }
  },
  extraReducers: builder => {
    // Check access
    builder
      .addCase(checkAccess.pending, state => {
        state.accessChecked = false
      })
      .addCase(checkAccess.fulfilled, (state, action: PayloadAction<SuperAdminAccessCheck>) => {
        state.accessChecked = true
        state.accessAllowed = action.payload.allowed
        state.requiresReAuth = action.payload.requiresReAuth
        state.showReAuthModal = action.payload.requiresReAuth && action.payload.ipAllowed
      })
      .addCase(checkAccess.rejected, state => {
        state.accessChecked = true
        state.accessAllowed = false
      })

    // Re-authenticate
    builder
      .addCase(reAuthenticate.fulfilled, (state, action) => {
        state.requiresReAuth = false
        state.showReAuthModal = false
        state.reAuthExpiresAt = action.payload.expiresAt
        state.accessAllowed = true
      })
      .addCase(reAuthenticate.rejected, (state, action) => {
        state.error = action.error.message || 'Re-authentication failed'
      })

    // Fetch users
    builder
      .addCase(fetchUsers.pending, state => {
        state.isLoadingList = true
        state.error = null
      })
      .addCase(fetchUsers.fulfilled, (state, action) => {
        state.isLoadingList = false
        state.users = action.payload.users
        state.totalUsers = action.payload.total
        state.totalPages = action.payload.totalPages
        state.filters.page = action.payload.page
      })
      .addCase(fetchUsers.rejected, (state, action) => {
        state.isLoadingList = false
        if (action.error.message === 'RE_AUTH_REQUIRED') {
          state.requiresReAuth = true
          state.showReAuthModal = true
        } else {
          state.error = action.error.message || 'Failed to fetch users'
        }
      })

    // Fetch user by ID
    builder
      .addCase(fetchUserById.pending, state => {
        state.isLoadingUser = true
        state.error = null
      })
      .addCase(fetchUserById.fulfilled, (state, action) => {
        state.isLoadingUser = false
        state.selectedUser = action.payload
      })
      .addCase(fetchUserById.rejected, (state, action) => {
        state.isLoadingUser = false
        if (action.error.message === 'RE_AUTH_REQUIRED') {
          state.requiresReAuth = true
          state.showReAuthModal = true
        } else {
          state.error = action.error.message || 'Failed to fetch user'
        }
      })

    // Update user
    builder
      .addCase(updateUser.fulfilled, (state, action) => {
        state.selectedUser = action.payload
        state.showEditModal = false
        state.operationSuccess = 'User updated successfully'
      })
      .addCase(updateUser.rejected, (state, action) => {
        state.error = action.error.message || 'Failed to update user'
      })

    // Reset password
    builder
      .addCase(resetUserPassword.fulfilled, state => {
        state.operationSuccess = 'Password reset email sent'
      })
      .addCase(resetUserPassword.rejected, (state, action) => {
        state.error = action.error.message || 'Failed to reset password'
      })

    // Force logout
    builder
      .addCase(forceLogoutUser.fulfilled, state => {
        state.operationSuccess = 'User logged out from all sessions'
      })
      .addCase(forceLogoutUser.rejected, (state, action) => {
        state.error = action.error.message || 'Failed to force logout'
      })

    // Unlock account
    builder
      .addCase(unlockUserAccount.fulfilled, state => {
        if (state.selectedUser) {
          state.selectedUser.lockedUntil = null
          state.selectedUser.failedLoginAttempts = 0
        }
        state.operationSuccess = 'Account unlocked'
      })
      .addCase(unlockUserAccount.rejected, (state, action) => {
        state.error = action.error.message || 'Failed to unlock account'
      })

    // Suspend user
    builder
      .addCase(suspendUser.fulfilled, state => {
        if (state.selectedUser) {
          state.selectedUser.status = 'Suspended'
        }
        state.showSuspendModal = false
        state.operationSuccess = 'User suspended'
      })
      .addCase(suspendUser.rejected, (state, action) => {
        state.error = action.error.message || 'Failed to suspend user'
      })

    // Unsuspend user
    builder
      .addCase(unsuspendUser.fulfilled, state => {
        if (state.selectedUser) {
          state.selectedUser.status = 'Active'
        }
        state.operationSuccess = 'User reactivated'
      })
      .addCase(unsuspendUser.rejected, (state, action) => {
        state.error = action.error.message || 'Failed to unsuspend user'
      })

    // Delete user
    builder
      .addCase(deleteUser.fulfilled, state => {
        if (state.selectedUser) {
          state.selectedUser.status = 'Deleted'
        }
        state.showDeleteModal = false
        state.operationSuccess = 'User deleted'
      })
      .addCase(deleteUser.rejected, (state, action) => {
        state.error = action.error.message || 'Failed to delete user'
      })

    // Restore user
    builder
      .addCase(restoreUser.fulfilled, state => {
        if (state.selectedUser) {
          state.selectedUser.status = 'Active'
        }
        state.operationSuccess = 'User restored'
      })
      .addCase(restoreUser.rejected, (state, action) => {
        state.error = action.error.message || 'Failed to restore user'
      })

    // Impersonation
    builder
      .addCase(startImpersonation.fulfilled, (state, action) => {
        state.impersonationSession = action.payload
        state.isImpersonating = true
        state.showImpersonateModal = false
      })
      .addCase(startImpersonation.rejected, (state, action) => {
        state.error = action.error.message || 'Failed to start impersonation'
      })
      .addCase(endImpersonation.fulfilled, state => {
        state.impersonationSession = null
        state.isImpersonating = false
      })

    // Sessions
    builder
      .addCase(fetchUserSessions.fulfilled, (state, action) => {
        state.selectedUserSessions = action.payload
      })
      .addCase(terminateSession.fulfilled, (state, action) => {
        state.selectedUserSessions = state.selectedUserSessions.filter(
          s => s.id !== action.payload
        )
        state.operationSuccess = 'Session terminated'
      })
      .addCase(terminateAllSessions.fulfilled, state => {
        state.selectedUserSessions = []
        state.operationSuccess = 'All sessions terminated'
      })

    // Audit logs
    builder.addCase(fetchUserAuditLogs.fulfilled, (state, action) => {
      state.selectedUserAuditLogs = action.payload
    })

    // Notes
    builder
      .addCase(fetchUserNotes.fulfilled, (state, action) => {
        state.selectedUserNotes = action.payload
      })
      .addCase(createUserNote.fulfilled, (state, action) => {
        state.selectedUserNotes.unshift(action.payload)
        state.operationSuccess = 'Note added'
      })
      .addCase(deleteUserNote.fulfilled, (state, action) => {
        state.selectedUserNotes = state.selectedUserNotes.filter(
          n => n.id !== action.payload
        )
        state.operationSuccess = 'Note deleted'
      })
  }
})

export const {
  setFilters,
  resetFilters,
  setActiveTab,
  setShowReAuthModal,
  setShowSuspendModal,
  setShowDeleteModal,
  setShowImpersonateModal,
  setShowEditModal,
  clearSelectedUser,
  clearError,
  clearOperationSuccess
} = userAdminSlice.actions

export default userAdminSlice.reducer
