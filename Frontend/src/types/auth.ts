export interface AuthUser {
  userId: number;
  fullName: string;
  email: string;
  role: string;
  accountType: string;
  departmentId: number | null;
}

// Mirrors Backend AuthResponseDto exactly
export interface AuthResponse {
  token: string;
  expiresAt: string;
  userId: number;
  fullName: string;
  email: string;
  role: string;
  accountType: string;
  departmentId: number | null;
}

export interface LoginInput {
  mobile: string;
  otp: string;
}

export interface RequestLoginOtpInput {
  mobile: string;
}

export interface RequestLoginOtpResponse {
  message: string;
  developmentOtp?: string | null;
}

// Mirrors Backend RegisterRequestDto exactly
export interface RegisterInput {
  fullName: string;
  companyName: string;
  address?: string;
  email: string;
  mobile: string;
  termsAccepted: boolean;
}

export interface RegisterResponse {
  userId: number;
  message: string;
}

export interface Role {
  roleId: number;
  roleCode: string;
  roleName: string;
  description?: string | null;
}

export const ROLE_NAMES = {
  JuniorEngineer: "Junior Engineer (JE)",
  TenderCell: "Tender Cell",
  TenderCommitteeChairman: "Tender Committee Chairman (Nivida Samiti Adhyaksha)",
  DirectorAuditor: "Director Auditor",
  ExternalBidder: "ExternalBidder",
} as const;
