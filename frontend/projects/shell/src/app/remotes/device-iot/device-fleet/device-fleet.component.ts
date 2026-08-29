import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'fms-device-fleet',
  template: `
    <div class="page-header">
      <h2>📡 Device Fleet</h2>
      <button class="add-btn">+ Add Device</button>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-value">{{ totalDevices }}</div>
        <div class="stat-label">Total Devices</div>
      </div>
      <div class="stat-card success">
        <div class="stat-value">{{ onlineDevices }}</div>
        <div class="stat-label">Online</div>
      </div>
      <div class="stat-card danger">
        <div class="stat-value">{{ offlineDevices }}</div>
        <div class="stat-label">Offline</div>
      </div>
    </div>

    <!-- Vendor Breakdown -->
    <div class="vendor-grid">
      <div class="vendor-card" *ngFor="let vendor of vendorStats">
        <div class="vendor-header">
          <strong>{{ vendor.name }}</strong>
          <span class="vendor-count">{{ vendor.count }} devices</span>
        </div>
        <div class="vendor-status">
          <span class="online">{{ vendor.online }} online</span>
          <span class="offline" *ngIf="vendor.offline > 0">{{ vendor.offline }} offline</span>
        </div>
        <div class="vendor-bar">
          <div class="bar-fill" [style.width.%]="(vendor.online / vendor.count) * 100"></div>
        </div>
      </div>
    </div>

    <fms-dynamic-table
      pageId="device-fleet"
      [data]="devices">
    </fms-dynamic-table>
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    .page-header h2 { font-size: 1.5rem; font-weight: 700; color: #111827; }
    .add-btn { padding: 0.5rem 1rem; background: #1e40af; color: white; border: none; border-radius: 8px; cursor: pointer; }
    .stats-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 1rem; margin-bottom: 1.5rem; }
    .stat-card { background: white; padding: 1.25rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); border-left: 4px solid #1e40af; }
    .stat-card.success { border-left-color: #059669; }
    .stat-card.danger { border-left-color: #dc2626; }
    .stat-value { font-size: 2rem; font-weight: 700; color: #111827; }
    .stat-label { color: #6b7280; font-size: 0.875rem; }
    .vendor-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 1rem; margin-bottom: 1.5rem; }
    .vendor-card { background: white; padding: 1.25rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
    .vendor-header { display: flex; justify-content: space-between; margin-bottom: 0.5rem; }
    .vendor-count { color: #6b7280; font-size: 0.875rem; }
    .vendor-status { display: flex; gap: 1rem; margin-bottom: 0.5rem; font-size: 0.8rem; }
    .online { color: #059669; }
    .offline { color: #dc2626; }
    .vendor-bar { height: 6px; background: #e5e7eb; border-radius: 3px; overflow: hidden; }
    .bar-fill { height: 100%; background: #059669; border-radius: 3px; transition: width 0.5s; }
  `]
})
export class DeviceFleetComponent implements OnInit {
  devices: any[] = [];
  totalDevices = 0;
  onlineDevices = 0;
  offlineDevices = 0;

  vendorStats = [
    { name: 'iTriangle', count: 3, online: 2, offline: 1 },
    { name: 'Streamax', count: 1, online: 1, offline: 0 },
    { name: 'Teltonika', count: 1, online: 0, offline: 1 },
  ];

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadDevices();
  }

  loadDevices(): void {
    this.http.get<any>('http://localhost:5000/api/v1/fleet/devices').subscribe({
      next: (response) => {
        this.devices = (response.items || []).map((d: any) => ({
          imei: d.imei,
          model: d.model,
          status: d.status,
          lastSeen: d.lastSeen,
          signalStrength: d.signalStrength,
          batteryLevel: d.batteryLevel,
        }));
        this.totalDevices = response.totalCount || this.devices.length;
        this.onlineDevices = this.devices.filter(d => d.status === 'active').length;
        this.offlineDevices = this.devices.filter(d => d.status === 'offline').length;
      },
      error: () => {
        this.devices = [
          { imei: '867959033200001', model: 'iTriangle VT300', status: 'active', batteryLevel: 87, signalStrength: -75 },
          { imei: '867959033200002', model: 'iTriangle VT300', status: 'active', batteryLevel: 92, signalStrength: -82 },
          { imei: '863456012300001', model: 'Streamax X1', status: 'active', batteryLevel: 100, signalStrength: -68 },
          { imei: '352093081200001', model: 'Teltonika FMC130', status: 'offline', batteryLevel: 45, signalStrength: null },
          { imei: '867959033200003', model: 'iTriangle VT300', status: 'active', batteryLevel: 78, signalStrength: -71 },
        ];
        this.totalDevices = 5;
        this.onlineDevices = 4;
        this.offlineDevices = 1;
      }
    });
  }
}
