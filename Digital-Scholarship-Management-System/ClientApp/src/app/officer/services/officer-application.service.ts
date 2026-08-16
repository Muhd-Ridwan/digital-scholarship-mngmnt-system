import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { fetchAuthSession } from 'aws-amplify/auth';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OfficerApplicationService {

  private readonly http = inject(HttpClient);

  private async authHeaders(): Promise<{ Authorization: string }> {
    const session = await fetchAuthSession();

    const token = session.tokens?.accessToken?.toString();

    if (!token) {
      throw new Error('Not authenticated');
    }

    return {
      Authorization: `Bearer ${token}`
    };
  }

  // Get all applications
  async getAllApplications(): Promise<any[]> {

    const headers = await this.authHeaders();

    return await firstValueFrom(
      this.http.get<any[]>(
        `${environment.apiUrl}/officer/applications`,
        { headers }
      )
    );
  }

  // Get one application including its documents
  async getApplication(id: number): Promise<any> {

    const headers = await this.authHeaders();

    return await firstValueFrom(
      this.http.get<any>(
        `${environment.apiUrl}/officer/applications/${id}`,
        { headers }
      )
    );
  }

  //for changing status to under review
  async startReview(id: number): Promise<void> {
    const headers = await this.authHeaders();

    await firstValueFrom(
      this.http.post(
        `${environment.apiUrl}/officer/applications/${id}/start-review`,
        {},
        { headers }
      )
    );
  }

  //to shortlist / reject after while under review
  async makeDecision(
    id: number,
    status: 'Shortlisted' | 'Rejected'
  ): Promise<void> {

    const headers = await this.authHeaders();

    await firstValueFrom(
      this.http.post(
        `${environment.apiUrl}/officer/applications/${id}/decision?status=${status}`,
        {},
        { headers }
      )
    );
  }

  //undo decision
  async undoDecision(id: number): Promise<void> {

    const headers = await this.authHeaders();

    await firstValueFrom(
      this.http.post(
        `${environment.apiUrl}/officer/applications/${id}/undo-decision`,
        {},
        { headers }
      )
    );
  }

  // Review application
  async reviewApplication(
    id: number,
    status: string,
    reviewNotes?: string
  ): Promise<any> {

    const headers = await this.authHeaders();

    return await firstValueFrom(
      this.http.post<any>(
        `${environment.apiUrl}/officer/applications/${id}/review`,
        {},
        {
          headers,
          params: {
            status,
            reviewNotes: reviewNotes ?? ''
          }
        }
      )
    );
  }

  // Get a document download URL
  async getDocumentDownloadUrl(
    documentId: number
  ): Promise<string> {

    const headers = await this.authHeaders();

    const response = await firstValueFrom(
      this.http.get<{ url: string }>(
        `${environment.apiUrl}/officer/applications/documents/${documentId}/download`,
        { headers }
      )
    );

    return response.url;
  }
    async scheduleInterview(data: any): Promise<any> {
    const headers = await this.authHeaders();

    return await firstValueFrom(
      this.http.post(
        `${environment.apiUrl}/officer/interviews`,
        data,
        { headers }
      )
    );
  }

  
}