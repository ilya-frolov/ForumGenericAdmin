import {Injectable, Renderer2, RendererFactory2} from '@angular/core';
import {TranslateService} from "@ngx-translate/core";
import {LanguageModel} from "../interfaces/language-models";
import {Subject} from "rxjs";
import { PrimeNG } from 'primeng/config';
import {InitData} from "../interfaces/models";

@Injectable({
  providedIn: 'root'
})
export class LangService {

  private readonly LOCAL_STORAGE_LANGUAGE_KEY = 'language';

  private allLanguages: LanguageModel[] = [{
    code: 'en',
    isRtl: false,
    name: 'English'
  }, {
    code: 'he',
    isRtl: true,
    name: 'עברית'
  }];

  public availableLanguages: LanguageModel[] = this.allLanguages;

  private selectedLanguage?: LanguageModel;
  private selectedLanguageChangedSubject: Subject<LanguageModel> = new Subject<LanguageModel>();
  public selectedLanguageChanged = this.selectedLanguageChangedSubject.asObservable();

  private renderer: Renderer2;
  private ltrStylesheet: string;
  private rtlStylesheet: string;

  constructor(private translate: TranslateService,
              private rendererFactory: RendererFactory2,
              private primeng: PrimeNG) {
    this.renderer = this.rendererFactory.createRenderer(null, null);

    // Load the dir styles
    document.querySelectorAll('link[rel=stylesheet]').forEach((value: any) => {
      const stylePath: string = value.attributes['href'].value;
      if (stylePath.includes('styles-ltr')) {
        this.ltrStylesheet = stylePath;
        value.remove();
      } else if (stylePath.includes('styles-rtl')) {
        this.rtlStylesheet = stylePath;
        value.remove();
      }
    });
  }

  public setAvailableLanguagesFromInitData(initData: InitData): void {
    this.availableLanguages = this.allLanguages.filter(lang => {
      if (lang.code === 'en') {
        return initData.allowEnglish;
      } else if (lang.code === 'he') {
        return initData.allowHebrew;
      }
      return true; // Keep other languages if added in the future
    });

    // If the currently selected language is no longer available, clear it
    if (this.selectedLanguage && !this.availableLanguages.some(lang => lang.code === this.selectedLanguage.code)) {
      this.selectedLanguage = undefined;
    }
  }

  public getDefaultLanguage(): LanguageModel {
    const browserLang = this.translate.getBrowserLang();
    let defaultLang = this.availableLanguages.find(x => x.code === browserLang);
    if (!defaultLang) {
      // If browser language is not available, prefer Hebrew if available, otherwise use first available
      defaultLang = this.availableLanguages.find(x => x.code === 'he') || this.availableLanguages[0];
    }

    return defaultLang;
  }

  public selectLanguage(langCode: string): void {
    if (this.selectedLanguage?.code !== langCode) {
      const language = this.availableLanguages.find(x => x.code === langCode);
      if (language) {
        this.translate.use(langCode);
        const html = document.querySelector('html');
        this.renderer.setAttribute(html, 'lang', langCode);

        // Dynamically set PrimeNG locale
        if (language.code === 'he') {
          import('../shared/locale/he').then(locale => {
            this.primeng.setTranslation(locale.hebrewLocale);
          });
        } else {
          this.primeng.setTranslation({}); // Clear or set to default English locale
        }

        const changeDir = this.selectedLanguage?.isRtl !== language.isRtl;

        this.selectedLanguage = language;
        // Save the selected language to localStorage for persistence across sessions
        localStorage.setItem(this.LOCAL_STORAGE_LANGUAGE_KEY, langCode);

        // Update the dir
        if (changeDir) {
          this.renderer.setAttribute(html, 'dir', language.isRtl ? 'rtl' : 'ltr');
          let dirStyle = document.querySelector('#dirStyle');
          if (!dirStyle) {
            dirStyle = document.createElement('link');
            dirStyle.setAttribute('id', 'dirStyle');
            dirStyle.setAttribute('rel', 'stylesheet');
            document.head.appendChild(dirStyle);
          }

          dirStyle.setAttribute('href', language.isRtl ? this.rtlStylesheet : this.ltrStylesheet);
        }

        this.selectedLanguageChangedSubject.next(language);
      }
    }
  }

  public getLanguage(): LanguageModel {
    if (!this.selectedLanguage) {
      // Try to load from storage
      let langCode = localStorage.getItem(this.LOCAL_STORAGE_LANGUAGE_KEY);
      const language = this.availableLanguages.find(x => x.code === langCode);

      if (!langCode || !language) {
        // If no language is saved in localStorage or the saved language is not valid,
        // use the default language from available languages
        if (this.availableLanguages.length > 0) {
          langCode = this.getDefaultLanguage().code;

          // Save the default language to localStorage for future sessions
          localStorage.setItem(this.LOCAL_STORAGE_LANGUAGE_KEY, langCode);
        } else {
          // If no languages are available, return null or handle error
          console.error('No languages are available based on init data configuration');
          return null;
        }
      }

      this.selectLanguage(langCode);
    }

    return this.selectedLanguage;
  }
}
