export interface StaffDto {
  staffId: string;
  staffName: string;
}

export interface PriorityDto {
  priorityKey: number;
  description: string | null;
}

export interface StaffingHeader {
  staffId: string;
  staffName: string;
  items: number;
  totalBudgetHours: number;
  totalPlannedHours: number;
}

export interface StaffingRow {
  scheduleItemKey: number;
  projectNumber: string | null;
  client: string | null;
  service: string | null;
  reportTo: string | null;
  startDate: string | null;   // yyyy-MM-dd
  dueDate: string | null;     // yyyy-MM-dd
  budgetHours: number | null;
  actualHours: number | null;
  taxOrAA: string | null;
  priority: string | null;    // the GET sends the priority key as text, for example "2"
  staffComments: string | null;
  reviewerComments: string | null;
  hoursToGo: number | null;
  hours: Record<string, number | null>;   // key = week ending date (yyyy-MM-dd)
}

export interface StaffingScreen {
  header: StaffingHeader;
  weeks: string[];                         // week ending dates (Fridays), yyyy-MM-dd
  standardWeekHours: number;
  weekTotals: Record<string, number>;
  rows: StaffingRow[];
}

// ---- Saving ----

export interface SaveWeekHoursResponse {
  scheduleItemKey: number;
  weekEnding: string;
  hours: number;
  cleared: boolean;
}

/** All four fields are always sent. null clears a value. */
export interface SaveItemDetailsRequest {
  priorityKey: number | null;
  staffComments: string | null;
  reviewerComments: string | null;
  hoursToGo: number | null;
}

export interface SaveItemDetailsResponse extends SaveItemDetailsRequest {
  scheduleItemKey: number;
}