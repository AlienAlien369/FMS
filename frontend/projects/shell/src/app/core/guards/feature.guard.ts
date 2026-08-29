import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({ providedIn: 'root' })
export class FeatureGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const requiredPermission = route.data['permission'] as string;

    if (!requiredPermission) {
      return true;
    }

    if (this.authService.hasPermission(requiredPermission)) {
      return true;
    }

    this.router.navigate(['/command-center/operations']);
    return false;
  }
}
