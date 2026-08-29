import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface TableColumn {
  field: string;
  header: string;
  visible: boolean;
  width: number;
  order: number;
}

export interface TableConfig {
  columns: TableColumn[];
  pageSize: number;
  defaultSort?: { field: string; direction: string };
}

@Component({
  selector: 'fms-dynamic-table',
  template: `
    <div class="dynamic-table-container">
      <!-- Column Configuration Bar -->
      <div class="table-toolbar">
        <div class="search-box">
          <input type="text" placeholder="Search..." [(ngModel)]="searchTerm" (input)="onSearch()" />
        </div>
        <button class="config-btn" (click)="showColumnConfig = !showColumnConfig">
          ⚙️ Columns
        </button>
      </div>

      <!-- Column Config Panel -->
      <div class="column-config" *ngIf="showColumnConfig">
        <div *ngFor="let col of columns" class="column-toggle">
          <label>
            <input type="checkbox" [(ngModel)]="col.visible" (change)="onColumnToggle()" />
            {{ col.header }}
          </label>
        </div>
        <button class="save-config-btn" (click)="savePreferences()">Save Preferences</button>
      </div>

      <!-- Table -->
      <table class="data-table">
        <thead>
          <tr>
            <th *ngFor="let col of visibleColumns"
                [style.width.px]="col.width"
                (click)="onSort(col.field)"
                class="sortable">
              {{ col.header }}
              <span *ngIf="sortField === col.field" class="sort-indicator">
                {{ sortDirection === 'asc' ? '↑' : '↓' }}
              </span>
            </th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let row of displayedData" (click)="onRowClick(row)">
            <td *ngFor="let col of visibleColumns">
              {{ row[col.field] }}
            </td>
          </tr>
          <tr *ngIf="displayedData.length === 0">
            <td [attr.colspan]="visibleColumns.length" class="no-data">No data found</td>
          </tr>
        </tbody>
      </table>

      <!-- Pagination -->
      <div class="pagination">
        <span class="page-info">
          Showing {{ (currentPage - 1) * pageSize + 1 }}-{{ Math.min(currentPage * pageSize, totalItems) }}
          of {{ totalItems }}
        </span>
        <div class="page-controls">
          <button [disabled]="currentPage === 1" (click)="goToPage(1)">«</button>
          <button [disabled]="currentPage === 1" (click)="goToPage(currentPage - 1)">‹</button>
          <span class="page-number">{{ currentPage }} / {{ totalPages }}</span>
          <button [disabled]="currentPage === totalPages" (click)="goToPage(currentPage + 1)">›</button>
          <button [disabled]="currentPage === totalPages" (click)="goToPage(totalPages)">»</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dynamic-table-container { background: white; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); overflow: hidden; }
    .table-toolbar { display: flex; justify-content: space-between; align-items: center; padding: 1rem; border-bottom: 1px solid #e5e7eb; }
    .search-box input { padding: 0.5rem 1rem; border: 1px solid #d1d5db; border-radius: 6px; width: 300px; font-size: 0.875rem; }
    .config-btn { padding: 0.5rem 1rem; background: #f3f4f6; border: 1px solid #d1d5db; border-radius: 6px; cursor: pointer; font-size: 0.875rem; }
    .column-config { display: flex; flex-wrap: wrap; gap: 1rem; padding: 1rem; background: #f9fafb; border-bottom: 1px solid #e5e7eb; }
    .column-toggle label { display: flex; align-items: center; gap: 0.5rem; font-size: 0.875rem; cursor: pointer; }
    .save-config-btn { padding: 0.5rem 1rem; background: #1e40af; color: white; border: none; border-radius: 6px; cursor: pointer; }
    .data-table { width: 100%; border-collapse: collapse; }
    .data-table th { padding: 0.75rem 1rem; text-align: left; background: #f9fafb; font-weight: 600; font-size: 0.875rem; color: #374151; border-bottom: 2px solid #e5e7eb; }
    .data-table th.sortable { cursor: pointer; }
    .data-table th.sortable:hover { background: #f3f4f6; }
    .sort-indicator { margin-left: 0.25rem; color: #1e40af; }
    .data-table td { padding: 0.75rem 1rem; border-bottom: 1px solid #f3f4f6; font-size: 0.875rem; }
    .data-table tr:hover { background: #f9fafb; cursor: pointer; }
    .no-data { text-align: center; color: #9ca3af; padding: 2rem !important; }
    .pagination { display: flex; justify-content: space-between; align-items: center; padding: 1rem; border-top: 1px solid #e5e7eb; }
    .page-info { font-size: 0.875rem; color: #6b7280; }
    .page-controls { display: flex; gap: 0.25rem; align-items: center; }
    .page-controls button { padding: 0.25rem 0.75rem; border: 1px solid #d1d5db; background: white; border-radius: 4px; cursor: pointer; }
    .page-controls button:disabled { opacity: 0.5; cursor: not-allowed; }
    .page-number { padding: 0 0.5rem; font-size: 0.875rem; }
  `]
})
export class DynamicTableComponent implements OnInit {
  @Input() pageId = '';
  @Input() data: any[] = [];
  @Output() rowClicked = new EventEmitter<any>();

  Math = Math;
  columns: TableColumn[] = [];
  visibleColumns: TableColumn[] = [];
  displayedData: any[] = [];
  searchTerm = '';
  sortField = '';
  sortDirection: 'asc' | 'desc' = 'asc';
  currentPage = 1;
  pageSize = 25;
  totalItems = 0;
  totalPages = 0;
  showColumnConfig = false;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadPreferences();
  }

  private loadPreferences(): void {
    // Load saved column preferences from API
    const savedColumns = localStorage.getItem(`fms_table_${this.pageId}`);
    if (savedColumns) {
      this.columns = JSON.parse(savedColumns);
    } else {
      // Default columns based on data
      this.columns = Object.keys(this.data[0] || {}).map((key, i) => ({
        field: key,
        header: key.charAt(0).toUpperCase() + key.slice(1).replace(/([A-Z])/g, ' $1'),
        visible: true,
        width: 150,
        order: i + 1,
      }));
    }
    this.refreshVisibleColumns();
    this.refreshDisplayedData();
  }

  private refreshVisibleColumns(): void {
    this.visibleColumns = this.columns
      .filter(c => c.visible)
      .sort((a, b) => a.order - b.order);
  }

  private refreshDisplayedData(): void {
    let filtered = [...this.data];

    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(row =>
        Object.values(row).some(v => String(v).toLowerCase().includes(term))
      );
    }

    if (this.sortField) {
      filtered.sort((a, b) => {
        const valA = a[this.sortField];
        const valB = b[this.sortField];
        const cmp = String(valA).localeCompare(String(valB));
        return this.sortDirection === 'asc' ? cmp : -cmp;
      });
    }

    this.totalItems = filtered.length;
    this.totalPages = Math.ceil(this.totalItems / this.pageSize) || 1;
    this.currentPage = Math.min(this.currentPage, this.totalPages);

    this.displayedData = filtered.slice(
      (this.currentPage - 1) * this.pageSize,
      this.currentPage * this.pageSize
    );
  }

  onSearch(): void {
    this.currentPage = 1;
    this.refreshDisplayedData();
  }

  onSort(field: string): void {
    if (this.sortField === field) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortField = field;
      this.sortDirection = 'asc';
    }
    this.refreshDisplayedData();
  }

  onColumnToggle(): void {
    this.refreshVisibleColumns();
    this.savePreferences();
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.refreshDisplayedData();
  }

  onRowClick(row: any): void {
    this.rowClicked.emit(row);
  }

  savePreferences(): void {
    localStorage.setItem(`fms_table_${this.pageId}`, JSON.stringify(this.columns));
  }
}
