import {
  Component,
  OnInit,
  ViewChild,
  OnDestroy,
  AfterViewInit,
  SimpleChanges,
  OnChanges,
} from '@angular/core';
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
import {
  finalize,
  map,
  Observable,
  of,
  Subject,
  Subscription,
  takeUntil,
  from,
  switchMap,
  scheduled,
  asyncScheduler,
  firstValueFrom,
} from 'rxjs';
import {
  ListDef,
  TableDataRequest,
  ListResponseData,
} from '../../interfaces/table-models';
import { LangService } from '../../services/lang.service';
import { ListService } from 'src/app/services/list.service';
import { ServerResponse } from 'src/app/interfaces/server-response';
import { HttpResponse } from '@angular/common/http';
import { AdminSegment } from '../../interfaces/models';
import { AppService } from '../../services/app.service';
import { ActivatedRoute, Router } from '@angular/router';
import { DynamicFormDialogComponent } from 'src/app/components/dynamic-form-dialog/dynamic-form-dialog.component';
import { ImportExportService } from 'src/app/services/import-export.service';
import { ImportDialogComponent } from 'src/app/components/import-dialog/import-dialog.component';
import { FormBuilderService } from 'src/app/services/form-builder.service';
import { ExportFormat } from 'src/app/interfaces/export-moodel';

@Component({
  selector: 'admin-list',
  templateUrl: './list.component.html',
  standalone: false,
  providers: [ConfirmationService, MessageService],
})
export class ListComponent implements OnInit, OnDestroy {
  @ViewChild(DynamicFormDialogComponent) formDialog: DynamicFormDialogComponent;
  @ViewChild('importDialog') importDialog: ImportDialogComponent;

  private destroy$ = new Subject<void>();
  loading: boolean = true;
  segment: AdminSegment;
  refId?: string;

  listDef: ListDef | null = null;
  tableData: any[] = [];

  // Request parameters
  tableRequest: TableDataRequest = {
    showArchive: false,
    showDeleted: false,
    refId: null,
    filters: {
      pageIndex: 0,
      pageSize: 10,
      filter: '',
      sortColumns: [],
      advancedFilters: [],
    },
  };

  // Total records for pagination
  totalRecordsFetched: number = 0;
  totalRowCount: number = 0;

  exportOptions: MenuItem[] = [
    {
      label: 'Excel',
      icon: 'pi pi-file-excel',
      command: () => this.exportData(ExportFormat.Excel, this.tableRequest),
    },
    {
      label: 'PDF',
      icon: 'pi pi-file-pdf',
      command: () => this.exportData(ExportFormat.Pdf, this.tableRequest),
    },
    {
      label: 'CSV',
      icon: 'pi pi-file',
      command: () => this.exportData(ExportFormat.Csv, this.tableRequest),
    },
  ];

  private subsCleanup: Subscription[] = [];
  private formSubmitSubscription: Subscription;
  private formCancelSubscription: Subscription;

  constructor(
    private appService: AppService,
    private listService: ListService,
    private importExportService: ImportExportService,
    private messageService: MessageService,
    private route: ActivatedRoute,
    private formBuilderService: FormBuilderService
  ) {}

  async ngOnInit(): Promise<void> {
    this.subsCleanup.push(
      this.route.params.subscribe(async (params) => {
        this.refId = params['refId'];
        this.segment = await this.appService.getSegment(params['id']);
        this.tableRequest.refId = this.refId;
        // Load initial data
        this.loadTableDefinition();
      })
    );
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();

    for (const currSub of this.subsCleanup) {
      currSub.unsubscribe();
    }

    // Clean up form subscriptions
    if (this.formSubmitSubscription) {
      this.formSubmitSubscription.unsubscribe();
    }
    
    if (this.formCancelSubscription) {
      this.formCancelSubscription.unsubscribe();
    }
  }

  private loadTableDefinition(): void {
    this.loading = true;
    this.listService
      .getListDefinition(this.segment, this.refId)
      .pipe(
        finalize(() => {
          this.loading = false;
        })
      )
      .subscribe({
        next: (response) => {
          if (response.result) {
            this.listDef = response.data;
          } else {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: response.error || 'Failed to load table definition',
            });
          }
        },
        error: (error) => {
          console.error('Error loading table definition:', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to load table definition. Please try again later.',
          });
        },
      });
  }

  // Handle lazy loading event from PrimeNG table
  public lazyLoadData(data: TableDataRequest): Observable<any[]> {
    this.tableRequest = data;
    this.tableRequest.refId = this.refId;
    return this.loadData();
  }

  private loadData(): Observable<any[]> {
    if (!this.listDef) {
      return null;
    }

    return this.listService
      .getListData(this.segment.navigation.controllerName, this.tableRequest)
      .pipe(
        map((response: ServerResponse<ListResponseData>) => {
          if (response.result) {
            // Clear array while preserving reference
            this.tableData.length = 0;

            // Push all new items
            response.data.items.forEach((item) => this.tableData.push(item));

            // Update total records from recordsTotal in the response
            this.totalRecordsFetched =
              response.data.recordsFiltered || response.data.recordsTotal || 0;

            // Update total records in the table footer
            if (this.listDef?.columns) {
              this.totalRowCount = response.data.recordsTotal || 0;
            }

            return response.data.items;
          }

          throw response.error;
        })
      );
  }

  exportData(
    format: ExportFormat,
    data: TableDataRequest
  ): Observable<HttpResponse<Blob>> {
    // get the format name
    const formatName = ExportFormat[format];
    this.messageService.add({
      severity: 'info',
      summary: 'Export Requested',
      detail: `Exporting data to ${formatName}`,
    });

    return this.listService.exportData(this.segment, format, data).pipe(
      map((response: HttpResponse<Blob>) => {
        return response;
      })
    );
  }

  // Edit item handler
  // public editItem(item: any): Observable<boolean> {
  //   return scheduled(this.editItemAsync(item), asyncScheduler).pipe(
  //     switchMap((obs) => obs)
  //   );
  // }

  public editItem(item: any): Observable<any> {
    const shouldReloadSubject = new Subject<boolean>();
    const destroy$ = new Subject<void>();

    // Cleanup any existing form subscriptions
    if (this.formSubmitSubscription) {
      this.formSubmitSubscription.unsubscribe();
      this.formSubmitSubscription = null;
    }
    
    if (this.formCancelSubscription) {
      this.formCancelSubscription.unsubscribe();
      this.formCancelSubscription = null;
    }

    // Open dynamic form dialog in edit mode
    if (this.formDialog) {
      this.formBuilderService
        .buildFormAndStructure(
          this.segment.navigation.controllerName,
          this.refId,
          item.id
        )
        .subscribe((structure) => {
          this.formDialog.open();
          this.formDialog.dialogHeader = `Edit ${
            this.listDef?.title || 'Item'
          }`;
          this.formDialog.formId = item.id;
          this.formDialog.formMode = 'edit';
          this.formDialog.structure = structure;

          // Handle form submission
          this.formSubmitSubscription = this.formDialog.formSubmit
            .pipe(takeUntil(destroy$))
            .subscribe((res) => {
              this.appService
                .submitForm(
                  this.segment.navigation.controllerName,
                  res,
                  item.id
                )
                .pipe(takeUntil(destroy$))
                .subscribe((response) => {
                  if (response.result) {
                    this.messageService.add({
                      severity: 'success',
                      summary: 'Success',
                      detail: `Item updated successfully`,
                    });
                    shouldReloadSubject.next(true);
                    shouldReloadSubject.complete();
                    destroy$.next();
                    destroy$.complete();
                    this.formDialog.close();
                  } else {
                    this.messageService.add({
                      severity: 'error',
                      summary: 'Error',
                      detail: response.error || 'Failed to update item',
                    });
                  }
                });
            });

          // Handle form cancellation
          this.formCancelSubscription = this.formDialog.formCancel
            .pipe(takeUntil(destroy$))
            .subscribe(() => {
              shouldReloadSubject.next(false);
              shouldReloadSubject.complete();
              destroy$.next();
              destroy$.complete();
            });
        });
    } else {
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: 'Form dialog component not available',
      });
      shouldReloadSubject.next(false);
      shouldReloadSubject.complete();
    }

    return shouldReloadSubject.asObservable();
  }

  // New item handler
  public newItem(): Observable<boolean> {
    const shouldReloadSubject = new Subject<boolean>();
    const destroy$ = new Subject<void>();

    // Cleanup any existing form subscriptions
    if (this.formSubmitSubscription) {
      this.formSubmitSubscription.unsubscribe();
      this.formSubmitSubscription = null;
    }
    
    if (this.formCancelSubscription) {
      this.formCancelSubscription.unsubscribe();
      this.formCancelSubscription = null;
    }

    this.formBuilderService
      .buildFormAndStructure(
        this.segment.navigation.controllerName,
        this.refId,
        null
      )
      .subscribe((structure) => {
        // Open dynamic form dialog in create mode
        if (this.formDialog) {
          this.formDialog.open();
          this.formDialog.dialogHeader = `New ${
            this.listDef?.title || 'Item'
          }`;
          this.formDialog.formId = null;
          this.formDialog.formMode = 'edit';
          this.formDialog.structure = structure;

          // Handle form submission
          this.formSubmitSubscription = this.formDialog.formSubmit
            .pipe(takeUntil(destroy$))
            .subscribe((res) => {
              this.appService
                .submitForm(this.segment.navigation.controllerName, res, null)
                .pipe(takeUntil(destroy$))
                .subscribe((response) => {
                  if (response.result) {
                    this.messageService.add({
                      severity: 'success',
                      summary: 'Success',
                      detail: `Item created successfully`,
                    });
                    shouldReloadSubject.next(true);
                    shouldReloadSubject.complete();
                    destroy$.next();
                    destroy$.complete();
                    this.formDialog.close();
                  } else {
                    this.messageService.add({
                      severity: 'error',
                      summary: 'Error',
                      detail: response.error || 'Failed to create item',
                    });
                  }
                });
            });

          // Handle form cancellation
          this.formCancelSubscription = this.formDialog.formCancel
            .pipe(takeUntil(destroy$))
            .subscribe(() => {
              shouldReloadSubject.next(false);
              shouldReloadSubject.complete();
              destroy$.next();
              destroy$.complete();
            });
        } else {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Form dialog component not available',
          });
          shouldReloadSubject.next(false);
          shouldReloadSubject.complete();
        }
      }, error => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to build form structure',
        });
        shouldReloadSubject.next(false);
        shouldReloadSubject.complete();
      });

    return shouldReloadSubject.asObservable();
  }

  // View item handler
  public viewItem(item: any): Observable<boolean> {
    const viewCompleteSubject = new Subject<boolean>();
    const destroy$ = new Subject<void>();

    // Cleanup any existing form subscriptions
    if (this.formCancelSubscription) {
      this.formCancelSubscription.unsubscribe();
      this.formCancelSubscription = null;
    }

    this.formBuilderService
      .buildFormAndStructure(
        this.segment.navigation.controllerName,
        this.refId,
        item.id
      )
      .subscribe((structure) => {
        if (this.formDialog) {
          this.formDialog.open();
          this.formDialog.dialogHeader = `View ${this.listDef?.title || 'Item'}`;
          this.formDialog.formId = item.id;
          this.formDialog.formMode = 'view';
          this.formDialog.structure = structure;

          // Handle form cancellation (close button)
          this.formCancelSubscription = this.formDialog.formCancel
            .pipe(takeUntil(this.destroy$))
            .subscribe(() => {
              viewCompleteSubject.next(false);
              viewCompleteSubject.complete();
              destroy$.next();
              destroy$.complete();
            });

          // Add subscription to component's destroy$ for cleanup
          destroy$.pipe(takeUntil(this.destroy$)).subscribe();
        } else {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Form dialog component not available',
          });
          viewCompleteSubject.next(false);
          viewCompleteSubject.complete();
        }
      }, error => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to build form structure',
        });
        viewCompleteSubject.next(false);
        viewCompleteSubject.complete();
      });

    return viewCompleteSubject.asObservable();
  }

  // Handle the row delete event from dynamic table component
  public deleteItem(item: any): Observable<ServerResponse<any>> {
    this.messageService.add({
      severity: 'info',
      summary: 'Export Requested',
      detail: `Deleting item ID: ${item.id}`,
    });
    return this.listService.deleteItem(this.segment, item.id);
  }

  // Handle the delete all event
  public deleteAllItems(): Observable<ServerResponse<any>> {
    this.messageService.add({
      severity: 'info',
      summary: 'Delete All Requested',
      detail: `Deleting all items for this list.`,
    });
    // Assuming refId might be needed to scope the deletion, or it might be global for the segment
    return this.listService.deleteAllItems(this.segment, this.refId);
  }

  public saveReordering(items: any[]): Observable<ServerResponse<any>> {
    // Extract IDs from items
    const itemIds = items.map((entity) => entity.id);

    // Call service to save the new order
    return this.listService.saveReorder(this.segment, itemIds);
  }

  // Handle the archive event from dynamic table component
  public archiveItem(item: any): Observable<ServerResponse<any>> {
    this.messageService.add({
      severity: 'info',
      summary: 'Archive Requested',
      detail: `Archiving item ID: ${item.id}`,
    });
    return this.listService.archiveItem(this.segment, item.id);
  }

  /**
   * Opens the import dialog
   */
  public openImportDialog(): void {
    if (this.importDialog) {
      this.importDialog.open();
    }
  }

  /**
   * Handle import completion event
   */
  public onImportComplete(success: boolean): void {
    if (success) {
      // Reload data on successful import
      this.loadData().subscribe();
    }
  }

  /**
   * Downloads an import template for the current entity
   */
  public downloadImportTemplate(): Observable<HttpResponse<Blob>> {
    this.messageService.add({
      severity: 'info',
      summary: 'Template Requested',
      detail: 'Generating import template...',
    });

    return this.importExportService.downloadImportTemplate(this.segment);
  }
}
