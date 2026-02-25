import {
  Component,
  Input,
  OnInit,
  OnDestroy,
  Output,
  EventEmitter,
  ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, FormArray, ReactiveFormsModule } from '@angular/forms';
import {
  NodeStructure,
  NodeType,
  FieldNode,
  SelectOption,
} from '../../interfaces/form-structure';
import { DynamicFieldDirective } from '../field-types/dynamic-field.directive';
import { FieldData, FieldType } from '../../interfaces/field-data';
import { FieldMetadata } from '../../interfaces/field-metadata';
import {
  FormBuilderService,
  FormMode,
} from '../../services/form-builder.service';
import { LoggerService } from '../../services/logger.service';
import { PanelModule } from 'primeng/panel';
import { ButtonModule } from 'primeng/button';
import { AccordionModule } from 'primeng/accordion';
import { TooltipModule } from 'primeng/tooltip';
import { RippleModule } from 'primeng/ripple';
import { TabViewModule } from 'primeng/tabview';
import { dynamicNodeAnimations } from './dynamic-node.animations';

@Component({
  selector: 'app-dynamic-node',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    DynamicFieldDirective,
    PanelModule,
    ButtonModule,
    AccordionModule,
    TooltipModule,
    RippleModule,
    TabViewModule,
  ],
  templateUrl: './dynamic-node.component.html',
  animations: dynamicNodeAnimations,
})
export class DynamicNodeComponent implements OnInit, OnDestroy {
  @Input() node!: NodeStructure;
  @Input() formGroup!: FormGroup;
  @Input() formMode: FormMode = 'edit';
  @Input() inputOptions: Record<string, SelectOption[]> = {};
  @Input() path: string = '';
  @Input() test: boolean = false;

  @Output() fieldInitialized = new EventEmitter<string>();

  // For container collapsing
  isCollapsed = false;
  canCollapse = false;

  // Index of the last added repeater item that should be expanded
  lastAddedIndexToExpand: number | null = null;

  activeTabIndex = 0;

  // For field data
  fieldData: FieldData<any, FieldMetadata> | null = null;

  simpleFieldData: FieldData<any, FieldMetadata> | null = null;
  // For complex type fields
  isComplexType = false;
  complexTypeName: string | null = null;
  complexTypeStructure: NodeStructure | null = null;

  // Subscription for form value changes
  private formValueChangesSub: any;
  isHidden: boolean;

  constructor(
    public formBuilderService: FormBuilderService,
    private logger: LoggerService,
    private cdr: ChangeDetectorRef // Inject ChangeDetectorRef
  ) {}

  /**
   * Convert width percentage to Bootstrap column width
   */
  widthToBootstrapCol(width?: number): number {
    if (!width || width >= 100) return 12;
    if (width >= 75) return 9;
    if (width >= 66) return 8;
    if (width >= 50) return 6;
    if (width >= 33) return 4;
    if (width >= 25) return 3;
    return Math.max(1, Math.round((width / 100) * 12));
  }

  getTabs(children?: NodeStructure[]): NodeStructure[] {
    if (!children) return [];
    return children.filter((child) => child.nodeType === NodeType.TAB);
  }

  hasTabs(children?: NodeStructure[]): boolean {
    return this.getTabs(children).length > 0;
  }

  getNonTabContent(children?: NodeStructure[]): NodeStructure[] {
    if (!children) return [];
    return children.filter((child) => child.nodeType !== NodeType.TAB);
  }

  ngOnInit(): void {
    if (this.test) {
      this.test = false;
    }
    this.logger.debug('DynamicNodeComponent initialized with node:', this.node);
    // Set up container collapse state
    if (this.node.nodeType === NodeType.CONTAINER && this.node.attributes) {
      this.canCollapse = this.node.attributes['defaultCollapsed'] !== undefined; // if there is no defaultCollapsed attribute, the container cannot collapse
      this.isCollapsed = !!this.node.attributes['defaultCollapsed'];
      this.logger.debug(
        `Container node: ${this.node.name}, isCollapsed: ${this.isCollapsed}`
      );
    }

    if (this.node.nodeType === NodeType.FIELD) {
      this.path = this.path ? this.path + '.' + this.node.name : this.node.name;
      // Prepare field data if this is a field node
      this.logger.debug(
        `Preparing field data for node: ${this.node.name}, type: ${this.node.fieldType}`
      );
      this.initializeFieldData();
    }

    // Subscribe to form value changes to update visibility
    if (
      this.formGroup &&
      this.node.nodeType === NodeType.FIELD &&
      (this.node as FieldNode).visibilityConditions?.length > 0
    ) {
      this.formValueChangesSub = this.formGroup.valueChanges.subscribe(() => {
        this.logger.debug(
          `Re-evaluating visibility for ${this.node.name} due to form change`
        );
      });
    }
  }

  ngOnDestroy(): void {
    // Clean up subscriptions
    if (this.formValueChangesSub) {
      this.formValueChangesSub.unsubscribe();
    }
  }

  initializeFieldData(){
    if (this.node.nodeType === NodeType.FIELD){
      if (
        this.node.fieldType === 'DateTime' &&
        this.node.attributes?.['dateType'] != undefined
      ) {
        const dateType = this.node.attributes?.['dateType'];
        if (dateType == 1) {
          this.node.fieldType = 'DateOnly';
        } else if (dateType == 2) {
          this.node.fieldType = 'TimeOnly';
        }
      }
  
      if (this.node.complexType === 'ComplexType') {
        // Check if this is a complex type field - either by fieldType or complexType property
        this.setupComplexTypeField(this.node);
      } else if (this.node.complexType === 'Repeater') {
        this.setupRepeaterField(this.node);
      } else {
        this.prepareFieldData(this.node);
      }
    }
  }

  /**
   * Check if the repeater is for a simple type (like array of strings)
   * This is detected when the repeater has both complexType='Repeater' and a valid fieldType
   */
  get isSimpleTypeRepeater(): boolean {
    if (
      this.node.nodeType === NodeType.FIELD &&
      this.node.complexType === 'Repeater' &&
      !!this.node.fieldType
    ) {
      return true;
    }
    return false;
  }

  /**
   * Check if we are in create mode (vs edit mode)
   */
  get isCreateMode(): boolean {
    // Determine create vs edit mode based on whether the model has an ID value
    // If the model exists and has an 'id' or 'Id' field with a value, we're in edit mode
    if (this.formGroup?.value) {
      const model = this.formGroup.value;
      const idValue = model['id'] || model['Id'];
      return !idValue; // If no ID value, we're in create mode
    }
    return false; // Default to edit mode if no model data
  }

  /**
   * Check if a field is a complex field with null field type that needs special handling
   */
  isFieldWithNullType(node: FieldNode): boolean {
    // For complex types or repeaters, fieldType can be null
    if (
      !node.fieldType &&
      (node.complexType === 'ComplexType' || node.complexType === 'Repeater')
    ) {
      return true;
    }

    // For fields with complex data but no recognized complex type
    const value = this.getControlValue(node.name);
    if (!node.fieldType && typeof value === 'object' && value !== null) {
      return true;
    }

    return false;
  }

  /**
   * Get a control value by name
   */
  getControlValue(name: string): any {
    const control = this.formGroup?.get(name);
    return control?.value;
  }

  /**
   * Format a complex value for display
   */
  formatComplexValue(value: any): string {
    if (value === null || value === undefined) {
      return 'null';
    }

    if (typeof value === 'object') {
      // For arrays, show length
      if (Array.isArray(value)) {
        return `Array with ${value.length} items`;
      }

      // For objects, stringify with limited length for better display
      try {
        const json = JSON.stringify(value, null, 2);
        // Truncate if too long
        if (json.length > 100) {
          return json.substring(0, 100) + '...';
        }
        return json;
      } catch (e) {
        return 'Complex object';
      }
    }

    // For primitive values
    return String(value);
  }

  /**
   * Setup complex type field
   */
  setupComplexTypeField(node: FieldNode): void {
    this.isComplexType = true;
    if (node.complexTypeSettings?.typeName) {
      this.complexTypeName = node.complexTypeSettings.typeName;
      this.complexTypeStructure =
        this.formBuilderService.getForeignTypeStructure(this.complexTypeName);
      this.logger.debug(
        `Complex type field: ${node.name}, type: ${this.complexTypeName}`
      );
    } else {
      // For complex objects without type information, generate a structure dynamically
      const value = this.getControlValue(node.name);
      if (typeof value === 'object' && value !== null) {
        this.createDynamicComplexTypeStructure(node, value);
      }
    }
  }

  /**
   * Create a dynamic structure for complex objects without type information
   */
  private createDynamicComplexTypeStructure(node: FieldNode, value: any): void {
    if (!value || typeof value !== 'object') return;

    // Create a structure with fields for each property
    const children: NodeStructure[] = [];

    Object.entries(value).forEach(([key, propValue]) => {
      // Create a field node for each property
      const fieldNode: FieldNode = {
        name: key,
        displayName: key,
        nodeType: NodeType.FIELD,
        fieldType: this.getFieldTypeForValue(propValue),
        attributes: {},
        originalName: key,
      };

      children.push(fieldNode);
    });

    // Create the structure
    this.complexTypeStructure = {
      nodeType: NodeType.ROOT,
      name: `Dynamic_${node.name}`,
      children: children,
      attributes: {},
    };

    this.logger.debug(
      `Created dynamic complex type structure for ${node.name}:`,
      this.complexTypeStructure
    );
  }

  /**
   * Determine appropriate field type based on value type
   */
  private getFieldTypeForValue(value: any): string {
    if (value === null || value === undefined) return 'Text';

    if (typeof value === 'number') return 'Number';
    if (typeof value === 'boolean') return 'Checkbox';
    if (typeof value === 'string') {
      // Check if it looks like a date
      if (/^\d{4}-\d{2}-\d{2}/.test(value)) return 'Date';
      return 'Text';
    }

    // For arrays and objects, default to text (or could be handled differently)
    return 'Text';
  }

  /**
   * Check if a node is a field node
   */
  isFieldNode(node: NodeStructure): node is FieldNode {
    return node.nodeType === NodeType.FIELD;
  }

  /**
   * Check if a field is a repeater field
   */
  isRepeaterField(node: FieldNode): boolean {
    return node.complexType === 'Repeater';
  }

  /**
   * Check if a field should be hidden based on visibility settings
   */
  shouldHideField(node: FieldNode): boolean {
    // First check VisibilitySettings attributes (showOnCreate/showOnEdit/showOnView)
    if (node.attributes) {
      const showOnCreate = node.attributes['showOnCreate'] !== false; // Default to true if not specified
      const showOnEdit = node.attributes['showOnEdit'] !== false; // Default to true if not specified
      const showOnView = node.attributes['showOnView'] !== false; // Default to true if not specified

      // Check visibility based on form mode
      if (this.formMode === 'edit') {
        // In edit mode, distinguish between create and edit operations
        if (this.isCreateMode && !showOnCreate) {
          // Creating new item and field should not be shown on create
          return this.setHiddenState(true);
        } else if (!this.isCreateMode && !showOnEdit) {
          // Editing existing item and field should not be shown on edit
          return this.setHiddenState(true);
        }
      } else if (this.formMode === 'view' || this.formMode === 'list') {
        // In view or list mode, check showOnView setting
        if (!showOnView) {
          return this.setHiddenState(true);
        }
      }
      // For other modes, field is visible by default
    }

    // Now check the conditional visibility rules
    let isHidden = false;
    if (node.visibilityConditions && node.visibilityConditions.length > 0) {
      const formData = this.formGroup.value;
      // Evaluate each condition group (multiple conditions are treated as OR between groups)
      for (const conditionGroup of node.visibilityConditions) {
        const conditionResult = this.evaluateCondition(
          conditionGroup,
          formData
        );

        // AND between multiple conditions
        if (conditionGroup.show === true && conditionResult === false) {
          isHidden = true;
        } else if (conditionGroup.show === false && conditionResult === true) {
          isHidden = true;
        } else if (conditionGroup.show === true && conditionResult === true) {
          isHidden = false;
        } else if (conditionGroup.show === false && conditionResult === false) {
          isHidden = false;
        }
      }
    } else {
      // Default to showing the field if no visibility conditions are specified
      isHidden = false;
    }

    return this.setHiddenState(isHidden);
  }

  /**
   * Set the hidden state and update the form control accordingly
   */
  private setHiddenState(isHidden: boolean): boolean {
    if (isHidden !== this.isHidden) {
      this.isHidden = isHidden;
      if (this.isHidden) {
        this.removeControl();
      } else {
        this.initializeFieldData();
      }
    }
    return this.isHidden;
  }

  /**
   * Evaluate a visibility condition recursively
   */
  private evaluateCondition(condition: any, formData: any): boolean {
    if (!condition) return true;

    // If it's a group with sub-conditions
    let hasSubConditions =
      condition.conditions && condition.conditions.length > 0;
    if (hasSubConditions) {
      const results = condition.conditions.map((c: any) =>
        this.evaluateCondition(c, formData)
      );

      if (condition.rule === 'AND') {
        return results.every((result: boolean) => result === true);
      } else if (condition.rule === 'OR') {
        return results.some((result: boolean) => result === true);
      }

      return false;
    }

    // For leaf conditions
    const propertyValue = this.getNestedPropertyValue(
      formData,
      condition.property
    );
    const conditionValue = condition.value;

    // If property doesn't exist
    if (propertyValue === undefined || propertyValue === null) {
      // For operators that check existence, we can still evaluate
      if (
        condition.operator === '==' &&
        (conditionValue === null || conditionValue === '')
      ) {
        return true;
      } else if (
        condition.operator === '!=' &&
        conditionValue !== null &&
        conditionValue !== ''
      ) {
        return true;
      } else {
        return false;
      }
    }

    switch (condition.operator) {
      case '==':
        if (Array.isArray(conditionValue)) {
          return conditionValue.includes(propertyValue);
        }
        return propertyValue == conditionValue;
      case '!=':
        if (Array.isArray(conditionValue)) {
          return !conditionValue.includes(propertyValue);
        }
        return propertyValue != conditionValue;
      case '>':
        return propertyValue > conditionValue;
      case '>=':
        return propertyValue >= conditionValue;
      case '<':
        return propertyValue < conditionValue;
      case '<=':
        return propertyValue <= conditionValue;
      case 'contains':
        if (typeof propertyValue === 'string') {
          return propertyValue.includes(String(conditionValue));
        } else if (Array.isArray(propertyValue)) {
          return propertyValue.includes(conditionValue);
        }
        return false;
      case 'startswith':
        return (
          typeof propertyValue === 'string' &&
          propertyValue.startsWith(String(conditionValue))
        );
      case 'endswith':
        return (
          typeof propertyValue === 'string' &&
          propertyValue.endsWith(String(conditionValue))
        );
      default:
        this.logger.warn(
          `Unknown operator in condition: ${condition.operator}`
        );
        return false;
    }
  }

  /**
   * Get a nested property value from an object using dot notation
   */
  private getNestedPropertyValue(obj: any, path: string): any {
    if (!obj || !path) return undefined;

    return path.split('.').reduce((prev, curr) => {
      // Handle array indices in the path (e.g., "items.0.name")
      if (!isNaN(Number(curr)) && Array.isArray(prev)) {
        return prev[Number(curr)];
      }

      // Handle normal object properties
      return prev && typeof prev === 'object' ? prev[curr] : undefined;
    }, obj);
  }

  /**
   * Prepare field data for the dynamic field directive
   */
  prepareFieldData(node: FieldNode): void {
    this.logger.debug(
      `Preparing field data for ${node.name}, fieldType: ${node.fieldType}, inputOptionsKey: ${node.inputOptionsKey}`
    );

    // Get the value from the form control
    const fieldValue = this.formGroup.get(this.path)?.value;

    // For select fields, find the options
    let options: SelectOption[] | undefined;
    if (node.inputOptionsKey || node.attributes?.['options']) {
      options = this.findInputOptions(node);
      this.logger.debug(
        `Found ${options?.length || 0} options for select field: ${node.name}`,
        options
      );
    }

    this.logger.debug(
      `Field value from form control (${node.name}):`,
      fieldValue
    );

    // Create the field data object
    this.fieldData = {
      id: node.name,
      type: node.fieldType as unknown as FieldType,
      label: node.displayName,
      value: fieldValue,
      path: this.path,
      metadata: {
        ...(node.attributes || {}),
        placeholder:
          node.attributes?.['placeholder'] || `Enter ${node.displayName}`,
        // Add options for select fields
        options: options,
        // Add helper text if available
        helperText: node.attributes?.['tooltip'],
      },
    };

    this.logger.debug(`Final field data for ${node.name}:`, this.fieldData);
  }

  /**
   * Find input options for a field node by checking inputOptionsKey
   */
  private findInputOptions(node: FieldNode): SelectOption[] | undefined {
    if (!node.inputOptionsKey && !node.attributes?.['options']) {
      return undefined;
    }

    // If options are directly in attributes, use those
    if (node.attributes?.['options']) {
      return node.attributes['options'];
    }

    // If we have an inputOptionsKey but no inputOptions, return undefined
    if (!this.inputOptions || Object.keys(this.inputOptions).length === 0) {
      return undefined;
    }

    const inputOptionsKey = node.inputOptionsKey;
    if (!inputOptionsKey) {
      return undefined;
    }

    // Try exact match first
    if (this.inputOptions[inputOptionsKey]) {
      return this.inputOptions[inputOptionsKey];
    }

    // Try case-insensitive match
    const lowerCaseKey = inputOptionsKey.toLowerCase();
    const normalizedOptions: Record<string, SelectOption[]> = {};

    // Create a normalized map of all available options
    Object.entries(this.inputOptions).forEach(([key, value]) => {
      normalizedOptions[key.toLowerCase()] = value;
    });

    // Try to find by case-insensitive key
    if (normalizedOptions[lowerCaseKey]) {
      this.logger.debug(
        `Found options for ${inputOptionsKey} using case-insensitive match: ${lowerCaseKey}`
      );
      return normalizedOptions[lowerCaseKey];
    }

    // Try to find by partial match for "DbType_" prefix which might be inconsistent
    if (lowerCaseKey.includes('dbtype_')) {
      const withoutPrefix = lowerCaseKey.replace('dbtype_', '');

      for (const key in normalizedOptions) {
        if (key.includes(withoutPrefix)) {
          this.logger.debug(
            `Found options for ${inputOptionsKey} using partial match: ${key}`
          );
          return normalizedOptions[key];
        }
      }
    }

    this.logger.warn(`No options found for key: ${inputOptionsKey}`);
    return undefined;
  }

  /**
   * Get a form group by name
   */
  getFormGroup(name: string): FormGroup | null {
    const control = this.formGroup.get(name);
    return control instanceof FormGroup ? control : null;
  }

  /**
   * Check if a repeater can add more items
   */
  canAddRepeaterItem(node: FieldNode): boolean {
    if (this.formMode === 'view' || this.formMode === 'list') {
      return false;
    }

    if (!node.complexTypeSettings) {
      return true;
    }

    const numControls =
      this.formBuilderService.getNumOfControls(this.path) ?? 0; // Ensure numControls is not null

    const maxItems = node.complexTypeSettings.maxItems;
    const disabled = node.complexTypeSettings.disableRepeaterAddItemButton;

    // Disable if explicitly set or if maxItems is reached
    if (disabled || (maxItems !== undefined && numControls >= maxItems)) {
      return false;
    }

    return true;
  }

  canAddComplexTypeItem(): boolean {
    if (this.formMode === 'view' || this.formMode === 'list') {
      return false;
    }

    return true;
  }

  /**
   * Check if a repeater allows reordering
   */
  canReorderRepeaterItems(node: FieldNode): boolean {
    if (this.formMode === 'view' || this.formMode === 'list') {
      return false;
    }

    return node.complexTypeSettings?.allowReordering ?? true;
  }

  /**
   * Check if a repeater allows item removal
   */
  canRemoveRepeaterItem(node: FieldNode): boolean {
    if (this.formMode === 'view' || this.formMode === 'list') {
      return false;
    }

    // Check if removal is generally disabled
    if (node.complexTypeSettings?.disableRepeaterRemoveItemButton) {
      return false;
    }

    // Check minItems constraint
    if (node.complexTypeSettings?.minItems !== undefined) {
      const numControls =
        this.formBuilderService.getNumOfControls(this.path) ?? 0;
      if (numControls <= node.complexTypeSettings.minItems) {
        return false; // Cannot remove if at or below minItems
      }
    }

    return true;
  }

  /**
   * Add a new repeater item
   */
  addRepeaterItem(node: FieldNode, isExpanded: boolean = false): void {
    if (this.formMode === 'view' || this.formMode === 'list') {
      return;
    }

    const currentLength =
      this.formBuilderService.getNumOfControls(this.path) ?? 0;

    // Reset the expansion tracker before adding
    this.lastAddedIndexToExpand = null;

    if (this.isSimpleTypeRepeater) {
      // For simple type repeaters (like array of strings), add a control with default empty value
      const defaultValue = this.formBuilderService.getDefaultValueForFieldType(
        node.fieldType
      );
      this.formBuilderService.addRepeaterItem(
        this.path,
        defaultValue,
        this.formMode,
        node.fieldType
      );
      this.logger.debug(
        `Added simple repeater item to ${node.name} with default value:`,
        defaultValue
      );
    } else if (node.complexTypeSettings?.typeName) {
      // For complex type repeaters
      this.formBuilderService.addRepeaterItem(
        this.path,
        null,
        this.formMode,
        node.complexTypeSettings.typeName
      );
    } else {
      // For normal repeaters
      this.formBuilderService.addRepeaterItem(
        this.path,
        node.subTypeStructure,
        this.formMode
      );
    }

    // Check if the length actually increased to get the new index
    const newLength = this.formBuilderService.getNumOfControls(this.path) ?? 0;
    if (newLength > currentLength) {
      const newIndex = newLength - 1;
      if (isExpanded) {
        this.lastAddedIndexToExpand = newIndex;
        this.logger.debug(`Setting lastAddedIndexToExpand to: ${newIndex}`);
      } else {
        this.lastAddedIndexToExpand = null; // Ensure it's null if not expanding
      }
      // Trigger change detection so the template updates [selected] binding
      this.cdr.detectChanges();
    } else {
      // If length didn't increase (e.g., error in service), ensure tracker is null
      this.lastAddedIndexToExpand = null;
    }
  }

  addComplexTypeItem(node: FieldNode): void {
    if (this.formMode === 'view' || this.formMode === 'list') {
      return;
    }

    this.formBuilderService.addRepeaterItem(
      this.path,
      null,
      this.formMode,
      node.complexTypeSettings.typeName
    );
  }

  /**
   * Remove a repeater item
   */
  removeRepeaterItem(index: number): void {
    if (this.formMode === 'view' || this.formMode === 'list') {
      return;
    }

    this.formBuilderService.removeRepeaterItem(this.path, index);
  }

  /**
   * Move a repeater item
   */
  moveRepeaterItem(fromIndex: number, toIndex: number): void {
    if (this.formMode === 'view' || this.formMode === 'list') {
      return;
    }

    this.formBuilderService.moveRepeaterItem(this.path, fromIndex, toIndex);
  }

  /**
   * Get a readable header for repeater items
   * @param node The repeater field node
   * @param itemGroup The form group for the item
   * @param index The item index
   * @returns A descriptive header
   */
  getRepeaterItemHeader(
    node: FieldNode,
    itemGroup: FormGroup,
    index: number
  ): string {
    // Default header
    let header = `Item ${index + 1}`;

    // Try to get a more descriptive title
    if (node.complexTypeSettings?.typeName && itemGroup.value) {
      const value = itemGroup.value;

      // If that fails, check if there's any non-empty value we can use
      for (const key in value) {
        if (value[key] && typeof value[key] === 'string') {
          return `${header}: ${value[key]}`;
        }
      }
    }

    return header;
  }

  /**
   * Setup repeater field
   */
  setupRepeaterField(node: FieldNode): void {
    if (node.complexType === 'Repeater') {
      this.logger.debug(`Setting up repeater field: ${node.name}`);

      // Ensure the form array exists or is created
      const currentNumControls = this.formBuilderService.getNumOfControls(
        this.path
      );
      if (currentNumControls === null) {
        this.formBuilderService.getOrCreateControlByPath(this.path, []);
      }

      // get path from model
      const modelFieldValueFromPath =
        this.formBuilderService.getModelFieldValueFromPath(this.path);

      const minItems = Math.max(
        node.complexTypeSettings?.minItems ?? 0,
        modelFieldValueFromPath?.length ?? 0
      );

      if (minItems !== undefined && minItems > 0) {
        let itemsToAdd =
          minItems - (this.formBuilderService.getNumOfControls(this.path) ?? 0);

        if (itemsToAdd > 0) {
          this.logger.debug(
            `Auto-adding ${itemsToAdd} items to repeater ${node.name} to meet minItems: ${minItems}`
          );
          for (let i = 0; i < itemsToAdd; i++) {
            // Determine the type of item to add
            if (this.isSimpleTypeRepeater && node.fieldType) {
              const defaultValue =
                this.formBuilderService.getDefaultValueForFieldType(
                  node.fieldType
                );
              this.formBuilderService.addRepeaterItem(
                this.path,
                defaultValue,
                this.formMode,
                node.fieldType
              );
            } else if (node.complexTypeSettings?.typeName) {
              this.formBuilderService.addRepeaterItem(
                this.path,
                null, // Structure for complex type is handled by service based on typeName
                this.formMode,
                node.complexTypeSettings.typeName
              );
            } else if (node.subTypeStructure) {
              // For normal repeaters with a defined subTypeStructure
              this.formBuilderService.addRepeaterItem(
                this.path,
                node.subTypeStructure,
                this.formMode
              );
            } else {
              this.logger.warn(
                `Cannot auto-add item to repeater ${node.name}: missing typeName or subTypeStructure.`
              );
            }
          }
        }
      }

      // For simple type repeaters, ensure we have at least the empty FormArray
      // This part might be redundant if getOrCreateControlByPath already handles it
      if (this.isSimpleTypeRepeater && node.fieldType) {
        const numControls = this.formBuilderService.getNumOfControls(this.path);
        if (numControls === null) {
          // If the form array doesn't exist yet, create it
          this.formBuilderService.getOrCreateControlByPath(this.path, []);
        }
      }
    }
  }

  /**
   * Creates field data for a simple repeater item at the given index
   * @param index The index of the repeater item
   * @returns The field data for the repeater item
   */
  getSimpleRepeaterItemData(
    index: number
  ): FieldData<any, FieldMetadata> | null {
    if (!this.isSimpleTypeRepeater || !this.isFieldNode(this.node)) {
      return null;
    }

    const fieldNode = this.node as FieldNode;
    if (!fieldNode.fieldType) {
      return null;
    }

    // Get the value for this item
    const itemPath = `${this.path}.${index}`;
    const itemValue = this.formGroup.get(itemPath)?.value;

    // Create a field data object for this item
    const itemId = `${this.path}_${index}`;

    return {
      id: itemId,
      type: fieldNode.fieldType as unknown as FieldType,
      label: '', // No label needed for each item
      value: itemValue,
      path: itemPath,
      metadata: {
        ...(fieldNode.attributes || {}),
        placeholder:
          fieldNode.attributes?.['itemPlaceholder'] ||
          `Enter ${fieldNode.fieldType} value`,
        // For clean inline display with buttons
        compact: true,
        // Format for display in list mode
        displayFormat: (val: any) => {
          if (val === null || val === undefined || val === '') {
            return '(empty)';
          }
          return String(val);
        },
      },
    };
  }

  /**
   * Handle field initialization event from child directive
   */
  onFieldInitialized(fieldPath: string): void {
    this.logger.debug(
      `Field initialized in node ${this.node.name}: ${fieldPath}`
    );
    // Forward the event to parent components
    this.fieldInitialized.emit(fieldPath);
  }

  /**
   * Update validators on the form control based on current state (e.g., isHidden)
   */
  private removeControl(): void {
    if (!this.formGroup) {
      return;
    }

    const pathSegments = this.path.split('.');

    if (pathSegments.length > 0) {
      const controlName = pathSegments[pathSegments.length - 1];

      if (pathSegments.length === 1) {
        // Direct child of root form group
        this.formGroup.removeControl(controlName);
      } else {
        // Nested control - find parent form group
        const parentPath = pathSegments.slice(0, -1).join('.');
        const parentGroup = this.formGroup.get(parentPath);

        if (parentGroup instanceof FormGroup) {
          parentGroup.removeControl(controlName);
        }
      }
    }
  }
}
