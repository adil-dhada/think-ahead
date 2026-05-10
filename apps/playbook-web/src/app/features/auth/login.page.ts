import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, LucideAngularModule],
  template: `
<div class="min-h-screen grid lg:grid-cols-[1.1fr_1fr]">

  <!-- Left pane -->
  <div class="hidden lg:flex flex-col auth-pattern auth-grid relative overflow-hidden p-12">
    <div class="relative z-10 flex items-center gap-3">
      <div class="w-9 h-9 rounded-xl bg-gradient-to-br from-indigo-500 to-violet-600 grid place-items-center">
        <lucide-icon name="book-open" class="w-5 h-5 text-white"></lucide-icon>
      </div>
      <span class="text-white font-semibold text-lg tracking-tight">Playbook</span>
    </div>
    <div class="relative z-10 mt-auto">
      <h1 class="text-4xl font-semibold text-white tracking-tight leading-tight">
        Your how‑to,<br>ready when you need it.
      </h1>
      <p class="mt-4 text-ink-300 text-[15px] leading-relaxed max-w-sm">
        Store reusable playbooks for anything — deployments, workouts, recipes, travel prep. Pull it up and get it right.
      </p>
      <div class="mt-8 flex flex-col gap-3">
        @for (f of features; track f.label) {
          <div class="flex items-center gap-3 text-ink-300 text-sm">
            <div class="w-1.5 h-1.5 rounded-full bg-indigo-400"></div>
            {{f.label}}
          </div>
        }
      </div>
    </div>
  </div>

  <!-- Right pane -->
  <div class="flex items-center justify-center p-8 bg-white dark:bg-ink-950">
    <div class="w-full max-w-sm">
      <!-- Mobile logo -->
      <div class="flex lg:hidden items-center gap-2.5 mb-8">
        <div class="w-8 h-8 rounded-lg bg-gradient-to-br from-indigo-500 to-violet-600 grid place-items-center">
          <lucide-icon name="book-open" class="w-4 h-4 text-white"></lucide-icon>
        </div>
        <span class="font-semibold tracking-tight">Playbook</span>
      </div>

      <h2 class="text-2xl font-semibold tracking-tight">Welcome back</h2>
      <p class="mt-1 text-sm text-ink-500 dark:text-ink-400">Sign in to your account</p>

      @if (error()) {
        <div class="mt-4 flex items-center gap-2 px-3 py-2.5 rounded-lg bg-rose-50 dark:bg-rose-900/20 border border-rose-200 dark:border-rose-800 text-rose-700 dark:text-rose-300 text-sm">
          <lucide-icon name="alert-circle" class="w-4 h-4 shrink-0"></lucide-icon>
          {{error()}}
        </div>
      }

      <form [formGroup]="form" (ngSubmit)="submit()" class="mt-6 space-y-4">
        <div>
          <label class="block text-sm font-medium mb-1.5">Email</label>
          <input formControlName="email" type="email" autocomplete="email"
            placeholder="you@example.com"
            class="w-full px-3 py-2.5 rounded-lg text-sm border border-ink-200 dark:border-ink-800 bg-white dark:bg-ink-900 placeholder-ink-400" />
        </div>

        <div>
          <div class="flex items-center justify-between mb-1.5">
            <label class="text-sm font-medium">Password</label>
          </div>
          <div class="relative">
            <input formControlName="password" [type]="showPw() ? 'text' : 'password'" autocomplete="current-password"
              placeholder="••••••••"
              class="w-full px-3 py-2.5 pr-10 rounded-lg text-sm border border-ink-200 dark:border-ink-800 bg-white dark:bg-ink-900 placeholder-ink-400" />
            <button type="button" (click)="showPw.set(!showPw())"
              class="absolute right-3 top-1/2 -translate-y-1/2 text-ink-400 hover:text-ink-600">
              @if (showPw()) {
                <lucide-icon name="eye-off" class="w-4 h-4"></lucide-icon>
              } @else {
                <lucide-icon name="eye" class="w-4 h-4"></lucide-icon>
              }
            </button>
          </div>
        </div>

        <button type="submit" [disabled]="loading() || form.invalid"
          class="w-full py-2.5 rounded-lg bg-indigo-600 hover:bg-indigo-500 disabled:opacity-60 text-white text-sm font-medium transition">
          {{loading() ? 'Signing in…' : 'Sign in'}}
        </button>
      </form>

      <p class="mt-6 text-center text-sm text-ink-500 dark:text-ink-400">
        No account? <a routerLink="/signup" class="text-indigo-600 dark:text-indigo-400 hover:underline font-medium">Create one</a>
      </p>
    </div>
  </div>
</div>
  `
})
export class LoginPage {
  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb     = inject(FormBuilder);

  protected readonly form = this.fb.nonNullable.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  protected readonly loading = signal(false);
  protected readonly error   = signal<string | null>(null);
  protected readonly showPw  = signal(false);

  protected readonly features = [
    { label: 'Structured do\'s, don\'ts and rich notes' },
    { label: 'Category & tag filtering' },
    { label: 'Attach files and track views' },
    { label: 'Works on any device, any time' }
  ];

  protected async submit(): Promise<void> {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);
    const { email, password } = this.form.getRawValue();
    try {
      await this.auth.login(email, password);
      this.router.navigate(['/dashboard']);
    } catch (e: any) {
      this.error.set(e?.message ?? 'Invalid email or password.');
    } finally {
      this.loading.set(false);
    }
  }
}
