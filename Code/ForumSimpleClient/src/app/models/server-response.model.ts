export interface ServerResponse<T> {
  result: boolean;
  error: string | null;
  data: T;
}
