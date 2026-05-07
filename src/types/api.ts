export interface ApiPagination {
  page: number;
  per_page: number;
  total: number;
  total_pages: number;
}

export interface ApiError {
  code: string;
  message: string;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  pagination?: ApiPagination;
  error?: ApiError;
}

export interface PagedResult<T> {
  items: T[];
  pagination: ApiPagination;
}
