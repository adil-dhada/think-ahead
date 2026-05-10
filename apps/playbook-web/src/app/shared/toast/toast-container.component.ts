import { Component, inject } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { ToastService, ToastKind } from './toast.service';

const KIND_CLASS: Record<ToastKind, string> = {
  success: 'bg-emerald-50 dark:bg-emerald-900/30 border-emerald-200 dark:border-emerald-700 text-emerald-800 dark:text-emerald-200',
  error:   'bg-rose-50 dark:bg-rose-900/30 border-rose-200 dark:border-rose-700 text-rose-800 dark:text-rose-200',
  info:    'bg-indigo-50 dark:bg-indigo-900/30 border-indigo-200 dark:border-indigo-700 text-indigo-800 dark:text-indigo-200'
};
const KIND_ICON: Record<ToastKind, string>       = { success: 'check-circle-2', error: 'alert-circle', info: 'info' };
const KIND_ICON_COLOR: Record<ToastKind, string> = { success: 'text-emerald-500', error: 'text-rose-500', info: 'text-indigo-500' };

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [LucideAngularModule],
  template: `
<div class="fixed top-4 right-4 z-[200] flex flex-col gap-2 pointer-events-none w-80" aria-live="polite">
  @for (t of toast.toasts(); track t.id) {
    <div class="toast-slide pointer-events-auto flex items-start gap-3 px-4 py-3 rounded-xl shadow-[var(--shadow-lift)] border {{kindClass(t.kind)}}">
      <lucide-icon [name]="kindIcon(t.kind)" class="w-4 h-4 shrink-0 mt-0.5 {{kindIconColor(t.kind)}}"></lucide-icon>
      <span class="flex-1 text-sm leading-snug">{{t.message}}</span>
      <button (click)="toast.dismiss(t.id)" class="opacity-50 hover:opacity-100 transition shrink-0" aria-label="Dismiss">
        <lucide-icon name="x" class="w-3.5 h-3.5"></lucide-icon>
      </button>
    </div>
  }
</div>
  `
})
export class ToastContainerComponent {
  protected readonly toast = inject(ToastService);
  protected kindClass(k: ToastKind): string     { return KIND_CLASS[k]; }
  protected kindIcon(k: ToastKind): string      { return KIND_ICON[k]; }
  protected kindIconColor(k: ToastKind): string { return KIND_ICON_COLOR[k]; }
}
