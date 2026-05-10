import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { Activity } from './activities.store';
import { ActivitiesStore } from './activities.store';
import { firstValueFrom } from 'rxjs';
import { ToastService } from '../../shared/toast/toast.service';

const COLOR_GRADIENT: Record<string, string> = {
  INDIGO: 'gradient-indigo', EMERALD: 'gradient-emerald', AMBER: 'gradient-amber',
  ROSE: 'gradient-rose',     SKY: 'gradient-sky',         VIOLET: 'gradient-violet',
  STONE: 'gradient-stone'
};
const TAG_PALETTE = ['indigo','emerald','amber','rose','sky','violet','stone'];

@Component({
  selector: 'app-activity-card',
  standalone: true,
  imports: [RouterLink, LucideAngularModule],
  template: `
<article class="activity-card rounded-xl border border-ink-200 dark:border-ink-800 bg-white dark:bg-ink-900 overflow-hidden flex flex-col">

  <!-- Color banner -->
  <div class="h-24 {{gradientClass}} flex items-start justify-between p-3 relative">
    <div class="w-8 h-8 rounded-lg bg-white/20 grid place-items-center">
      <span class="text-white font-semibold text-[11px]">{{initials}}</span>
    </div>
    @if (activity.isStale) {
      <span class="absolute bottom-2 left-3 px-1.5 py-0.5 rounded text-[10px] font-semibold bg-amber-500/90 text-white">
        Needs review
      </span>
    }
    <button (click)="onFavorite($event)" class="text-white/80 hover:text-white transition">
      <lucide-icon name="star" [class]="activity.isFavorite ? 'w-4 h-4 fill-white text-white' : 'w-4 h-4'"></lucide-icon>
    </button>
  </div>

  <!-- Body -->
  <div class="p-4 flex flex-col flex-1">
    <a [routerLink]="['/activities', activity.id]" class="font-semibold text-[15px] leading-snug hover:text-indigo-600 dark:hover:text-indigo-400 transition">
      {{activity.title}}
    </a>

    @if (activity.tags.length) {
      <div class="mt-2 flex flex-wrap gap-1">
        @for (tag of activity.tags.slice(0,3); track tag; let i = $index) {
          <span class="tag tag-{{tagColor(i)}}">#{{tag}}</span>
        }
        @if (activity.tags.length > 3) {
          <span class="tag tag-stone">+{{activity.tags.length - 3}}</span>
        }
      </div>
    }

    <div class="mt-auto pt-3 border-t border-ink-100 dark:border-ink-800 flex items-center justify-between text-[12px] text-ink-500 dark:text-ink-400 mono mt-3">
      @if (activity.category) {
        <span class="flex items-center gap-1.5">
          <span class="w-1.5 h-1.5 rounded-full {{catColor}}"></span>
          {{activity.category.name}}
        </span>
      } @else {
        <span>Uncategorised</span>
      }
      <span>{{relativeDate}}</span>
    </div>
  </div>
</article>
  `
})
export class ActivityCardComponent {
  @Input({ required: true }) activity!: Activity;
  @Output() favoriteToggled = new EventEmitter<void>();

  private readonly store = inject(ActivitiesStore);
  private readonly toast = inject(ToastService);

  get gradientClass(): string {
    return COLOR_GRADIENT[this.activity.category?.color ?? ''] ?? 'gradient-stone';
  }

  get catColor(): string {
    const map: Record<string,string> = {
      INDIGO: 'bg-indigo-500', EMERALD: 'bg-emerald-500', AMBER: 'bg-amber-500',
      SKY: 'bg-sky-500',       VIOLET: 'bg-violet-500',   ROSE: 'bg-rose-500',
      STONE: 'bg-stone-400'
    };
    return map[this.activity.category?.color ?? ''] ?? 'bg-ink-400';
  }

  get initials(): string {
    return this.activity.title.split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase();
  }

  get relativeDate(): string {
    const ms = Date.now() - new Date(this.activity.updatedAt).getTime();
    const days = Math.floor(ms / 86_400_000);
    if (days === 0) return 'today';
    if (days === 1) return '1d';
    if (days < 7) return `${days}d`;
    if (days < 30) return `${Math.floor(days / 7)}w`;
    return `${Math.floor(days / 30)}mo`;
  }

  tagColor(idx: number): string {
    return TAG_PALETTE[idx % TAG_PALETTE.length];
  }

  async onFavorite(e: Event): Promise<void> {
    e.preventDefault();
    try {
      await firstValueFrom(this.store.toggleFavorite(this.activity.id));
      this.favoriteToggled.emit();
    } catch {
      this.toast.error('Could not update favorite.');
    }
  }
}
