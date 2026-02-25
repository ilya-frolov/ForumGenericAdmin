import { Injectable } from '@angular/core';
import { FieldType } from '../interfaces/field-data';
import * as XLSX from 'xlsx';

@Injectable({
  providedIn: 'root'
})
export class ExcelImportService {
  
  /**
   * Convert CSV data to Excel format
   * @param csvData CSV data as string
   * @returns Excel workbook
   */
  csvToWorkbook(csvData: string): XLSX.WorkBook {
    // Parse CSV
    const workbook = XLSX.read(csvData, { type: 'string' });
    return workbook;
  }
  
  /**
   * Read data from Excel/CSV file
   * @param file Excel or CSV file
   * @returns Promise with worksheets data
   */
  async readFile(file: File): Promise<any[][]> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      
      reader.onload = (e: any) => {
        try {
          const data = new Uint8Array(e.target.result);
          const workbook = XLSX.read(data, { type: 'array' });
          
          // Get first sheet
          const firstSheetName = workbook.SheetNames[0];
          const worksheet = workbook.Sheets[firstSheetName];
          
          // Convert to JSON
          const jsonData = XLSX.utils.sheet_to_json(worksheet, { header: 1 });
          resolve(jsonData as any[][]);
        } catch (error) {
          reject(error);
        }
      };
      
      reader.onerror = (error) => {
        reject(error);
      };
      
      // Read as array buffer
      reader.readAsArrayBuffer(file);
    });
  }
  
  /**
   * Map row data to object based on headers
   * @param headers Array of header names
   * @param row Array of row values
   * @returns Mapped object
   */
  mapRowToObject(headers: string[], row: any[]): any {
    const obj: any = {};
    
    headers.forEach((header, index) => {
      if (index < row.length) {
        obj[header] = row[index];
      }
    });
    
    return obj;
  }
} 