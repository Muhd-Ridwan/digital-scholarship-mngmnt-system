import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, firstValueFrom, of } from 'rxjs';
import { fetchAuthSession } from 'aws-amplify/auth';
import { environment } from '../../../../environments/environment';
import { User, UserRole, UserStatus } from '../../models/user.model';
import { SponsorProfile } from '../../models/sponsor-profile.model';

interface ApiUser {
  id: number;
  fullName: string;
  email: string;
  role: UserRole;
  status: UserStatus;
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

  // Require sponsor code/data for reference — SponsorProfile is still a placeholder
  getPendingSponsors(): Observable<SponsorProfile[]> {
    return of([{ id: 'sp-1', companyName: 'Green Future Sdn Bhd', ssmNumber: '202601099887' }]);
  }
}
