import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Apollo } from 'apollo-angular';
import { toSignal } from '@angular/core/rxjs-interop';
import { map, switchMap } from 'rxjs';
import { LucideAngularModule } from 'lucide-angular';
import { FormsModule } from '@angular/forms';
import { ActivityCardComponent } from './activity-card.component';
import { Activity, LIST_ACTIVITIES } from './activities.store';

@Component({
  selector: 'app-activity-list',
  standalone: true,
  imports: [RouterLink, ActivityCardComponent, FormsModule,
    LucideAngularModule],
  template: `
<div class="px-6 py-6">

  <!-- Toolbar -->
  <div class="flex items-center gap-3 mb-6 flex-wrap">
    <h2 class="font-semibold text-lg tracking-tight flex-1">
      {{pageTitle}}
      @if (totalCount()) { <span class="ml-2 text-sm font-normal text-ink-500 dark:text-ink-400">{{totalCount()}}</span> }
    </h2>

    <!-- Search -->
    <div class="flex items-center gap-2 px-3 py-2 rounded-lg border border-ink-200 dark:border-ink-800 bg-white dark:bg-ink-900 flex-1 max-w-xs">
      <lucide-icon name="search" class="w-4 h-4 text-ink-400 shrink-0"></lucide-icon>
      <input type="text" [(ngModel)]="searchText" (ngModelChange)="onSearch($event)" placeholder="Search…"
        class="flex-1 text-sm bg-transparent outline-none placeholder-ink-400 min-w-0" />
    </div>

    <a routerLink="/activities/new"
      class="flex items-center gap-2 px-3 py-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white text-sm font-medium transition shrink-0">
      <lucide-icon name="plus" class="w-4 h-4"></lucide-icon> New
    </a>
  </div>

  <!-- Active filters -->
  @if (categoryFilter() || tagFilter()) {
    <div class="flex gap-2 mb-4 flex-wrap">
      @if (categoryFilter()) {
        <span class="flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-indigo-100 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-300 text-xs font-medium">
          {{categoryNameFilter() || 'Category'}}
          <button (click)="clearCategory()" class="hover:text-indigo-900"><lucide-icon name="x" class="w-3 h-3"></lucide-icon></button>
        </span>
      }
      @if (tagFilter()) {
        <span class="flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-indigo-100 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-300 text-xs font-medium">
          #{{tagFilter()}}
          <button (click)="clearTag()" class="hover:text-indigo-900"><lucide-icon name="x" class="w-3 h-3"></lucide-icon></button>
        </span>
      }
    </div>
  }

  <!-- Grid -->
  @if (loading()) {
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
      @for (i of skeleton; track i) {
        <div class="rounded-xl border border-ink-200 dark:border-ink-800 bg-white dark:bg-ink-900 overflow-hidden animate-pulse">
          <div class="h-24 bg-ink-100 dark:bg-ink-800"></div>
          <div class="p-4 space-y-2">
            <div class="h-4 bg-ink-100 dark:bg-ink-800 rounded w-3/4"></div>
            <div class="h-3 bg-ink-100 dark:bg-ink-800 rounded w-1/2"></div>
          </div>
        </div>
      }
    </div>
  } @else if (activities().length) {
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
      @for (a of activities(); track a.id) {
        <app-activity-card [activity]="a" />
      }
    </div>

    @if (hasNextPage()) {
      <div class="mt-8 text-center">
        <button (click)="loadMore()" class="px-4 py-2 rounded-lg border border-ink-200 dark:border-ink-800 text-sm hover:bg-ink-100 dark:hover:bg-ink-900 transition">
          Load more
        </button>
      </div>
    }
  } @else {
    <div class="py-24 text-center">
      <p class="text-ink-500 dark:text-ink-400">No activities found.</p>
      <a routerLink="/activities/new" class="mt-3 inline-block text-sm text-indigo-600 dark:text-indigo-400 hover:underline">Create one →</a>
    </div>
  }
</div>
  `
})
export class ActivityListPage implements OnInit {
  private readonly apollo = inject(Apollo);
  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected searchText = '';
  protected readonly skeleton = Array(8).fill(0);
  protected readonly loading  = signal(true);
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  private readonly _queryRef = this.apollo.watchQuery<{
    activities: { nodes: Activity[]; pageInfo: { hasNextPage: boolean; endCursor: string | null } }
  }>({ query: LIST_ACTIVITIES, variables: this.buildVars() });

  private readonly _data = toSignal(
    this._queryRef.valueChanges.pipe(map(r => r.data)),
    { initialValue: null }
  );

  protected readonly activities  = () => (this._data()?.activities?.nodes ?? []) as Activity[];
  protected readonly hasNextPage = () => this._data()?.activities?.pageInfo?.hasNextPage ?? false;
  protected readonly endCursor   = () => this._data()?.activities?.pageInfo?.endCursor ?? null;
  protected readonly totalCount  = () => this.activities().length;

  protected readonly categoryFilter = toSignal(
    this.route.queryParamMap.pipe(map(p => p.get('category') ?? '')),
    { initialValue: '' }
  );
  protected readonly categoryNameFilter = toSignal(
    this.route.queryParamMap.pipe(map(p => p.get('categoryName') ?? '')),
    { initialValue: '' }
  );
  protected readonly tagFilter = toSignal(
    this.route.queryParamMap.pipe(map(p => p.get('tag') ?? '')),
    { initialValue: '' }
  );

  get pageTitle(): string {
    const data = this.route.snapshot.data;
    if (data['favoritesOnly']) return 'Favorites';
    if (data['includeArchived']) return 'Archive';
    return 'Activities';
  }

  ngOnInit(): void {
    this.searchText = this.route.snapshot.queryParamMap.get('q') ?? '';
    this.route.queryParamMap.subscribe(() => {
      this.loading.set(true);
      this._queryRef.refetch(this.buildVars()).finally(() => this.loading.set(false));
    });
    this._queryRef.valueChanges.subscribe(() => this.loading.set(false));
  }

  protected onSearch(val: string): void {
    this.searchText = val;
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.loading.set(true);
      this._queryRef.refetch(this.buildVars()).finally(() => this.loading.set(false));
    }, 350);
  }

  protected loadMore(): void {
    this._queryRef.fetchMore({
      variables: { ...this.buildVars(), after: this.endCursor() }
    });
  }

  protected clearCategory(): void {
    this.router.navigate([], { queryParams: { category: null, categoryName: null }, queryParamsHandling: 'merge' });
  }

  protected clearTag(): void {
    this.router.navigate([], { queryParams: { tag: null }, queryParamsHandling: 'merge' });
  }

  private buildVars(): Record<string, unknown> {
    const data = this.route.snapshot.data;
    const params = this.route.snapshot.queryParamMap;
    return {
      first: 24,
      filter: {
        categoryId:     params.get('category') ?? undefined,
        tags:           params.get('tag') ? [params.get('tag')!] : undefined,
        search:         this.searchText || params.get('q') || undefined,
        favoritesOnly:  data['favoritesOnly'] ?? false,
        includeArchived: data['includeArchived'] ?? false
      },
      sort: 'UPDATED_DESC'
    };
  }
}
