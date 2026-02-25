import { Component, ElementRef, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseFieldComponent } from '../base-field.component';
import { PictureMetadata } from 'src/app/interfaces/field-metadata';
import { ImageModule } from 'primeng/image';
import { ButtonModule } from 'primeng/button';
import { FileUploadModule } from 'primeng/fileupload';
import { ToastModule } from 'primeng/toast';
import { ProgressBarModule } from 'primeng/progressbar';
import { MessagesModule } from 'primeng/messages';
import { MessageModule } from 'primeng/message';
import { MessageService } from 'primeng/api';
import { LoggerService } from 'src/app/services/logger.service';
import { AppService } from 'src/app/services/app.service';
import { RippleModule } from 'primeng/ripple';
import { catchError, finalize } from 'rxjs/operators';
import { of } from 'rxjs';
import { FileContainer } from 'src/app/interfaces/fileInfo-model';

// Image format constants
enum ForceFormat {
  None = 0,
  JPG = 1,
  PNG = 2,
  WebP = 3
}

// Platform constants
enum Platforms {
  None = 0,
  Desktop = 1,
  Mobile = 2,
  All = 3 // Desktop | Mobile
}

// Interface to represent platform information for files
interface PlatformFiles {
  [platform: string]: FileData[];
}

// Simplified representation of file data for the backend
interface FileData {
  path: string;
}

@Component({
  selector: 'app-picture-field',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    ImageModule, 
    ButtonModule, 
    FileUploadModule, 
    ToastModule,
    ProgressBarModule,
    MessagesModule,
    MessageModule,
    RippleModule
  ],
  templateUrl: './picture-field.component.html',
  providers: [MessageService]
})
export class PictureFieldComponent extends BaseFieldComponent<string, PictureMetadata> implements OnInit {

  uploadInProgress = false;
  acceptedFileTypes: string = '';
  errorMessage: string | null = null;
  fileInfos: FileContainer[] = [];
  platformsEnum = Platforms;
  selectedPlatform: Platforms = Platforms.Desktop;
  filesByPlatform: { platform: string; files: FileContainer[] }[] = [];

  constructor(
    private logger: LoggerService, 
    private messageService: MessageService,
    private appService: AppService
  ) {
    super();
  }

  override ngOnInit(): void {
    super.ngOnInit();
    this.prepareAcceptedFileTypes();

    // Parse existing value (if any)
    if (this.value) {
      this.parseValueFromServer(this.value);
    }
  }

  /**
   * Prepare accepted file types string based on metadata
   */
  private prepareAcceptedFileTypes(): void {
    // Create mapping from extensions to MIME types
    const extensionToMimeType: Record<string, string> = {
      'jpg': 'image/jpeg',
      'jpeg': 'image/jpeg',
      'png': 'image/png',
      'gif': 'image/gif',
      'svg': 'image/svg+xml',
      'webp': 'image/webp'
    };

    if (this.metadata.allowedTypes && this.metadata.allowedTypes.length > 0) {
      this.acceptedFileTypes = this.metadata.allowedTypes.join(',');
    } else if (this.metadata.allowedExtensions && this.metadata.allowedExtensions.length > 0) {
      // Convert extensions to MIME types
      const mimeTypes = this.metadata.allowedExtensions.map(ext => {
        const lowerExt = ext.toLowerCase();
        return extensionToMimeType[lowerExt] || `.${lowerExt}`;
      });
      this.acceptedFileTypes = mimeTypes.join(',');
    } else {
      this.acceptedFileTypes = 'image/*';
    }

    this.logger.debug('Accepted file types set to:', this.acceptedFileTypes);
  }

  /**
   * Parse the JSON value from server into file infos
   */
  override parseValueFromServer(value: string): string {
    if (!value) {
      this.fileInfos = [];
      return value;
    }

    try {
      const rawValue: any = value as any;
      let serverData: any;

      if (typeof rawValue === 'string') {
        const trimmed = rawValue.trim();
        if (trimmed.startsWith('{') || trimmed.startsWith('[')) {
          serverData = JSON.parse(rawValue);
        } else {
          // Treat a plain path as a single file for the default platform
          serverData = { Desktop: [{ path: rawValue }] };
        }
      } else {
        serverData = rawValue;
      }

      const platformFiles: PlatformFiles = serverData?.platformFiles || serverData;
      this.fileInfos = [];

      // Process each platform's files
      Object.keys(platformFiles || {}).forEach(platform => {
        platformFiles[platform].forEach(fileData => {
          const fileDataAny = fileData as any;
          const filePath = fileDataAny.path || fileDataAny.Path || '';
          if (!filePath) {
            return;
          }

          // Extract filename from path
          const fileName = filePath.split('/').pop() || filePath.split('\\').pop() || filePath;
          
          this.fileInfos.push({
            name: fileName,
            size: 0, // Size isn't stored in the server response
            path: filePath,
            platform,
            isNew: false,
          });
        });
      });

      this.filesByPlatform = this.getFilesByPlatform();

      this.logger.debug('Parsed image data from server:', this.fileInfos);
    } catch (error) {
      this.logger.error('Error parsing image data:', error);
      this.showErrorMessage('Error parsing image data');
      return null;
    }

    return value;
  }

  /**
   * Convert file infos to the format expected by the server
   */
  override getValueForServer(value: string): string {
    // Group files by platform
    const platformFiles: PlatformFiles = {};
    
    this.fileInfos.forEach(fileInfo => {
      if (!platformFiles[fileInfo.platform]) {
        platformFiles[fileInfo.platform] = [];
      }
      
      // Only include the path in the data sent to server
      platformFiles[fileInfo.platform].push({
        path: fileInfo.path
      });
    });
    
    // Return stringified JSON
    const result = JSON.stringify(platformFiles);
    this.logger.debug('Image data for server:', result);
    return result;
  }

  /**
   * Get the main image URL for display
   */
  getMainImageUrl(): string | null {
    if (this.fileInfos.length === 0) {
      return null;
    }

    // Prioritize Desktop platform images
    const desktopImages = this.fileInfos.filter(f => f.platform === Platforms[Platforms.Desktop]);
    if (desktopImages.length > 0) {
      return desktopImages[0].path || desktopImages[0].path;
    }

    // Fall back to any platform
    return this.fileInfos[0].path || this.fileInfos[0].path;
  }

  /**
   * Validate file type against allowed types
   */
  private validateFileType(file: File): boolean {
    if (!this.metadata.allowedExtensions || this.metadata.allowedExtensions.length === 0) {
      return true; // No restrictions
    }

    const fileExt = file.name.split('.').pop()?.toLowerCase();
    if (!fileExt) {
      this.errorMessage = 'Invalid file format: no file extension detected';
      this.showErrorMessage(this.errorMessage);
      return false;
    }
    
    const isValid = this.metadata.allowedExtensions.some(ext => 
      ext.toLowerCase() === fileExt
    );

    if (!isValid) {
      this.errorMessage = `Invalid file type. Allowed formats: ${this.getAllowedExtensionsText()}`;
      this.showErrorMessage(this.errorMessage);
    }

    return isValid;
  }

  /**
   * Validate file size against max size
   */
  private validateFileSize(file: File): boolean {
    if (!this.metadata.maxSize) {
      return true; // No size limit
    }

    const fileSizeInMB = file.size / (1024 * 1024);
    const isValid = fileSizeInMB <= this.metadata.maxSize;

    if (!isValid) {
      this.errorMessage = `File is too large. Maximum size is ${this.metadata.maxSize} MB`;
      this.showErrorMessage(this.errorMessage);
    }

    return isValid;
  }

  /**
   * Upload an image to the server
   */
  private uploadFile(file: File): void {
    this.uploadInProgress = true;
    this.errorMessage = null;
    
    // Get the controller name, default to 'File'
    const controllerName = this.metadata?.uploadUrl || 'File';
    
    // Check if we need to generate a preview before upload
    if (file.type.startsWith('image/')) {
      this.createImagePreview(file);
    }
    
    // // Call the app service to upload the file
    // this.appService.uploadFile(file, controllerName, this.selectedPlatform)
    //   .pipe(
    //     catchError(error => {
    //       this.logger.error('Error uploading image:', error);
    //       this.showErrorMessage('Error uploading image: ' + (error.message || 'Unknown error'));
    //       return of(null);
    //     }),
    //     finalize(() => {
    //       this.uploadInProgress = false;
    //     })
    //   )
    //   .subscribe(response => {
    //     if (response && response.result && response.data) {
    //       // Extract filename from path
    //       const fileName = file.name;
    //       const filePath = response.data;
          
    //       // Add to fileInfos
    //       this.fileInfos.push({
    //         name: fileName,
    //         size: file.size,
    //         path: filePath,
    //         platform: Platforms[this.selectedPlatform],
    //         isNew: true,
    //         imageUrl: filePath
    //       });
          
    //       this.emitValueChange();
    //       this.showSuccessMessage('Image uploaded successfully');
    //     } else {
    //       this.showErrorMessage('Failed to upload image');
    //     }
    //   });
  }

  /**
   * Create image preview for a file
   */
  private createImagePreview(file: File): void {
    const reader = new FileReader();
    reader.onload = (event: ProgressEvent<FileReader>) => {
      const imageUrl = event.target?.result as string;
      // This is just for preview - we don't add to fileInfos until server upload completes
      this.logger.debug('Image preview created:', imageUrl);
    };
    reader.readAsDataURL(file);
  }

  /**
   * Emit value change event
   */
  private emitValueChange(): void {
    // Set the value to trigger valueChange
    const newValue = this.getValueForServer(null);
    this.value = newValue;
    this.valueChange.emit(newValue);
  }

  /**
   * Get text representation of the force format value
   */
  getForceFormatText(): string {
    const formatValue = this.metadata.forceFormat || 0;
    
    switch (formatValue) {
      case ForceFormat.PNG:
        return 'PNG format';
      case ForceFormat.JPG:
        return 'JPEG format';
      case ForceFormat.WebP:
        return 'WebP format';
      default:
        return 'original format';
    }
  }

  /**
   * Get supported platforms from metadata
   */
  getSupportedPlatforms(): Platforms[] {
    const platforms: Platforms[] = [];
    const platformValue = this.metadata?.platforms || Platforms.All;
    
    if ((platformValue & Platforms.Desktop) === Platforms.Desktop) {
      platforms.push(Platforms.Desktop);
    }
    
    if ((platformValue & Platforms.Mobile) === Platforms.Mobile) {
      platforms.push(Platforms.Mobile);
    }
    
    return platforms;
  }

  /**
   * Change selected platform for file upload
   */
  onPlatformChange(platform: Platforms): void {
    this.selectedPlatform = platform;
  }

  /**
   * Get files for a specific platform
   */
  getFilesForPlatform(platform: string): FileContainer[] {
    return this.fileInfos.filter(file => file.platform === platform);
  }

  /**
   * Handle file upload from either basic or advanced mode
   */
  onFileSelect(event: any): void {
    this.errorMessage = null;
    this.logger.debug('PictureFieldComponent.onFileSelect event:', event);
    
    // Check if multiple images are allowed
    if (!this.metadata?.multiple && this.fileInfos.length > 0) {
      this.showErrorMessage('Only one image can be uploaded for this field');
      return;
    }
    
    let files: File[] = [];
    
    // Handle different event structures between basic and advanced modes
    if (event.files && event.files.length) {
      // Advanced mode with multiple files
      files = event.files;
    } else if (event.currentFiles && event.currentFiles.length) {
      // Some PrimeNG versions use currentFiles
      files = event.currentFiles;
    } else if (event.originalEvent && event.originalEvent.files && event.originalEvent.files.length) {
      // Basic mode
      files = Array.from(event.originalEvent.files);
    }
    
    if (files.length === 0) {
      this.errorMessage = 'No file selected';
      this.logger.error('No file found in upload event', event);
      this.showErrorMessage('No file selected');
      return;
    }
    
    // Process each selected file
    files.forEach(file => {
      // Validate file type
      if (!this.validateFileType(file)) {
        return;
      }
      
      // Validate file size
      if (!this.validateFileSize(file)) {
        return;
      }
      
      // Upload file
      this.uploadFile(file);
    });
  }

  /**
   * Show success message
   */
  private showSuccessMessage(message: string): void {
    this.messageService.add({
      severity: 'success',
      summary: 'Success',
      detail: message,
      life: 3000
    });
  }

  /**
   * Show error message
   */
  private showErrorMessage(message: string): void {
    this.messageService.add({
      severity: 'error',
      summary: 'Error',
      detail: message,
      life: 5000
    });
  }

  /**
   * Remove a file
   */
  removeFile(index: number): void {
    const fileInfo = this.fileInfos[index];
    
    // If it's a newly uploaded file, we may want to delete it from the server
    if (fileInfo && fileInfo.path) {
      // Optional: Call delete API if the file was already uploaded
      // this.appService.deleteFile(fileInfo.path).subscribe(...);
    }
    
    this.fileInfos.splice(index, 1);
    this.emitValueChange();
  }

  /**
   * Handle file removal - clear all files
   */
  onClear(): void {
    this.fileInfos = [];
    this.emitValueChange();
    this.errorMessage = null;
  }

  /**
   * Get file size in human-readable format
   */
  formatSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  /**
   * Get allowed extensions as a formatted string
   */
  getAllowedExtensionsText(): string {
    if (this.metadata.allowedExtensions && this.metadata.allowedExtensions.length > 0) {
      return this.metadata.allowedExtensions.map(ext => `.${ext.toLowerCase()}`).join(', ');
    }
    return 'All image formats';
  }

  openImageInNewTab(path: string, event?: MouseEvent): void {
    if (!path) {
      return;
    }

    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }

    window.open(path, '_blank', 'noopener');
  }

  private getFilesByPlatform(): { platform: string; files: FileContainer[] }[] {
    const grouped = new Map<string, FileContainer[]>();

    this.fileInfos.forEach(file => {
      const platform = file.platform || 'Unknown';
      if (!grouped.has(platform)) {
        grouped.set(platform, []);
      }
      grouped.get(platform)!.push(file);
    });

    return Array.from(grouped.entries()).map(([platform, files]) => ({
      platform,
      files
    }));
  }

  override getEditTemplate(): TemplateRef<ElementRef> {
    return this.editTemplateRef;
  }

  override getListTemplate(): TemplateRef<ElementRef> {
    return this.listTemplateRef;
  }

  override getViewTemplate(): TemplateRef<ElementRef> {
    return this.viewTemplateRef;
  }
}