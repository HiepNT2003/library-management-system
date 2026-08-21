<template>
  <div class="transaction-list">
    <!-- Header -->
    <div class="page-header">
      <div>
        <h1 class="page-title">Quản lý mượn trả</h1>
        <p class="page-desc">Danh sách tất cả giao dịch mượn sách</p>
      </div>
      <button class="btn btn-primary" @click="goToReturn">
        <Icon class="icon_return" icon="lsicon:sales-return-outline" width="16" height="16" /> Trả
        sách
      </button>
    </div>

    <!-- Stats -->
    <div class="stats-row">
      <div v-for="s in statsDisplay" :key="s.value" class="stat-card" :class="{ active: filters.status === s.value }"
        @click="setStatusFilter(s.value)">
        <div class="stat-num" :class="s.color">{{ s.count }}</div>
        <div class="stat-label">{{ s.label }}</div>
      </div>
    </div>

    <!-- Filters -->
    <div class="filter-bar">
      <input v-model="filters.search" class="search-input" placeholder="Tìm tên bạn đọc, mã SV, barcode, tên sách..."
        @input="onSearchInput" />
      <label class="overdue-check">
        <input type="checkbox" class="ms-checkbox" v-model="filters.overdueOnly" @change="onFilterChange" />
        <span :class="{ selected: filters.overdueOnly }" class="ms-checkbox-custom"></span>
        Chỉ xem quá hạn
      </label>
      <button class="btn-refresh" @click="fetchData(1)" title="Làm mới">
        <Icon icon="mingcute:refresh-3-line" width="20" height="20" />
      </button>
    </div>

    <!-- Table -->
    <div class="table-wrapper">
      <div v-if="isLoading" class="state-box">Đang tải...</div>
      <div v-else-if="loadError" class="state-box state-error">{{ loadError }}</div>
      <table v-else class="tx-table">
        <thead>
          <tr>
            <th>#</th>
            <th>Bạn đọc</th>
            <th>Sách</th>
            <th>Barcode</th>
            <th>Ngày mượn</th>
            <th>Hạn trả</th>
            <th>Ngày trả</th>
            <th>Trạng thái</th>
            <th>Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="items.length === 0">
            <td colspan="9" class="empty-row">Không có giao dịch nào</td>
          </tr>
          <tr v-for="tx in items" :key="tx.transactionId" class="tx-row" :class="{ 'row-overdue': tx.isOverdue }">
            <td class="tx-id">#{{ tx.transactionId }}</td>
            <td>
              <div class="user-name">{{ tx.user?.fullName || "—" }}</div>
              <div class="user-code">{{ tx.user?.studentCode || "" }}</div>
            </td>
            <td>
              <div class="book-title" :title="tx.copy?.bookTitle">
                {{ truncate(tx.copy?.bookTitle, 40) }}
              </div>
            </td>
            <td>
              <span class="code-text">{{ tx.copy?.barcode || "—" }}</span>
            </td>
            <td>{{ formatDate(tx.borrowDate) }}</td>
            <td :class="{ 'text-red': tx.isOverdue }">
              {{ formatDate(tx.dueDate) }}
              <div v-if="tx.isOverdue" class="overdue-days">+{{ tx.overdueDays }} ngày</div>
            </td>
            <td>{{ tx.returnDate ? formatDate(tx.returnDate) : "—" }}</td>
            <td>
              <span class="status-badge" :class="txStatusClass(tx.status, tx.isOverdue)">
                {{ txStatusLabel(tx.status, tx.isOverdue) }}
              </span>
            </td>
            <td>
              <div class="action-buttons">
                <button class="action-btn" @click="openDetail(tx)" title="Chi tiết">
                  <Icon icon="carbon:task-view" width="16" height="16" />
                </button>
                <button v-if="
                  tx.status === 'Borrowed' ||
                  tx.status === 'Overdue' ||
                  tx.status === 0 ||
                  tx.status === 2
                " class="action-btn action-return" @click="goToReturn(tx)" title="Xử lý trả">
                  <Icon icon="lsicon:sales-return-outline" width="16" height="16" />
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    <div class="pagination" v-if="totalPages > 1">
      <button class="page-btn" :disabled="currentPage === 1" @click="goToPage(currentPage - 1)">
        ‹
      </button>
      <template v-for="p in visiblePages" :key="p">
        <span v-if="p === '...'" class="page-dots">...</span>
        <button v-else class="page-btn" :class="{ active: p === currentPage }" @click="goToPage(p)">
          {{ p }}
        </button>
      </template>
      <button class="page-btn" :disabled="currentPage === totalPages" @click="goToPage(currentPage + 1)">
        ›
      </button>
      <span class="page-info">{{ total }} giao dịch</span>
    </div>

    <!-- Modal chi tiết -->
    <Teleport to="body">
      <div v-if="showDetail" class="modal-overlay" @click.self="showDetail = false">
        <div class="modal modal-lg">
          <div class="modal-header">
            <h3>Chi tiết giao dịch #{{ detail?.transactionId }}</h3>
            <button class="modal-close" @click="showDetail = false">✕</button>
          </div>
          <div v-if="detailLoading" class="modal-loading">Đang tải...</div>
          <template v-else-if="detail">
            <div class="modal-body">
              <div class="detail-grid">
                <div class="detail-section">
                  <div class="detail-section-title">Bạn đọc</div>
                  <div class="detail-rows">
                    <div class="detail-row">
                      <span class="detail-label">Họ tên</span>
                      <span class="detail-value">{{ detail.user?.fullName }}</span>
                    </div>
                    <div class="detail-row">
                      <span class="detail-label">Mã SV</span>
                      <span class="detail-value code-text">{{
                        detail.user?.studentCode || "—"
                      }}</span>
                    </div>
                    <div class="detail-row">
                      <span class="detail-label">Email</span>
                      <span class="detail-value">{{ detail.user?.email }}</span>
                    </div>
                  </div>
                </div>
                <div class="detail-section">
                  <div class="detail-section-title">Sách</div>
                  <div class="detail-rows">
                    <div class="detail-row">
                      <span class="detail-label">Tên sách</span>
                      <span class="detail-value">{{ detail.copy?.bookTitle }}</span>
                    </div>
                    <div class="detail-row">
                      <span class="detail-label">Barcode</span>
                      <span class="detail-value code-text">{{ detail.copy?.barcode }}</span>
                    </div>
                    <div class="detail-row">
                      <span class="detail-label">Vị trí kệ</span>
                      <span class="detail-value">{{ detail.copy?.shelfLocation || "—" }}</span>
                    </div>
                  </div>
                </div>
              </div>

              <div class="detail-info-row">
                <div class="info-item">
                  <span class="info-label">Ngày mượn</span>
                  <span class="info-value">{{ formatDate(detail.borrowDate) }}</span>
                </div>
                <div class="info-item">
                  <span class="info-label">Hạn trả</span>
                  <span class="info-value" :class="{ 'text-red': detail.overdueDays > 0 }">
                    {{ formatDate(detail.dueDate) }}
                  </span>
                </div>
                <div class="info-item" v-if="detail.returnDate">
                  <span class="info-label">Ngày trả</span>
                  <span class="info-value">{{ formatDate(detail.returnDate) }}</span>
                </div>
                <div class="info-item" v-if="detail.overdueDays > 0">
                  <span class="info-label">Quá hạn</span>
                  <span class="info-value text-red">{{ detail.overdueDays }} ngày</span>
                </div>
                <div class="info-item" v-if="detail.returnCondition">
                  <span class="info-label">Tình trạng trả</span>
                  <span class="info-value">{{ detail.returnCondition }}</span>
                </div>
              </div>

              <!-- Fines -->
              <div v-if="detail.fines?.length > 0" class="fines-section">
                <div class="fines-title">Tiền phạt</div>
                <div v-for="f in detail.fines" :key="f.fineId" class="fine-item">
                  <div class="fine-reason">{{ f.reason }}</div>
                  <div class="fine-amount">{{ formatMoney(f.amount) }}</div>
                  <span class="fine-status" :class="fineStatusClass(f.status)">
                    {{ fineStatusLabel(f.status) }}
                  </span>
                </div>
              </div>

              <!-- Estimated fine -->
              <div class="estimated-fine" v-if="detail.estimatedFine > 0 && detail.status !== 'Returned'">
                ⚠️ Phạt dự kiến: <strong>{{ formatMoney(detail.estimatedFine) }}</strong> ({{
                  detail.overdueDays
                }}
                ngày × {{ formatMoney(detail.finePerDay) }}/ngày)
              </div>
            </div>
            <div class="modal-footer">
              <button class="btn btn-outline" @click="showDetail = false">Đóng</button>
              <button v-if="
                detail.status === 'Borrowed' ||
                detail.status === 'Overdue' ||
                detail.status === 0 ||
                detail.status === 2
              " class="btn btn-primary" @click="goToReturn(detail)">
                <Icon icon="lsicon:sales-return-outline" width="16" height="16" /> Xử lý trả sách
              </button>
              <button v-if="detail.status === 'Borrowed' || detail.status === 'Overdue' ||
                detail.status === 0 || detail.status === 2" class="btn btn-extend" @click="extendFromDetail"
                :disabled="isExtending">
                {{ isExtending ? 'Đang gia hạn...' : '📅 Gia hạn' }}
              </button>
            </div>
          </template>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from "vue"
import { useRoute, useRouter } from "vue-router"
import api from "../../../services/api"
import { Icon } from "@iconify/vue"

const router = useRouter()
const route = useRoute()

const items = ref([])
const isLoading = ref(false)
const loadError = ref("")
const total = ref(0)
const totalPages = ref(1)
const currentPage = ref(1)
const pageSize = 20
const statsRaw = ref([])

const showDetail = ref(false)
const detailLoading = ref(false)
const detail = ref(null)

const filters = reactive({ search: "", status: "", overdueOnly: false })
let searchTimer = null

onMounted(() => {
  if (route?.query?.status) {
    filters.status = route.query.status
  }
  fetchData()
})

const fetchData = async (page = 1) => {
  isLoading.value = true
  loadError.value = ""
  try {
    const params = new URLSearchParams({ page, pageSize })
    if (filters.status) params.append("status", filters.status)
    if (filters.search.trim()) params.append("search", filters.search.trim())
    if (filters.overdueOnly) params.append("overdueOnly", "true")

    const res = await api.get(`/Transactions?${params}`)
    if (res.status === 200) {
      items.value = res.data.items
      total.value = res.data.total
      totalPages.value = res.data.totalPages
      currentPage.value = res.data.page
      statsRaw.value = res.data.stats || []
    }
  } catch (err) {
    loadError.value = err.response?.data?.message || "Không thể tải dữ liệu"
  } finally {
    isLoading.value = false
  }
}

const openDetail = async (tx) => {
  showDetail.value = true
  detailLoading.value = true
  detail.value = null
  try {
    const res = await api.get(`/Transactions/${tx.transactionId}`)
    if (res.status === 200) detail.value = res.data
  } catch {
  } finally {
    detailLoading.value = false
  }
}

const isExtending = ref(false)

const extendFromDetail = async () => {
  if (!detail.value) return
  isExtending.value = true
  try {
    const res = await api.post(`/Transactions/${detail.value.transactionId}/extend`)
    if (res.status === 200) {
      alert(`Gia hạn thành công! Hạn mới: ${new Date(res.data.newDueDate).toLocaleDateString('vi-VN')}`)
      showDetail.value = false
      await fetchData(currentPage.value)
    }
  } catch (err) {
    alert(err.response?.data?.message || 'Gia hạn thất bại')
  } finally {
    isExtending.value = false
  }
}

const goToReturn = (tx) => {
  showDetail.value = false
  router.push({
    name: "returnBook",
    query: tx ? { barcode: tx.copy?.barcode } : null,
  })
}

const onSearchInput = () => {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => fetchData(1), 400)
}
const onFilterChange = () => fetchData(1)
const setStatusFilter = (val) => {
  filters.status = filters.status === val ? "" : val
  fetchData(1)
}
const goToPage = (page) => {
  if (page >= 1 && page <= totalPages.value) fetchData(page)
}

// Helpers
const txStatusLabel = (status, isOverdue) => {
  if (isOverdue) return "Quá hạn"
  const map = {
    Borrowed: "Đang mượn",
    Returned: "Đã trả",
    Overdue: "Quá hạn",
    Cancelled: "Đã huỷ",
    0: "Đang mượn",
    1: "Đã trả",
    2: "Quá hạn",
    3: "Đã huỷ",
  }
  return map[status] ?? status
}
const txStatusClass = (status, isOverdue) => {
  if (isOverdue || status === "Overdue" || status === 2) return "status-red"
  const map = {
    Borrowed: "status-blue",
    Returned: "status-green",
    Cancelled: "status-gray",
    0: "status-blue",
    1: "status-green",
    3: "status-gray",
  }
  return map[status] ?? ""
}
const fineStatusLabel = (s) =>
({ Pending: "Chưa thu", Paid: "Đã thu", Waived: "Miễn", 0: "Chưa thu", 1: "Đã thu", 2: "Miễn" }[
  s
] ?? s)
const fineStatusClass = (s) =>
({
  Pending: "fine-pending",
  Paid: "fine-paid",
  Waived: "fine-waived",
  0: "fine-pending",
  1: "fine-paid",
  2: "fine-waived",
}[s] ?? "")
const formatDate = (d) => (d ? new Date(d).toLocaleDateString("vi-VN") : "—")
const formatMoney = (n) =>
  n ? new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(n) : "0đ"
const truncate = (str, len) => (!str ? "—" : str.length > len ? str.slice(0, len) + "..." : str)

const statsDisplay = computed(() => {
  const get = (s) => statsRaw.value.find((x) => x.status === s)?.count ?? 0
  return [
    { value: "Borrowed", label: "Đang mượn", count: get("Borrowed"), color: "text-blue" },
    { value: "Overdue", label: "Quá hạn", count: get("Overdue"), color: "text-red" },
    { value: "Returned", label: "Đã trả", count: get("Returned"), color: "text-green" },
  ]
})

const visiblePages = computed(() => {
  const pages = []
  const t = totalPages.value,
    cur = currentPage.value
  if (t <= 7) {
    for (let i = 1; i <= t; i++) pages.push(i)
  } else {
    pages.push(1)
    if (cur > 3) pages.push("...")
    for (let i = Math.max(2, cur - 1); i <= Math.min(t - 1, cur + 1); i++) pages.push(i)
    if (cur < t - 2) pages.push("...")
    pages.push(t)
  }
  return pages
})
</script>

<style lang="scss" scoped>
.transaction-list {
  display: flex;
  flex-direction: column;
  gap: 20px;
  font-family: "Segoe UI", sans-serif;
  color: #1a1a2e;
  padding: 16px 24px;
  background: #ffffff;
  border-radius: 12px;
}

.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.page-title {
  font-size: 22px;
  font-weight: 800;
  margin: 0 0 4px;
}

.page-desc {
  font-size: 14px;
  color: #666;
  margin: 0;
}

.icon_return {
  margin-bottom: 2px;
}

.stats-row {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 12px;
}

.stat-card {
  background: #fff;
  border-radius: 12px;
  border: 1.5px solid #e0e0e0;
  padding: 16px 20px;
  cursor: pointer;
  transition: all 0.15s;

  &:hover {
    border-color: #435ebe;
  }

  &.active {
    border-color: #435ebe;
    background: #f0f4ff;
  }
}

.stat-num {
  font-size: 28px;
  font-weight: 800;
  line-height: 1;

  &.text-blue {
    color: #1565c0;
  }

  &.text-red {
    color: #c62828;
  }

  &.text-green {
    color: #2e7d32;
  }

  &.text-gray {
    color: #9e9e9e;
  }
}

.stat-label {
  font-size: 12px;
  color: #888;
  margin-top: 4px;
}

.filter-bar {
  display: flex;
  gap: 10px;
  align-items: center;
  flex-wrap: wrap;
}

.search-input {
  flex: 1;
  min-width: 220px;
  padding: 9px 14px;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  font-size: 14px;
  outline: none;
  font-family: inherit;

  &:focus {
    border-color: #435ebe;
  }
}

.overdue-check {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  cursor: pointer;
  white-space: nowrap;

  input {
    cursor: pointer;
  }

  .ms-checkbox {
    display: none;
  }

  .ms-checkbox-custom {
    width: 18px;
    height: 18px;
    border: 1.5px solid #d1d5db;
    border-radius: 5px;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: border-color 0.15s, background 0.15s;
    position: relative;
    margin-bottom: 3px;
  }

  .selected {
    background: #6366f1;
    border-color: #6366f1;
  }

  .selected::after {
    content: "";
    width: 10px;
    height: 7px;
    border-left: 2px solid #fff;
    border-bottom: 2px solid #fff;
    transform: rotate(-45deg) translate(1px, -1px);
  }
}

.btn-refresh {
  padding: 8px 10px;
  background: #fff;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  cursor: pointer;
  font-size: 16px;
  color: #333333;

  &:hover {
    border-color: #435ebe;
  }
}

.table-wrapper {
  overflow-x: auto;
  border-radius: 10px;
  border: 1px solid #e0e0e0;
}

.btn-extend {
  background: #e8eaf6;
  color: #3949ab;
  border: 1.5px solid #c5cae9;

  &:hover:not(:disabled) {
    background: #c5cae9;
  }
}

.tx-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 14px;

  thead tr {
    background: #f5f5f5;
  }

  th {
    padding: 10px 14px;
    text-align: left;
    font-weight: 600;
    color: #555;
    white-space: nowrap;
    border-bottom: 1px solid #e0e0e0;
  }

  td {
    padding: 10px 14px;
    border-bottom: 1px solid #f0f0f0;
    vertical-align: middle;
  }
}

.tx-row {
  &:last-child td {
    border-bottom: none;
  }

  &:hover {
    background: #fafafa;
  }

  &.row-overdue {
    background: #fff8f8;
  }
}

.tx-id {
  color: #aaa;
  font-size: 12px;
  font-family: monospace;
}

.user-name {
  font-weight: 600;
}

.user-code {
  font-size: 12px;
  color: #999;
  font-family: monospace;
  margin-top: 2px;
}

.book-title {
  color: #333;
}

.code-text {
  font-family: monospace;
  color: #435ebe;
  font-weight: 600;
  font-size: 13px;
}

.text-red {
  color: #c62828;
  font-weight: 600;
}

.overdue-days {
  font-size: 11px;
  color: #e53935;
  font-weight: 600;
  margin-top: 2px;
}

.empty-row {
  text-align: center;
  color: #aaa;
  padding: 40px;
}

.status-badge {
  display: inline-block;
  padding: 3px 10px;
  border-radius: 99px;
  font-size: 12px;
  font-weight: 600;

  &.status-blue {
    background: #e3f2fd;
    color: #1565c0;
  }

  &.status-green {
    background: #e8f5e9;
    color: #2e7d32;
  }

  &.status-red {
    background: #ffebee;
    color: #c62828;
  }

  &.status-gray {
    background: #f5f5f5;
    color: #757575;
  }
}

.action-buttons {
  display: flex;
  gap: 4px;
}

.action-btn {
  background: none;
  border: none;
  cursor: pointer;
  padding: 4px 6px;
  border-radius: 6px;
  font-size: 15px;
  transition: background 0.15s;
  color: #435ebe;

  &:hover {
    background: #f0f0f0;
  }

  &.action-return:hover {
    background: #e8f5e9;
  }
}

// Modal
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 16px;
}

.modal {
  background: #fff;
  border-radius: 14px;
  width: 100%;
  max-width: 540px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.2);
  display: block;
  height: unset;
  top: unset;
  left: unset;
}

.modal-lg {
  max-width: 640px;
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 24px 16px;
  border-bottom: 1px solid #f0f0f0;

  h3 {
    margin: 0;
    font-size: 17px;
    font-weight: 700;
  }
}

.modal-close {
  background: none;
  border: none;
  font-size: 18px;
  cursor: pointer;
  color: #aaa;
  padding: 4px 8px;
  border-radius: 6px;

  &:hover {
    background: #f0f0f0;
  }
}

.modal-loading {
  padding: 40px;
  text-align: center;
  color: #888;
}

.modal-body {
  padding: 20px 24px;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding: 16px 24px 20px;
  border-top: 1px solid #f0f0f0;
}

.detail-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
  margin-bottom: 16px;
}

.detail-section-title {
  font-size: 12px;
  font-weight: 700;
  color: #435ebe;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 10px;
  padding-bottom: 6px;
  border-bottom: 1.5px solid #e8eaf6;
}

.detail-rows {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 4px 8px;
}

.detail-row {
  display: flex;
  justify-content: space-between;
  gap: 8px;
}

.detail-label {
  font-size: 13px;
  color: #888;
}

.detail-value {
  font-size: 13px;
  font-weight: 500;
  text-align: right;
}

.detail-info-row {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
  background: #f9f9f9;
  border-radius: 10px;
  padding: 14px 16px;
  margin-bottom: 14px;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 3px;
  min-width: 100px;
  flex: 1;
}

.info-label {
  font-size: 11px;
  color: #999;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.info-value {
  font-size: 14px;
  font-weight: 600;
}

.fines-section {
  margin-bottom: 14px;
}

.fines-title {
  font-size: 13px;
  font-weight: 700;
  color: #c62828;
  margin-bottom: 8px;
}

.fine-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 12px;
  background: #fff8f8;
  border-radius: 8px;
  margin-bottom: 6px;
  font-size: 13px;
}

.fine-reason {
  flex: 1;
  color: #555;
}

.fine-amount {
  font-weight: 700;
  color: #c62828;
}

.fine-status {
  padding: 2px 8px;
  border-radius: 99px;
  font-size: 11px;
  font-weight: 600;

  &.fine-pending {
    background: #fff3e0;
    color: #e65100;
  }

  &.fine-paid {
    background: #e8f5e9;
    color: #2e7d32;
  }

  &.fine-waived {
    background: #f5f5f5;
    color: #757575;
  }
}

.estimated-fine {
  padding: 10px 14px;
  background: #fff3e0;
  border-left: 3px solid #fb8c00;
  border-radius: 0 8px 8px 0;
  font-size: 13px;
  color: #e65100;
}

.btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 9px 18px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  border: none;
  transition: all 0.15s;

  &.btn-primary {
    background: #435ebe;
    color: #fff;

    &:hover {
      background: #2c3a8c;
    }
  }

  &.btn-outline {
    background: #fff;
    color: #435ebe;
    border: 1.5px solid #435ebe;

    &:hover {
      background: #e8eaf6;
    }
  }
}

.state-box {
  padding: 40px;
  text-align: center;
  color: #888;
  font-size: 14px;

  &.state-error {
    color: #c62828;
  }
}

.pagination {
  display: flex;
  align-items: center;
  gap: 4px;
  justify-content: center;
}

.page-btn {
  min-width: 34px;
  height: 34px;
  padding: 0 8px;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  background: #fff;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.15s;
  color: #333;

  &:hover:not(:disabled) {
    border-color: #435ebe;
    color: #435ebe;
  }

  &.active {
    background: #435ebe;
    border-color: #435ebe;
    color: #fff;
    font-weight: 700;
  }

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }
}

.page-dots {
  padding: 0 4px;
  color: #aaa;
}

.page-info {
  margin-left: 8px;
  font-size: 13px;
  color: #888;
}
</style>