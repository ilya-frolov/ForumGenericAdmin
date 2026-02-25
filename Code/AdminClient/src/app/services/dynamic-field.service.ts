import { Injectable, Type } from '@angular/core';
import { FieldType } from '../interfaces/field-data';
import { TextFieldComponent } from '../components/field-types/text-field/text-field.component';
import { NumberFieldComponent } from '../components/field-types/number-field/number-field.component';
import { CheckboxFieldComponent } from '../components/field-types/checkbox-field/checkbox-field.component';
import { SelectFieldComponent } from '../components/field-types/select-field/select-field.component';
import { RichTextFieldComponent } from '../components/field-types/richtext-field/richtext-field.component';
import { DateTimeFieldComponent } from '../components/field-types/datetime-field/datetime-field.component';
import { MultiSelectFieldComponent } from '../components/field-types/multiselect-field/multiselect-field.component';
import { ColorPickerComponent } from '../components/field-types/color-picker/color-picker.component';
import { RadioButtonFieldComponent } from '../components/field-types/radio-button-field/radio-button-field.component';
import { SliderFieldComponent } from '../components/field-types/slider-field/slider-field.component';
import { RatingFieldComponent } from '../components/field-types/rating-field/rating-field.component';
import { SwitchFieldComponent } from '../components/field-types/switch-field/switch-field.component';
import { KnobFieldComponent } from '../components/field-types/knob-field/knob-field.component';
import { ExternalVideoFieldComponent } from '../components/field-types/external-video-field/external-video-field.component';
import { PictureFieldComponent } from '../components/field-types/picture-field/picture-field.component';
import { PasswordFieldComponent } from '../components/field-types/password-field/password-field.component';
import { TextAreaFieldComponent } from '../components/field-types/textarea-field/textarea-field.component';
import { FileFieldComponent } from '../components/field-types/file-field/file-field.component';
import { TimeOnlyFieldComponent } from '../components/field-types/timeonly-field/timeonly-field.component';
import { DateOnlyFieldComponent } from '../components/field-types/dateonly-field/dateonly-field.component';
import { UrlFieldComponent } from '../components/field-types/url-field/url-field.component';
import { LoggerService } from './logger.service';

@Injectable({
  providedIn: 'root'
})
export class DynamicFieldService {

  /// ************** for testing purposes, it should be dynamic from the backend *********8
  private fieldComponents: Map<FieldType, Type<any>> = new Map();

  constructor(private logger: LoggerService) {
    this.logger.info('DynamicFieldService initialized');
    
    // Pre-register default components for direct testing
    // These will be overridden by the APP_INITIALIZER if configured
    this.registerFieldComponent(FieldType.TEXT, TextFieldComponent);
    this.registerFieldComponent(FieldType.PASSWORD, PasswordFieldComponent);
    this.registerFieldComponent(FieldType.TEXTAREA, TextAreaFieldComponent);
    this.registerFieldComponent(FieldType.NUMBER, NumberFieldComponent);
    this.registerFieldComponent(FieldType.CHECKBOX, CheckboxFieldComponent);
    this.registerFieldComponent(FieldType.SELECT, SelectFieldComponent);
    this.registerFieldComponent(FieldType.RICHTEXT, RichTextFieldComponent);
    this.registerFieldComponent(FieldType.DATETIME, DateTimeFieldComponent);
    this.registerFieldComponent(FieldType.MULTISELECT, MultiSelectFieldComponent);
    this.registerFieldComponent(FieldType.COLOR, ColorPickerComponent);
    this.registerFieldComponent(FieldType.RADIO_BUTTON, RadioButtonFieldComponent);
    this.registerFieldComponent(FieldType.SLIDER, SliderFieldComponent);
    this.registerFieldComponent(FieldType.RATING, RatingFieldComponent);
    this.registerFieldComponent(FieldType.SWITCH, SwitchFieldComponent);
    this.registerFieldComponent(FieldType.KNOB, KnobFieldComponent);
    this.registerFieldComponent(FieldType.EXTERNAL_VIDEO, ExternalVideoFieldComponent);
    this.registerFieldComponent(FieldType.PICTURE, PictureFieldComponent);
    this.registerFieldComponent(FieldType.FILE, FileFieldComponent);
    this.registerFieldComponent(FieldType.TIMEONLY, TimeOnlyFieldComponent);
    this.registerFieldComponent(FieldType.DATEONLY, DateOnlyFieldComponent);
    this.registerFieldComponent(FieldType.URL, UrlFieldComponent);
  }

  /**
   * Register a field component by type
   * @param type The field type
   * @param component The component class
   */
  registerFieldComponent(type: FieldType, component: Type<any>): void {
    this.logger.debug(`Registering component for field type: ${type}`, component);
    this.fieldComponents.set(type, component);
  }

  /**
   * Get a field component by type
   * @param type The field type
   * @returns The component class or undefined if not found
   */
  getFieldComponent(type: FieldType): Type<any> | undefined {
    const component = this.fieldComponents.get(type);
    this.logger.debug(`Getting component for field type: ${type}`, component ? 'Found' : 'Not found');
    return component;
  }

  /**
   * Check if a field type is registered
   * @param type The field type
   * @returns True if the field type is registered
   */
  hasFieldType(type: FieldType): boolean {
    return this.fieldComponents.has(type);
  }

  /**
   * Get all registered field types
   * @returns Array of registered field types
   */
  getRegisteredFieldTypes(): FieldType[] {
    return Array.from(this.fieldComponents.keys());
  }
} 