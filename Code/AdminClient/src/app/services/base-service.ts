import {HttpClient, HttpParams, HttpResponse} from "@angular/common/http";
import {ActivatedRoute, Router} from "@angular/router";
import {AppConfig} from "./app.config";
import {Observable} from "rxjs";
import {ServerResponse} from "../interfaces/server-response";
import {EnvironmentInjector, inject, runInInjectionContext} from "@angular/core";

export abstract class BaseService {

  protected http: HttpClient = inject(HttpClient);
  protected router: Router = inject(Router);
  private environmentInjector = inject(EnvironmentInjector);

  // **************************************************************** //
  // ************************** Helpers ***************************** //
  // **************************************************************** //
  public getServerUrl(url: string): string {
    return AppConfig.settings.apiServer + '/' + url;
  }

  protected get<T>(url: string, queryParams?: HttpParams): Observable<ServerResponse<T>> {
    return this.getClean<ServerResponse<T>>(url, queryParams);
  }

  protected getClean<T>(url: string, queryParams?: HttpParams): Observable<T> {
    queryParams = this.addAllQueryParamsToRequest(queryParams);
    this.runBeforeAction(url, null, queryParams);

    return this.http.get<T>(this.getServerUrl(url), {
      withCredentials: true,
      params: queryParams
    });
  }

  protected post<T>(url: string, data: any, queryParams?: HttpParams): Observable<ServerResponse<T>> {
    return this.postClean<ServerResponse<T>>(url, data, queryParams);
  }

  protected postClean<T>(url: string, data: any, queryParams?: HttpParams): Observable<T> {
    queryParams = this.addAllQueryParamsToRequest(queryParams);
    this.runBeforeAction(url, data, queryParams);

    return this.http.post<T>(this.getServerUrl(url), data, {
      withCredentials: true,
      params: queryParams
    });
  }

  protected delete<T>(url: string, queryParams?: HttpParams): Observable<ServerResponse<T>> {
    return this.deleteClean<ServerResponse<T>>(url, queryParams);
  }

  protected deleteClean<T>(url: string, queryParams?: HttpParams): Observable<T> {
    queryParams = this.addAllQueryParamsToRequest(queryParams);
    this.runBeforeAction(url, null, queryParams);

    return this.http.delete<T>(this.getServerUrl(url), {
      withCredentials: true,
      params: queryParams
    });
  }

  protected postBlob(url: string, data: any, queryParams?: HttpParams): Observable<HttpResponse<Blob>> {
    queryParams = this.addAllQueryParamsToRequest(queryParams);
    this.runBeforeAction(url, data, queryParams);

    return this.http.post(this.getServerUrl(url), data, {
      withCredentials: true,
      responseType: 'blob',
      observe: 'response',
      params: queryParams
    });
  }

  protected getBlob(url: string, queryParams?: HttpParams): Observable<HttpResponse<Blob>> {
    queryParams = this.addAllQueryParamsToRequest(queryParams);
    this.runBeforeAction(url, null, queryParams);

    return this.http.get(this.getServerUrl(url), {
      withCredentials: true,
      responseType: 'blob',
      observe: 'response',
      params: queryParams
    });
  }

  protected postFormData<T>(url: string, formData: FormData, queryParams?: HttpParams): Observable<T> {
    queryParams = this.addAllQueryParamsToRequest(queryParams);
    this.runBeforeAction(url, formData, queryParams);

    return this.http.post<T>(this.getServerUrl(url), formData, {
      withCredentials: true,
      params: queryParams
    });
  }

  public getCurrentLang(): string{
    return 'he';
  }

  private addAllQueryParamsToRequest(queryParams?: HttpParams): HttpParams {
    if (!queryParams) {
      queryParams = new HttpParams();
    }

    let activatedRoute: ActivatedRoute;
    runInInjectionContext(this.environmentInjector, () => {
      activatedRoute = inject(ActivatedRoute);
    });

    // Get all current router params from the activated route
    const currentRouteParams = activatedRoute.snapshot.queryParams;

    // Add all current URL params to the request params
    Object.entries(currentRouteParams).forEach(([key, value]) => {
      if ((value !== null) && (value !== undefined) && !queryParams.has(key)) {
        queryParams = queryParams.set(key, value.toString());
      }
    });

    return queryParams;
  }

  protected runBeforeAction(url: string, data?: any, queryParams?: HttpParams): void {
    // For Overrides
  }
}
