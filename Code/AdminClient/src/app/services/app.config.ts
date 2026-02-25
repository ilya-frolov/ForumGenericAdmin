import { HttpClient } from '@angular/common/http';
import {Injectable} from '@angular/core';
import {IAppConfig} from '../interfaces/app-config.model';
import {TRANSLATIONS_SUPPORT} from "../app.module";

@Injectable()
export class AppConfig {

    static settings: IAppConfig;

    constructor(private http: HttpClient) {}

    load(): any {
        const jsonFile = `configs/config.json`;
        return new Promise<void>((resolve, reject) => {
            this.http.get(jsonFile).toPromise().then((response) => {
                AppConfig.settings = response as IAppConfig;

                AppConfig.settings.withTranslations = () => {
                  return TRANSLATIONS_SUPPORT;
                };
                resolve();
            }).catch((response: any) => {
                reject(`Could not load file '${jsonFile}': ${JSON.stringify(response)}`);
            });
        });
    }
}
