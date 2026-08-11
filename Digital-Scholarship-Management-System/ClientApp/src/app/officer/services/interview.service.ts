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
}
