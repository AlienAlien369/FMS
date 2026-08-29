import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

export interface NavigationModule {
  key: string;
  label: string;
  icon: string;
  items: NavigationItem[];
}

export interface NavigationItem {
  key: string;
  label: string;
  route: string;
  icon?: string;
  requiredPermissions: string[];
}

@Injectable({ providedIn: 'root' })
export class NavigationService {
  private readonly API_URL = 'http://localhost:5000/api/v1';
  private modulesSubject = new BehaviorSubject<NavigationModule[]>([]);
  modules$ = this.modulesSubject.asObservable();

  constructor(private http: HttpClient) {}

  loadNavigation(): Observable<NavigationModule[]> {
    return this.http.get<{ modules: NavigationModule[] }>(`${this.API_URL}/config/navigation`).pipe(
      tap(response => this.modulesSubject.next(response.modules)),
      catchError(() => {
        // Fallback navigation when API is unavailable
        this.modulesSubject.next(this.getDefaultNavigation());
        return of(this.getDefaultNavigation());
      })
    );
  }

  private getDefaultNavigation(): NavigationModule[] {
    return [
      {
        key: 'command-center',
        label: 'Command Center',
        icon: 'dashboard',
        items: [
          { key: 'operations-overview', label: 'Operations Overview', route: '/command-center/operations', requiredPermissions: ['command-center:read'] },
          { key: 'live-fleet-map', label: 'Live Fleet Map', route: '/command-center/fleet-map', requiredPermissions: ['command-center:read'] },
          { key: 'active-alerts', label: 'Active Alerts Hub', route: '/command-center/alerts', requiredPermissions: ['command-center:read'] },
        ],
      },
      {
        key: 'fleet-intelligence',
        label: 'Fleet Intelligence',
        icon: 'directions_car',
        items: [
          { key: 'vehicle-directory', label: 'Vehicle Directory', route: '/fleet/vehicles', requiredPermissions: ['fleet-intelligence:read'] },
          { key: 'driver-hub', label: 'Driver Hub', route: '/fleet/drivers', requiredPermissions: ['fleet-intelligence:read'] },
          { key: 'maintenance-studio', label: 'Maintenance Studio', route: '/fleet/maintenance', requiredPermissions: ['fleet-intelligence:read'] },
          { key: 'fuel-analytics', label: 'Fuel & Energy Analytics', route: '/fleet/fuel', requiredPermissions: ['fleet-intelligence:read'] },
        ],
      },
      {
        key: 'trip-logistics',
        label: 'Trip & Logistics',
        icon: 'route',
        items: [
          { key: 'trip-planner', label: 'Trip Planner', route: '/logistics/trips', requiredPermissions: ['trip-logistics:read'] },
          { key: 'active-deliveries', label: 'Active Deliveries', route: '/logistics/deliveries', requiredPermissions: ['trip-logistics:read'] },
        ],
      },
      {
        key: 'safety-compliance',
        label: 'Safety & Compliance',
        icon: 'shield',
        items: [
          { key: 'video-telematics', label: 'Video Telematics', route: '/safety/video', requiredPermissions: ['safety-compliance:read'] },
          { key: 'incident-center', label: 'Incident Center', route: '/safety/incidents', requiredPermissions: ['safety-compliance:read'] },
        ],
      },
      {
        key: 'settings',
        label: 'Settings & Config',
        icon: 'settings',
        items: [
          { key: 'organization', label: 'Organization Hub', route: '/settings/organization', requiredPermissions: ['settings:read'] },
          { key: 'access-control', label: 'Access Control', route: '/settings/access', requiredPermissions: ['settings:read'] },
        ],
      },
      {
        key: 'device-iot',
        label: 'Device & IoT',
        icon: 'memory',
        items: [
          { key: 'device-fleet', label: 'Device Fleet', route: '/devices/fleet', requiredPermissions: ['device-iot:read'] },
          { key: 'device-lab', label: 'Device Lab', route: '/devices/lab', requiredPermissions: ['device-iot:read'] },
        ],
      },
    ];
  }
}
