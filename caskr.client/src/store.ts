import { configureStore } from '@reduxjs/toolkit'
import ordersReducer from './features/ordersSlice'
import statusReducer from './features/statusSlice'
import productsReducer from './features/productsSlice'
import usersReducer from './features/usersSlice'
import userTypesReducer from './features/userTypesSlice'
import spiritTypesReducer from './features/spiritTypesSlice'
import mashBillsReducer from './features/mashBillsSlice'
import barrelsReducer from './features/barrelsSlice'
import accountingReducer from './features/accountingSlice'
import ttbReportsReducer from './features/ttbReportsSlice'
import ttbTransactionsReducer from './features/ttbTransactionsSlice'
import ttbGaugeRecordsReducer from './features/ttbGaugeRecordsSlice'
import authReducer from './features/authSlice'
import reportsReducer from './features/reportsSlice'
import warehousesReducer from './features/warehousesSlice'

export const store = configureStore({
  reducer: {
    orders: ordersReducer,
    statuses: statusReducer,
    products: productsReducer,
    users: usersReducer,
    userTypes: userTypesReducer,
    spiritTypes: spiritTypesReducer,
    mashBills: mashBillsReducer,
    barrels: barrelsReducer,
    accounting: accountingReducer,
    ttbReports: ttbReportsReducer,
    ttbTransactions: ttbTransactionsReducer,
    ttbGaugeRecords: ttbGaugeRecordsReducer,
    auth: authReducer,
    reports: reportsReducer,
    warehouses: warehousesReducer
  }
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch

