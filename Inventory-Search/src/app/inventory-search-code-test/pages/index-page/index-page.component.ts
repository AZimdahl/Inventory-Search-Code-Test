// pages/index-page/index-page.component.ts

// TypeScript
import { ChangeDetectionStrategy, Component, OnDestroy, InjectionToken, Inject, OnInit, Optional } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BehaviorSubject, merge, Subject } from 'rxjs';
import { debounceTime, filter, finalize, switchMap, takeUntil, tap } from 'rxjs/operators';
import { InventoryItem, InventoryItemSortableFields, InventorySearchQuery, SearchBy, } from '../../models/inventory-search.models';
import { InventorySearchApiService } from '../../services/inventory-search-api.service';

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
  /**
   * Challenge hint (replace this block with your state fields):
   * - Define reactive controllers for: search trigger, sort state, and current page.
   * - Expose public observables for: total count and items list derived from responses.
   * - Track loading as a boolean BehaviorSubject toggled around requests.
   * - Keep a simple string errorMessage to show failures inline.
   * - Keep a configurable debounce value (overridable via DI) for throttling user actions.
   * - Create a form group with fields for criteria, by, branches, and onlyAvailable.
   */
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

  /**
   * Code challenge – high-level goal:
   * - Compose a reactive search pipeline driven by three inputs: manual search trigger, sort changes, and page changes.
   * - Debounce and transform those inputs into a typed query object, then execute the request while canceling stale ones.
   * - Expose loading, total count, and items as observables suitable for OnPush + async pipe.
   * - Handle failures with a simple inline message; keep all UI state separate from API concerns.
   * - Ensure proper cleanup of subscriptions and efficient re-use of the latest emissions.
   */

  ngOnInit(): void {
    // Compose the reactive search pipeline
    // Merge all three input streams: manual search, sort changes, and page changes
    merge(
      this.searchTrigger$.pipe(
        tap(() => this.currentPage = 0) // Reset page when new search is triggered
      ),
      this.pageChange$.pipe(
        tap((pageIndex) => this.currentPage = pageIndex) // Update current page
      ),
      this.sortChange$.pipe(
        tap((sortState) => {
          // Update sort state and reset page
          this.currentSort = sortState;
          this.currentPage = 0;
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

  // Handle branches input changes from template
  onBranchesChange(event: Event) {

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
    }
  }

  private handleSearchError(err: any): void {
    this.errorMessage = err?.message || 'Search failed. Please try again.';
    this.items$.next([]);
    this.total$.next(0);
  }

  protected readonly String = String;
}
