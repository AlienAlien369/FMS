import { Component, OnInit } from '@angular/core';
import { NavigationService, NavigationModule } from '../core/services/navigation.service';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'fms-shell',
  template: `
    <div class="shell-layout">
      <!-- Sidebar -->
      <aside class="sidebar" [class.collapsed]="sidebarCollapsed">
        <div class="sidebar-header">
          <div class="logo">
            <span class="logo-icon">🚚</span>
            <span class="logo-text" *ngIf="!sidebarCollapsed">FMS</span>
          </div>
          <button class="collapse-btn" (click)="sidebarCollapsed = !sidebarCollapsed">
            {{ sidebarCollapsed ? '→' : '←' }}
          </button>
        </div>

        <nav class="sidebar-nav">
          <div *ngFor="let module of modules" class="nav-section">
            <div class="nav-section-title" (click)="module.expanded = !module.expanded">
              <span class="section-icon">{{ module.icon }}</span>
              <span *ngIf="!sidebarCollapsed">{{ module.label }}</span>
              <span class="expand-icon" *ngIf="!sidebarCollapsed">{{ module.expanded ? '▼' : '▶' }}</span>
            </div>
            <div class="nav-items" *ngIf="module.expanded && !sidebarCollapsed">
              <a *ngFor="let item of module.items"
                 [routerLink]="item.route"
                 routerLinkActive="active"
                 class="nav-item">
                {{ item.label }}
              </a>
            </div>
          </div>
        </nav>

        <div class="sidebar-footer">
          <div class="tenant-info" *ngIf="!sidebarCollapsed">
            <span class="tenant-name">{{ tenantName }}</span>
            <span class="tenant-plan">{{ userRole }}</span>
          </div>
          <button class="logout-btn" (click)="logout()">Logout</button>
        </div>
      </aside>

      <!-- Main Content -->
      <main class="main-content">
        <header class="top-bar">
          <div class="page-title">Fleet Management Dashboard</div>
          <div class="top-bar-actions">
            <span class="user-name">{{ userName }}</span>
            <div class="notification-bell">🔔</div>
          </div>
        </header>

        <div class="content-area">
          <router-outlet></router-outlet>
        </div>
      </main>
    </div>
  `,
  styles: [`
    .shell-layout { display: flex; height: 100vh; overflow: hidden; }
    .sidebar { width: 260px; background: #1e293b; color: white; display: flex; flex-direction: column; transition: width 0.3s; }
    .sidebar.collapsed { width: 60px; }
    .sidebar-header { display: flex; justify-content: space-between; align-items: center; padding: 1rem; border-bottom: 1px solid #334155; }
    .logo { display: flex; align-items: center; gap: 0.5rem; }
    .logo-icon { font-size: 1.5rem; }
    .logo-text { font-size: 1.25rem; font-weight: 700; color: #60a5fa; }
    .collapse-btn { background: none; border: none; color: #94a3b8; cursor: pointer; font-size: 1rem; }
    .sidebar-nav { flex: 1; overflow-y: auto; padding: 0.5rem 0; }
    .nav-section { margin-bottom: 0.25rem; }
    .nav-section-title { display: flex; align-items: center; gap: 0.75rem; padding: 0.75rem 1rem; cursor: pointer; color: #94a3b8; font-weight: 500; }
    .nav-section-title:hover { background: #334155; color: white; }
    .section-icon { font-size: 1.25rem; }
    .expand-icon { margin-left: auto; font-size: 0.75rem; }
    .nav-item { display: block; padding: 0.5rem 1rem 0.5rem 2.5rem; color: #cbd5e1; text-decoration: none; font-size: 0.875rem; }
    .nav-item:hover { background: #334155; color: white; }
    .nav-item.active { background: #1e40af; color: white; border-right: 3px solid #60a5fa; }
    .sidebar-footer { padding: 1rem; border-top: 1px solid #334155; }
    .tenant-info { margin-bottom: 0.5rem; }
    .tenant-name { display: block; font-weight: 600; font-size: 0.875rem; }
    .tenant-plan { display: block; font-size: 0.75rem; color: #94a3b8; }
    .logout-btn { width: 100%; padding: 0.5rem; background: #dc2626; color: white; border: none; border-radius: 6px; cursor: pointer; font-size: 0.875rem; }
    .main-content { flex: 1; display: flex; flex-direction: column; overflow: hidden; }
    .top-bar { display: flex; justify-content: space-between; align-items: center; padding: 1rem 1.5rem; background: white; border-bottom: 1px solid #e5e7eb; }
    .page-title { font-size: 1.25rem; font-weight: 600; color: #111827; }
    .top-bar-actions { display: flex; align-items: center; gap: 1rem; }
    .user-name { color: #6b7280; font-size: 0.875rem; }
    .content-area { flex: 1; overflow-y: auto; padding: 1.5rem; background: #f3f4f6; }
  `]
})
export class ShellComponent implements OnInit {
  modules: (NavigationModule & { expanded?: boolean })[] = [];
  sidebarCollapsed = false;
  userName = '';
  tenantName = '';
  userRole = '';

  constructor(
    private navigationService: NavigationService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.navigationService.loadNavigation().subscribe(modules => {
      this.modules = modules.map(m => ({ ...m, expanded: true }));
    });

    const user = this.authService.currentUser;
    if (user) {
      this.userName = `${user.firstName || ''} ${user.lastName || ''}`.trim() || user.email;
      this.tenantName = user.tenantName;
      this.userRole = user.roleName || 'User';
    }
  }

  logout(): void {
    this.authService.logout();
  }
}
