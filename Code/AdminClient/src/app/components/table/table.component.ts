import {
  Component,
  EventEmitter,
  Input,
  OnInit,
  Output,
  ViewChild,
} from '@angular/core';
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
import { LangService } from '../../services/lang.service';
import {
  ActionResponseType,
  AdvancedListFilter,
  DialogStructure,
  FilterOperator,
  FilterRule,
  InputOption,
  ListAction,
  ListActionType,
  ListColumnInfo,
  ListDef,
  ListRetrieveParams,
  MainFilterType,
  SortColumn,
  SortDirection,
  TableDataRequest,
} from '../../interfaces/table-models';
import { Table, TableLazyLoadEvent, TableRowReorderEvent } from 'primeng/table';
import { finalize, Observable } from 'rxjs';
import { ServerResponse } from 'src/app/interfaces/server-response';
import { HttpResponse } from '@angular/common/http';
import { FileSaverService } from 'ngx-filesaver';
import { AppService } from '../../services/app.service';
import { DialogService } from 'primeng/dynamicdialog';
import { TranslateService } from '@ngx-translate/core';
import { DynamicConfirmModalComponent } from '../../modals/dynamic-confirm-modal/dynamic-confirm-modal.component';
import { FormBuilder, FormGroup } from '@angular/forms';
import { FieldData, FieldType } from 'src/app/interfaces/field-data';
import { FieldMetadata } from 'src/app/interfaces/field-metadata';
import { ExportFormat } from 'src/app/interfaces/export-moodel';

@Component({
  selector: 'admin-dynamic-table',
  templateUrl: './table.component.html',
  standalone: false,
  providers: [ConfirmationService, MessageService, DialogService],
})
export class TableComponent implements OnInit {
  protected readonly ListActionType = ListActionType;
  protected readonly MainFilterType = MainFilterType;
  public readonly FieldType = FieldType;

  // Status options for filtering (can be moved to inputs if needed)
  @Input() statuses: string[] = ['In Stock', 'Low Stock', 'Out of Stock'];

  // Dynamic inputs for reusable component
  @Input() loadDataFunc!: (data: TableDataRequest) => Observable<any[]>;
  @Input() deleteItemFunc?: (item: any) => Observable<ServerResponse<any>>;
  @Input() archiveItemFunc?: (item: any) => Observable<ServerResponse<any>>;
  @Input() editItemFunc?: (item: any) => Observable<boolean>;
  @Input() newItemFunc?: () => Observable<boolean>;
  @Input() saveReorderFunc?: (items: any[]) => Observable<ServerResponse<any>>;
  @Input() exportDataFunc?: (
    format: ExportFormat,
    data: TableDataRequest
  ) => Observable<HttpResponse<Blob>>;
  @Input() importTemplateFunc?: () => Observable<HttpResponse<Blob>>;
  @Input() importDataFunc?: () => void;
  @Input() deleteAllFunc?: () => Observable<ServerResponse<any>>;
  @Input() allowedExportFormats: number = 0;
  @Input() listDef: ListDef | null = null;
  @Input() entityId?: number;
  @Input() totalRecordsFetched: number = 0;
  @Input() totalRowCount: number = 0;
  @Input() showArchived: boolean = false;
  @Input() showDeleted: boolean = false;
  @Input() pageSize: number = 10;

  // Events for parent component to handle
  @Output() rowView = new EventEmitter<any>();

  private filters: ListRetrieveParams = {
    pageIndex: 0,
    pageSize: 10,
    filter: '',
    sortColumns: [],
    advancedFilters: [],
  };

  private tableRequest: TableDataRequest = {
    showArchive: false,
    showDeleted: false,
    refId: null,
    filters: Object.assign({}, this.filters),
  };

  isExportDisabled: boolean = false;
  isImportDisabled: boolean = false;
  isImportTemplateDisabled: boolean = false;
  isNewItemDisabled: boolean = false;
  isDeleteDisabled: boolean = false;
  isDeleteAllDisabled: boolean = false;

  loading: boolean = true;
  tableData: any[] = [];
  first: number = 0;

  // Local component state
  expandedRows: { [key: string]: boolean } = {};

  // Settings menu items
  settingsItems: MenuItem[] = [];

  // Export options for split button
  exportOptions: MenuItem[] = [];

  // Import options for split button
  importOptions: MenuItem[] = [];

  // Reordering state
  isReordering: boolean = false;
  private originalData: any[] = [];

  @ViewChild('dt') dataTable: Table;
  isEditDisabled: boolean;
  public formGroup: FormGroup;
  searchValue: string = '';
  mainFilterValues: { [columnName: string]: any } = {};
  mainDateFromValues: { [columnName: string]: Date | null } = {};
  mainDateToValues: { [columnName: string]: Date | null } = {};

  constructor(
    public langService: LangService,
    private appService: AppService,
    private dialogService: DialogService,
    private confirmationService: ConfirmationService,
    private messageService: MessageService,
    private translateService: TranslateService,
    private fileSaverService: FileSaverService,
    private fb: FormBuilder
  ) {
    this.formGroup = this.fb.group({});
  }

  ngOnInit(): void {
    // Initialize expandedRows with empty object
    this.expandedRows = {};

    // Initialize settings menu items
    this.settingsItems = [
      {
        label: this.translateService.instant('SHARED.RefreshData'),
        icon: 'pi pi-refresh',
        command: () => this.loadData(),
      },
      { separator: true },
      this.showArchived
        ? {
            label: this.translateService.instant('SHARED.ShowArchived'),
            icon: 'pi pi-archive',
            command: () => this.toggleArchived(),
          }
        : null,
      this.showDeleted
        ? {
            label: this.translateService.instant('SHARED.ShowDeleted'),
            icon: 'pi pi-trash',
            command: () => this.toggleDeleted(),
          }
        : null,
      { separator: true },
      {
        label: this.translateService.instant('SHARED.Settings'),
        icon: 'pi pi-cog',
      },
    ].filter((item) => item !== null);

    if (this.allowedExportFormats == null || this.allowedExportFormats == 0) {
      this.isExportDisabled = true;
    }

    if (this.importDataFunc == null) {
      this.isImportDisabled = true;
    }

    if (this.importTemplateFunc == null) {
      this.isImportTemplateDisabled = true;
    }

    if (this.newItemFunc == null) {
      this.isNewItemDisabled = true;
    }

    if (this.deleteItemFunc == null) {
      this.isDeleteDisabled = true;
    }

    if (this.editItemFunc == null) {
      this.isEditDisabled = true;
    }

    // Disable Delete All if function not provided
    if (this.deleteAllFunc == null) {
      this.isDeleteAllDisabled = true;
    }

    this.initExportOptions();
    this.initImportOptions();
    this.applyDefaultSortFromListDef();

    this.loadData();
  }

  public confirmDelete(event: Event, item: any): void {
    // Prevent event propagation to avoid multiple dialogs
    event.stopPropagation();

    this.confirmationService.confirm({
      message: this.translateService.instant('SHARED.DeleteItem'),
      header: this.translateService.instant('SHARED.DangerZone'),
      icon: 'pi pi-info-circle',
      rejectLabel: this.translateService.instant('SHARED.Cancel'),
      rejectButtonProps: {
        label: this.translateService.instant('SHARED.Cancel'),
        severity: 'secondary',
        outlined: true,
      },
      acceptButtonProps: {
        label: this.translateService.instant('SHARED.Delete'),
        severity: 'danger',
      },
      accept: () => {
        this.deleteItemFunc(item).subscribe((response) => {
          if (response.result) {
            // Show a success message
            this.messageService.add({
              severity: 'info',
              summary: this.translateService.instant('SHARED.Deleted'),
              detail: `Item with ID ${item.id} deleted`,
            });

            // Refresh the data after deletion
            this.loadData();
          } else {
            // Show error message
            this.messageService.add({
              severity: 'error',
              summary: this.translateService.instant('SHARED.Failed'),
              detail: `Item with ID ${item.id} deletion failed`,
            });
          }
        });
      },
      reject: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Rejected',
          detail: 'Delete operation cancelled',
        });
      },
    });
  }

  public confirmArchive(event: Event, item: any): void {
    event.stopPropagation();

    this.archiveItemFunc(item).subscribe((response) => {
      if (response.result) {
        this.messageService.add({
          severity: 'info',
          summary: this.translateService.instant('SHARED.Archived'),
          detail: `Item with ID ${item.id} archived`,
        });

        this.loadData();
      } else {
        this.messageService.add({
          severity: 'error',
          summary: this.translateService.instant('SHARED.Failed'),
          detail: `Item with ID ${item.id} archiving failed`,
        });
      }
    });
  }

  public editItem(item: any): void {
    this.editItemFunc(item).subscribe((shouldReload) => {
      if (shouldReload) {
        this.loadData();
      }
    });
  }

  public newItem(): void {
    this.newItemFunc().subscribe((shouldReload) => {
      if (shouldReload) {
        this.loadData();
      }
    });
  }
  // Get severity for status tag
  getSeverity(
    status: string
  ): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
    switch (status?.toLowerCase()) {
      case 'active':
      case 'approved':
      case 'completed':
      case 'in stock':
      case 'true':
        return 'success';
      case 'low stock':
      case 'pending':
      case 'processing':
      case 'warning':
        return 'warn';
      case 'out of stock':
      case 'inactive':
      case 'rejected':
      case 'failed':
      case 'false':
        return 'danger';
      default:
        return 'info';
    }
  }

  public initExportOptions() {
    if (this.isExportDisabled) {
      return;
    }

    if ((this.allowedExportFormats & 1) == 1) {
      this.exportOptions.push({
        label: 'Excel',
        icon: 'pi pi-file-excel',
        command: () => this.onExport(ExportFormat.Excel),
      });
    }

    if ((this.allowedExportFormats & 2) == 2) {
      this.exportOptions.push({
        label: 'PDF',
        icon: 'pi pi-file-pdf',
        command: () => this.onExport(ExportFormat.Pdf),
      });
    }

    if ((this.allowedExportFormats & 4) == 4) {
      this.exportOptions.push({
        label: 'CSV',
        icon: 'pi pi-file',
        command: () => this.onExport(ExportFormat.Csv),
      });
    }
  }

  public initImportOptions() {
    if (!this.isImportDisabled) {
      this.importOptions.push({
        label: this.translateService.instant('SHARED.UploadFile'),
        icon: 'pi pi-upload',
        command: () => this.onImport(),
      });
    }

    if (!this.isImportTemplateDisabled) {
      this.importOptions.push({
        label: this.translateService.instant('SHARED.DownloadTemplate'),
        icon: 'pi pi-download',
        command: () => this.onImportTemplate(),
      });
    }
  }

  // Format cell value based on column type
  formatCellValue(value: any, column: ListColumnInfo): string {
    if (value === null || value === undefined) {
      return '';
    }

    switch (column.type?.toLowerCase()) {
      case 'number':
        return new Intl.NumberFormat().format(value);
      case 'text':
        return value.toString();
      default:
        return value.toString();
    }
  }

  // Format date and time values
  formatDateTime(dateValue: string | Date): string {
    if (!dateValue) return '';

    const date = new Date(dateValue);
    if (isNaN(date.getTime())) return '';

    return (
      date.toLocaleDateString() +
      ' ' +
      date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    );
  }

  // Get display value for dropdown/select options
  getDisplayValueForOption(optionsKey: string | undefined, value: any): string {
    if (
      !optionsKey ||
      value === null ||
      value === undefined ||
      !this.listDef?.inputOptions
    ) {
      return value?.toString() || '';
    }

    const allInputOptions = this.listDef?.inputOptions || {};
    const allInputOptionsLower = Object.fromEntries(
      Object.entries(allInputOptions).map(([key, value]) => [
        key.toLowerCase(),
        value,
      ])
    );
    const optionsArray = allInputOptionsLower[optionsKey.toLowerCase()];
    if (!optionsArray) return value?.toString() || '';

    const option = (optionsArray as InputOption[]).find(
      (opt: InputOption) => opt.value == value
    ); // Using == for type coercion
    return option ? option.display : value?.toString() || '';
  }

  // Get array of display values for multi-select field
  getMultiSelectValues(optionsKey: string | undefined, values: any): string[] {
    if (!optionsKey || !values || !this.listDef?.inputOptions) {
      return [];
    }

    // Handle both string (comma-separated) and array formats
    const valueArray = Array.isArray(values)
      ? values
      : values.toString().split(',');

    const options = this.listDef.inputOptions[optionsKey.toLowerCase()];
    if (!options) return valueArray.map((v: any) => v.toString());

    return valueArray.map((val: any) => {
      const option = options.find((opt) => opt.value == val); // Using == for type coercion
      return option ? option.display : val.toString();
    });
  }

  public getColumnFilterOptions(col: ListColumnInfo): any[] {
    if (!col.inputOptionsKey || !this.listDef?.inputOptions) {
      return [];
    }

    const optionsKey = col.inputOptionsKey.toLowerCase();
    const allInputOptions = this.listDef?.inputOptions || {};
    const allInputOptionsLower = Object.fromEntries(
      Object.entries(allInputOptions).map(([key, value]) => [
        key.toLowerCase(),
        value,
      ])
    );
    const resolvedOptions =
      allInputOptionsLower[optionsKey] ||
      allInputOptionsLower[col.inputOptionsKey] ||
      [];

    if (!Array.isArray(resolvedOptions)) {
      return [];
    }

    return resolvedOptions.map((opt) => ({
      display: opt.display,
      value: opt.value,
    }));
  }

  // region Data Loading

  onLazyLoad(event: TableLazyLoadEvent): void {
    // Check which event properties have changed
    let needsUpdate = false;
    // Handle pagination
    if (event.first !== undefined && event.rows !== undefined) {
      const newPageIndex = event.first / event.rows;
      if (
        this.tableRequest.filters &&
        this.tableRequest.filters.pageIndex !== newPageIndex
      ) {
        this.first = event.first; // Update first position
        this.tableRequest.filters.pageIndex = newPageIndex;
        this.tableRequest.filters.pageSize = event.rows;
        needsUpdate = true;
      }
    }

    // Handle sorting
    if (
      event.sortField ||
      (event.multiSortMeta && event.multiSortMeta.length > 0)
    ) {
      const newSortColumns: SortColumn[] = [];

      if (event.sortField) {
        newSortColumns.push({
          propertyName: event.sortField as string,
          direction:
            event.sortOrder === 1
              ? SortDirection.Ascending
              : SortDirection.Descending,
        });
      }

      // Check if sort has changed to avoid loops
      const currentSort = JSON.stringify(
        this.tableRequest.filters.sortColumns || []
      );
      const newSort = JSON.stringify(newSortColumns);

      if (currentSort !== newSort) {
        this.tableRequest.filters.sortColumns = newSortColumns;
        needsUpdate = true;
      }
    }

    // Handle filtering
    if (event.filters) {
      const newAdvancedFilters: AdvancedListFilter[] = [];

      // Process each filter
      Object.entries(event.filters).forEach(([field, constraints]) => {
        if (field === 'global') {
          // Handle global filter
          const globalFilter = event.globalFilter as string;
          if (
            globalFilter &&
            this.tableRequest.filters.filter !== globalFilter
          ) {
            this.tableRequest.filters.filter = globalFilter;
            needsUpdate = true;
          }
          return;
        }

        // Get column definition to determine type
        const column = this.listDef?.columns.find(
          (col) => col.propertyName === field
        );
        if (!column) return;

        const constraintsArray = Array.isArray(constraints)
          ? constraints
          : [constraints];

        if (!constraintsArray[0] || !constraintsArray[0].value) return;
        const isMatchAll = constraintsArray[0].operator == 'and' ? true : false;

        const rules: FilterRule[] = [];
        constraintsArray.forEach((constraint) => {
          if (!constraint || !constraint.value) return;
          rules.push({
            operator: constraint.matchMode as FilterOperator,
            value: constraint.value.toString(),
          });
        });

        if (rules.length > 0) {
          const advancedFilter: AdvancedListFilter = {
            propertyName: field,
            matchAll: isMatchAll ?? true,
            rules: rules,
          };
          newAdvancedFilters.push(advancedFilter);
        }
      });

      // Check if filters have changed to avoid loops
      const currentFilters = JSON.stringify(
        this.tableRequest.filters.advancedFilters || []
      );
      const mergedFilters = [
        ...this.buildMainAdvancedFilters(),
        ...newAdvancedFilters,
      ];
      const mergedFiltersJson = JSON.stringify(mergedFilters);

      if (currentFilters !== mergedFiltersJson) {
        this.tableRequest.filters.advancedFilters = mergedFilters;
        // Reset to first page on new filter if we're not explicitly handling pagination in this same event
        if (!event.first && this.tableRequest.filters.pageIndex !== 0) {
          this.tableRequest.filters.pageIndex = 0;
          this.first = 0;
        }
        needsUpdate = true;
      }
    }

    // Only load data if something has changed
    if (needsUpdate) {
      this.loadData();
    }
  }

  public loadData(): void {
    this.loading = true;
    this.loadDataFunc(this.tableRequest)
      .pipe(
        finalize(() => {
          this.loading = false;
        })
      )
      .subscribe({
        next: (response) => {
          this.tableData = response.filter((item) =>
            Object.values(item).some((value) =>
              value
                ?.toString()
                ?.toLowerCase()
                .includes(this.searchValue?.toLowerCase())
            )
          );

          // Parse items
          for (const currItem of this.tableData) {
            this.calcExtraMenuItems(currItem);
          }
        },
        error: (error) => {
          console.error('Error loading table data:', error);
          this.messageService.add({
            severity: 'error',
            summary: this.translateService.instant('SHARED.Error'),
            detail: this.translateService.instant(
              'SHARED.FailedToLoadTableData'
            ),
          });
        },
      });
  }

  // endregion

  public toggleArchived(): void {
    this.tableRequest.showArchive = !this.tableRequest.showArchive;
    this.loadData();
  }

  public toggleDeleted(): void {
    this.tableRequest.showDeleted = !this.tableRequest.showDeleted;
    this.loadData();
  }

  // Removed individual handlers as they're replaced by onLazyLoad
  onGlobalFilter(value: string): void {
    this.loadData();
  }

  clearSearch(): void {
    if (this.searchValue !== '') {
      this.searchValue = '';
      this.loadData();
    }
  }

  onExport(format: ExportFormat): void {
    this.exportDataFunc(format, this.tableRequest).subscribe((response) => {
      // TODO: Add file name?
      this.fileSaverService.save(response.body);
    });
  }

  clear(dt: Table): void {
    dt.clear();
    this.tableRequest.filters = Object.assign({}, this.filters);
    this.mainFilterValues = {};
    this.mainDateFromValues = {};
    this.mainDateToValues = {};
    this.loadData();
  }

  // Getter for global filter fields
  get globalFilterFields(): string[] {
    return this.listDef?.columns?.map((col) => col.propertyName) || [];
  }

  get mainFilterColumns(): ListColumnInfo[] {
    if (!this.listDef?.columns) {
      return [];
    }

    return this.listDef.columns.filter(
      (col) => col.mainFilter && col.mainFilter !== MainFilterType.Default
    );
  }

  public getMainFilterDropdownOptions(col: ListColumnInfo): any[] {
    if (col.type === 'Select' || col.type === 'MultiSelect') {
      return this.getColumnFilterOptions(col);
    }

    if (col.type === 'Boolean' || col.type === 'Checkbox') {
      return [
        { display: 'Yes', value: true },
        { display: 'No', value: false },
      ];
    }

    return [];
  }

  public applyMainFilters(): void {
    this.tableRequest.filters.pageIndex = 0;
    this.first = 0;
    this.tableRequest.filters.advancedFilters = [
      ...this.buildMainAdvancedFilters(),
    ];
    this.loadData();
  }

  public clearMainFilters(): void {
    this.mainFilterValues = {};
    this.mainDateFromValues = {};
    this.mainDateToValues = {};
    this.tableRequest.filters.advancedFilters = [];
    this.tableRequest.filters.pageIndex = 0;
    this.first = 0;
    this.loadData();
  }

  private applyDefaultSortFromListDef(): void {
    if (
      !this.listDef ||
      !this.listDef.defaultSortColumnName ||
      (this.tableRequest.filters.sortColumns &&
        this.tableRequest.filters.sortColumns.length > 0)
    ) {
      return;
    }

    const direction =
      this.listDef.defaultSortDirection === SortDirection.Descending
        ? SortDirection.Descending
        : SortDirection.Ascending;

    this.tableRequest.filters.sortColumns = [
      {
        propertyName: this.listDef.defaultSortColumnName,
        direction,
      },
    ];
  }

  private buildMainAdvancedFilters(): AdvancedListFilter[] {
    const filters: AdvancedListFilter[] = [];

    for (const col of this.mainFilterColumns) {
      if (col.mainFilter === MainFilterType.TextSearch) {
        const value = this.mainFilterValues[col.propertyName];
        if (value != null && value.toString().trim() !== '') {
          filters.push({
            propertyName: col.propertyName,
            matchAll: true,
            rules: [
              {
                operator: FilterOperator.Contains,
                value: value.toString().trim(),
              },
            ],
          });
        }
        continue;
      }

      if (col.mainFilter === MainFilterType.Dropdown) {
        const value = this.mainFilterValues[col.propertyName];
        if (value !== null && value !== undefined && value !== '') {
          filters.push({
            propertyName: col.propertyName,
            matchAll: true,
            rules: [
              {
                operator: FilterOperator.Equals,
                value: value.toString(),
              },
            ],
          });
        }
        continue;
      }

      if (col.mainFilter === MainFilterType.DateRange) {
        const from = this.mainDateFromValues[col.propertyName];
        const to = this.mainDateToValues[col.propertyName];

        if (from && to) {
          filters.push({
            propertyName: col.propertyName,
            matchAll: true,
            rules: [
              {
                operator: FilterOperator.Between,
                value: from.toISOString(),
                value2: to.toISOString(),
              },
            ],
          });
        } else if (from) {
          filters.push({
            propertyName: col.propertyName,
            matchAll: true,
            rules: [
              {
                operator: FilterOperator.GreaterThanOrEqual,
                value: from.toISOString(),
              },
            ],
          });
        } else if (to) {
          filters.push({
            propertyName: col.propertyName,
            matchAll: true,
            rules: [
              {
                operator: FilterOperator.LessThanOrEqual,
                value: to.toISOString(),
              },
            ],
          });
        }
      }
    }

    return filters;
  }

  // region ReOrdering

  public startReordering(): void {
    if (!this.listDef.allowReOrdering || this.isReordering) return;

    const sortColumn = this.listDef?.columns?.find((col) => col.isSortIndex);
    if (!sortColumn) return;

    this.isReordering = true;
    this.originalData = [...this.tableData];

    // Save the current sort index on each item
    for (const currentItem of this.tableData) {
      currentItem['backupIndex'] = currentItem[sortColumn.propertyName];
    }

    // Trigger lazy loading with the new sort parameters
    this.tableRequest.filters.pageIndex = 0;
    this.tableRequest.filters.sortColumns = [
      {
        propertyName: sortColumn.propertyName,
        direction: SortDirection.Ascending,
      },
    ];

    this.loadData();
  }

  public onRowReorder(e: TableRowReorderEvent): void {
    if (!this.isReordering) return;

    // Update sortIndex based on new order
    const sortColumn = this.listDef?.columns?.find((col) => col.isSortIndex);

    // Find the lowest sort index from current items
    const lowestIndex = Math.min(
      ...this.tableData.map((item) => item[sortColumn.propertyName])
    );

    // Update sort indices sequentially starting from lowest
    this.tableData.forEach((item, index) => {
      item[sortColumn.propertyName] = lowestIndex + index;
    });
  }

  public cancelReordering(): void {
    if (!this.isReordering) return;

    // Restore the backup index
    const sortColumn = this.listDef?.columns?.find((col) => col.isSortIndex);
    for (const item of this.tableData) {
      item[sortColumn.propertyName] = item['backupIndex'];
    }

    this.tableData = [...this.originalData];
    this.originalData = [];
    this.isReordering = false;
  }

  public saveReordering(): void {
    if (!this.isReordering || !this.saveReorderFunc) return;

    this.loading = true;
    this.saveReorderFunc(this.tableData)
      .pipe(
        finalize(() => {
          this.loading = false;
        })
      )
      .subscribe({
        next: (response) => {
          if (response.result) {
            this.messageService.add({
              severity: 'success',
              summary: this.translateService.instant('SHARED.Success'),
              detail: this.translateService.instant(
                'SHARED.ReorderingSavedSuccessfully'
              ),
            });

            // Reload the data after completion and cancel reorder state
            this.loadData();
            this.isReordering = false;
            this.originalData = [];
          } else {
            this.messageService.add({
              severity: 'error',
              summary: this.translateService.instant('SHARED.Error'),
              detail:
                response.error ||
                this.translateService.instant('SHARED.FailedToSaveNewOrder'),
            });
          }
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: this.translateService.instant('SHARED.Error'),
            detail:
              this.translateService.instant(
                'SHARED.AnErrorOccurredWhileSavingTheNewOrder'
              ) +
              ' ' +
              error,
          });
        },
      });
  }

  // endregion

  public onArchiveClick(item: any): void {
    this.confirmationService.confirm({
      message: this.translateService.instant(
        'SHARED.DoYouWantToArchiveThisItem'
      ),
      header: this.translateService.instant('SHARED.ArchiveItem'),
      icon: 'pi pi-info-circle',
      accept: () => {
        if (this.archiveItemFunc) {
          this.archiveItemFunc(item).subscribe((response) => {
            if (response.result) {
              this.messageService.add({
                severity: 'success',
                summary: this.translateService.instant('SHARED.Archived'),
                detail: this.translateService.instant(
                  'SHARED.ItemArchivedSuccessfully'
                ),
              });
              this.loadData();
            } else {
              this.messageService.add({
                severity: 'error',
                summary: this.translateService.instant('SHARED.Error'),
                detail:
                  response.error ||
                  this.translateService.instant('SHARED.FailedToArchiveItem'),
              });
            }
          });
        }
      },
    });
  }

  // region Links and Actions

  public getRouterLink(
    listAction: ListAction,
    row: any,
    isSelfAction: boolean = false
  ): any[] {
    const linkParts = ['/'];

    if (listAction.type === ListActionType.Custom) {
      if (listAction.isCustomRoute) {
        linkParts.push(listAction.actionName);
      } else {
        linkParts.push('custom');
      }
    } else {
      linkParts.push(listAction.segmentId);

      switch (listAction.type) {
        case ListActionType.List: {
          break;
        }
        case ListActionType.Edit: {
          linkParts.push('edit');
          break;
        }
      }

      if (listAction.actionName) {
        linkParts.push(listAction.actionName);
      }
    }

    if (listAction.passEntityId) {
      if (isSelfAction) {
        linkParts.push(this.entityId.toString());
      } else {
        if (listAction.idPropertyName) {
          linkParts.push(row[listAction.idPropertyName]);
        } else {
          linkParts.push(row.id);
        }
      }
    }

    return linkParts;
  }

  public getOuterLink(
    listAction: ListAction,
    row: any,
    isSelfAction: boolean = false
  ): string {
    let url: string = listAction.actionName;

    if (listAction.passEntityId) {
      let id;
      if (isSelfAction) {
        id = this.entityId;
      } else {
        if (listAction.idPropertyName) {
          id = row[listAction.idPropertyName];
        } else {
          id = row.id;
        }
      }

      if (listAction.urlFormat) {
        let idName;
        if (listAction.idName) {
          idName = listAction.idName;
        } else {
          idName = 'id';
        }

        url = url.replace('{' + idName + '}', id);
      } else {
        if (listAction.idName) {
          if (!url.includes('?')) {
            url += '?';
          } else {
            url += '&';
          }
          url += listAction.idName + '=' + id;
        } else {
          if (!url.endsWith('/')) {
            url += '/';
          }
          url += id;
        }
      }
    }

    return url;
  }

  public canShowListAction(listAction: ListAction, row: any): boolean {
    //let canShow = false;

    // Check conditions
    /*if (!listAction.condition) {
      canShow = true;
    } else {
      canShow = ConditionHelpers.isConditionMet(listAction.condition, row);
    }*/

    //return canShow;

    return true;
  }

  public isLinkValidWithParameter(
    listAction: ListAction,
    row: any,
    isSelfAction: boolean = false
  ): boolean {
    let valid: boolean = false;
    if (listAction.passEntityId) {
      if (isSelfAction) {
        valid = this.entityId != null;
      } else {
        if (listAction.idPropertyName) {
          valid = row[listAction.idPropertyName] != null;
        } else {
          valid = row.id != null;
        }
      }
    }

    return valid;
  }

  private calcExtraMenuItems(item: any): void {
    const menuItems: MenuItem[] = [];

    if (this.listDef.showArchive) {
      menuItems.push({
        label: this.translateService.instant('SHARED.Archive'),
        icon: 'pi pi-book',
        command: () => {
          this.onArchiveClick(item);
        },
      });
    }

    if (this.listDef.actions?.length > 0) {
      for (const currAction of this.listDef.actions) {
        if (
          currAction.type === ListActionType.OuterLink &&
          this.isLinkValidWithParameter(currAction, item)
        ) {
          menuItems.push({
            label: currAction.text,
            url: this.getOuterLink(currAction, item, false),
            target: currAction.redirect ? '_self' : '_blank',
          });
        } else if (
          currAction.type !== ListActionType.OuterLink &&
          currAction.redirect &&
          this.isLinkValidWithParameter(currAction, item)
        ) {
          menuItems.push({
            label: currAction.text,
            routerLink: this.getRouterLink(currAction, item),
          });
        } else if (
          currAction.type !== ListActionType.OuterLink &&
          !currAction.redirect &&
          this.isLinkValidWithParameter(currAction, item, false)
        ) {
          menuItems.push({
            label: currAction.text,
            command: (e) => {
              this.runCustomAction(currAction, item, false, e.item);
            },
          });
        }
      }
    }

    item.extraMenuItems = menuItems;
  }

  public runCustomAction(
    listAction: ListAction,
    row: any,
    isSelfAction: boolean = false,
    menuItem?: MenuItem
  ): void {
    const confirmAction = (data: any) => {
      if (listAction.passEntityId) {
        let id;
        if (isSelfAction) {
          id = this.entityId;
        } else {
          if (listAction.idPropertyName) {
            id = row[listAction.idPropertyName];
          } else {
            id = row.id;
          }
        }

        if (listAction.idName) {
          data[listAction.idName] = id;
        } else {
          data['id'] = id;
        }
      }

      /*if (listAction.passItemSelection) {
        data[listAction.itemSelectionPropertyName] = this.selectedItemIds;
      }*/

      const segment = this.appService
        .getHomeDataSync()
        .segments.find((x) => x.general.id === listAction.segmentId);

      listAction.isSubmitting = true;
      if (menuItem) {
        menuItem.disabled = true;
      }

      this.appService
        .submitCustomAction(
          segment.navigation.controllerName,
          listAction.actionName,
          data
        )
        .pipe(
          finalize(() => {
            listAction.isSubmitting = false;
            if (menuItem) {
              menuItem.disabled = false;
            }
          })
        )
        .subscribe((response: ServerResponse<any>) => {
          if (!response.result) {
            this.messageService.add({
              severity: 'error',
              summary: this.translateService.instant('SHARED.Failed'),
              detail: response.error,
            });
          } else {
            if (listAction.reloadData) {
              this.loadData();
            }

            if (listAction.showSuccessMessage) {
              this.messageService.add({
                severity: 'success',
                icon: 'pi pi-verified',
                detail: listAction.successMessageTitle
                  ? listAction.successMessageTitle
                  : this.translateService.instant(
                      'SHARED.TheActionWasCompletedSuccessfully'
                    ),
              });
            }

            /*if (listAction.responseType === ActionResponseType.File) {
              this.fileSaverService.save()
            } else if (listAction.responseType === ActionResponseType.Redirect) {
              const redirectResponse = (response.data as ActionRedirectResponse);

              const tmpListAction: ListAction = {
                ...redirectResponse,
              };

              this.router.navigate(this.getRouterLink(tmpListAction, null, true))
            }*/
          }
        });
    };

    if (listAction.requireConfirmation) {
      if (!listAction.confirmationDialog) {
        listAction.confirmationDialog = new DialogStructure();
        listAction.confirmationDialog.title = this.translateService.instant(
          'LIST.CUSTOM_ACTION_DEFAULT_CONFIRMATION_TITLE'
        );
        listAction.confirmationDialog.description =
          this.translateService.instant(
            'LIST.CUSTOM_ACTION_DEFAULT_CONFIRMATION_DESCRIPTION'
          );
      }

      if (!listAction.confirmationDialog.fileUploadController) {
        // TODO
        //listAction.confirmationDialog.fileUploadController = this.segment.controllerName;
      }

      if (listAction.passModelToConfirmation) {
        listAction.confirmationDialog.model = row;
      } else {
        listAction.confirmationDialog.model = {};
      }

      const dialogRef = this.dialogService.open(DynamicConfirmModalComponent, {
        inputValues: {
          structure: listAction.confirmationDialog,
          listDef: this.listDef,
        },
      });

      dialogRef.onClose.subscribe((result: any) => {
        if (result.result) {
          confirmAction(result.data);
        }
      });
    } else {
      confirmAction({});
    }
  }

  // endregion

  /**
   * Handles the import template download
   */
  onImportTemplate(): void {
    if (!this.importTemplateFunc) {
      this.messageService.add({
        severity: 'error',
        summary: this.translateService.instant('SHARED.Error'),
        detail: this.translateService.instant(
          'SHARED.ImportTemplateFunctionalityNotAvailable'
        ),
      });
      return;
    }

    this.importTemplateFunc().subscribe({
      next: (response) => {
        // Get filename from content-disposition header or use default
        let filename = 'import-template.xlsx';
        const disposition = response.headers.get('content-disposition');
        if (disposition && disposition.indexOf('attachment') !== -1) {
          const filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
          const matches = filenameRegex.exec(disposition);
          if (matches != null && matches[1]) {
            filename = matches[1].replace(/['"]/g, '');
          }
        }

        this.fileSaverService.save(response.body, filename);

        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Template downloaded successfully',
        });
      },
      error: (error) => {
        console.error('Error downloading template:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to download import template',
        });
      },
    });
  }

  /**
   * Handles the import action
   */
  onImport(): void {
    if (!this.importDataFunc) {
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: 'Import functionality not available',
      });
      return;
    }

    this.importDataFunc();
  }

  public getCellFieldData(
    col: ListColumnInfo,
    item: any,
    rowIndex: number
  ): FieldData<any, FieldMetadata> {
    const itemValue = item[col.propertyName];
    const fieldType = col.type as FieldType; // col.type should align with FieldType string enum values

    const baseMetadata: FieldMetadata = {
      readOnly: true,
      inputOptionsKey: col.inputOptionsKey,
      // componentType: col.componentType, // If you have this and it's needed by components
    };

    let specificMetadata: FieldMetadata = { ...baseMetadata };

    // Example for Select/MultiSelect: Ensure options are correctly passed
    if (
      (fieldType === FieldType.SELECT || fieldType === FieldType.MULTISELECT) &&
      col.inputOptionsKey
    ) {
      const optionsKey = col.inputOptionsKey.toLowerCase();
      const allInputOptions = this.listDef?.inputOptions || {};
      const allInputOptionsLower = Object.fromEntries(
        Object.entries(allInputOptions).map(([key, value]) => [
          key.toLowerCase(),
          value,
        ])
      );
      const resolvedOptions =
        allInputOptionsLower[optionsKey] ||
        allInputOptionsLower[col.inputOptionsKey] ||
        [];

      // Ensure SelectMetadata is imported from 'src/app/interfaces/field-metadata'
      // For this example, I'll assume specificMetadata can hold 'options' directly
      // or you would cast to SelectMetadata if strictly typed here.
      specificMetadata = {
        ...baseMetadata,
        options: resolvedOptions.map((opt) => ({
          display: opt.display,
          value: opt.value,
        })),
        // If SelectMetadata is imported:
        // } as SelectMetadata;
      };
    }
    // Add other 'else if' blocks here if other field types from 'col'
    // need to pass specific properties into their metadata.

    return {
      id: `${col.propertyName}-${item.id || rowIndex}`,
      label: col.name,
      type: fieldType,
      value: itemValue,
      metadata: specificMetadata,
      path: `${col.propertyName}-${item.id || rowIndex}`,
    };
  }

  public confirmDeleteAll(event: Event): void {
    // Prevent event propagation
    event.stopPropagation();

    this.confirmationService.confirm({
      message: this.translateService.instant(
        'SHARED.DeleteAllItemsConfirmation'
      ),
      header: this.translateService.instant('SHARED.DangerZone'),
      icon: 'pi pi-exclamation-triangle', // More severe icon
      rejectLabel: this.translateService.instant('SHARED.Cancel'),
      rejectButtonProps: {
        label: this.translateService.instant('SHARED.Cancel'),
        severity: 'secondary',
        outlined: true,
      },
      acceptButtonProps: {
        label: this.translateService.instant('SHARED.DeleteAll'),
        severity: 'danger',
      },
      accept: () => {
        if (this.deleteAllFunc) {
          this.deleteAllFunc().subscribe((response) => {
            if (response.result) {
              this.messageService.add({
                severity: 'info',
                summary: this.translateService.instant('SHARED.Deleted'),
                detail: this.translateService.instant(
                  'SHARED.AllItemsSuccessfullyDeleted'
                ),
              });
              this.loadData(); // Refresh data
            } else {
              this.messageService.add({
                severity: 'error',
                summary: this.translateService.instant('SHARED.Failed'),
                detail:
                  response.error ||
                  this.translateService.instant(
                    'SHARED.FailedToDeleteAllItems'
                  ),
              });
            }
          });
        }
      },
      reject: () => {
        this.messageService.add({
          severity: 'error',
          summary: this.translateService.instant('SHARED.Rejected'),
          detail: this.translateService.instant(
            'SHARED.DeleteAllOperationCancelled'
          ),
        });
      },
    });
  }
}
