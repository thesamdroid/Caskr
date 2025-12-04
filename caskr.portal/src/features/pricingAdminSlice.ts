import { createAsyncThunk, createSlice, PayloadAction } from '@reduxjs/toolkit'
import type {
  PricingTier,
  PricingFeature,
  PricingFaq,
  PricingPromotion,
  PricingAuditLog
} from '../types/pricing'
import {
  tiersApi,
  featuresApi,
  faqsApi,
  promotionsApi,
  auditLogsApi,
  checkAdminAccess
} from '../api/pricingAdminApi'

interface PricingAdminState {
  // Data
  tiers: PricingTier[]
  features: PricingFeature[]
  faqs: PricingFaq[]
  promotions: PricingPromotion[]
  auditLogs: PricingAuditLog[]

  // UI state
  activeTab: 'tiers' | 'features' | 'faqs' | 'promotions' | 'audit'
  isLoading: boolean
  isSaving: boolean
  error: string | null
  hasAdminAccess: boolean | null

  // Editor state
  editingTier: PricingTier | null
  editingFeature: PricingFeature | null
  editingFaq: PricingFaq | null
  editingPromotion: PricingPromotion | null

  // Unsaved changes tracking
  hasUnsavedChanges: boolean
}

const initialState: PricingAdminState = {
  tiers: [],
  features: [],
  faqs: [],
  promotions: [],
  auditLogs: [],
  activeTab: 'tiers',
  isLoading: false,
  isSaving: false,
  error: null,
  hasAdminAccess: null,
  editingTier: null,
  editingFeature: null,
  editingFaq: null,
  editingPromotion: null,
  hasUnsavedChanges: false
}

// Async thunks
export const checkAccess = createAsyncThunk(
  'pricingAdmin/checkAccess',
  async () => {
    return await checkAdminAccess()
  }
)

export const fetchTiers = createAsyncThunk(
  'pricingAdmin/fetchTiers',
  async (_, { rejectWithValue }) => {
    try {
      return await tiersApi.getAll()
    } catch (error) {
      return rejectWithValue(error instanceof Error ? error.message : 'Failed to fetch tiers')
    }
  }
)

export const fetchFeatures = createAsyncThunk(
  'pricingAdmin/fetchFeatures',
  async (_, { rejectWithValue }) => {
    try {
      return await featuresApi.getAll()
    } catch (error) {
      return rejectWithValue(error instanceof Error ? error.message : 'Failed to fetch features')
    }
  }
)

export const fetchFaqs = createAsyncThunk(
  'pricingAdmin/fetchFaqs',
  async (_, { rejectWithValue }) => {
    try {
      return await faqsApi.getAll()
    } catch (error) {
      return rejectWithValue(error instanceof Error ? error.message : 'Failed to fetch FAQs')
    }
  }
)

export const fetchPromotions = createAsyncThunk(
  'pricingAdmin/fetchPromotions',
  async (_, { rejectWithValue }) => {
    try {
      return await promotionsApi.getAll()
    } catch (error) {
      return rejectWithValue(error instanceof Error ? error.message : 'Failed to fetch promotions')
    }
  }
)

export const fetchAuditLogs = createAsyncThunk(
  'pricingAdmin/fetchAuditLogs',
  async (
    params: {
      entityType?: string
      startDate?: string
      endDate?: string
      userId?: number
      limit?: number
    } | undefined,
    { rejectWithValue }
  ) => {
    try {
      return await auditLogsApi.getAll(params)
    } catch (error) {
      return rejectWithValue(error instanceof Error ? error.message : 'Failed to fetch audit logs')
    }
  }
)

export const saveTier = createAsyncThunk(
  'pricingAdmin/saveTier',
  async (tier: Partial<PricingTier>, { rejectWithValue }) => {
    try {
      if (tier.id) {
        return await tiersApi.update(tier.id, tier)
      } else {
        return await tiersApi.create(tier)
      }
    } catch (error) {
      return rejectWithValue(error instanceof Error ? error.message : 'Failed to save tier')
    }
  }
)

export const deleteTier = createAsyncThunk(
  'pricingAdmin/deleteTier',
  async (id: number, { rejectWithValue }) => {
    try {
      await tiersApi.delete(id)
      return id
    } catch (error) {
      return rejectWithValue(error instanceof Error ? error.message : 'Failed to delete tier')
    }
  }
)

export const saveFeature = createAsyncThunk(
  'pricingAdmin/saveFeature',
  async (feature: Partial<PricingFeature>, { rejectWithValue }) => {
    try {
      if (feature.id) {
        return await featuresApi.update(feature.id, feature)
      } else {
        return await featuresApi.create(feature)
      }
    } catch (error) {
      return rejectWithValue(error instanceof Error ? error.message : 'Failed to save feature')
    }
  }
)

export const deleteFeature = createAsyncThunk(
  'pricingAdmin/deleteFeature',
  async (id: number, { rejectWithValue }) => {
    try {
      await featuresApi.delete(id)
      return id
    } catch (error) {
      return rejectWithValue(error instanceof Error ? error.message : 'Failed to delete feature')
    }
  }
)

export const saveFaq = createAsyncThunk(
  'pricingAdmin/saveFaq',
  async (faq: Partial<PricingFaq>, { rejectWithValue }) => {
    try {
      if (faq.id) {
        return await faqsApi.update(faq.id, faq)
      } else {
        return await faqsApi.create(faq)
      }
    } catch (error) {
      return rejectWithValue(error instanceof Error ? error.message : 'Failed to save FAQ')
    }
  }
)

export const deleteFaq = createAsyncThunk(
  'pricingAdmin/deleteFaq',
  async (id: number, { rejectWithValue }) => {
    try {
      await faqsApi.delete(id)
      return id
    } catch (error) {
      return rejectWithValue(error instanceof Error ? error.message : 'Failed to delete FAQ')
    }
  }
)

export const savePromotion = createAsyncThunk(
  'pricingAdmin/savePromotion',
  async (promo: Partial<PricingPromotion>, { rejectWithValue }) => {
    try {
      if (promo.id) {
        return await promotionsApi.update(promo.id, promo)
      } else {
        return await promotionsApi.create(promo)
      }
    } catch (error) {
      return rejectWithValue(error instanceof Error ? error.message : 'Failed to save promotion')
    }
  }
)

export const deletePromotion = createAsyncThunk(
  'pricingAdmin/deletePromotion',
  async (id: number, { rejectWithValue }) => {
    try {
      await promotionsApi.delete(id)
      return id
    } catch (error) {
      return rejectWithValue(error instanceof Error ? error.message : 'Failed to delete promotion')
    }
  }
)

const pricingAdminSlice = createSlice({
  name: 'pricingAdmin',
  initialState,
  reducers: {
    setActiveTab: (state, action: PayloadAction<PricingAdminState['activeTab']>) => {
      state.activeTab = action.payload
    },
    setEditingTier: (state, action: PayloadAction<PricingTier | null>) => {
      state.editingTier = action.payload
    },
    setEditingFeature: (state, action: PayloadAction<PricingFeature | null>) => {
      state.editingFeature = action.payload
    },
    setEditingFaq: (state, action: PayloadAction<PricingFaq | null>) => {
      state.editingFaq = action.payload
    },
    setEditingPromotion: (state, action: PayloadAction<PricingPromotion | null>) => {
      state.editingPromotion = action.payload
    },
    setHasUnsavedChanges: (state, action: PayloadAction<boolean>) => {
      state.hasUnsavedChanges = action.payload
    },
    clearError: (state) => {
      state.error = null
    },
    updateTierLocally: (state, action: PayloadAction<PricingTier>) => {
      const index = state.tiers.findIndex(t => t.id === action.payload.id)
      if (index !== -1) {
        state.tiers[index] = action.payload
      }
      state.hasUnsavedChanges = true
    },
    reorderTiersLocally: (state, action: PayloadAction<number[]>) => {
      const tierMap = new Map(state.tiers.map(t => [t.id, t]))
      state.tiers = action.payload.map((id, index) => {
        const tier = tierMap.get(id)!
        return { ...tier, sortOrder: index }
      })
      state.hasUnsavedChanges = true
    }
  },
  extraReducers: builder => {
    builder
      // Check access
      .addCase(checkAccess.fulfilled, (state, action) => {
        state.hasAdminAccess = action.payload
      })
      .addCase(checkAccess.rejected, state => {
        state.hasAdminAccess = false
      })

      // Fetch tiers
      .addCase(fetchTiers.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(fetchTiers.fulfilled, (state, action) => {
        state.isLoading = false
        state.tiers = action.payload
      })
      .addCase(fetchTiers.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload as string
      })

      // Fetch features
      .addCase(fetchFeatures.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(fetchFeatures.fulfilled, (state, action) => {
        state.isLoading = false
        state.features = action.payload
      })
      .addCase(fetchFeatures.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload as string
      })

      // Fetch FAQs
      .addCase(fetchFaqs.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(fetchFaqs.fulfilled, (state, action) => {
        state.isLoading = false
        state.faqs = action.payload
      })
      .addCase(fetchFaqs.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload as string
      })

      // Fetch promotions
      .addCase(fetchPromotions.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(fetchPromotions.fulfilled, (state, action) => {
        state.isLoading = false
        state.promotions = action.payload
      })
      .addCase(fetchPromotions.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload as string
      })

      // Fetch audit logs
      .addCase(fetchAuditLogs.pending, state => {
        state.isLoading = true
        state.error = null
      })
      .addCase(fetchAuditLogs.fulfilled, (state, action) => {
        state.isLoading = false
        state.auditLogs = action.payload
      })
      .addCase(fetchAuditLogs.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload as string
      })

      // Save tier
      .addCase(saveTier.pending, state => {
        state.isSaving = true
        state.error = null
      })
      .addCase(saveTier.fulfilled, (state, action) => {
        state.isSaving = false
        state.editingTier = null
        const index = state.tiers.findIndex(t => t.id === action.payload.id)
        if (index !== -1) {
          state.tiers[index] = action.payload
        } else {
          state.tiers.push(action.payload)
        }
      })
      .addCase(saveTier.rejected, (state, action) => {
        state.isSaving = false
        state.error = action.payload as string
      })

      // Delete tier
      .addCase(deleteTier.pending, state => {
        state.isSaving = true
        state.error = null
      })
      .addCase(deleteTier.fulfilled, (state, action) => {
        state.isSaving = false
        state.tiers = state.tiers.filter(t => t.id !== action.payload)
      })
      .addCase(deleteTier.rejected, (state, action) => {
        state.isSaving = false
        state.error = action.payload as string
      })

      // Save feature
      .addCase(saveFeature.pending, state => {
        state.isSaving = true
        state.error = null
      })
      .addCase(saveFeature.fulfilled, (state, action) => {
        state.isSaving = false
        state.editingFeature = null
        const index = state.features.findIndex(f => f.id === action.payload.id)
        if (index !== -1) {
          state.features[index] = action.payload
        } else {
          state.features.push(action.payload)
        }
      })
      .addCase(saveFeature.rejected, (state, action) => {
        state.isSaving = false
        state.error = action.payload as string
      })

      // Delete feature
      .addCase(deleteFeature.pending, state => {
        state.isSaving = true
        state.error = null
      })
      .addCase(deleteFeature.fulfilled, (state, action) => {
        state.isSaving = false
        state.features = state.features.filter(f => f.id !== action.payload)
      })
      .addCase(deleteFeature.rejected, (state, action) => {
        state.isSaving = false
        state.error = action.payload as string
      })

      // Save FAQ
      .addCase(saveFaq.pending, state => {
        state.isSaving = true
        state.error = null
      })
      .addCase(saveFaq.fulfilled, (state, action) => {
        state.isSaving = false
        state.editingFaq = null
        const index = state.faqs.findIndex(f => f.id === action.payload.id)
        if (index !== -1) {
          state.faqs[index] = action.payload
        } else {
          state.faqs.push(action.payload)
        }
      })
      .addCase(saveFaq.rejected, (state, action) => {
        state.isSaving = false
        state.error = action.payload as string
      })

      // Delete FAQ
      .addCase(deleteFaq.pending, state => {
        state.isSaving = true
        state.error = null
      })
      .addCase(deleteFaq.fulfilled, (state, action) => {
        state.isSaving = false
        state.faqs = state.faqs.filter(f => f.id !== action.payload)
      })
      .addCase(deleteFaq.rejected, (state, action) => {
        state.isSaving = false
        state.error = action.payload as string
      })

      // Save promotion
      .addCase(savePromotion.pending, state => {
        state.isSaving = true
        state.error = null
      })
      .addCase(savePromotion.fulfilled, (state, action) => {
        state.isSaving = false
        state.editingPromotion = null
        const index = state.promotions.findIndex(p => p.id === action.payload.id)
        if (index !== -1) {
          state.promotions[index] = action.payload
        } else {
          state.promotions.push(action.payload)
        }
      })
      .addCase(savePromotion.rejected, (state, action) => {
        state.isSaving = false
        state.error = action.payload as string
      })

      // Delete promotion
      .addCase(deletePromotion.pending, state => {
        state.isSaving = true
        state.error = null
      })
      .addCase(deletePromotion.fulfilled, (state, action) => {
        state.isSaving = false
        state.promotions = state.promotions.filter(p => p.id !== action.payload)
      })
      .addCase(deletePromotion.rejected, (state, action) => {
        state.isSaving = false
        state.error = action.payload as string
      })
  }
})

export const {
  setActiveTab,
  setEditingTier,
  setEditingFeature,
  setEditingFaq,
  setEditingPromotion,
  setHasUnsavedChanges,
  clearError,
  updateTierLocally,
  reorderTiersLocally
} = pricingAdminSlice.actions

export default pricingAdminSlice.reducer
