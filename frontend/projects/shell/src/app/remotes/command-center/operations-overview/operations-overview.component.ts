import { Component } from '@angular/core';

@Component({
  selector: 'fms-operations-overview',
  template: `
    <div class="page-header">
      <h2>📊 Operations Overview</h2>
      <div class="time-filter">
        <button class="active">Today</button>
        <button>This Week</button>
        <button>This Month</button>
      </div>
    </div>

    <!-- KPI Cards -->
    <div class="kpi-grid">
      <div class="kpi-card">
        <div class="kpi-icon">🚛</div>
        <div class="kpi-content">
          <div class="kpi-value">5</div>
          <div class="kpi-label">Total Vehicles</div>
        </div>
      </div>
      <div class="kpi-card success">
        <div class="kpi-icon">✅</div>
        <div class="kpi-content">
          <div class="kpi-value">4</div>
          <div class="kpi-label">Active Now</div>
        </div>
      </div>
      <div class="kpi-card warning">
        <div class="kpi-icon">👥</div>
        <div class="kpi-content">
          <div class="kpi-value">7</div>
          <div class="kpi-label">Drivers On Duty</div>
        </div>
      </div>
      <div class="kpi-card danger">
        <div class="kpi-icon">⚠️</div>
        <div class="kpi-content">
          <div class="kpi-value">2</div>
          <div class="kpi-label">Active Alerts</div>
        </div>
      </div>
      <div class="kpi-card">
        <div class="kpi-icon">📍</div>
        <div class="kpi-content">
          <div class="kpi-value">9</div>
          <div class="kpi-label">Devices Online</div>
        </div>
      </div>
      <div class="kpi-card">
        <div class="kpi-icon">🔋</div>
        <div class="kpi-content">
          <div class="kpi-value">82%</div>
          <div class="kpi-label">Avg Battery</div>
        </div>
      </div>
    </div>

    <!-- Recent Activity -->
    <div class="content-grid">
      <div class="card">
        <h3>🔴 Active Alerts</h3>
        <div class="alert-list">
          <div class="alert-item critical">
            <span class="alert-dot"></span>
            <div>
              <strong>Truck-07 offline</strong>
              <p>Last seen: 2 hours ago • Teltonika FMC130</p>
            </div>
          </div>
          <div class="alert-item high">
            <span class="alert-dot"></span>
            <div>
              <strong>Low battery warning</strong>
              <p>Truck-15 • 15% battery • Weak signal</p>
            </div>
          </div>
        </div>
      </div>

      <div class="card">
        <h3>📋 Recent Trips</h3>
        <div class="trip-list">
          <div class="trip-item">
            <div class="trip-status completed">✓</div>
            <div>
              <strong>MH-01-AB-1234 → Delhi</strong>
              <p>Driver: Suresh Patil • Completed 2h ago</p>
            </div>
          </div>
          <div class="trip-item">
            <div class="trip-status in-progress">●</div>
            <div>
              <strong>MH-02-CD-5678 → Pune</strong>
              <p>Driver: Vikram Singh • In Progress • ETA 4h</p>
            </div>
          </div>
          <div class="trip-item">
            <div class="trip-status in-progress">●</div>
            <div>
              <strong>DL-01-IJ-7890 → Jaipur</strong>
              <p>Driver: Arun Desai • In Progress • ETA 6h</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    .page-header h2 { font-size: 1.5rem; font-weight: 700; color: #111827; }
    .time-filter { display: flex; gap: 0.5rem; }
    .time-filter button { padding: 0.5rem 1rem; border: 1px solid #d1d5db; background: white; border-radius: 6px; cursor: pointer; font-size: 0.875rem; }
    .time-filter button.active { background: #1e40af; color: white; border-color: #1e40af; }
    .kpi-grid { display: grid; grid-template-columns: repeat(6, 1fr); gap: 1rem; margin-bottom: 1.5rem; }
    .kpi-card { display: flex; align-items: center; gap: 1rem; background: white; padding: 1.25rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
    .kpi-card.success { border-top: 3px solid #059669; }
    .kpi-card.warning { border-top: 3px solid #f59e0b; }
    .kpi-card.danger { border-top: 3px solid #dc2626; }
    .kpi-icon { font-size: 2rem; }
    .kpi-value { font-size: 1.75rem; font-weight: 700; color: #111827; }
    .kpi-label { color: #6b7280; font-size: 0.8rem; }
    .content-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 1.5rem; }
    .card { background: white; border-radius: 8px; padding: 1.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
    .card h3 { margin: 0 0 1rem; font-size: 1rem; font-weight: 600; }
    .alert-item, .trip-item { display: flex; gap: 0.75rem; padding: 0.75rem; border-radius: 6px; margin-bottom: 0.5rem; }
    .alert-item.critical { background: #fef2f2; }
    .alert-item.high { background: #fff7ed; }
    .alert-dot { width: 8px; height: 8px; border-radius: 50%; margin-top: 0.5rem; flex-shrink: 0; }
    .alert-item.critical .alert-dot { background: #dc2626; }
    .alert-item.high .alert-dot { background: #f59e0b; }
    .alert-item strong, .trip-item strong { font-size: 0.875rem; }
    .alert-item p, .trip-item p { margin: 0.25rem 0 0; font-size: 0.75rem; color: #6b7280; }
    .trip-status { width: 24px; height: 24px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 0.75rem; flex-shrink: 0; }
    .trip-status.completed { background: #d1fae5; color: #059669; }
    .trip-status.in-progress { background: #dbeafe; color: #1e40af; }
  `]
})
export class OperationsOverviewComponent {}
