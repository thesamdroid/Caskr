export interface AuthUser {
  id: number
  name: string
  email: string
  companyId: number
  companyName: string
  userTypeId: number
  role: string
  permissions: string[]
}

export interface LoginResponseDto {
  token: string
  refreshToken: string
  expiresAt: string
  user: AuthUser
}

export interface LoginRequestDto {
  email: string
  password: string
}
