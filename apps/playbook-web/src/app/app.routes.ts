import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },

  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.page').then(m => m.LoginPage)
  },
  {
    path: 'signup',
    loadComponent: () => import('./features/auth/signup.page').then(m => m.SignupPage)
  },

  {
    path: '',
    loadComponent: () => import('./layouts/shell.component').then(m => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.page').then(m => m.DashboardPage)
      },
      {
        path: 'activities',
        loadComponent: () => import('./features/activities/activity-list.page').then(m => m.ActivityListPage)
      },
      {
        path: 'activities/new',
        loadComponent: () => import('./features/activities/activity-edit.modal').then(m => m.ActivityEditModal)
      },
      {
        path: 'activities/:id/edit',
        loadComponent: () => import('./features/activities/activity-edit.modal').then(m => m.ActivityEditModal)
      },
      {
        path: 'activities/:id',
        loadComponent: () => import('./features/activities/activity-detail.page').then(m => m.ActivityDetailPage)
      },
      {
        path: 'favorites',
        loadComponent: () => import('./features/activities/activity-list.page').then(m => m.ActivityListPage),
        data: { favoritesOnly: true }
      },
      {
        path: 'archive',
        loadComponent: () => import('./features/activities/activity-list.page').then(m => m.ActivityListPage),
        data: { includeArchived: true }
      }
    ]
  },

  { path: '**', redirectTo: '/dashboard' }
];
