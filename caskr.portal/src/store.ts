import { configureStore } from '@reduxjs/toolkit'
import authReducer from './features/authSlice'
import barrelsReducer from './features/barrelsSlice'

export const store = configureStore({
  reducer: {
    auth: authReducer,
    barrels: barrelsReducer
  }
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch
