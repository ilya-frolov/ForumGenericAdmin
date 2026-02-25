import { FieldMetadata } from './field-metadata';
import { FieldType } from './field-data';

/**
 * Node types in the form structure
 */
export enum NodeType {
  ROOT = 'Root',
  TAB = 'Tab',
  CONTAINER = 'Container',
  FIELD = 'Field',
  SUBTYPE = 'SubType'
}

/**
 * Base node interface for all node types
 */
export interface NodeBase {
  name: string;
  nodeType: NodeType;
  attributes?: Record<string, any>;
  children?: NodeStructure[];
}

/**
 * Condition structure for field visibility
 */
export interface VisibilityCondition {
  show: boolean;
  rule: 'AND' | 'OR';
  conditions: Array<VisibilityCondition | ConditionLeaf>;
}

/**
 * Leaf condition for visibility rules
 */
export interface ConditionLeaf {
  property: string;
  operator: string;
  value: any;
}

/**
 * Field node interface
 */
export interface FieldNode extends NodeBase {
  nodeType: NodeType.FIELD;
  displayName: string;
  fieldType?: string;
  attributes: Record<string, any>;
  visibilityConditions?: VisibilityCondition[];
  inputOptionsKey?: string | null;
  complexType?: 'Repeater' | 'ComplexType' | null;
  complexTypeSettings?: any;
  subTypeStructure?: NodeStructure | null;
  repeaterItems?: any[] | null;
  conditions?: any | null;
  originalName?: string;
}

/**
 * Container, Tab, or Root node
 */
export interface ContainerNode extends NodeBase {
  attributes: Record<string, any>;
  nodeType: NodeType.CONTAINER | NodeType.TAB | NodeType.ROOT;
  children: NodeStructure[];
}

/**
 * Union type for all node structures
 */
export type NodeStructure = FieldNode | ContainerNode;

/**
 * Input options for select fields
 */
export interface SelectOption {
  display: string;
  value: any;
}

/**
 * Form structure as returned from the server
 */
export interface FormStructure {
  structure: NodeStructure;
  inputOptions: Record<string, SelectOption[]>;
  foreignTypes?: Record<string, NodeStructure>;
  model: any;
}
