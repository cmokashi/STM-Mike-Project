import type { StaffingRow } from '../api/types';

const TOLERANCE = 0.005;

/** "2026-10-30" -> "1030" (the week column title) */
export function weekLabel(weekEnding: string): string {
  return weekEnding.slice(5, 7) + weekEnding.slice(8, 10);
}

/** Today as yyyy-MM-dd in the user's own time zone. */
export function todayIso(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

/** Avoids 0.1 + 0.2 = 0.30000000000000004 in totals. */
export function round(value: number): number {
  return Math.round(value * 10000) / 10000;
}

export function sumHours(values: Array<number | null | undefined>): number {
  return round(values.reduce<number>((total, value) => total + (value ?? 0), 0));
}

/** Total planned hours of one item over the weeks shown. */
export function plannedTotal(row: StaffingRow, weeks: string[]): number {
  return sumHours(weeks.map((week) => row.hours[week]));
}

/** RULE: the due date has passed. (Confirm with Mike whether "due today" is late.) */
export function isPastDue(row: StaffingRow, today: string): boolean {
  return row.dueDate !== null && row.dueDate < today;
}

/** RULE: actual hours are more than the budget. */
export function isOverBudget(row: StaffingRow): boolean {
  return (row.actualHours ?? 0) > (row.budgetHours ?? 0) + TOLERANCE;
}


export function hoursToGoMismatch(row: StaffingRow, weeks: string[]): boolean {
  return Math.abs((row.hoursToGo ?? 0) - plannedTotal(row, weeks)) > TOLERANCE;
}

/** RULE: a week total below, equal to or above the standard week. */
export function weekTotalLevel(total: number, standard: number): 'low' | 'ok' | 'high' {
  if (Math.abs(total - standard) <= TOLERANCE) return 'ok';
  return total < standard ? 'low' : 'high';
}


/** "2026-10-30" -> "10/30" */
export function monthDay(isoDate: string): string {
  return `${Number(isoDate.slice(5, 7))}/${Number(isoDate.slice(8, 10))}`;
}

/** Week title: "1. This Week", "2. Next", "3", "4" ... */
export function weekNumberLabel(index: number): string {
  if (index === 0) return '1. This Week';
  if (index === 1) return '2. Next';
  return String(index + 1);
}