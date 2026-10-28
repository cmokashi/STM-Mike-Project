const hoursFormat = new Intl.NumberFormat('en-US', {
  minimumFractionDigits: 0,
  maximumFractionDigits: 2,
});

/** 613.75 -> "613.75", 6 -> "6", null -> "" */
export function formatHours(value: number | null | undefined): string {
  return value === null || value === undefined ? '' : hoursFormat.format(value);
}

const fixedTwo = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** 20 -> "20.00" (as in the spreadsheet: Budget Hours and the weekly hours show two decimals) */
export function formatFixed2(value: number | null | undefined): string {
  return value === null || value === undefined ? '' : fixedTwo.format(value);
}

/** Totals: 0 -> "-", 12.5 -> "12.5", 45 -> "45" (as in the spreadsheet's total rows) */
export function formatTotal(value: number | null | undefined): string {
  if (value === null || value === undefined) return '';
  return value === 0 ? '-' : hoursFormat.format(value);
}