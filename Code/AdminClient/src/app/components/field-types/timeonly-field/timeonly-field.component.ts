import { Component, ElementRef, TemplateRef, ViewChild, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseFieldComponent } from '../base-field.component';
import { DatePickerModule } from 'primeng/datepicker';
import { DateTimeMetadata } from 'src/app/interfaces/field-metadata';

@Component({
  selector: 'app-timeonly-field',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePickerModule],
  templateUrl: './timeonly-field.component.html',
})
export class TimeOnlyFieldComponent extends BaseFieldComponent<Date | string, DateTimeMetadata> implements OnInit {

  override ngOnInit(): void {
    super.ngOnInit();
    // Ensure metadata defaults for time-only behavior
    if (!this.metadata) {
      this.metadata = {} as DateTimeMetadata;
    }
    this.metadata.timeOnly = true;
    this.metadata.showTime = true; // timeOnly usually implies showTime, but explicit is good

    // Ensure string dates are converted to Date objects
    if (typeof this._value === 'string' && this._value) {
      this._value = new Date(this._value);
    }
  }

  get displayValue(): string {
    if (!this.value) return '-';
    
    const dateValue = this.value instanceof Date ? this.value : new Date(this.value);
    
    // Should always be true due to ngOnInit and metadata settings
    if (this.metadata.timeOnly) { 
      return dateValue.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
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

  override getValueForServer(value: Date | string | null): string | null {
    if (!value) {
      return null;
    }

    let date: Date;
    if (typeof value === 'string') {
      // Attempt to parse if it's a string that might represent a date/time
      date = new Date(value);
      if (isNaN(date.getTime())) {
        // If parsing fails, return null or handle as an error case
        console.error('Invalid date string received in getValueForServer:', value);
        return null;
      }
    } else {
      date = value; // It's already a Date object
    }

    // Format the time part as HH:mm:ss.fff
    const hours = date.getHours().toString().padStart(2, '0');
    const minutes = date.getMinutes().toString().padStart(2, '0');
    const seconds = date.getSeconds().toString().padStart(2, '0');
    const milliseconds = date.getMilliseconds().toString().padStart(3, '0');

    return `${hours}:${minutes}:${seconds}.${milliseconds}`;
  }

  override parseValueFromServer(value: string | null): Date | null {
    if (!value || typeof value !== 'string') {
      // Expecting a string like "HH:mm:ss" or "HH:mm:ss.fff" from server
      return null;
    }

    let time = null;
    const splittedDateAndTime = value.split('T');
    if (splittedDateAndTime.length === 2) {
      time = splittedDateAndTime[1];
    }
    else if (splittedDateAndTime.length === 1) {
      time = splittedDateAndTime[0];
    }
    else {
      console.error('Invalid date and time format received from server:', value);
      return null; // Invalid format
    }


    const timeParts = time.split(/[:.]/);
    if (timeParts.length < 3 || timeParts.length > 4) {
       console.error('Invalid time format received from server:', value);
       return null; // Invalid format
    }

    const hours = parseInt(timeParts[0], 10);
    const minutes = parseInt(timeParts[1], 10);

    if (isNaN(hours) || isNaN(minutes)) {
        console.error('Failed to parse time components from server:', value);
        return null; // Parsing failed
    }

    // Create a new Date object with today's date but the server's time
    const date = new Date();
    date.setHours(hours);
    date.setMinutes(minutes);
    return date;
  }

  override set value(val: Date | string) {
    if (typeof val === 'string' && val) {
      // Attempt to create a date. If it's just a time string like "10:00 AM",
      // new Date() will create a date with today's date and that time.
      this._value = new Date(val);
    } else {
      this._value = val;
    }
  }

  override get value(): Date | string {
    return this._value;
  }
} 