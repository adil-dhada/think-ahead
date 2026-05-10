import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormArray, FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Apollo, gql } from 'apollo-angular';
import { firstValueFrom, map } from 'rxjs';
import { LucideAngularModule } from 'lucide-angular';
import { TiptapComponent } from '../../shared/editor/tiptap.component';
import { Activity, ActivitiesStore, GET_ACTIVITY } from './activities.store';
import { HttpClient } from '@angular/common/http';
import { ToastService } from '../../shared/toast/toast.service';

interface Category { id: string; name: string; color: string; }

const CATEGORIES_Q = gql`query EditCategories { categories { id name color } }`;

function parseDoc(val: unknown): unknown {
  if (!val) return null;
  if (typeof val === 'string') { try { return JSON.parse(val); } catch { return null; } }
  return val;
}

const COLOR_DOT: Record<string,string> = {
  INDIGO: 'bg-indigo-500', EMERALD: 'bg-emerald-500', AMBER: 'bg-amber-500',
  SKY: 'bg-sky-500',       VIOLET: 'bg-violet-500',   ROSE: 'bg-rose-500',
  STONE: 'bg-stone-400'
};

@Component({
  selector: 'app-activity-edit',
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, TiptapComponent, LucideAngularModule],
  template: `
<!-- Modal overlay -->
<div class="fixed inset-0 z-50 flex items-start justify-center pt-12 pb-8 px-4 overflow-y-auto fade-in"
  (click)="onOverlayClick($event)">
  <div class="w-full max-w-2xl bg-white dark:bg-ink-900 rounded-2xl shadow-[var(--shadow-lift)] scale-pop" (click)="$event.stopPropagation()">

    <!-- Header -->
    <div class="flex items-center justify-between px-6 py-4 border-b border-ink-200 dark:border-ink-800">
      <h2 class="font-semibold text-lg tracking-tight">{{isEdit ? 'Edit activity' : 'New activity'}}</h2>
      <button (click)="close()" class="p-1.5 rounded-lg hover:bg-ink-100 dark:hover:bg-ink-800 text-ink-400 transition">
        <lucide-icon name="x" class="w-5 h-5"></lucide-icon>
      </button>
    </div>

    @if (error()) {
      <div class="mx-6 mt-4 flex items-center gap-2 px-3 py-2.5 rounded-lg bg-rose-50 dark:bg-rose-900/20 border border-rose-200 dark:border-rose-800 text-rose-700 dark:text-rose-300 text-sm">
        <lucide-icon name="alert-circle" class="w-4 h-4 shrink-0"></lucide-icon>
        {{error()}}
      </div>
    }

    <form [formGroup]="form" (ngSubmit)="submit()" class="px-6 py-5 space-y-5">

      <!-- Title -->
      <div>
        <label class="block text-sm font-medium mb-1.5">Title <span class="text-rose-500">*</span></label>
        <input formControlName="title" type="text" placeholder="E.g. Production Deployment Checklist"
          class="w-full px-3 py-2.5 rounded-lg text-sm border bg-white dark:bg-ink-900 placeholder-ink-400 transition"
          [class.border-ink-200]="!titleInvalid"
          [class.dark:border-ink-800]="!titleInvalid"
          [class.border-rose-400]="titleInvalid"
          [class.dark:border-rose-700]="titleInvalid" />
        @if (titleInvalid) {
          <p class="mt-1 text-xs text-rose-500">Title is required (max 200 characters).</p>
        }
      </div>

      <!-- Category searchable dropdown -->
      <div>
        <label class="block text-sm font-medium mb-1.5">Category</label>
        <div class="relative">

          <!-- Backdrop — closes dropdown on outside click -->
          @if (catOpen()) {
            <div class="fixed inset-0 z-10" (click)="catOpen.set(false)"></div>
          }

          <!-- Trigger -->
          <button type="button" (click)="toggleCatOpen()"
            class="relative z-20 w-full flex items-center justify-between px-3 py-2.5 rounded-lg text-sm border border-ink-200 dark:border-ink-800 bg-white dark:bg-ink-900 text-left transition hover:border-indigo-400 dark:hover:border-indigo-500">
            <span [class.text-ink-400]="!selectedCatName()">
              @if (selectedCatName()) {
                <span class="flex items-center gap-2">
                  <span class="w-1.5 h-1.5 rounded-full shrink-0 {{selectedCatDot()}}"></span>
                  {{selectedCatName()}}
                </span>
              } @else {
                — None —
              }
            </span>
            <lucide-icon name="chevron-down" class="w-4 h-4 text-ink-400 transition-transform shrink-0"
              [class.rotate-180]="catOpen()"></lucide-icon>
          </button>

          <!-- Dropdown panel -->
          @if (catOpen()) {
            <div class="absolute z-30 w-full mt-1 bg-white dark:bg-ink-900 border border-ink-200 dark:border-ink-800 rounded-xl shadow-lift overflow-hidden">

              <!-- Search input -->
              <div class="flex items-center gap-2 px-3 py-2.5 border-b border-ink-200 dark:border-ink-800">
                <lucide-icon name="search" class="w-3.5 h-3.5 text-ink-400 shrink-0"></lucide-icon>
                <input #catSearchEl type="text"
                  [value]="catSearch()"
                  (input)="catSearch.set($any($event.target).value)"
                  placeholder="Search categories…"
                  class="flex-1 text-sm bg-transparent outline-none placeholder-ink-400 min-w-0" />
              </div>

              <!-- Options list -->
              <ul class="max-h-52 overflow-y-auto sb-scroll py-1">
                <li>
                  <button type="button" (click)="selectCat('', '')"
                    [class]="'w-full text-left px-3 py-2 text-sm text-ink-400 hover:bg-ink-100 dark:hover:bg-ink-800 transition ' + (!selectedCatName() ? 'bg-ink-100 dark:bg-ink-800' : '')">
                    — None —
                  </button>
                </li>
                @for (cat of filteredCats(); track cat.id) {
                  <li>
                    <button type="button" (click)="selectCat(cat.id, cat.name, cat.color)"
                      [class]="'w-full text-left px-3 py-2 text-sm flex items-center gap-2.5 hover:bg-ink-100 dark:hover:bg-ink-800 transition ' + (selectedCatName() === cat.name ? 'bg-indigo-50 dark:bg-indigo-900/20' : '')">
                      <span class="w-1.5 h-1.5 rounded-full shrink-0 {{catDot(cat.color)}}"></span>
                      {{cat.name}}
                    </button>
                  </li>
                }
                @if (!filteredCats().length) {
                  <li class="px-3 py-3 text-sm text-ink-400 text-center">No categories found</li>
                }
              </ul>
            </div>
          }
        </div>
      </div>

      <!-- Tags -->
      <div>
        <label class="block text-sm font-medium mb-1.5">Tags</label>
        <div class="flex flex-wrap gap-1.5 mb-2">
          @for (tag of tagList(); track tag; let i = $index) {
            <span class="flex items-center gap-1 px-2 py-0.5 rounded-full bg-indigo-100 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-300 text-xs font-medium">
              #{{tag}}
              <button type="button" (click)="removeTag(i)" class="hover:text-indigo-900">
                <lucide-icon name="x" class="w-3 h-3"></lucide-icon>
              </button>
            </span>
          }
        </div>
        <input type="text" [(ngModel)]="tagInput" [ngModelOptions]="{standalone:true}"
          (keydown.enter)="$event.preventDefault(); addTag()"
          (keydown.comma)="$event.preventDefault(); addTag()"
          placeholder="Type a tag and press Enter"
          class="w-full px-3 py-2 rounded-lg text-sm border border-ink-200 dark:border-ink-800 bg-white dark:bg-ink-900 placeholder-ink-400" />
      </div>

      <!-- Description -->
      <div>
        <label class="block text-sm font-medium mb-1.5">Description</label>
        <div class="rounded-xl border border-ink-200 dark:border-ink-800 overflow-hidden">
          <app-tiptap [content]="descContent()" placeholder="What is this playbook about?" (contentChange)="descContent.set($event)" />
        </div>
      </div>

      <!-- Do's -->
      <div>
        <label class="block text-sm font-medium mb-1.5">Do's</label>
        <div formArrayName="dos" class="space-y-2">
          @for (c of dosControls; track $index; let i = $index) {
            <div class="flex items-center gap-2">
              <span class="w-5 h-5 rounded-full bg-emerald-100 dark:bg-emerald-900/30 grid place-items-center shrink-0">
                <span class="text-emerald-600 text-[9px]">✓</span>
              </span>
              <input [formControlName]="i" placeholder="Something you should always do"
                class="flex-1 px-3 py-2 rounded-lg text-sm border border-ink-200 dark:border-ink-800 bg-white dark:bg-ink-900 placeholder-ink-400" />
              <button type="button" (click)="removeDo(i)" class="text-ink-400 hover:text-rose-500 transition">
                <lucide-icon name="trash-2" class="w-4 h-4"></lucide-icon>
              </button>
            </div>
          }
        </div>
        <button type="button" (click)="addDo()"
          class="mt-2 flex items-center gap-1.5 text-sm text-ink-500 dark:text-ink-400 hover:text-ink-900 dark:hover:text-ink-100 transition">
          <lucide-icon name="plus" class="w-3.5 h-3.5"></lucide-icon> Add Do
        </button>
      </div>

      <!-- Don'ts -->
      <div>
        <label class="block text-sm font-medium mb-1.5">Don'ts</label>
        <div formArrayName="donts" class="space-y-2">
          @for (c of dontsControls; track $index; let i = $index) {
            <div class="flex items-center gap-2">
              <span class="w-5 h-5 rounded-full bg-rose-100 dark:bg-rose-900/30 grid place-items-center shrink-0">
                <span class="text-rose-600 text-[9px]">✕</span>
              </span>
              <input [formControlName]="i" placeholder="Something to avoid"
                class="flex-1 px-3 py-2 rounded-lg text-sm border border-ink-200 dark:border-ink-800 bg-white dark:bg-ink-900 placeholder-ink-400" />
              <button type="button" (click)="removeDont(i)" class="text-ink-400 hover:text-rose-500 transition">
                <lucide-icon name="trash-2" class="w-4 h-4"></lucide-icon>
              </button>
            </div>
          }
        </div>
        <button type="button" (click)="addDont()"
          class="mt-2 flex items-center gap-1.5 text-sm text-ink-500 dark:text-ink-400 hover:text-ink-900 dark:hover:text-ink-100 transition">
          <lucide-icon name="plus" class="w-3.5 h-3.5"></lucide-icon> Add Don't
        </button>
      </div>

      <!-- Notes -->
      <div>
        <label class="block text-sm font-medium mb-1.5">Notes</label>
        <div class="rounded-xl border border-ink-200 dark:border-ink-800 overflow-hidden">
          <app-tiptap [content]="notesContent()" placeholder="Additional notes…" (contentChange)="notesContent.set($event)" />
        </div>
      </div>

      <!-- File upload -->
      <div>
        <label class="block text-sm font-medium mb-1.5">Attachments</label>
        <label class="flex items-center justify-center gap-2 px-4 py-3 rounded-xl border-2 border-dashed border-ink-300 dark:border-ink-700 hover:border-indigo-400 cursor-pointer transition text-sm text-ink-500 dark:text-ink-400">
          <lucide-icon name="upload" class="w-4 h-4"></lucide-icon>
          Click to upload (max 25 MB)
          <input type="file" class="hidden" (change)="onFileChange($event)" multiple
            accept="image/*,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.csv" />
        </label>
        @if (pendingFiles().length) {
          <ul class="mt-2 space-y-1">
            @for (f of pendingFiles(); track f.name; let i = $index) {
              <li class="flex items-center gap-2 text-xs text-ink-600 dark:text-ink-300">
                <span class="flex-1 truncate">{{f.name}}</span>
                <span class="font-mono text-ink-400">{{(f.size/1024).toFixed(0)}}KB</span>
                <button type="button" (click)="removePending(i)" class="text-ink-400 hover:text-rose-500">
                  <lucide-icon name="x" class="w-3 h-3"></lucide-icon>
                </button>
              </li>
            }
          </ul>
        }
      </div>

      <!-- Actions -->
      <div class="flex items-center justify-end gap-3 pt-2 border-t border-ink-100 dark:border-ink-800">
        <button type="button" (click)="close()"
          class="px-4 py-2 rounded-lg border border-ink-200 dark:border-ink-800 text-sm hover:bg-ink-100 dark:hover:bg-ink-900 transition">
          Cancel
        </button>
        <button type="submit" [disabled]="saving() || form.invalid"
          class="px-5 py-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 disabled:opacity-60 text-white text-sm font-medium transition">
          {{saving() ? 'Saving…' : (isEdit ? 'Save changes' : 'Create activity')}}
        </button>
      </div>
    </form>
  </div>
</div>
  `
})
export class ActivityEditModal implements OnInit {
  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly apollo = inject(Apollo);
  private readonly store  = inject(ActivitiesStore);
  private readonly http   = inject(HttpClient);
  private readonly fb     = inject(FormBuilder);
  private readonly toast  = inject(ToastService);

  protected isEdit = false;
  protected activityId: string | null = null;

  protected readonly categories   = signal<Category[]>([]);
  protected readonly saving       = signal(false);
  protected readonly error        = signal<string | null>(null);
  protected readonly pendingFiles = signal<File[]>([]);
  protected readonly tagList      = signal<string[]>([]);
  protected readonly descContent  = signal<unknown>(null);
  protected readonly notesContent = signal<unknown>(null);
  protected tagInput = '';

  // Category dropdown
  protected readonly catOpen        = signal(false);
  protected readonly catSearch      = signal('');
  protected readonly selectedCatName = signal('');
  protected readonly selectedCatColor = signal('');

  protected readonly filteredCats = computed(() => {
    const q = this.catSearch().toLowerCase().trim();
    return q
      ? this.categories().filter(c => c.name.toLowerCase().includes(q))
      : this.categories();
  });

  private static readonly CAT_DOTS: Record<string, string> = {
    INDIGO: 'bg-indigo-500', EMERALD: 'bg-emerald-500', AMBER: 'bg-amber-500',
    SKY: 'bg-sky-500',       VIOLET: 'bg-violet-500',   ROSE: 'bg-rose-500',
    STONE: 'bg-stone-400'
  };

  protected catDot(color: string): string {
    return ActivityEditModal.CAT_DOTS[color] ?? 'bg-ink-400';
  }

  protected selectedCatDot(): string {
    return this.catDot(this.selectedCatColor());
  }

  protected toggleCatOpen(): void { this.catOpen.update(v => !v); }

  protected selectCat(id: string, name: string, color = ''): void {
    this.form.patchValue({ categoryId: id });
    this.selectedCatName.set(name);
    this.selectedCatColor.set(color);
    this.catOpen.set(false);
    this.catSearch.set('');
  }

  protected readonly form = this.fb.group({
    title:      ['', [Validators.required, Validators.maxLength(200)]],
    categoryId: [''],
    dos:        this.fb.array<string>([]),
    donts:      this.fb.array<string>([])
  });

  get dosControls()   { return (this.form.get('dos')   as FormArray).controls; }
  get dontsControls() { return (this.form.get('donts') as FormArray).controls; }
  get titleInvalid(): boolean {
    const c = this.form.get('title')!;
    return c.invalid && c.touched;
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.isEdit = !!id && id !== 'new';
    this.activityId = this.isEdit ? id : null;

    this.apollo.query<{ categories: Category[] }>({ query: CATEGORIES_Q })
      .pipe(map(r => r.data!.categories))
      .subscribe(cats => this.categories.set(cats as Category[]));

    if (this.isEdit && this.activityId) {
      this.apollo.query<{ activity: Activity }>({ query: GET_ACTIVITY, variables: { id: this.activityId } })
        .pipe(map(r => r.data!.activity))
        .subscribe(a => this.patchForm(a as Activity));
    }
  }

  private patchForm(a: Activity): void {
    this.form.patchValue({ title: a.title, categoryId: a.category?.id ?? '' });
    this.selectedCatName.set(a.category?.name ?? '');
    this.selectedCatColor.set(a.category?.color ?? '');
    this.tagList.set([...a.tags]);
    this.descContent.set(parseDoc(a.description));
    this.notesContent.set(parseDoc(a.notes));
    const dos   = this.form.get('dos')   as FormArray;
    const donts = this.form.get('donts') as FormArray;
    a.dos.forEach(v => dos.push(this.fb.control(v)));
    a.donts.forEach(v => donts.push(this.fb.control(v)));
  }

  protected addTag(): void {
    const tag = this.tagInput.trim().replace(/^#/, '');
    if (tag && !this.tagList().includes(tag)) this.tagList.update(t => [...t, tag]);
    this.tagInput = '';
  }

  protected removeTag(i: number): void { this.tagList.update(t => t.filter((_,j) => j !== i)); }

  protected addDo():   void { (this.form.get('dos')   as FormArray).push(this.fb.control('')); }
  protected addDont(): void { (this.form.get('donts') as FormArray).push(this.fb.control('')); }
  protected removeDo(i: number):   void { (this.form.get('dos')   as FormArray).removeAt(i); }
  protected removeDont(i: number): void { (this.form.get('donts') as FormArray).removeAt(i); }

  protected onFileChange(e: Event): void {
    const files = Array.from((e.target as HTMLInputElement).files ?? []);
    this.pendingFiles.update(existing => [...existing, ...files]);
  }

  protected removePending(i: number): void {
    this.pendingFiles.update(f => f.filter((_,j) => j !== i));
  }

  protected async submit(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.saving.set(true);
    this.error.set(null);
    try {
      const { title, categoryId } = this.form.getRawValue();
      const dos   = (this.form.get('dos')   as FormArray).value.filter(Boolean);
      const donts = (this.form.get('donts') as FormArray).value.filter(Boolean);
      const input = {
        title, categoryId: categoryId || null,
        tags: this.tagList(),
        dos, donts,
        description: this.descContent() ? JSON.stringify(this.descContent()) : null,
        notes: this.notesContent() ? JSON.stringify(this.notesContent()) : null
      };

      let id = this.activityId;
      if (this.isEdit && id) {
        await firstValueFrom(this.store.updateActivity(id, input));
        this.toast.success('Activity updated.');
      } else {
        const result = await firstValueFrom(
          this.store.createActivity(input).pipe(map(r => r.data?.createActivity))
        );
        id = result?.id ?? null;
        this.toast.success('Activity created.');
      }

      if (id && this.pendingFiles().length) {
        await this.uploadFiles(id);
      }

      this.router.navigate(id ? ['/activities', id] : ['/activities']);
    } catch (e: any) {
      const msg = e?.message ?? 'Failed to save. Please try again.';
      this.error.set(msg);
      this.toast.error(msg);
    } finally {
      this.saving.set(false);
    }
  }

  private async uploadFiles(activityId: string): Promise<void> {
    for (const file of this.pendingFiles()) {
      const fd = new FormData();
      fd.append('file', file);
      fd.append('activityId', activityId);
      const blob = await firstValueFrom(
        this.http.post<{ blobPath: string; fileName: string; contentType: string; sizeBytes: number }>(
          '/api/uploads', fd
        )
      );
      await firstValueFrom(
        this.store.attachFile(activityId, blob.blobPath, blob.fileName, blob.contentType, blob.sizeBytes)
      );
    }
  }

  protected close(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (this.isEdit && id) {
      this.router.navigate(['/activities', id]);
    } else {
      this.router.navigate(['/activities']);
    }
  }

  protected onOverlayClick(e: MouseEvent): void {
    if ((e.target as Element).classList.contains('fixed')) this.close();
  }
}
