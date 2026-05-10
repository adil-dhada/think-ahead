import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Apollo } from 'apollo-angular';
import { firstValueFrom } from 'rxjs';
import { map } from 'rxjs/operators';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { ToastService } from '../../shared/toast/toast.service';
import { GET_ACTIVITY, RECORD_RUN, Activity } from './activities.store';

@Component({
  selector: 'app-activity-run',
  standalone: true,
  imports: [RouterLink, FormsModule, LucideAngularModule],
  template: `
<div class="fixed inset-0 z-50 flex items-start justify-center pt-12 pb-8 px-4 overflow-y-auto fade-in bg-ink-950/60 backdrop-blur-sm"
  (click)="onOverlayClick($event)">
  <div class="w-full max-w-2xl bg-white dark:bg-ink-900 rounded-2xl shadow-[var(--shadow-lift)] scale-pop mb-8"
    (click)="$event.stopPropagation()">

    <!-- Header -->
    <div class="flex items-center justify-between px-6 py-4 border-b border-ink-200 dark:border-ink-800">
      <div>
        <div class="text-xs font-mono text-ink-400 uppercase tracking-widest mb-0.5">Running</div>
        <h2 class="font-semibold text-ink-900 dark:text-ink-100 text-lg leading-tight">
          {{activity()?.title ?? 'Loading…'}}
        </h2>
      </div>
      <button (click)="close()" class="p-1.5 rounded-md text-ink-400 hover:text-ink-700 dark:hover:text-ink-200 hover:bg-ink-100 dark:hover:bg-ink-800 transition">
        <lucide-icon name="x" class="w-4 h-4"></lucide-icon>
      </button>
    </div>

    @if (activity(); as a) {
      <div class="px-6 py-5 space-y-6">

        <!-- Progress bar -->
        @if (a.dos.length > 0) {
          <div>
            <div class="flex items-center justify-between mb-2">
              <span class="text-sm font-medium text-ink-700 dark:text-ink-300">Progress</span>
              <span class="text-sm text-ink-500 dark:text-ink-400">{{checkedCount()}} / {{a.dos.length}}</span>
            </div>
            <div class="h-2 bg-ink-100 dark:bg-ink-800 rounded-full overflow-hidden">
              <div class="h-full bg-indigo-500 rounded-full transition-all duration-300"
                [style.width.%]="progressPercent()"></div>
            </div>
          </div>
        }

        <!-- Do's checklist -->
        @if (a.dos.length > 0) {
          <div>
            <h3 class="text-xs font-mono uppercase tracking-widest text-ink-500 dark:text-ink-400 mb-3">Do's</h3>
            <ul class="space-y-2">
              @for (item of a.dos; track $index) {
                <li class="flex items-start gap-3 cursor-pointer" (click)="toggleDo($index)">
                  <div class="mt-0.5 w-5 h-5 shrink-0 rounded border-2 flex items-center justify-center transition-colors"
                    [class]="isChecked($index) ? 'bg-indigo-500 border-indigo-500' : 'border-ink-300 dark:border-ink-600'">
                    @if (isChecked($index)) {
                      <lucide-icon name="check-circle-2" class="w-3 h-3 text-white"></lucide-icon>
                    }
                  </div>
                  <span class="text-sm leading-relaxed transition-colors"
                    [class]="isChecked($index) ? 'line-through text-ink-400 dark:text-ink-500' : 'text-ink-700 dark:text-ink-300'">
                    {{item}}
                  </span>
                </li>
              }
            </ul>
          </div>
        }

        @if (a.dos.length === 0) {
          <div class="py-4 text-center text-sm text-ink-400 dark:text-ink-500">
            No do's defined for this activity. You can still log a run below.
          </div>
        }

        <!-- Outcome note -->
        <div>
          <label class="block text-xs font-mono uppercase tracking-widest text-ink-500 dark:text-ink-400 mb-2">
            Outcome note <span class="normal-case tracking-normal font-sans">(optional)</span>
          </label>
          <textarea [(ngModel)]="outcomeNote" rows="3" placeholder="How did it go? Any observations…"
            class="w-full px-3 py-2 text-sm rounded-lg border border-ink-200 dark:border-ink-700 bg-white dark:bg-ink-900 text-ink-900 dark:text-ink-100 placeholder-ink-400 outline-none focus:border-indigo-400 focus:ring-2 focus:ring-indigo-200 dark:focus:ring-indigo-900/40 resize-none transition"></textarea>
        </div>
      </div>

      <!-- Footer -->
      <div class="px-6 py-4 border-t border-ink-200 dark:border-ink-800 flex items-center justify-between gap-3">
        <button (click)="close()" class="px-4 py-2 text-sm text-ink-600 dark:text-ink-400 hover:text-ink-900 dark:hover:text-ink-100 transition">
          Cancel
        </button>
        <button (click)="complete()" [disabled]="!canComplete() || submitting()"
          class="flex items-center gap-2 px-4 py-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed text-white text-sm font-medium transition">
          <lucide-icon name="rocket" class="w-4 h-4"></lucide-icon>
          {{submitting() ? 'Logging…' : 'Complete Run'}}
        </button>
      </div>
    } @else {
      <div class="px-6 py-12 text-center text-ink-400">Loading…</div>
    }
  </div>
</div>
  `
})
export class ActivityRunModal implements OnInit {
  private readonly apollo   = inject(Apollo);
  private readonly route    = inject(ActivatedRoute);
  private readonly router   = inject(Router);
  private readonly toast    = inject(ToastService);

  protected outcomeNote = '';
  protected readonly submitting = signal(false);
  protected readonly activity   = signal<Activity | null>(null);
  private readonly checked      = signal<Set<number>>(new Set());

  protected readonly checkedCount   = computed(() => this.checked().size);
  protected readonly progressPercent = computed(() => {
    const dos = this.activity()?.dos ?? [];
    return dos.length === 0 ? 100 : Math.round((this.checked().size / dos.length) * 100);
  });
  protected readonly canComplete = computed(() => {
    const dos = this.activity()?.dos ?? [];
    return dos.length === 0 || this.checked().size > 0;
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.apollo.query<{ activity: Activity }>({ query: GET_ACTIVITY, variables: { id } })
      .pipe(map(r => r.data?.activity))
      .subscribe({ next: a => this.activity.set(a ?? null) });
  }

  protected isChecked(index: number): boolean {
    return this.checked().has(index);
  }

  protected toggleDo(index: number): void {
    this.checked.update(s => {
      const next = new Set(s);
      if (next.has(index)) next.delete(index); else next.add(index);
      return next;
    });
  }

  protected onOverlayClick(e: MouseEvent): void {
    if ((e.target as Element).classList.contains('fixed')) this.close();
  }

  protected close(): void {
    this.router.navigate(['/activities', this.route.snapshot.paramMap.get('id')]);
  }

  protected async complete(): Promise<void> {
    if (!this.canComplete() || this.submitting()) return;
    this.submitting.set(true);
    try {
      const id = this.route.snapshot.paramMap.get('id')!;
      await firstValueFrom(
        this.apollo.mutate({ mutation: RECORD_RUN, variables: { id, outcomeNote: this.outcomeNote || null } })
      );
      this.toast.success('Run logged!');
      this.close();
    } catch {
      this.toast.error('Failed to log run.');
      this.submitting.set(false);
    }
  }
}
