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
}

export interface RegisterRequest {
  username: string;
  email: string;
  fullName: string;
  role: 'user' | 'sponsor';
  companyName?: string;
  ssmNumber?: string;
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
      return { status: 'signedIn' };
    }
    if (nextStep.signInStep === 'CONFIRM_SIGN_IN_WITH_NEW_PASSWORD_REQUIRED') {
      return { status: 'newPasswordRequired' };
    }
    return { status: 'failed' };
  }

  async completeNewPassword(newPassword: string): Promise<boolean> {
    const { isSignedIn } = await confirmSignIn({ challengeResponse: newPassword });
    return isSignedIn;
  }

  async logout() {
    await signOut();
    this.profilePromise = null;
    this._profile.set(null);
  }

  async register(request: RegisterRequest): Promise<void> {
    await firstValueFrom(this.http.post(`${environment.apiUrl}/auth/register`, request));
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
