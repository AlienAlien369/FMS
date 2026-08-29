import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ShellComponent } from './shell/shell.component';
import { LoginComponent } from './core/pages/login/login.component';
import { AuthGuard } from './core/guards/auth.guard';
import { DynamicRouteLoaderComponent } from './shared/components/dynamic-route-loader/dynamic-route-loader.component';

const routes: Routes = [
  { path: 'login', component: LoginComponent },
  {
    path: '',
    component: ShellComponent,
    canActivate: [AuthGuard],
    children: [
      { path: '', redirectTo: 'command-center/operations', pathMatch: 'full' },
      // Command Center
      {
        path: 'command-center/operations',
        loadComponent: () =>
          import('./remotes/command-center/operations-overview/operations-overview.component')
            .then(m => m.OperationsOverviewComponent),
      },
      {
        path: 'command-center/fleet-map',
        loadComponent: () =>
          import('./remotes/command-center/live-fleet-map/live-fleet-map.component')
            .then(m => m.LiveFleetMapComponent),
      },
      // Fleet Intelligence
      {
        path: 'fleet/vehicles',
        loadComponent: () =>
          import('./remotes/fleet-intelligence/vehicle-directory/vehicle-directory.component')
            .then(m => m.VehicleDirectoryComponent),
      },
      {
        path: 'fleet/drivers',
        loadComponent: () =>
          import('./remotes/fleet-intelligence/driver-hub/driver-hub.component')
            .then(m => m.DriverHubComponent),
      },
      {
        path: 'fleet/maintenance',
        loadComponent: () =>
          import('./remotes/fleet-intelligence/maintenance-studio/maintenance-studio.component')
            .then(m => m.MaintenanceStudioComponent),
      },
      {
        path: 'fleet/fuel',
        loadComponent: () =>
          import('./remotes/fleet-intelligence/fuel-analytics/fuel-analytics.component')
            .then(m => m.FuelAnalyticsComponent),
      },
      // Trip & Logistics
      {
        path: 'logistics/trips',
        loadComponent: () =>
          import('./remotes/trip-logistics/trip-planner/trip-planner.component')
            .then(m => m.TripPlannerComponent),
      },
      // Safety & Compliance
      {
        path: 'safety/video',
        loadComponent: () =>
          import('./remotes/safety-compliance/video-telematics/video-telematics.component')
            .then(m => m.VideoTelematicsComponent),
      },
      // Settings
      {
        path: 'settings/organization',
        loadComponent: () =>
          import('./remotes/settings/organization-hub/organization-hub.component')
            .then(m => m.OrganizationHubComponent),
      },
      {
        path: 'settings/access',
        loadComponent: () =>
          import('./remotes/settings/access-control/access-control.component')
            .then(m => m.AccessControlComponent),
      },
      // Device & IoT
      {
        path: 'devices/fleet',
        loadComponent: () =>
          import('./remotes/device-iot/device-fleet/device-fleet.component')
            .then(m => m.DeviceFleetComponent),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
