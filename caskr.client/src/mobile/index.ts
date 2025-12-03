// Mobile App Shell
export { MobileAppShell } from './MobileAppShell'

// Mobile Router
export { MobileRouter } from './MobileRouter'

// Components
export { BottomNav, defaultNavItems } from './components/BottomNav'
export type { BottomNavProps, NavItem } from './components/BottomNav'

export { MobileHeader } from './components/MobileHeader'
export type { MobileHeaderProps } from './components/MobileHeader'

export { DrawerMenu } from './components/DrawerMenu'
export type { DrawerMenuProps, DrawerMenuSection, DrawerMenuItem, UserProfile } from './components/DrawerMenu'

// Hooks
export {
  useSafeArea,
  useBottomNavHeight,
  useHeaderCollapse,
  useDrawer
} from './hooks'
export type {
  SafeAreaInsets,
  HeaderCollapseState,
  UseHeaderCollapseOptions,
  UseDrawerOptions,
  UseDrawerReturn
} from './hooks'
