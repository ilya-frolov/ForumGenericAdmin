import { NgModule } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { ToastModule } from 'primeng/toast';
import { MenuModule } from 'primeng/menu';
import { DialogModule } from 'primeng/dialog';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { CardModule } from 'primeng/card';
import { MessageService } from 'primeng/api';
import { ConfirmationService } from 'primeng/api';
import { MegaMenuModule } from 'primeng/megamenu';
import { AvatarModule } from 'primeng/avatar';
import { DropdownModule } from 'primeng/dropdown';
import { MenubarModule } from 'primeng/menubar';
import { TextareaModule } from 'primeng/textarea';
import { CalendarModule } from 'primeng/calendar';
import { DatePickerModule } from 'primeng/datepicker';
import { BadgeModule } from 'primeng/badge';
import { CheckboxModule } from 'primeng/checkbox';
import { RadioButtonModule } from 'primeng/radiobutton';
import { InputSwitchModule } from 'primeng/inputswitch';
import { DividerModule } from 'primeng/divider';
import { PanelModule } from 'primeng/panel';
import { InputNumberModule } from 'primeng/inputnumber';
import { ColorPickerModule } from 'primeng/colorpicker';
import { SliderModule } from 'primeng/slider';
import { MultiSelectModule } from 'primeng/multiselect';
import { ListboxModule } from 'primeng/listbox';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { FileUploadModule } from 'primeng/fileupload';
import { RatingModule } from 'primeng/rating';
import { KnobModule } from 'primeng/knob';
import { DrawerModule } from 'primeng/drawer';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { TieredMenuModule } from 'primeng/tieredmenu';
import { SplitButtonModule } from 'primeng/splitbutton';
import { SelectModule } from 'primeng/select';
import { AccordionModule } from 'primeng/accordion';
import { TabsModule } from 'primeng/tabs';
import { ImageModule } from 'primeng/image';
import { BlockUIModule } from 'primeng/blockui';
import { BreadcrumbModule } from 'primeng/breadcrumb';
import { FloatLabelModule } from 'primeng/floatlabel';
import { MessageModule } from 'primeng/message';

@NgModule({
  exports: [
    ButtonModule,
    InputTextModule,
    TableModule,
    ToastModule,
    MenuModule,
    DialogModule,
    ConfirmDialogModule,
    CardModule,
    MegaMenuModule,
    AvatarModule,
    DropdownModule,
    MenubarModule,
    TextareaModule,
    CalendarModule,
    DatePickerModule,
    BadgeModule,
    CheckboxModule,
    RadioButtonModule,
    InputSwitchModule,
    DividerModule,
    PanelModule,
    InputNumberModule,
    ColorPickerModule,
    SliderModule,
    MultiSelectModule,
    ListboxModule,
    TagModule,
    TooltipModule,
    FileUploadModule,
    RatingModule,
    KnobModule,
    DrawerModule,
    IconFieldModule,
    InputIconModule,
    TieredMenuModule,
    SplitButtonModule,
    SelectModule,
    AccordionModule,
    TabsModule,
    ImageModule,
    BlockUIModule,
    BreadcrumbModule,
    FloatLabelModule,
    MessageModule
  ],
  providers: [
    MessageService,
    ConfirmationService
  ]
})
export class PrimeModule { } 