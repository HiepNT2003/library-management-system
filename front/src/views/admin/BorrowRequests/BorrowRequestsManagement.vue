<template>
  <div class="borrow-requests">
    <!-- Header -->
    <div class="page-header">
      <div>
        <h1 class="page-title">Yêu cầu mượn sách</h1>
        <p class="page-desc">Duyệt hoặc từ chối yêu cầu từ bạn đọc</p>
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
      <button class="btn-refresh" @click="fetchData(1)" title="Làm mới">
        <Icon icon="mingcute:refresh-3-line" width="20" height="20" />
      </button>
    </div>

    <!-- Table -->
    <div class="table-wrapper">
      <div v-if="isLoading" class="state-box">Đang tải...</div>
      <div v-else-if="loadError" class="state-box state-error">{{ loadError }}</div>
      <table v-else class="request-table">
        <thead>
          <tr>
            <th>Bạn đọc</th>
            <th>Sách yêu cầu</th>
            <th>Ngày yêu cầu</th>
            <th>Ngày dự kiến lấy</th>
            <th>Trạng thái</th>
            <th>Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="items.length === 0">
            <td colspan="6" class="empty-row">Không có yêu cầu nào</td>
          </tr>
          <tr
            v-for="req in items"
            :key="req.requestId"
            class="request-row"
            @click="openDetail(req.requestId)"
          >
            <td>
              <div class="user-name">{{ req.user.fullName || "—" }}</div>
              <div class="user-code">
                {{ req.user.studentCode || req.user.staffCode || req.user.email }}
              </div>
            </td>
            <td>
              <div class="book-title" :title="req.book.title">
                {{ truncate(req.book.title, 45) }}
              </div>
              <div class="book-type">{{ docTypeLabel(req.book.documentTypeId) }}</div>
            </td>
            <td>{{ formatDateTime(req.requestDate) }}</td>
            <td>{{ req.expectedBorrowDate ? formatDate(req.expectedBorrowDate) : "—" }}</td>
            <td @click.stop>
              <span class="status-badge" :class="statusClass(req.status)">
                {{ statusLabel(req.status) }}
              </span>
            </td>
            <td @click.stop>
              <div class="action-buttons" v-if="req.status === 0 || req.status === 'Pending'">
                <button class="btn-approve" @click="quickApprove(req)" :disabled="isSubmitting">
                  ✓ Duyệt
                </button>
                <button class="btn-reject" @click="openReject(req)" :disabled="isSubmitting">
                  ✕ Từ chối
                </button>
              </div>
              <button v-else class="btn-view" @click="openDetail(req.requestId)">Xem</button>
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
      <button
        class="page-btn"
        :disabled="currentPage === totalPages"
        @click="goToPage(currentPage + 1)"
      >
        ›
      </button>
      <span class="page-info">{{ total }} yêu cầu</span>
    </div>

    <!-- Modal chi tiết + duyệt/từ chối -->
    <Teleport to="body">
      <div v-if="showDetailModal" class="modal-overlay" @click.self="showDetailModal = false">
        <div class="modal modal-lg">
          <div class="modal-header">
            <h3>Chi tiết yêu cầu #{{ detail?.requestId }}</h3>
            <button class="modal-close" @click="showDetailModal = false">✕</button>
          </div>

          <div v-if="detailLoading" class="modal-loading">Đang tải...</div>
          <template v-else-if="detail">
            <div class="modal-body">
              <div class="detail-grid">
                <!-- Thông tin bạn đọc -->
                <div class="detail-section">
                  <div class="detail-section-title">Bạn đọc</div>
                  <div class="detail-rows">
                    <div class="detail-row">
                      <span class="detail-label">Họ tên</span>
                      <span class="detail-value">{{ detail.user.fullName }}</span>
                    </div>
                    <div class="detail-row">
                      <span class="detail-label">Email</span>
                      <span class="detail-value">{{ detail.user.email }}</span>
                    </div>
                    <div class="detail-row" v-if="detail.user.studentCode">
                      <span class="detail-label">Mã sinh viên</span>
                      <span class="detail-value code-text">{{ detail.user.studentCode }}</span>
                    </div>
                    <div class="detail-row" v-if="detail.user.faculty">
                      <span class="detail-label">Khoa</span>
                      <span class="detail-value">{{ detail.user.faculty }}</span>
                    </div>
                    <div class="detail-row">
                      <span class="detail-label">Đang mượn</span>
                      <span class="detail-value">
                        <span
                          :class="
                            detail.user.borrowingCount > 0 ? 'count-badge count-blue' : 'count-zero'
                          "
                        >
                          {{ detail.user.borrowingCount }} cuốn
                        </span>
                        <span v-if="detail.policy" class="policy-limit"
                          >/ tối đa {{ detail.policy.maxBooks }}</span
                        >
                      </span>
                    </div>
                  </div>
                </div>

                <!-- Thông tin sách -->
                <div class="detail-section">
                  <div class="detail-section-title">Sách yêu cầu</div>
                  <div class="book-preview">
                    <img v-if="detail.book.imageUrl" :src="detail.book.imageUrl" class="book-img" />
                    <div class="book-info">
                      <div class="book-title-lg">{{ detail.book.title }}</div>
                      <div class="book-type-badge">
                        {{ docTypeLabel(detail.book.documentTypeId) }}
                      </div>
                      <div v-if="detail.policy" class="policy-info">
                        Thời hạn mượn: <strong>{{ detail.policy.maxBorrowDays }} ngày</strong>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Thông tin yêu cầu -->
              <div class="request-info">
                <div class="detail-row">
                  <span class="detail-label">Ngày yêu cầu</span>
                  <span class="detail-value">{{ formatDateTime(detail.requestDate) }}</span>
                </div>
                <div class="detail-row" v-if="detail.expectedBorrowDate">
                  <span class="detail-label">Ngày dự kiến lấy</span>
                  <span class="detail-value">{{ formatDate(detail.expectedBorrowDate) }}</span>
                </div>
                <div class="detail-row" v-if="detail.note">
                  <span class="detail-label">Ghi chú</span>
                  <span class="detail-value">{{ detail.note }}</span>
                </div>
                <div class="detail-row" v-if="detail.rejectedReason">
                  <span class="detail-label">Lý do từ chối</span>
                  <span class="detail-value text-red">{{ detail.rejectedReason }}</span>
                </div>
                <div class="detail-row">
                  <span class="detail-label">Trạng thái</span>
                  <span class="status-badge" :class="statusClass(detail.status)">{{
                    statusLabel(detail.status)
                  }}</span>
                </div>
              </div>

              <!-- Cảnh báo giới hạn -->
              <div
                class="warning-box"
                v-if="detail.policy && detail.user.borrowingCount >= detail.policy.maxBooks"
              >
                ⚠️ Bạn đọc đã đạt giới hạn {{ detail.policy.maxBooks }} cuốn đang mượn. Không thể
                duyệt.
              </div>
            </div>

            <!-- Actions -->
            <div class="modal-footer" v-if="detail.status === 0 || detail.status === 'Pending'">
              <button class="btn btn-outline" @click="showDetailModal = false">Đóng</button>
              <button
                class="btn btn-reject-lg"
                @click="openRejectFromDetail"
                :disabled="isSubmitting"
              >
                ✕ Từ chối
              </button>
              <button
                class="btn btn-approve-lg"
                @click="approveFromDetail"
                :disabled="
                  isSubmitting ||
                  (detail.policy && detail.user.borrowingCount >= detail.policy.maxBooks)
                "
              >
                {{ isSubmitting ? "Đang xử lý..." : "✓ Duyệt yêu cầu" }}
              </button>
            </div>
            <div class="modal-footer" v-else>
              <button class="btn btn-outline" @click="showDetailModal = false">Đóng</button>
            </div>
          </template>
        </div>
      </div>
    </Teleport>

    <!-- Modal từ chối -->
    <Teleport to="body">
      <div v-if="showRejectModal" class="modal-overlay" @click.self="showRejectModal = false">
        <div class="modal modal-sm">
          <div class="modal-header">
            <h3>Từ chối yêu cầu</h3>
            <button class="modal-close" @click="showRejectModal = false">✕</button>
          </div>
          <div class="modal-body">
            <p class="reject-book">📚 {{ rejectingReq?.book?.title || detail?.book?.title }}</p>
            <div class="form-group">
              <label>Lý do từ chối <span class="required">*</span></label>
              <textarea
                v-model="rejectReason"
                rows="3"
                placeholder="VD: Sách hiện không có bản sao khả dụng..."
                :class="{ 'input-error': rejectError }"
              ></textarea>
              <span v-if="rejectError" class="field-error">{{ rejectError }}</span>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" @click="showRejectModal = false">Huỷ</button>
            <button class="btn btn-reject-lg" @click="submitReject" :disabled="isSubmitting">
              {{ isSubmitting ? "Đang xử lý..." : "Xác nhận từ chối" }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from "vue"
import api from "../../../services/api"
import { Icon } from "@iconify/vue"

// ---- State ----
const items = ref([])
const isLoading = ref(false)
const loadError = ref("")
const total = ref(0)
const totalPages = ref(1)
const currentPage = ref(1)
const pageSize = 20
const isSubmitting = ref(false)
const statsRaw = ref([])

const showDetailModal = ref(false)
const showRejectModal = ref(false)
const detail = ref(null)
const detailLoading = ref(false)
const rejectingReq = ref(null)
const rejectReason = ref("")
const rejectError = ref("")

const filters = reactive({ search: "", status: "0" }) // mặc định Pending
let searchTimer = null

// ---- Lifecycle ----
onMounted(() => fetchData())

// ---- Fetch ----
const fetchData = async (page = 1) => {
  isLoading.value = true
  loadError.value = ""
  try {
    const params = new URLSearchParams({ page, pageSize })
    if (filters.status !== "") params.append("status", filters.status)
    if (filters.search.trim()) params.append("search", filters.search.trim())

    const res = await api.get(`/BorrowRequests?${params}`)
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

const fetchDetail = async (id) => {
  detailLoading.value = true
  detail.value = null
  try {
    const res = await api.get(`/BorrowRequests/${id}`)
    if (res.status === 200) detail.value = res.data
  } catch (err) {
    detail.value = null
  } finally {
    detailLoading.value = false
  }
}

// ---- Events ----
const onSearchInput = () => {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => fetchData(1), 400)
}
const goToPage = (page) => {
  if (page >= 1 && page <= totalPages.value) fetchData(page)
}
const setStatusFilter = (val) => {
  filters.status = filters.status === val ? "" : val
  fetchData(1)
}

// ---- Detail modal ----
const openDetail = async (id) => {
  showDetailModal.value = true
  await fetchDetail(id)
}

// ---- Approve ----
const quickApprove = async (req) => {
  isSubmitting.value = true
  try {
    const res = await api.patch(`/BorrowRequests/${req.requestId}/approve`)
    if (res.status === 200) {
      req.status = 1 // Approved
      await fetchData(currentPage.value)
    }
  } catch (err) {
    alert(err.response?.data?.message || "Duyệt thất bại")
  } finally {
    isSubmitting.value = false
  }
}

const approveFromDetail = async () => {
  if (!detail.value) return
  isSubmitting.value = true
  try {
    const res = await api.patch(`/BorrowRequests/${detail.value.requestId}/approve`)
    if (res.status === 200) {
      detail.value.status = 1
      showDetailModal.value = false
      await fetchData(currentPage.value)
    }
  } catch (err) {
    alert(err.response?.data?.message || "Duyệt thất bại")
  } finally {
    isSubmitting.value = false
  }
}

// ---- Reject ----
const openReject = (req) => {
  rejectingReq.value = req
  rejectReason.value = ""
  rejectError.value = ""
  showRejectModal.value = true
}

const openRejectFromDetail = () => {
  rejectingReq.value = null
  rejectReason.value = ""
  rejectError.value = ""
  showDetailModal.value = false
  showRejectModal.value = true
}

const submitReject = async () => {
  if (!rejectReason.value.trim()) {
    rejectError.value = "Vui lòng nhập lý do từ chối"
    return
  }
  isSubmitting.value = true
  try {
    const id = rejectingReq.value?.requestId ?? detail.value?.requestId
    const res = await api.patch(`/BorrowRequests/${id}/reject`, { reason: rejectReason.value })
    if (res.status === 200) {
      showRejectModal.value = false
      showDetailModal.value = false
      await fetchData(currentPage.value)
    }
  } catch (err) {
    alert(err.response?.data?.message || "Từ chối thất bại")
  } finally {
    isSubmitting.value = false
  }
}

// ---- Helpers ----
const statusLabel = (status) => {
  const map = {
    0: "Chờ duyệt",
    1: "Đã duyệt",
    2: "Từ chối",
    3: "Đã huỷ",
    4: "Đã hoàn thành",
    Pending: "Chờ duyệt",
    Approved: "Đã duyệt",
    Rejected: "Từ chối",
    Cancelled: "Đã huỷ",
    Completed: "Đã hoàn thành",
  }
  return map[status] ?? status
}

const statusClass = (status) => {
  const map = {
    0: "status-yellow",
    1: "status-green",
    2: "status-red",
    3: "status-gray",
    4: "status-blue",
    Pending: "status-yellow",
    Approved: "status-green",
    Rejected: "status-red",
    Cancelled: "status-gray",
    Completed: "status-blue",
  }
  return map[status] ?? ""
}

const docTypeLabel = (id) => {
  const map = { 1: "Sách vật lý", 2: "Bài trích", 3: "Luận án", 4: "Ebook" }
  return map[id] ?? "—"
}

const formatDate = (d) => (d ? new Date(d).toLocaleDateString("vi-VN") : "—")
const formatDateTime = (d) => (d ? new Date(d).toLocaleString("vi-VN") : "—")
const truncate = (str, len) => (!str ? "—" : str.length > len ? str.slice(0, len) + "..." : str)

const statsDisplay = computed(() => {
  const get = (s) => statsRaw.value.find((x) => x.status === s)?.count ?? 0
  return [
    { value: "0", label: "Chờ duyệt", count: get("Pending"), color: "text-yellow" },
    { value: "1", label: "Đã duyệt", count: get("Approved"), color: "text-green" },
    { value: "2", label: "Từ chối", count: get("Rejected"), color: "text-red" },
    { value: "3", label: "Đã huỷ", count: get("Cancelled"), color: "text-gray" },
    { value: "4", label: "Đã hoàn thành", count: get("Completed"), color: "text-blue" },
  ]
})

const visiblePages = computed(() => {
  const pages = []
  const t = totalPages.value
  const cur = currentPage.value
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
.borrow-requests {
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

// Stats
.stats-row {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
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
    border-color: #3949ab;
  }
  &.active {
    border-color: #3949ab;
    background: #f0f4ff;
  }
}
.stat-num {
  font-size: 28px;
  font-weight: 800;
  line-height: 1;
  &.text-yellow {
    color: #f57f17;
  }
  &.text-green {
    color: #2e7d32;
  }
  &.text-red {
    color: #c62828;
  }
  &.text-gray {
    color: #9e9e9e;
  }
  &.text-blue {
    color: #1565c0;
  }
}
.stat-label {
  font-size: 12px;
  color: #888;
  margin-top: 4px;
  font-weight: 500;
}

// Filters
.filter-bar {
  display: flex;
  gap: 10px;
  align-items: center;
}
.search-input {
  flex: 1;
  padding: 9px 14px;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  font-size: 14px;
  outline: none;
  font-family: inherit;
  &:focus {
    border-color: #3949ab;
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
    border-color: #3949ab;
  }
}

// Table
.table-wrapper {
  overflow-x: auto;
  border-radius: 10px;
  border: 1px solid #e0e0e0;
}
.request-table {
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
.request-row {
  cursor: pointer;
  &:last-child td {
    border-bottom: none;
  }
  &:hover {
    background: #fafafa;
  }
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
  font-weight: 500;
}
.book-type {
  font-size: 12px;
  color: #999;
  margin-top: 2px;
}
.empty-row {
  text-align: center;
  color: #aaa;
  padding: 40px;
}

// Status badges
.status-badge {
  display: inline-block;
  padding: 3px 10px;
  border-radius: 99px;
  font-size: 12px;
  font-weight: 600;
  &.status-yellow {
    background: #fff8e1;
    color: #f57f17;
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
  &.status-blue {
    background: #e3f2fd;
    color: #1565c0;
  }
}

// Action buttons in table
.action-buttons {
  display: flex;
  gap: 6px;
}
.btn-approve {
  padding: 5px 12px;
  background: #e8f5e9;
  color: #2e7d32;
  border: 1.5px solid #a5d6a7;
  border-radius: 6px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  white-space: nowrap;
  &:hover:not(:disabled) {
    background: #c8e6c9;
  }
  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}
.btn-reject {
  padding: 5px 12px;
  background: #ffebee;
  color: #c62828;
  border: 1.5px solid #ef9a9a;
  border-radius: 6px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  white-space: nowrap;
  &:hover:not(:disabled) {
    background: #ffcdd2;
  }
  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}
.btn-view {
  padding: 5px 12px;
  background: #f5f5f5;
  color: #555;
  border: 1.5px solid #e0e0e0;
  border-radius: 6px;
  font-size: 13px;
  cursor: pointer;
  &:hover {
    background: #e0e0e0;
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
  max-width: 680px;
}
.modal-sm {
  max-width: 420px;
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
  textarea {
    background: transparent;
    color: #333333;
  }
}
.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  align-items: center;
  padding: 16px 24px 20px;
  border-top: 1px solid #f0f0f0;
}

// Detail grid
.detail-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
  margin-bottom: 16px;
}
.detail-section {
}
.detail-section-title {
  font-size: 12px;
  font-weight: 700;
  color: #3949ab;
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
}
.detail-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 8px;
}
.detail-label {
  font-size: 13px;
  color: #888;
  white-space: nowrap;
}
.detail-value {
  font-size: 13px;
  font-weight: 500;
  text-align: right;
}
.code-text {
  font-family: monospace;
  color: #3949ab;
  font-weight: 600;
}

.book-preview {
  display: flex;
  gap: 12px;
  align-items: flex-start;
}
.book-img {
  width: 56px;
  height: 72px;
  object-fit: cover;
  border-radius: 6px;
  border: 1px solid #e0e0e0;
  flex-shrink: 0;
}
.book-title-lg {
  font-size: 14px;
  font-weight: 700;
  color: #1a1a2e;
  margin-bottom: 6px;
  line-height: 1.4;
}
.book-type-badge {
  display: inline-block;
  padding: 2px 8px;
  background: #e8eaf6;
  color: #3949ab;
  border-radius: 99px;
  font-size: 11px;
  font-weight: 600;
  margin-bottom: 6px;
}
.policy-info {
  font-size: 12px;
  color: #666;
}

.request-info {
  background: #f9f9f9;
  border-radius: 10px;
  padding: 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 12px;
}

.policy-limit {
  font-size: 12px;
  color: #999;
  margin-left: 4px;
}
.count-badge {
  display: inline-block;
  padding: 1px 8px;
  border-radius: 99px;
  font-size: 12px;
  font-weight: 700;
  &.count-blue {
    background: #e3f2fd;
    color: #1565c0;
  }
}
.count-zero {
  color: #ccc;
}

.warning-box {
  background: #fff3e0;
  border-left: 3px solid #fb8c00;
  border-radius: 0 8px 8px 0;
  padding: 10px 14px;
  font-size: 13px;
  color: #e65100;
}
.text-red {
  color: #c62828;
}

// Reject form
.reject-book {
  font-size: 14px;
  font-weight: 600;
  color: #333;
  background: #f5f5f5;
  padding: 10px 14px;
  border-radius: 8px;
  margin-bottom: 16px;
}
.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
  label {
    font-size: 13px;
    font-weight: 600;
    color: #444;
  }
  textarea {
    padding: 8px 12px;
    border: 1.5px solid #e0e0e0;
    border-radius: 8px;
    font-size: 14px;
    outline: none;
    font-family: inherit;
    resize: vertical;
    &:focus {
      border-color: #3949ab;
    }
    &.input-error {
      border-color: #e53935;
    }
  }
}
.required {
  color: #e53935;
}
.field-error {
  color: #e53935;
  font-size: 12px;
}

// Big action buttons in modal footer
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
  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}
.btn-outline {
  background: #fff;
  color: #555;
  border: 1.5px solid #e0e0e0;
  &:hover:not(:disabled) {
    background: #f5f5f5;
  }
}
.btn-approve-lg {
  background: #2e7d32;
  color: #fff;
  &:hover:not(:disabled) {
    background: #1b5e20;
  }
}
.btn-reject-lg {
  background: #ffebee;
  color: #c62828;
  border: 1.5px solid #ef9a9a;
  &:hover:not(:disabled) {
    background: #ffcdd2;
  }
}

// State
.state-box {
  padding: 40px;
  text-align: center;
  color: #888;
  font-size: 14px;
  &.state-error {
    color: #c62828;
  }
}

// Pagination
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
    border-color: #3949ab;
    color: #3949ab;
  }
  &.active {
    background: #3949ab;
    border-color: #3949ab;
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