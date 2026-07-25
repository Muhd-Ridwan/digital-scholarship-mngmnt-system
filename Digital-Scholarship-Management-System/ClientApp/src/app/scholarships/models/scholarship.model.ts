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
