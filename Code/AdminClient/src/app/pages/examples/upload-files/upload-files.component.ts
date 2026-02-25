import { Component } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { FileUploadEvent } from 'primeng/fileupload';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';

// Interface for uploaded files
interface UploadedFile extends File {
  objectURL: string;
  safeUrl?: SafeUrl;
}

@Component({
  selector: 'app-upload-files',
  standalone: false,
  templateUrl: './upload-files.component.html',
  providers: [MessageService]
})
export class UploadFilesComponent {
  formGroup: FormGroup;
  uploadedFiles: UploadedFile[] = [];
  audioFiles: UploadedFile[] = [];
  videoFiles: UploadedFile[] = [];

  constructor(
    private fb: FormBuilder, 
    private messageService: MessageService,
    private sanitizer: DomSanitizer
  ) {
    this.formGroup = this.fb.group({});
  }

  onUpload(event: FileUploadEvent) {
      for(let file of event.files as UploadedFile[]) {
          this.uploadedFiles.push(file);
      }

      this.messageService.add({severity: 'info', summary: 'File Uploaded', detail: ''});
  }

  onAudioUpload(event: FileUploadEvent) {
      for(let file of event.files as UploadedFile[]) {
          // Create a safe URL for the audio file
          file.safeUrl = this.getSafeUrl(file.objectURL);
          this.audioFiles.push(file);
      }

      this.messageService.add({severity: 'info', summary: 'Audio File Uploaded', detail: ''});
  }

  onVideoUpload(event: FileUploadEvent) {
      for(let file of event.files as UploadedFile[]) {
          // Create a safe URL for the video file
          file.safeUrl = this.getSafeUrl(file.objectURL);
          this.videoFiles.push(file);
      }

      this.messageService.add({severity: 'info', summary: 'Video File Uploaded', detail: ''});
  }

  getSafeUrl(url: string): SafeUrl {
    return this.sanitizer.bypassSecurityTrustUrl(url);
  }

  formatSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  removeUploadedFileCallback(index: number): void {
    this.uploadedFiles.splice(index, 1);
    this.messageService.add({severity: 'success', summary: 'File Removed', detail: ''});
  }

  removeAudioFile(index: number): void {
    this.audioFiles.splice(index, 1);
    this.messageService.add({severity: 'success', summary: 'Audio File Removed', detail: ''});
  }

  removeVideoFile(index: number): void {
    this.videoFiles.splice(index, 1);
    this.messageService.add({severity: 'success', summary: 'Video File Removed', detail: ''});
  }

  playAudio(audioElement: HTMLAudioElement): void {
    // Reset all other audio players
    document.querySelectorAll('audio').forEach(audio => {
      if (audio !== audioElement && !audio.paused) {
        audio.pause();
      }
    });
    
    // Play the selected audio
    if (audioElement.paused) {
      audioElement.play().catch(error => {
        console.error('Error playing audio:', error);
        this.messageService.add({
          severity: 'error', 
          summary: 'Audio Playback Error', 
          detail: 'Could not play the audio file. Please try again.'
        });
      });
    } else {
      audioElement.pause();
    }
  }

  playVideo(videoElement: HTMLVideoElement): void {
    // Reset all other video players
    document.querySelectorAll('video').forEach(video => {
      if (video !== videoElement && !video.paused) {
        video.pause();
      }
    });
    
    // Play the selected video
    if (videoElement.paused) {
      videoElement.play().catch(error => {
        console.error('Error playing video:', error);
        this.messageService.add({
          severity: 'error', 
          summary: 'Video Playback Error', 
          detail: 'Could not play the video file. Please try again.'
        });
      });
    } else {
      videoElement.pause();
    }
  }

  onSubmit(): void {
    if (this.formGroup.valid) {
      console.log(this.formGroup.value);
    }
  }
}
