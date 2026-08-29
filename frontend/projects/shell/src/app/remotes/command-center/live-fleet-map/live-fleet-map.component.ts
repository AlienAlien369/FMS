import { Component, OnInit, AfterViewInit } from '@angular/core';

@Component({
  selector: 'fms-live-fleet-map',
  template: `
    <div class="page-header">
      <h2>🗺️ Live Fleet Map</h2>
      <div class="map-controls">
        <button [class.active]="showAllVehicles" (click)="showAllVehicles = true">All</button>
        <button [class.active]="!showAllVehicles" (click)="showAllVehicles = false">Moving Only</button>
        <span class="vehicle-count">{{ vehicles.length }} vehicles</span>
      </div>
    </div>

    <div class="map-container">
      <div id="fleet-map" class="map"></div>

      <!-- Vehicle Legend -->
      <div class="map-legend">
        <h4>Fleet Status</h4>
        <div class="legend-item">
          <span class="legend-dot" style="background: #059669"></span> Active (4)
        </div>
        <div class="legend-item">
          <span class="legend-dot" style="background: #f59e0b"></span> Maintenance (1)
        </div>
        <div class="legend-item">
          <span class="legend-dot" style="background: #dc2626"></span> Offline (0)
        </div>
      </div>
    </div>
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .page-header h2 { font-size: 1.5rem; font-weight: 700; color: #111827; }
    .map-controls { display: flex; gap: 0.5rem; align-items: center; }
    .map-controls button { padding: 0.5rem 1rem; border: 1px solid #d1d5db; background: white; border-radius: 6px; cursor: pointer; }
    .map-controls button.active { background: #1e40af; color: white; border-color: #1e40af; }
    .vehicle-count { margin-left: 0.5rem; color: #6b7280; font-size: 0.875rem; }
    .map-container { position: relative; height: calc(100vh - 200px); border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
    .map { width: 100%; height: 100%; background: #e5e7eb; display: flex; align-items: center; justify-content: center; font-size: 1.5rem; color: #9ca3af; }
    .map-legend { position: absolute; top: 1rem; right: 1rem; background: white; padding: 1rem; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.15); }
    .map-legend h4 { margin: 0 0 0.5rem; font-size: 0.875rem; }
    .legend-item { display: flex; align-items: center; gap: 0.5rem; font-size: 0.8rem; margin-bottom: 0.25rem; }
    .legend-dot { width: 10px; height: 10px; border-radius: 50%; }
  `]
})
export class LiveFleetMapComponent implements OnInit {
  showAllVehicles = true;
  vehicles = [
    { id: '1', number: 'MH-01-AB-1234', lat: 19.0760, lng: 72.8777, status: 'active', speed: 65 },
    { id: '2', number: 'MH-02-CD-5678', lat: 18.5204, lng: 73.8567, status: 'active', speed: 0 },
    { id: '3', number: 'MH-03-EF-9012', lat: 19.8762, lng: 75.3433, status: 'active', speed: 42 },
    { id: '4', number: 'KA-01-GH-3456', lat: 12.9716, lng: 77.5946, status: 'maintenance', speed: 0 },
    { id: '5', number: 'DL-01-IJ-7890', lat: 28.7041, lng: 77.1025, status: 'active', speed: 80 },
  ];

  ngOnInit(): void {
    // In production, this would initialize Leaflet.js map
    // For now, showing placeholder
  }
}
