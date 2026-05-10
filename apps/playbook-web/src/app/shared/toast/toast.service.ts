import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info';
export interface Toast { id: number; kind: ToastKind; message: string; }

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<Toast[]>([]);
  private nextId = 0;

  success(message: string, duration = 4000): void { this.push('success', message, duration); }
  error(message: string, duration = 6000): void   { this.push('error',   message, duration); }
  info(message: string, duration = 4000): void    { this.push('info',    message, duration); }

  dismiss(id: number): void {
    this.toasts.update(ts => ts.filter(t => t.id !== id));
  }

  private push(kind: ToastKind, message: string, duration: number): void {
    const id = ++this.nextId;
    this.toasts.update(ts => [...ts, { id, kind, message }]);
    if (duration > 0) setTimeout(() => this.dismiss(id), duration);
  }
}
