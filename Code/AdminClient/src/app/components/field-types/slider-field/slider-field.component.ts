import { Component, ElementRef, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseFieldComponent } from '../base-field.component';
import { SliderMetadata } from 'src/app/interfaces/field-metadata';
import { SliderModule } from 'primeng/slider';

@Component({
  selector: 'app-slider-field',
  standalone: true,
  imports: [CommonModule, FormsModule, SliderModule],
  templateUrl: './slider-field.component.html',
})
export class SliderFieldComponent extends BaseFieldComponent<number, SliderMetadata> implements OnInit {

  constructor() {
    super();
  }

  override ngOnInit(): void {
    super.ngOnInit();
    
    // Initialize default values if not provided
    if (this.metadata) {
      this.metadata.min = this.metadata.min ?? 0;
      this.metadata.max = this.metadata.max ?? 100;
      this.metadata.step = this.metadata.step ?? 1;
    }
    
    // Initialize value if not set
    if (this.value === undefined || this.value === null) {
      this.value = this.metadata?.min || 0;
    }
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

  /**
   * Convert the display value to the model value
   */
  override getValueForServer(value: number): number {
    return value || 0;
  }

  /**
   * Convert the model value to the display value
   */
  override parseValueFromServer(value: number): number {
    return value || 0;
  }
} 