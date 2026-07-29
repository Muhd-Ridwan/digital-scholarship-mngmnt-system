import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { fetchAuthSession } from 'aws-amplify/auth';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OfficerDocumentService {

  private readonly http = inject(HttpClient);


  private async authHeaders(): Promise<{ Authorization: string }> {
    const session = await fetchAuthSession();
    const token = session.tokens?.accessToken?.toString();
    if (!token) throw new Error('Not authenticated');
    return { Authorization: `Bearer ${token}` };
  }

  // Officer: list all student documents
  async getAllDocuments(): Promise<any[]> {
    const headers = await this.authHeaders();
    return await firstValueFrom(
      this.http.get<any[]>(`${environment.apiUrl}/officer/documents`, { headers })
    );
  }

  // Officer: list documents for a specific student
  async getStudentDocuments(studentId: number): Promise<any[]> {
    const headers = await this.authHeaders();
    return await firstValueFrom(
      this.http.get<any[]>(`${environment.apiUrl}/officer/documents/${studentId}`, { headers })
    );
  }

  // Officer: approve/reject
  async reviewDocument(id: number, status: string): Promise<void> {
    const headers = await this.authHeaders();
    await firstValueFrom(
      this.http.post(`${environment.apiUrl}/officer/documents/${id}/review?status=${status}`, {}, { headers })
    );
  }

  // Officer: download any student document
  async getDownloadUrlOfficer(id: number): Promise<string> {
    const headers = await this.authHeaders();
    const response = await firstValueFrom(
      this.http.get<{ url: string }>(`${environment.apiUrl}/officer/documents/${id}/download`, { headers })
    );
    return response.url;
  }
}