// API Response types
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
}

// Pool types
export interface CodeCountDto {
  totalCount: number;
  usedCount: number;
  unusedCount: number;
  errorCount: number;
}

export interface PoolInfoDto {
  id: number;
  poolName: string;
  poolDescription: string;
  poolCreateID: string;
  poolNote: string;
  poolCreatedBy: string;
  poolCreateDatetime: string;
  count?: CodeCountDto;
}

export interface PagedPoolListDto {
  items: PoolInfoDto[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPrevPage: boolean;
}

export interface CodeDto {
  id: number;
  poolCode: string;
  status: number;
  statusName: string;
  poolCodeUsedBatchID: string;
  poolCodeUsedDatetime: string;
  poolCodeNote: string;
  poolCodeCreateID: string;
  poolCodeCreatedBy: string;
  poolCodeCreateDatetime: string;
}

export interface PagedCodesDto {
  items: CodeDto[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPrevPage: boolean;
}

export interface AddCodesResultDto {
  totalCount: number;
  addedCount: number;
  duplicateCount: number;
  errorCount: number;
  errors: string[];
}

// Request types
export interface CreatePoolRequest {
  poolName: string;
  poolDescription?: string;
  createID?: string;
  note?: string;
  createdBy?: string;
}

export interface AddCodesRequest {
  mode: number;
  singleCode?: string;
  codes?: string[];
  createID?: string;
  createdBy?: string;
}

export interface UpdateStatusRequest {
  status: number;
}

// PO types
export interface POInfo {
  orderNo: string;
  gtin: string;
  orderQty: number;
  cartonCapacity: number;
  productionDate: string;
  shift: string;
  productName: string;
  productCode: string;
  lotNumber: string;
  site: string;
  factory: string;
  productionLine: string;
  customerOrderNo: string;
  uom: string;
  createdBy: string;
}

export interface POCodeInfo {
  code: string;
  status: number;
  cartonCode?: string;
  activateDatetime?: string;
  activateUser?: string;
}

export interface POCartonInfo {
  id: number;
  cartonCode: string;
  status: number;
  startTime?: string;
  completedTime?: string;
  user?: string;
  codeCount: number;
}

export interface PORecordInfo {
  id: number;
  code: string;
  status: number;
  cartonCode?: string;
  createDatetime: string;
  activateDatetime?: string;
  activateUser?: string;
}

// PO API Response
export interface POResult {
  success: boolean;
  message?: string;
  data?: unknown;
  count?: number;
}

// PO Request types
export interface LoadCodesRequest {
  gtin: string;
  qty?: number;
}

export interface ActivateCodeRequest {
  code: string;
  user?: string;
}

export interface CreateCartonRequest {
  user?: string;
}

export interface AddToCartonRequest {
  code: string;
  cartonCode: string;
}

// Code status enum
export enum CodeStatus {
  Unused = 0,
  Used = 1,
  Error = -1,
}

export const CodeStatusLabels: Record<number, string> = {
  [CodeStatus.Unused]: 'Chưa dùng',
  [CodeStatus.Used]: 'Đã dùng',
  [CodeStatus.Error]: 'Lỗi',
};

// Pool code status
export enum PoolCodeStatus {
  Available = 0,
  InUse = 1,
  Deleted = 2,
  Error = -1,
}
