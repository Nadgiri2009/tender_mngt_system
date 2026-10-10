import { apiClient, toApiError } from "./client";

// Mirrors Backend/.../DTOs/Workflow/WorkflowDtos.cs exactly.

// Workflow transitions mapping - mirrors backend WorkflowEngine
const WORKFLOW_TRANSITIONS: Record<
  string,
  {
    canSubmit: boolean;
    submitRoles?: string[];
    canApprove: boolean;
    approverRoles?: string[];
    canReject: boolean;
    rejectRoles?: string[];
  }
> = {
  Draft: { canSubmit: true, submitRoles: ["DepartmentOfficer"], canApprove: false, canReject: false },
  RequirementSubmitted: { canSubmit: true, submitRoles: ["DepartmentOfficer"], canApprove: false, canReject: false },
  EstimatePrepared: { canSubmit: true, submitRoles: ["DepartmentOfficer"], canApprove: false, canReject: false },
  TSApprovalPending: { canSubmit: false, canApprove: true, approverRoles: ["TechnicalSanctionOfficer"], canReject: true, rejectRoles: ["TechnicalSanctionOfficer"] },
  TSApproved: { canSubmit: true, submitRoles: ["DepartmentOfficer"], canApprove: false, canReject: false },
  GBApprovalPending: { canSubmit: false, canApprove: true, approverRoles: ["GBOfficer"], canReject: true, rejectRoles: ["GBOfficer"] },
  GBApproved: { canSubmit: true, submitRoles: ["DepartmentOfficer"], canApprove: false, canReject: false },
  AdministrativeApprovalPending: { canSubmit: false, canApprove: true, approverRoles: ["DPDCOfficer"], canReject: true, rejectRoles: ["DPDCOfficer"] },
  AdministrativeApproved: { canSubmit: true, submitRoles: ["DepartmentOfficer"], canApprove: false, canReject: false },
  BudgetApprovalPending: { canSubmit: false, canApprove: true, approverRoles: ["CAFOOfficer"], canReject: true, rejectRoles: ["CAFOOfficer"] },
  BudgetApproved: { canSubmit: true, submitRoles: ["DepartmentOfficer"], canApprove: false, canReject: false },
  TenderDraftCreated: { canSubmit: true, submitRoles: ["DepartmentOfficer"], canApprove: false, canReject: false },
  TenderCommitteeApproval: { canSubmit: false, canApprove: true, approverRoles: ["TenderCommittee"], canReject: true, rejectRoles: ["TenderCommittee"] },
  PublicationApprovalPending: { canSubmit: false, canApprove: true, approverRoles: ["CompetentAuthority"], canReject: true, rejectRoles: ["CompetentAuthority"] },
  TenderPublished: { canSubmit: true, submitRoles: ["TenderCommittee", "DepartmentOfficer"], canApprove: false, canReject: false },
  TechnicalBidOpening: { canSubmit: true, submitRoles: ["TenderCommittee"], canApprove: false, canReject: false },
  TechnicalEvaluation: { canSubmit: false, canApprove: true, approverRoles: ["TenderSection"], canReject: true, rejectRoles: ["TenderSection"] },
  TechnicalEvaluationApproved: { canSubmit: false, canApprove: true, approverRoles: ["TenderCommittee"], canReject: true, rejectRoles: ["TenderCommittee"] },
  FinancialBidOpening: { canSubmit: false, canApprove: false, canReject: false },
  CAFORateOpinion: { canSubmit: true, submitRoles: ["CAFOOfficer"], canApprove: false, canReject: false },
  FinancialEvaluation: { canSubmit: false, canApprove: false, canReject: false },
  MCAOpinion: { canSubmit: true, submitRoles: ["MCAOfficer"], canApprove: false, canReject: false },
  TenderCommitteeRecommendation: { canSubmit: false, canApprove: true, approverRoles: ["TenderCommittee"], canReject: true, rejectRoles: ["TenderCommittee"] },
  FinalApproval: { canSubmit: false, canApprove: true, approverRoles: ["CompetentAuthority"], canReject: true, rejectRoles: ["CompetentAuthority"] },
  AgreementCompleted: { canSubmit: true, submitRoles: ["DepartmentOfficer"], canApprove: false, canReject: false },
  WorkOrderGenerated: { canSubmit: true, submitRoles: ["DepartmentOfficer"], canApprove: false, canReject: false },
  WorkStarted: { canSubmit: true, submitRoles: ["Contractor"], canApprove: false, canReject: false },
  MeasurementBookUpdated: { canSubmit: true, submitRoles: ["Contractor"], canApprove: false, canReject: false },
  BillSubmitted: { canSubmit: false, canApprove: true, approverRoles: ["DepartmentOfficer"], canReject: true, rejectRoles: ["DepartmentOfficer"] },
  BillApproved: { canSubmit: true, submitRoles: ["DepartmentOfficer"], canApprove: false, canReject: false },
  PaymentCompleted: { canSubmit: false, canApprove: false, canReject: false },
  Rejected: { canSubmit: false, canApprove: false, canReject: false },
};

function normalizeStatusKey(status: string): string {
  return status.replace(/\s+/g, "").trim();
}

/**
 * Check if a user can perform an action at the current tender status
 */
export function getAvailableActions(
  currentStatus: string,
  userRole?: string
): { canSubmit: boolean; canApprove: boolean; canReject: boolean } {
  const transition =
    WORKFLOW_TRANSITIONS[currentStatus] ||
    WORKFLOW_TRANSITIONS[normalizeStatusKey(currentStatus)] ||
    { canSubmit: false, canApprove: false, canReject: false };

  if (!userRole) {
    return { canSubmit: false, canApprove: false, canReject: false };
  }

  return {
    canSubmit: transition.canSubmit && (transition.submitRoles?.includes(userRole) ?? false),
    canApprove: transition.canApprove && (transition.approverRoles?.includes(userRole) ?? false),
    canReject: transition.canReject && (transition.rejectRoles?.includes(userRole) ?? false),
  };
}

export interface ApprovalHistoryEntry {
  approvalId: number;
  approvalStage: string;
  previousStatus: string | null;
  newStatus: string;
  status: string;
  actionTakenBy: string;
  roleAtAction: string | null;
  remarks: string | null;
  approvalDate: string;
}

export interface PendingApproval {
  tenderId: number;
  tenderNumber: string;
  title: string;
  currentStatus: string;
  estimatedAmount: number;
  departmentName: string;
  createdDate: string;
}

export interface RoleDashboardStats {
  totalTenders: number;
  publishedTenders: number;
  pendingApprovals: number;
  completedTenders: number;
  myTenders: number;
}


// POST /api/tenders/{id}/submit -- plain forward hand-off, no approve/reject decision
export async function submitTender(tenderId: number, remarks?: string): Promise<ApprovalHistoryEntry> {
  try {
    const { data } = await apiClient.post<ApprovalHistoryEntry>(`/tenders/${tenderId}/submit`, { remarks });
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// POST /api/tenders/{id}/approve -- approvalStage is required by the backend so it can verify
// the caller's role is the one actually configured to approve the tender's CURRENT stage;
// we always pass the tender's currentStatus-derived stage name back to the server, but the
// server is the source of truth for whether that's actually valid right now.
export async function approveTender(
  tenderId: number,
  approvalStage: string,
  remarks?: string
): Promise<ApprovalHistoryEntry> {
  try {
    const { data } = await apiClient.post<ApprovalHistoryEntry>(`/tenders/${tenderId}/approve`, {
      approvalStage,
      remarks,
    });
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// POST /api/tenders/{id}/reject
export async function rejectTender(
  tenderId: number,
  approvalStage: string,
  remarks?: string
): Promise<ApprovalHistoryEntry> {
  try {
    const { data } = await apiClient.post<ApprovalHistoryEntry>(`/tenders/${tenderId}/reject`, {
      approvalStage,
      remarks,
    });
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// GET /api/tenders/{id}/history
export async function fetchTenderHistory(tenderId: number): Promise<ApprovalHistoryEntry[]> {
  try {
    const { data } = await apiClient.get<ApprovalHistoryEntry[]>(`/tenders/${tenderId}/history`);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// GET /api/approvals/pending -- tenders awaiting the current (authenticated) user's role
export async function fetchPendingApprovals(): Promise<PendingApproval[]> {
  try {
    const { data } = await apiClient.get<PendingApproval[]>("/approvals/pending");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// GET /api/dashboard/stats -- role/user-scoped personal stats (distinct from
// /api/dashboard/public-stats and /api/dashboard/admin-stats in lib/api/dashboard.ts,
// which are unauthenticated/Admin-wide respectively; this one is "stuff relevant to me").
export async function fetchRoleDashboardStats(): Promise<RoleDashboardStats> {
  try {
    const { data } = await apiClient.get<RoleDashboardStats>("/dashboard/stats");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}
