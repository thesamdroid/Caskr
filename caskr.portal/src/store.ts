import { configureStore } from '@reduxjs/toolkit'
import authReducer from './features/authSlice'
import barrelsReducer from './features/barrelsSlice'
import pricingAdminReducer from './features/pricingAdminSlice'
import signupReducer from './features/signupSlice'
import userAdminReducer from './features/userAdminSlice'

export const store = configureStore({
  reducer: {
    auth: authReducer,
    barrels: barrelsReducer,
    pricingAdmin: pricingAdminReducer,
    signup: signupReducer,
    userAdmin: userAdminReducer
  }
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch
