import { Component } from '@angular/core';

@Component({
  selector: 'fms-maintenance-studio',
  template: `
    <div class="page-header">
      <h2>🔧 Maintenance Studio</h2>
      <button class="add-btn">+ Schedule Maintenance</button>
    </div>
    <div class="card">
      <h3>Scheduled Maintenance</h3>
      <p>Maintenance scheduling for vehicles, insurance, fitness, pollution, tax, and permits.</p>
      <div class="maintenance-list">
        <div class="maintenance-item">
          <span class="type">🛢️ Oil Change</span>
          <span class="vehicle">MH-01-AB-1234</span>
          <span class="due">Due in 500 km</span>
          <span class="status upcoming">Upcoming</span>
        </div>
        <div class="maintenance-item">
          <span class="type">📋 Insurance</span>
          <span class="vehicle">MH-02-CD-5678</span>
          <span class="due">Expires Sep 15, 2026</span>
          <span class="status warning">Warning</span>
        </div>
        <div class="maintenance-item">
          <span class="type">🛞 Tyre Replacement</span>
          <span class="vehicle">KA-01-GH-3456</span>
          <span class="due">Overdue by 200 km</span>
          <span class="status overdue">Overdue</span>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    .page-header h2 { font-size: 1.5rem; font-weight: 700; color: #111827; }
    .add-btn { padding: 0.5rem 1rem; background: #1e40af; color: white; border: none; border-radius: 8px; cursor: pointer; }
    .card { background: white; padding: 1.5rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
    .card h3 { margin: 0 0 1rem; }
    .maintenance-item { display: flex; gap: 1.5rem; align-items: center; padding: 1rem; border-bottom: 1px solid #f3f4f6; }
    .maintenance-item:last-child { border-bottom: none; }
    .type { font-weight: 600; min-width: 160px; }
    .vehicle { color: #6b7280; min-width: 150px; }
    .due { flex: 1; }
    .status { padding: 0.25rem 0.75rem; border-radius: 20px; font-size: 0.8rem; font-weight: 500; }
    .upcoming { background: #dbeafe; color: #1e40af; }
    .warning { background: #fef3c7; color: #92400e; }
    .overdue { background: #fee2e2; color: #991b1b; }
  `]
})
export class MaintenanceStudioComponent {}
