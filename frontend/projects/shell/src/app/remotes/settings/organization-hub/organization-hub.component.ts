import { Component } from '@angular/core';

@Component({
  selector: 'fms-organization-hub',
  template: `
    <div class="page-header">
      <h2>🏢 Organization Hub</h2>
    </div>
    <div class="card">
      <h3>Company Settings</h3>
      <p>Manage company profile, regions, areas, zones, and organizational structure.</p>
    </div>
  `,
  styles: [`
    .page-header { margin-bottom: 1.5rem; }
    .page-header h2 { font-size: 1.5rem; font-weight: 700; color: #111827; }
    .card { background: white; padding: 1.5rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
  `]
})
export class OrganizationHubComponent {}
