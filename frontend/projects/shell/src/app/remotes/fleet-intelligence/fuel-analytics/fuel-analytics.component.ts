import { Component } from '@angular/core';

@Component({
  selector: 'fms-fuel-analytics',
  template: `
    <div class="page-header">
      <h2>⛽ Fuel & Energy Analytics</h2>
    </div>
    <div class="card">
      <h3>Fuel Consumption Overview</h3>
      <p>Fuel analytics across your fleet with theft detection, consumption trends, and efficiency scoring.</p>
      <div class="fuel-stats">
        <div class="fuel-stat">
          <div class="value">4,520 L</div>
          <div class="label">Total Fuel (This Month)</div>
        </div>
        <div class="fuel-stat">
          <div class="value">8.2 km/L</div>
          <div class="label">Avg Efficiency</div>
        </div>
        <div class="fuel-stat">
          <div class="value">₹3,84,200</div>
          <div class="label">Total Cost</div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .page-header { margin-bottom: 1.5rem; }
    .page-header h2 { font-size: 1.5rem; font-weight: 700; color: #111827; }
    .card { background: white; padding: 1.5rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
    .card h3 { margin: 0 0 0.5rem; }
    .fuel-stats { display: grid; grid-template-columns: repeat(3, 1fr); gap: 1rem; margin-top: 1.5rem; }
    .fuel-stat { text-align: center; padding: 1rem; background: #f9fafb; border-radius: 8px; }
    .fuel-stat .value { font-size: 1.5rem; font-weight: 700; color: #111827; }
    .fuel-stat .label { color: #6b7280; font-size: 0.875rem; margin-top: 0.25rem; }
  `]
})
export class FuelAnalyticsComponent {}
