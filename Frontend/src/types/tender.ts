// Mirrors Backend/src/TenderManagement.Application/DTOs/Tender/TenderDtos.cs
// Status, method, and type options are loaded from their SQL-backed lookup
// endpoints at runtime; see lib/api/lookups.ts.

export type TenderStatus = string;
export type TenderType = string;

export interface Department {
  departmentId: number;
  departmentName: string;
  departmentCode?: string | null;
  headOfficerName?: string | null;
  isActive?: boolean;
  createdDate?: string;
}

export interface TenderListItem {
  tenderId: number;
  tenderNumber: string;
  trackingId: string;
  title: string;
  departmentName: string;
  tenderType: string;
  tenderCategory: string | null;
  estimatedAmount: number;
  currentStatus: string;
  publicationDate: string | null;
  submissionStartDate: string | null;
  submissionDeadline: string | null;
  assignedRespOfficer: string;
  assignedRole: string | null;
  createdDate: string;
  canApply: boolean;
  applicationStatus: string;
}

export interface BOQItem {
  boqId: number;
  itemName: string;
  description: string | null;
  quantity: number;
  unit: string;
  rate: number;
  amount: number;
}

export interface TenderDocument {
  documentId: number;
  documentType: string;
  fileName: string;
  filePath: string;
  uploadedDate: string;
}

export interface TenderDetail extends TenderListItem {
  description: string | null;
  createdByName: string;
  boqItems: BOQItem[];
  documents: TenderDocument[];
}

// Matches CreateTenderDto exactly -- same field names, same required-ness.
export interface CreateTenderInput {
  title: string;
  description?: string;
  departmentId: number;
  estimatedAmount: number;
  tenderType: string;
  tenderCategory: string;
  budgetInformation?: string;
  technicalSanctionNumber?: string;
  technicalSanctionAuthority?: string;
  technicalSanctionDate?: string;
  preBidMeetingUtc?: string;
  preBidLocation?: string;
  preBidDetails?: string;
  technicalBidOpeningScheduledUtc?: string;
  submissionStartDate?: string;
  submissionDeadline?: string;
}

// Matches UpdateTenderDto -- note: no departmentId, backend doesn't allow moving
// a tender to a different department after creation.
export interface UpdateTenderInput {
  title: string;
  description?: string;
  estimatedAmount: number;
  tenderType: string;
  tenderCategory: string;
  submissionStartDate?: string;
  submissionDeadline?: string;
}

export interface PaginationQuery {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  status?: string;
  statusGroup?: string;
  departmentId?: number;
  tenderType?: string;
  submissionDateFrom?: string;
  submissionDateTo?: string;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}
