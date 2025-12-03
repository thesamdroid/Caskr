import { useNavigate } from 'react-router-dom'
import { useSafeArea } from '../../hooks'
import styles from './DrawerMenu.module.css'

export interface DrawerMenuItem {
  id: string
  label: string
  path?: string
  icon?: React.ReactNode
  onClick?: () => void
}

export interface DrawerMenuSection {
  id: string
  title?: string
  items: DrawerMenuItem[]
}

export interface UserProfile {
  name: string
  email?: string
  avatarUrl?: string
  company?: string
}

export interface DrawerMenuProps {
  isOpen: boolean
  onClose: () => void
  drawerRef?: React.RefObject<HTMLDivElement>
  backdropRef?: React.RefObject<HTMLDivElement>
  sections: DrawerMenuSection[]
  userProfile?: UserProfile
  onLogout?: () => void
}

// Default avatar placeholder
const DefaultAvatar = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.defaultAvatar}>
    <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>
  </svg>
)

// Logout icon
const LogoutIcon = () => (
  <svg viewBox="0 0 24 24" fill="currentColor" className={styles.menuIcon}>
    <path d="M17 7l-1.41 1.41L18.17 11H8v2h10.17l-2.58 2.58L17 17l5-5zM4 5h8V3H4c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h8v-2H4V5z"/>
  </svg>
)

/**
 * Slide-out drawer menu for mobile navigation
 */
export function DrawerMenu({
  isOpen,
  onClose,
  drawerRef,
  backdropRef,
  sections,
  userProfile,
  onLogout
}: DrawerMenuProps) {
  const navigate = useNavigate()
  const safeArea = useSafeArea()

  const handleItemClick = (item: DrawerMenuItem) => {
    if (item.onClick) {
      item.onClick()
    } else if (item.path) {
      navigate(item.path)
    }
    onClose()
  }

  const handleLogout = () => {
    onLogout?.()
    onClose()
  }

  return (
    <>
      {/* Backdrop overlay */}
      <div
        ref={backdropRef}
        className={`${styles.backdrop} ${isOpen ? styles.visible : ''}`}
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Drawer panel */}
      <div
        ref={drawerRef}
        className={`${styles.drawer} ${isOpen ? styles.open : ''}`}
        style={{
          paddingTop: safeArea.top,
          paddingBottom: safeArea.bottom
        }}
        role="dialog"
        aria-modal="true"
        aria-label="Navigation menu"
      >
        {/* User profile section */}
        {userProfile && (
          <div className={styles.profileSection}>
            <div className={styles.avatar}>
              {userProfile.avatarUrl ? (
                <img
                  src={userProfile.avatarUrl}
                  alt={`${userProfile.name}'s avatar`}
                  className={styles.avatarImage}
                />
              ) : (
                <DefaultAvatar />
              )}
            </div>
            <div className={styles.profileInfo}>
              <span className={styles.userName}>{userProfile.name}</span>
              {userProfile.company && (
                <span className={styles.userCompany}>{userProfile.company}</span>
              )}
              {userProfile.email && (
                <span className={styles.userEmail}>{userProfile.email}</span>
              )}
            </div>
          </div>
        )}

        {/* Navigation sections */}
        <nav className={styles.navigation}>
          {sections.map((section) => (
            <div key={section.id} className={styles.section}>
              {section.title && (
                <h2 className={styles.sectionTitle}>{section.title}</h2>
              )}
              <ul className={styles.menuList}>
                {section.items.map((item) => (
                  <li key={item.id}>
                    <button
                      type="button"
                      className={styles.menuItem}
                      onClick={() => handleItemClick(item)}
                    >
                      {item.icon && (
                        <span className={styles.menuIconWrapper}>{item.icon}</span>
                      )}
                      <span className={styles.menuLabel}>{item.label}</span>
                    </button>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </nav>

        {/* Logout button */}
        {onLogout && (
          <div className={styles.logoutSection}>
            <button
              type="button"
              className={styles.logoutButton}
              onClick={handleLogout}
            >
              <LogoutIcon />
              <span>Log Out</span>
            </button>
          </div>
        )}
      </div>
    </>
  )
}

export default DrawerMenu
