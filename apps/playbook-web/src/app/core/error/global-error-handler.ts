import { ErrorHandler, Injectable, inject } from '@angular/core';
import { ToastService } from '../../shared/toast/toast.service';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly toast = inject(ToastService);

  handleError(error: unknown): void {
    // Unwrap promise rejections Angular wraps
    const unwrapped = error && typeof error === 'object' && 'rejection' in error
      ? (error as { rejection: unknown }).rejection
      : error;

    console.error('[GlobalErrorHandler]', unwrapped);

    if (!(unwrapped instanceof Error)) return;

    const msg = unwrapped.message ?? '';

    // GraphQL/Apollo errors are handled inline — don't double-toast
    if (msg.includes('ApolloError') || msg.includes('GraphQL')) return;
    // Angular dev-mode CD invariant check — not a user-facing error
    if (msg.includes('NG0100') || msg.includes('ExpressionChangedAfterItHasBeenChecked')) return;

    this.toast.error(msg.length > 120 ? 'An unexpected error occurred.' : msg);
  }
}
