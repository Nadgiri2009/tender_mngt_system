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
