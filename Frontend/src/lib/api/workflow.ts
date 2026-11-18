import { apiClient, toApiError } from "./client";
import type { PagedResult, TenderListItem } from "@/types/tender";

// Mirrors Backend/.../DTOs/Workflow/WorkflowDtos.cs exactly.

export interface WorkflowActionOption {
  actionName: string;
  stageName: string;
  fromStatus: string;
  toStatus: string;
}

export interface WorkflowStep {
  status: string;
  stageName: string;
  assigneeRole: string | null;
  isCurrent: boolean;
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
  availableActions: string[];
}

export interface RoleDashboardStats {
  totalTenders: number;
  approvedTenders: number;
  pendingTenders: number;
  rejectedTenders: number;
  underReviewTenders: number;
  publishedTenders: number;
  draftTenders: number;
  closedExpiredTenders: number;
  pendingApprovals: number;
  completedTenders: number;
  myTenders: number;
  categoryBreakdown: { category: string; tenderCount: number }[];
  recentTenders: TenderListItem[];
  upcomingDeadlines: TenderListItem[];
}


// GET /api/tenders/{id}/actions
export async function fetchAvailableWorkflowActions(tenderId: number): Promise<WorkflowActionOption[]> {
  try {
    const { data } = await apiClient.get<WorkflowActionOption[]>(`/tenders/${tenderId}/actions`);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// POST /api/tenders/{id}/actions/{actionName}
export async function executeWorkflowAction(
  tenderId: number,
  actionName: string,
  remarks?: string
): Promise<ApprovalHistoryEntry> {
  try {
    const { data } = await apiClient.post<ApprovalHistoryEntry>(
      `/tenders/${tenderId}/actions/${encodeURIComponent(actionName)}`,
      { remarks }
    );
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// GET /api/tenders/workflow-steps
export async function fetchWorkflowSteps(): Promise<WorkflowStep[]> {
  try {
    const { data } = await apiClient.get<WorkflowStep[]>("/tenders/workflow-steps");
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
export async function fetchPendingApprovals(query: {
  pageNumber: number;
  pageSize: number;
  search?: string;
}): Promise<PagedResult<PendingApproval>> {
  try {
    const { data } = await apiClient.get<PagedResult<PendingApproval>>("/approvals/pending", {
      params: {
        pageNumber: query.pageNumber,
        pageSize: query.pageSize,
        search: query.search || undefined,
      },
    });
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
