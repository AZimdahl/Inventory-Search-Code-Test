// pages/index-page/index-page.component.ts

// TypeScript
import { ChangeDetectionStrategy, Component, OnDestroy } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { BehaviorSubject, merge, Subject } from 'rxjs';
import { debounceTime, filter, map, shareReplay, startWith, switchMap, takeUntil, tap } from 'rxjs/operators';
import { InventoryItem, InventorySearchQuery, SearchBy, } from '../../models/inventory-search.models';
import { InventorySearchApiService } from '../../services/inventory-search-api.service';
import { InjectionToken, Inject, OnInit, Optional } from '@angular/core';
import { finalize } from 'rxjs/operators';

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
  // (Implement fields here)\
  _debounce = 50;
  loading$: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);
  errorMessage: string | null = null;
  form: ReturnType<FormBuilder['group']>;
  items$: Subject<InventoryItem[]> = new Subject<InventoryItem[]>();
  total$: Subject<number> = new Subject<number>();
  pageSize = 10;

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
    // implement the code
  }

  ngOnDestroy(): void {
    // implement the cleanup
  }

  onSearch() {
    // implement the search
    const query = this.buildQuery();
    this.api.search(query).pipe(
      finalize(() => this.loading$.next(false))
    ).subscribe({
      next: (response) => {
        this.errorMessage = null;
        if (response?.data) {
          this.items$.next(response.data.items);
          this.total$.next(response.data.total);
        }
      },
      error: (err) => {
        this.errorMessage = 'Search failed. Please try again.';
      }
    });
  }

  onEnterKey() {
    // basic debounce handled on query$ level; just trigger search
    this.onSearch();
  }

  onSort(field: keyof InventoryItem) {
    // implement the sort functionality
  }

  onPageChange(pageIndex: number) {
    // implement the required code
  }
  // Handle branches input changes from template
  onBranchesChange(event: Event) {
    // implement the code
  }

  // Build the query
  private buildQuery(): InventorySearchQuery {
    // implement the code
    return {
      criteria: this.form.value.criteria,
      by: this.form.value.by,
      branches: this.form.value.branches,
      onlyAvailable: this.form.value.onlyAvailable,
    } as InventorySearchQuery;
  }

  protected readonly String = String;
}
