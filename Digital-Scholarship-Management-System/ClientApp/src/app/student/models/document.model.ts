export type DocumentType = 'bank_statement' | 'academic_result' | 'certificate' | 'other';

export interface StudentDocument {
  id: number;
  documentType: DocumentType;
  fileName: string;
  fileType: string;
  uploadAt: string;
}
