import { Component, ElementRef, OnInit, TemplateRef, ViewChild, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseFieldComponent } from '../base-field.component';
import { RatingMetadata } from 'src/app/interfaces/field-metadata';
import { RatingModule } from 'primeng/rating';

@Component({
  selector: 'app-rating-field',
  standalone: true,
  imports: [CommonModule, FormsModule, RatingModule],
  templateUrl: './rating-field.component.html',
  schemas: [CUSTOM_ELEMENTS_SCHEMA]
})
export class RatingFieldComponent extends BaseFieldComponent<number, RatingMetadata> implements OnInit {

  constructor() {
    super();
  }

  override ngOnInit(): void {
    super.ngOnInit();
    
    // Initialize value if not set
    if (this.value === undefined || this.value === null) {
      this.value = 0;
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