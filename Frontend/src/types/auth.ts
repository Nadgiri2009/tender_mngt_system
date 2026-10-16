export interface AuthUser {
  userId: number;
  fullName: string;
  email: string;
  role: string;
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
  departmentId: number | null;
}

export interface LoginInput {
  email: string;
  password: string;
}

// Mirrors Backend RegisterRequestDto exactly
export interface RegisterInput {
  fullName: string;
  companyName: string;
  address?: string;
  email: string;
  mobile: string;
  password: string;
  termsAccepted: boolean;
  roleId: number;
  departmentId?: number;
}

export interface Role {
  roleId: number;
  roleName: string;
  description?: string | null;
}

// Real backend roles (Domain.Enums.RoleNames) -- not the invented 6-role list
// from the original spec. These are what the backend actually enforces via
// [Authorize(Roles = ...)].
export const ROLE_NAMES = {
  Admin: "Admin",
  DepartmentOfficer: "DepartmentOfficer",
  TechnicalSanctionOfficer: "TechnicalSanctionOfficer",
  TenderSection: "TenderSection",
  GBOfficer: "GBOfficer",
  DPDCOfficer: "DPDCOfficer",
  CAFOOfficer: "CAFOOfficer",
  MCAOfficer: "MCAOfficer",
  TenderCommittee: "TenderCommittee",
  CompetentAuthority: "CompetentAuthority",
  Contractor: "Contractor",
} as const;
