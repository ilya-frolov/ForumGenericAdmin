import { FieldMetadata } from "./field-metadata";

/**
 * Field data interface to represent the structure of a form field
 */
export interface FieldData<InPutType, MetaType extends FieldMetadata> {
    id: string;
    type: FieldType;
    label: string;
    value: InPutType;
    metadata: MetaType;
    path: string;
  } 
  
  export interface FieldTypeOption {
    label: string;
    value: FieldType;
  }

/**
 * Field types enum for reference
 */
export enum FieldType {
    TEXT = 'Text',
    PASSWORD = 'Password',
    TEXTAREA = 'TextArea',
    NUMBER = 'Number',
    DATETIME = 'DateTime',
    SELECT = 'Select',
    MULTISELECT = 'MultiSelect',
    RICHTEXT = 'RichText',
    CHECKBOX = 'Checkbox',
    REPEATER = 'Repeater',
    EXTERNAL_VIDEO = 'ExternalVideo',
    COLOR = 'Color',
    RADIO_BUTTON = 'RadioButton',
    SLIDER = 'Slider',
    RATING = 'Rating',
    SWITCH = 'Switch',
    KNOB = 'Knob',
    PICTURE = 'Picture',
    FILE = 'File',
    TIMEONLY = 'TimeOnly',
    DATEONLY = 'DateOnly',
    URL = 'URL'
  }
  