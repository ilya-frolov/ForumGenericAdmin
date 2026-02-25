import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { ProgressBarModule } from 'primeng/progressbar';
import { ButtonModule } from 'primeng/button';
import { AppService, FileUploadProgress } from '../../services/app.service';
import { Subscription, timer } from 'rxjs';
import { trigger, transition, style, animate } from '@angular/animations';

@Component({
  selector: 'app-upload-progress-dialog',
  standalone: true,
  imports: [
    CommonModule,
    DialogModule,
    ProgressBarModule,
    ButtonModule
  ],
  templateUrl: './upload-progress-dialog.component.html',
  animations: [
    trigger('fadeInOut', [
      transition(':enter', [
        style({ opacity: 0, transform: 'scale(0.8)' }),
        animate('300ms ease-out', style({ opacity: 1, transform: 'scale(1)' }))
      ]),
      transition(':leave', [
        animate('200ms ease-in', style({ opacity: 0, transform: 'scale(0.8)' }))
      ])
    ]),
    trigger('svgAnimation', [
      transition(':enter', [
        style({ opacity: 0, transform: 'scale(0.5)' }),
        animate('500ms 100ms ease-out', style({ opacity: 1, transform: 'scale(1)' }))
      ])
    ])
  ]
})
export class UploadProgressDialogComponent implements OnInit, OnDestroy {
  visible = false;
  uploadProgress: FileUploadProgress[] = [];
  overallProgress = 0;
  
  // Completion status
  completionState: 'uploading' | 'success' | 'error' | null = null;
  showCompletionScreen = false;
  completionMessage = '';
  
  // Auto-close timer
  autoCloseSeconds = 8; // How long to keep dialog open after completion
  timeRemaining = 0;
  countdownInterval: any;
  
  private progressSubscription: Subscription;
  private uploadingSubscription: Subscription;
  
  constructor(private appService: AppService) {}
  
  ngOnInit(): void {
    // Subscribe to upload progress updates
    this.progressSubscription = this.appService.uploadProgress$.subscribe(progress => {
      this.uploadProgress = progress;
      this.calculateOverallProgress();
      this.checkCompletionStatus();
    });
    
    // Subscribe to uploading status to show/hide dialog
    this.uploadingSubscription = this.appService.uploadingInProgress$.subscribe(isUploading => {
      // Only handle opening the dialog here - we'll control closing separately
      if (isUploading) {
        this.visible = true;
        this.showCompletionScreen = false;
        this.completionState = 'uploading';
        this.stopCountdown();
      }
    });
  }
  
  ngOnDestroy(): void {
    if (this.progressSubscription) {
      this.progressSubscription.unsubscribe();
    }
    
    if (this.uploadingSubscription) {
      this.uploadingSubscription.unsubscribe();
    }
    
    this.stopCountdown();
  }
  
  /**
   * Check if uploads are complete and show the appropriate completion screen
   */
  private checkCompletionStatus(): void {
    if (this.allComplete && this.uploadProgress.length > 0) {
      if (this.hasErrors) {
        this.showCompletionWithState('error', 'Some files failed to upload');
      } else {
        this.showCompletionWithState('success', 'All files uploaded successfully');
      }
    }
  }
  
  /**
   * Show the completion screen with the specified state and message
   */
  private showCompletionWithState(state: 'success' | 'error', message: string): void {
    // Only update if this is a new completion state to avoid resetting timers unnecessarily
    if (!this.showCompletionScreen) {
      this.completionState = state;
      this.completionMessage = message;
      this.showCompletionScreen = true;
      
      // Start the countdown for auto-close
      this.startCountdown();
    }
  }
  
  /**
   * Start countdown for auto-closing the dialog
   */
  private startCountdown(): void {
    this.stopCountdown(); // Clear any existing countdown
    this.timeRemaining = this.autoCloseSeconds;
    
    this.countdownInterval = setInterval(() => {
      this.timeRemaining--;
      if (this.timeRemaining <= 0) {
        this.close();
      }
    }, 1000);
  }
  
  /**
   * Stop the auto-close countdown
   */
  private stopCountdown(): void {
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
      this.countdownInterval = null;
    }
  }
  
  /**
   * Calculate the overall progress percentage across all files
   */
  private calculateOverallProgress(): void {
    if (this.uploadProgress.length === 0) {
      this.overallProgress = 0;
      return;
    }
    
    const totalProgress = this.uploadProgress.reduce((sum, file) => sum + file.progress, 0);
    this.overallProgress = Math.round(totalProgress / this.uploadProgress.length);
  }
  
  /**
   * Get the severity class for the progress bar based on status
   */
  getSeverity(status: string): string {
    switch (status) {
      case 'completed':
        return 'success';
      case 'error':
        return 'danger';
      case 'uploading':
        return 'info';
      default:
        return 'secondary';
    }
  }
  
  /**
   * Check if all uploads are complete
   */
  get allComplete(): boolean {
    return this.uploadProgress.every(file => 
      file.status === 'completed' || file.status === 'error');
  }
  
  /**
   * Check if any upload has error
   */
  get hasErrors(): boolean {
    return this.uploadProgress.some(file => file.status === 'error');
  }
  
  /**
   * Get count of completed uploads
   */
  get completedCount(): number {
    return this.uploadProgress.filter(file => file.status === 'completed').length;
  }
  
  /**
   * Close the dialog and stop any timers
   */
  close(): void {
    this.stopCountdown();
    this.visible = false;
  }
} 