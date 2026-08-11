import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { fetchAuthSession } from 'aws-amplify/auth';
import { environment } from '../../../environments/environment';
import { StudentProfile, UpdateStudentProfileRequest } from '../models/student-profile.model';

@Injectable({ providedIn: 'root' })
export class StudentProfileService {
  private readonly http = inject(HttpClient);

  async getProfile(): Promise<StudentProfile | null> {
    const headers = await this.authHeaders();
    try {
      return await firstValueFrom(
        this.http.get<StudentProfile>(`${environment.apiUrl}/users/me/profile`, { headers }),
      );
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 404) {
        return null;
      }
      throw err;
    }
  }

  async updateProfile(request: UpdateStudentProfileRequest): Promise<StudentProfile> {
    const headers = await this.authHeaders();
    return await firstValueFrom(
      this.http.put<StudentProfile>(`${environment.apiUrl}/users/me/profile`, request, { headers }),
    );
  }

  private async authHeaders(): Promise<{ Authorization: string }> {
    const session = await fetchAuthSession();
    const accessToken = session.tokens?.accessToken?.toString();
    if (!accessToken) {
      throw new Error('Not Authenticated');
    }
    return { Authorization: `Bearer ${accessToken}` };
  }
}
