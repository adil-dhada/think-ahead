import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Apollo } from 'apollo-angular';
import { firstValueFrom, map } from 'rxjs';
import { LucideAngularModule } from 'lucide-angular';
import { TiptapComponent } from '../../shared/editor/tiptap.component';
import { Activity, ActivitiesStore, GET_ACTIVITY, RECORD_VIEW } from './activities.store';
import { ToastService } from '../../shared/toast/toast.service';

const COLOR_DOT: Record<string,string> = {
  INDIGO: 'bg-indigo-500', EMERALD: 'bg-emerald-500', AMBER: 'bg-amber-500',
  SKY: 'bg-sky-500',       VIOLET: 'bg-violet-500',   ROSE: 'bg-rose-500',
  STONE: 'bg-stone-400'
};

@Component({
  selector: 'app-activity-detail',
  standalone: true,
  imports: [RouterLink, TiptapComponent, LucideAngularModule],
  template: `
<div class="px-6 py-6 max-w-5xl mx-auto">

  <!-- Back + actions -->
  <div class="flex items-center justify-between mb-6 gap-4">
    <a routerLink="/activities" class="flex items-center gap-1.5 text-sm text-ink-500 dark:text-ink-400 hover:text-ink-900 dark:hover:text-ink-100 transition">
      <lucide-icon name="chevron-left" class="w-4 h-4"></lucide-icon> Activities
    </a>

    @if (activity()) {
      <div class="flex items-center gap-2">
        <button (click)="toggleFav()" class="p-2 rounded-lg hover:bg-ink-100 dark:hover:bg-ink-900 transition"
          [title]="activity()!.isFavorite ? 'Remove from favorites' : 'Add to favorites'">
          <lucide-icon name="star" [class]="activity()!.isFavorite ? 'w-4 h-4 text-amber-500 fill-amber-500' : 'w-4 h-4 text-ink-400'"></lucide-icon>
        </button>
        <button (click)="toggleArchive()" class="p-2 rounded-lg hover:bg-ink-100 dark:hover:bg-ink-900 transition"
          [title]="activity()!.isArchived ? 'Unarchive' : 'Archive'">
          <lucide-icon name="archive" class="w-4 h-4 text-ink-400"></lucide-icon>
        </button>
        <a [routerLink]="['/activities', activity()!.id, 'run']"
          class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white text-sm font-medium transition">
          <lucide-icon name="rocket" class="w-3.5 h-3.5"></lucide-icon> Run
        </a>
        <a [routerLink]="['/activities', activity()!.id, 'edit']"
          class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-ink-200 dark:border-ink-800 text-sm hover:bg-ink-100 dark:hover:bg-ink-900 transition">
          <lucide-icon name="pencil" class="w-3.5 h-3.5"></lucide-icon> Edit
        </a>
        <button (click)="deleteActivity()" class="p-2 rounded-lg hover:bg-rose-50 dark:hover:bg-rose-900/20 text-ink-400 hover:text-rose-600 transition">
          <lucide-icon name="trash-2" class="w-4 h-4"></lucide-icon>
        </button>
      </div>
    }
  </div>

  @if (loading()) {
    <div class="animate-pulse space-y-4">
      <div class="h-8 bg-ink-100 dark:bg-ink-800 rounded w-2/3"></div>
      <div class="h-4 bg-ink-100 dark:bg-ink-800 rounded w-1/3"></div>
      <div class="h-40 bg-ink-100 dark:bg-ink-800 rounded mt-6"></div>
    </div>
  } @else if (activity()) {
    @let a = activity()!;
    <div class="grid lg:grid-cols-[1fr_280px] gap-8">

      <!-- Main content -->
      <div class="space-y-8">
        <div>
          <div class="flex items-center gap-2 mb-2">
            @if (a.category) {
              <span class="flex items-center gap-1.5 text-xs text-ink-500 dark:text-ink-400">
                <span class="w-1.5 h-1.5 rounded-full {{dotColor(a.category.color)}}"></span>
                {{a.category.name}}
              </span>
            }
            @if (a.isArchived) {
              <span class="text-xs px-2 py-0.5 rounded-full bg-ink-100 dark:bg-ink-800 text-ink-500">Archived</span>
            }
          </div>
          <h1 class="text-2xl font-semibold tracking-tight">{{a.title}}</h1>
          @if (a.tags.length) {
            <div class="mt-3 flex flex-wrap gap-1.5">
              @for (tag of a.tags; track tag; let i = $index) {
                <span class="tag tag-{{tagColor(i)}}">#{{tag}}</span>
              }
            </div>
          }
        </div>

        <!-- Description -->
        @if (a.description) {
          <div>
            <h3 class="font-semibold mb-3">Description</h3>
            <div class="rounded-xl border border-ink-200 dark:border-ink-800 overflow-hidden">
              <app-tiptap [content]="parseDoc(a.description)" [editable]="false" />
            </div>
          </div>
        }

        <!-- Do's and Don'ts -->
        @if (a.dos.length || a.donts.length) {
          <div class="grid sm:grid-cols-2 gap-4">
            @if (a.dos.length) {
              <div class="rounded-xl border border-emerald-200 dark:border-emerald-900/40 bg-emerald-50 dark:bg-emerald-900/10 p-4">
                <h4 class="font-semibold text-emerald-700 dark:text-emerald-400 mb-3 text-sm">Do's</h4>
                <ul class="space-y-2">
                  @for (item of a.dos; track item) {
                    <li class="flex items-start gap-2 text-sm">
                      <span class="w-4 h-4 rounded-full bg-emerald-500 grid place-items-center shrink-0 mt-0.5">
                        <span class="text-white text-[8px]">✓</span>
                      </span>
                      {{item}}
                    </li>
                  }
                </ul>
              </div>
            }
            @if (a.donts.length) {
              <div class="rounded-xl border border-rose-200 dark:border-rose-900/40 bg-rose-50 dark:bg-rose-900/10 p-4">
                <h4 class="font-semibold text-rose-700 dark:text-rose-400 mb-3 text-sm">Don'ts</h4>
                <ul class="space-y-2">
                  @for (item of a.donts; track item) {
                    <li class="flex items-start gap-2 text-sm">
                      <span class="w-4 h-4 rounded-full bg-rose-500 grid place-items-center shrink-0 mt-0.5">
                        <span class="text-white text-[8px]">✕</span>
                      </span>
                      {{item}}
                    </li>
                  }
                </ul>
              </div>
            }
          </div>
        }

        <!-- Notes -->
        @if (a.notes) {
          <div>
            <h3 class="font-semibold mb-3">Notes</h3>
            <div class="rounded-xl border border-ink-200 dark:border-ink-800 overflow-hidden">
              <app-tiptap [content]="parseDoc(a.notes)" [editable]="false" />
            </div>
          </div>
        }
      </div>

      <!-- Metadata rail -->
      <aside class="space-y-6">
        <div class="rounded-xl border border-ink-200 dark:border-ink-800 bg-white dark:bg-ink-900 p-4 space-y-3">
          <div class="flex items-center justify-between text-sm">
            <span class="text-ink-500 dark:text-ink-400">Views</span>
            <span class="flex items-center gap-1.5 font-mono text-xs">
              <lucide-icon name="eye" class="w-3.5 h-3.5 text-ink-400"></lucide-icon>
              {{a.viewCount}}
            </span>
          </div>
          @if (a.runCount > 0) {
            <div class="flex items-center justify-between text-sm">
              <span class="text-ink-500 dark:text-ink-400">Runs</span>
              <span class="flex items-center gap-1.5 font-mono text-xs">
                <lucide-icon name="rocket" class="w-3.5 h-3.5 text-ink-400"></lucide-icon>
                {{a.runCount}}
              </span>
            </div>
            @if (a.lastRunAt) {
              <div class="flex items-center justify-between text-sm">
                <span class="text-ink-500 dark:text-ink-400">Last run</span>
                <span class="font-mono text-xs">{{formatDate(a.lastRunAt)}}</span>
              </div>
            }
          }
          <div class="flex items-center justify-between text-sm">
            <span class="text-ink-500 dark:text-ink-400">Updated</span>
            <span class="font-mono text-xs">{{formatDate(a.updatedAt)}}</span>
          </div>
          <div class="flex items-center justify-between text-sm">
            <span class="text-ink-500 dark:text-ink-400">Created</span>
            <span class="font-mono text-xs">{{formatDate(a.createdAt)}}</span>
          </div>
        </div>

        <!-- Attachments -->
        @if (a.attachments.length) {
          <div>
            <h4 class="font-semibold text-sm mb-2 flex items-center gap-1.5">
              <lucide-icon name="paperclip" class="w-3.5 h-3.5 text-ink-400"></lucide-icon> Attachments
            </h4>
            <ul class="space-y-2">
              @for (att of a.attachments; track att.blobPath) {
                <li>
                  <a [href]="att.downloadUrl" target="_blank"
                    class="flex items-center gap-2 p-2 rounded-lg border border-ink-200 dark:border-ink-800 hover:bg-ink-50 dark:hover:bg-ink-900 text-sm transition">
                    <lucide-icon name="file-text" class="w-4 h-4 text-ink-400 shrink-0"></lucide-icon>
                    <span class="flex-1 truncate text-xs">{{att.fileName}}</span>
                    <span class="text-[10px] font-mono text-ink-400">{{formatSize(att.sizeBytes)}}</span>
                  </a>
                </li>
              }
            </ul>
          </div>
        }
      </aside>
    </div>
  } @else {
    <!-- Not found -->
    <div class="py-24 text-center">
      <div class="w-16 h-16 mx-auto rounded-2xl bg-ink-100 dark:bg-ink-800 grid place-items-center mb-4">
        <lucide-icon name="file-text" class="w-8 h-8 text-ink-400"></lucide-icon>
      </div>
      <h3 class="font-semibold text-lg tracking-tight">Activity not found</h3>
      <p class="mt-1 text-sm text-ink-500 dark:text-ink-400">It may have been deleted or you don't have access.</p>
      <a routerLink="/activities" class="mt-4 inline-flex items-center gap-2 px-4 py-2 rounded-lg border border-ink-200 dark:border-ink-800 text-sm hover:bg-ink-100 dark:hover:bg-ink-900 transition">
        Back to activities
      </a>
    </div>
  }
</div>
  `
})
export class ActivityDetailPage implements OnInit {
  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly apollo = inject(Apollo);
  private readonly store  = inject(ActivitiesStore);
  private readonly toast  = inject(ToastService);

  protected readonly loading  = signal(true);
  protected readonly activity = signal<Activity | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.apollo.query<{ activity: Activity }>({ query: GET_ACTIVITY, variables: { id } })
      .pipe(map(r => r.data!.activity))
      .subscribe({
        next: a => { this.activity.set(a); this.loading.set(false); this.store.recordView(id).subscribe(); },
        error: () => { this.loading.set(false); this.toast.error('Failed to load activity.'); }
      });
  }

  protected async toggleFav(): Promise<void> {
    const a = this.activity();
    if (!a) return;
    try {
      await firstValueFrom(this.store.toggleFavorite(a.id));
      this.activity.update(cur => cur ? { ...cur, isFavorite: !cur.isFavorite } : cur);
      this.toast.success(a.isFavorite ? 'Removed from favorites.' : 'Added to favorites.');
    } catch {
      this.toast.error('Could not update favorite.');
    }
  }

  protected async toggleArchive(): Promise<void> {
    const a = this.activity();
    if (!a) return;
    try {
      await firstValueFrom(this.store.archiveActivity(a.id, !a.isArchived));
      this.activity.update(cur => cur ? { ...cur, isArchived: !cur.isArchived } : cur);
      this.toast.success(a.isArchived ? 'Activity unarchived.' : 'Activity archived.');
    } catch {
      this.toast.error('Could not update archive status.');
    }
  }

  protected async deleteActivity(): Promise<void> {
    const a = this.activity();
    if (!a || !confirm(`Delete "${a.title}"?`)) return;
    try {
      await firstValueFrom(this.store.deleteActivity(a.id));
      this.toast.success('Activity deleted.');
      this.router.navigate(['/activities']);
    } catch {
      this.toast.error('Could not delete activity.');
    }
  }

  protected tagColor(i: number): string {
    return ['indigo','emerald','amber','rose','sky','violet','stone'][i % 7];
  }

  protected dotColor(color: string): string {
    return COLOR_DOT[color] ?? 'bg-ink-400';
  }

  protected formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  protected formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes}B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)}KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)}MB`;
  }

  protected parseDoc(val: unknown): unknown {
    if (!val) return null;
    if (typeof val === 'string') { try { return JSON.parse(val); } catch { return null; } }
    return val;
  }
}
