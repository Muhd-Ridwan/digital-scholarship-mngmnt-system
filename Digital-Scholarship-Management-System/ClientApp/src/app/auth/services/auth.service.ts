import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { signIn, signOut, getCurrentUser, fetchAuthSession, confirmSignIn } from 'aws-amplify/auth';
import { environment } from '../../../environments/environment';

export interface UserProfile {
  cognitoSub: string;
  email: string;
  fullName: string;
  role: string;
  createdAt: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  fullName: string;
  role: 'user' | 'sponsor';
  companyName?: string;
  ssmNumber?: string;
}

export interface RegisterOfficerRequest {
  username: string;
  email: string;
  fullName: string;
}

export type LoginResult =
  { status: 'signedIn' } | { status: 'newPasswordRequired' } | { status: 'failed' };

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private profilePromise: Promise<UserProfile | null> | null = null;
  // To fetch and display user dashboard
  private readonly _profile = signal<UserProfile | null>(null);
  readonly profile = this._profile.asReadonly();

  async login(username: string, password: string): Promise<LoginResult> {
    const { isSignedIn, nextStep } = await signIn({ username, password });

    if (isSignedIn) {
      this.profilePromise = null;
      this.logLogin();
      return { status: 'signedIn' };
    }
    if (nextStep.signInStep === 'CONFIRM_SIGN_IN_WITH_NEW_PASSWORD_REQUIRED') {
      return { status: 'newPasswordRequired' };
    }
    return { status: 'failed' };
  }

  async completeNewPassword(newPassword: string): Promise<boolean> {
    const { isSignedIn } = await confirmSignIn({ challengeResponse: newPassword });
    if (isSignedIn) {
      this.profilePromise = null;
      this.logLogin();
    }
    return isSignedIn;
  }

  async logout() {
    await signOut();
    this.profilePromise = null;
    this._profile.set(null);
  }

  private async logLogin(): Promise<void> {
    try {
      const session = await fetchAuthSession();
      const accessToken = session.tokens?.accessToken?.toString();
      if (!accessToken) {
        return;
      }

      await firstValueFrom(
        this.http.post(`${environment.apiUrl}/auth/log-login`, null, {
          headers: { Authorization: `Bearer ${accessToken}` },
        }),
      );
    } catch {}
  }

  async register(request: RegisterRequest): Promise<void> {
    await firstValueFrom(this.http.post(`${environment.apiUrl}/auth/register`, request));
  }

  async forgotPassword(email: string): Promise<void> {
    await firstValueFrom(this.http.post(`${environment.apiUrl}/auth/forgot-password`, { email }));
  }

  // Admin-only — the officer branch of the same /auth/register endpoint, gated server-side
  // on the caller's own bearer token being an Admin (AuthController.Register).
  async registerOfficer(request: RegisterOfficerRequest): Promise<void> {
    const session = await fetchAuthSession();
    const accessToken = session.tokens?.accessToken?.toString();
    if (!accessToken) {
      throw new Error('Not authenticated.');
    }
    await firstValueFrom(
      this.http.post(
        `${environment.apiUrl}/auth/register`,
        { ...request, role: 'officer' },
        { headers: { Authorization: `Bearer ${accessToken}` } },
      ),
    );
  }

  // Admin edits their own full name; email/password stay Cognito-owned.
  async updateFullName(fullName: string): Promise<UserProfile> {
    const session = await fetchAuthSession();
    const accessToken = session.tokens?.accessToken?.toString();
    if (!accessToken) {
      throw new Error('Not authenticated.');
    }
    const updated = await firstValueFrom(
      this.http.put<UserProfile>(
        `${environment.apiUrl}/users/me`,
        { fullName },
        { headers: { Authorization: `Bearer ${accessToken}` } },
      ),
    );
    this._profile.set(updated);
    return updated;
  }

  async getCurrentUsername(): Promise<string | null> {
    try {
      const user = await getCurrentUser();
      return user.username;
    } catch {
      return null;
    }
  }

  async getProfile(): Promise<UserProfile | null> {
    if (!this.profilePromise) {
      this.profilePromise = this.fetchProfile();
    }
    const profile = await this.profilePromise;
    this._profile.set(profile);
    return profile;
  }

  private async fetchProfile(): Promise<UserProfile | null> {
    const session = await fetchAuthSession();
    const accessToken = session.tokens?.accessToken?.toString();
    if (!accessToken) {
      return null;
    }
    try {
      return await firstValueFrom(
        this.http.get<UserProfile>(`${environment.apiUrl}/users/me`, {
          headers: { Authorization: `Bearer ${accessToken}` },
        }),
      );
    } catch {
      return null;
    }
  }
}
