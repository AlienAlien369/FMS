import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'fms-vehicle-directory',
  template: `
    <div class="page-header">
      <h2>🚛 Vehicle Directory</h2>
      <button class="add-btn" (click)="showAddModal = true">+ Add Vehicle</button>
    </div>

    <!-- Stats Cards -->
    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-value">{{ totalVehicles }}</div>
        <div class="stat-label">Total Vehicles</div>
      </div>
      <div class="stat-card active">
        <div class="stat-value">{{ activeVehicles }}</div>
        <div class="stat-label">Active</div>
      </div>
      <div class="stat-card warning">
        <div class="stat-value">{{ maintenanceVehicles }}</div>
        <div class="stat-label">Maintenance</div>
      </div>
      <div class="stat-card danger">
        <div class="stat-value">{{ offlineVehicles }}</div>
        <div class="stat-label">Offline</div>
      </div>
    </div>

    <!-- Dynamic Table -->
    <fms-dynamic-table
      pageId="vehicle-directory"
      [data]="vehicles"
      (rowClicked)="onVehicleClick($event)">
    </fms-dynamic-table>
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    .page-header h2 { font-size: 1.5rem; font-weight: 700; color: #111827; }
    .add-btn { padding: 0.5rem 1rem; background: #1e40af; color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500; }
    .add-btn:hover { background: #1e3a8a; }
    .stats-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 1rem; margin-bottom: 1.5rem; }
    .stat-card { background: white; padding: 1.25rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); border-left: 4px solid #1e40af; }
    .stat-card.active { border-left-color: #059669; }
    .stat-card.warning { border-left-color: #f59e0b; }
    .stat-card.danger { border-left-color: #dc2626; }
    .stat-value { font-size: 2rem; font-weight: 700; color: #111827; }
    .stat-label { color: #6b7280; font-size: 0.875rem; margin-top: 0.25rem; }
  `]
})
export class VehicleDirectoryComponent implements OnInit {
  vehicles: any[] = [];
  totalVehicles = 0;
  activeVehicles = 0;
  maintenanceVehicles = 0;
  offlineVehicles = 0;
  showAddModal = false;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadVehicles();
  }

  loadVehicles(): void {
    this.http.get<any>('http://localhost:5000/api/v1/fleet/vehicles').subscribe({
      next: (response) => {
        this.vehicles = response.items || [];
        this.totalVehicles = response.totalCount || this.vehicles.length;
        this.activeVehicles = this.vehicles.filter(v => v.status === 'active').length;
        this.maintenanceVehicles = this.vehicles.filter(v => v.status === 'maintenance').length;
        this.offlineVehicles = this.vehicles.filter(v => v.status === 'offline').length;
      },
      error: () => {
        // Sample data fallback
        this.vehicles = [
          { vehicleNumber: 'MH-01-AB-1234', type: 'Truck', model: 'Tata Prima 2525.K', status: 'active', fuelType: 'Diesel', year: 2024 },
          { vehicleNumber: 'MH-02-CD-5678', type: 'Truck', model: 'Ashok Leyland Viking', status: 'active', fuelType: 'Diesel', year: 2023 },
          { vehicleNumber: 'MH-03-EF-9012', type: 'Container', model: 'BharatBenz 2823C', status: 'active', fuelType: 'Diesel', year: 2024 },
          { vehicleNumber: 'KA-01-GH-3456', type: 'Tanker', model: 'Tata Signa 3118T', status: 'maintenance', fuelType: 'Diesel', year: 2023 },
          { vehicleNumber: 'DL-01-IJ-7890', type: 'Refrigerated', model: 'Eicher Pro 2110', status: 'active', fuelType: 'Diesel', year: 2024 },
        ];
        this.totalVehicles = 5;
        this.activeVehicles = 4;
        this.maintenanceVehicles = 1;
        this.offlineVehicles = 0;
      }
    });
  }

  onVehicleClick(vehicle: any): void {
    console.log('Vehicle clicked:', vehicle);
  }
}
