import { Injectable } from '@angular/core';
import {
  FormArray,
  FormBuilder,
  FormControl,
  FormGroup,
  Validators,
  AbstractControl,
  ValidatorFn,
} from '@angular/forms';
import {
  FormStructure,
  NodeStructure,
  NodeType,
  FieldNode,
} from '../interfaces/form-structure';
import { LoggerService } from './logger.service';
import { Observable, map } from 'rxjs';
import { HttpParams } from '@angular/common/http';
import { ServerResponse } from '../interfaces/server-response';
import { environment } from 'src/environments/environment';
import { BaseService } from './base-service';
import { FieldData, FieldType } from '../interfaces/field-data';
import { FieldMetadata } from '../interfaces/field-metadata';

// Debug flag to use mock data instead of real API
export const USE_MOCK_FORM_DATA = environment?.useMockFormData ?? false;

export type FormMode = 'edit' | 'view' | 'list';

@Injectable({
  providedIn: 'root',
})
export class FormBuilderService extends BaseService {
  // Store foreign types for complex type fields
  private foreignTypes: Record<string, NodeStructure> = {};

  // Store the original model for reference
  private originalModel: any = null;

  // Store the built model that reflects form control values
  private modelData: any = null;

  private formGroup: FormGroup = null;

  constructor(private fb: FormBuilder, private logger: LoggerService) {
    super();
  }

  /**
   * Build a form and structure from server data
   */
  buildFormAndStructure(
    controllerName: string,
    refId: string | null,
    formId: string
  ): Observable<FormStructure> {
    try {
      // Get the form structure data
      return this.fetchFormStructure(controllerName, refId, formId).pipe(
        map((response) => {
          const formData: FormStructure = response.data;
          if (!formData) {
            this.logger.error('No form data found');
            return null;
          }

          // Store foreign types for reference with normalized keys
          this.foreignTypes = {};
          if (formData.foreignTypes) {
            // Normalize all keys to lowercase for case-insensitive lookup
            Object.entries(formData.foreignTypes).forEach(([key, value]) => {
              this.foreignTypes[key.toLowerCase()] = value;
            });
          }

          // Store the original model
          this.originalModel = formData.model || {};

          // Create a deep copy for the model data we'll build
          this.modelData = JSON.parse(JSON.stringify(this.originalModel || {}));

          const formGroup = this.fb.group({});

          this.formGroup = formGroup;

          return formData;
        })
      );
    } catch (error) {
      this.logger.error('Error building form and structure:', error);
      throw error;
    }
  }

  public getFormGroup(): FormGroup {
    return this.formGroup;
  }

  public fetchFormStructure(
    controllerName: string,
    refId: string = null,
    formId: string = null
  ): Observable<ServerResponse<FormStructure>> {
    // If mock data flag is enabled, return the example form structure
    if (USE_MOCK_FORM_DATA) {
      console.info('[FormBuilderService] Using mock form data for debugging');

      // Import the example form structure from assets
      return this.http
        .get<FormStructure>('assets/examples/form-example.json')
        .pipe(
          map((data) => {
            // Wrap the data in the expected ServerResponse format
            return {
              result: true,
              data: data,
              error: null,
            } as ServerResponse<FormStructure>;
          })
        );
    }

    // Otherwise, make the real API call
    let params = new HttpParams();

    // Add the specific params needed for this request
    if (refId != null && refId != undefined) {
      params = params.set('refId', refId);
    }
    if (formId != null && formId != undefined) {
      params = params.set('id', formId);
    }

    return this.get<FormStructure>(
      `${controllerName}/GetFormStructure`,
      params
    );
  }

  /**
   * Create flat controls for array items using paths
   */
  private createArrayItemControls(
    path: string,
    index: number,
    itemValue: any,
    structure: NodeStructure,
    formMode: FormMode
  ): void {
    if (!structure?.children) {
      return;
    }

    const processNodes = (nodes: NodeStructure[], value: any): void => {
      for (const child of nodes) {
        if (child.nodeType === NodeType.FIELD) {
          const childField = child as FieldNode;

          // Create control path using array notation: arrayName[index].fieldName
          const controlPath = `${path}.${index ?? ''}.${childField.name}`;

          // Get field value from the item
          const fieldValue = value ? value[childField.name] : null;

          // Create validators
          const validators = this.createValidators(childField);

          // Create and add control to root form group
          this.getOrCreateControlByPath(
            controlPath,
            fieldValue,
            formMode === 'list',
            validators
          );
        } else if (child.children && child.children.length > 0) {
          processNodes(child.children, value);
        }
      }
    };

    // Process children of the structure (including Tabs/Containers) and create flat controls
    processNodes(structure.children, itemValue);
  }

  /**
   * Create form validators based on field attributes
   */
  private createValidators(fieldNode: FieldNode): ValidatorFn[] {
    const validators: ValidatorFn[] = [];

    // Check if required
    if (fieldNode.attributes['required']) {
      validators.push(Validators.required);
    }

    // Add other validators based on field type and attributes
    if (fieldNode.fieldType === 'Text' || fieldNode.fieldType === 'TextArea') {
      // Max length
      if (fieldNode.attributes['maxLength']) {
        validators.push(
          Validators.maxLength(fieldNode.attributes['maxLength'])
        );
      }

      // Min length
      if (fieldNode.attributes['minLength']) {
        validators.push(
          Validators.minLength(fieldNode.attributes['minLength'])
        );
      }

      // Pattern
      if (fieldNode.attributes['pattern']) {
        validators.push(Validators.pattern(fieldNode.attributes['pattern']));
      }
    } else if (fieldNode.fieldType === 'Number') {
      // Min value
      if (
        fieldNode.attributes['min'] !== undefined &&
        fieldNode.attributes['min'] !== null
      ) {
        validators.push(Validators.min(fieldNode.attributes['min']));
      }

      // Max value
      if (
        fieldNode.attributes['max'] !== undefined &&
        fieldNode.attributes['max'] !== null
      ) {
        validators.push(Validators.max(fieldNode.attributes['max']));
      }
    }

    return validators;
  }

  /**
   * Get the model data
   */
  public getModelData(
    lowerCase: boolean = false
  ): any {
    if (lowerCase) {
      return this.toLowerCase(this.modelData);
    }
    return this.modelData;
  }

  getModelFieldValueFromPath(path: string): any {
    const value = path.split('.').reduce((acc, part) => {
      if (!acc) return null;
      // Case-insensitive property lookup
      const keys = Object.keys(acc);
      const matchingKey = keys.find(key => key.toLowerCase() === part.toLowerCase());
      return matchingKey ? acc[matchingKey] : null;
    }, this.modelData)

    return value;
  }


  /**
   * Get a foreign type structure
   */
  public getForeignTypeStructure(typeName: string): NodeStructure | null {
    if (!typeName) {
      return null;
    }

    const lowercaseTypeName = typeName.toLowerCase();
    return this.foreignTypes[lowercaseTypeName] || null;
  }

  public getNumOfControls(path: string): number | null {
    const controls = this.formGroup.get(path);
    if (controls instanceof FormArray) {
      return controls.length;
    }
    if (controls instanceof FormGroup) {
      return Object.keys(controls.controls).length;
    }
    return null;
  }

  /**
   * Add a new item to a repeater FormArray
   * @param path The path to the repeater array
   * @param structure Optional structure for complex type items or default value for simple types
   * @param formMode The current form mode
   * @param typeNameOrFieldType Optional type name for complex type repeaters or field type for simple repeaters
   */
  public addRepeaterItem(
    path: string,
    structure: NodeStructure | any,
    formMode: FormMode,
    typeNameOrFieldType?: string,
  ): void {
    if (!path) {
      this.logger.error('Could not determine array path for repeater');
      return;
    }

    // Get or create the form array
    let formArray = this.formGroup.get(path);
    if (!formArray) {
      this.getOrCreateControlByPath(path, []);
      formArray = this.formGroup.get(path);
    }

    // Get the next index
    const index = this.getNumOfControls(path);

    // Check if this is a simple type repeater (with a field type) or a complex type repeater
    const isSimpleType = this.isSimpleFieldType(typeNameOrFieldType);

    if (isSimpleType) {
      // For simple type repeaters (array of primitives)
      const itemPath = `${path}.${index ?? ''}`;
      const defaultValue = this.getDefaultValueForFieldType(typeNameOrFieldType);
      this.getOrCreateControlByPath(
        itemPath,
        defaultValue,
        formMode === 'list'
      );
      this.logger.debug(
        `Added simple repeater item of type ${typeNameOrFieldType} at ${itemPath}`
      );
    } else if (typeNameOrFieldType) {
      // For complex type repeaters with a type name
      const foreignType = this.getForeignTypeStructure(typeNameOrFieldType);
      if (foreignType) {
        this.createArrayItemControls(path, index, null, foreignType, formMode);
        this.logger.debug(
          `Added complex repeater item of type ${typeNameOrFieldType} at ${path}.${
            index ?? ''
          }`
        );
      } else {
        this.logger.error(
          `Foreign type ${typeNameOrFieldType} not found for complex repeater`
        );
      }
    } else if (structure) {
      // For repeaters with a structure
      this.createArrayItemControls(path, index, null, structure, formMode);
      this.logger.debug(
        `Added structured repeater item at ${path}.${index ?? ''}`
      );
    } else {
      // Default case - just add an empty control
      const itemPath = `${path}.${index ?? ''}`;
      this.getOrCreateControlByPath(itemPath, null, formMode === 'list');
      this.logger.debug(`Added default repeater item at ${itemPath}`);
    }

    // Update the model with the new item
    this.updateModelWithRepeaterItem(path, index);
  }

  /**
   * Check if a type name is a simple field type
   */
  private isSimpleFieldType(type?: string): boolean {
    if (!type) return false;

    const simpleTypes = [
      'Text',
      'TextArea',
      'Number',
      'Date',
      'Checkbox',
      'Radio',
      'Select',
      'Email',
      'Password',
      'Color',
      'Tel',
      'Url',
      'Range',
    ];

    return simpleTypes.includes(type);
  }

  /**
   * Update the model data with a new repeater item
   */
  private updateModelWithRepeaterItem(path: string, index: number): void {
    if (!this.modelData) return;

    // Use the path to navigate to the right place in the model
    const pathParts = path.split('.');
    let current = this.modelData;

    // Navigate to the array in the model, creating objects along the way if needed
    for (const part of pathParts) {
      if (!current[part]) {
        // If we're at the last part, create an array, otherwise an object
        current[part] = [];
      }
      current = current[part];
    }

    // Ensure it's an array and add the empty item
    if (Array.isArray(current)) {
      if (current.length <= index) {
        current.push(null);
      } else {
        current[index] = null;
      }
    }
  }

  /**
   * Remove an item from a repeater FormArray
   * @param formArray The FormArray to remove from
   * @param index The index to remove
   */
  public removeRepeaterItem(path: string, index: number): void {
    if (index < 0) {
      return;
    }

    if (!path) {
      this.logger.error('Could not determine array path for repeater');
      return;
    }
    const controls = this.formGroup.get(path);
    if (controls instanceof FormArray) {
      controls.removeAt(index);
    }
  }

  /**
   * Move an item within a repeater FormArray
   * @param formArray The FormArray to modify
   * @param fromIndex The source index
   * @param toIndex The target index
   */
  public moveRepeaterItem(
    path: string,
    fromIndex: number,
    toIndex: number
  ): void {
    if (fromIndex === toIndex) {
      return;
    }
    if (fromIndex < 0 || toIndex < 0) {
      return;
    }
    if (
      fromIndex >= this.getNumOfControls(path) ||
      toIndex >= this.getNumOfControls(path)
    ) {
      return;
    }
    const controls = this.formGroup.get(path);
    if (controls instanceof FormArray) {
      const fromIndexItem = controls.at(fromIndex).value;
      const toIndexItem = controls.at(toIndex).value;
      controls.at(fromIndex).setValue(toIndexItem);
      controls.at(toIndex).setValue(fromIndexItem);
    }
  }

  /**
   * Creates or retrieves a FormControl at the specified path.
   * Creates a proper nested hierarchy of FormGroups and FormArrays along the path.
   * @param path Dot-separated path (e.g., "parent.child.grandchild")
   * @param defaultValue Optional default value for the control if created
   * @returns The FormControl at the specified path
   */
  public getOrCreateControlByPath(
    path: string,
    defaultValue: any = null,
    isDisabled: boolean = false,
    validators: ValidatorFn[] = []
  ): FormControl {
    if (!path || !this.formGroup) {
      return null;
    }

    // Parse the path into segments
    const segments = path.split('.');

    // Start at the root form group
    let current: AbstractControl = this.formGroup;

    // Track the path built so far for logging
    let builtPath = '';

    // Build the path segment by segment
    for (let i = 0; i < segments.length; i++) {
      const segment = segments[i];
      builtPath = builtPath ? `${builtPath}.${segment}` : segment;

      // Check if we're at the final segment
      const isFinalSegment = i === segments.length - 1;

      if (isFinalSegment) {
        // For the final segment, create or return a FormControl
        if (current instanceof FormGroup) {
          let control = current.get(segment);

          if (!control) {
            // Create a new control if it doesn't exist
            control = this.fb.control(
              {
                value: defaultValue === undefined ? null : defaultValue,
                disabled: isDisabled,
              },
              { validators: validators }
            );
            current.addControl(segment, control);
            this.logger.debug(`Created control at ${builtPath}`);
          } else if (!(control instanceof FormControl)) {
            // Replace with a FormControl if it exists but is not a FormControl
            current.removeControl(segment);
            control = this.fb.control(
              {
                value: defaultValue === undefined ? null : defaultValue,
                disabled: isDisabled,
              },
              { validators: validators }
            );
            current.addControl(segment, control);
            this.logger.debug(
              `Replaced container with control at ${builtPath}`
            );
          }

          return control as FormControl;
        } else if (current instanceof FormArray) {
          // For arrays, add the control at the specified index
          const index = Number(segment);

          // Ensure the array has enough elements
          while (current.length <= index) {
            const control = this.fb.control(
              {
                value: defaultValue === undefined ? null : defaultValue,
                disabled: isDisabled,
              },
              { validators: validators }
            );
            current.push(control);
            this.logger.debug(
              `Created control at index ${index} in FormArray at ${builtPath}`
            );
          }

          const control = current.at(index);
          if (!(control instanceof FormControl)) {
            // Replace with a FormControl if needed
            current.setControl(
              index,
              this.fb.control(
                {
                  value: defaultValue === undefined ? null : defaultValue,
                  disabled: isDisabled,
                },
                { validators: validators }
              )
            );
            this.logger.debug(
              `Replaced container with control at ${builtPath}[${index}]`
            );
          }

          return current.at(index) as FormControl;
        } else {
          this.logger.error(
            `Cannot add control to non-container at ${builtPath}`
          );
          return null;
        }
      } else {
        // For intermediate segments, ensure appropriate container exists
        if (
          !(current instanceof FormGroup) &&
          !(current instanceof FormArray)
        ) {
          this.logger.error(
            `Cannot navigate to ${segment} in non-container at ${builtPath}`
          );
          return null;
        }

        let container: AbstractControl;
        const isNextSegmentNumeric = !isNaN(Number(segments[i + 1]));

        if (current instanceof FormGroup) {
          container = current.get(segment);

          if (!container) {
            // Create the appropriate container type
            if (isNextSegmentNumeric) {
              container = this.fb.array([]);
              this.logger.debug(`Created FormArray at ${builtPath}`);
            } else {
              container = this.fb.group({});
              this.logger.debug(`Created FormGroup at ${builtPath}`);
            }

            current.addControl(segment, container);
          } else if (
            isNextSegmentNumeric &&
            !(container instanceof FormArray)
          ) {
            // Replace with FormArray if needed
            current.removeControl(segment);
            container = this.fb.array([]);
            current.addControl(segment, container);
            this.logger.debug(`Replaced with FormArray at ${builtPath}`);
          } else if (
            !isNextSegmentNumeric &&
            !(container instanceof FormGroup)
          ) {
            // Replace with FormGroup if needed
            current.removeControl(segment);
            container = this.fb.group({});
            current.addControl(segment, container);
            this.logger.debug(`Replaced with FormGroup at ${builtPath}`);
          }
        } else if (current instanceof FormArray) {
          const index = Number(segment);

          // Ensure the array has enough elements
          while (current.length <= index) {
            if (isNextSegmentNumeric) {
              current.push(this.fb.array([]));
            } else {
              current.push(this.fb.group({}));
            }
          }

          container = current.at(index);
        }

        // Move to the next container
        current = container;

        // If the next segment is numeric, ensure the index exists in the array
        if (isNextSegmentNumeric && current instanceof FormArray) {
          const index = Number(segments[i + 1]);

          // Expand the array if needed
          while (current.length <= index) {
            const nextIsLast = i + 2 >= segments.length;

            if (nextIsLast) {
              // If the next segment is the last one, add a FormControl
              current.push(this.fb.control(defaultValue));
            } else {
              // Otherwise add a container based on what comes next
              const isNextNextSegmentNumeric = !isNaN(Number(segments[i + 2]));

              if (isNextNextSegmentNumeric) {
                current.push(this.fb.array([]));
              } else {
                current.push(this.fb.group({}));
              }
            }
          }
        }
      }
    }

    // This should never be reached if the path is valid
    this.logger.error(`Failed to create control at path: ${path}`);
    return null;
  }

  private toLowerCase(obj: any): any {
    if (obj === null || obj === undefined || typeof obj !== 'object') {
      return obj;
    }

    if (Array.isArray(obj)) {
      return obj.map((item) => this.toLowerCase(item));
    }

    const result: any = {};

    Object.keys(obj).forEach((key) => {
      const lowerCaseKey = key.toLowerCase();
      result[lowerCaseKey] = this.toLowerCase(obj[key]);
    });

    return result;
  }

  /**
   * Get a default value for a given field type
   */
  public getDefaultValueForFieldType(fieldType?: string): any {
    if (!fieldType) return '';

    switch (fieldType) {
      case 'Number':
        return null;
      case 'Checkbox':
        return false;
      case 'Date':
        return null;
      case 'Select':
      case 'Text':
      case 'TextArea':
      default:
        return '';
    }
  }


  /**
   * Get validators from field data based on metadata and field type
   */
  public getValidatorsFromFieldData(fieldData:FieldData<any, FieldMetadata>, isHidden: boolean): ValidatorFn[] {
    const validators: ValidatorFn[] = [];

    // Check if field is required and not hidden
    if (fieldData.metadata?.['required'] && !isHidden) {
      validators.push(Validators.required);
    }

    // Add validators based on field type
    switch (fieldData.type) {
      case FieldType.TEXT:
        // Add text-specific validators
        if (fieldData.metadata?.['minLength']) {
          validators.push(
            Validators.minLength(fieldData.metadata['minLength'])
          );
        }
        if (fieldData.metadata?.['maxLength']) {
          validators.push(
            Validators.maxLength(fieldData.metadata['maxLength'])
          );
        }
        if (fieldData.metadata?.['pattern']) {
          validators.push(
            Validators.pattern(fieldData.metadata['pattern'])
          );
        }
        // Add email validation if specified
        if (fieldData.metadata?.['isEmail']) {
          validators.push(Validators.email);
        }
        break;
      case FieldType.TEXTAREA:
        // Add text-specific validators
        if (fieldData.metadata?.['minLength']) {
          validators.push(
            Validators.minLength(fieldData.metadata['minLength'])
          );
        }
        if (fieldData.metadata?.['maxLength']) {
          validators.push(
            Validators.maxLength(fieldData.metadata['maxLength'])
          );
        }
        if (fieldData.metadata?.['pattern']) {
          validators.push(
            Validators.pattern(fieldData.metadata['pattern'])
          );
        }
        break;
      case FieldType.NUMBER:
        // Add number-specific validators
        if (fieldData.metadata?.['min'] !== undefined) {
          validators.push(Validators.min(fieldData.metadata['min']));
        }
        if (fieldData.metadata?.['max'] !== undefined) {
          validators.push(Validators.max(fieldData.metadata['max']));
        }
        break;
      // Add more field type specific validators as needed
    }

    return validators;
  }
}
