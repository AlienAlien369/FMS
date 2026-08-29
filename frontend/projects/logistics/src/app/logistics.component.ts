import { Component } from '@angular/core';

@Component({
  selector: 'fms-logistics-root',
  template: `
    <div class="logistics-container">
      <h2>📦 Trip & Logistics Module</h2>
      <p>Logistics operations: Trip Planner, Active Deliveries, Yard & Dock Manager</p>
    </div>
  `,
  styles: [`
    .logistics-container { padding: 2rem; }
    h2 { color: #1e40af; }
  `]
})
export class LogisticsComponent {}
