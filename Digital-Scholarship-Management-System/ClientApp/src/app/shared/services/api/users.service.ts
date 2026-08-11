import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { fetchAuthSession } from 'aws-amplify/auth';
import { environment } from '../../../../environments/environment';
import { User, UserRole, UserStatus } from '../../models/user.model';
import { SponsorDecision, SponsorProfile } from '../../models/sponsor-profile.model';

interface ApiUser {
  id: number;
  fullName: string;
  email: string;
  role: UserRole;
  status: UserStatus;
}

interface ApiSponsor {
  id: number;
  companyName: string;
  ssmNumber: string | null;
  registeredAt: string;
  status: SponsorDecision;
  decidedAt: string | null;
  decidedBy: string | null;
}

// Users and access. getUsers() / setStatus() are real calls. Register-officer is handled
// by AuthService.registerOfficer(); approve-sponsor still needs its own POST wiring.
@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);

  private async authHeader(): Promise<{ Authorization: string }> {
    const session = await fetchAuthSession();
    const accessToken = session.tokens?.accessToken?.toString();
    if (!accessToken) {
      throw new Error('Not authenticated.');
    }
    return { Authorization: `Bearer ${accessToken}` };
  }

  async getUsers(): Promise<User[]> {
    const session = await fetchAuthSession();
    const accessToken = session.tokens?.accessToken?.toString();
    if (!accessToken) {
      return [];
    }
    const apiUsers = await firstValueFrom(
      this.http.get<ApiUser[]>(`${environment.apiUrl}/users`, {
        headers: { Authorization: `Bearer ${accessToken}` },
      }),
    );
    return apiUsers.map((u) => ({
      id: String(u.id),
      name: u.fullName,
      email: u.email,
      role: u.role,
      status: u.status,
    }));
  }

  async setStatus(userId: string, status: UserStatus): Promise<User> {
    const headers = await this.authHeader();
    const updated = await firstValueFrom(
      this.http.patch<ApiUser>(
        `${environment.apiUrl}/users/${userId}/status`,
        { status },
        { headers },
      ),
    );
    return {
      id: String(updated.id),
      name: updated.fullName,
      email: updated.email,
      role: updated.role,
      status: updated.status,
    };
  }

  // Every sponsor with its decision — the screen splits pending from decided itself,
  // so both tables come from one round trip.
  async getSponsors(): Promise<SponsorProfile[]> {
    const headers = await this.authHeader();
    const sponsors = await firstValueFrom(
      this.http.get<ApiSponsor[]>(`${environment.apiUrl}/users/sponsors`, { headers }),
    );
    return sponsors.map(toSponsor);
  }

  async approveSponsor(id: string): Promise<SponsorProfile> {
    const headers = await this.authHeader();
    return toSponsor(
      await firstValueFrom(
        this.http.post<ApiSponsor>(`${environment.apiUrl}/users/${id}/approve-sponsor`, {}, { headers }),
      ),
    );
  }

  async rejectSponsor(id: string): Promise<SponsorProfile> {
    const headers = await this.authHeader();
    return toSponsor(
      await firstValueFrom(
        this.http.post<ApiSponsor>(`${environment.apiUrl}/users/${id}/reject-sponsor`, {}, { headers }),
      ),
    );
  }
}

function toSponsor(sponsor: ApiSponsor): SponsorProfile {
  return { ...sponsor, id: String(sponsor.id) };
}
