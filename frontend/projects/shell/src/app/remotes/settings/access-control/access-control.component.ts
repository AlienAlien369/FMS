import { Component } from '@angular/core';

@Component({
  selector: 'fms-access-control',
  template: `
    <div class="page-header">
      <h2>🔐 Access Control</h2>
    </div>
    <div class="card">
      <h3>User & Role Management</h3>
      <p>Manage users, roles, permissions, and dynamic RBAC configuration.</p>
    </div>
  `,
  styles: [`
    .page-header { margin-bottom: 1.5rem; }
    .page-header h2 { font-size: 1.5rem; font-weight: 700; color: #111827; }
    .card { background: white; padding: 1.5rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
  `]
})
export class AccessControlComponent {}
