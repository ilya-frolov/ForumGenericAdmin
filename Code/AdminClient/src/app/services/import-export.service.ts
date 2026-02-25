import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, firstValueFrom } from 'rxjs';
import { BaseService } from './base-service';
import { ServerResponse } from '../interfaces/server-response';
import { AdminSegment } from '../interfaces/models';
import { HttpResponse } from '@angular/common/http';
import { ExcelImportService } from './excel-import.service';
import { FieldType } from '../interfaces/field-data';
import * as XLSX from 'xlsx';
import { ImportResult } from '../interfaces/import-result';

@Injectable({
  providedIn: 'root'
})
export class ImportExportService extends BaseService {
  
  constructor(private excelService: ExcelImportService) {
    super();
  }
  
  /**
   * Download import template for the given entity
   * @param segment The admin segment
   */
  downloadImportTemplate(segment: AdminSegment): Observable<HttpResponse<Blob>> {
    return this.getBlob(`${segment.navigation.controllerName}/DownloadImportTemplate`);
  }

  /**
   * Import data from Excel or CSV file
   * @param segment The admin segment
   * @param file The file to import
   * @param batchSize Number of records to process in a single batch (default: 1000)
   */
  importData(segment: AdminSegment, file: File, refId: string = null, batchSize: number = 1000): Observable<ServerResponse<ImportResult>> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('batchSize', batchSize.toString());
    
    console.log('ImportExportService refId:', refId, 'Type:', typeof refId);
    
    if (refId !== null && refId !== undefined && refId !== '') {
      formData.append('refId', refId);
    }

    return this.postFormData<ServerResponse<ImportResult>>(`${segment.navigation.controllerName}/Import`, formData);
  }
  
  /**
   * Process and map imported Excel data on the client
   * @param file Excel or CSV file
   * @param formModel Form model for mapping
   */
  async processImportFile(file: File, formModel: any): Promise<any[]> {
    try {
      // Read file data
      const rows = await this.excelService.readFile(file);
      
      if (rows.length < 2) {
        throw new Error('File has insufficient data. Expected at least headers and one data row.');
      }
      
      // Extract headers
      const headers = rows[0] as string[];
      
      // Map data rows to objects
      const mappedData = [];
      
      for (let i = 1; i < rows.length; i++) {
        const row = rows[i];
        if (row.some(cell => cell !== undefined)) {
          mappedData.push(this.excelService.mapRowToObject(headers, row));
        }
      }
      
      return mappedData;
    } catch (error) {
      console.error('Error processing import file:', error);
      throw error;
    }
  }
} 