import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { TableModule } from 'primeng/table';
import { ToastModule } from 'primeng/toast';
import { ImportResult } from 'src/app/interfaces/import-result';
import { MessageService } from 'primeng/api';
import { AdminSegment } from 'src/app/interfaces/models';
import { ImportExportService } from 'src/app/services/import-export.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'admin-import-dialog',
  templateUrl: './import-dialog.component.html',
  standalone: true,
  imports: [CommonModule, DialogModule, ButtonModule, TableModule, ToastModule],
  providers: [MessageService],
})
export class ImportDialogComponent {
  @Input() segment: AdminSegment;
  @Input() refId: string;

  // Dialog visibility states
  importDialogVisible: boolean = false;
  importErrorsDialogVisible: boolean = false;

  // Import state
  selectedFile: File | null = null;
  importError: string | null = null;
  importResult: ImportResult | null = null;
  loading: boolean = false;

  // Events
  @Output() importComplete = new EventEmitter<boolean>();

  constructor(
    private importExportService: ImportExportService,
    private messageService: MessageService
  ) {}

  /**
   * Opens the import dialog
   */
  open(): void {
    this.reset();
    this.importDialogVisible = true;
  }

  /**
   * Closes all dialogs
   */
  close(): void {
    this.importDialogVisible = false;
    this.importErrorsDialogVisible = false;
  }

  cancelImport(): void {
    this.selectedFile = null;
    this.importDialogVisible = false;
  }

  /**
   * Resets the component state
   */
  reset(): void {
    this.selectedFile = null;
    this.importError = null;
    this.importResult = null;
  }

  /**
   * Handles file selection for import
   */
  onFileSelected(event: any): void {
    this.importError = null;
    const files = event.target.files;

    if (files.length > 0) {
      const file = files[0];
      const validExtensions = ['.xlsx', '.xls', '.csv'];
      const fileExt = file.name
        .substring(file.name.lastIndexOf('.'))
        .toLowerCase();

      if (validExtensions.includes(fileExt)) {
        this.selectedFile = file;
      } else {
        this.importError = 'Please select a valid Excel or CSV file';
        this.selectedFile = null;
      }
    } else {
      this.selectedFile = null;
    }
  }

  /**
   * Uploads the selected file for import processing
   */
  uploadImportFile(): void {
    if (!this.selectedFile || !this.segment) {
      return;
    }

    this.loading = true;
    this.importDialogVisible = false;

    this.importExportService
      .importData(this.segment, this.selectedFile, this.refId)
      .pipe(
        finalize(() => {
          this.selectedFile = null;
          this.loading = false;
        })
      )
      .subscribe({
        next: (response) => {
          if (response.result) {
            // Show success message
            this.messageService.add({
              severity: 'success',
              summary: 'Import Successful',
              detail: `Successfully imported ${
                this.importResult?.successCount || 0
              } records from ${this.selectedFile?.name}`,
            });
            this.importComplete.emit(true);
          } else {
            // Check if there are import errors
            this.importResult = response.data;

            if (this.importResult && this.importResult.errorCount > 0) {
              // Show error dialog with detailed errors
              this.importErrorsDialogVisible = true;

              // Show a summary message
              this.messageService.add({
                severity: 'warn',
                summary: 'Import Completed with Errors',
                detail: `Imported ${this.importResult.successCount} of ${this.importResult.totalRecords} records. See error details.`,
              });
            } else {
              this.messageService.add({
                severity: 'error',
                summary: 'Import Failed',
                detail: response.error || 'An error occurred during import',
              });
            }
            // Emit complete event with success = false
            this.importComplete.emit(false);
          }
        },
        error: (error) => {
          console.error('Error importing data:', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Import Failed',
            detail:
              'An error occurred during import. Please check the file format.',
          });

          // Emit complete event with success = false
          this.importComplete.emit(false);
        },
      });
  }

  /**
   * Downloads an import template for the current entity
   */
  downloadImportTemplate(): void {
    if (!this.segment) {
      return;
    }

    this.messageService.add({
      severity: 'info',
      summary: 'Template Requested',
      detail: 'Generating import template...',
    });

    this.importExportService.downloadImportTemplate(this.segment).subscribe({
      next: (response) => {
        // Create a URL for the blob
        const url = window.URL.createObjectURL(response.body);

        // Create a link and click it to trigger download
        const a = document.createElement('a');
        a.href = url;
        a.download = `${this.segment.general.name}_ImportTemplate.xlsx`;
        document.body.appendChild(a);
        a.click();

        // Clean up
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
      },
      error: (error) => {
        console.error('Error downloading template:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Download Failed',
          detail: 'Failed to download import template',
        });
      },
    });
  }
}
