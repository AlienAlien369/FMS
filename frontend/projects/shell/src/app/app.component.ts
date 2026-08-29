import { Component } from '@angular/core';

@Component({
  selector: 'fms-root',
  template: `
    <router-outlet></router-outlet>
  `,
  styles: [],
})
export class AppComponent {
  title = 'FMS Fleet Management';
}
