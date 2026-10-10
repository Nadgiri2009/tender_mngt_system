import { apiClient, toApiError } from "./client";

// Mirrors Backend/.../DTOs/Bid/BidDtos.cs exactly.

export interface TechnicalBidDto {
  technicalBidId: number;
  tenderId: number;
  contractorId: number;
  contractorName: string;
  status: string; // "Submitted" | "Qualified" | "Disqualified"
  remarks: string | null;
  submittedDate: string;
}

export interface FinancialBidDto {
  financialBidId: number;
  tenderId: number;
  contractorId: number;
  contractorName: string;
  bidAmount: number;
  rank: number | null;
  status: string; // "Submitted" | "Selected" | "Rejected"
}

export interface ContractorProfile {
  contractorId: number;
  name: string;
  registrationNo: string;
  address: string | null;
  contactNo: string | null;
  email: string | null;
  userId: number | null;
  isActive: boolean;
}

// POST /api/bids/technical
export async function submitTechnicalBid(
  tenderId: number,
  contractorId: number,
  documentPath?: string
): Promise<TechnicalBidDto> {
  try {
    const { data } = await apiClient.post<TechnicalBidDto>("/bids/technical", {
      tenderId,
      contractorId,
      documentPath,
    });
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// PUT /api/bids/technical/evaluate
export async function evaluateTechnicalBid(
  technicalBidId: number,
  status: "Qualified" | "Disqualified",
  remarks?: string
): Promise<TechnicalBidDto> {
  try {
    const { data } = await apiClient.put<TechnicalBidDto>("/bids/technical/evaluate", {
      technicalBidId,
      status,
      remarks,
    });
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// GET /api/bids/{tenderId}
export async function fetchTechnicalBids(tenderId: number): Promise<TechnicalBidDto[]> {
  try {
    const { data } = await apiClient.get<TechnicalBidDto[]>(`/bids/${tenderId}`);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// POST /api/bids/financial
export async function submitFinancialBid(
  tenderId: number,
  contractorId: number,
  bidAmount: number
): Promise<FinancialBidDto> {
  try {
    const { data } = await apiClient.post<FinancialBidDto>("/bids/financial", {
      tenderId,
      contractorId,
      bidAmount,
    });
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// GET /api/bids/{tenderId}/financial - ranked L1 first
export async function fetchFinancialBidsRanked(tenderId: number): Promise<FinancialBidDto[]> {
  try {
    const { data } = await apiClient.get<FinancialBidDto[]>(`/bids/${tenderId}/financial`);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// POST /api/bids/financial/select
export async function selectContractor(
  tenderId: number,
  contractorId: number,
  remarks?: string
): Promise<FinancialBidDto> {
  try {
    const { data } = await apiClient.post<FinancialBidDto>("/bids/financial/select", {
      tenderId,
      contractorId,
      remarks,
    });
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// GET /api/contractors/me - resolves the logged-in Contractor user's own ContractorId.
// Returns null (not thrown) on 404 so the UI can show a "profile not linked" message
// instead of a raw error toast, since this is an expected state for new contractor accounts.
export async function fetchMyContractorProfile(): Promise<ContractorProfile | null> {
  try {
    const { data } = await apiClient.get<ContractorProfile>("/contractors/me");
    return data;
  } catch (error) {
    const apiError = toApiError(error);
    if (apiError.statusCode === 404) return null;
    throw apiError;
  }
}

// GET /api/contractors - full list, used by Admin/staff to pick a contractor manually
// (e.g. Admin submitting a technical bid on behalf of a contractor).
export async function fetchContractors(): Promise<ContractorProfile[]> {
  try {
    const { data } = await apiClient.get<ContractorProfile[]>("/contractors");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

/**
 * Bid-stage statuses are stored with spaces on the backend (Domain.Enums.TenderStatus),
 * e.g. "Technical Bid Opening". This normalizes for comparison the same way
 * lib/api/workflow.ts does internally.
 */
export function normalizeStatus(status: string): string {
  return status.replace(/\s+/g, "").trim();
}

// The Bids panel becomes relevant once a tender has been published and stays relevant
// through contractor selection -- it should still render (read-only) after that so the
// bid history remains visible on later-stage tenders.
const BID_RELEVANT_FROM = normalizeStatus("Technical Bid Opening");
const STATUS_SEQUENCE = [
  "Draft",
  "RequirementSubmitted",
  "EstimatePrepared",
  "TSApprovalPending",
  "TSApproved",
  "GBApprovalPending",
  "GBApproved",
  "AdministrativeApprovalPending",
  "AdministrativeApproved",
  "BudgetApprovalPending",
  "BudgetApproved",
  "TenderDraftCreated",
  "TenderCommitteeApproval",
  "PublicationApprovalPending",
  "TenderPublished",
  "TechnicalBidOpening",
  "TechnicalEvaluation",
  "TechnicalEvaluationApproved",
  "FinancialBidOpening",
  "FinancialEvaluation",
  "CAFORateOpinion",
  "MCAOpinion",
  "TenderCommitteeRecommendation",
  "FinalApproval",
  "AgreementCompleted",
  "WorkOrderGenerated",
  "WorkStarted",
  "MeasurementBookUpdated",
  "BillSubmitted",
  "BillApproved",
  "PaymentCompleted",
];

export function isBidStageOrLater(currentStatus: string): boolean {
  const normalized = normalizeStatus(currentStatus);
  if (normalized === "Rejected") return true; // could have been rejected mid-bid-stage; still show history
  const idx = STATUS_SEQUENCE.indexOf(normalized);
  const bidIdx = STATUS_SEQUENCE.indexOf(BID_RELEVANT_FROM);
  return idx >= 0 && idx >= bidIdx;
}
