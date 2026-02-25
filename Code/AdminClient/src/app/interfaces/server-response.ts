export interface ServerResponse<T> {
    result: boolean;
    data: T;
    error?: string;
}
