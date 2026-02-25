import { Injectable } from '@angular/core';
import {HttpClient, HttpParams} from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, map } from 'rxjs';
import { BaseService } from './base-service';
import { ServerResponse } from '../interfaces/server-response';
import { ListResponseData, ListRetrieveParams, TableDataRequest, ListDef } from '../interfaces/table-models';
import {AdminSegment} from "../interfaces/models";
import { HttpResponse } from '@angular/common/http';
import { ExportFormat } from '../interfaces/export-moodel';

@Injectable({
  providedIn: 'root'
})
export class ListService extends BaseService {

  /**
   * Fetches the table structure/definition
   */
  getListDefinition(segment: AdminSegment, refId?: string): Observable<ServerResponse<ListDef>> {
    let params = new HttpParams();
    params = params.set('refId', refId);

    return this.get<ListDef>(`${segment.navigation.controllerName}/GetListDefinition`, params);
  }

  /**
   * Fetches the table data with filtering, sorting and pagination
   */
  getListData(controllerName: string, request: TableDataRequest): Observable<ServerResponse<ListResponseData>> {
    // Construct query parameters
    let params = new HttpParams();
    params = params.set('showArchive', request.showArchive.toString());
    params = params.set('showDeleted', request.showDeleted.toString());

    if (request.refId) {
      params = params.set('refId', request.refId);
    }

    // Create default filters if not provided
    const filters: ListRetrieveParams = request.filters || {
      pageIndex: 0,
      pageSize: 10
    };

    // Use POST to send filter parameters
    return this.post<ListResponseData>(`${controllerName}/list`, filters, params);
  }

  /**
   * Saves the new order of items in the list
   * @param segment The admin segment
   * @param entityIds Array of entity IDs in their new order
   */
  saveReorder(segment: AdminSegment, entityIds: any[]): Observable<ServerResponse<any>> {
    return this.post<any>(`${segment.navigation.controllerName}/SaveReorder`, entityIds);
  }

  /**
   * Exports the list data in the specified format
   * @param segment The admin segment
   * @param format The export format (xlsx, pdf, csv)
   * @param request The table data request with filters
   */
  exportData(segment: AdminSegment, format: ExportFormat, request: TableDataRequest): Observable<HttpResponse<Blob>> {
    // Construct query parameters
    let params = new HttpParams();
    params = params.set('format', format);
    params = params.set('showArchive', request.showArchive.toString());
    params = params.set('showDeleted', request.showDeleted.toString());

    if (request.refId) {
      params = params.set('refId', request.refId);
    }

    // Create filters with a very large page size to get all results
    const filters: ListRetrieveParams = {
      ...request.filters,
      pageIndex: 0,
      pageSize: 99999999 // Very large number to get all results
    };

    // Use postBlob to send filter parameters and get blob response
    return this.postBlob(`${segment.navigation.controllerName}/Export`, filters, params);
  }

  deleteItem(segment: AdminSegment, id: any): Observable<ServerResponse<any>> {
    return this.post<any>(`${segment.navigation.controllerName}/Delete`, { id });
  }
  
  public deleteAllItems(segment: AdminSegment, refId?: string): Observable<ServerResponse<any>> {
    const params: any = {};
    if (refId) {
      params.refId = refId;
    }
    return this.delete<any>(`${segment.navigation.controllerName}/DeleteAll`, params);
  }

  archiveItem(segment: AdminSegment, id: any): Observable<ServerResponse<any>> {
    return this.post<any>(`${segment.navigation.controllerName}/Archive`, { id });
  }
}
