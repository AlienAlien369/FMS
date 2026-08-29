import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'fms-login',
  template: `
    <div class="login-container">
      <div class="login-card">
        <div class="login-header">
          <h1>FMS</h1>
          <p>Fleet Management System</p>
        </div>
        <form (ngSubmit)="onLogin()">
          <div class="form-group">
            <label>Email</label>
            <input type="email" [(ngModel)]="email" name="email" placeholder="admin@acme-logistics.com" required />
          </div>
          <div class="form-group">
            <label>Password</label>
            <input type="password" [(ngModel)]="password" name="password" placeholder="Password" required />
          </div>
          <div class="form-group">
            <label>Company</label>
            <select [(ngModel)]="subdomain" name="subdomain">
              <option value="acme-logistics">Acme Logistics Corp</option>
              <option value="saferide-taxi">SafeRide Taxi Services</option>
              <option value="gulf-mining">Gulf Mining Group</option>
            </select>
          </div>
          <button type="submit" [disabled]="isLoading">
            {{ isLoading ? 'Signing in...' : 'Sign In' }}
          </button>
          <p class="error" *ngIf="error">{{ error }}</p>
        </form>
        <div class="demo-credentials">
          <strong>Demo Credentials:</strong>
          <p>Admin: admin&#64;acme-logistics.com / Admin&#64;123</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .login-container { display: flex; justify-content: center; align-items: center; min-height: 100vh; background: #f0f2f5; }
    .login-card { background: white; padding: 2rem; border-radius: 12px; box-shadow: 0 4px 24px rgba(0,0,0,0.1); width: 400px; }
    .login-header { text-align: center; margin-bottom: 2rem; }
    .login-header h1 { color: #1e40af; font-size: 2rem; margin: 0; }
    .login-header p { color: #6b7280; margin: 0.5rem 0 0; }
    .form-group { margin-bottom: 1rem; }
    .form-group label { display: block; margin-bottom: 0.5rem; font-weight: 500; color: #374151; }
    .form-group input, .form-group select { width: 100%; padding: 0.75rem; border: 1px solid #d1d5db; border-radius: 8px; font-size: 1rem; }
    .form-group input:focus, .form-group select:focus { outline: none; border-color: #1e40af; box-shadow: 0 0 0 3px rgba(30,64,175,0.1); }
    button { width: 100%; padding: 0.75rem; background: #1e40af; color: white; border: none; border-radius: 8px; font-size: 1rem; font-weight: 600; cursor: pointer; }
    button:hover { background: #1e3a8a; }
    button:disabled { opacity: 0.7; cursor: not-allowed; }
    .error { color: #dc2626; text-align: center; margin-top: 1rem; }
    .demo-credentials { margin-top: 1.5rem; padding: 1rem; background: #f9fafb; border-radius: 8px; font-size: 0.875rem; color: #6b7280; }
    .demo-credentials strong { color: #374151; }
  `]
})
export class LoginComponent {
  email = 'admin@acme-logistics.com';
  password = 'Admin@123';
  subdomain = 'acme-logistics';
  isLoading = false;
  error = '';

  constructor(private authService: AuthService, private router: Router) {}

  onLogin(): void {
    this.isLoading = true;
    this.error = '';
    this.authService.login(this.email, this.password, this.subdomain).subscribe({
      next: () => {
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.error = 'Invalid credentials. Try admin@acme-logistics.com / Admin@123';
        this.isLoading = false;
      },
    });
  }
}
