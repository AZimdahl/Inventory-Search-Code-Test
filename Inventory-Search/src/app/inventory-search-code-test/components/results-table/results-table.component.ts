//components/results-table/results-table.component.ts

// TypeScript
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, Output } from '@angular/core';
import { InventoryItem, PeakAvailability } from '../../models/inventory-search.models';
import { InventorySearchApiService } from '../../services/inventory-search-api.service';
import { PageEvent } from '@angular/material/paginator';


@Component({
  selector: 'inventory-results-table',
  templateUrl: './results-table.component.html',
  styleUrls: ['./results-table.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class ResultsTableComponent {
  @Input() items: InventoryItem[] | null = [];
  @Input() total = 0;
  @Input() pageSize = 20;

  @Output() sort = new EventEmitter<keyof InventoryItem>();
  @Output() pageChange = new EventEmitter<number>();

  pageIndex = 0;
  expanded: Record<string, boolean> = {};
  // Added: keep per-part peak availability and loading state
  peakLoading: Record<string, boolean> = {};
  peakByPart: Record<string, PeakAvailability | null> = {};
  // Simple inline error message
  errorMessage: string | null = null;

  headers: Array<{ label: string; field: keyof InventoryItem }> = [
    { label: 'Part Number', field: 'partNumber' },
    { label: 'Supplier SKU', field: 'supplierSku' },
    { label: 'Description', field: 'description' },
    { label: 'Branch', field: 'branch' },
    { label: 'Available', field: 'availableQty' },
    { label: 'UOM', field: 'uom' },
    { label: 'Lead Time (days)', field: 'leadTimeDays' },
    { label: 'Last Purchase', field: 'lastPurchaseDate' },
  ];

  constructor(
    private readonly api: InventorySearchApiService,
    private readonly cdr: ChangeDetectorRef
  ) { }

  onHeaderClick(field: keyof InventoryItem) {
    // The data to be sorted on the column
    this.pageIndex = 0; // reset to first page on sort
    this.sort.emit(field);
  }

  toggleExpand(item: InventoryItem) {
    // Toggle the expanded state of a row
    const key = item.partNumber;
    this.expanded = { ...this.expanded, [key]: !this.expanded[key] }; // immutable update
    
    // if all are collapsed, reset this.expanded for header icon state
    if (Object.values(this.expanded).every(v => !v)) {
      this.expanded = {};
    }

    this.cdr.markForCheck();
  }

  collapseAll() {
    // Collapse all expanded rows
    this.expanded = {};
    this.cdr.markForCheck();
  }

  // Fetch peak availability for a given item/part
  fetchPeakAvailability(item: InventoryItem) {
    // Needs to call the API to get the peak availability for the part
    // on an error
    // Failed to load peak availability along with any error returned by the API
    const key = item.partNumber;

    // Set loading state with immutable update
    this.peakLoading = { ...this.peakLoading, [key]: true };
    this.errorMessage = null;
    this.cdr.markForCheck();

    this.api.getPeakAvailability(item.partNumber).subscribe({
      next: (response) => {
        this.peakLoading = { ...this.peakLoading, [key]: false };
        this.handlePeakResponse(response, key);
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.peakLoading = { ...this.peakLoading, [key]: false };
        this.handlePeakError(err, key);
        this.cdr.markForCheck();
      }
    });
  }

  // Convenience: fetch and expand inline panel
  onPeakButton(item: InventoryItem) {
    const key = item.partNumber;

    // Expand the row if not expanded
    if (!this.expanded[key]) {
      this.expanded[key] = true;
    }

    // Fetch peak availability if not already loaded
    if (!this.peakByPart[key] && !this.peakLoading[key]) {
      this.fetchPeakAvailability(item);
    }
  }

  totalPages(total: number, size: number) {
    return Math.max(1, Math.ceil((total ?? 0) / (size || 1)));
  }

  handlePageEvent(event: PageEvent) {
    this.goTo(event.pageIndex);
  }

  goTo(page: number) {
    // go to specific page
    this.pageIndex = page;
    this.pageChange.emit(page);
  }

  private handlePeakResponse(response: any, partNumber: string): void {
    this.errorMessage = null;
    if (response.isFailed) this.handlePeakError(response, partNumber);
    else if (response.data) {
      this.peakByPart = { ...this.peakByPart, [partNumber]: response.data };
    }
  }

  private handlePeakError(err: any, partNumber: string): void {
    this.errorMessage = `Failed to load peak availability: ${err?.message || 'Unknown error'}`;
    this.peakByPart = { ...this.peakByPart, [partNumber]: null };
  }
}
