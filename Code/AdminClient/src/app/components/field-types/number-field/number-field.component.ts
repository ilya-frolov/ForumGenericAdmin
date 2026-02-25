import { Component, ElementRef, Input, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseFieldComponent } from '../base-field.component';
import { NumberMetadata } from 'src/app/interfaces/field-metadata';
import { InputNumberModule } from 'primeng/inputnumber';

@Component({
  selector: 'app-number-field',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    InputNumberModule
  ],
  templateUrl: './number-field.component.html',
})
export class NumberFieldComponent extends BaseFieldComponent<number, NumberMetadata> implements OnInit {

  constructor() {
    super();
  }

  override ngOnInit(): void {
    super.ngOnInit();
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
   * Handle value changes from the input
   */
  override onValueChange(value: number): void {
    if (value === null || value === undefined || isNaN(value)) {
      this.value = null;
    } else {
      // Apply min/max constraints if specified
      if (this.metadata?.min !== undefined && value < this.metadata.min) {
        value = this.metadata.min;
      }
      if (this.metadata?.max !== undefined && value > this.metadata.max) {
        value = this.metadata.max;
      }
      
      // If it's a decimal field, preserve decimal places
      if (this.metadata?.isDecimal) {
        const decimalPlaces = this.getDecimalPlaces();
        this.value = parseFloat(value.toFixed(decimalPlaces));
      } else {
        // For integer fields, round to closest integer
        this.value = Math.round(value);
      }
    }
    this.valueChange.emit(this.value);
  }

  /**
   * Convert the display value to the model value
   * For number fields, ensure it's a proper number
   */
  override getValueForServer(value: number): number {
    if (value === null || value === undefined || isNaN(value)) {
      return 0;
    }
    
    // If it's a decimal field, preserve decimal places
    if (this.metadata?.isDecimal) {
      const decimalPlaces = this.getDecimalPlaces();
      return parseFloat(value.toFixed(decimalPlaces));
    }
    
    // For integer fields, round to closest integer
    return Math.round(value);
  }

  /**
   * Convert the model value to the display value
   * For number fields, this is a direct passthrough
   */
  override parseValueFromServer(value: number): number {
    return value;
  }

  getDecimalPlaces(): number {
    if (!this.metadata?.isDecimal) {
      return 0;
    }
    const decimalPlaces = this.metadata.decimalPlaces;
    return decimalPlaces >= 0 ? Math.min(decimalPlaces, 20) : 2;
  }
} 