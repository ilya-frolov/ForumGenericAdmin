import { ElementRef, TemplateRef, EventEmitter } from '@angular/core';
import { FieldMetadata } from './field-metadata';

/**
 * Base interface for dynamic form field components
 */
export interface FieldTypeComponent<InPutType, MetaType extends FieldMetadata> {
  /**
   * Unique identifier for the form field
   */
  id: string;

  /**
   * The current value of the form field
   */
  value: InPutType;

  /**
   * Field label to display
   */
  label: string;

  /**
   * Metadata for validation and configuration
   */
  metadata: MetaType;

  /**
   * Event emitter for value changes
   */
  valueChange: EventEmitter<InPutType>;

  /**
   * Returns the template for edit mode
   */
  getEditTemplate(): TemplateRef<ElementRef> | string;

  /**
   * Returns the template for list/display mode
   */
  getListTemplate(): TemplateRef<ElementRef> | string;


  /**
   * Returns the template for view mode
   */
  getViewTemplate(): TemplateRef<ElementRef> | string;

  /**
   * Converts user input to the appropriate data format
   * @param value The value to convert
   * @returns The converted value
   */
  getValueForServer(value: InPutType): InPutType;

  /**
   * Converts the data format to a display format
   * @param value The value to convert
   * @returns The display value
   */
  parseValueFromServer(value: InPutType): InPutType;
}



