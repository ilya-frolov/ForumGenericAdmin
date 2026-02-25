export interface ImportError {
  rowNumber: number;
  fieldName: string;
  errorMessage: string;
}

export interface ImportResult {
  totalRecords: number;
  successCount: number;
  errorCount: number;
  errors: ImportError[];
} 