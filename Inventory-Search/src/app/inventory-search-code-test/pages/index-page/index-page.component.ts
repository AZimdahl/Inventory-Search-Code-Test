// pages/index-page/index-page.component.ts

// TypeScript
import { ChangeDetectionStrategy, Component, OnDestroy, InjectionToken, Inject, OnInit, Optional, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BehaviorSubject, merge, Subject } from 'rxjs';
import { debounceTime, filter, finalize, switchMap, takeUntil, tap } from 'rxjs/operators';
import { InventoryItem, InventoryItemSortableFields, InventorySearchQuery, SearchBy, } from '../../models/inventory-search.models';
import { InventorySearchApiService } from '../../services/inventory-search-api.service';
import { ResultsTableComponent } from '../../components/results-table/results-table.component';

type SortDir = 'asc' | 'desc';
interface SortState { field: keyof InventoryItem | ''; direction: SortDir; }

// Configurable debounce for searches (defaults to 50ms)
export const INVENTORY_SEARCH_DEBOUNCE_MS = new InjectionToken<number>('INVENTORY_SEARCH_DEBOUNCE_MS');

@Component({
  selector: 'inv-index-page',
  templateUrl: './index-page.component.html',
  styleUrls: ['./index-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class IndexPageComponent implements OnDestroy, OnInit {
  @ViewChild(ResultsTableComponent) resultsTable?: ResultsTableComponent; // access to child component methods to collapse rows on new search/page/sort

  private _debounce = 50; // default 50ms
  private destroy$ = new Subject<void>();
  private searchTrigger$ = new Subject<void>();
  private sortChange$ = new Subject<SortState>();
  private pageChange$ = new Subject<number>();

  form: FormGroup;
  pageSize = 20;
  currentPage = 0;
  currentSort: SortState = { field: '', direction: 'asc' };

  total$ = new BehaviorSubject<number>(0);
  items$ = new BehaviorSubject<InventoryItem[]>([]);
  loading$ = new BehaviorSubject<boolean>(false);
  errorMessage: string | null = null;

  constructor(
    private readonly fb: FormBuilder,
    private readonly api: InventorySearchApiService,
    @Inject(INVENTORY_SEARCH_DEBOUNCE_MS) @Optional() debounceMs: number | null
  ) {
    if (typeof debounceMs === 'number') {
      this._debounce = debounceMs;
    }
    this.form = this.fb.group({
      criteria: ['', Validators.required],
      by: ['PartNumber' as SearchBy, Validators.required],
      branches: [[] as string[]],
      onlyAvailable: [false],
    });
  }

  ngOnInit(): void {
    // Compose the reactive search pipeline
    // Merge all three input streams: manual search, sort changes, and page changes
    merge(
      this.searchTrigger$.pipe(
        tap(() => {
          this.currentPage = 0; // Reset page when new search is triggered
          this.resultsTable?.collapseAll(); // Collapse all expanded rows
        })
      ),
      this.pageChange$.pipe(
        tap((pageIndex) => {
          this.currentPage = pageIndex; // Update current page
          this.resultsTable?.collapseAll(); // Collapse all expanded rows
        })
      ),
      this.sortChange$.pipe(
        tap((sortState) => {
          // Update sort state and reset page
          this.currentSort = sortState;
          this.currentPage = 0;
          this.resultsTable?.collapseAll(); // Collapse all expanded rows
        })
      ),
    )
      .pipe(
        debounceTime(this._debounce), // Debounce to throttle rapid user interactions
        filter(() => this.form.valid), // Only proceed if form is valid
        tap(() => {
          this.loading$.next(true);
          this.errorMessage = null;
        }),
        switchMap(() => {
          // Transform to query and execute search
          const query = this.buildQuery();
          return this.api.search(query).pipe(
            finalize(() => this.loading$.next(false))
          );
        }),
        takeUntil(this.destroy$) // Cleanup on destroy
      )
      .subscribe({
        next: (response) => this.handleSearchResponse(response),
        error: (err) => this.handleSearchError(err)
      });

    // Trigger initial search
    this.searchTrigger$.next();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.searchTrigger$.complete();
    this.sortChange$.complete();
    this.pageChange$.complete();
    this.loading$.complete();
    this.items$.complete();
    this.total$.complete();
  }

  onSearch() {
    // Trigger the reactive search pipeline
    this.searchTrigger$.next();
  }

  onEnterKey() {
    // basic debounce handled on query$ level; just trigger search
    this.onSearch();
  }

  onSort(field: keyof InventoryItem) {
    // implement the sort functionality
    const direction: SortDir = (
      this.currentSort.field === field && this.currentSort.direction === 'asc'
        ? 'desc'
        : 'asc'
    );

    this.sortChange$.next({ field, direction });
  }

  onPageChange(pageIndex: number) {
    // Emit page change to the reactive pipeline
    this.pageChange$.next(pageIndex);
  }

  // Build the query from current form state and pagination/sort state
  private buildQuery(): InventorySearchQuery {
    const query: InventorySearchQuery = {
      criteria: this.form.value.criteria,
      by: this.form.value.by,
      branches: this.form.value.branches || [],
      onlyAvailable: this.form.value.onlyAvailable || false,
      page: this.currentPage,
      size: this.pageSize,
    };

    // Add sort if a field is selected
    if (this.currentSort.field) {
      query.sort = {
        field: this.currentSort.field as InventoryItemSortableFields,
        direction: this.currentSort.direction
      };
    }

    return query;
  }

  private handleSearchResponse(response: any): void {
    this.errorMessage = null;

    if (response.isFailed) this.handleSearchError(response);
    else if (response.data) {
      this.items$.next(response.data.items);
      this.total$.next(response.data.total);

      if (response.data.total === 0) {
        this.errorMessage = 'No results found.';
      }
    }
  }

  private handleSearchError(err: any): void {
    this.errorMessage = err?.message || 'Search failed. Please try again.';
    this.items$.next([]);
    this.total$.next(0);
  }

  protected readonly String = String;
}
