import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    roleName: string;
    permissions: string[];
    tenantId: string;
    tenantName: string;
  };
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly API_URL = 'http://localhost:5000/api/v1';
  private currentUserSubject = new BehaviorSubject<LoginResponse['user'] | null>(null);
  currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {
    const stored = localStorage.getItem('fms_user');
    if (stored) {
      this.currentUserSubject.next(JSON.parse(stored));
    }
  }

  get currentUser(): LoginResponse['user'] | null {
    return this.currentUserSubject.value;
  }

  get isLoggedIn(): boolean {
    return !!localStorage.getItem('fms_access_token');
  }

  get token(): string | null {
    return localStorage.getItem('fms_access_token');
  }

  get tenantId(): string | null {
    return this.currentUser?.tenantId || null;
  }

  login(email: string, password: string, subdomain?: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.API_URL}/auth/login`, {
      email,
      password,
      tenantSubdomain: subdomain,
    }).pipe(
      tap(response => {
        localStorage.setItem('fms_access_token', response.accessToken);
        localStorage.setItem('fms_refresh_token', response.refreshToken);
        localStorage.setItem('fms_user', JSON.stringify(response.user));
        this.currentUserSubject.next(response.user);
      })
    );
  }

  logout(): void {
    localStorage.removeItem('fms_access_token');
    localStorage.removeItem('fms_refresh_token');
    localStorage.removeItem('fms_user');
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  hasPermission(permission: string): boolean {
    return this.currentUser?.permissions?.includes(permission) || false;
  }
}
