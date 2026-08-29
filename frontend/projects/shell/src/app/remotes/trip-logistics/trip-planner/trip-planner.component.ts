import { Component } from '@angular/core';

@Component({
  selector: 'fms-trip-planner',
  template: `
    <div class="page-header">
      <h2>📋 Trip Planner</h2>
      <button class="add-btn">+ Plan New Trip</button>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-value">12</div>
        <div class="stat-label">Planned Today</div>
      </div>
      <div class="stat-card active">
        <div class="stat-value">8</div>
        <div class="stat-label">In Progress</div>
      </div>
      <div class="stat-card success">
        <div class="stat-value">24</div>
        <div class="stat-label">Completed Today</div>
      </div>
    </div>

    <fms-dynamic-table
      pageId="trip-planner"
      [data]="trips">
    </fms-dynamic-table>
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    .page-header h2 { font-size: 1.5rem; font-weight: 700; color: #111827; }
    .add-btn { padding: 0.5rem 1rem; background: #1e40af; color: white; border: none; border-radius: 8px; cursor: pointer; }
    .stats-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 1rem; margin-bottom: 1.5rem; }
    .stat-card { background: white; padding: 1.25rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); border-left: 4px solid #1e40af; }
    .stat-card.active { border-left-color: #f59e0b; }
    .stat-card.success { border-left-color: #059669; }
    .stat-value { font-size: 2rem; font-weight: 700; color: #111827; }
    .stat-label { color: #6b7280; font-size: 0.875rem; }
  `]
})
export class TripPlannerComponent {
  trips = [
    { tripId: 'TRP-001', vehicle: 'MH-01-AB-1234', driver: 'Suresh Patil', origin: 'Mumbai', destination: 'Delhi', status: 'completed', eta: '14:00' },
    { tripId: 'TRP-002', vehicle: 'MH-02-CD-5678', driver: 'Vikram Singh', origin: 'Mumbai', destination: 'Pune', status: 'in-progress', eta: '18:00' },
    { tripId: 'TRP-003', vehicle: 'DL-01-IJ-7890', driver: 'Arun Desai', origin: 'Delhi', destination: 'Jaipur', status: 'in-progress', eta: '20:00' },
    { tripId: 'TRP-004', vehicle: 'MH-03-EF-9012', driver: 'Manoj Joshi', origin: 'Mumbai', destination: 'Nagpur', status: 'planned', eta: 'Tomorrow' },
  ];
}
