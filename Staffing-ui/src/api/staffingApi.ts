import { apiRequest } from './client';
import type {
  PriorityDto,
  SaveItemDetailsRequest,
  SaveItemDetailsResponse,
  SaveWeekHoursResponse,
  StaffDto,
  StaffingScreen,
} from './types';

export function getStaff(signal?: AbortSignal): Promise<StaffDto[]> {
  return apiRequest<StaffDto[]>('/api/Staff', { signal });
}

export function getPriorities(signal?: AbortSignal): Promise<PriorityDto[]> {
  return apiRequest<PriorityDto[]>('/api/Priority', { signal });
}

export function getScreen(staffId: string, weeks: number, signal?: AbortSignal): Promise<StaffingScreen> {
  return apiRequest<StaffingScreen>(`/api/Staffing/${encodeURIComponent(staffId)}?weeks=${weeks}`, { signal });
}

/** Saves the hours of one item in one week. hours = 0 clears the cell. */
export function saveWeekHours(
  scheduleItemKey: number,
  weekEnding: string,
  hours: number,
): Promise<SaveWeekHoursResponse> {
  return apiRequest<SaveWeekHoursResponse>(
    `/api/Staffing/items/${scheduleItemKey}/weeks/${encodeURIComponent(weekEnding)}`,
    { method: 'PUT', body: JSON.stringify({ hours }) },
  );
}

/** Saves priority, staff comments, reviewer comments and hours to go of one item. */
export function saveItemDetails(
  scheduleItemKey: number,
  details: SaveItemDetailsRequest,
): Promise<SaveItemDetailsResponse> {
  return apiRequest<SaveItemDetailsResponse>(`/api/Staffing/items/${scheduleItemKey}`, {
    method: 'PUT',
    body: JSON.stringify(details),
  });
}