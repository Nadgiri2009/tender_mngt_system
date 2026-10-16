import { apiClient, getStoredToken, toApiError } from "./client";
import type {
  CreateTenderInput,
  PagedResult,
  PaginationQuery,
  TenderDetail,
  TenderListItem,
  UpdateTenderInput,
} from "@/types/tender";

// GET /api/tenders -- TendersController.GetAll
export async function fetchTenders(
  query: PaginationQuery
): Promise<PagedResult<TenderListItem>> {
  try {
    const endpoint = getStoredToken() ? "/tenders" : "/dashboard/public-tenders";
    const { data } = await apiClient.get<PagedResult<TenderListItem>>(endpoint, {
      params: {
        pageNumber: query.pageNumber ?? 1,
        pageSize: query.pageSize ?? 10,
        search: query.search || undefined,
        status: query.status || undefined,
        departmentId: query.departmentId || undefined,
        tenderType: query.tenderType || undefined,
        submissionDateFrom: query.submissionDateFrom || undefined,
        submissionDateTo: query.submissionDateTo || undefined,
        sortBy: query.sortBy || undefined,
        sortDescending: query.sortDescending ?? true,
      },
    });
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function fetchPublicTenders(
  query: PaginationQuery
): Promise<PagedResult<TenderListItem>> {
  try {
    const { data } = await apiClient.get<PagedResult<TenderListItem>>("/dashboard/public-tenders", {
      params: {
        pageNumber: query.pageNumber ?? 1,
        pageSize: query.pageSize ?? 10,
        search: query.search || undefined,
        departmentId: query.departmentId || undefined,
        tenderType: query.tenderType || undefined,
        submissionDateFrom: query.submissionDateFrom || undefined,
        submissionDateTo: query.submissionDateTo || undefined,
      },
    });
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// GET /api/tenders/{id} -- TendersController.GetById
export async function fetchTenderById(id: number): Promise<TenderDetail> {
  try {
    const { data } = await apiClient.get<TenderDetail>(`/tenders/${id}`);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// POST /api/tenders -- TendersController.Create expects [FromForm] string tenderJson
// plus optional files, i.e. multipart/form-data, NOT a plain JSON body. This is a
// deliberate backend design choice to support attaching documents at creation time.
export async function createTender(
  input: CreateTenderInput,
  files: File[] = []
): Promise<TenderDetail> {
  try {
    const formData = new FormData();
    formData.append("tenderJson", JSON.stringify(input));
    files.forEach((file) => formData.append("files", file));

    const { data } = await apiClient.post<TenderDetail>("/tenders", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// PUT /api/tenders/{id} -- TendersController.Update, plain JSON body.
// Backend enforces server-side that only Draft-status tenders can be edited
// (BusinessRuleException -> 400) -- mirror that check client-side too so the
// user gets instant feedback instead of a round trip, but never rely on the
// client-side check alone.
export async function updateTender(
  id: number,
  input: UpdateTenderInput
): Promise<TenderDetail> {
  try {
    const { data } = await apiClient.put<TenderDetail>(`/tenders/${id}`, input);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// DELETE /api/tenders/{id} -- soft delete, Draft-only (enforced server-side)
export async function deleteTender(id: number): Promise<void> {
  try {
    await apiClient.delete(`/tenders/${id}`);
  } catch (error) {
    throw toApiError(error);
  }
}
