export interface LoginModel {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
}

export interface UserProfile {
  id: number;
  email: string;
}
