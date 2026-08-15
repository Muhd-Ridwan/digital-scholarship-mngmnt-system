export type ScholarshipStatus = 'draft' | 'active' | 'closed';

export interface Scholarship {
  id: number;
  title: string;
  description: string;
  fundType: string;
  studyLocation: string;
  organisationType: string;
  fundingAmount: number;
  deadline: string;
  status: ScholarshipStatus;
  applications: number;
}

export interface CreateScholarshipRequest {
  title: string;
  description: string;
  eligibilityCriteria: string;
  fundType: string;
  studyLocation: string;
  organisationType: string;
  fundingAmount: number;
  deadline: string;
}


export interface ScholarshipSummary {
  id: number;
  title: string;
  description: string;
  fundType: string;
  studyLocation: string;
  organisationType: string;
  fundingAmount: number;
  deadline: string;
  sponsorName: string;
}

export interface ScholarshipDetail extends ScholarshipSummary {
  eligibilityCriteria: string;
}
