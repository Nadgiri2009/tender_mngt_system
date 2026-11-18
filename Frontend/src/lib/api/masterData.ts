import { apiClient, toApiError } from "./client";
import type { PagedResult } from "@/types/tender";

export type MasterDataCatalog =
  | "tender-methods"
  | "tender-types"
  | "document-types"
  | "units";

export interface MasterDataItem {
  name: string;
  sortOrder: number;
  isActive: boolean;
}

export interface MasterDataQuery {
  pageNumber: number;
  pageSize: number;
  search?: string;
}

export interface SaveMasterDataItemInput {
  name: string;
  sortOrder: number;
  isActive: boolean;
}

export async function fetchMasterData(
  catalog: MasterDataCatalog,
  query: MasterDataQuery
): Promise<PagedResult<MasterDataItem>> {
  try {
    const { data } = await apiClient.get<PagedResult<MasterDataItem>>(
      `/master-data/${catalog}/paged`,
      {
        params: {
          pageNumber: query.pageNumber,
          pageSize: query.pageSize,
          search: query.search || undefined,
        },
      }
    );
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function createMasterDataItem(
  catalog: MasterDataCatalog,
  input: SaveMasterDataItemInput
): Promise<MasterDataItem> {
  try {
    const { data } = await apiClient.post<MasterDataItem>(`/master-data/${catalog}`, input);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function updateMasterDataItem(
  catalog: MasterDataCatalog,
  key: string,
  input: SaveMasterDataItemInput
): Promise<MasterDataItem> {
  try {
    const { data } = await apiClient.put<MasterDataItem>(
      `/master-data/${catalog}/${encodeURIComponent(key)}`,
      input
    );
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function deleteMasterDataItem(
  catalog: MasterDataCatalog,
  key: string
): Promise<{ message?: string }> {
  try {
    const { data } = await apiClient.delete<{ message?: string }>(
      `/master-data/${catalog}/${encodeURIComponent(key)}`
    );
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}
