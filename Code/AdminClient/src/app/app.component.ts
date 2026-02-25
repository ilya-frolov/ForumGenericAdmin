import {Component, inject, OnInit} from '@angular/core';
import {LangService} from "./services/lang.service";
import {AppConfig} from "./services/app.config";
import {AppService} from "./services/app.service";
import {MessageService} from "primeng/api";

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    standalone: false
})
export class AppComponent implements OnInit {

  private appService: AppService = inject(AppService);
  private langService: LangService = inject(LangService);
  private messageService: MessageService = inject(MessageService);

  public isLoading: boolean = true;

  ngOnInit(): void {
    // Get the language from localStorage or use default if not available
    this.langService.getLanguage();

    // The getLanguage method already handles loading from localStorage
    // and falling back to default if needed, so we don't need to explicitly
    // call selectLanguage with the default language here

    this.appService.getInitData()
      .subscribe({
        next: (response) => {
          this.isLoading = false;
          // Apply language filtering based on init data
          this.langService.setAvailableLanguagesFromInitData(response);
        },
        error: (err) => {
          console.error('Error loading init data:', err);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to load init data. Please try again later.',
          });
        }
      });
  }
}
