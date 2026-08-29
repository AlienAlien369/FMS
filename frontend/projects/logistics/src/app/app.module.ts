import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LogisticsRoutingModule } from './app-routing.module';
import { LogisticsComponent } from './logistics.component';

@NgModule({
  declarations: [LogisticsComponent],
  imports: [CommonModule, LogisticsRoutingModule],
  exports: [LogisticsComponent],
})
export class LogisticsModule {}
