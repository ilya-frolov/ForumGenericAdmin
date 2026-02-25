
export interface IAppConfig {
    envName: string;
    isDebug: boolean;
    defaultLanguage: string;
    apiServer: string;
    withTranslations(): boolean;
}
