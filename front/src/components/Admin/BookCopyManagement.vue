<template>
  <div class="management-page">
    <!-- Stats -->
    <div class="stats-row">
      <div class="stat-card stat-total">
        <div class="stat-value">{{ stats.total }}</div>
        <div class="stat-label">Tổng bản sao</div>
      </div>
      <div class="stat-card stat-available">
        <div class="stat-value">{{ stats.Available ?? 0 }}</div>
        <div class="stat-label">Có thể mượn</div>
      </div>
      <div class="stat-card stat-borrowed">
        <div class="stat-value">{{ stats.Borrowed ?? 0 }}</div>
        <div class="stat-label">Đang mượn</div>
      </div>
      <div class="stat-card stat-damaged">
        <div class="stat-value">{{ stats.Damaged ?? 0 }}</div>
        <div class="stat-label">Hư hỏng</div>
      </div>
      <div class="stat-card stat-lost">
        <div class="stat-value">{{ stats.Lost ?? 0 }}</div>
        <div class="stat-label">Mất</div>
      </div>
    </div>

    <!-- Filters -->
    <div class="filter-bar">
      <input
        v-model="filters.search"
        class="search-input"
        placeholder="Tìm barcode hoặc tên sách..."
        @input="onSearchInput"
      />
      <select v-model="filters.warehouseId" class="filter-select" @change="onFilterChange">
        <option :value="null">Tất cả phòng</option>
        <option
          v-for="warehouse in warehouses"
          :key="warehouse.warehouseId"
          :value="warehouse.warehouseId"
        >
          {{ warehouse.name }}
        </option>
      </select>
      <select v-model="filters.status" class="filter-select" @change="onFilterChange">
        <option value="">Tất cả trạng thái</option>
        <option value="Available">Có sẵn</option>
        <option value="Borrowed">Đang mượn</option>
        <option value="Damaged">Hư hỏng</option>
        <option value="Lost">Mất</option>
      </select>
      <button class="btn-import" @click="importBookCopies">
        <Icon class="icon_import" icon="lets-icons:arhive-import" width="14" height="14" />
        Nhập hàng loạt
      </button>
      <button class="btn-export" @click="exportExcel" :disabled="isExporting">
        <Icon
          v-if="!isExporting"
          class="icon_excel"
          icon="file-icons:microsoft-excel"
          width="14"
          height="14"
        />
        {{ isExporting ? "Đang xuất..." : "Xuất Excel" }}
      </button>
    </div>

    <!-- Table -->
    <div class="table-wrapper">
      <div v-if="isLoading" class="state-box">Đang tải...</div>
      <div v-else-if="loadError" class="state-box state-error">{{ loadError }}</div>
      <table v-else class="copy-table">
        <thead>
          <tr>
            <th>Barcode</th>
            <th>Tên sách</th>
            <th>Phòng</th>
            <th>Vị trí kệ</th>
            <th>Tình trạng</th>
            <th>Ngày nhập</th>
            <th>Trạng thái</th>
            <th>Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="items.length === 0">
            <td colspan="7" class="empty-row">Không có bản sao nào</td>
          </tr>
          <tr v-for="copy in items" :key="copy.copyId" class="copy-row">
            <td>
              <span class="barcode-text">{{ copy.barcode || "—" }}</span>
            </td>
            <td>
              <span class="book-title" :title="copy.bookTitle">
                {{ truncate(copy.bookTitle, 40) }}
              </span>
            </td>
            <td>
              <span class="warehouse-tag">{{ copy.warehouseName || "—" }}</span>
            </td>
            <td>{{ copy.shelfLocation || "—" }}</td>
            <td>{{ copy.bookCondition || "—" }}</td>
            <td>{{ formatDate(copy.purchaseDate) }}</td>
            <td>
              <template v-if="allowedTransitions[copy.status]?.length > 0">
                <span v-if="copy.isReferenceOnly" class="status-select" :class="statusClass('Unavailable')">
                  {{ statusLabel('Unavailable') }}
                </span>
                <select
                  v-else
                  class="status-select"
                  :class="statusClass(copy.status)"
                  :value="copy.status"
                  @change="onStatusChange(copy, $event.target.value)"
                  :disabled="updatingId === copy.copyId"
                >
                  <option :value="copy.status">{{ statusLabel(copy.status) }}</option>
                  <option v-for="s in allowedTransitions[copy.status]" :key="s" :value="s">
                    {{ statusLabel(s) }}
                  </option>
                </select>
              </template>
              <template v-else>
                <span class="status-select" :class="statusClass(copy.status)">
                  {{ statusLabel(copy.status) }}
                </span>
              </template>
            </td>
            <td>
              <div class="action-buttons">
                <button class="action-btn" @click="openEdit(copy)" title="Chỉnh sửa">
                  <i class="bi bi-pencil"></i>
                </button>
                <button class="action-btn delete" @click="confirmDelete(copy)" title="Xoá">
                  <i class="bi bi-trash"></i>
                </button>
                <button class="action-btn" @click="openHistory(copy)" title="Lịch sử">
                  <i class="bi bi-clock-history"></i>
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
      <button
        class="page-btn"
        :disabled="currentPage === totalPages"
        @click="goToPage(currentPage + 1)"
      >
        ›
      </button>
      <span class="page-info"
        >{{ (currentPage - 1) * pageSize + 1 }}–{{ Math.min(currentPage * pageSize, total) }} /
        {{ total }}</span
      >
    </div>

    <!-- Modal Edit -->
    <Teleport to="body">
      <div v-if="showEditModal" class="modal-overlay" @click.self="showEditModal = false">
        <div class="modal">
          <div class="modal-header">
            <h3>Chỉnh sửa bản sao</h3>
            <button class="modal-close" @click="showEditModal = false">✕</button>
          </div>
          <div class="modal-body">
            <div class="form-grid">
              <div class="form-group">
                <label>Barcode</label>
                <input v-model="editForm.barcode" />
                <span v-if="editErrors.barcode" class="field-error">{{ editErrors.barcode }}</span>
              </div>
              <div class="form-group">
                <label>Phòng</label>
                <select v-model="editForm.warehouseId">
                  <option :value="null">-- Chọn phòng --</option>
                  <option
                    v-for="warehouse in warehouses"
                    :key="warehouse.roomId"
                    :value="warehouse.warehouseId"
                  >
                    {{ warehouse.name }}
                  </option>
                </select>
              </div>
              <div class="form-group">
                <label>Vị trí kệ</label>
                <input v-model="editForm.shelfLocation" placeholder="VD: A1-03" />
              </div>
              <div class="form-group">
                <label>Tình trạng sách</label>
                <select v-model="editForm.bookCondition">
                  <option value="">-- Chọn --</option>
                  <option value="Mới">Mới</option>
                  <option value="Tốt">Tốt</option>
                  <option value="Bình thường">Bình thường</option>
                  <option value="Cũ">Cũ</option>
                  <option value="Hư hỏng nhẹ">Hư hỏng nhẹ</option>
                </select>
              </div>
              <div class="form-group">
                <label>Ngày nhập</label>
                <input type="date" v-model="editForm.purchaseDate" />
              </div>
              <div class="form-group checkbox-group">
                <label>
                  <input type="checkbox" v-model="editForm.isReferenceOnly" />
                  Chỉ tham khảo
                </label>
              </div>
              <div class="form-group full-width">
                <label>Ghi chú</label>
                <textarea v-model="editForm.notes" rows="2"></textarea>
              </div>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" @click="showEditModal = false">Huỷ</button>
            <button class="btn btn-primary" @click="submitEdit" :disabled="isSubmitting">
              {{ isSubmitting ? "Đang lưu..." : "Cập nhật" }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Confirm Delete -->
    <Teleport to="body">
      <div v-if="showDeleteConfirm" class="modal-overlay" @click.self="showDeleteConfirm = false">
        <div class="modal modal-sm">
          <div class="modal-header">
            <h3>Xác nhận xoá</h3>
            <button class="modal-close" @click="showDeleteConfirm = false">✕</button>
          </div>
          <div class="modal-body">
            <p>
              Xoá bản sao <strong>{{ deletingCopy?.barcode }}</strong
              >?
            </p>
            <p class="text-muted">Chỉ xoá được nếu chưa có lịch sử mượn trả.</p>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" @click="showDeleteConfirm = false">Huỷ</button>
            <button class="btn btn-danger" @click="deleteCopy" :disabled="isSubmitting">
              {{ isSubmitting ? "Đang xoá..." : "Xoá" }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
    <!-- Modal lịch sử -->
    <Teleport to="body">
      <div v-if="showHistoryModal" class="modal-overlay" @click.self="showHistoryModal = false">
        <div class="modal modal-lg">
          <div class="modal-header">
            <div>
              <h3>Lịch sử trạng thái</h3>
              <div class="modal-subtitle">{{ historyBarcode }}</div>
            </div>
            <button class="modal-close" @click="showHistoryModal = false">✕</button>
          </div>
          <div class="modal-body">
            <div v-if="historyLoading" class="history-loading">Đang tải...</div>
            <div v-else-if="historyList.length === 0" class="history-empty">
              Chưa có lịch sử thay đổi
            </div>
            <div v-else class="timeline">
              <div
                v-for="(item, idx) in historyList"
                :key="item.id"
                class="timeline-item"
                :class="{ last: idx === historyList.length - 1 }"
              >
                <div class="timeline-dot" :class="historyStatusClass(item.newStatus)"></div>
                <div class="timeline-content">
                  <div class="timeline-top">
                    <div class="timeline-transition">
                      <span
                        v-if="item.oldStatus"
                        class="status-pill"
                        :class="historyStatusClass(item.oldStatus)"
                      >
                        {{ statusLabelHistory(item.oldStatus) }}
                      </span>
                      <span v-if="item.oldStatus" class="arrow">→</span>
                      <span class="status-pill" :class="historyStatusClass(item.newStatus)">
                        {{ statusLabelHistory(item.newStatus) }}
                      </span>
                    </div>
                    <div class="timeline-time">{{ formatDateTime(item.changedAt) }}</div>
                  </div>
                  <div class="timeline-meta">
                    <span class="timeline-by">👤 {{ item.changedBy }}</span>
                    <span v-if="item.reason" class="timeline-reason">"{{ item.reason }}"</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
    <!-- Confirm status change -->
    <Teleport to="body">
      <div v-if="showStatusConfirm" class="modal-overlay" @click.self="showStatusConfirm = false">
        <div class="modal modal-sm">
          <div class="modal-header">
            <h3>Xác nhận thay đổi trạng thái</h3>
            <button class="modal-close" @click="showStatusConfirm = false">✕</button>
          </div>
          <div class="modal-body">
            <div class="status-change-preview">
              <span
                class="status-pill"
                :class="historyStatusClass(pendingStatusChange.copy?.status)"
              >
                {{ statusLabel(pendingStatusChange.copy?.status) }}
              </span>
              <span class="arrow">→</span>
              <span class="status-pill" :class="historyStatusClass(pendingStatusChange.newStatus)">
                {{ statusLabel(pendingStatusChange.newStatus) }}
              </span>
            </div>
            <div class="form-group" style="margin-top: 16px">
              <label
                >Lý do <span style="color: #999; font-weight: 400">(không bắt buộc)</span></label
              >
              <textarea
                v-model="statusChangeReason"
                rows="3"
                placeholder="VD: Sách bị rách bìa, mờ chữ..."
              ></textarea>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" @click="showStatusConfirm = false">Huỷ</button>
            <button class="btn btn-primary" @click="confirmStatusChange">Xác nhận</button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from "vue"
import api from "../../services/api"
import { Icon } from "@iconify/vue"
import { useToastMessageStore } from "../../stores/toastMessage"
import { TOAST_MESSAGE_STATUS } from "../../constants"
import { useRouter } from "vue-router"
import { useAuthStore } from "../../stores/auth"

const authStore = useAuthStore()
const toasMessageStore = useToastMessageStore()
const router = useRouter()

// ---- State ----
const items = ref([])
const warehouses = ref([])
const isLoading = ref(false)
const loadError = ref("")
const isExporting = ref(false)
const updatingId = ref(null)

const total = ref(0)
const totalPages = ref(1)
const currentPage = ref(1)
const pageSize = 20

const statsRaw = ref([])
const stats = computed(() => {
  const map = { total: total.value }
  statsRaw.value.forEach((s) => {
    map[s.status] = s.count
  })
  return map
})

const allowedTransitions = {
  Available: ["Damaged", "Lost"],
  Borrowed: [],
  Damaged: ["Lost"],
  Lost: [],
}
// ---- Edit ----
const showEditModal = ref(false)
const showDeleteConfirm = ref(false)
const editingCopy = ref(null)
const deletingCopy = ref(null)
const isSubmitting = ref(false)
const editErrors = reactive({ barcode: "" })

const editForm = reactive({
  barcode: "",
  shelfLocation: "",
  bookCondition: "",
  purchaseDate: "",
  isReferenceOnly: false,
  notes: "",
  warehouseId: null,
})

const openEdit = (copy) => {
  editingCopy.value = copy
  Object.assign(editForm, {
    barcode: copy.barcode || "",
    shelfLocation: copy.shelfLocation || "",
    bookCondition: copy.bookCondition || "",
    purchaseDate: copy.purchaseDate || "",
    isReferenceOnly: copy.isReferenceOnly || false,
    notes: copy.notes || "",
    warehouseId: copy.warehouseId || null,
  })
  editErrors.barcode = ""
  showEditModal.value = true
}

const submitEdit = async () => {
  editErrors.barcode = ""
  isSubmitting.value = true
  try {
    const res = await api.put(`/BookCopies/${editingCopy.value.copyId}`, {
      ...editForm
    })
    if (res.status === 409) {
      const err = res.data
      editErrors.barcode = err.message
      return
    }
    if (res.status !== 200) throw new Error("Cập nhật thất bại")
    showEditModal.value = false
    await fetchData(currentPage.value)
  } catch (err) {
    alert(err.message)
  } finally {
    isSubmitting.value = false
  }
}

// ---- Delete ----
const confirmDelete = (copy) => {
  deletingCopy.value = copy
  showDeleteConfirm.value = true
}

const deleteCopy = async () => {
  isSubmitting.value = true
  try {
    const res = await api.delete(`/BookCopies/${deletingCopy.value.copyId}`)
    if (res.status !== 200 && res.status !== 204) {
      toasMessageStore.showToastMessage(res?.data?.message, TOAST_MESSAGE_STATUS.error, 5000)
      return
    }
    showDeleteConfirm.value = false
    await fetchData(currentPage.value)
  } catch (err) {
    showDeleteConfirm.value = false
    toasMessageStore.showToastMessage(
      err?.response?.data?.message,
      TOAST_MESSAGE_STATUS.error,
      5000
    )
  } finally {
    isSubmitting.value = false
  }
}

const filters = reactive({
  search: "",
  warehouseId: null,
  status: "",
})

let searchTimer = null

// ---- History ----
const showHistoryModal = ref(false)
const historyLoading = ref(false)
const historyList = ref([])
const historyBarcode = ref("")

const openHistory = async (copy) => {
  historyBarcode.value = copy.barcode || `#${copy.copyId}`
  historyList.value = []
  showHistoryModal.value = true
  historyLoading.value = true
  try {
    const res = await api.get(`/BookCopies/${copy.copyId}/history`)
    if (res.status === 200) historyList.value = res.data
  } catch {
    historyList.value = []
  } finally {
    historyLoading.value = false
  }
}

const statusLabelHistory = (status) => {
  const map = { Available: "Có thể mượn", Borrowed: "Đang mượn", Lost: "Mất", Damaged: "Hư hỏng" }
  return map[status] || status
}

const historyStatusClass = (status) => {
  const map = { Available: "hs-green", Borrowed: "hs-blue", Lost: "hs-red", Damaged: "hs-orange" }
  return map[status] || ""
}

const formatDateTime = (dt) => {
  if (!dt) return "—"
  return new Date(dt).toLocaleString("vi-VN")
}

// ---- Lifecycle ----
onMounted(async () => {
  authStore.setIsLoadingApi(true)
  await Promise.all([fetchWarehouses(), fetchData()])
  authStore.setIsLoadingApi(false)
})

// ---- Fetch ----
const fetchWarehouses = async () => {
  try {
    const res = await api.get("/Warehouses")
    if (res.status == 200) warehouses.value = res.data
  } catch {}
}

const fetchData = async (page = 1) => {
  isLoading.value = true
  loadError.value = ""
  try {
    const params = new URLSearchParams({ page, pageSize })
    if (filters.warehouseId) params.append("warehouseId", filters.warehouseId)
    if (filters.status) params.append("status", filters.status)
    if (filters.search.trim()) params.append("search", filters.search.trim())

    const res = await api.get(`/BookCopies/all?${params}`)
    if (res.status !== 200) throw new Error("Không thể tải dữ liệu")
    const data = res.data

    items.value = data.items
    total.value = data.total
    totalPages.value = data.totalPages
    currentPage.value = data.page
    statsRaw.value = data.stats
  } catch (err) {
    loadError.value = err.message
  } finally {
    isLoading.value = false
  }
}

// ---- Events ----
const onSearchInput = () => {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => fetchData(1), 400)
}

const onFilterChange = () => fetchData(1)

const goToPage = (page) => {
  if (page < 1 || page > totalPages.value) return
  fetchData(page)
}
const importBookCopies = () => {
  router.push({
    name: "bookCopyImport",
  })
}

// ---- Confirm status change ----
const showStatusConfirm = ref(false)
const pendingStatusChange = ref({ copy: null, newStatus: "" })
const statusChangeReason = ref("")

const onStatusChange = (copy, newStatus) => {
  if (newStatus === copy.status) return
  pendingStatusChange.value = { copy, newStatus }
  statusChangeReason.value = ""
  showStatusConfirm.value = true
}

const confirmStatusChange = async () => {
  const { copy, newStatus } = pendingStatusChange.value
  updatingId.value = copy.copyId
  showStatusConfirm.value = false
  try {
    const res = await api.patch(`/BookCopies/${copy.copyId}/status`, {
      status: newStatus,
      reason: statusChangeReason.value || null,
    })
    if (res.status === 200) {
      copy.status = newStatus
      await fetchData(currentPage.value)
    }
  } catch (err) {
    alert(err.response?.data?.message || "Cập nhật thất bại")
  } finally {
    updatingId.value = null
  }
}

// ---- Export Excel ----
const exportExcel = async () => {
  isExporting.value = true
  try {
    const params = new URLSearchParams({ page: 1, pageSize: 9999 })
    if (filters.warehouseId) params.append("warehouseId", filters.warehouseId)
    if (filters.status) params.append("status", filters.status)
    if (filters.search.trim()) params.append("search", filters.search.trim())

    const res = await api.get(`/BookCopies/all?${params}`)
    const data = res.data

    // Tạo CSV đơn giản
    const headers = [
      "Barcode",
      "Tên sách",
      "Phòng",
      "Vị trí kệ",
      "Tình trạng sách",
      "Ngày nhập",
      "Trạng thái",
    ]
    const rows = data.items.map((c) => [
      c.barcode || "",
      `"${(c.bookTitle || "").replace(/"/g, '""')}"`,
      c.warehouseName || "",
      c.shelfLocation || "",
      c.bookCondition || "",
      c.purchaseDate || "",
      statusLabel(c.status),
    ])

    const csv = [headers.join(","), ...rows.map((r) => r.join(","))].join("\n")
    const blob = new Blob(["\uFEFF" + csv], { type: "text/csv;charset=utf-8" })
    const url = URL.createObjectURL(blob)
    const a = document.createElement("a")
    a.href = url
    a.download = `ban-sao-vat-ly-${new Date().toISOString().slice(0, 10)}.csv`
    a.click()
    URL.revokeObjectURL(url)
  } catch (err) {
    alert("Xuất thất bại: " + err.message)
  } finally {
    isExporting.value = false
  }
}

// ---- Helpers ----
const statusLabel = (status) => {
  const map = { Available: "Có thể mượn", Unavailable: "Đọc tại chỗ", Borrowed: "Đang mượn", Lost: "Mất", Damaged: "Hư hỏng" }
  return map[status] || status
}

const statusClass = (status) => {
  const map = {
    Available: "status-green",
    Borrowed: "status-blue",
    Lost: "status-red",
    Damaged: "status-orange",
    Unavailable: "status-gray"
  }
  return map[status] || ""
}

const formatDate = (date) => {
  if (!date) return "—"
  return new Date(date).toLocaleDateString("vi-VN")
}

const truncate = (str, len) => {
  if (!str) return "—"
  return str.length > len ? str.slice(0, len) + "..." : str
}

const visiblePages = computed(() => {
  const pages = []
  const total = totalPages.value
  const cur = currentPage.value
  if (total <= 7) {
    for (let i = 1; i <= total; i++) pages.push(i)
  } else {
    pages.push(1)
    if (cur > 3) pages.push("...")
    for (let i = Math.max(2, cur - 1); i <= Math.min(total - 1, cur + 1); i++) pages.push(i)
    if (cur < total - 2) pages.push("...")
    pages.push(total)
  }
  return pages
})
</script>

<style lang="scss" scoped>
.management-page {
  color: #1a1a2e;
  display: flex;
  flex-direction: column;
  gap: 16px;
  background: #ffffff;
  padding: 16px 24px;
  border-radius: 12px;
}

/* Stats */
.stats-row {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 12px;
}

.stat-card {
  border-radius: 12px;
  padding: 16px 20px;
  display: flex;
  flex-direction: column;
  gap: 4px;
  border: 1.5px solid transparent;
}

.stat-value {
  font-size: 28px;
  font-weight: 800;
  line-height: 1;
}

.stat-label {
  font-size: 12px;
  font-weight: 500;
  opacity: 0.75;
}

.stat-total {
  background: #f0f0f5;
  border-color: #d0d0e0;
}

.stat-total .stat-value {
  color: #1a1a2e;
}

.stat-available {
  background: #e8f5e9;
  border-color: #c8e6c9;
}

.stat-available .stat-value {
  color: #2e7d32;
}

.stat-borrowed {
  background: #e3f2fd;
  border-color: #bbdefb;
}

.stat-borrowed .stat-value {
  color: #1565c0;
}

.stat-damaged {
  background: #fff3e0;
  border-color: #ffe0b2;
}

.stat-damaged .stat-value {
  color: #e65100;
}

.stat-lost {
  background: #ffebee;
  border-color: #ffcdd2;
}

.stat-lost .stat-value {
  color: #c62828;
}

/* Filters */
.filter-bar {
  display: flex;
  gap: 10px;
  align-items: center;
  flex-wrap: wrap;
}

.search-input {
  flex: 1;
  min-width: 200px;
  padding: 9px 14px;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  font-size: 14px;
  outline: none;
}

.search-input:focus {
  border-color: #435ebe;
}

.filter-select {
  padding: 9px 12px;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  font-size: 14px;
  background: #fff;
  outline: none;
  cursor: pointer;
  color: #333333;
}

.filter-select:focus {
  border-color: #435ebe;
}

.btn-import {
  padding: 8px 16px;
  background: transparent;
  border: 1px solid #435ebe;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  white-space: nowrap;
  color: #435ebe;
  .icon_import {
    height: 14px;
    margin-bottom: 2px;
  }
  &:hover {
    background: #435ebe;
    color: #ffffff;
  }
}

.btn-export {
  padding: 9px 16px;
  background: #435ebe;
  color: #fff;
  border: none;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  white-space: nowrap;
  transition: background 0.15s;

  .icon_excel {
    margin-bottom: 2px;
    margin-right: 2px;
  }
}

.btn-export:hover:not(:disabled) {
  background: #145218;
}

.btn-export:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

/* Table */
.table-wrapper {
  overflow-x: auto;
  border-radius: 10px;
  border: 1px solid #e0e0e0;
  margin: 0;
  padding: 0;
}

.copy-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 14px;
}

.copy-table thead tr {
  background: #f5f5f5;
}

.copy-table th {
  padding: 10px 14px;
  text-align: left;
  font-weight: 600;
  color: #555;
  white-space: nowrap;
  border-bottom: 1px solid #e0e0e0;
}

.copy-table td {
  padding: 10px 14px;
  border-bottom: 1px solid #f0f0f0;
  vertical-align: middle;
}

.copy-row:last-child td {
  border-bottom: none;
}

.copy-row:hover {
  background: #fafafa;
}

.empty-row {
  text-align: center;
  color: #aaa;
  padding: 40px;
}

.barcode-text {
  font-family: monospace;
  font-size: 13px;
  color: #435ebe;
  font-weight: 600;
}

.book-title {
  color: #333;
}

.warehouse-tag {
  display: inline-block;
  padding: 2px 10px;
  background: #e8eaf6;
  color: #435ebe;
  border-radius: 99px;
  font-size: 12px;
  font-weight: 500;
  white-space: nowrap;
}

/* Inline status select */
.status-select {
  padding: 4px 10px;
  border-radius: 99px;
  font-size: 12px;
  font-weight: 600;
  border: none;
  cursor: pointer;
  outline: none;
  appearance: none;
  -webkit-appearance: none;
  text-align: center;
}

.status-select:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.status-green {
  background: #e8f5e9;
  color: #2e7d32;
}

.status-blue {
  background: #e3f2fd;
  color: #1565c0;
}

.status-red {
  background: #ffebee;
  color: #c62828;
}

.status-orange {
  background: #fff3e0;
  color: #e65100;
}

.status-gray {
  background: #dcdcdc;
  color: #666666;
}

/* State boxes */
.state-box {
  padding: 40px;
  text-align: center;
  color: #888;
  font-size: 14px;
}

.state-error {
  color: #c62828;
}

/* Pagination */
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
}

.page-btn:hover:not(:disabled) {
  border-color: #435ebe;
  color: #435ebe;
}

.page-btn.active {
  background: #435ebe;
  border-color: #435ebe;
  color: #fff;
  font-weight: 700;
}

.page-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
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

.action-buttons {
  display: flex;
  gap: 4px;
  button {
    color: #435ebe;
  }
}

.action-btn {
  background: none;
  border: none;
  cursor: pointer;
  padding: 4px 6px;
  border-radius: 6px;
  font-size: 15px;
  transition: background 0.15s;
}

.action-btn:hover {
  background: #f0f0f0;
}

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
  top: unset;
  left: unset;
  height: unset;
}

.modal-sm {
  max-width: 380px;
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 24px 16px;
  border-bottom: 1px solid #f0f0f0;
}

.modal-header h3 {
  margin: 0;
  font-size: 17px;
  font-weight: 700;
}

.modal-close {
  background: none;
  border: none;
  font-size: 18px;
  cursor: pointer;
  color: #aaa;
  padding: 4px 8px;
  border-radius: 6px;
}

.modal-close:hover {
  background: #f0f0f0;
  color: #333;
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

.form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.form-group.full-width {
  grid-column: 1 / -1;
}

.form-group.checkbox-group {
  justify-content: flex-end;
}

.form-group label {
  font-size: 13px;
  font-weight: 600;
  color: #444;
}

.form-group input,
.form-group select,
.form-group textarea {
  padding: 8px 12px;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  font-size: 14px;
  outline: none;
  background: #fff;
  color: #333333;
}

.form-group input:focus,
.form-group select:focus,
.form-group textarea:focus {
  border-color: #435ebe;
}

.checkbox-group label {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 500 !important;
  cursor: pointer;
}

.field-error {
  color: #e53935;
  font-size: 12px;
}

.text-muted {
  color: #888;
  font-size: 13px;
  margin-top: 4px;
}

.btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  border: none;
  transition: all 0.15s;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-primary {
  background: #435ebe;
  color: #fff;
}

.btn-primary:hover:not(:disabled) {
  background: #2c3a8c;
}

.btn-outline {
  background: #fff;
  color: #435ebe;
  border: 1.5px solid #435ebe;
}

.btn-outline:hover:not(:disabled) {
  background: #e8eaf6;
}

.btn-danger {
  background: #e53935;
  color: #fff;
}

.btn-danger:hover:not(:disabled) {
  background: #c62828;
}

.modal-lg {
  max-width: 560px;
}
.modal-subtitle {
  font-size: 13px;
  color: #888;
  margin-top: 2px;
  font-family: monospace;
}

.history-loading,
.history-empty {
  text-align: center;
  padding: 32px;
  color: #aaa;
  font-size: 14px;
}

.timeline {
  display: flex;
  flex-direction: column;
  gap: 0;
  padding: 8px 0;
}

.timeline-item {
  display: flex;
  gap: 16px;
  position: relative;
  padding-bottom: 24px;

  &::before {
    content: "";
    position: absolute;
    left: 7px;
    top: 16px;
    width: 2px;
    bottom: 0;
    background: #e0e0e0;
  }
  &.last::before {
    display: none;
  }
}

.timeline-dot {
  width: 16px;
  height: 16px;
  border-radius: 50%;
  flex-shrink: 0;
  margin-top: 2px;
  border: 2px solid transparent;
  &.hs-green {
    background: #43a047;
    border-color: #c8e6c9;
  }
  &.hs-blue {
    background: #1e88e5;
    border-color: #bbdefb;
  }
  &.hs-red {
    background: #e53935;
    border-color: #ffcdd2;
  }
  &.hs-orange {
    background: #fb8c00;
    border-color: #ffe0b2;
  }
}

.timeline-content {
  flex: 1;
}

.timeline-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
}

.timeline-transition {
  display: flex;
  align-items: center;
  gap: 6px;
}

.status-pill {
  display: inline-block;
  padding: 2px 10px;
  border-radius: 99px;
  font-size: 12px;
  font-weight: 600;
  &.hs-green {
    background: #e8f5e9;
    color: #2e7d32;
  }
  &.hs-blue {
    background: #e3f2fd;
    color: #1565c0;
  }
  &.hs-red {
    background: #ffebee;
    color: #c62828;
  }
  &.hs-orange {
    background: #fff3e0;
    color: #e65100;
  }
}

.arrow {
  color: #aaa;
  font-size: 14px;
}

.timeline-time {
  font-size: 12px;
  color: #999;
  white-space: nowrap;
}

.timeline-meta {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-top: 4px;
}

.timeline-by {
  font-size: 13px;
  color: #555;
}
.timeline-reason {
  font-size: 13px;
  color: #888;
  font-style: italic;
}

.status-change-preview {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 16px;
  background: #f5f5f5;
  border-radius: 8px;
  justify-content: center;
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
    resize: vertical;
    &:focus {
      border-color: #3949ab;
    }
  }
}

@media (max-width: 768px) {
  .stats-row {
    grid-template-columns: repeat(2, 1fr);
  }

  .stat-card:first-child {
    grid-column: 1 / -1;
  }
}
</style>