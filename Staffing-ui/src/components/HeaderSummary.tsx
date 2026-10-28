import type { StaffingHeader } from '../api/types';
import { formatHours } from '../utils/format';

interface Props {
  header: StaffingHeader;
  weekCount: number;
}

export function HeaderSummary({ header, weekCount }: Props) {
  return (
    <section className="summary" aria-label="Staff summary">
      <div className="card card--wide">
        <span className="card__label">Staff</span>
        <span className="card__value">{header.staffName}</span>
        <span className="card__hint">{header.staffId}</span>
      </div>
      <div className="card">
        <span className="card__label">Items</span>
        <span className="card__value">{header.items}</span>
      </div>
      <div className="card">
        <span className="card__label">Total budget hours</span>
        <span className="card__value">{formatHours(header.totalBudgetHours)}</span>
      </div>
      <div className="card">
        <span className="card__label">Total planned hours</span>
        <span className="card__value">{formatHours(header.totalPlannedHours)}</span>
        <span className="card__hint">next {weekCount} weeks</span>
      </div>
    </section>
  );
}