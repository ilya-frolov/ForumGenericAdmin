import { Component, ElementRef, TemplateRef, ViewChild, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseFieldComponent } from '../base-field.component';
import { DatePickerModule } from 'primeng/datepicker';
import { DateTimeMetadata } from 'src/app/interfaces/field-metadata';

@Component({
  selector: 'app-datetime-field',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePickerModule],
  templateUrl: './datetime-field.component.html',
})
export class DateTimeFieldComponent extends BaseFieldComponent<Date | string, DateTimeMetadata> implements OnInit {

  override ngOnInit(): void {
    super.ngOnInit();
    
    // Ensure string dates are converted to Date objects
    if (typeof this._value === 'string' && this._value) {
      this._value = new Date(this._value);
    }
  }

  get displayValue(): string {
    if (!this.value) return '';
    
    // Ensure we're working with a Date object
    const dateValue = this.value instanceof Date ? this.value : new Date(this.value);
    
    if (this.metadata.timeOnly) {
      return dateValue.toLocaleTimeString();
    } else if (this.metadata.showTime) {
      return dateValue.toLocaleString();
    } else {
      return dateValue.toLocaleDateString();
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
   * Convert date value for server submission
   */
  override getValueForServer(value: Date | string): any {
    if (!value) {
      return null;
    }
    
    // If it's already a string in ISO format, return it
    if (typeof value === 'string') {
      return value;
    }
    
    // Otherwise convert Date to ISO string
    return value.toISOString();
  }

  /**
   * Parse date value from server
   */
  override parseValueFromServer(value: Date | string): Date | string {
    if (!value) {
      return null;
    }
    
    // Convert string to Date object
    if (typeof value === 'string') {
      return new Date(value);
    }
    
    return value;
  }

  /**
   * Override setter to ensure we convert strings to Dates
   */
  override set value(val: Date | string) {
    if (typeof val === 'string' && val) {
      this._value = new Date(val);
    } else {
      this._value = val;
    }
  }

  /**
   * Override getter to ensure we return a Date object
   */
  override get value(): Date | string {
    return this._value;
  }
} 