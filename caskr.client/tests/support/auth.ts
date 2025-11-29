import type { Page } from '@playwright/test'

const defaultAuthUser = {
  id: 1,
  name: 'Compliance User',
  email: 'compliance@example.com',
  companyId: 1,
  companyName: 'CASKr Demo Distilling',
  userTypeId: 1,
  role: 'Compliance Manager',
  permissions: ['TTB_COMPLIANCE']
}

export const seedAuthenticatedUser = async (page: Page, overrides: Partial<typeof defaultAuthUser> = {}) => {
  const user = { ...defaultAuthUser, ...overrides }
  await page.addInitScript(initialUser => {
    localStorage.setItem('token', 'playwright-token')
    localStorage.setItem('refreshToken', 'playwright-refresh')
    localStorage.setItem('auth.user', JSON.stringify(initialUser))
    localStorage.setItem('auth.expiresAt', new Date().toISOString())
  }, user)
}
