import { apiClient, toApiError } from "./client";

export const PORTAL_TERMS_VERSION = "portal-terms-v1";

export interface TenderApplicationResponse {
  technicalBidId: number;
  tenderId: number;
  applicationReference: string;
  status: string;
  submittedDate: string;
  documents: string[];
}

export interface TenderApplicationRequirement {
  documentType: string;
  description: string | null;
  isMandatory: boolean;
  allowedExtensions: string | null;
  maxFileSizeBytes: number;
}

export interface ContractorApplicationListItem {
  technicalBidId: number;
  tenderId: number;
  tenderNumber: string;
  tenderTitle: string;
  contractorId: number;
  contractorName: string;
  applicationReference: string;
  status: string;
  createdAtUtc: string;
  submittedAtUtc: string | null;
  documentCount: number;
  pendingMandatoryDocumentCount: number;
}

export interface ContractorApplicationDetail extends ContractorApplicationListItem {
  departmentName: string;
  estimatedAmount: number;
  contractorRepresentativeName: string | null;
  contractorEmail: string | null;
  contractorMobile: string | null;
  contractorAddress: string | null;
  requirements: TenderApplicationRequirement[];
  documents: {
    documentId: number;
    documentType: string;
    fileName: string;
    uploadedAtUtc: string;
    uploadedByName: string;
    verificationStatus: string | null;
    verificationRemarks: string | null;
    verifiedAtUtc: string | null;
  }[];
  remarks: {
    remarkId: number;
    remarkType: string;
    message: string;
    authorName: string;
    authorRole: string | null;
    visibleToContractor: boolean;
    createdAtUtc: string;
  }[];
  history: {
    historyId: number;
    eventType: string;
    previousStatus: string | null;
    newStatus: string | null;
    remarks: string | null;
    authorName: string;
    authorRole: string | null;
    visibleToContractor: boolean;
    createdAtUtc: string;
  }[];
}

export interface ContractorApplicationDashboard {
  contractorName: string;
  companyName: string;
  totalApplications: number;
  draftApplications: number;
  submittedApplications: number;
  underVerificationApplications: number;
  shortfallApplications: number;
  approvedApplications: number;
  rejectedApplications: number;
  notifications: {
    notificationId: number;
    tenderId: number;
    tenderNumber: string;
    message: string;
    notificationType: string;
    createdAtUtc: string;
    readAtUtc: string | null;
  }[];
  applications: ContractorApplicationListItem[];
}

export async function getApplicationRequirements(tenderId: number): Promise<TenderApplicationRequirement[]> {
  try {
    const { data } = await apiClient.get<TenderApplicationRequirement[]>(
      `/bids/applications/tender/${tenderId}/requirements`
    );
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function setApplicationRequirements(
  tenderId: number,
  requirements: TenderApplicationRequirement[]
): Promise<void> {
  try {
    await apiClient.put(`/bids/applications/tender/${tenderId}/requirements`, requirements);
  } catch (error) {
    throw toApiError(error);
  }
}

export async function saveApplicationDraft(
  tenderId: number,
  documents: { documentType: string; file: File }[]
): Promise<ContractorApplicationDetail> {
  try {
    const formData = new FormData();
    documents.forEach(({ documentType, file }) => {
      formData.append("documentTypes", documentType);
      formData.append("files", file);
    });
    const { data } = await apiClient.post<ContractorApplicationDetail>(
      `/bids/applications/drafts/${tenderId}`,
      formData,
      { headers: { "Content-Type": "multipart/form-data" } }
    );
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function submitApplication(applicationId: number): Promise<ContractorApplicationDetail> {
  try {
    const { data } = await apiClient.post<ContractorApplicationDetail>(
      `/bids/applications/${applicationId}/submit`,
      { termsAccepted: true, termsVersion: PORTAL_TERMS_VERSION }
    );
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function getContractorApplicationDashboard(): Promise<ContractorApplicationDashboard> {
  try {
    const { data } = await apiClient.get<ContractorApplicationDashboard>("/bids/applications/dashboard");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function markContractorNotificationRead(notificationId: number): Promise<void> {
  try {
    await apiClient.put(`/bids/applications/notifications/${notificationId}/read`);
  } catch (error) {
    throw toApiError(error);
  }
}

export async function getContractorApplication(applicationId: number): Promise<ContractorApplicationDetail> {
  try {
    const { data } = await apiClient.get<ContractorApplicationDetail>(`/bids/applications/${applicationId}`);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function getInternalApplications(): Promise<ContractorApplicationListItem[]> {
  try {
    const { data } = await apiClient.get<ContractorApplicationListItem[]>("/bids/applications");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function getApplicationDetails(applicationId: number): Promise<ContractorApplicationDetail> {
  return getContractorApplication(applicationId);
}

export async function reviewApplicationDocument(
  applicationId: number,
  documentId: number,
  verificationStatus: "Verified" | "Shortfall" | "Rejected",
  remarks: string
): Promise<ContractorApplicationDetail> {
  try {
    const { data } = await apiClient.put<ContractorApplicationDetail>(
      `/bids/applications/${applicationId}/documents/${documentId}/verification`,
      { verificationStatus, remarks }
    );
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function reviewApplication(
  applicationId: number,
  decision: "Qualified" | "Disqualified",
  remarks: string
): Promise<ContractorApplicationDetail> {
  try {
    const { data } = await apiClient.put<ContractorApplicationDetail>(
      `/bids/applications/${applicationId}/evaluation`,
      { decision, remarks }
    );
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function getContractors(): Promise<{
  contractorId: number;
  name: string;
  registrationNo: string;
  address: string | null;
  contactNo: string | null;
  email: string | null;
  isActive: boolean;
}[]> {
  try {
    const { data } = await apiClient.get("/contractors");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function addApplicationDocuments(
  applicationId: number,
  documents: { documentType: string; file: File }[]
): Promise<ContractorApplicationDetail> {
  try {
    const formData = new FormData();
    documents.forEach(({ documentType, file }) => {
      formData.append("documentTypes", documentType);
      formData.append("files", file);
    });
    const { data } = await apiClient.post<ContractorApplicationDetail>(
      `/bids/applications/${applicationId}/documents`,
      formData,
      { headers: { "Content-Type": "multipart/form-data" } }
    );
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function addApplicationRemark(
  applicationId: number,
  message: string,
  remarkType: "Clarification" | "Internal" | "Response"
): Promise<ContractorApplicationDetail> {
  try {
    const { data } = await apiClient.post<ContractorApplicationDetail>(
      `/bids/applications/${applicationId}/remarks`,
      { message, remarkType }
    );
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function downloadApplicationDocument(applicationId: number, documentId: number): Promise<Blob> {
  try {
    const { data } = await apiClient.get<Blob>(
      `/bids/applications/${applicationId}/documents/${documentId}`,
      { responseType: "blob" }
    );
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function submitTenderApplication(
  tenderId: number,
  files: File[]
): Promise<TenderApplicationResponse> {
  try {
    const formData = new FormData();
    formData.append("termsAccepted", "true");
    formData.append("termsVersion", PORTAL_TERMS_VERSION);
    files.forEach((file) => formData.append("files", file));
    const { data } = await apiClient.post<TenderApplicationResponse>(
      `/bids/apply/${tenderId}`,
      formData,
      { headers: { "Content-Type": "multipart/form-data" } }
    );
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}
