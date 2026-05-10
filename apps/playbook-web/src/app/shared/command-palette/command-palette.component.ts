import {
  Component, inject, signal, AfterViewInit, ViewChild, ElementRef, HostListener
} from '@angular/core';
import { Router } from '@angular/router';
import { Apollo } from 'apollo-angular';
import { LucideAngularModule } from 'lucide-angular';
import { CommandPaletteService } from './command-palette.service';
import { LIST_ACTIVITIES, Activity } from '../../features/activities/activities.store';

const COLOR_DOT: Record<string, string> = {
  INDIGO: 'bg-indigo-500', EMERALD: 'bg-emerald-500', AMBER: 'bg-amber-500',
  SKY: 'bg-sky-500', VIOLET: 'bg-violet-500', ROSE: 'bg-rose-500', STONE: 'bg-stone-400'
};

interface QuickAction { label: string; icon: string; route: string[]; }

const QUICK_ACTIONS: QuickAction[] = [
  { label: 'New activity',  icon: 'plus',             route: ['/activities', 'new'] },
  { label: 'Dashboard',     icon: 'layout-dashboard', route: ['/dashboard'] },
  { label: 'Favorites',     icon: 'star',             route: ['/favorites'] },
  { label: 'Archive',       icon: 'archive',          route: ['/archive'] },
];

@Component({
  selector: 'app-command-palette',
  standalone: true,
  imports: [LucideAngularModule],
  template: `
<div class="fixed inset-0 z-[100] bg-ink-950/60 backdrop-blur-sm fade-in"
  (click)="svc.close()">

  <div class="absolute top-[15vh] left-1/2 -translate-x-1/2 w-full max-w-xl px-4"
    (click)="$event.stopPropagation()">
    <div class="bg-white dark:bg-ink-900 rounded-2xl shadow-[var(--shadow-lift)] overflow-hidden scale-pop">

      <!-- Search row -->
      <div class="flex items-center gap-3 px-4 py-3 border-b border-ink-200 dark:border-ink-800">
        <lucide-icon name="search" class="w-4 h-4 text-ink-400 shrink-0"></lucide-icon>
        <input #searchInput type="text" placeholder="Search activities or jump to…"
          class="flex-1 text-sm bg-transparent outline-none placeholder-ink-400 text-ink-900 dark:text-ink-100"
          (input)="onInput($event)" (keydown)="onKeyDown($event)" />
        @if (query()) {
          <button (click)="clearQuery()" class="text-ink-400 hover:text-ink-600 dark:hover:text-ink-300">
            <lucide-icon name="x" class="w-4 h-4"></lucide-icon>
          </button>
        } @else {
          <kbd class="hidden sm:inline-flex px-1.5 py-0.5 rounded border border-ink-200 dark:border-ink-700 text-[10px] font-mono text-ink-400">Esc</kbd>
        }
      </div>

      <!-- Results -->
      <div class="max-h-[60vh] overflow-y-auto sb-scroll py-1">
        @if (!query()) {
          <div class="px-3 pt-2 pb-1">
            <div class="text-[10px] font-mono uppercase tracking-widest text-ink-400 dark:text-ink-500 px-2 mb-1">Quick actions</div>
            @for (action of quickActions; track action.label; let i = $index) {
              <button class="w-full flex items-center gap-3 px-2 py-2 rounded-lg text-sm text-left transition"
                [class]="activeIndex() === i
                  ? 'bg-indigo-50 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-300'
                  : 'text-ink-700 dark:text-ink-300 hover:bg-ink-50 dark:hover:bg-ink-800'"
                (click)="navigate(action.route)" (mouseenter)="activeIndex.set(i)">
                <div class="w-7 h-7 rounded-md bg-ink-100 dark:bg-ink-800 flex items-center justify-center shrink-0">
                  <lucide-icon [name]="action.icon" class="w-3.5 h-3.5 text-ink-600 dark:text-ink-300"></lucide-icon>
                </div>
                {{action.label}}
              </button>
            }
          </div>
        } @else if (loading()) {
          <div class="px-5 py-8 text-center text-sm text-ink-400">Searching…</div>
        } @else if (results().length === 0) {
          <div class="px-5 py-8 text-center text-sm text-ink-400">No activities found for "{{query()}}"</div>
        } @else {
          <div class="px-3 pt-2 pb-1">
            <div class="text-[10px] font-mono uppercase tracking-widest text-ink-400 dark:text-ink-500 px-2 mb-1">Activities</div>
            @for (item of results(); track item.id; let i = $index) {
              <button class="w-full flex items-center gap-3 px-2 py-2 rounded-lg text-sm text-left transition"
                [class]="activeIndex() === i
                  ? 'bg-indigo-50 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-300'
                  : 'text-ink-700 dark:text-ink-300 hover:bg-ink-50 dark:hover:bg-ink-800'"
                (click)="navigate(['/activities', item.id])" (mouseenter)="activeIndex.set(i)">
                <div class="w-2 h-2 rounded-full shrink-0 mt-0.5" [class]="dotClass(item)"></div>
                <span class="flex-1 truncate">{{item.title}}</span>
                @if (item.category) {
                  <span class="text-xs text-ink-400 dark:text-ink-500 shrink-0">{{item.category.name}}</span>
                }
              </button>
            }
          </div>
        }

        <!-- Keyboard hint -->
        <div class="px-5 py-2 border-t border-ink-100 dark:border-ink-800 flex items-center gap-4 text-[10px] text-ink-400 dark:text-ink-500">
          <span class="flex items-center gap-1">
            <lucide-icon name="corner-down-left" class="w-3 h-3"></lucide-icon> select
          </span>
          <span class="flex items-center gap-1">
            <lucide-icon name="arrow-up" class="w-3 h-3"></lucide-icon>
            <lucide-icon name="arrow-down" class="w-3 h-3"></lucide-icon> navigate
          </span>
          <span><kbd class="font-mono">Esc</kbd> close</span>
        </div>
      </div>
    </div>
  </div>
</div>
  `
})
export class CommandPaletteComponent implements AfterViewInit {
  protected readonly svc   = inject(CommandPaletteService);
  private  readonly apollo = inject(Apollo);
  private  readonly router = inject(Router);

  @ViewChild('searchInput') searchInputRef!: ElementRef<HTMLInputElement>;

  protected readonly quickActions = QUICK_ACTIONS;
  protected readonly query        = signal('');
  protected readonly results      = signal<Activity[]>([]);
  protected readonly loading      = signal(false);
  protected readonly activeIndex  = signal(0);

  private debounceTimer: ReturnType<typeof setTimeout> | null = null;

  ngAfterViewInit(): void {
    setTimeout(() => this.searchInputRef?.nativeElement.focus(), 0);
  }

  @HostListener('document:keydown', ['$event'])
  onGlobalKey(e: KeyboardEvent): void {
    if (e.key === 'Escape') this.svc.close();
  }

  protected dotClass(item: Activity): string {
    return COLOR_DOT[item.category?.color ?? ''] ?? 'bg-ink-300';
  }

  protected onInput(e: Event): void {
    const val = (e.target as HTMLInputElement).value;
    this.query.set(val);
    this.activeIndex.set(0);
    if (this.debounceTimer) clearTimeout(this.debounceTimer);
    if (!val.trim()) { this.results.set([]); this.loading.set(false); return; }
    this.loading.set(true);
    this.debounceTimer = setTimeout(() => this.search(val.trim()), 250);
  }

  protected onKeyDown(e: KeyboardEvent): void {
    const total = this.query() ? this.results().length : this.quickActions.length;
    if (e.key === 'ArrowDown') { e.preventDefault(); this.activeIndex.update(i => Math.min(i + 1, total - 1)); }
    else if (e.key === 'ArrowUp') { e.preventDefault(); this.activeIndex.update(i => Math.max(i - 1, 0)); }
    else if (e.key === 'Enter') { e.preventDefault(); this.activateSelected(); }
  }

  protected clearQuery(): void {
    this.query.set('');
    this.results.set([]);
    this.activeIndex.set(0);
    if (this.searchInputRef) { this.searchInputRef.nativeElement.value = ''; this.searchInputRef.nativeElement.focus(); }
  }

  protected navigate(route: string[]): void {
    this.router.navigate(route);
    this.svc.close();
  }

  private activateSelected(): void {
    const idx = this.activeIndex();
    if (!this.query()) {
      const action = this.quickActions[idx];
      if (action) this.navigate(action.route);
    } else {
      const item = this.results()[idx];
      if (item) this.navigate(['/activities', item.id]);
    }
  }

  private search(q: string): void {
    this.apollo.query<{ activities: { nodes: Activity[] } }>({
      query: LIST_ACTIVITIES,
      variables: { first: 8, filter: { search: q, includeArchived: false }, sort: 'UPDATED_DESC' }
    }).subscribe({
      next: r => { this.results.set(r.data?.activities?.nodes ?? []); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }
}
