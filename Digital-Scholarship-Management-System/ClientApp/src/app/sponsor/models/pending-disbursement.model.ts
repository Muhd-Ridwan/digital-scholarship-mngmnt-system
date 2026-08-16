export interface PendingDisbursement {
    id: number;
    scholarshipId: number;
    scholarshipTitle: string;
    fundType: string;
    fundingAmount: number;
    studentId: number;
    studentName: string;
    decisionAt: string;
}