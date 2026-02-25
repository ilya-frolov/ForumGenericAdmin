import { Injectable } from '@angular/core';
import {BaseService} from "./base-service";
import {ActivatedRoute, Router} from "@angular/router";
import { HttpClient, HttpParams, HttpHeaders, HttpEventType, HttpEvent } from "@angular/common/http";
import {Observable, tap, forkJoin, of, Subscription, firstValueFrom, Subject, BehaviorSubject} from "rxjs";
import {ServerResponse} from "../interfaces/server-response";
import {AdminHomeData, AdminSegment, AdminSettingsSegment, ConnectedUser, InitData} from "../interfaces/models";
import { switchMap, map, catchError } from 'rxjs/operators';
import { Platforms } from '../interfaces/fileInfo-model';

interface PendingFile {
  formData: FormData;
  fileName: string;
}

export interface FileUploadProgress {
  fileName: string;
  progress: number;
  status: 'pending' | 'uploading' | 'completed' | 'error';
  message?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AppService extends BaseService {

  private initData: InitData;
  private homeData: AdminHomeData;

  private filesWaitingForUpload: PendingFile[] = [];
  private uploadProgressSubject = new BehaviorSubject<FileUploadProgress[]>([]);
  public uploadProgress$ = this.uploadProgressSubject.asObservable();
  
  // Flag to indicate if uploads are in progress
  private uploadingInProgress = false;
  private uploadingInProgressSubject = new BehaviorSubject<boolean>(false);
  public uploadingInProgress$ = this.uploadingInProgressSubject.asObservable();
  
  refId: any;
  segment: AdminSegment;


  getInitData(): Observable<InitData> {
    return this.getClean<InitData>(`AdminHome/GetInitData`)
    .pipe(tap(response => this.initData = response));
  }

  getInitDataSync(): InitData {
    return this.initData;
  }

  getHomeData(): Observable<AdminHomeData> {
    return this.getClean<AdminHomeData>(`AdminHome/GetHomeData`)
      .pipe(tap(response => this.homeData = response));
  }

  getHomeDataSync(): AdminHomeData {
    return this.homeData;
  }

  getSegment(segmentId: string) {
      if (!segmentId) {
        return null;
      }
      const homeData = this.getHomeDataSync();
      this.segment = homeData.segments.find((x) => x.general.id === segmentId);
      if (!this.segment) {
        return null;
      }
      return this.segment;
  }

  getSettingsSegment(segmentId: string):AdminSettingsSegment {
    if (!segmentId) {
      return null;
    }
    const homeData = this.getHomeDataSync();
    const settingsSegment = homeData.settings.find((x) => x.id === segmentId);
    if (!settingsSegment) {
      return null;
    }
    return settingsSegment;
  }


  /**
   * Submit form data to the server
   * @param controllerName The controller name
   * @param formData The form data to submit
   * @param formId Optional ID for edit mode
   * @returns Observable with the server response
   */
  submitForm(controllerName: string, formData: any, formId: string = null): Observable<ServerResponse<any>> {
    // Adapt form data to match server-side AdminItemModel structure
    const adaptedFormData = this.adaptFormDataForServer(formData);

    let params = new HttpParams();

    if (formId != null && formId != undefined) {
      params = params.set('id', formId);
    }

    // If there are files waiting to be uploaded, upload them first
    if (this.filesWaitingForUpload.length > 0) {
      // Mark upload as in progress
      this.uploadingInProgressSubject.next(true);
      
      // Initialize progress tracking for each file
      const progressData: FileUploadProgress[] = this.filesWaitingForUpload.map(file => ({
        fileName: file.fileName,
        progress: 0,
        status: 'pending' as const
      }));
      this.uploadProgressSubject.next(progressData);
      
      // Create an array of upload observables with progress tracking
      const uploadObservables = this.filesWaitingForUpload.map((pendingFile, index) => 
        this.uploadFileWithProgress(`${controllerName}/UploadFile`, pendingFile, index)
      );
      
      // Execute all uploads in parallel, then submit form
      return forkJoin(uploadObservables).pipe(
        switchMap(uploadResults => {
          // Update paths in the form data where needed
          this.updateFilePathsInFormData(adaptedFormData, this.filesWaitingForUpload, uploadResults);
          
          // Clear processed files
          this.filesWaitingForUpload = [];
          
          // Mark upload as completed
          this.uploadingInProgressSubject.next(false);
          
          // Submit the form with updated file paths
          return this.post<any>(`${controllerName}/Save`, adaptedFormData, params);
        }),
        catchError(error => {
          // Mark upload as completed even if error
          this.uploadingInProgressSubject.next(false);
          
          // Update all pending files to error status
          const currentProgress = this.uploadProgressSubject.getValue();
          const updatedProgress = currentProgress.map(item => {
            if (item.status === 'uploading' || item.status === 'pending') {
              return { ...item, status: 'error' as const, message: 'Upload failed' };
            }
            return item;
          });
          this.uploadProgressSubject.next(updatedProgress);
          
          throw error;
        })
      );
    }

    // If no files to upload, just submit the form
    return this.post<any>(`${controllerName}/Save`, adaptedFormData, params);
  }

  /**
   * Upload a file with progress tracking
   * @param url The API endpoint
   * @param pendingFile The file to upload
   * @param fileIndex Index of the file in the pending array
   */
  private uploadFileWithProgress(url: string, pendingFile: PendingFile, fileIndex: number): Observable<any> {
    return this.http.post(
      this.getServerUrl(url), 
      pendingFile.formData, 
      { 
        withCredentials: true,
        reportProgress: true,
        observe: 'events'
      }
    ).pipe(
      map((event: HttpEvent<any>) => {
        // Update progress based on event type
        const currentProgress = this.uploadProgressSubject.getValue();
        let updatedProgress = [...currentProgress];
        
        switch (event.type) {
          case HttpEventType.Sent:
            // Upload started
            updatedProgress[fileIndex] = {
              ...updatedProgress[fileIndex],
              status: 'uploading' as const,
              progress: 0
            };
            this.uploadProgressSubject.next(updatedProgress);
            break;
            
          case HttpEventType.UploadProgress:
            // Calculate progress percentage
            const progress = Math.round(100 * event.loaded / (event.total || 1));
            updatedProgress[fileIndex] = {
              ...updatedProgress[fileIndex],
              status: 'uploading' as const,
              progress: progress
            };
            this.uploadProgressSubject.next(updatedProgress);
            break;
            
          case HttpEventType.Response:
            // Upload completed
            updatedProgress[fileIndex] = {
              ...updatedProgress[fileIndex],
              status: 'completed' as const,
              progress: 100
            };
            this.uploadProgressSubject.next(updatedProgress);
            
            // Return the response body for further processing
            return event.body;
        }
        return null;
      }),
      catchError(error => {
        // Handle error for this specific file
        const currentProgress = this.uploadProgressSubject.getValue();
        const updatedProgress = [...currentProgress];
        updatedProgress[fileIndex] = {
          ...updatedProgress[fileIndex],
          status: 'error' as const,
          progress: 0,
          message: error.message || 'Upload failed'
        };
        this.uploadProgressSubject.next(updatedProgress);
        
        // Re-throw the error to be caught by the caller
        throw error;
      })
    );
  }

  /**
   * Upload a file to the server
   * @param file The file to upload
   * @param platform The platform identifier (1 = Desktop, 2 = Mobile)
   */
  uploadFile(file: File): void {
    const formData = new FormData();
    formData.append('file', file, file.name);

    this.filesWaitingForUpload.push({
      formData: formData,
      fileName: file.name
    });
  }

  /**
   * Delete a file from the pending uploads
   * @param fileName The name of the file to delete
   */
  deleteFile(fileName: string): void {
    this.filesWaitingForUpload = this.filesWaitingForUpload.filter(
      pending => pending.fileName !== fileName
    );
  }

  /**
   * Check if a file is pending upload
   * @param fileName The file name to check
   * @returns True if the file is pending upload
   */
  isFilePendingUpload(fileName: string): boolean {
    return this.filesWaitingForUpload.some(
      pending => pending.fileName === fileName
    );
  }

  /**
   * Get all pending files
   * @returns Array of pending file names
   */
  getPendingFiles(): string[] {
    return this.filesWaitingForUpload.map(file => file.fileName);
  }

  /**
   * Get the current upload progress for all files
   * @returns Array of file upload progress objects
   */
  getUploadProgress(): FileUploadProgress[] {
    return this.uploadProgressSubject.getValue();
  }

  /**
   * Check if uploads are currently in progress
   * @returns True if uploads are in progress
   */
  isUploadingInProgress(): boolean {
    return this.uploadingInProgressSubject.getValue();
  }

  /**
   * Check if there are files for a specific platform
   * @param platform The platform to check
   * @returns True if files exist for the platform
   */
  hasFilesForPlatform(platform: number): boolean {
    return this.filesWaitingForUpload.some(
      file => file.formData.get('platform')?.toString() === platform.toString()
    );
  }

  /**
   * Check if a value is a FileInfoMap object
   * FileInfoMap has platform keys (Desktop, Mobile, etc.) with arrays of file objects
   */
  private isFileInfoMap(value: any): boolean {
    if (!value || typeof value !== 'object' || Array.isArray(value)) {
      return false;
    }
    
    // Check if it has the platformFiles property
    if (value.platformFiles && typeof value.platformFiles === 'object') {
      // Get valid platform names (string keys) from the Platforms enum
      const platformKeys = Object.keys(Platforms)
        .filter(key => isNaN(Number(key)));  // Get only the named keys, not numeric values
      
      // Check if at least one key in platformFiles matches a platform name
      const hasValidKey = Object.keys(value.platformFiles).some(key => 
        platformKeys.map(k => k.toLowerCase()).includes(key.toLowerCase())
      );
      
      // Check if at least one platform has an array value
      const hasArrayValue = Object.values(value.platformFiles).some(val => Array.isArray(val));
      
      return hasValidKey && hasArrayValue;
    }
    
    return false;
  }

  /**
   * Update file paths in form data based on upload results
   * @param formData The form data to update
   * @param pendingFiles The pending files information
   * @param uploadResults The upload results containing paths
   */
  private updateFilePathsInFormData(formData: any, pendingFiles: PendingFile[], uploadResults: any[]): void {
    // Find all properties in the form data that might contain file information
    for (const key in formData) {
      if (typeof formData[key] === 'object' && formData[key] !== null) {
        const value = formData[key];
        
        // Check if it matches FileInfoMap structure (has platformFiles property)
        if (this.isFileInfoMap(value)) {
          let updated = false;
          
          // Process each platform's file array within the platformFiles property
          for (const platform in value.platformFiles) {
            if (Array.isArray(value.platformFiles[platform])) {
              // Update file paths in this platform's array
              value.platformFiles[platform].forEach((fileInfo: any) => {
                if (fileInfo && fileInfo.path && !fileInfo.isMarkedForDeletion) {
                  // Find matching pending file by name
                  const index = pendingFiles.findIndex(pf => pf.fileName === fileInfo.name);
                  
                  if (index !== -1 && index < uploadResults.length) {
                    // Update path with server response
                    const serverResponse = uploadResults[index];
                    if (serverResponse && serverResponse.data) {
                      fileInfo.path = serverResponse.data;
                      // Also update the URL to match the new path
                      if (fileInfo.url) {
                        fileInfo.url = serverResponse.data;
                      }
                      updated = true;
                    }
                  }
                }
              });
            }
          }
          
          // If we updated any paths, update the form data
          if (updated) {
            formData[key] = value;
          }
        } else {
          // Recursively check nested objects that aren't FileInfoMap
          this.updateFilePathsInFormData(value, pendingFiles, uploadResults);
        }
      }
    }
  }

  /**
   * Adapt form data to match the required server model structure
   * @param formData The original form data from the client
   * @returns Adapted form data for the server
   */
  private adaptFormDataForServer(formData: any): any {
    // Create a copy of the form data to avoid modifying the original
    const adaptedData = { ...formData };
    // logic before sending to server goes here
    return adaptedData;
  }

  public getConnectedUser(force: boolean = false): Observable<ConnectedUser | undefined> {
    console.warn("Get connected user is not implemented");
    return null;
  }

  public submitCustomAction(controllerName: string, actionName: string, data: any): Observable<ServerResponse<any>> {
    return this.post<any>(controllerName + '/' + actionName, data);
  }
}
