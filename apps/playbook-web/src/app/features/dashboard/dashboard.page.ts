import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Apollo, gql } from 'apollo-angular';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth/auth.service';
import { ActivityCardComponent } from '../activities/activity-card.component';
import { Activity } from '../activities/activities.store';

const DASHBOARD_Q = gql`
  query Dashboard {
    dashboard { totalActivities totalCategories totalTags favoritesCount }
    recentlyViewed(limit: 5) { id title updatedAt createdAt lastViewedAt viewCount category { id name color } tags isFavorite isArchived dos donts description notes attachments { blobPath fileName contentType sizeBytes downloadUrl } }
    favorites(limit: 3) { id title updatedAt createdAt lastViewedAt viewCount category { id name color } tags isFavorite isArchived dos donts description notes attachments { blobPath fileName contentType sizeBytes downloadUrl } }
    activities(first: 4, sort: UPDATED_DESC) {
      nodes { id title updatedAt createdAt lastViewedAt viewCount category { id name color } tags isFavorite isArchived dos donts description notes attachments { blobPath fileName contentType sizeBytes downloadUrl } }
    }
  }
`;

interface DashboardStats { totalActivities: number; totalCategories: number; totalTags: number; favoritesCount: number; }

const COLOR_GRADIENT: Record<string, string> = {
  INDIGO: 'gradient-indigo', EMERALD: 'gradient-emerald', AMBER: 'gradient-amber',
  ROSE: 'gradient-rose',     SKY: 'gradient-sky',         VIOLET: 'gradient-violet',
  STONE: 'gradient-stone'
};

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, ActivityCardComponent, LucideAngularModule],
  template: `
<div class="px-8 py-7 space-y-10">

  <!-- Greeting -->
  <div class="flex items-end justify-between gap-6 flex-wrap">
    <div>
      <div class="text-[13px] font-mono text-ink-500 dark:text-ink-400">{{today}}</div>
      <h1 class="mt-1 text-3xl font-semibold tracking-tight">{{greeting}}, {{firstName}}.</h1>
    </div>

    <!-- Stats -->
    <div class="grid grid-cols-3 gap-3 text-sm">
      <div class="px-4 py-3 rounded-xl border border-ink-200 dark:border-ink-800 bg-white dark:bg-ink-900 min-w-[110px]">
        <div class="font-mono text-[10px] uppercase tracking-wider text-ink-500 dark:text-ink-400">Total</div>
        <div class="mt-1 text-xl font-semibold">{{stats()?.totalActivities ?? '—'}}</div>
      </div>
      <div class="px-4 py-3 rounded-xl border border-ink-200 dark:border-ink-800 bg-white dark:bg-ink-900 min-w-[110px]">
        <div class="font-mono text-[10px] uppercase tracking-wider text-ink-500 dark:text-ink-400">Categories</div>
        <div class="mt-1 text-xl font-semibold">{{stats()?.totalCategories ?? '—'}}</div>
      </div>
      <div class="px-4 py-3 rounded-xl border border-ink-200 dark:border-ink-800 bg-white dark:bg-ink-900 min-w-[110px]">
        <div class="font-mono text-[10px] uppercase tracking-wider text-ink-500 dark:text-ink-400">Favorites</div>
        <div class="mt-1 text-xl font-semibold">{{stats()?.favoritesCount ?? '—'}}</div>
      </div>
    </div>
  </div>

  <!-- Recently viewed -->
  @if (recent().length) {
    <div>
      <div class="flex items-center justify-between mb-3">
        <h3 class="font-semibold tracking-tight flex items-center gap-2">
          <lucide-icon name="clock" class="w-4 h-4 text-ink-400"></lucide-icon> Recently viewed
        </h3>
        <a routerLink="/activities" class="text-[13px] text-ink-500 dark:text-ink-400 hover:text-indigo-600">View all →</a>
      </div>
      <div class="flex gap-4 overflow-x-auto no-scrollbar pb-2 -mx-1 px-1">
        @for (a of recent(); track a.id) {
          <a [routerLink]="['/activities', a.id]"
            class="activity-card shrink-0 w-[240px] rounded-xl border border-ink-200 dark:border-ink-800 bg-white dark:bg-ink-900 overflow-hidden">
            <div class="h-20 {{gradientFor(a)}} flex items-end p-3">
              <span class="text-white font-semibold text-sm">{{initials(a.title)}}</span>
            </div>
            <div class="p-3.5">
              <div class="font-medium text-[14px] truncate">{{a.title}}</div>
              <div class="mt-0.5 text-[12px] text-ink-500 dark:text-ink-400">{{a.category?.name ?? 'Uncategorised'}}</div>
            </div>
          </a>
        }
      </div>
    </div>
  }

  <!-- Favorites -->
  @if (favs().length) {
    <div>
      <div class="flex items-center justify-between mb-3">
        <h3 class="font-semibold tracking-tight flex items-center gap-2">
          <lucide-icon name="star" class="w-4 h-4 text-amber-500"></lucide-icon> Favorites
        </h3>
        <a routerLink="/favorites" class="text-[13px] text-ink-500 dark:text-ink-400 hover:text-indigo-600">{{stats()?.favoritesCount}} starred →</a>
      </div>
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        @for (a of favs(); track a.id) {
          <app-activity-card [activity]="a" />
        }
      </div>
    </div>
  }

  <!-- All activities preview -->
  @if (all().length) {
    <div>
      <div class="flex items-center justify-between mb-3">
        <h3 class="font-semibold tracking-tight">All activities</h3>
        <a routerLink="/activities" class="text-[13px] text-ink-500 dark:text-ink-400 hover:text-indigo-600">Open list →</a>
      </div>
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        @for (a of all(); track a.id) {
          <app-activity-card [activity]="a" />
        }
      </div>
    </div>
  }

  <!-- Empty state -->
  @if (!loading() && !all().length) {
    <div class="py-24 text-center">
      <div class="w-16 h-16 mx-auto rounded-2xl bg-indigo-50 dark:bg-indigo-900/20 grid place-items-center mb-4">
        <lucide-icon name="rocket" class="w-8 h-8 text-indigo-500"></lucide-icon>
      </div>
      <h3 class="font-semibold text-lg tracking-tight">No activities yet</h3>
      <p class="mt-1 text-sm text-ink-500 dark:text-ink-400">Create your first playbook to get started.</p>
      <a routerLink="/activities/new"
        class="mt-4 inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white text-sm font-medium transition">
        New activity
      </a>
    </div>
  }
</div>
  `
})
export class DashboardPage {
  private readonly apollo = inject(Apollo);
  protected readonly auth = inject(AuthService);

  private readonly _data = toSignal(
    this.apollo.watchQuery<{
      dashboard: DashboardStats;
      recentlyViewed: Activity[];
      favorites: Activity[];
      activities: { nodes: Activity[] };
    }>({ query: DASHBOARD_Q }).valueChanges.pipe(map(r => r.data!)),
    { initialValue: null }
  );

  protected readonly loading = () => this._data() === null;
  protected readonly stats   = () => this._data()?.dashboard ?? null;
  protected readonly recent  = () => (this._data()?.recentlyViewed ?? []) as Activity[];
  protected readonly favs    = () => (this._data()?.favorites ?? []) as Activity[];
  protected readonly all     = () => (this._data()?.activities?.nodes ?? []) as Activity[];

  protected readonly today = new Date().toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' });

  protected get greeting(): string {
    const h = new Date().getHours();
    if (h < 12) return 'Good morning';
    if (h < 18) return 'Good afternoon';
    return 'Good evening';
  }

  protected get firstName(): string {
    return this.auth.currentUser()?.displayName?.split(' ')[0] ?? '';
  }

  protected gradientFor(a: Activity): string {
    return COLOR_GRADIENT[a.category?.color ?? ''] ?? 'gradient-stone';
  }

  protected initials(title: string): string {
    return title.split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase();
  }
}
