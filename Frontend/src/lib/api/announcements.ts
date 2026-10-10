import { apiClient, toApiError } from "./client";

export interface Announcement {
  announcementId: number;
  title: string;
  body: string | null;
  isPublished: boolean;
  startDate?: string | null;
  publishDate?: string | null;
  expiryDate?: string | null;
  createdByName: string;
  createdDate: string;
}

export interface AnnouncementInput {
  title: string;
  body?: string;
  isPublished: boolean;
  startDate?: string;
  publishDate?: string;
  expiryDate?: string;
}

// GET /api/announcements/published - public, backs the homepage
export async function fetchPublishedAnnouncements(): Promise<Announcement[]> {
  try {
    const { data } = await apiClient.get<Announcement[]>("/announcements/published");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// GET /api/announcements - Admin only
export async function fetchAllAnnouncements(): Promise<Announcement[]> {
  try {
    const { data } = await apiClient.get<Announcement[]>("/announcements");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// POST /api/announcements - Admin only
export async function createAnnouncement(input: AnnouncementInput): Promise<Announcement> {
  try {
    const { data } = await apiClient.post<Announcement>("/announcements", input);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// PUT /api/announcements/{id} - Admin only
export async function updateAnnouncement(id: number, input: AnnouncementInput): Promise<Announcement> {
  try {
    const { data } = await apiClient.put<Announcement>(`/announcements/${id}`, input);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// DELETE /api/announcements/{id} - Admin only
export async function deleteAnnouncement(id: number): Promise<void> {
  try {
    await apiClient.delete(`/announcements/${id}`);
  } catch (error) {
    throw toApiError(error);
  }
}
