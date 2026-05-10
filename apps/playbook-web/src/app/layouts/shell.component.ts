import { Component, computed, inject, signal, HostListener } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { Apollo, gql } from 'apollo-angular';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { AuthService } from '../core/auth/auth.service';
import { ThemeService } from '../core/theme/theme.service';
import { CommandPaletteService } from '../shared/command-palette/command-palette.service';
import { CommandPaletteComponent } from '../shared/command-palette/command-palette.component';
import {
  LucideAngularModule,
  BookOpen, Search, Plus, Star, Archive, Settings, LogOut, Sun, Moon,
  LayoutDashboard, ChevronDown, ChevronUp
} from 'lucide-angular';

const SIDEBAR_Q = gql`
  query SidebarData {
    categories { id name color }
    tags { name count }
  }
`;

interface Category { id: string; name: string; color: string; }
interface TagSummary { name: string; count: number; }

const COLOR_CLASS: Record<string, string> = {
  INDIGO: 'bg-indigo-500', EMERALD: 'bg-emerald-500', AMBER: 'bg-amber-500',
  SKY: 'bg-sky-500',       VIOLET: 'bg-violet-500',   ROSE: 'bg-rose-500',
  STONE: 'bg-stone-400'
};

const TAG_PALETTE = ['indigo','emerald','amber','rose','sky','violet','stone'];

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, LucideAngularModule, CommandPaletteComponent],
  template: `
<div class="h-screen flex bg-ink-100 dark:bg-ink-950 text-ink-900 dark:text-ink-100">

  <!-- ── SIDEBAR ─────────────────────────────── -->
  <aside class="w-[264px] shrink-0 border-r border-ink-200 dark:border-ink-800 bg-ink-50/60 dark:bg-ink-950/40 flex flex-col">

    <!-- Logo -->
    <div class="px-5 pt-5 pb-3 flex items-center gap-2.5">
      <div class="w-8 h-8 rounded-lg bg-gradient-to-br from-indigo-500 to-violet-500 grid place-items-center shadow-[var(--shadow-soft)]">
        <lucide-icon name="book-open" class="w-4 h-4 text-white"></lucide-icon>
      </div>
      <div>
        <div class="font-semibold tracking-tight leading-none">Playbook</div>
        <div class="text-[11px] text-ink-500 dark:text-ink-400 mt-0.5 font-mono">v0.1</div>
      </div>
    </div>

    <!-- Search -->
    <div class="px-4 pb-2">
      <div class="flex items-center gap-2 px-3 py-2 rounded-lg bg-white dark:bg-ink-900 border border-ink-200 dark:border-ink-800">
        <lucide-icon name="search" class="w-4 h-4 text-ink-400 shrink-0"></lucide-icon>
        <input type="text" placeholder="Search activities…" (input)="onSidebarSearch($event)"
          class="flex-1 text-sm bg-transparent outline-none placeholder-ink-400 min-w-0" />
      </div>
    </div>

    <!-- New activity -->
    <div class="px-4 pt-2 pb-4">
      <button (click)="newActivity()"
        class="w-full flex items-center justify-center gap-2 px-3 py-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white text-sm font-medium shadow-[var(--shadow-soft)] transition">
        <lucide-icon name="plus" class="w-4 h-4"></lucide-icon> New activity
      </button>
    </div>

    <!-- Scrollable nav -->
    <div class="flex-1 overflow-y-auto sb-scroll">

      <!-- Categories -->
      <div class="px-4 pt-2 pb-2">
        <div class="flex items-center justify-between mb-1.5">
          <div class="font-mono text-[10px] uppercase tracking-[0.15em] text-ink-500 dark:text-ink-400">Categories</div>
        </div>
        <ul class="space-y-0.5 text-sm">
          @for (cat of visibleCategories(); track cat.id) {
            <li>
              <a [routerLink]="['/activities']" [queryParams]="{category: cat.id, categoryName: cat.name}"
                class="flex items-center gap-2.5 px-2.5 py-1.5 rounded-md hover:bg-ink-100 dark:hover:bg-ink-900 text-ink-700 dark:text-ink-300">
                <span class="w-1.5 h-1.5 rounded-full {{colorClass(cat.color)}}"></span>
                <span class="flex-1 truncate">{{cat.name}}</span>
              </a>
            </li>
          }
          @if (!categories().length) {
            <li class="text-xs text-ink-400 px-2.5 py-1">No categories yet</li>
          }
        </ul>
        @if (categories().length > catPageSize) {
          <button (click)="toggleCats()"
            class="mt-1 flex items-center gap-1 px-2.5 py-1 text-xs text-ink-500 dark:text-ink-400 hover:text-ink-800 dark:hover:text-ink-200 transition w-full">
            @if (showAllCats()) {
              <lucide-icon name="chevron-up" class="w-3 h-3"></lucide-icon> Show less
            } @else {
              <lucide-icon name="chevron-down" class="w-3 h-3"></lucide-icon> {{categories().length - catPageSize}} more
            }
          </button>
        }
      </div>

      <!-- Tags -->
      <div class="px-4 pt-3 pb-2">
        <div class="font-mono text-[10px] uppercase tracking-[0.15em] text-ink-500 dark:text-ink-400 mb-1.5">Tags</div>
        <div class="flex flex-wrap gap-1.5 max-h-[110px] overflow-y-auto sb-scroll pr-1">
          @for (tag of tags(); track tag.name; let i = $index) {
            <a [routerLink]="['/activities']" [queryParams]="{tag: tag.name}"
              class="tag tag-{{tagPalette(i)}} cursor-pointer">#{{tag.name}}</a>
          }
        </div>
      </div>

      <!-- Quick access -->
      <div class="px-4 pt-3 pb-4">
        <div class="font-mono text-[10px] uppercase tracking-[0.15em] text-ink-500 dark:text-ink-400 mb-1.5">Quick access</div>
        <ul class="space-y-0.5 text-sm">
          <li>
            <a routerLink="/favorites" routerLinkActive="bg-ink-100 dark:bg-ink-900"
              class="flex items-center gap-2.5 px-2.5 py-1.5 rounded-md hover:bg-ink-100 dark:hover:bg-ink-900 text-ink-700 dark:text-ink-300">
              <lucide-icon name="star" class="w-3.5 h-3.5 text-amber-500"></lucide-icon> Favorites
            </a>
          </li>
          <li>
            <a routerLink="/archive" routerLinkActive="bg-ink-100 dark:bg-ink-900"
              class="flex items-center gap-2.5 px-2.5 py-1.5 rounded-md hover:bg-ink-100 dark:hover:bg-ink-900 text-ink-700 dark:text-ink-300">
              <lucide-icon name="archive" class="w-3.5 h-3.5 text-ink-400"></lucide-icon> Archive
            </a>
          </li>
        </ul>
      </div>
    </div>

    <!-- User row -->
    <div class="border-t border-ink-200 dark:border-ink-800 p-3 flex items-center gap-2">
      <div class="w-8 h-8 rounded-full bg-gradient-to-br from-emerald-400 to-indigo-500 grid place-items-center text-white text-[11px] font-semibold shrink-0">
        {{auth.initials()}}
      </div>
      <div class="flex-1 min-w-0">
        <div class="text-sm font-medium leading-tight truncate">{{auth.currentUser()?.displayName}}</div>
        <div class="text-[11px] text-ink-500 dark:text-ink-400 truncate">{{auth.currentUser()?.email}}</div>
      </div>
      <button class="p-1 rounded-md text-ink-400 hover:text-ink-700 dark:hover:text-ink-200 hover:bg-ink-100 dark:hover:bg-ink-800 transition" title="Settings">
        <lucide-icon name="settings" class="w-4 h-4"></lucide-icon>
      </button>
      <button (click)="logout()" class="p-1 rounded-md text-ink-400 hover:text-rose-500 hover:bg-rose-50 dark:hover:bg-rose-900/20 transition" title="Sign out">
        <lucide-icon name="log-out" class="w-4 h-4"></lucide-icon>
      </button>
    </div>
  </aside>

  <!-- ── MAIN CONTENT ─────────────────────────── -->
  <div class="flex-1 flex flex-col min-w-0">

    <!-- Top bar -->
    <div class="h-14 px-6 flex items-center gap-3 border-b border-ink-200 dark:border-ink-800 bg-white/60 dark:bg-ink-950/60 backdrop-blur shrink-0">
      <nav class="flex items-center gap-1 text-sm">
        <a routerLink="/dashboard" routerLinkActive="text-ink-900 dark:text-ink-100 font-medium"
          class="flex items-center gap-1.5 px-2.5 py-1.5 rounded-md hover:bg-ink-100 dark:hover:bg-ink-900 text-ink-500 dark:text-ink-400">
          <lucide-icon name="layout-dashboard" class="w-3.5 h-3.5"></lucide-icon> Dashboard
        </a>
        <a routerLink="/activities" routerLinkActive="text-ink-900 dark:text-ink-100 font-medium"
          class="flex items-center gap-1.5 px-2.5 py-1.5 rounded-md hover:bg-ink-100 dark:hover:bg-ink-900 text-ink-500 dark:text-ink-400">
          Activities
        </a>
      </nav>
      <div class="ml-auto flex items-center gap-2">
        <button (click)="cmdPalette.toggle()"
          class="hidden sm:flex items-center gap-2 px-2.5 py-1.5 rounded-lg border border-ink-200 dark:border-ink-700 text-xs text-ink-500 dark:text-ink-400 hover:bg-ink-100 dark:hover:bg-ink-800 transition">
          <lucide-icon name="search" class="w-3.5 h-3.5"></lucide-icon>
          <span>Search…</span>
          <kbd class="font-mono text-[10px] px-1 py-0.5 rounded bg-ink-100 dark:bg-ink-800 border border-ink-200 dark:border-ink-700">⌘K</kbd>
        </button>
        <button (click)="theme.toggle()" class="p-1.5 rounded-md hover:bg-ink-100 dark:hover:bg-ink-900 text-ink-600 dark:text-ink-300">
          @if (theme.theme() === 'dark') {
            <lucide-icon name="sun" class="w-4 h-4"></lucide-icon>
          } @else {
            <lucide-icon name="moon" class="w-4 h-4"></lucide-icon>
          }
        </button>
        <div class="w-8 h-8 rounded-full bg-gradient-to-br from-emerald-400 to-indigo-500 grid place-items-center text-white text-[11px] font-semibold">
          {{auth.initials()}}
        </div>
      </div>
    </div>

    <!-- Routed content -->
    <main class="flex-1 overflow-y-auto sb-scroll">
      <router-outlet />
    </main>
  </div>
</div>

@if (cmdPalette.isOpen()) {
  <app-command-palette />
}
  `
})
export class ShellComponent {
  protected readonly auth       = inject(AuthService);
  protected readonly theme      = inject(ThemeService);
  protected readonly cmdPalette = inject(CommandPaletteService);
  private  readonly apollo      = inject(Apollo);
  private  readonly router      = inject(Router);

  @HostListener('document:keydown', ['$event'])
  onGlobalKeyDown(e: KeyboardEvent): void {
    if ((e.metaKey || e.ctrlKey) && e.key === 'k') { e.preventDefault(); this.cmdPalette.toggle(); }
    if (e.key === 'Escape') this.cmdPalette.close();
  }

  protected readonly catPageSize = 6;
  protected readonly showAllCats = signal(false);
  private sidebarSearchTimer: ReturnType<typeof setTimeout> | null = null;

  private readonly _sidebar = toSignal(
    this.apollo.watchQuery<{ categories: Category[]; tags: TagSummary[] }>({
      query: SIDEBAR_Q
    }).valueChanges.pipe(map(r => r.data!)),
    { initialValue: null }
  );

  protected readonly categories = computed(() => (this._sidebar()?.categories ?? []) as Category[]);
  protected readonly tags        = computed(() => (this._sidebar()?.tags ?? []) as TagSummary[]);

  protected readonly visibleCategories = computed(() =>
    this.showAllCats() ? this.categories() : this.categories().slice(0, this.catPageSize)
  );

  protected toggleCats(): void {
    this.showAllCats.update(v => !v);
  }

  protected onSidebarSearch(e: Event): void {
    const val = (e.target as HTMLInputElement).value;
    if (this.sidebarSearchTimer) clearTimeout(this.sidebarSearchTimer);
    this.sidebarSearchTimer = setTimeout(() => {
      this.router.navigate(['/activities'], { queryParams: { q: val || null }, queryParamsHandling: 'merge' });
    }, 400);
  }

  protected colorClass(color: string): string {
    return COLOR_CLASS[color] ?? 'bg-ink-400';
  }

  protected tagPalette(idx: number): string {
    return TAG_PALETTE[idx % TAG_PALETTE.length];
  }

  protected newActivity(): void {
    this.router.navigate(['/activities', 'new']);
  }

  protected async logout(): Promise<void> {
    await this.auth.logout();
    this.router.navigate(['/login']);
  }
}
