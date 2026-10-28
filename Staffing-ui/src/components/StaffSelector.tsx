import type { StaffDto } from '../api/types';

interface Props {
  staff: StaffDto[];
  value: string;
  disabled?: boolean;
  onChange: (staffId: string) => void;
}

export function StaffSelector({ staff, value, disabled = false, onChange }: Props) {
  return (
    <div className="field">
      <label htmlFor="staff-select">Staff</label>
      <select
        id="staff-select"
        value={value}
        disabled={disabled}
        onChange={(event) => onChange(event.target.value)}
      >
        <option value="">Select a staff member...</option>
        {staff.map((person) => (
          <option key={person.staffId} value={person.staffId}>
            {person.staffName} ({person.staffId})
          </option>
        ))}
      </select>
    </div>
  );
}
