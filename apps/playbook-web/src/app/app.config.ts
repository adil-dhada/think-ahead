import { APP_INITIALIZER, ApplicationConfig, ErrorHandler, inject, importProvidersFrom, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideApollo } from 'apollo-angular';
import { HttpLink } from 'apollo-angular/http';
import { InMemoryCache } from '@apollo/client/core';
import {
  LucideAngularModule,
  BookOpen, Search, Plus, Star, Clock, Archive, Settings, LogOut, Sun, Moon,
  Home, LayoutDashboard, ChevronDown, ChevronUp, ChevronLeft,
  Eye, EyeOff, AlertCircle, SlidersHorizontal, X,
  Pencil, Trash2, Upload, Paperclip, FileText, Rocket,
  CheckCircle2, Info
} from 'lucide-angular';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { AuthService } from './core/auth/auth.service';
import { GlobalErrorHandler } from './core/error/global-error-handler';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor])),
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    provideApollo(() => {
      const httpLink = inject(HttpLink);
      return {
        link: httpLink.create({ uri: '/graphql', withCredentials: true }),
        cache: new InMemoryCache({
          typePolicies: {
            Query: {
              fields: {
                activities: { keyArgs: ['filter', 'sort'] }
              }
            }
          }
        }),
        defaultOptions: {
          watchQuery: { fetchPolicy: 'cache-and-network' }
        }
      };
    }),
    importProvidersFrom(LucideAngularModule.pick({
      BookOpen, Search, Plus, Star, Clock, Archive, Settings, LogOut, Sun, Moon,
      Home, LayoutDashboard, ChevronDown, ChevronUp, ChevronLeft,
      Eye, EyeOff, AlertCircle, SlidersHorizontal, X,
      Pencil, Trash2, Upload, Paperclip, FileText, Rocket,
      CheckCircle2, Info
    })),
    {
      provide: APP_INITIALIZER,
      useFactory: (auth: AuthService) => () => auth.init(),
      deps: [AuthService],
      multi: true
    }
  ]
};
