<template>
  <div class="fine-management">

    <!-- Header -->
    <div class="page-header">
      <div>
        <h1 class="page-title">Quản lý phạt</h1>
        <p class="page-desc">Danh sách phiếu phạt, thu phạt và miễn phạt</p>
      </div>
    </div>

    <!-- Stats -->
    <div class="stats-row">
      <div
        v-for="s in statsDisplay"
        :key="s.value"
        class="stat-card"
        :class="{ active: filters.status === s.value }"
        @click="setStatusFilter(s.value)"
      >
        <div class="stat-num" :class="s.color">{{ s.count }}</div>
        <div class="stat-label">{{ s.label }}</div>
        <div class="stat-amount" v-if="s.total > 0">{{ formatMoney(s.total) }}</div>
      </div>
    </div>

    <!-- Filters -->
    <div class="filter-bar">
      <input
        v-model="filters.search"
        class="search-input"
        placeholder="Tìm tên bạn đọc, mã SV, tên sách..."
        @input="onSearchInput"
      />
      <button class="btn-refresh" @click="fetchData(1)" title="Làm mới"><Icon icon="mingcute:refresh-3-line" width="18" height="18" /></button>
    </div>

    <!-- Table -->
    <div class="table-wrapper">
      <div v-if="isLoading" class="state-box">Đang tải...</div>
      <div v-else-if="loadError" class="state-box state-error">{{ loadError }}</div>
      <table v-else class="fine-table">
        <thead>
          <tr>
            <th>#</th>
            <th>Bạn đọc</th>
            <th>Sách</th>
            <th>Lý do</th>
            <th>Số tiền</th>
            <th>Ngày tạo</th>
            <th>Ngày thu</th>
            <th>Trạng thái</th>
            <th>Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="items.length === 0">
            <td colspan="9" class="empty-row">Không có phiếu phạt nào</td>
          </tr>
          <tr v-for="fine in items" :key="fine.fineId" class="fine-row">
            <td class="fine-id">#{{ fine.fineId }}</td>
            <td>
              <div class="user-name">{{ fine.user?.fullName || '—' }}</div>
              <div class="user-code">{{ fine.user?.studentCode || fine.user?.email }}</div>
            </td>
            <td>
              <div class="book-title" :title="fine.book?.title">{{ truncate(fine.book?.title, 35) }}</div>
              <div class="book-barcode">{{ fine.book?.barcode }}</div>
            </td>
            <td>
              <span class="reason-text">{{ fine.reason }}</span>
              <div class="note-text" v-if="fine.note && fine.status !== 'Waived'">{{ fine.note }}</div>
            </td>
            <td>
              <span class="amount-text">{{ formatMoney(fine.amount) }}</span>
            </td>
            <td>{{ formatDate(fine.createdDate) }}</td>
            <td>{{ fine.paidDate ? formatDate(fine.paidDate) : '—' }}</td>
            <td>
              <span class="status-badge" :class="fineStatusClass(fine.status)">
                {{ fineStatusLabel(fine.status) }}
              </span>
                <div class="waive-note" v-if="(fine.status === 2 || fine.status === 'Waived') && fine.note">
                {{ fine.note }}
              </div>
            </td>
            <td>
            <div class="action-buttons" v-if="fine.status === 0 || fine.status === 'Pending'">
                <button class="btn-pay" @click="openPay(fine)">💰 Thu</button>
                <button class="btn-waive" @click="openWaive(fine)">✓ Miễn</button>
              </div>
              <span v-else class="action-done">—</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    <div class="pagination" v-if="totalPages > 1">
      <button class="page-btn" :disabled="currentPage === 1" @click="goToPage(currentPage - 1)">‹</button>
      <template v-for="p in visiblePages" :key="p">
        <span v-if="p === '...'" class="page-dots">...</span>
        <button v-else class="page-btn" :class="{ active: p === currentPage }" @click="goToPage(p)">{{ p }}</button>
      </template>
      <button class="page-btn" :disabled="currentPage === totalPages" @click="goToPage(currentPage + 1)">›</button>
      <span class="page-info">{{ total }} phiếu phạt</span>
    </div>

    <!-- Modal Thu phạt -->
    <Teleport to="body">
      <div v-if="showPayModal" class="modal-overlay" @click.self="showPayModal = false">
        <div class="modal modal-sm">
          <div class="modal-header">
            <h3>Thu phạt</h3>
            <button class="modal-close" @click="showPayModal = false">✕</button>
          </div>
          <div class="modal-body">
            <div class="fine-summary">
              <div class="fine-summary-row">
                <span>Bạn đọc</span>
                <strong>{{ selectedFine?.user?.fullName }}</strong>
              </div>
              <div class="fine-summary-row">
                <span>Sách</span>
                <strong>{{ truncate(selectedFine?.book?.title, 40) }}</strong>
              </div>
              <div class="fine-summary-row">
                <span>Lý do</span>
                <span>{{ selectedFine?.reason }}</span>
              </div>
              <div class="fine-summary-row amount-row">
                <span>Số tiền thu</span>
                <strong class="text-red">{{ formatMoney(selectedFine?.amount) }}</strong>
              </div>
            </div>
            <div class="form-group">
              <label>Ghi chú <span class="optional-tag">Tuỳ chọn</span></label>
              <input v-model="payNote" placeholder="VD: Đã thu tiền mặt..." />
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" @click="showPayModal = false">Huỷ</button>
            <button class="btn btn-pay-lg" @click="submitPay" :disabled="isSubmitting">
              <i v-if="!isSubmitting" class="bi bi-cash-stack"></i>
              {{ isSubmitting ? 'Đang xử lý...' : ` Xác nhận thu ${formatMoney(selectedFine?.amount)}` }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Modal Miễn phạt -->
    <Teleport to="body">
      <div v-if="showWaiveModal" class="modal-overlay" @click.self="showWaiveModal = false">
        <div class="modal modal-sm">
          <div class="modal-header">
            <h3>Miễn phạt</h3>
            <button class="modal-close" @click="showWaiveModal = false">✕</button>
          </div>
          <div class="modal-body">
            <div class="fine-summary">
              <div class="fine-summary-row">
                <span>Bạn đọc</span>
                <strong>{{ selectedFine?.user?.fullName }}</strong>
              </div>
              <div class="fine-summary-row amount-row">
                <span>Số tiền miễn</span>
                <strong class="text-orange">{{ formatMoney(selectedFine?.amount) }}</strong>
              </div>
            </div>
            <div class="form-group">
              <label>Lý do miễn phạt <span class="required">*</span></label>
              <textarea
                v-model="waiveReason"
                rows="3"
                placeholder="VD: Sinh viên có hoàn cảnh khó khăn..."
                :class="{ 'input-error': waiveError }"
              ></textarea>
              <span v-if="waiveError" class="field-error">{{ waiveError }}</span>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" @click="showWaiveModal = false">Huỷ</button>
            <button class="btn btn-waive-lg" @click="submitWaive" :disabled="isSubmitting">
              {{ isSubmitting ? 'Đang xử lý...' : 'Xác nhận miễn phạt' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import api from '../../../services/api'
import { Icon } from '@iconify/vue'

const items = ref([])
const isLoading = ref(false)
const loadError = ref('')
const total = ref(0)
const totalPages = ref(1)
const currentPage = ref(1)
const pageSize = 20
const statsRaw = ref([])
const isSubmitting = ref(false)

const showPayModal   = ref(false)
const showWaiveModal = ref(false)
const selectedFine   = ref(null)
const payNote        = ref('')
const waiveReason    = ref('')
const waiveError     = ref('')

const filters = reactive({ search: '', status: 'Pending' })
let searchTimer = null

onMounted(() => fetchData())

const fetchData = async (page = 1) => {
  isLoading.value = true
  loadError.value = ''
  try {
    const params = new URLSearchParams({ page, pageSize })
    if (filters.status) params.append('status', filters.status)
    if (filters.search.trim()) params.append('search', filters.search.trim())

    const res = await api.get(`/Fines?${params}`)
    if (res.status === 200) {
      items.value       = res.data.items
      total.value       = res.data.total
      totalPages.value  = res.data.totalPages
      currentPage.value = res.data.page
      statsRaw.value    = res.data.stats || []
    }
  } catch (err) {
    loadError.value = err.response?.data?.message || 'Không thể tải dữ liệu'
  } finally {
    isLoading.value = false
  }
}

// ---- Pay ----
const openPay = (fine) => {
  selectedFine.value = fine
  payNote.value      = ''
  showPayModal.value = true
}

const submitPay = async () => {
  isSubmitting.value = true
  try {
    const res = await api.patch(`/Fines/${selectedFine.value.fineId}/pay`, { note: payNote.value || null })
    if (res.status === 200) {
      showPayModal.value = false
      await fetchData(currentPage.value)
    }
  } catch (err) {
    alert(err.response?.data?.message || 'Thu phạt thất bại')
  } finally {
    isSubmitting.value = false
  }
}

// ---- Waive ----
const openWaive = (fine) => {
  selectedFine.value  = fine
  waiveReason.value   = ''
  waiveError.value    = ''
  showWaiveModal.value = true
}

const submitWaive = async () => {
  if (!waiveReason.value.trim()) {
    waiveError.value = 'Vui lòng nhập lý do miễn phạt'
    return
  }
  isSubmitting.value = true
  try {
    const res = await api.patch(`/Fines/${selectedFine.value.fineId}/waive`, { reason: waiveReason.value })
    if (res.status === 200) {
      showWaiveModal.value = false
      await fetchData(currentPage.value)
    }
  } catch (err) {
    alert(err.response?.data?.message || 'Miễn phạt thất bại')
  } finally {
    isSubmitting.value = false
  }
}

// ---- Events ----
const onSearchInput = () => {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => fetchData(1), 400)
}
const setStatusFilter = (val) => {
  filters.status = filters.status === val ? '' : val
  fetchData(1)
}
const goToPage = (page) => { if (page >= 1 && page <= totalPages.value) fetchData(page) }

// ---- Helpers ----
const fineStatusLabel = (s) => {
  const map = {
    0: 'Chưa thu', 1: 'Đã thu', 2: 'Đã miễn',
    Pending: 'Chưa thu', Paid: 'Đã thu', Waived: 'Đã miễn'
  }
  return map[s] ?? s
}

const fineStatusClass = (s) => {
  const map = {
    0: 'status-orange', 1: 'status-green', 2: 'status-gray',
    Pending: 'status-orange', Paid: 'status-green', Waived: 'status-gray'
  }
  return map[s] ?? ''
}
const formatDate  = (d) => d ? new Date(d).toLocaleDateString('vi-VN') : '—'
const formatMoney = (n) => n != null ? new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(n) : '—'
const truncate    = (str, len) => !str ? '—' : str.length > len ? str.slice(0, len) + '...' : str

const statsDisplay = computed(() => {
  const get = (s) => statsRaw.value.find(x => x.status === s) ?? { count: 0, total: 0 }
  return [
    { value: 'Pending', label: 'Chưa thu', count: get('Pending').count, total: get('Pending').total, color: 'text-orange' },
    { value: 'Paid',    label: 'Đã thu',   count: get('Paid').count,    total: get('Paid').total,    color: 'text-green'  },
    { value: 'Waived',  label: 'Đã miễn',  count: get('Waived').count,  total: get('Waived').total,  color: 'text-gray'   },
  ]
})

const visiblePages = computed(() => {
  const pages = []
  const t = totalPages.value, cur = currentPage.value
  if (t <= 7) { for (let i = 1; i <= t; i++) pages.push(i) }
  else {
    pages.push(1)
    if (cur > 3) pages.push('...')
    for (let i = Math.max(2, cur - 1); i <= Math.min(t - 1, cur + 1); i++) pages.push(i)
    if (cur < t - 2) pages.push('...')
    pages.push(t)
  }
  return pages
})
</script>

<style lang="scss" scoped>
.fine-management {
  display: flex; flex-direction: column; gap: 20px;
  font-family: 'Segoe UI', sans-serif; color: #1a1a2e;
  border-radius: 12px;
  padding: 16px 24px;
  background: #ffffff;
}
.page-header { display: flex; align-items: flex-start; justify-content: space-between; }
.page-title  { font-size: 22px; font-weight: 800; margin: 0 0 4px; }
.page-desc   { font-size: 14px; color: #666; margin: 0; }

// Stats
.stats-row { display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; }
.stat-card {
  background: #fff; border-radius: 12px; border: 1.5px solid #e0e0e0;
  padding: 16px 20px; cursor: pointer; transition: all 0.15s;
  &:hover  { border-color: #3949ab; }
  &.active { border-color: #3949ab; background: #f0f4ff; }
}
.stat-num {
  font-size: 28px; font-weight: 800; line-height: 1;
  &.text-orange { color: #e65100; }
  &.text-green  { color: #2e7d32; }
  &.text-gray   { color: #9e9e9e; }
}
.stat-label  { font-size: 12px; color: #888; margin-top: 4px; }
.stat-amount { font-size: 13px; font-weight: 600; color: #3949ab; margin-top: 4px; }

// Filters
.filter-bar { display: flex; gap: 10px; align-items: center; }
.search-input {
  flex: 1; padding: 9px 14px; border: 1.5px solid #e0e0e0;
  border-radius: 8px; font-size: 14px; outline: none; font-family: inherit;
  &:focus { border-color: #3949ab; }
}
.btn-refresh {
  padding: 7px 10px; background: #fff; border: 1.5px solid #e0e0e0;
  border-radius: 8px; cursor: pointer; font-size: 16px;
  color: #3949ab;
  &:hover { border-color: #3949ab; }
}

// Table
.table-wrapper { overflow-x: auto; border-radius: 10px; border: 1px solid #e0e0e0; }
.fine-table {
  width: 100%; border-collapse: collapse; font-size: 14px;
  thead tr { background: #f5f5f5; }
  th { padding: 10px 14px; text-align: left; font-weight: 600; color: #555; white-space: nowrap; border-bottom: 1px solid #e0e0e0; }
  td { padding: 10px 14px; border-bottom: 1px solid #f0f0f0; vertical-align: middle; }
}
.fine-row {
  &:last-child td { border-bottom: none; }
  &:hover { background: #fafafa; }
}
.fine-id     { color: #aaa; font-size: 12px; font-family: monospace; }
.user-name   { font-weight: 600; }
.user-code   { font-size: 12px; color: #999; font-family: monospace; margin-top: 2px; }
.book-title  { color: #333; font-weight: 500; }
.book-barcode { font-size: 12px; color: #999; font-family: monospace; margin-top: 2px; }
.reason-text { font-size: 13px; color: #555; }
.note-text   { font-size: 12px; color: #999; margin-top: 2px; font-style: italic; }
.amount-text { font-weight: 700; color: #c62828; font-size: 14px; }
.waive-note  { font-size: 12px; color: #666; font-style: italic; margin-top: 4px; }
.empty-row   { text-align: center; color: #aaa; padding: 40px; }

.status-badge {
  display: inline-block; padding: 3px 10px; border-radius: 99px; font-size: 12px; font-weight: 600;
  &.status-orange { background: #fff3e0; color: #e65100; }
  &.status-green  { background: #e8f5e9; color: #2e7d32; }
  &.status-gray   { background: #f5f5f5; color: #757575; }
}

.action-buttons { display: flex; gap: 6px; }
.btn-pay {
  padding: 5px 10px; background: #e8f5e9; color: #2e7d32;
  border: 1.5px solid #a5d6a7; border-radius: 6px; font-size: 12px;
  font-weight: 600; cursor: pointer; white-space: nowrap;
  &:hover { background: #c8e6c9; }
}
.btn-waive {
  padding: 5px 10px; background: #f5f5f5; color: #555;
  border: 1.5px solid #e0e0e0; border-radius: 6px; font-size: 12px;
  font-weight: 600; cursor: pointer; white-space: nowrap;
  &:hover { background: #e0e0e0; }
}
.action-done { color: #ccc; font-size: 13px; }

// Modal
.modal-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,0.45);
  display: flex; align-items: center; justify-content: center; z-index: 1000; padding: 16px;
}
.modal {
  background: #fff; border-radius: 14px; width: 100%;
  max-width: 540px; max-height: 90vh; overflow-y: auto;
  box-shadow: 0 20px 60px rgba(0,0,0,0.2);
  display: block;
  height: unset;
  top: unset;
  left: unset;
}
.modal-sm { max-width: 440px; }
.modal-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 20px 24px 16px; border-bottom: 1px solid #f0f0f0;
  h3 { margin: 0; font-size: 17px; font-weight: 700; }
}
.modal-close {
  background: none; border: none; font-size: 18px; cursor: pointer;
  color: #aaa; padding: 4px 8px; border-radius: 6px;
  &:hover { background: #f0f0f0; }
}
.modal-body { padding: 20px 24px; }
.modal-footer {
  display: flex; justify-content: flex-end; gap: 8px;
  padding: 16px 24px 20px; border-top: 1px solid #f0f0f0;
}

.fine-summary {
  background: #f9f9f9; border-radius: 10px; padding: 14px 16px;
  display: flex; flex-direction: column; gap: 10px; margin-bottom: 16px;
}
.fine-summary-row {
  display: flex; justify-content: space-between; align-items: center;
  font-size: 14px; gap: 12px;
  span:first-child { color: #888; }
  strong { text-align: right; }
  &.amount-row strong { font-size: 18px; }
}

.form-group {
  display: flex; flex-direction: column; gap: 6px;
  label { font-size: 13px; font-weight: 600; color: #444; display: flex; align-items: center; gap: 8px; }
  input, textarea {
    padding: 8px 12px; border: 1.5px solid #e0e0e0; border-radius: 8px;
    font-size: 14px; outline: none; font-family: inherit;
    background: #ffffff;
    color: #333333;
    &:focus { border-color: #3949ab; }
    &.input-error { border-color: #e53935; }
  }
  textarea { resize: vertical; }
}
.optional-tag { font-size: 11px; color: #999; background: #f5f5f5; padding: 1px 6px; border-radius: 99px; font-weight: 400; }
.required    { color: #e53935; }
.field-error { color: #e53935; font-size: 12px; }
.text-red    { color: #c62828; }
.text-orange { color: #e65100; }

.btn {
  display: inline-flex; align-items: center; gap: 6px; padding: 9px 18px;
  border-radius: 8px; font-size: 14px; font-weight: 500; cursor: pointer; border: none; transition: all 0.15s;
  &:disabled { opacity: 0.5; cursor: not-allowed; }
  &.btn-outline  { background: #fff; color: #555; border: 1.5px solid #e0e0e0; &:hover:not(:disabled) { background: #f5f5f5; } }
  &.btn-pay-lg   { background: #2e7d32; color: #fff; &:hover:not(:disabled) { background: #1b5e20; } }
  &.btn-waive-lg { background: #f5f5f5; color: #333; border: 1.5px solid #e0e0e0; &:hover:not(:disabled) { background: #e0e0e0; } }
}

.state-box { padding: 40px; text-align: center; color: #888; font-size: 14px; &.state-error { color: #c62828; } }

.pagination { display: flex; align-items: center; gap: 4px; justify-content: center; }
.page-btn {
  min-width: 34px; height: 34px; padding: 0 8px; border: 1.5px solid #e0e0e0;
  border-radius: 8px; background: #fff; font-size: 14px; cursor: pointer; transition: all 0.15s; color: #333;
  &:hover:not(:disabled) { border-color: #3949ab; color: #3949ab; }
  &.active { background: #3949ab; border-color: #3949ab; color: #fff; font-weight: 700; }
  &:disabled { opacity: 0.4; cursor: not-allowed; }
}
.page-dots { padding: 0 4px; color: #aaa; }
.page-info { margin-left: 8px; font-size: 13px; color: #888; }
</style>