import { Component, OnInit } from '@angular/core';
import { AppService } from "../../services/app.service";
import {AdminHomeData, ConnectedUser, IconType, InitData} from "../../interfaces/models";
import { LangService } from "../../services/lang.service";
import { LanguageModel } from "../../interfaces/language-models";
import {MenuItem, MessageService} from 'primeng/api';
import { ThemeService } from '../../services/theme.service';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LoginService } from '../../services/login.service';
import { finalize } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-app-layout',
  templateUrl: './app-layout.component.html',
  standalone: false,
})
export class AppLayoutComponent implements OnInit {
  protected readonly IconType = IconType;

  public initData: InitData;
  public homeData: AdminHomeData;

  public isLoading = true;
  public connectedUser?: ConnectedUser;
  items: MenuItem[] = [];
  isDarkMode = false;

  public selectedLanguage!: LanguageModel;
  public availableLanguages!: LanguageModel[];
  public isLanguageDropdownDisabled: boolean = false;
  
  // Change password dialog
  showChangePasswordDialog = false;
  changePasswordForm: FormGroup;
  submitted = false;
  loading = false;

  constructor(
    private appService: AppService,
    private langService: LangService,
    private themeService: ThemeService,
    private messageService: MessageService,
    private formBuilder: FormBuilder,
    private loginService: LoginService,
    private translateService: TranslateService
  ) {
  }

  ngOnInit(): void {
    this.initData = this.appService.getInitDataSync();
    this.appService.getHomeData()
      .subscribe({
        next: (response) => {
          this.homeData = response;

          this.initMenuItems();

          this.isLoading = false;
        },
        error: (err) => {
          console.error('Error loading home data:', err);
          this.messageService.add({
            severity: 'error',
            summary: this.translateService.instant('SHARED.Error'),
            detail: this.translateService.instant('SHARED.FailedToLoadHomeData'),
          });
        }
      });

    this.connectedUser = this.loginService.getCurrentUser()


    // Get languages.
    this.availableLanguages = this.langService.availableLanguages;
    // Disable dropdown if only one language is available
    this.isLanguageDropdownDisabled = this.availableLanguages.length <= 1;
    // Get the selected language from localStorage or use default if not available
    this.selectedLanguage = this.langService.getLanguage();

    // Subscribe to theme changes
    this.themeService.darkMode$.subscribe(isDark => {
      this.isDarkMode = isDark;
    });
    
    // Initialize change password form
    this.initChangePasswordForm();
  }
  
  private initChangePasswordForm(): void {
    this.changePasswordForm = this.formBuilder.group({
      currentPassword: ['', [Validators.required, Validators.minLength(6)]],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    }, {
      validator: this.passwordMatchValidator
    });
  }
  
  get cpf() {
    return this.changePasswordForm.controls;
  }

  private initMenuItems(): void {
    this.items = [{
      separator: true
    }];

    // Sort segments by priority and create menu items with custom headers
    if (this.homeData?.segments) {
      const sortedSegments = [...this.homeData.segments].sort((a, b) => a.general.priority - b.general.priority);
      const filteredSegments = sortedSegments.filter(segment => segment.ui.showInMenu);

      // Group segments by MenuHeader
      const menuGroups: { [header: string]: typeof filteredSegments } = {};
      let currentHeader = 'SHARED.Admin'; // Default header

      for (const segment of filteredSegments) {
        if (segment.general.menuHeader) {
          currentHeader = segment.general.menuHeader;
        }

        if (!menuGroups[currentHeader]) {
          menuGroups[currentHeader] = [];
        }
        menuGroups[currentHeader].push(segment);
      }

      // Create menu items for each group
      Object.keys(menuGroups).forEach(headerKey => {
        const groupSegments = menuGroups[headerKey];
        const groupItems = groupSegments.map(segment => ({
          label: segment.general.name,
          iconType: segment.ui.iconType,
          icon: segment.ui.icon,
          routerLink: [segment.navigation.customPath || `/${segment.general.id}`]
        }));

        if (groupItems.length > 0) {
          const headerLabel = headerKey.startsWith('SHARED.') ?
            this.translateService.instant(headerKey) : headerKey;

          this.items.push({
            label: headerLabel,
            items: groupItems
          });
        }
      });
    }

    // Add settings section
    if (this.homeData?.settings) {
      const settingItems = this.homeData.settings // Use the original array
        .map(setting => ({
          label: setting.name,
          icon: 'cog', // Default to cog icon
          routerLink: [`/settings/${setting.id}`]
        }));

      if (settingItems.length > 0) {
        this.items.push({
          separator: true // Add separator before settings
        }, {
          label: this.translateService.instant('SHARED.Settings'),
          icon: 'pi pi-cog', // Add cog icon to the section label
          items: settingItems
        });
      }
    }

    // this.items.push({
    //   separator: true
    // }, {
    //   label: 'Other',
    //   items: [{
    //     label: 'Examples',
    //     iconType: IconType.PrimeIcons,
    //     icon: 'book',
    //     routerLink: ['/examples'],
    //   }]
    // });
  }

  public selectLanguage(lang: LanguageModel): void {
    this.selectedLanguage = lang;
    this.langService.selectLanguage(this.selectedLanguage.code);
  }

  public toggleTheme(): void {
    this.themeService.toggleDarkMode();
  }
  
  public logout(): void {
    this.loginService.logout();
  }
  
  openChangePasswordDialog(): void {
    this.submitted = false;
    this.changePasswordForm.reset();
    this.showChangePasswordDialog = true;
  }
  
  closeChangePasswordDialog(): void {
    this.showChangePasswordDialog = false;
  }
  
  onSubmitChangePassword(): void {
    this.submitted = true;

    // Stop here if form is invalid
    if (this.changePasswordForm.invalid) {
      return;
    }

    this.loading = true;
    const currentPassword = this.cpf['currentPassword'].value;
    const newPassword = this.cpf['newPassword'].value;

    this.loginService
      .changePassword(currentPassword, newPassword)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (errorMessage) => {
          if (errorMessage === null) {
            // Success
            this.messageService.add({
              severity: 'success',
              summary: this.translateService.instant('SHARED.Success'),
              detail: this.translateService.instant('SHARED.PasswordChangedSuccessfully'),
            });
            this.closeChangePasswordDialog();
          } else {
            // Show error message
            this.messageService.add({
              severity: 'error',
              summary: this.translateService.instant('SHARED.Error'),
              detail: errorMessage,
            });
          }
        },
      });
  }
  
  passwordMatchValidator(formGroup: FormGroup): { [key: string]: boolean } | null {
    const password = formGroup.get('newPassword')?.value;
    const confirmPassword = formGroup.get('confirmPassword')?.value;

    if (password !== confirmPassword) {
      formGroup.get('confirmPassword')?.setErrors({ passwordMismatch: true });
    } else {
      // Only clear the passwordMismatch error
      const confirmErrors = formGroup.get('confirmPassword')?.errors;
      if (
        confirmErrors &&
        Object.keys(confirmErrors).length === 1 &&
        confirmErrors['passwordMismatch']
      ) {
        formGroup.get('confirmPassword')?.setErrors(null);
      }
    }
    
    return null;
  }
}
