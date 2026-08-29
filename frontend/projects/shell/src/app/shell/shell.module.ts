import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ShellComponent } from './shell.component';
import { DynamicTableComponent } from '../shared/components/dynamic-table/dynamic-table.component';

@NgModule({
  declarations: [
    ShellComponent,
    DynamicTableComponent,
  ],
  imports: [
    CommonModule,
    FormsModule,
  ],
  exports: [
    ShellComponent,
    DynamicTableComponent,
  ],
})
export class ShellModule {}
