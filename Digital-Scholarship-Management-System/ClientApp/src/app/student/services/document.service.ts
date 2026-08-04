import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { fetchAuthSession } from 'aws-amplify/auth';
import { environment } from '../../../environments/environment';
import { DocumentType, StudentDocument } from '../models/document.model';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly http = inject(HttpClient);

  private async authHeaders(): Promise<{ Authorization: string }> {
    const session = await fetchAuthSession();
    const accessToken = session.tokens?.accessToken?.toString();

    if (!accessToken) {
      throw new Error('Not authenticated');
    }
    return { Authorization: `Bearer ${accessToken}` };
  }

  async getDocuments(): Promise<StudentDocument[]> {
    const headers = await this.authHeaders();
    return await firstValueFrom(
      this.http.get<StudentDocument[]>(`${environment.apiUrl}/documents`, { headers }),
    );
  }

  async upload(file: File, documentType: DocumentType): Promise<StudentDocument> {
    const headers = await this.authHeaders();
    const formData = new FormData();
    formData.append('file', file);
    formData.append('documentType', documentType);
    return await firstValueFrom(
      this.http.post<StudentDocument>(`${environment.apiUrl}/documents`, formData, { headers }),
    );
  }

  async getDownloadUrl(id: number): Promise<string> {
    const headers = await this.authHeaders();
    const response = await firstValueFrom(
      this.http.get<{ url: string }>(`${environment.apiUrl}/documents/${id}/download`, { headers }),
    );

    return response.url;
  }

  async deleteDocument(id: number): Promise<void> {
    const headers = await this.authHeaders();
    await firstValueFrom(this.http.delete(`${environment.apiUrl}/documents/${id}`, { headers }));
  }
}
