export type DocumentType = 'bank_statement' | 'academic_result' | 'certificate' | 'other';

export const DOCUMENT_TYPE_LABELS: Record<DocumentType, string> = {
  bank_statement: 'Bank Statement',
  academic_result: 'Academic Result',
  certificate: 'Certificate',
  other: 'Other',
};

export const DOCUMENT_TYPE_CLASSES: Record<DocumentType, string> = {
  bank_statement: 'bg-status-success/10 text-status-success',
  academic_result: 'bg-gold-500/10 text-gold-500',
  certificate: 'bg-status-warning/10 text-status-warning',
  other: 'bg-mist-400/10 text-mist-400',
};

export interface StudentDocument {
  id: number;
  documentType: DocumentType;
  fileName: string;
  fileType: string;
  uploadAt: string;
}
