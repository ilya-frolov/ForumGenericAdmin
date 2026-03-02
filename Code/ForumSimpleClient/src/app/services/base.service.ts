import { inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ServerResponse } from '../models/server-response.model';
import { APP_SETTINGS } from './app-settings';

export abstract class BaseService {
  protected readonly http = inject(HttpClient);
  private readonly apiBaseUrl = APP_SETTINGS.apiBaseUrl;

  protected get<T>(url: string): Observable<ServerResponse<T>> {
    return this.http.get<ServerResponse<T>>(this.getServerUrl(url));
  }

  protected post<T>(url: string, body: unknown): Observable<ServerResponse<T>> {
    return this.http.post<ServerResponse<T>>(this.getServerUrl(url), body);
  }

  protected deleteWithBody<T>(url: string, body: unknown): Observable<ServerResponse<T>> {
    return this.http.delete<ServerResponse<T>>(this.getServerUrl(url), { body });
  }

  protected getServerUrl(url: string): string {
    return `${this.apiBaseUrl}/${url}`;
  }
}
