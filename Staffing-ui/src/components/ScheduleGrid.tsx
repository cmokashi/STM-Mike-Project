import { useMemo } from 'react';
import { AgGridReact } from 'ag-grid-react';
import { AllCommunityModule, ModuleRegistry, themeQuartz } from 'ag-grid-community';
import type {
  CellEditRequestEvent,
  CellClassParams,
  ColDef,
  ColGroupDef,
  GetRowIdParams,
  ValueFormatterParams,
} from 'ag-grid-community';
import type { PriorityDto, StaffingRow } from '../api/types';
import { formatHours } from '../utils/format';
import {
  hoursToGoMismatch,
  isOverBudget,
  isPastDue,
  todayIso,
  monthDay,
  weekNumberLabel,
  weekTotalLevel,
} from '../utils/schedule';

ModuleRegistry.registerModules([AllCommunityModule]);

/** A normal row, or one of the two fixed rows at the bottom of the grid. */
export interface GridRow extends StaffingRow {
  pinnedKind?: 'total' | 'standard';
}

export const MAX_COMMENT_LENGTH = 100;

/** Column ids. A week column uses its own date (yyyy-MM-dd) as the id. */
export type EditableColumn = 'priority' | 'staffComments' | 'reviewerComments' | 'hoursToGo' | string;

export interface CellEdit {
  scheduleItemKey: number;
  columnId: EditableColumn;
  oldValue: unknown;
  newValue: unknown;
}

interface Props {
  rows: GridRow[];
  pinnedRows: GridRow[];
  weeks: string[];
  standardWeekHours: number;
  priorities: PriorityDto[];
  onEdit: (edit: CellEdit) => void;
}

const theme = themeQuartz.withParams({
  fontSize: 12,
  headerFontSize: 12,
  rowHeight: 28,
  headerHeight: 30,
  cellHorizontalPadding: 7,
  wrapperBorderRadius: 8,
});

const isPinned = (params: CellClassParams<GridRow>) => params.data?.pinnedKind !== undefined;

function pinnedClass(params: CellClassParams<GridRow>): string {
  return params.data?.pinnedKind === 'total' ? 'cell-total' : params.data?.pinnedKind === 'standard' ? 'cell-standard' : '';
}

const hoursFormatter = (params: ValueFormatterParams<GridRow>) => formatHours(params.value as number | null);

export function ScheduleGrid({ rows, pinnedRows, weeks, standardWeekHours, priorities, onEdit }: Props) {
  const today = useMemo(() => todayIso(), []);

  const columnDefs = useMemo<Array<ColDef<GridRow> | ColGroupDef<GridRow>>>(() => {
    const priorityName = new Map(priorities.map((p) => [String(p.priorityKey), p.description ?? String(p.priorityKey)]));
    const priorityValues = ['', ...priorities.map((p) => String(p.priorityKey))];

    // Classes shared by one colour group. Pinned rows get their own look instead.
    const practice = (extra?: (p: CellClassParams<GridRow>) => string) => (p: CellClassParams<GridRow>) =>
      isPinned(p) ? pinnedClass(p) : ['cell-practice', extra?.(p) ?? ''];
    const staffing = (extra?: (p: CellClassParams<GridRow>) => string) => (p: CellClassParams<GridRow>) =>
      isPinned(p) ? pinnedClass(p) : ['cell-staffing', extra?.(p) ?? ''];

    const practiceGroup: ColGroupDef<GridRow> = {
      headerName: 'PRACTICE CS (read-only)',
      headerClass: 'hdr-practice',
      children: [
        {
          colId: 'scheduleItemKey',
          headerName: 'Item key',
          valueGetter: (p) => (p.data?.pinnedKind ? '' : p.data?.scheduleItemKey),
          width: 80,
          pinned: 'left',
          headerClass: 'hdr-practice',
          cellClass: practice(),
        },
        {
          colId: 'client',
          headerName: 'Client',
          valueGetter: (p) =>
            p.data?.pinnedKind === 'total'
              ? `TOTAL (${rows.length} items)`
              : p.data?.pinnedKind === 'standard'
                ? 'Standard week hours'
                : p.data?.client,
          width: 250,
          pinned: 'left',
          headerClass: 'hdr-practice',
          cellClass: practice(),
        },
        { field: 'service', headerName: 'Service', width: 100, headerClass: 'hdr-practice', cellClass: practice() },
        { field: 'reportTo', headerName: 'Report to', width: 120, headerClass: 'hdr-practice', cellClass: practice() },
        { field: 'startDate', headerName: 'Start', width: 95, headerClass: 'hdr-practice', cellClass: practice() },
        {
          field: 'dueDate',
          headerName: 'Due',
          width: 95,
          headerClass: 'hdr-practice',
          cellClass: practice((p) => (p.data && isPastDue(p.data, today) ? 'cell-late' : '')),
        },
        {
          field: 'budgetHours',
          headerName: 'Budget',
          width: 80,
          type: 'rightAligned',
          valueFormatter: hoursFormatter,
          headerClass: 'hdr-practice',
          cellClass: practice(),
        },
        {
          field: 'actualHours',
          headerName: 'Actual',
          width: 80,
          type: 'rightAligned',
          valueFormatter: hoursFormatter,
          headerClass: 'hdr-practice',
          cellClass: practice((p) => (p.data && isOverBudget(p.data) ? 'cell-over' : '')),
        },
      ],
    };

    const editable = (p: { data: GridRow | undefined }) => p.data?.pinnedKind === undefined;

    const staffingGroup: ColGroupDef<GridRow> = {
      headerName: 'STAFFING (editable)',
      headerClass: 'hdr-staffing',
      children: [
        {
          colId: 'priority',
          headerName: 'Priority',
          valueGetter: (p) => (p.data?.pinnedKind ? '' : (p.data?.priority ?? '')),
          valueFormatter: (p) => priorityName.get(String(p.value)) ?? '',
          editable,
          cellEditor: 'agSelectCellEditor',
          cellEditorParams: { values: priorityValues },
          width: 125,
          headerClass: 'hdr-staffing',
          cellClass: staffing(),
        },
        {
          field: 'staffComments',
          headerName: 'Staff comments',
          editable,
          cellEditor: 'agTextCellEditor',
          cellEditorParams: { maxLength: MAX_COMMENT_LENGTH },
          width: 190,
          headerClass: 'hdr-staffing',
          cellClass: staffing(),
          tooltipField: 'staffComments',
        },
        {
          field: 'reviewerComments',
          headerName: 'Reviewer comments',
          editable,
          cellEditor: 'agTextCellEditor',
          cellEditorParams: { maxLength: MAX_COMMENT_LENGTH },
          width: 190,
          headerClass: 'hdr-staffing',
          cellClass: staffing(),
          tooltipField: 'reviewerComments',
        },
        {
          field: 'hoursToGo',
          headerName: 'Hours to go',
          editable,
          type: 'rightAligned',
          cellEditor: 'agNumberCellEditor',
          valueFormatter: hoursFormatter,
          width: 100,
          headerClass: 'hdr-staffing',
          cellClass: staffing((p) => (p.data && hoursToGoMismatch(p.data, weeks) ? 'cell-late' : '')),
          tooltipValueGetter: (p) =>
            p.data && !p.data.pinnedKind && hoursToGoMismatch(p.data, weeks)
              ? 'Hours to go is different from the hours planned in the weeks.'
              : undefined,
        },
      ],
    };

    const weekGroup: ColGroupDef<GridRow> = {
      headerName: `WEEKLY HOURS (editable, week ending Friday, MMDD, ${weeks.length} weeks)`,
      headerClass: 'hdr-weeks',
      children: weeks.map<ColDef<GridRow>>((week, index) => ({
        colId: week,
        headerName: `${weekNumberLabel(index)}\n${monthDay(week)}`,
        headerTooltip: `Week ending ${week}`,
        valueGetter: (p) => p.data?.hours[week] ?? null,
        valueFormatter: hoursFormatter,
        type: 'rightAligned',
        editable,
        cellEditor: 'agNumberCellEditor',
        width: index === 0 ? 92 : 72,
        headerClass: 'hdr-weeks hdr-week-col',
        cellClass: (p: CellClassParams<GridRow>) => {
          if (p.data?.pinnedKind === 'total') {
            return ['cell-total', `week-${weekTotalLevel(Number(p.value ?? 0), standardWeekHours)}`];
          }
          return p.data?.pinnedKind === 'standard' ? 'cell-standard' : 'cell-week';
        },
      })),
    };

    return [practiceGroup, staffingGroup, weekGroup];
  }, [rows.length, weeks, priorities, standardWeekHours, today]);

  const defaultColDef = useMemo<ColDef<GridRow>>(() => ({ resizable: true, sortable: false, suppressMovable: true }), []);

  function handleEditRequest(event: CellEditRequestEvent<GridRow>) {
    const columnId = event.column.getColId();
    if (event.data.pinnedKind !== undefined) return;
    onEdit({
      scheduleItemKey: event.data.scheduleItemKey,
      columnId,
      oldValue: event.oldValue,
      newValue: event.newValue,
    });
  }

  return (
    <div className="grid-wrap">
      <AgGridReact<GridRow>
        theme={theme}
        rowData={rows}
        pinnedBottomRowData={pinnedRows}
        columnDefs={columnDefs}
        defaultColDef={defaultColDef}
        getRowId={(params: GetRowIdParams<GridRow>) => String(params.data.scheduleItemKey)}
        headerHeight={48}
        readOnlyEdit
        onCellEditRequest={handleEditRequest}
        stopEditingWhenCellsLoseFocus
        singleClickEdit={false}
        tooltipShowDelay={400}
        suppressMovableColumns
      />
    </div>
  );
}