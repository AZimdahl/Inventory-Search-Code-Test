//services/inventory-search-api.service.ts

// TypeScript
import { HttpClient, HttpParams } from '@angular/common/http';
import { Inject, Injectable, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { shareReplay } from 'rxjs/operators';
import {
  ApiEnvelope,
  InventorySearchQuery,
  PagedInventoryResponse,
  PeakAvailability,
} from '../models/inventory-search.models';

export const INVENTORY_API_BASE = new InjectionToken<string>('INVENTORY_API_BASE');

// TTL 60s, keep up to 5 cached queries
const CACHE_TTL_MS = 60_000;
const CACHE_MAX_ENTRIES = 5;

interface CacheEntry<T> {
  key: string;
  expiry: number;
  obs$: Observable<T>;
}

@Injectable({ providedIn: 'root' })
export class InventorySearchApiService {
  private cache: CacheEntry<ApiEnvelope<PagedInventoryResponse>>[] = [];
  private peakCache: CacheEntry<ApiEnvelope<PeakAvailability>>[] = [];

  constructor(
    private readonly http: HttpClient,
    @Inject(INVENTORY_API_BASE) private readonly baseUrl: string
  ) { }

  search(query: InventorySearchQuery): Observable<ApiEnvelope<PagedInventoryResponse>> {

    /**
     * Challenge hint:
     * - Derive a stable cache key from the query (include all fields that affect results).
     * - Keep a small in-memory cache with expiration; reuse in-flight/completed observables.
     * - Translate the query into HTTP params; include optional fields only when present.
     * - Return a shared observable so multiple subscribers don’t duplicate requests.
     * - Avoid mixing UI concerns; this layer should only compose and return data streams.
     *
     * this.http.get<??????>(`${this.baseUrl}/inventory/search`, { params })
     */

    // init cache key
    const key = this.cacheKey(query);

    // check for cached entry
    const now = Date.now();
    this.cache = this.cache.filter(e => e.expiry > now);
    const cached = this.cache.find(e => e.key === key);

    if (cached) {
      return cached.obs$;
    }

    // init params with paging
    let params: HttpParams = new HttpParams();

    // build params
    if (query) {
      params = new HttpParams({
        ...params,
        fromObject: {
          criteria: query.criteria,
          by: query.by,
          page: query.page?.toString() || '0',
          size: query.size?.toString() || '20',
          onlyAvailable: query.onlyAvailable || 'false',
        }
      });

      if (query.sort) params = params.set('sort', `${query.sort.field}:${query.sort.direction}`);
      if (query.branches && query.branches.length > 0) {
        params = params.set('branches', query.branches.join(','));
      }
    }

    // make request
    const response: Observable<ApiEnvelope<PagedInventoryResponse>> = this.http.get<ApiEnvelope<PagedInventoryResponse>>(
      `${this.baseUrl}/inventory/search`, { params }
    ).pipe(shareReplay(1));

    // store response
    this.remember(this.cache, { key, obs$: response });

    return response;
  }

  getPeakAvailability(partNumber: string): Observable<ApiEnvelope<PeakAvailability>> {
    /**
     * Challenge hint:
     * - Use the part number to form a cache key for this lookup.
     * - Evict stale entries before attempting a cache hit.
     * - If cached, return the shared observable to avoid duplicate requests.
     * - Otherwise, issue a GET with the partNumber as a query param and share the result.
     * - Remember the observable with a TTL (time to live); keep this method free of UI concerns.
     * this.http.get<??????>(`${this.baseUrl}/inventory/availability/peak`, { params})
     */
    const key = partNumber.trim().toLowerCase();

    // check for cached entry
    const now = Date.now();
    this.peakCache = this.peakCache.filter(e => e.expiry > now);
    const cached = this.peakCache.find(e => e.key === key);

    if (cached) {
      return cached.obs$;
    }

    // otherwise, make request
    // init params
    const params: HttpParams = new HttpParams({ fromObject: { partNumber } });

    // make request
    const response: Observable<ApiEnvelope<PeakAvailability>> = this.http.get<ApiEnvelope<PeakAvailability>>(
      `${this.baseUrl}/inventory/availability/peak`, { params }
    ).pipe(shareReplay(1));

    // store response
    this.remember(this.peakCache, { key, obs$: response });

    return response;
  }

  /**
   * Challenge hint:
   * - Keep the cache small and predictable; decide what to evict when full.
   * - Consider how expiration (TTL) interacts with capacity-based eviction.
   * - Think about whether failed results should be cached the same way as successful ones.
   * - Keep this purely about data/memoization; avoid UI/side-effects here.
   */
  private remember<T>(
    cache: CacheEntry<T>[],
    entry: { key: string; obs$: Observable<T> }
  ) {
    const now = Date.now();

    // Evict expired entries (mutate in place)
    for (let i = cache.length - 1; i >= 0; i--) {
      if (cache[i].expiry <= now) {
        cache.splice(i, 1);
      }
    }
    // Evict oldest if at capacity
    if (cache.length >= CACHE_MAX_ENTRIES) {
      cache.sort((a, b) => a.expiry - b.expiry);
      cache.splice(0, 1);
    }
    cache.push({ ...entry, expiry: now + CACHE_TTL_MS });
  }


  private cacheKey(q: InventorySearchQuery): string {
    /**
     * Challenge hint:
     * - Produce a stable key that uniquely represents the query.
     * - Normalize values (e.g., trim, lowercase) to avoid duplicate keys for equivalent inputs.
     * - Ensure ordering doesn’t affect the key (e.g., sort arrays like branches).
     * - Include every parameter that can change results; omit those that do not.
     * - Choose delimiters that won’t collide with real data.
     */
    let key: string;
    key = q.criteria.trim().toLowerCase();
    key += `|by:${q.by}`;
    key += `|onlyAvailable:${q.onlyAvailable ? 'true' : 'false'}`;
    key += `|page:${q.page || 0}`;
    key += `|size:${q.size || 20}`;

    if (q.sort) {
      key += `|sort:${q.sort.field}:${q.sort.direction}`;
    }
    if (q.branches && q.branches.length > 0) {
      // no need to sort or normalize branches as selection order is preserved using multiple select and are not typed values
      key += `|branches:${q.branches.join(',')}`;
    }

    console.log('Cache key:', key);

    return key;
  }
}
