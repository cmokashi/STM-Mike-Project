import { useCallback, useState } from 'react';
import { getPriorities, getScreen, getStaff } from './api/staffingApi';
import { ScheduleView } from './components/ScheduleView';
import { StaffSelector } from './components/StaffSelector';
import { useApiResource } from './hooks/useApiResource';
import './App.css';

const WEEK_COUNT = 26;

export default function App() {
  const [selectedStaffId, setSelectedStaffId] = useState('');

  const staffList = useApiResource('staff', getStaff);
  const priorities = useApiResource('priorities', getPriorities);

  const fetchScreen = useCallback(
    (signal: AbortSignal) => getScreen(selectedStaffId, WEEK_COUNT, signal),
    [selectedStaffId],
  );
  const screen = useApiResource(selectedStaffId === '' ? null : selectedStaffId, fetchScreen);

  return (
    <div className="app">
      <header className="app__header">
        <h1>Staffing Time Management</h1>
        <StaffSelector
          staff={staffList.data ?? []}
          value={selectedStaffId}
          disabled={staffList.loading || staffList.error !== null}
          onChange={setSelectedStaffId}
        />
      </header>

      <main aria-busy={staffList.loading || screen.loading}>
        {staffList.loading && <p className="status">Loading staff...</p>}

        {staffList.error !== null && (
          <div className="banner banner--error" role="alert">
            <span>{staffList.error}</span>
            <button type="button" onClick={staffList.reload}>Retry</button>
          </div>
        )}

        {selectedStaffId === '' && staffList.data !== null && (
          <p className="status">Select a staff member to see their schedule.</p>
        )}

        {screen.loading && <p className="status">Loading schedule...</p>}

        {screen.error !== null && (
          <div className="banner banner--error" role="alert">
            <span>{screen.error}</span>
            <button type="button" onClick={screen.reload}>Retry</button>
          </div>
        )}

        {priorities.error !== null && (
          <div className="banner banner--error" role="alert">
            <span>Priorities could not be loaded. {priorities.error}</span>
            <button type="button" onClick={priorities.reload}>Retry</button>
          </div>
        )}

        {screen.data !== null && (
          <ScheduleView
            key={screen.data.header.staffId}
            screen={screen.data}
            priorities={priorities.data ?? []}
          />
        )}
      </main>
    </div>
  );
}