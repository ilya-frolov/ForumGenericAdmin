import { Component, ElementRef, TemplateRef, ViewChild } from '@angular/core';
import { BaseFieldComponent } from '../base-field.component';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ColorPickerModule } from 'primeng/colorpicker';
import { InputTextModule } from 'primeng/inputtext';
import { ColorMetadata } from 'src/app/interfaces/field-metadata';

@Component({
  selector: 'app-color-picker',
  templateUrl: './color-picker.component.html',
  standalone: true,
  imports: [FormsModule, CommonModule, ColorPickerModule, InputTextModule]
})
export class ColorPickerComponent extends BaseFieldComponent<string, ColorMetadata> {
  
  constructor() {
    super();
  }

  override get value(): string {
    if (!this._value) {
      return '#FFFFFF';
    }

    // Ensure the color starts with #
    if (!this._value.startsWith('#')) {
      return `#${this._value}`;
    }
    return this._value;
  }

  override set value(value: string) {
    this._value = value;
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
   * Gets a contrasting text color (black or white) based on the background color brightness
   */
  getContrastingTextColor(hexColor: string): string {
    // Default to black text if no color is provided
    if (!hexColor) {
      return '#000000';
    }

    // Remove # if present
    const hex = hexColor.replace('#', '');
    
    // Convert to RGB
    const r = parseInt(hex.substring(0, 2), 16) || 0;
    const g = parseInt(hex.substring(2, 4), 16) || 0;
    const b = parseInt(hex.substring(4, 6), 16) || 0;
    
    // Calculate brightness (formula from W3C accessibility guidelines)
    const brightness = (r * 299 + g * 587 + b * 114) / 1000;
    
    // Return white for dark colors, black for light colors
    return brightness > 128 ? '#000000' : '#FFFFFF';
  }
} 