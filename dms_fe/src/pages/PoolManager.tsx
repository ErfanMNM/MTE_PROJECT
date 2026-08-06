import { useState, useEffect, useCallback } from 'react';
import {
  Database,
  Plus,
  ChevronDown,
  ChevronRight,
  Search,
  Upload,
  RefreshCw,
  Trash2,
  Package,
  AlertCircle,
} from 'lucide-react';
import { Card, CardContent, Button, Modal, Input, Select, Badge, ProgressBar, StatBox, Spinner, PageLoader } from '@/components/ui';
import { useAppStore } from '@/stores/useAppStore';
import { getPools, createPool, getCodes, getCodeCounts } from '@/services/poolApi';
import type { PoolInfoDto, CodeDto, CodeCountDto } from '@/types';

export function PoolManager() {
  const { addToast } = useAppStore();
  const [pools, setPools] = useState<PoolInfoDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [expandedPool, setExpandedPool] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  
  // Modal states
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showAddCodeModal, setShowAddCodeModal] = useState(false);
  const [showImportModal, setShowImportModal] = useState(false);
  const [selectedPool, setSelectedPool] = useState<string | null>(null);
  
  // Code search
  const [codeSearchTerm, setCodeSearchTerm] = useState('');
  const [poolCodes, setPoolCodes] = useState<CodeDto[]>([]);
  const [codeCounts, setCodeCounts] = useState<Record<string, CodeCountDto>>({});
  const [loadingCodes, setLoadingCodes] = useState(false);

  const fetchPools = useCallback(async () => {
    try {
      setLoading(true);
      const data = await getPools();
      setPools(data.items);
      
      // Fetch counts for each pool
      const counts: Record<string, CodeCountDto> = {};
      for (const pool of data.items) {
        try {
          counts[pool.poolName] = await getCodeCounts(pool.poolName);
        } catch {
          // Ignore errors for individual pool counts
        }
      }
      setCodeCounts(counts);
    } catch (error) {
      addToast('error', 'Failed to load pools');
      console.error(error);
    } finally {
      setLoading(false);
    }
  }, [addToast]);

  useEffect(() => {
    fetchPools();
  }, [fetchPools]);

  const toggleExpand = async (poolName: string) => {
    if (expandedPool === poolName) {
      setExpandedPool(null);
    } else {
      setExpandedPool(poolName);
      if (!poolCodes.length || poolCodes[0]?.poolCode !== poolName) {
        setLoadingCodes(true);
        try {
          const data = await getCodes(poolName, { pageSize: 100 });
          setPoolCodes(data.items);
        } catch (error) {
          addToast('error', 'Failed to load codes');
        } finally {
          setLoadingCodes(false);
        }
      }
    }
  };

  const filteredPools = pools.filter(pool =>
    pool.poolName.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const getAvailabilityBadge = (count: CodeCountDto) => {
    if (!count) return <Badge variant="default">N/A</Badge>;
    const percentage = count.totalCount > 0 ? (count.unusedCount / count.totalCount) * 100 : 0;
    if (percentage > 50) return <Badge variant="success">Tốt</Badge>;
    if (percentage > 20) return <Badge variant="warning">Còn ít</Badge>;
    return <Badge variant="danger">Cạn kiệt</Badge>;
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Data Pool Manager</h1>
          <p className="text-slate-500 dark:text-slate-400 mt-1">Quản lý pools và codes</p>
        </div>
        <div className="flex gap-2">
          <Button variant="secondary" leftIcon={<RefreshCw className="w-4 h-4" />} onClick={fetchPools}>
            Refresh
          </Button>
          <Button leftIcon={<Plus className="w-4 h-4" />} onClick={() => setShowCreateModal(true)}>
            Tạo Pool
          </Button>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <StatBox
          label="Tổng Pools"
          value={pools.length}
          icon={<Database className="w-5 h-5" />}
          variant="primary"
        />
        <StatBox
          label="Tổng Codes"
          value={Object.values(codeCounts).reduce((sum, c) => sum + (c?.totalCount || 0), 0)}
          icon={<Package className="w-5 h-5" />}
          variant="default"
        />
        <StatBox
          label="Codes Còn"
          value={Object.values(codeCounts).reduce((sum, c) => sum + (c?.unusedCount || 0), 0)}
          icon={<AlertCircle className="w-5 h-5" />}
          variant="success"
        />
        <StatBox
          label="Codes Đã Dùng"
          value={Object.values(codeCounts).reduce((sum, c) => sum + (c?.usedCount || 0), 0)}
          icon={<Trash2 className="w-5 h-5" />}
          variant="warning"
        />
      </div>

      {/* Search */}
      <Card>
        <CardContent className="py-4">
          <Input
            placeholder="Tìm kiếm pool..."
            leftIcon={<Search className="w-4 h-4" />}
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </CardContent>
      </Card>

      {/* Pool List */}
      {loading ? (
        <PageLoader />
      ) : (
        <Card>
          <div className="divide-y divide-slate-200 dark:divide-slate-700">
            {filteredPools.map((pool) => {
              const counts = codeCounts[pool.poolName];
              const isExpanded = expandedPool === pool.poolName;
              
              return (
                <div key={pool.poolName} className="divide-y divide-slate-200 dark:divide-slate-700">
                  {/* Pool Row */}
                  <div
                    className="p-4 hover:bg-slate-50 dark:hover:bg-slate-800/50 cursor-pointer transition-colors"
                    onClick={() => toggleExpand(pool.poolName)}
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3 min-w-0">
                        {isExpanded ? (
                          <ChevronDown className="w-5 h-5 text-slate-400 flex-shrink-0" />
                        ) : (
                          <ChevronRight className="w-5 h-5 text-slate-400 flex-shrink-0" />
                        )}
                        <div className="min-w-0">
                          <p className="font-mono font-semibold text-slate-900 dark:text-slate-100 truncate">
                            {pool.poolName}
                          </p>
                          <p className="text-xs text-slate-500 dark:text-slate-400 truncate">
                            {pool.poolCreatedBy} - {pool.poolCreateDatetime}
                          </p>
                        </div>
                      </div>
                      
                      <div className="flex items-center gap-6">
                        {/* Stats */}
                        <div className="hidden sm:flex items-center gap-4 text-sm">
                          <div className="text-center">
                            <p className="font-semibold text-success">{counts?.unusedCount || 0}</p>
                            <p className="text-xs text-slate-500">Còn</p>
                          </div>
                          <div className="text-center">
                            <p className="font-semibold text-info">{counts?.usedCount || 0}</p>
                            <p className="text-xs text-slate-500">Đã dùng</p>
                          </div>
                          <div className="text-center">
                            <p className="font-semibold text-slate-700 dark:text-slate-300">{counts?.totalCount || 0}</p>
                            <p className="text-xs text-slate-500">Tổng</p>
                          </div>
                        </div>
                        
                        {/* Progress */}
                        <div className="hidden md:block w-24">
                          <ProgressBar
                            value={counts?.unusedCount || 0}
                            max={counts?.totalCount || 1}
                            variant={
                              ((counts?.unusedCount || 0) / (counts?.totalCount || 1)) > 0.5
                                ? 'success'
                                : ((counts?.unusedCount || 0) / (counts?.totalCount || 1)) > 0.2
                                ? 'warning'
                                : 'danger'
                            }
                            size="sm"
                          />
                        </div>
                        
                        {/* Badge */}
                        {getAvailabilityBadge(counts)}
                        
                        {/* Actions */}
                        <div className="flex gap-1" onClick={(e) => e.stopPropagation()}>
                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() => {
                              setSelectedPool(pool.poolName);
                              setShowAddCodeModal(true);
                            }}
                          >
                            <Plus className="w-4 h-4" />
                          </Button>
                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() => {
                              setSelectedPool(pool.poolName);
                              setShowImportModal(true);
                            }}
                          >
                            <Upload className="w-4 h-4" />
                          </Button>
                        </div>
                      </div>
                    </div>
                    
                    {/* Expanded Code Search */}
                    {isExpanded && (
                      <div className="mt-4 pt-4 border-t border-slate-200 dark:border-slate-700">
                        <div className="flex gap-2 mb-4">
                          <Input
                            placeholder="Tìm kiếm code..."
                            leftIcon={<Search className="w-4 h-4" />}
                            value={codeSearchTerm}
                            onChange={(e) => setCodeSearchTerm(e.target.value)}
                          />
                        </div>
                        
                        {loadingCodes ? (
                          <div className="flex justify-center py-8">
                            <Spinner />
                          </div>
                        ) : (
                          <div className="max-h-96 overflow-y-auto">
                            <table className="w-full text-sm">
                              <thead className="bg-slate-50 dark:bg-slate-800 sticky top-0">
                                <tr>
                                  <th className="text-left px-3 py-2 font-medium text-slate-600 dark:text-slate-400">Code</th>
                                  <th className="text-left px-3 py-2 font-medium text-slate-600 dark:text-slate-400">Status</th>
                                  <th className="text-left px-3 py-2 font-medium text-slate-600 dark:text-slate-400">Batch ID</th>
                                  <th className="text-left px-3 py-2 font-medium text-slate-600 dark:text-slate-400">Created</th>
                                </tr>
                              </thead>
                              <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                                {poolCodes
                                  .filter(code =>
                                    code.poolCode.toLowerCase().includes(codeSearchTerm.toLowerCase())
                                  )
                                  .map((code) => (
                                    <tr key={code.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
                                      <td className="px-3 py-2 font-mono text-xs text-slate-900 dark:text-slate-100">
                                        {code.poolCode}
                                      </td>
                                      <td className="px-3 py-2">
                                        <Badge
                                          variant={
                                            code.status === 0
                                              ? 'success'
                                              : code.status === 1
                                              ? 'info'
                                              : 'danger'
                                          }
                                        >
                                          {code.statusName}
                                        </Badge>
                                      </td>
                                      <td className="px-3 py-2 text-slate-600 dark:text-slate-400">
                                        {code.poolCodeCreateID || '-'}
                                      </td>
                                      <td className="px-3 py-2 text-slate-600 dark:text-slate-400">
                                        {code.poolCodeCreateDatetime || '-'}
                                      </td>
                                    </tr>
                                  ))}
                              </tbody>
                            </table>
                            {poolCodes.length === 0 && (
                              <p className="text-center py-8 text-slate-500">Không có codes</p>
                            )}
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
          
          {filteredPools.length === 0 && (
            <CardContent>
              <p className="text-center py-8 text-slate-500">Không tìm thấy pool nào</p>
            </CardContent>
          )}
        </Card>
      )}

      {/* Create Pool Modal */}
      <CreatePoolModal
        isOpen={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        onSuccess={() => {
          setShowCreateModal(false);
          fetchPools();
        }}
      />

      {/* Add Code Modal */}
      <AddCodeModal
        isOpen={showAddCodeModal}
        onClose={() => {
          setShowAddCodeModal(false);
          setSelectedPool(null);
        }}
        poolName={selectedPool || ''}
        onSuccess={() => {
          setShowAddCodeModal(false);
          setSelectedPool(null);
          fetchPools();
          if (expandedPool) {
            toggleExpand(expandedPool);
          }
        }}
      />

      {/* Import CSV Modal */}
      <ImportCSVModal
        isOpen={showImportModal}
        onClose={() => {
          setShowImportModal(false);
          setSelectedPool(null);
        }}
        poolName={selectedPool || ''}
        pools={pools}
        onSuccess={() => {
          setShowImportModal(false);
          setSelectedPool(null);
          fetchPools();
        }}
      />
    </div>
  );
}

// Create Pool Modal Component
function CreatePoolModal({
  isOpen,
  onClose,
  onSuccess,
}: {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const { addToast } = useAppStore();
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState({
    poolName: '',
    poolDescription: '',
    note: '',
    createdBy: 'API',
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.poolName.trim()) {
      addToast('error', 'Pool name is required');
      return;
    }

    try {
      setLoading(true);
      await createPool(formData);
      addToast('success', 'Pool created successfully');
      setFormData({ poolName: '', poolDescription: '', note: '', createdBy: 'API' });
      onSuccess();
    } catch (error) {
      addToast('error', 'Failed to create pool');
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Tạo Pool Mới" size="md">
      <form onSubmit={handleSubmit} className="space-y-4">
        <Input
          label="Pool Name (GTIN)"
          placeholder="VD: 8934567890123"
          value={formData.poolName}
          onChange={(e) => setFormData({ ...formData, poolName: e.target.value })}
          required
        />
        <Input
          label="Description"
          placeholder="Mô tả pool"
          value={formData.poolDescription}
          onChange={(e) => setFormData({ ...formData, poolDescription: e.target.value })}
        />
        <Input
          label="Note"
          placeholder="Ghi chú"
          value={formData.note}
          onChange={(e) => setFormData({ ...formData, note: e.target.value })}
        />
        <div className="flex justify-end gap-2 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>
            Hủy
          </Button>
          <Button type="submit" isLoading={loading}>
            Tạo Pool
          </Button>
        </div>
      </form>
    </Modal>
  );
}

// Add Code Modal Component
function AddCodeModal({
  isOpen,
  onClose,
  poolName,
  onSuccess,
}: {
  isOpen: boolean;
  onClose: () => void;
  poolName: string;
  onSuccess: () => void;
}) {
  const { addToast } = useAppStore();
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState({
    code: '',
    createID: '',
    createdBy: 'API',
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.code.trim()) {
      addToast('error', 'Code is required');
      return;
    }

    try {
      setLoading(true);
      await fetch(`/api/datapool/pools/${encodeURIComponent(poolName)}/codes`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          mode: 0,
          singleCode: formData.code,
          createID: formData.createID || undefined,
          createdBy: formData.createdBy,
        }),
      });
      addToast('success', 'Code added successfully');
      setFormData({ code: '', createID: '', createdBy: 'API' });
      onSuccess();
    } catch (error) {
      addToast('error', 'Failed to add code');
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={`Thêm Code vào ${poolName}`} size="md">
      <form onSubmit={handleSubmit} className="space-y-4">
        <Input
          label="Code"
          placeholder="Nhập code"
          value={formData.code}
          onChange={(e) => setFormData({ ...formData, code: e.target.value })}
          required
        />
        <Input
          label="Batch ID"
          placeholder="ID của lô (tùy chọn)"
          value={formData.createID}
          onChange={(e) => setFormData({ ...formData, createID: e.target.value })}
        />
        <div className="flex justify-end gap-2 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>
            Hủy
          </Button>
          <Button type="submit" isLoading={loading}>
            Thêm Code
          </Button>
        </div>
      </form>
    </Modal>
  );
}

// Import CSV Modal Component
function ImportCSVModal({
  isOpen,
  onClose,
  poolName: initialPool,
  pools,
  onSuccess,
}: {
  isOpen: boolean;
  onClose: () => void;
  poolName: string;
  pools: PoolInfoDto[];
  onSuccess: () => void;
}) {
  const { addToast } = useAppStore();
  const [loading, setLoading] = useState(false);
  const [poolName, setPoolName] = useState(initialPool);
  const [createID, setCreateID] = useState('');
  const [file, setFile] = useState<File | null>(null);
  const [csvData, setCsvData] = useState<string[][]>([]);
  const [codeColumn, setCodeColumn] = useState(0);

  useEffect(() => {
    if (initialPool) setPoolName(initialPool);
  }, [initialPool]);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (!selectedFile) return;

    setFile(selectedFile);
    const reader = new FileReader();
    reader.onload = (event) => {
      const text = event.target?.result as string;
      const lines = text.split('\n').filter(line => line.trim());
      const parsed = lines.map(line => parseCSVLine(line));
      setCsvData(parsed.slice(0, 11)); // Preview first 10 rows
    };
    reader.readAsText(selectedFile);
  };

  const parseCSVLine = (line: string): string[] => {
    const result: string[] = [];
    let current = '';
    let inQuotes = false;
    
    for (let i = 0; i < line.length; i++) {
      const char = line[i];
      if (char === '"') {
        inQuotes = !inQuotes;
      } else if (char === ',' && !inQuotes) {
        result.push(current.trim());
        current = '';
      } else {
        current += char;
      }
    }
    result.push(current.trim());
    return result;
  };

  const handleImport = async () => {
    if (!file || !poolName) {
      addToast('error', 'Please select a file and pool');
      return;
    }

    try {
      setLoading(true);
      const text = await file.text();
      const lines = text.split('\n').filter(line => line.trim());
      const codes = lines.map(line => {
        const fields = parseCSVLine(line);
        return fields[codeColumn] || '';
      }).filter(code => code);

      if (codes.length === 0) {
        addToast('error', 'No codes found in selected column');
        return;
      }

      await fetch(`/api/datapool/pools/${encodeURIComponent(poolName)}/codes`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          mode: 1,
          codes: codes,
          createID: createID || undefined,
          createdBy: 'API',
        }),
      });

      addToast('success', `Imported ${codes.length} codes`);
      setFile(null);
      setCsvData([]);
      setCreateID('');
      onSuccess();
    } catch (error) {
      addToast('error', 'Failed to import codes');
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Import Codes từ CSV" size="xl">
      <div className="space-y-4">
        <Select
          label="Pool"
          value={poolName}
          onChange={(e) => setPoolName(e.target.value)}
          options={pools.map(p => ({ value: p.poolName, label: p.poolName }))}
        />
        
        <Input
          label="Batch ID"
          placeholder="ID của lô (tùy chọn)"
          value={createID}
          onChange={(e) => setCreateID(e.target.value)}
        />
        
        <div className="space-y-2">
          <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">
            File CSV
          </label>
          <div className="border-2 border-dashed border-slate-300 dark:border-slate-600 rounded-xl p-6 text-center hover:border-primary transition-colors">
            <input
              type="file"
              accept=".csv"
              onChange={handleFileChange}
              className="hidden"
              id="csv-upload"
            />
            <label htmlFor="csv-upload" className="cursor-pointer">
              <Upload className="w-8 h-8 mx-auto mb-2 text-slate-400" />
              <p className="text-sm text-slate-500">
                {file ? file.name : 'Click to upload CSV file'}
              </p>
            </label>
          </div>
        </div>

        {csvData.length > 0 && (
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">
                Chọn cột Code
              </label>
              <span className="text-xs text-slate-500">{csvData.length} rows</span>
            </div>
            <Select
              value={codeColumn}
              onChange={(e) => setCodeColumn(parseInt(e.target.value))}
              options={csvData[0].map((header, idx) => ({
                value: idx.toString(),
                label: `${header || `Column ${idx + 1}`} (${csvData.slice(1).map(row => row[idx] || '').filter(Boolean).length} values)`,
              }))}
            />
            
            <div className="mt-4 overflow-x-auto">
              <table className="w-full text-xs border border-slate-200 dark:border-slate-700 rounded-lg overflow-hidden">
                <thead className="bg-slate-50 dark:bg-slate-800">
                  <tr>
                    {csvData[0].map((header, idx) => (
                      <th
                        key={idx}
                        className={`px-3 py-2 text-left font-medium ${
                          idx === codeColumn
                            ? 'bg-primary-bg text-primary'
                            : 'text-slate-600 dark:text-slate-400'
                        }`}
                      >
                        {header || `Col ${idx + 1}`}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                  {csvData.slice(1, 6).map((row, rowIdx) => (
                    <tr key={rowIdx}>
                      {row.map((cell, cellIdx) => (
                        <td
                          key={cellIdx}
                          className={`px-3 py-1.5 font-mono ${
                            cellIdx === codeColumn
                              ? 'bg-primary-bg/30'
                              : 'text-slate-600 dark:text-slate-400'
                          }`}
                        >
                          {cell}
                        </td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
              {csvData.length > 6 && (
                <p className="text-xs text-slate-500 mt-2">... và {csvData.length - 6} rows khác</p>
              )}
            </div>
          </div>
        )}

        <div className="flex justify-end gap-2 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>
            Hủy
          </Button>
          <Button
            onClick={handleImport}
            isLoading={loading}
            disabled={!file || csvData.length === 0}
          >
            Import {csvData.length > 0 ? csvData.length - 1 : 0} Codes
          </Button>
        </div>
      </div>
    </Modal>
  );
}
