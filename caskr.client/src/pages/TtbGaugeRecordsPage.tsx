import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '../hooks'
import {
  fetchTtbGaugeRecordsForCompany,
  TtbGaugeRecord,
  TtbGaugeType,
  deleteTtbGaugeRecord
} from '../features/ttbGaugeRecordsSlice'
import TtbGaugeRecordModal from '../components/TtbGaugeRecordModal'

const gaugeTypeLabels: Record<TtbGaugeType, string> = {
  [TtbGaugeType.Fill]: 'Fill',
  [TtbGaugeType.Storage]: 'Storage',
  [TtbGaugeType.Removal]: 'Removal'
}

const formatDate = (dateString: string) => {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(dateString))
}

function TtbGaugeRecordsPage() {
  const dispatch = useAppDispatch()
  const gaugeRecords = useAppSelector(state => state.ttbGaugeRecords.items)
  const isLoading = useAppSelector(state => state.ttbGaugeRecords.isLoading)
  const error = useAppSelector(state => state.ttbGaugeRecords.error)
  const authUser = useAppSelector(state => state.auth.user)

  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingRecord, setEditingRecord] = useState<TtbGaugeRecord | null>(null)
  const [selectedBarrelId, setSelectedBarrelId] = useState<number>(0)
  const [startDate, setStartDate] = useState<string>('')
  const [endDate, setEndDate] = useState<string>('')

  const companyId = authUser?.companyId ?? 1

  useEffect(() => {
    dispatch(
      fetchTtbGaugeRecordsForCompany({
        companyId,
        startDate: startDate || undefined,
        endDate: endDate || undefined
      })
    )
  }, [dispatch, companyId, startDate, endDate])

  const handleEditRecord = (record: TtbGaugeRecord) => {
    setSelectedBarrelId(record.barrelId)
    setEditingRecord(record)
    setIsModalOpen(true)
  }

  const handleDeleteRecord = async (id: number) => {
    if (window.confirm('Are you sure you want to delete this gauge record? This action cannot be undone.')) {
      try {
        await dispatch(deleteTtbGaugeRecord(id)).unwrap()
      } catch (err) {
        console.error('Failed to delete gauge record:', err)
        alert('Failed to delete gauge record. Please try again.')
      }
    }
  }

  const handleModalClose = () => {
    setIsModalOpen(false)
    setEditingRecord(null)
    setSelectedBarrelId(0)
  }

  const handleModalSuccess = () => {
    setIsModalOpen(false)
    setEditingRecord(null)
    setSelectedBarrelId(0)
    dispatch(
      fetchTtbGaugeRecordsForCompany({
        companyId,
        startDate: startDate || undefined,
        endDate: endDate || undefined
      })
    )
  }

  const handleClearFilters = () => {
    setStartDate('')
    setEndDate('')
  }

  return (
    <div className="container mx-auto p-6">
      <div className="mb-6">
        <h1 className="text-3xl font-bold mb-2">TTB Gauge Records</h1>
        <p className="text-gray-600">
          Track proof, temperature, and volume measurements for barrels as required by TTB.
        </p>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-4 mb-6">
        <h2 className="text-lg font-semibold mb-4">Filters</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Start Date</label>
            <input
              type="date"
              value={startDate}
              onChange={e => setStartDate(e.target.value)}
              className="w-full border border-gray-300 rounded px-3 py-2"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">End Date</label>
            <input
              type="date"
              value={endDate}
              onChange={e => setEndDate(e.target.value)}
              className="w-full border border-gray-300 rounded px-3 py-2"
            />
          </div>
          <div className="flex items-end">
            <button
              onClick={handleClearFilters}
              className="px-4 py-2 border border-gray-300 rounded hover:bg-gray-50"
            >
              Clear Filters
            </button>
          </div>
        </div>
      </div>

      {/* Error Display */}
      {error && (
        <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-6">
          {error}
        </div>
      )}

      {/* Loading State */}
      {isLoading && (
        <div className="text-center py-8">
          <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900"></div>
          <p className="mt-2 text-gray-600">Loading gauge records...</p>
        </div>
      )}

      {/* Gauge Records Table */}
      {!isLoading && gaugeRecords.length === 0 && (
        <div className="bg-gray-50 rounded-lg p-8 text-center">
          <p className="text-gray-600 mb-4">No gauge records found.</p>
          <p className="text-sm text-gray-500">
            Gauge records are created when barrels are filled, during storage inventory checks, or when barrels are
            emptied.
          </p>
        </div>
      )}

      {!isLoading && gaugeRecords.length > 0 && (
        <div className="bg-white rounded-lg shadow overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Barrel SKU
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Gauge Date
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Type
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Proof
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Temp (°F)
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Wine Gal
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Proof Gal
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Gauged By
                </th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {gaugeRecords.map(record => (
                <tr key={record.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    {record.barrelSku || `Barrel #${record.barrelId}`}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                    {formatDate(record.gaugeDate)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm">
                    <span
                      className={`px-2 py-1 rounded text-xs font-medium ${
                        record.gaugeType === TtbGaugeType.Fill
                          ? 'bg-green-100 text-green-800'
                          : record.gaugeType === TtbGaugeType.Storage
                          ? 'bg-blue-100 text-blue-800'
                          : 'bg-orange-100 text-orange-800'
                      }`}
                    >
                      {gaugeTypeLabels[record.gaugeType]}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                    {record.proof.toFixed(2)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                    {record.temperature.toFixed(2)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                    {record.wineGallons.toFixed(2)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    {record.proofGallons.toFixed(2)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                    {record.gaugedByUserName || 'System'}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    <button
                      onClick={() => handleEditRecord(record)}
                      className="text-blue-600 hover:text-blue-900 mr-3"
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => handleDeleteRecord(record.id)}
                      className="text-red-600 hover:text-red-900"
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Summary */}
      {!isLoading && gaugeRecords.length > 0 && (
        <div className="mt-6 bg-white rounded-lg shadow p-4">
          <h3 className="text-lg font-semibold mb-2">Summary</h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
            <div>
              <p className="text-gray-600">Total Records</p>
              <p className="text-xl font-bold">{gaugeRecords.length}</p>
            </div>
            <div>
              <p className="text-gray-600">Total Proof Gallons</p>
              <p className="text-xl font-bold">
                {gaugeRecords.reduce((sum, r) => sum + r.proofGallons, 0).toFixed(2)}
              </p>
            </div>
            <div>
              <p className="text-gray-600">Total Wine Gallons</p>
              <p className="text-xl font-bold">
                {gaugeRecords.reduce((sum, r) => sum + r.wineGallons, 0).toFixed(2)}
              </p>
            </div>
            <div>
              <p className="text-gray-600">Unique Barrels</p>
              <p className="text-xl font-bold">
                {new Set(gaugeRecords.map(r => r.barrelId)).size}
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Modal */}
      <TtbGaugeRecordModal
        isOpen={isModalOpen}
        onClose={handleModalClose}
        onSuccess={handleModalSuccess}
        gaugeRecord={editingRecord}
        barrelId={selectedBarrelId}
        barrelSku={editingRecord?.barrelSku ?? undefined}
      />
    </div>
  )
}

export default TtbGaugeRecordsPage
