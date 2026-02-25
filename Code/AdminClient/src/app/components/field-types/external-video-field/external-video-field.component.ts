import { Component, ElementRef, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseFieldComponent } from '../base-field.component';
import { ExternalVideoMetadata } from 'src/app/interfaces/field-metadata';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-external-video-field',
  standalone: true,
  imports: [CommonModule, FormsModule, InputTextModule, ButtonModule],
  templateUrl: './external-video-field.component.html',
})
export class ExternalVideoFieldComponent extends BaseFieldComponent<string, ExternalVideoMetadata> implements OnInit {

  videoUrl: string = '';
  videoId: string = '';

  constructor() {
    super();
  }

  override ngOnInit(): void {
    super.ngOnInit();
    this.videoUrl = this.value || '';
    this.extractVideoId();
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

  override getValueForServer(value: string): string {
    return value || '';
  }

  override parseValueFromServer(value: string): string {
    return value || '';
  }

  onUrlChange(): void {
    this.extractVideoId();
    this.onValueChange(this.videoUrl);
  }

  openVideo(): void {
    if (this.videoId) {
      window.open(`https://www.youtube.com/watch?v=${this.videoId}`, '_blank');
    }
  }

  private extractVideoId(): void {
    if (!this.videoUrl) {
      this.videoId = '';
      return;
    }

    // Extract YouTube video ID
    const youtubeRegex = /(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^"&?\/\s]{11})/;
    const match = this.videoUrl.match(youtubeRegex);
    
    if (match && match[1]) {
      this.videoId = match[1];
    } else {
      this.videoId = '';
    }
  }
} 