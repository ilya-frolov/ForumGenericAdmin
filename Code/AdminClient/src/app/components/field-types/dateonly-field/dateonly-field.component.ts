import { Component, ElementRef, TemplateRef, ViewChild, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseFieldComponent } from '../base-field.component';
import { DatePickerModule } from 'primeng/datepicker';
import { DateTimeMetadata } from 'src/app/interfaces/field-metadata';

@Component({
  selector: 'app-dateonly-field',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePickerModule],
  templateUrl: './dateonly-field.component.html',
})
export class DateOnlyFieldComponent extends BaseFieldComponent<Date | string, DateTimeMetadata> implements OnInit {

  override ngOnInit(): void {
    super.ngOnInit();
    // Ensure metadata defaults for date-only behavior
    if (!this.metadata) {
      this.metadata = {} as DateTimeMetadata;
    }
    this.metadata.timeOnly = false;
    this.metadata.showTime = false;

    // Ensure string dates are converted to Date objects
    if (typeof this._value === 'string' && this._value) {
      this._value = new Date(this._value);
    }
  }

  get displayValue(): string {
    if (!this.value) return '-';
    
    const dateValue = this.value instanceof Date ? this.value : new Date(this.value);
    
    // Should always be false due to ngOnInit and metadata settings
    if (!this.metadata.showTime && !this.metadata.timeOnly) { 
      return dateValue.toLocaleDateString();
    }
    // Fallback, though ideally unreachable if metadata is set correctly
    return dateValue.toLocaleString();
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

  override getValueForServer(value: Date | string): any {
    if (!value) {
      return null;
    }
    if (typeof value === 'string') {
      const date = new Date(value);
      return isNaN(date.getTime()) ? value : date.toISOString();
    }
    // Ensure time part is zeroed out if needed? For now, keep full ISO string.
    return value.toISOString(); 
  }

  override parseValueFromServer(value: Date | string): Date | string {
    if (!value) {
      return null;
    }
    if (typeof value === 'string') {
      const date = new Date(value);
      return isNaN(date.getTime()) ? value : date;
    }
    return value;
  }

  override set value(val: Date | string) {
    if (typeof val === 'string' && val) {
      this._value = new Date(val);
    } else {
      this._value = val;
    }
  }

  override get value(): Date | string {
    return this._value;
  }
} 