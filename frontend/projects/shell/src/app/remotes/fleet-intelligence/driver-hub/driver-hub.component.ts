import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'fms-driver-hub',
  template: `
    <div class="page-header">
      <h2>👥 Driver Hub</h2>
      <button class="add-btn">+ Add Driver</button>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-value">{{ totalDrivers }}</div>
        <div class="stat-label">Total Drivers</div>
      </div>
      <div class="stat-card active">
        <div class="stat-value">{{ activeDrivers }}</div>
        <div class="stat-label">Active</div>
      </div>
      <div class="stat-card warning">
        <div class="stat-value">{{ avgScore | number:'1.1-1' }}</div>
        <div class="stat-label">Avg Behavior Score</div>
      </div>
    </div>

    <fms-dynamic-table
      pageId="driver-hub"
      [data]="drivers"
      (rowClicked)="onDriverClick($event)">
    </fms-dynamic-table>
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    .page-header h2 { font-size: 1.5rem; font-weight: 700; color: #111827; }
    .add-btn { padding: 0.5rem 1rem; background: #1e40af; color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500; }
    .stats-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 1rem; margin-bottom: 1.5rem; }
    .stat-card { background: white; padding: 1.25rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); border-left: 4px solid #1e40af; }
    .stat-card.active { border-left-color: #059669; }
    .stat-card.warning { border-left-color: #f59e0b; }
    .stat-value { font-size: 2rem; font-weight: 700; color: #111827; }
    .stat-label { color: #6b7280; font-size: 0.875rem; margin-top: 0.25rem; }
  `]
})
export class DriverHubComponent implements OnInit {
  drivers: any[] = [];
  totalDrivers = 0;
  activeDrivers = 0;
  avgScore = 0;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadDrivers();
  }

  loadDrivers(): void {
    this.http.get<any>('http://localhost:5000/api/v1/fleet/drivers').subscribe({
      next: (response) => {
        this.drivers = (response.items || []).map((d: any) => ({
          name: `${d.firstName || ''} ${d.lastName || ''}`.trim(),
          licenseNumber: d.licenseNumber,
          behaviorScore: d.behaviorScore,
          phone: d.phone,
          status: d.status,
          licenseExpiry: d.licenseExpiry,
        }));
        this.totalDrivers = response.totalCount || this.drivers.length;
        this.activeDrivers = this.drivers.filter(d => d.status === 'active').length;
        this.avgScore = this.drivers.reduce((sum, d) => sum + (d.behaviorScore || 0), 0) / (this.drivers.length || 1);
      },
      error: () => {
        this.drivers = [
          { name: 'Suresh Patil', licenseNumber: 'MH-DRV-001', behaviorScore: 87.5, phone: '+91-9876543210', status: 'active' },
          { name: 'Vikram Singh', licenseNumber: 'MH-DRV-002', behaviorScore: 92.3, phone: '+91-9876543211', status: 'active' },
          { name: 'Manoj Joshi', licenseNumber: 'KA-DRV-001', behaviorScore: 78.1, phone: '+91-9876543212', status: 'active' },
          { name: 'Arun Desai', licenseNumber: 'DL-DRV-001', behaviorScore: 95.0, phone: '+91-9876543213', status: 'active' },
        ];
        this.totalDrivers = 4;
        this.activeDrivers = 4;
        this.avgScore = 88.2;
      }
    });
  }

  onDriverClick(driver: any): void {
    console.log('Driver clicked:', driver);
  }
}
