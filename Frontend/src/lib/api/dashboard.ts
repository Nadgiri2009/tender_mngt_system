import { apiClient, toApiError } from "./client";
import type { TenderListItem } from "@/types/tender";

export interface PublicStats {
  activeTenders: number;
  publishedTenders: number;
  registeredContractors: number;
  departments: number;
  completedProjects: number;
}

export interface AdminStats {
  totalTenders: number;
  draftTenders: number;
  publishedTenders: number;
  closedExpiredTenders: number;
  approvedTenders: number;
  pendingTenders: number;
  rejectedTenders: number;
  underReviewTenders: number;
  totalDepartments: number;
  totalContractors: number;
  totalUsers: number;
  statusBreakdown: { status: string; count: number }[];
  categoryBreakdown: { category: string; tenderCount: number }[];
  recentTenders: TenderListItem[];
  upcomingDeadlines: TenderListItem[];
}

// GET /api/dashboard/public-stats - no auth required, backs the homepage stat cards
export async function fetchPublicStats(): Promise<PublicStats> {
  try {
    const { data } = await apiClient.get<PublicStats>("/dashboard/public-stats");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// GET /api/dashboard/admin-stats - Admin only
export async function fetchAdminStats(): Promise<AdminStats> {
  try {
    const { data } = await apiClient.get<AdminStats>("/dashboard/admin-stats");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}
