import { useCallback, useMemo, useRef, useState } from 'react';
import { saveItemDetails, saveWeekHours } from '../api/staffingApi';
import { errorMessage } from '../api/client';
import type { PriorityDto, SaveItemDetailsRequest, StaffingRow, StaffingScreen } from '../api/types';
import { formatHours } from '../utils/format';
import { sumHours, weekLabel } from '../utils/schedule';
import { HeaderSummary } from './HeaderSummary';
import { ScheduleGrid } from './ScheduleGrid';
import type { CellEdit, GridRow } from './ScheduleGrid';

interface Props {
  screen: StaffingScreen;
  priorities: PriorityDto[];
}

type SaveState =
  | { kind: 'idle' }
  | { kind: 'saving' }
  | { kind: 'saved'; at: string }
  | { kind: 'error'; message: string };

const WEEK_COLUMN = /^\d{4}-\d{2}-\d{2}$/;

/** Turns a technical server message into plain words: "The field HoursToGo must be..." -> "Hours to go must be...". */
function friendlyMessage(message: string): string {
  return message
    .replace(/^The field /, '')
    .replace(/\bHoursToGo\b/g, 'Hours to go')
    .replace(/\bStaffComments\b/g, 'Staff comments')
    .replace(/\bReviewerComments\b/g, 'Reviewer comments')
    .replace(/\bPriorityKey\b/g, 'Priority');
}

function clock(): string {
  return new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

/** "" and null both mean "empty". Comments are trimmed. */
function toText(value: unknown): string | null {
  if (value === null || value === undefined) return null;
  const text = String(value).trim();
  return text === '' ? null : text;
}

function toNumber(value: unknown): number | null {
  if (value === null || value === undefined || value === '') return null;
  const number = Number(value);
  return Number.isFinite(number) ? number : null;
}

export function ScheduleView({ screen, priorities }: Props) {
  const [rows, setRows] = useState<StaffingRow[]>(screen.rows);
  const [saveState, setSaveState] = useState<SaveState>({ kind: 'idle' });

  // The latest rows, readable inside a save that runs after other saves.
  const rowsRef = useRef<StaffingRow[]>(screen.rows);
  const queueRef = useRef<Promise<void>>(Promise.resolve());
  const pendingRef = useRef(0);

  const updateRow = useCallback((scheduleItemKey: number, change: (row: StaffingRow) => StaffingRow) => {
    rowsRef.current = rowsRef.current.map((row) => (row.scheduleItemKey === scheduleItemKey ? change(row) : row));
    setRows(rowsRef.current);
  }, []);

  /** Runs saves one after another, so two quick edits of the same item cannot overwrite each other. */
  const enqueue = useCallback((description: string, job: () => Promise<void>) => {
    pendingRef.current += 1;
    setSaveState({ kind: 'saving' });
    queueRef.current = queueRef.current.then(async () => {
      try {
        await job();
        pendingRef.current -= 1;
        if (pendingRef.current === 0) setSaveState((state) => (state.kind === 'error' ? state : { kind: 'saved', at: clock() }));
      } catch (error) {
        pendingRef.current -= 1;
        setSaveState({ kind: 'error', message: `${description}: ${friendlyMessage(errorMessage(error))}` });
      }
    });
  }, []);

  const handleEdit = useCallback(
    (edit: CellEdit) => {
      const { scheduleItemKey, columnId } = edit;
      const row = rowsRef.current.find((r) => r.scheduleItemKey === scheduleItemKey);
      if (!row) return;

      // ---- one week cell ----
      if (WEEK_COLUMN.test(columnId)) {
        const hours = toNumber(edit.newValue) ?? 0;   // an empty cell means 0, which clears the row in the database
        if (hours === (row.hours[columnId] ?? 0)) return;
        enqueue(`Item ${scheduleItemKey}, week ${weekLabel(columnId)}`, async () => {
          const saved = await saveWeekHours(scheduleItemKey, columnId, hours);
          updateRow(scheduleItemKey, (r) => ({
            ...r,
            hours: { ...r.hours, [columnId]: saved.cleared ? null : saved.hours },
          }));
        });
        return;
      }

      // ---- priority, comments, hours to go: one request carries all four fields ----
      const label = { priority: 'Priority', staffComments: 'Staff comments', reviewerComments: 'Reviewer comments', hoursToGo: 'Hours to go' }[columnId];
      if (label === undefined) return;

      enqueue(`Item ${scheduleItemKey}, ${label}`, async () => {
        const current = rowsRef.current.find((r) => r.scheduleItemKey === scheduleItemKey) ?? row;
        const request: SaveItemDetailsRequest = {
          priorityKey: toNumber(current.priority),
          staffComments: current.staffComments,
          reviewerComments: current.reviewerComments,
          hoursToGo: current.hoursToGo,
        };
        if (columnId === 'priority') request.priorityKey = toNumber(edit.newValue);
        if (columnId === 'staffComments') request.staffComments = toText(edit.newValue);
        if (columnId === 'reviewerComments') request.reviewerComments = toText(edit.newValue);
        if (columnId === 'hoursToGo') request.hoursToGo = toNumber(edit.newValue);

        const saved = await saveItemDetails(scheduleItemKey, request);
        updateRow(scheduleItemKey, (r) => ({
          ...r,
          priority: saved.priorityKey === null ? null : String(saved.priorityKey),
          staffComments: saved.staffComments,
          reviewerComments: saved.reviewerComments,
          hoursToGo: saved.hoursToGo,
        }));
      });
    },
    [enqueue, updateRow],
  );

  // ---- totals, recalculated after every successful save ----
  const { header, pinnedRows } = useMemo(() => {
    const weekTotals: Record<string, number> = {};
    for (const week of screen.weeks) weekTotals[week] = sumHours(rows.map((row) => row.hours[week]));

    const planned = sumHours(Object.values(weekTotals));
    const blank = { projectNumber: null, service: null, reportTo: null, startDate: null, dueDate: null, taxOrAA: null, priority: null, staffComments: null, reviewerComments: null };

    const total: GridRow = {
      ...blank,
      pinnedKind: 'total',
      scheduleItemKey: -1,
      client: null,
      budgetHours: sumHours(rows.map((row) => row.budgetHours)),
      actualHours: sumHours(rows.map((row) => row.actualHours)),
      hoursToGo: sumHours(rows.map((row) => row.hoursToGo)),
      hours: weekTotals,
    };
    const standard: GridRow = {
      ...blank,
      pinnedKind: 'standard',
      scheduleItemKey: -2,
      client: null,
      budgetHours: null,
      actualHours: null,
      hoursToGo: null,
      hours: Object.fromEntries(screen.weeks.map((week) => [week, screen.standardWeekHours])),
    };

    return {
      header: { ...screen.header, totalPlannedHours: planned },
      pinnedRows: [total, standard],
    };
  }, [rows, screen]);

  return (
    <>
      <HeaderSummary header={header} weekCount={screen.weeks.length} />

      <div className="toolbar">
        <span className="chip">Weeks shown: {screen.weeks.length} (scroll right)</span>
        <span className="chip">Standard week = {formatHours(screen.standardWeekHours)} h</span>
        <span className="toolbar__hint">Double-click a yellow or green cell (or select it and press Enter) to edit. Enter saves, Esc cancels.</span>
        <span className="toolbar__status" role="status" aria-live="polite">
          {saveState.kind === 'saving' && <span className="save save--busy">Saving...</span>}
          {saveState.kind === 'saved' && <span className="save save--ok">&#10003; Saved {saveState.at}</span>}
        </span>
      </div>

      {saveState.kind === 'error' && (
        <div className="banner banner--error" role="alert">
          <span>Not saved. {saveState.message}</span>
          <button type="button" onClick={() => setSaveState({ kind: 'idle' })}>Dismiss</button>
        </div>
      )}

      <ScheduleGrid
        rows={rows}
        pinnedRows={pinnedRows}
        weeks={screen.weeks}
        standardWeekHours={screen.standardWeekHours}
        priorities={priorities}
        onEdit={handleEdit}
      />

      <div className="legend">
        <b>Colors:</b>
        <span><i className="sw sw--practice" />Practice CS (read-only)</span>
        <span><i className="sw sw--staffing" />Staffing fields (editable)</span>
        <span><i className="sw sw--week" />Week hours (editable)</span>
        <span><i className="sw sw--over" />Actual &gt; Budget</span>
        <span><b className="late">Red text</b> = past due date, or Hours to go different from planned hours</span>
        <span><i className="sw sw--ok" />Week total = standard</span>
        <span><i className="sw sw--low" />below</span>
        <span><i className="sw sw--high" />above</span>
      </div>
    </>
  );
}