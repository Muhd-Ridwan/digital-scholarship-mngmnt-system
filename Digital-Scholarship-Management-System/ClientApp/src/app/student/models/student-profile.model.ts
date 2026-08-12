export type Gender = 'male' | 'female';

export type QualificationLevel =
  'spm' | 'stpm' | 'diploma' | 'degree' | 'masters' | 'phd' | 'other';

export interface StudentProfile {
  icNumber: string;
  dateOfBirth: string;
  gender: Gender;
  nationality: string;
  race: string | null;
  hasDisability: boolean;
  disabilityDetails: string | null;
  phoneNumber: string;
  address: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  fatherName: string | null;
  fatherDeceased: boolean;
  fatherOccupation: string | null;
  motherName: string | null;
  motherDeceased: boolean;
  motherOccupation: string | null;
  parentsDivorced: boolean;
  guardianName: string | null;
  guardianPhone: string | null;
  householdIncome: number;
  numberOfSiblings: number;
  highestQualification: QualificationLevel;
  examResults: string | null;
  currentInstitution: string | null;
  fieldOfStudy: string | null;
  bankName: string;
  bankAccNumber: string;
  updatedAt: string;
}

// Omit<type, key>
export type UpdateStudentProfileRequest = Omit<StudentProfile, 'updatedAt'>;
