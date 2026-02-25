import {ContainerNode, NodeBase} from "./form-structure";
import {IconType} from "./models";

export interface InputOption {
  display: string;
  value: string | number;
}

export interface ListDef {
  title: string;
  allowReOrdering: boolean;
  hideSearch: boolean;
  allowAdd: boolean;
  allowEdit: boolean;
  allowClone: boolean;
  allowDelete: boolean;
  allowDeleteAllRecords: boolean;
  showArchive: boolean;
  allowedExportFormats: number;
  allowExcelImport: boolean;
  allowItemSelection: boolean;
  excelExportFilename: string;
  defaultSortColumnName: string | null;
  defaultSortDirection: number;
  showDeleteConfirmation: boolean;
  deletionConfirmationTitle: string | null;
  deletionConfirmationDescription: string | null;
  columns: ListColumnInfo[];
  actions: ListAction[];
  inlineActions: ListAction[];
  selfActions: ListAction[];
  inputOptions: { [key: string]: InputOption[] };
}

export interface ListColumnInfo {
  propertyOrderIndex: number;
  type: string;
  inputOptionsKey: string | null;
  name: string;
  propertyName: string;
  allowSort: boolean;
  columnFilter: number;
  mainFilter: number;
  fixedColumn: boolean;
  inlineEdit: boolean;
  reservedProperty: string | null;
  isSortIndex: boolean;
}

export enum MainFilterType {
  Default = 0,
  DateRange = 1,
  TextSearch = 2,
  Dropdown = 3,
}

export interface FilterRule {
  operator: FilterOperator;
  value?: string;
  value2?: string; // For "between" operator
}

export interface AdvancedListFilter {
  propertyName: string;
  matchAll: boolean;
  rules: FilterRule[];
}

export interface SortColumn {
  propertyName: string;
  direction: SortDirection;
}

export interface ListRetrieveParams {
  filter?: string;
  pageIndex: number;
  pageSize: number;
  advancedFilters?: AdvancedListFilter[];
  sortColumns?: SortColumn[];
}

export interface TableDataRequest {
  showArchive: boolean;
  showDeleted: boolean;
  refId: string | null;
  filters?: ListRetrieveParams;
}

// Define the structure for list response data
export interface ListResponseData {
  recordsTotal: number;
  recordsFiltered: number;
  items: any[];
}

// Server filtering model interfaces
export enum SortDirection {
  Ascending = 0,
  Descending = 1,
}

export enum FilterOperator {
  StartsWith = 'startsWith',
  Contains = 'contains',
  EndsWith = 'endsWith',
  Equals = 'equals',
  NotEquals = 'notEquals',
  LessThan = 'lt',
  LessThanOrEqual = 'lte',
  GreaterThan = 'gt',
  GreaterThanOrEqual = 'gte',
  IsNull = 'null',
  IsNotNull = 'notNull',
  In = 'in',
  Between = 'between',
  IsEmpty = 'empty',
  IsNotEmpty = 'notEmpty',
}

export interface Constraint {
    matchMode: string;
    operator: string;
    value: string;
}

export enum ListActionType
{
  Custom = 0,
  List = 1,
  Edit = 2,
  OuterLink = 3
}

export enum ActionResponseType
{
  Json = 1,
  File = 2,
  Redirect = 3
}

export interface ListAction {
  type: ListActionType;
  text: string;
  segmentId: string;
  passEntityId: boolean;
  actionName: string | null;
  icon: string | null;
  iconType: IconType | null;
  idPropertyName: string | null;
  idName: string | null;
  urlFormat: boolean;
  redirect: boolean;
  requireConfirmation: boolean;
  passModelToConfirmation: boolean;
  passItemSelection: boolean;
  itemSelectionPropertyName: string | null;
  confirmationDialog: DialogStructure | null;
  reloadData: boolean;
  showSuccessMessage: boolean;
  successMessageTitle: string | null;
  isCustomRoute: boolean;
  responseType: ActionResponseType;

  // Client Only
  isSubmitting?: boolean;
}

export class DialogStructure {
  title: string;
  description: string;
  okButtonText: string;
  cancelButtonText: string;
  structure: ContainerNode;
  fileUploadController: string;

  // Client Only
  model: any;
}
