import { useState, useEffect, useCallback } from 'react';
import {
  Package,
  Plus,
  Search,
  RefreshCw,
  Trash2,
  Eye,
  Info,
  Grid3X3,
  Database,
  AlertCircle,
} from 'lucide-react';
import { Card, CardHeader, CardContent, Button, Modal, Input, Select, Badge, StatBox, PageLoader, ProgressBar } from '@/components/ui';
import { useAppStore } from '@/stores/useAppStore';
import { getPOList, getPOInfo, createPO, loadCodesFromGTIN } from '@/services/poApi';
import type { POInfo, POCodeInfo, POCartonInfo, PORecordInfo } from '@/types';

type Tab = 'list' | 'create' | 'detail';
type DetailTab = 'info' | 'codes' | 'cartons' | 'database';

export function POManager() {
  const { addToast } = useAppStore();
  const [activeTab, setActiveTab] = useState<Tab>('list');
  const [poList, setPOList] = useState<POInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedPO, setSelectedPO] = useState<POInfo | null>(null);
  const [detailTab, setDetailTab] = useState<DetailTab>('info');
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [deleteLoading, setDeleteLoading] = useState(false);
  
  // PO codes and records
  const [poCodes] = useState<POCodeInfo[]>([]);
  const [poCartons] = useState<POCartonInfo[]>([]);
  const [] = useState<PORecordInfo[]>([]);
  const [loadingDetail, setLoadingDetail] = useState(false);
  
  // Codes pagination
  const [codesPage, setCodesPage] = useState(1);
  const [codesFilter, setCodesFilter] = useState<'all' | 'unused' | 'active' | 'packed'>('all');

  const fetchPOList = useCallback(async () => {
    try {
      setLoading(true);
      const data = await getPOList();
      setPOList(data);
    } catch (error) {
      addToast('error', 'Failed to load PO list');
      console.error(error);
    } finally {
      setLoading(false);
    }
  }, [addToast]);

  useEffect(() => {
    fetchPOList();
  }, [fetchPOList]);

  const handleViewDetail = async (po: POInfo) => {
    setSelectedPO(po);
    setActiveTab('detail');
    setDetailTab('info');
    setCodesPage(1);
    setCodesFilter('all');
  };

  const fetchPODetail = async () => {
    if (!selectedPO) return;
    setLoadingDetail(true);
    try {
      const detail = await getPOInfo(selectedPO.orderNo);
      setSelectedPO(detail);
    } catch (error) {
      addToast('error', 'Failed to load PO details');
    } finally {
      setLoadingDetail(false);
    }
  };

  useEffect(() => {
    if (activeTab === 'detail' && selectedPO) {
      fetchPODetail();
    }
  }, [activeTab, selectedPO?.orderNo]);

  const handleDelete = async () => {
    if (!selectedPO) return;
    
    try {
      setDeleteLoading(true);
      // TODO: Call delete API
      addToast('success', `PO ${selectedPO.orderNo} deleted successfully`);
      setShowDeleteModal(false);
      setActiveTab('list');
      setSelectedPO(null);
      fetchPOList();
    } catch (error) {
      addToast('error', 'Failed to delete PO');
    } finally {
      setDeleteLoading(false);
    }
  };

  const filteredPOList = poList.filter(po =>
    po.orderNo.toLowerCase().includes(searchTerm.toLowerCase()) ||
    po.productName.toLowerCase().includes(searchTerm.toLowerCase()) ||
    po.gtin.includes(searchTerm)
  );

  // Stats calculation
  const stats = selectedPO ? {
    orderQty: selectedPO.orderQty || 0,
    activeCodes: poCodes.filter(c => c.status === 1).length,
    packedCodes: poCodes.filter(c => c.status === 2).length,
    cartonCount: poCartons.length,
    closedCartons: poCartons.filter(c => c.status === 2).length,
  } : null;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">PO Manager</h1>
          <p className="text-slate-500 dark:text-slate-400 mt-1">Quản lý Purchase Orders</p>
        </div>
        <div className="flex gap-2">
          <Button variant="secondary" leftIcon={<RefreshCw className="w-4 h-4" />} onClick={fetchPOList}>
            Refresh
          </Button>
          <Button leftIcon={<Plus className="w-4 h-4" />} onClick={() => setActiveTab('create')}>
            Tạo PO
          </Button>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 p-1 bg-slate-100 dark:bg-slate-800 rounded-xl w-fit">
        <button
          onClick={() => setActiveTab('list')}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
            activeTab === 'list'
              ? 'bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
          }`}
        >
          PO List
        </button>
        <button
          onClick={() => setActiveTab('create')}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
            activeTab === 'create'
              ? 'bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
          }`}
        >
          New PO
        </button>
        <button
          onClick={() => selectedPO && setActiveTab('detail')}
          disabled={!selectedPO}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
            activeTab === 'detail'
              ? 'bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
          } disabled:opacity-50 disabled:cursor-not-allowed`}
        >
          Detail
        </button>
      </div>

      {/* Content */}
      {activeTab === 'list' && (
        <>
          {/* Stats */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            <StatBox label="Tổng PO" value={poList.length} icon={<Package className="w-5 h-5" />} variant="primary" />
            <StatBox label="Đang hoạt động" value={poList.filter(p => p.orderQty).length} icon={<AlertCircle className="w-5 h-5" />} variant="success" />
            <StatBox label="Hoàn thành" value={0} icon={<Grid3X3 className="w-5 h-5" />} variant="warning" />
            <StatBox label="Tổng số thùng" value={0} icon={<Database className="w-5 h-5" />} variant="default" />
          </div>

          {/* Search */}
          <Card>
            <CardContent className="py-4">
              <Input
                placeholder="Tìm kiếm PO (Order No, Product Name, GTIN)..."
                leftIcon={<Search className="w-4 h-4" />}
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
              />
            </CardContent>
          </Card>

          {/* PO Table */}
          {loading ? (
            <PageLoader />
          ) : (
            <Card>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-slate-50 dark:bg-slate-800">
                    <tr>
                      <th className="text-left px-4 py-3 font-medium text-slate-600 dark:text-slate-400">Order No.</th>
                      <th className="text-left px-4 py-3 font-medium text-slate-600 dark:text-slate-400">Product</th>
                      <th className="text-left px-4 py-3 font-medium text-slate-600 dark:text-slate-400">GTIN</th>
                      <th className="text-left px-4 py-3 font-medium text-slate-600 dark:text-slate-400">Order Qty</th>
                      <th className="text-left px-4 py-3 font-medium text-slate-600 dark:text-slate-400">Production Date</th>
                      <th className="text-left px-4 py-3 font-medium text-slate-600 dark:text-slate-400">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
                    {filteredPOList.map((po) => (
                      <tr key={po.orderNo} className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
                        <td className="px-4 py-3 font-medium text-slate-900 dark:text-slate-100">{po.orderNo}</td>
                        <td className="px-4 py-3 text-slate-600 dark:text-slate-400">{po.productName}</td>
                        <td className="px-4 py-3 font-mono text-xs">{po.gtin}</td>
                        <td className="px-4 py-3">{po.orderQty?.toLocaleString()}</td>
                        <td className="px-4 py-3 text-slate-600 dark:text-slate-400">{po.productionDate}</td>
                        <td className="px-4 py-3">
                          <div className="flex gap-1">
                            <Button size="sm" variant="ghost" onClick={() => handleViewDetail(po)}>
                              <Eye className="w-4 h-4" />
                            </Button>
                            <Button size="sm" variant="ghost" onClick={() => { setSelectedPO(po); setShowDeleteModal(true); }}>
                              <Trash2 className="w-4 h-4 text-danger" />
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              {filteredPOList.length === 0 && (
                <CardContent>
                  <p className="text-center py-8 text-slate-500">Không tìm thấy PO nào</p>
                </CardContent>
              )}
            </Card>
          )}
        </>
      )}

      {activeTab === 'create' && (
        <CreatePOForm
          onSuccess={() => {
            setActiveTab('list');
            fetchPOList();
          }}
          pools={poList.map(p => p.gtin).filter(Boolean)}
        />
      )}

      {activeTab === 'detail' && selectedPO && (
        <>
          {/* Detail Tabs */}
          <div className="flex gap-1 p-1 bg-slate-100 dark:bg-slate-800 rounded-xl w-fit">
            <button
              onClick={() => setDetailTab('info')}
              className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                detailTab === 'info'
                  ? 'bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 shadow-sm'
                  : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
              }`}
            >
              <Info className="w-4 h-4" /> Info
            </button>
            <button
              onClick={() => setDetailTab('codes')}
              className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                detailTab === 'codes'
                  ? 'bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 shadow-sm'
                  : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
              }`}
            >
              <Package className="w-4 h-4" /> Codes
            </button>
            <button
              onClick={() => setDetailTab('cartons')}
              className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                detailTab === 'cartons'
                  ? 'bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 shadow-sm'
                  : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
              }`}
            >
              <Grid3X3 className="w-4 h-4" /> Cartons
            </button>
            <button
              onClick={() => setDetailTab('database')}
              className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                detailTab === 'database'
                  ? 'bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 shadow-sm'
                  : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
              }`}
            >
              <Database className="w-4 h-4" /> Database
            </button>
          </div>

          {detailTab === 'info' && (
            <POInfoTab po={selectedPO} stats={stats} onRefresh={fetchPODetail} loading={loadingDetail} />
          )}
          {detailTab === 'codes' && (
            <POCodesTab
              po={selectedPO}
              codes={poCodes}
              filter={codesFilter}
              setFilter={setCodesFilter}
              page={codesPage}
              setPage={setCodesPage}
            />
          )}
          {detailTab === 'cartons' && (
            <POCartonsTab cartons={poCartons} />
          )}
          {detailTab === 'database' && (
            <PODatabaseTab po={selectedPO} />
          )}
        </>
      )}

      {/* Delete Confirmation Modal */}
      <Modal isOpen={showDeleteModal} onClose={() => setShowDeleteModal(false)} title="Xác nhận xóa PO" size="sm">
        <div className="space-y-4">
          <p className="text-slate-600 dark:text-slate-400">
            Bạn có chắc chắn muốn xóa PO <strong>{selectedPO?.orderNo}</strong> không?
          </p>
          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={() => setShowDeleteModal(false)}>Hủy</Button>
            <Button variant="danger" onClick={handleDelete} isLoading={deleteLoading}>Xóa</Button>
          </div>
        </div>
      </Modal>
    </div>
  );
}

// Create PO Form Component
function CreatePOForm({ onSuccess, pools: _pools }: { onSuccess: () => void; pools: string[] }) {
  const { addToast } = useAppStore();
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState<POInfo>({
    orderNo: '',
    gtin: '',
    orderQty: 0,
    cartonCapacity: 50,
    productionDate: new Date().toISOString().split('T')[0],
    shift: 'A',
    productName: '',
    productCode: '',
    lotNumber: '',
    site: '',
    factory: '',
    productionLine: '',
    customerOrderNo: '',
    uom: 'PCS',
    createdBy: 'API',
  });
  const [autoLoadCodes, setAutoLoadCodes] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!formData.orderNo || !formData.gtin || !formData.orderQty) {
      addToast('error', 'Vui lòng điền đầy đủ thông tin bắt buộc');
      return;
    }

    try {
      setLoading(true);
      const result = await createPO(formData);
      
      if (result.success) {
        if (autoLoadCodes && formData.gtin) {
          await loadCodesFromGTIN(formData.orderNo, { gtin: formData.gtin, qty: formData.orderQty });
        }
        addToast('success', 'PO created successfully');
        onSuccess();
      } else {
        addToast('error', result.message || 'Failed to create PO');
      }
    } catch (error) {
      addToast('error', 'Failed to create PO');
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card>
      <CardHeader title="Tạo PO mới" />
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Input
              label="Order No. *"
              placeholder="VD: PO-2024-001"
              value={formData.orderNo}
              onChange={(e) => setFormData({ ...formData, orderNo: e.target.value })}
              required
            />
            <Input
              label="GTIN (Pool Name) *"
              placeholder="VD: 8934567890123"
              value={formData.gtin}
              onChange={(e) => setFormData({ ...formData, gtin: e.target.value })}
              required
            />
            <Input
              label="Product Name *"
              placeholder="Tên sản phẩm"
              value={formData.productName}
              onChange={(e) => setFormData({ ...formData, productName: e.target.value })}
              required
            />
            <Input
              label="Product Code"
              placeholder="Mã sản phẩm"
              value={formData.productCode}
              onChange={(e) => setFormData({ ...formData, productCode: e.target.value })}
            />
            <Input
              label="Order Qty *"
              type="number"
              placeholder="Số lượng"
              value={formData.orderQty}
              onChange={(e) => setFormData({ ...formData, orderQty: parseInt(e.target.value) || 0 })}
              required
            />
            <Input
              label="Carton Capacity"
              type="number"
              placeholder="Số sản phẩm mỗi thùng"
              value={formData.cartonCapacity}
              onChange={(e) => setFormData({ ...formData, cartonCapacity: parseInt(e.target.value) || 0 })}
            />
            <Input
              label="Production Date"
              type="date"
              value={formData.productionDate}
              onChange={(e) => setFormData({ ...formData, productionDate: e.target.value })}
            />
            <Select
              label="Shift"
              value={formData.shift}
              onChange={(e) => setFormData({ ...formData, shift: e.target.value })}
              options={[
                { value: 'A', label: 'Shift A' },
                { value: 'B', label: 'Shift B' },
                { value: 'C', label: 'Shift C' },
              ]}
            />
            <Input
              label="Lot Number"
              placeholder="Số lô"
              value={formData.lotNumber}
              onChange={(e) => setFormData({ ...formData, lotNumber: e.target.value })}
            />
            <Input
              label="Site"
              placeholder="Site"
              value={formData.site}
              onChange={(e) => setFormData({ ...formData, site: e.target.value })}
            />
            <Input
              label="Factory"
              placeholder="Nhà máy"
              value={formData.factory}
              onChange={(e) => setFormData({ ...formData, factory: e.target.value })}
            />
            <Input
              label="Production Line"
              placeholder="Dây chuyền sản xuất"
              value={formData.productionLine}
              onChange={(e) => setFormData({ ...formData, productionLine: e.target.value })}
            />
            <Input
              label="Customer Order No."
              placeholder="Mã đơn hàng khách"
              value={formData.customerOrderNo}
              onChange={(e) => setFormData({ ...formData, customerOrderNo: e.target.value })}
            />
            <Select
              label="UOM"
              value={formData.uom}
              onChange={(e) => setFormData({ ...formData, uom: e.target.value })}
              options={[
                { value: 'PCS', label: 'Pieces' },
                { value: 'BOX', label: 'Box' },
                { value: 'CTN', label: 'Carton' },
              ]}
            />
          </div>

          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              checked={autoLoadCodes}
              onChange={(e) => setAutoLoadCodes(e.target.checked)}
              className="w-4 h-4 rounded border-slate-300 text-primary focus:ring-primary"
            />
            <span className="text-sm text-slate-700 dark:text-slate-300">
              Tự động load codes từ DataPool (GTIN)
            </span>
          </label>

          <div className="flex justify-end gap-2">
            <Button type="button" variant="secondary" onClick={onSuccess}>
              Hủy
            </Button>
            <Button type="submit" isLoading={loading}>
              Tạo PO
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}

// PO Info Tab Component
function POInfoTab({ po, stats, onRefresh }: { po: POInfo; stats: any; onRefresh: () => void; loading: boolean }) {
  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
      {/* PO Details */}
      <div className="lg:col-span-2">
        <Card>
          <CardHeader title="Thông tin PO" action={<Button size="sm" variant="ghost" onClick={onRefresh}><RefreshCw className="w-4 h-4" /></Button>} />
          <CardContent>
            <div className="grid grid-cols-2 gap-x-8 gap-y-4 text-sm">
              <DetailRow label="Order No." value={po.orderNo} />
              <DetailRow label="GTIN" value={po.gtin} mono />
              <DetailRow label="Product Name" value={po.productName} />
              <DetailRow label="Product Code" value={po.productCode} />
              <DetailRow label="Order Qty" value={po.orderQty?.toLocaleString()} />
              <DetailRow label="Carton Capacity" value={po.cartonCapacity} />
              <DetailRow label="Production Date" value={po.productionDate} />
              <DetailRow label="Shift" value={po.shift} />
              <DetailRow label="Lot Number" value={po.lotNumber} />
              <DetailRow label="Site" value={po.site} />
              <DetailRow label="Factory" value={po.factory} />
              <DetailRow label="Production Line" value={po.productionLine} />
              <DetailRow label="Customer Order" value={po.customerOrderNo} />
              <DetailRow label="UOM" value={po.uom} />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Stats */}
      <div>
        <Card>
          <CardHeader title="Thống kê" />
          <CardContent className="space-y-4">
            <div>
              <p className="text-sm text-slate-500">Order Qty</p>
              <p className="text-2xl font-bold text-slate-900 dark:text-slate-100">{stats?.orderQty?.toLocaleString() || 0}</p>
            </div>
            <div>
              <p className="text-sm text-slate-500">Active Codes</p>
              <p className="text-2xl font-bold text-success">{stats?.activeCodes || 0}</p>
            </div>
            <div>
              <p className="text-sm text-slate-500">Packed Codes</p>
              <p className="text-2xl font-bold text-info">{stats?.packedCodes || 0}</p>
            </div>
            <div>
              <p className="text-sm text-slate-500">Carton Count</p>
              <p className="text-2xl font-bold text-slate-700 dark:text-slate-300">{stats?.cartonCount || 0}</p>
            </div>
            {stats && stats.orderQty > 0 && (
              <div>
                <p className="text-sm text-slate-500 mb-2">Progress</p>
                <ProgressBar
                  value={stats.activeCodes + stats.packedCodes}
                  max={stats.orderQty}
                  showLabel
                  variant={(stats.activeCodes + stats.packedCodes) / stats.orderQty >= 1 ? 'success' : 'default'}
                />
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

// PO Codes Tab Component
function POCodesTab({
  po: _po,
  codes,
  filter,
  setFilter,
  page,
  setPage,
}: {
  po: POInfo;
  codes: POCodeInfo[];
  filter: 'all' | 'unused' | 'active' | 'packed';
  setFilter: (f: 'all' | 'unused' | 'active' | 'packed') => void;
  page: number;
  setPage: (p: number) => void;
}) {
  const codesPerPage = 100;
  const filteredCodes = codes.filter(code => {
    if (filter === 'all') return true;
    if (filter === 'unused') return code.status === 0;
    if (filter === 'active') return code.status === 1;
    if (filter === 'packed') return code.status === 2;
    return true;
  });
  const totalPages = Math.ceil(filteredCodes.length / codesPerPage);
  const paginatedCodes = filteredCodes.slice((page - 1) * codesPerPage, page * codesPerPage);

  return (
    <Card>
      <CardHeader title={`Codes (${filteredCodes.length})`}>
        <div className="flex gap-1 p-1 bg-slate-100 dark:bg-slate-800 rounded-lg">
          {(['all', 'unused', 'active', 'packed'] as const).map((f) => (
            <button
              key={f}
              onClick={() => { setFilter(f); setPage(1); }}
              className={`px-3 py-1 rounded text-xs font-medium transition-colors ${
                filter === f
                  ? 'bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100'
                  : 'text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
              }`}
            >
              {f.charAt(0).toUpperCase() + f.slice(1)}
            </button>
          ))}
        </div>
      </CardHeader>
      <CardContent>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 dark:bg-slate-800">
              <tr>
                <th className="text-left px-4 py-3 font-medium text-slate-600 dark:text-slate-400">Code</th>
                <th className="text-left px-4 py-3 font-medium text-slate-600 dark:text-slate-400">Status</th>
                <th className="text-left px-4 py-3 font-medium text-slate-600 dark:text-slate-400">Carton</th>
                <th className="text-left px-4 py-3 font-medium text-slate-600 dark:text-slate-400">Activate Date</th>
                <th className="text-left px-4 py-3 font-medium text-slate-600 dark:text-slate-400">User</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              {paginatedCodes.map((code) => (
                <tr key={code.code} className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
                  <td className="px-4 py-3 font-mono text-xs">{code.code}</td>
                  <td className="px-4 py-3">
                    <Badge variant={code.status === 0 ? 'success' : code.status === 1 ? 'info' : 'warning'}>
                      {code.status === 0 ? 'Unused' : code.status === 1 ? 'Active' : 'Packed'}
                    </Badge>
                  </td>
                  <td className="px-4 py-3 font-mono text-xs">{code.cartonCode || '-'}</td>
                  <td className="px-4 py-3 text-slate-600 dark:text-slate-400">{code.activateDatetime || '-'}</td>
                  <td className="px-4 py-3 text-slate-600 dark:text-slate-400">{code.activateUser || '-'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {filteredCodes.length === 0 && (
          <p className="text-center py-8 text-slate-500">Không có codes</p>
        )}
        {totalPages > 1 && (
          <div className="flex justify-center gap-2 mt-4">
            <Button size="sm" variant="ghost" disabled={page === 1} onClick={() => setPage(page - 1)}>
              Previous
            </Button>
            <span className="px-3 py-1 text-sm text-slate-600">Page {page} of {totalPages}</span>
            <Button size="sm" variant="ghost" disabled={page === totalPages} onClick={() => setPage(page + 1)}>
              Next
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

// PO Cartons Tab Component
function POCartonsTab({ cartons }: { cartons: POCartonInfo[] }) {
  return (
    <Card>
      <CardHeader title={`Cartons (${cartons.length})`} />
      <CardContent>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 dark:bg-slate-800">
              <tr>
                <th className="text-left px-4 py-3 font-medium text-slate-600 dark:text-slate-400">ID</th>
                <th className="text-left px-4 py-3 font-medium text-slate-600 dark:text-slate-400">Carton Code</th>
                <th className="text-left px-4 py-3 font-medium text-slate-600 dark:text-slate-400">Status</th>
                <th className="text-left px-4 py-3 font-medium text-slate-600 dark:text-slate-400">Start Time</th>
                <th className="text-left px-4 py-3 font-medium text-slate-600 dark:text-slate-400">Completed</th>
                <th className="text-left px-4 py-3 font-medium text-slate-600 dark:text-slate-400">User</th>
                <th className="text-left px-4 py-3 font-medium text-slate-600 dark:text-slate-400">Codes</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              {cartons.map((carton) => (
                <tr key={carton.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
                  <td className="px-4 py-3">{carton.id}</td>
                  <td className="px-4 py-3 font-mono text-xs">{carton.cartonCode}</td>
                  <td className="px-4 py-3">
                    <Badge variant={carton.status === 0 ? 'warning' : carton.status === 1 ? 'info' : 'success'}>
                      {carton.status === 0 ? 'Empty' : carton.status === 1 ? 'Open' : 'Closed'}
                    </Badge>
                  </td>
                  <td className="px-4 py-3 text-slate-600 dark:text-slate-400">{carton.startTime || '-'}</td>
                  <td className="px-4 py-3 text-slate-600 dark:text-slate-400">{carton.completedTime || '-'}</td>
                  <td className="px-4 py-3 text-slate-600 dark:text-slate-400">{carton.user || '-'}</td>
                  <td className="px-4 py-3">{carton.codeCount}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {cartons.length === 0 && (
          <p className="text-center py-8 text-slate-500">Không có cartons</p>
        )}
      </CardContent>
    </Card>
  );
}

// PO Database Tab Component
function PODatabaseTab({ po: _po }: { po: POInfo }) {
  return (
    <Card>
      <CardHeader title="Database Status" />
      <CardContent>
        <div className="space-y-4">
          <div className="flex items-center gap-2 p-4 bg-success-bg rounded-xl">
            <AlertCircle className="w-5 h-5 text-success" />
            <span className="text-success font-medium">Database Ready</span>
          </div>
          
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <StatBox label="Loaded Codes" value={0} variant="success" />
            <StatBox label="Created Cartons" value={0} variant="primary" />
            <StatBox label="DB Files" value={0} variant="default" />
            <StatBox label="Missing Files" value={0} variant="danger" />
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

// Helper Component
function DetailRow({ label, value, mono = false }: { label: string; value: string | number; mono?: boolean }) {
  return (
    <div>
      <p className="text-slate-500 dark:text-slate-400">{label}</p>
      <p className={`font-medium text-slate-900 dark:text-slate-100 ${mono ? 'font-mono text-xs' : ''}`}>
        {value || '-'}
      </p>
    </div>
  );
}
