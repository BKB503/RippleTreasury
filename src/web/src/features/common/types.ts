export type Page<T> = {
  items: T[];
  pageNumber: number;
  pageSize: number;
  total: number;
};
