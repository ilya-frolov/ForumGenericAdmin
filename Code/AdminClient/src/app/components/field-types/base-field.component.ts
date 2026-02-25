import { Input, OnInit, Directive, Output, EventEmitter, TemplateRef, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { FieldMetadata } from 'src/app/interfaces/field-metadata';
import { FieldTypeComponent } from 'src/app/interfaces/field-type.interface';
import { v4 as uuidv4 } from 'uuid';

@Directive()
export abstract class BaseFieldComponent<InPutType, MetaType extends FieldMetadata> implements FieldTypeComponent<InPutType, MetaType>, OnInit, AfterViewInit {
  @Input() id: string;
  @Input() label!: string;
  @Input() metadata: MetaType = {} as MetaType;
  @Input() _value: InPutType;

  @Output() valueChange = new EventEmitter<InPutType>();

  @ViewChild('editTemplate', { static: true }) editTemplateRef!: TemplateRef<ElementRef>;
  @ViewChild('listTemplate', { static: true }) listTemplateRef!: TemplateRef<ElementRef>;
  @ViewChild('viewTemplate', { static: true }) viewTemplateRef!: TemplateRef<ElementRef>;
  
  constructor() {}

  ngOnInit(): void {
    if (!this.id) {
      this.id = this.generateUniqueId();
    }
  }

  ngAfterViewInit(): void {}

  /**
   * Generate a unique identifier for the component
   */
  protected generateUniqueId(): string {
    return uuidv4();
  }

  /**
   * Abstract methods that must be implemented by child classes
   */
  abstract getEditTemplate(): TemplateRef<ElementRef>;
  abstract getListTemplate(): TemplateRef<ElementRef>;
  abstract getViewTemplate(): TemplateRef<ElementRef>;

  /**
   * Default implementation for getValueForServer
   * Child classes should override this method if needed
   */
  getValueForServer(value: InPutType): InPutType {
    return value;
  }

  /**
   * Default implementation for parseValueFromServer
   * Child classes should override this method if needed
   */
  parseValueFromServer(value: InPutType): InPutType {
    return value;
  }

  /**
   * Handle value changes and emit the converted value
   */
  onValueChange(value: InPutType): void {
    this.valueChange.emit(value);
  }

  get value(): InPutType {
    return this._value;
  }

  set value(value: InPutType) {
    this._value = value;
  }
} 