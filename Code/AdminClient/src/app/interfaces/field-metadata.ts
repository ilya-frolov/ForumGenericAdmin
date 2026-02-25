/**
 * Metadata interface for field configuration and validation
 */
export interface FieldMetadata {
  /**
   * Whether the field is required
   */
  required?: boolean;

  /**
   * Placeholder text
   */
  placeholder?: string;

  /**
   * Whether the field is read-only
   */
  readOnly?: boolean;

  /**
   * Tooltip text
   */
  tooltip?: string;

  visible?: boolean;
  width?: number;

  /**
   * Any additional configuration options specific to the field type
   */
  [key: string]: any;
}

export interface TextMetadata extends FieldMetadata {
  /**
   * Maximum length for text inputs
   */
  maxLength?: number;

  /**
   * Minimum length for text inputs
   */
  minLength?: number;

  /**
   * Custom validation pattern
   */
  pattern?: string | RegExp;

  /**
   * Line number for text inputs
   */
  lineNumber?: number;

  /**
   * Number of rows for text inputs
   */
  rows?: number;
}

export interface NumberMetadata extends FieldMetadata {
  /**
   * Whether the field should accept decimal values
   */
  isDecimal?: boolean;

  /**
   * Whether to show the buttons
   */
  showButtons?: boolean;

  /**
   * Number of decimal places to show
   */
  decimalPlaces?: number;

  /**
   * Whether to show thousands comma separator
   */
  showThousandsComma?: boolean;

  /**
   * Step value for number inputs
   */
  step?: number;

  /**
   * Minimum value
   */
  min?: number;

  /**
   * Maximum value
   */
  max?: number;
}

export interface CheckboxMetadata extends FieldMetadata {
  /**
   * The label to display next to the checkbox
   */
  checkboxLabel?: string;
  
  /**
   * Whether this checkbox can be used to toggle a list of items
   */
  allowListToggle?: boolean;
  
  /**
   * True value representation (default is true)
   */
  trueValue?: any;
  
  /**
   * False value representation (default is false)
   */
  falseValue?: any;
}

export interface SelectMetadata extends FieldMetadata {
  /**
   * Whether multiple options can be selected
   */
  multiSelect?: boolean;
  
  /**
   * Whether the select should have search functionality
   */
  searchEnabled?: boolean;
  
  /**
   * View type (e.g., dropdown, radio, checkbox)
   */
  viewType?: number;
  
  /**
   * The options for the select field
   */
  options?: Array<{display: string, value: any}>;
  
  /**
   * Max items that can be selected (for multi-select)
   */
  maxSelectedItems?: number;
  
  /**
   * Placeholder text for the search input
   */
  searchPlaceholder?: string;
}

export interface MultiSelectFieldMetadata extends SelectMetadata {
}

export interface ColorMetadata extends FieldMetadata {
  /**
   * Default color value
   */
  defaultColor?: string;

  /**
   * Custom validation pattern
   */
  pattern?: string | RegExp;
}

export interface RadioButtonMetadata extends FieldMetadata {
  /**
   * Name of the radio button group
   */
  name?: string;

  /**
   * The options for the radio button field
   */
  options?: Array<{display: string, value: any}>;
}

export interface SliderMetadata extends FieldMetadata {
  /**
   * Minimum value
   */
  min?: number;

  /**
   * Maximum value
   */
  max?: number;

  /**
   * Step value for slider
   */
  step?: number;

  /**
   * Range mode - allows selecting a range with two handles
   */
  range?: boolean;

  /**
   * Whether to show value label
   */
  showValue?: boolean;
}

export interface RatingMetadata extends FieldMetadata {
  /**
   * Number of stars
   */
  stars?: number;
}

export interface SwitchMetadata extends FieldMetadata {
}

export interface KnobMetadata extends FieldMetadata {
  /**
   * Minimum value
   */
  min?: number;

  /**
   * Maximum value
   */
  max?: number;

  /**
   * Step value
   */
  step?: number;

  /**
   * Value template (e.g., "{value}%")
   */
  valueTemplate?: string;

  /**
   * Size of the knob
   */
  size?: number;
}

export interface DateTimeMetadata extends FieldMetadata {
  type?: number; // 0: Date, 1: Time, 2: DateTime
  isRangeSelection?: boolean;
  minDate?: Date;
  maxDate?: Date;
  showTime?: boolean;
  timeOnly?: boolean;
  dateFormat?: string;
}

export interface RichTextMetadata extends TextMetadata {
  editorType?: number;
}

// export interface RepeaterMetadata extends FieldMetadata {
//   minItems?: number;
//   maxItems?: number;
//   allowReordering?: boolean;
//   disableRepeaterAddItemButton?: boolean;
//   disableRepeaterRemoveItemButton?: boolean;
//   repeaterRemoveConfirmation?: boolean;
//   containerBehavior?: number;
//   defaultCollapsed?: boolean;
//   showTitle?: boolean;
// }

export interface ExternalVideoMetadata extends FieldMetadata {
  sourceType?: number; // 0: YouTube
}

export interface PictureMetadata extends FieldMetadata {
  uploadUrl?: string;
  allowedTypes?: string[];
  allowedExtensions?: string[];
  maxSize?: number;
  dragDrop?: boolean;
  multiple?: boolean;
  platforms?: number;
  forceFormat?: number;
  forceCrop?: boolean;
}

export interface FileMetadata extends FieldMetadata {
  uploadUrl?: string;
  allowedExtensions?: string[];
  maxSize?: number;
  dragDrop?: boolean;
  multiple?: boolean;
  platforms?: number;
}

export interface UrlMetadata extends TextMetadata {
  baseUrl?: string;
} 