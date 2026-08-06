import api from './api';
import type {
  ApiResponse,
  POInfo,
  POResult,
  PORecordInfo,
  LoadCodesRequest,
  ActivateCodeRequest,
  CreateCartonRequest,
  AddToCartonRequest,
} from '@/types';

// Get all PO list
export const getPOList = async (): Promise<POInfo[]> => {
  const response = await api.get<ApiResponse<POInfo[]>>('/production/polist');
  return response.data.data || [];
};

// Get PO details by orderNo
export const getPOInfo = async (orderNo: string): Promise<POInfo> => {
  const response = await api.get<ApiResponse<POInfo>>(`/production/${encodeURIComponent(orderNo)}`);
  return response.data.data!;
};

// Create new PO
export const createPO = async (poInfo: POInfo): Promise<POResult> => {
  const response = await api.post<POResult>('/production', poInfo);
  return response.data;
};

// Load codes from pool (GTIN)
export const loadCodesFromGTIN = async (orderNo: string, request: LoadCodesRequest): Promise<POResult> => {
  const response = await api.post<POResult>(
    `/production/${encodeURIComponent(orderNo)}/loadcodes`,
    request
  );
  return response.data;
};

// Get next available code
export const getNextCode = async (orderNo: string): Promise<{ code: string }> => {
  const response = await api.get<ApiResponse<{ code: string }>>(
    `/production/${encodeURIComponent(orderNo)}/nextcode`
  );
  return response.data.data!;
};

// Activate a code
export const activateCode = async (orderNo: string, request: ActivateCodeRequest): Promise<POResult> => {
  const response = await api.post<POResult>(
    `/production/${encodeURIComponent(orderNo)}/activate`,
    request
  );
  return response.data;
};

// Update code status
export const updateCodeStatus = async (
  orderNo: string,
  code: string,
  status: number
): Promise<POResult> => {
  const response = await api.put<POResult>(
    `/production/${encodeURIComponent(orderNo)}/code/${encodeURIComponent(code)}/status`,
    { status }
  );
  return response.data;
};

// Create carton
export const createCarton = async (orderNo: string, request: CreateCartonRequest = {}): Promise<POResult> => {
  const response = await api.post<POResult>(
    `/production/${encodeURIComponent(orderNo)}/carton`,
    request
  );
  return response.data;
};

// Add product to carton
export const addToCarton = async (
  orderNo: string,
  request: AddToCartonRequest
): Promise<POResult> => {
  const response = await api.post<POResult>(
    `/production/${encodeURIComponent(orderNo)}/carton/add`,
    request
  );
  return response.data;
};

// Get records with pagination
export const getRecords = async (
  orderNo: string,
  pageIndex = 1,
  pageSize = 100
): Promise<{ items: PORecordInfo[]; totalCount: number }> => {
  const response = await api.get<ApiResponse<{ items: PORecordInfo[]; totalCount: number }>>(
    `/production/${encodeURIComponent(orderNo)}/records`,
    { params: { pageIndex, pageSize } }
  );
  return response.data.data!;
};
