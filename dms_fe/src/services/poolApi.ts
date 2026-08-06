import api from './api';
import type {
  ApiResponse,
  PagedPoolListDto,
  PoolInfoDto,
  PagedCodesDto,
  CodeCountDto,
  CodeDto,
  AddCodesResultDto,
  CreatePoolRequest,
  AddCodesRequest,
  UpdateStatusRequest,
} from '@/types';

// Get all pools with pagination
export const getPools = async (pageIndex = 1, pageSize = 100): Promise<PagedPoolListDto> => {
  const response = await api.get<ApiResponse<PagedPoolListDto>>('/datapool/pools', {
    params: { pageIndex, pageSize },
  });
  return response.data.data!;
};

// Get pool info by name
export const getPoolInfo = async (poolName: string): Promise<PoolInfoDto> => {
  const response = await api.get<ApiResponse<PoolInfoDto>>(`/datapool/pools/${encodeURIComponent(poolName)}`);
  return response.data.data!;
};

// Get pool path
export const getPoolPath = async (poolName: string): Promise<string> => {
  const response = await api.get<ApiResponse<{ Path: string }>>(`/datapool/pools/${encodeURIComponent(poolName)}/path`);
  return response.data.data!.Path;
};

// Create a new pool
export const createPool = async (request: CreatePoolRequest): Promise<void> => {
  await api.post('/datapool/pools', request);
};

// Get codes from a pool with pagination and filters
export const getCodes = async (
  poolName: string,
  options: {
    pageIndex?: number;
    pageSize?: number;
    status?: number;
    batchID?: string;
    createID?: string;
    createdBy?: string;
    fromCreateDate?: string;
    toCreateDate?: string;
    fromUsedDate?: string;
    toUsedDate?: string;
  } = {}
): Promise<PagedCodesDto> => {
  const { pageIndex = 1, pageSize = 100, ...filters } = options;
  const response = await api.get<ApiResponse<PagedCodesDto>>(
    `/datapool/pools/${encodeURIComponent(poolName)}/codes`,
    { params: { pageIndex, pageSize, ...filters } }
  );
  return response.data.data!;
};

// Get code counts for a pool
export const getCodeCounts = async (poolName: string): Promise<CodeCountDto> => {
  const response = await api.get<ApiResponse<CodeCountDto>>(
    `/datapool/pools/${encodeURIComponent(poolName)}/codes/counts`
  );
  return response.data.data!;
};

// Get a specific code
export const getCode = async (poolName: string, code: string): Promise<CodeDto[]> => {
  const response = await api.get<ApiResponse<CodeDto[]>>(
    `/datapool/pools/${encodeURIComponent(poolName)}/codes/${encodeURIComponent(code)}`
  );
  return response.data.data!;
};

// Add codes to pool (mode 0 = single, mode 1 = batch)
export const addCodes = async (
  poolName: string,
  request: AddCodesRequest
): Promise<AddCodesResultDto> => {
  const response = await api.post<ApiResponse<AddCodesResultDto>>(
    `/datapool/pools/${encodeURIComponent(poolName)}/codes`,
    request
  );
  return response.data.data!;
};

// Update code status
export const updateCodeStatus = async (
  poolName: string,
  code: string,
  status: number
): Promise<void> => {
  const request: UpdateStatusRequest = { status };
  await api.patch(
    `/datapool/pools/${encodeURIComponent(poolName)}/codes/${encodeURIComponent(code)}/status`,
    request
  );
};
