import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface UserInfo {
  id: string;
  email: string;
  displayName: string;
}

interface AuthPayload {
  accessToken: string;
  user: UserInfo;
}

interface GqlResponse<T> {
  data?: T;
  errors?: Array<{ message: string }>;
}

const GRAPHQL = '/graphql';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  readonly accessToken = signal<string | null>(null);
  readonly currentUser  = signal<UserInfo | null>(null);
  readonly isAuthenticated = computed(() => this.accessToken() !== null);

  private post<T>(query: string, variables?: object): Promise<T> {
    return firstValueFrom(
      this.http.post<GqlResponse<T>>(GRAPHQL, { query, variables }, { withCredentials: true })
    ).then(r => {
      if (r.errors?.length) throw new Error(r.errors[0].message);
      return r.data as T;
    });
  }

  async init(): Promise<void> {
    try {
      const data = await this.post<{ refreshToken: AuthPayload }>(
        `mutation { refreshToken { accessToken user { id email displayName } } }`
      );
      this.setSession(data.refreshToken);
    } catch {
      // No active session — user needs to log in.
    }
  }

  async signup(email: string, password: string, displayName: string): Promise<void> {
    const data = await this.post<{ signup: AuthPayload }>(
      `mutation Signup($i: SignupInput!) { signup(input: $i) { accessToken user { id email displayName } } }`,
      { i: { email, password, displayName } }
    );
    this.setSession(data.signup);
  }

  async login(email: string, password: string): Promise<void> {
    const data = await this.post<{ login: AuthPayload }>(
      `mutation Login($i: LoginInput!) { login(input: $i) { accessToken user { id email displayName } } }`,
      { i: { email, password } }
    );
    this.setSession(data.login);
  }

  async logout(): Promise<void> {
    try {
      await this.post<{ logout: boolean }>(`mutation { logout }`);
    } finally {
      this.clearSession();
    }
  }

  private setSession(payload: AuthPayload): void {
    this.accessToken.set(payload.accessToken);
    this.currentUser.set(payload.user);
  }

  private clearSession(): void {
    this.accessToken.set(null);
    this.currentUser.set(null);
  }

  initials(): string {
    const name = this.currentUser()?.displayName ?? '';
    return name.split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase();
  }
}
