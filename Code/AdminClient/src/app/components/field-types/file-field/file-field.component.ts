import { Component, ElementRef, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseFieldComponent } from '../base-field.component';
import { FileMetadata } from 'src/app/interfaces/field-metadata';
import { ButtonModule } from 'primeng/button';
import { FileUploadModule } from 'primeng/fileupload';
import { RippleModule } from 'primeng/ripple';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { LoggerService } from 'src/app/services/logger.service';
import { AppService } from 'src/app/services/app.service';
import { BadgeModule } from 'primeng/badge';
import { DropdownModule } from 'primeng/dropdown';
import { FileContainer, FileContainerCollection, Platforms } from 'src/app/interfaces/fileInfo-model';




@Component({
  selector: 'app-file-field',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    FileUploadModule, 
    ButtonModule, 
    RippleModule,
    TooltipModule,
    ToastModule,
    BadgeModule,
    DropdownModule
  ],
  templateUrl: './file-field.component.html',
  providers: [MessageService]
})
export class FileFieldComponent extends BaseFieldComponent<FileContainerCollection, FileMetadata> implements OnInit {
  @ViewChild('fileUpload') fileUpload: any;
  
  // Platform configuration
  platformsEnum = Platforms;
  selectedPlatform: Platforms = Platforms.Desktop;
  supportedPlatforms: Platforms[] = [];
  showPlatformSelector: boolean = true;
  
  // File upload configuration
  acceptedFileTypes: string = '';
  allowMultiple: boolean = false;
  maxFileSize: number = 10; // Default 10MB
  isEditMode: boolean = true;
  errorMessage: string | null = null;
  originalValueFromServer:FileContainerCollection;
  
  // Helper property to get all files as a flat array
  get allFiles(): FileContainer[] {
    if (!this.value) return [];
    
    if (!this.value.platformFiles) return [];
    
    const values = Object.values(this.value.platformFiles);
    const valuesFlat = values.flat();
    return valuesFlat;
  }
  
  // Platform selector options
  get platformOptions() {
    return this.supportedPlatforms.map(platform => ({
      label: this.getPlatformName(platform),
      value: platform,
      icon: this.getPlatformIcon(platform)
    }));
  }

  constructor(
    private logger: LoggerService,
    private messageService: MessageService,
    private appService: AppService
  ) {
    super();
  }

  override ngOnInit(): void {
    super.ngOnInit();
    
    // Set configuration from metadata
    this.isEditMode = true;
    
    if (this.metadata?.allowedExtensions?.length) {
      this.acceptedFileTypes = this.metadata.allowedExtensions.map(ext => `.${ext}`).join(',');
    }
    
    this.maxFileSize = this.metadata?.maxSize || 10;
    this.allowMultiple = !!this.metadata?.multiple;
    
    // Configure platforms
    this.supportedPlatforms = this.getSupportedPlatforms();
    this.showPlatformSelector = this.supportedPlatforms.length > 1;
    
    if (this.supportedPlatforms.length > 0) {
      this.selectedPlatform = this.supportedPlatforms[0];
    }
    
    // Initialize value if needed
    if (!this.value) {
      this.value = { platformFiles: {} };
    }
  }

  /**
   * Parse the value from server into file infos
   */
  override parseValueFromServer(value: FileContainerCollection): FileContainerCollection {
    // If value is empty, return an empty object with platformFiles property
    if (!value) {
      return { platformFiles: {} };
    }

    try {
      // Value from server could be a string or object
      let serverData: any;
      
      if (typeof value === 'string') {
        serverData = JSON.parse(value as string);
      } else {
        serverData = value;
      }
      
      // Initialize result with the new structure
      const result: FileContainerCollection = { platformFiles: {} };
      
      // Check if the serverData is already in the new format
      if (serverData.platformFiles) {
        // It's already in the new format, process the platformFiles
        Object.keys(serverData.platformFiles).forEach(platform => {
          result.platformFiles[platform] = [];
          
          serverData.platformFiles[platform].forEach((fileData: any) => {
            // Handle both "path" and "Path" from the server (case sensitivity issue)
            const filePath = fileData.path || fileData.Path || '';
            
            // Extract filename from path
            const fileName = fileData.name || fileData.Name || 
                            (filePath ? this.extractFileName(filePath) : 'Unknown File');
            
            const fileInfo: FileContainer = {
              name: fileName,
              size: fileData.size || fileData.Size || 0,
              path: filePath,
              platform: platform,
              isNew: false,
            };
            
            result.platformFiles[platform].push(fileInfo);
          });
        });
      } 
      this.logger.debug('Parsed file data from server:', result);
      this.originalValueFromServer = JSON.parse(JSON.stringify(result));
      return result;
    } catch (error) {
      this.logger.error('Error parsing file data:', error);
      this.showErrorMessage('Error parsing file data');
      return { platformFiles: {} };
    }
  }

  /**
   * Extract filename from a file path
   */
  private extractFileName(path: string): string {
    return path.split('/').pop() || path.split('\\').pop() || path;
  }

  /**
   * Convert file infos to the format expected by the server
   */
  override getValueForServer(files: FileContainerCollection): FileContainerCollection {
    // If no files, return null
    if (!files || !files.platformFiles || Object.keys(files.platformFiles).length === 0) {
      return null;
    }
    
    // Group files by platform and include only necessary data
    const result: FileContainerCollection = { platformFiles: {} };
    
    Object.keys(files.platformFiles || {}).forEach(platform => {
      if (!result.platformFiles[platform]) {
        result.platformFiles[platform] = [];
      }
      
      // Process each file in this platform
      files.platformFiles[platform].forEach(fileInfo => {
        // Only include files that are not marked for deletion
        if (fileInfo) {
          result.platformFiles[platform].push({
            path: fileInfo.path,
            name: fileInfo.name,
            size: fileInfo.size,
            platform: fileInfo.platform,
            isNew: undefined, // now need for the server
            file: undefined, // now need for the server
          });
        }
      });
    });
    
    // Check for files from server that have been removed and mark them for deletion
    if (this.originalValueFromServer && this.originalValueFromServer.platformFiles) {
      Object.keys(this.originalValueFromServer.platformFiles).forEach(platform => {
        if (!result.platformFiles[platform]) {
          result.platformFiles[platform] = [];
        }
        
        // For each file in the original data
        this.originalValueFromServer.platformFiles[platform].forEach(originalFile => {
          // Check if this file exists in the current data
          const existsInCurrent = files.platformFiles[platform] && 
            files.platformFiles[platform].some(f => f.path === originalFile.path);
          
          // If not found in current data, add it with deletion flag
          if (!existsInCurrent) {
            result.platformFiles[platform].push({
              path: originalFile.path,
              name: originalFile.name,
              size: originalFile.size,
              platform: originalFile.platform,
              isNew: undefined,
              file: undefined,
              isMarkedForDeletion: true
            });
          }
        });
      });
    }

    return result;
  }

  /**
   * Handle file upload
   */
  onUpload(event: any): void {
    this.errorMessage = null;
    this.logger.debug('FileFieldComponent.onUpload event:', event);
    
    // Extract files from the event
    let files: File[] = [];
    
    if (event.files && event.files.length) {
      files = Array.from(event.files);
    } else if (event.currentFiles && event.currentFiles.length) {
      files = Array.from(event.currentFiles);
    } else if (event.originalEvent && event.originalEvent.files && event.originalEvent.files.length) {
      files = Array.from(event.originalEvent.files);
    }
    
    if (files.length === 0) {
      this.showErrorMessage('No file selected');
      return;
    }
    
    // Process the selected files
    files.forEach(file => this.validateAndStoreFile(file));
    
    // Clear the file input after processing
    if (this.fileUpload) {
      this.fileUpload.clear();
    }
  }

  /**
   * Validate and store a file if it passes all checks
   */
  private validateAndStoreFile(file: File): void {
    // Validate file type if specified
    if (this.metadata?.allowedExtensions?.length) {
      const fileExt = this.getFileExtension(file.name).toLowerCase();
      if (!this.metadata.allowedExtensions.map(ext => ext.toLowerCase()).includes(fileExt)) {
        this.showErrorMessage(`File type .${fileExt} is not allowed. Allowed types: ${this.metadata.allowedExtensions.join(', ')}`);
        return;
      }
    }
    
    // Validate file size
    if (this.maxFileSize && file.size > this.maxFileSize * 1024 * 1024) {
      this.showErrorMessage(`File size exceeds the maximum allowed size of ${this.maxFileSize}MB`);
      return;
    }
    
    // Store the file
    this.storeFile(file);
  }

  /**
   * Store a file for later upload and add to files array
   */
  private storeFile(file: File): void {
    // Clear any previous error
    this.errorMessage = null;
    
    // Get platform name for current selection
    const platformName = this.getPlatformName(this.selectedPlatform).toString().toLowerCase();
    
    // Ensure we have an object with platformFiles property to work with
    if (!this.value) {
      this.value = { platformFiles: {} };
    }
    
    if (!this.value.platformFiles) {
      this.value.platformFiles = {};
    }
    
    // Initialize array for this platform if needed
    if (!this.value.platformFiles[platformName]) {
      this.value.platformFiles[platformName] = [];
    }
    
    // If not allowing multiple files, clear existing files for this platform by deleting them
    if (!this.allowMultiple) {
      this.value.platformFiles[platformName].forEach(f => this.removeFile({file: f}));
      this.value.platformFiles[platformName] = [];
    }

    // Check if the file is already uploaded and replace it if so
    if (this.appService.isFilePendingUpload(file.name) || 
        this.value.platformFiles[platformName].find(f => f.name === file.name)) {
        this.appService.deleteFile(file.name);
        this.value.platformFiles[platformName] = this.value.platformFiles[platformName].filter(f => f.name !== file.name);
    }
    
    // Create file info object
    const fileInfo: FileContainer = {
      name: file.name,
      size: file.size,
      path: file.name, // Initially use filename as path (will be updated after upload)
      platform: platformName,
      isNew: true,
      file: file,
    };
    
    // Add to the platform's files array
    this.value.platformFiles[platformName].push(fileInfo);

    // Add file to the app service for later upload
    this.appService.uploadFile(file);
    
    // Emit value change to update the form
    this.emitValueChange();
  }

  /**
   * Get file extension from filename
   */
  private getFileExtension(filename: string): string {
    return filename?.split('.').pop() || '';
  }

  /**
   * Remove a file
   */
  removeFile(event: any): void {
    // Handle both direct file objects and event objects
    const file: FileContainer = event.file ? event.file : event;
    this.logger.debug('File to remove:', file);
    
    // Get platform name for current selection
    const platformName = this.getPlatformName(this.selectedPlatform).toString().toLowerCase();
    
    // Check if we have files for this platform
    if (!this.value || !this.value.platformFiles || !this.value.platformFiles[file.platform || platformName]) {
      return;
    }
    
    // Find the file in our array
    const index = this.value.platformFiles[file.platform || platformName].findIndex(f => f.name === file.name);
    
    if (index >= 0) {
      // Mark file for deletion instead of removing it
      this.appService.deleteFile(file.name);
      this.value.platformFiles[file.platform || platformName] = this.value.platformFiles[file.platform || platformName].filter(f => f.name !== file.name);
      
      this.emitValueChange();
    }
  }

  /**
   * Check if a file is uploaded
   */
  isFileUploaded(file: FileContainer): boolean {
    return !file.isNew || !this.appService.isFilePendingUpload(file.name);
  }

  /**
   * Emit value change event
   */
  private emitValueChange(): void {
    this.valueChange.emit(this.value);
  }

  /**
   * Get supported platforms from metadata
   */
  getSupportedPlatforms(): Platforms[] {
    const platforms: Platforms[] = [];
    const platformValue = this.metadata?.platforms || Platforms.All;
    
    // Check each platform flag using bitwise operations
    if ((platformValue & Platforms.Desktop) === Platforms.Desktop) {
      platforms.push(Platforms.Desktop);
    }
    
    if ((platformValue & Platforms.Tablet) === Platforms.Tablet) {
      platforms.push(Platforms.Tablet);
    }
    
    if ((platformValue & Platforms.Mobile) === Platforms.Mobile) {
      platforms.push(Platforms.Mobile);
    }
    
    if ((platformValue & Platforms.App) === Platforms.App) {
      platforms.push(Platforms.App);
    }
    
    if ((platformValue & Platforms.Custom1) === Platforms.Custom1) {
      platforms.push(Platforms.Custom1);
    }
    
    if ((platformValue & Platforms.Custom2) === Platforms.Custom2) {
      platforms.push(Platforms.Custom2);
    }
    
    if ((platformValue & Platforms.Custom3) === Platforms.Custom3) {
      platforms.push(Platforms.Custom3);
    }
    
    // If no platforms are found, default to Desktop
    if (platforms.length === 0) {
      platforms.push(Platforms.Desktop);
    }
    
    return platforms;
  }

  /**
   * Get platform name from enum value
   */
  getPlatformName(platform: Platforms): string {
    return Platforms[platform];
  }
  
  /**
   * Get platform value from name
   */
  getPlatformValueFromName(name: string): string {
    // Find the enum value by name
    const value = Object.entries(Platforms)
      .find(([key, val]) => key === name)?.[1];
    
    return value ? value.toString() : '1'; // Default to Desktop (1)
  }
  
  /**
   * Get platform name from value
   */
  getPlatformNameFromValue(value: number): string {
    return Platforms[value] || 'Desktop';
  }
  
  /**
   * Get platform icon class
   */
  getPlatformIcon(platform: Platforms): string {
    switch (platform) {
      case Platforms.Desktop:
        return 'pi pi-desktop';
      case Platforms.Tablet:
        return 'pi pi-tablet';
      case Platforms.Mobile:
        return 'pi pi-mobile';
      case Platforms.App:
        return 'pi pi-android';
      case Platforms.Custom1:
      case Platforms.Custom2:
      case Platforms.Custom3:
        return 'pi pi-cog';
      default:
        return 'pi pi-file';
    }
  }
  
  /**
   * Get file icon based on file extension
   */
  getFileIcon(fileName: string): string {
    const ext = this.getFileExtension(fileName).toLowerCase();
    
    switch (ext) {
      case 'pdf':
        return 'pi pi-file-pdf';
      case 'doc':
      case 'docx':
        return 'pi pi-file-word';
      case 'xls':
      case 'xlsx':
        return 'pi pi-file-excel';
      case 'ppt':
      case 'pptx':
        return 'pi pi-file-powerpoint';
      case 'jpg':
      case 'jpeg':
      case 'png':
      case 'gif':
      case 'bmp':
      case 'svg':
        return 'pi pi-image';
      case 'mp4':
      case 'avi':
      case 'mov':
      case 'wmv':
        return 'pi pi-video';
      case 'mp3':
      case 'wav':
      case 'ogg':
        return 'pi pi-volume-up';
      case 'zip':
      case 'rar':
      case '7z':
        return 'pi pi-file-archive';
      default:
        return 'pi pi-file';
    }
  }

  /**
   * Format file size for display
   */
  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  /**
   * Show error message
   */
  private showErrorMessage(message: string): void {
    this.errorMessage = message;
    this.messageService.add({
      severity: 'error',
      summary: 'Error',
      detail: message
    });
  }

  /**
   * Clear error message
   */
  clearError(): void {
    this.errorMessage = null;
  }

  // /**
  //  * Clear all files from the uploader
  //  */
  // clearFiles(): void {
  //   // Get all pending files and delete them from service
  //   const pendingFiles = this.allFiles
  //     .filter(file => !file.alreadyUploaded)
  //     .map(file => file.name);
    
  //   pendingFiles.forEach(fileName => {
  //     this.appService.deleteFile(fileName);
  //   });
    
  //   // Clear files object
  //   this.value = null;
    
  //   // Reset file upload component if available
  //   if (this.fileUpload) {
  //     this.fileUpload.clear();
  //   }
    
  //   // Emit value change
  //   this.emitValueChange();
  // }

  override getEditTemplate(): TemplateRef<ElementRef> {
    return this.editTemplateRef;
  }

  override getListTemplate(): TemplateRef<ElementRef> {
    return this.listTemplateRef;
  }

  override getViewTemplate(): TemplateRef<ElementRef> {
    return this.viewTemplateRef;
  }

  /**
   * Handle value changes and emit the converted value
   */
  override onValueChange(value: FileContainerCollection): void {
    this.value = value;
    this.valueChange.emit(value);
  }
} 