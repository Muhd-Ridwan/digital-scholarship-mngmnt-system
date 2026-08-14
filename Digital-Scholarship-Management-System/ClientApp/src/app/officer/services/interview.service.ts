import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { fetchAuthSession } from 'aws-amplify/auth';
import { environment } from '../../../environments/environment';

export interface ScheduleInterviewRequest {
applicationId: number;
interviewDate: string;
interviewTime: string;
interviewMethod: string;
location?: string;
meetingLink?: string;
notes?: string;
}

@Injectable({
providedIn: 'root'
})
export class InterviewService {

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

async getShortlistedApplicants(): Promise<any[]> {
  const headers = await this.authHeaders();

  return await firstValueFrom(
    this.http.get<any[]>(
      `${environment.apiUrl}/officer/interviews/shortlisted`,
      { headers }
    )
  );
}

async scheduleInterview(
request: ScheduleInterviewRequest
): Promise<any> {


const headers = await this.authHeaders();

return await firstValueFrom(
  this.http.post(
    `${environment.apiUrl}/officer/interviews`,
    request,
    { headers }
  )
);

}

async getUpcomingInterviews(): Promise<any[]> {
  const headers = await this.authHeaders();

  return await firstValueFrom(
    this.http.get<any[]>(
      `${environment.apiUrl}/officer/interviews/upcoming`,
      { headers }
    )
  );
}

async getAllInterviews(): Promise<any[]> {

  const headers = await this.authHeaders();

  return await firstValueFrom(
    this.http.get<any[]>(
      `${environment.apiUrl}/officer/interviews`,
      { headers }
    )
  );
}

async getPastInterviews(): Promise<any[]> {
  const headers = await this.authHeaders();

  return await firstValueFrom(
    this.http.get<any[]>(
      `${environment.apiUrl}/officer/interviews/past`,
      { headers }
    )
  );
}

async makeFinalDecision(
  applicationId: number,
  status: 'approved' | 'rejected'
): Promise<any> {

  const headers = await this.authHeaders();

  return await firstValueFrom(
    this.http.post(
      `${environment.apiUrl}/officer/interviews/${applicationId}/decision?status=${status}`,
      {},
      { headers }
    )
  );
}

async updateInterviewNotes(
  interviewId: number,
  notes: string
): Promise<any> {

  const headers = await this.authHeaders();

  return await firstValueFrom(
    this.http.put(
      `${environment.apiUrl}/officer/interviews/${interviewId}/notes`,
      { notes },
      { headers }
    )
  );
}

}
