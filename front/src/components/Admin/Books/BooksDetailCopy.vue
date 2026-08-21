<template>
  <div class="book-copy-manager">
    <!-- Header -->
    <div class="manager-header">
      <div class="header-left">
        <h2 class="section-title">Bản sao vật lý</h2>
        <span class="copy-count">{{ copies.length }} bản</span>
      </div>
      <div class="header-actions">
        <button class="btn btn-outline" @click="openSingleModal">
          <span class="btn-icon">+</span> Thêm 1 bản
        </button>
        <button class="btn btn-primary" @click="openBulkModal">
          <span class="btn-icon">⊕</span> Thêm nhiều bản
        </button>
      </div>
    </div>

    <!-- Loading / Error -->
    <div v-if="isLoading" class="state-box">Đang tải dữ liệu...</div>
    <div v-else-if="loadError" class="state-box state-error">{{ loadError }}</div>

    <!-- Filter bar -->
    <div class="filter-bar">
      <button
        v-for="status in statusFilters"
        :key="status.value"
        class="filter-chip"
        :class="{ active: activeFilter === status.value }"
        @click="activeFilter = status.value"
      >
        <span class="chip-dot" :class="status.color"></span>
        {{ status.label }}
        <span class="chip-count">{{ countByStatus(status.value) }}</span>
      </button>
    </div>

    <!-- Table -->
    <div class="table-wrapper">
      <table class="copy-table">
        <thead>
          <tr>
            <th>Barcode</th>
            <th>Kho</th>
            <th>Vị trí kệ</th>
            <th>Tình trạng</th>
            <th>Chỉ tham khảo</th>
            <th>Ngày nhập</th>
            <th>Trạng thái</th>
            <th>Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="filteredCopies.length === 0">
            <td colspan="8" class="empty-row">Chưa có bản sao nào</td>
          </tr>
          <tr v-for="copy in filteredCopies" :key="copy.copyId" class="copy-row">
            <td>
              <span class="barcode-text">{{ copy.barcode || "—" }}</span>
            </td>
            <td>{{ copy.warehouseName || "—" }}</td>
            <td>{{ copy.shelfLocation || "—" }}</td>
            <td>{{ copy.bookCondition || "—" }}</td>
            <td>
              <span class="badge" :class="copy.isReferenceOnly ? 'badge-warning' : 'badge-neutral'">
                {{ copy.isReferenceOnly ? "Có" : "Không" }}
              </span>
            </td>
            <td>{{ formatDate(copy.purchaseDate) }}</td>
            <td>
              <span class="status-badge" :class="statusClass(copy.status)">
                {{ statusLabel(copy.status) }}
              </span>
            </td>
            <td>
              <div class="action-buttons">
                <button class="action-btn edit" @click="openEditModal(copy)" title="Chỉnh sửa">
                  <i class="bi bi-pencil"></i>
                </button>
                <button
                  class="action-btn delete"
                  @click="confirmDelete(copy)"
                  :disabled="copy.status === 'Borrowed'"
                  title="Xoá"
                >
                  <i class="bi bi-trash"></i>
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Modal thêm/sửa 1 bản -->
    <Teleport to="body">
      <div v-if="showSingleModal" class="modal-overlay" @click.self="closeSingleModal">
        <div class="modal">
          <div class="modal-header">
            <h3>{{ editingCopy ? "Chỉnh sửa bản sao" : "Thêm 1 bản sao" }}</h3>
            <button class="modal-close" @click="closeSingleModal">✕</button>
          </div>
          <div class="modal-body">
            <div class="form-grid">
              <div class="form-group">
                <label>Barcode<span class="required">*</span></label>
                <div class="input-with-btn">
                  <input
                    v-model="singleForm.barcode"
                    placeholder="VD: BC-00001"
                    :class="{ 'input-error': singleErrors.barcode }"
                  />
                  <button class="btn-gen" @click="generateBarcode" type="button">Tự sinh</button>
                </div>
                <span v-if="singleErrors.barcode" class="field-error">{{
                  singleErrors.barcode
                }}</span>
              </div>
              <div class="form-group">
                <label>Phòng <span class="required">*</span></label>
                <select
                  v-model="singleForm.warehouseId"
                  :class="{ 'input-error': singleErrors.warehouseId }"
                >
                  <option :value="null">-- Chọn phòng --</option>
                  <option
                    v-for="warehouse in warehouses"
                    :key="warehouse.warehouseId"
                    :value="warehouse.warehouseId"
                  >
                    {{ warehouse.name }}
                  </option>
                </select>
                <span v-if="singleErrors.warehouseId" class="field-error">{{
                  singleErrors.warehouseId
                }}</span>
              </div>
              <div class="form-group">
                <label>Vị trí kệ</label>
                <input v-model="singleForm.shelfLocation" placeholder="VD: A1-03" />
              </div>
              <div class="form-group">
                <label>Tình trạng sách</label>
                <select
                  v-model="singleForm.bookCondition"
                  :class="{ 'input-error': singleErrors.bookCondition }"
                >
                  <option value="">-- Chọn --</option>
                  <option value="Mới">Mới</option>
                  <option value="Tốt">Tốt</option>
                  <option value="Bình thường">Bình thường</option>
                  <option value="Cũ">Cũ</option>
                  <option value="Hư hỏng nhẹ">Hư hỏng nhẹ</option>
                  <option value="Hư hỏng nặng">Hư hỏng nặng</option>
                </select>
                <span v-if="singleErrors.bookCondition" class="field-error">{{
                  singleErrors.bookCondition
                }}</span>
              </div>
              <div class="form-group">
                <label>Ngày nhập<span class="required">*</span></label>
                <input
                  type="date"
                  v-model="singleForm.purchaseDate"
                  :class="{ 'input-error': singleErrors.purchaseDate }"
                />
                <span v-if="singleErrors.purchaseDate" class="field-error">{{
                  singleErrors.purchaseDate
                }}</span>
              </div>
              <div class="form-group checkbox-group">
                <label>
                  <input type="checkbox" class="ms-checkbox" v-model="singleForm.isReferenceOnly" />
                  <span
                    :class="{ selected: singleForm.isReferenceOnly }"
                    class="ms-checkbox-custom"
                  ></span>
                  Chỉ tham khảo (không cho mượn)
                </label>
              </div>
              <div class="form-group full-width">
                <label>Ghi chú</label>
                <textarea
                  v-model="singleForm.notes"
                  rows="2"
                  placeholder="Ghi chú thêm..."
                ></textarea>
              </div>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" @click="closeSingleModal">Huỷ</button>
            <button class="btn btn-primary" @click="submitSingle" :disabled="isSubmitting">
              {{ isSubmitting ? "Đang lưu..." : editingCopy ? "Cập nhật" : "Thêm bản sao" }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Modal thêm nhiều bản -->
    <Teleport to="body">
      <div v-if="showBulkModal" class="modal-overlay" @click.self="closeBulkModal">
        <div class="modal">
          <div class="modal-header">
            <h3>Thêm nhiều bản sao</h3>
            <button class="modal-close" @click="closeBulkModal">✕</button>
          </div>
          <div class="modal-body">
            <div class="form-grid">
              <div class="form-group">
                <label>Số lượng <span class="required">*</span></label>
                <input
                  type="number"
                  v-model.number="bulkForm.quantity"
                  min="1"
                  max="100"
                  :class="{ 'input-error': bulkErrors.quantity }"
                />
                <span v-if="bulkErrors.quantity" class="field-error">{{
                  bulkErrors.quantity
                }}</span>
              </div>
              <div class="form-group">
                <label>Kho<span class="required">*</span></label>
                <select
                  v-model="bulkForm.warehouseId"
                  :class="{ 'input-error': bulkErrors.warehouseId }"
                >
                  <option :value="null">-- Chọn phòng --</option>
                  <option
                    v-for="warehouse in warehouses"
                    :key="warehouse.warehouseId"
                    :value="warehouse.warehouseId"
                  >
                    {{ warehouse.name }}
                  </option>
                </select>
                <span v-if="bulkErrors.warehouseId" class="field-error">{{
                  bulkErrors.warehouseId
                }}</span>
              </div>
              <div class="form-group">
                <label>Vị trí kệ</label>
                <input v-model="bulkForm.shelfLocation" placeholder="VD: A1-03" />
              </div>
              <div class="form-group">
                <label>Tình trạng sách</label>
                <select v-model="bulkForm.bookCondition">
                  <option value="Mới">Mới</option>
                  <option value="Tốt">Tốt</option>
                  <option value="Bình thường">Bình thường</option>
                  <option value="Cũ">Cũ</option>
                </select>
              </div>
              <div class="form-group">
                <label>Ngày nhập<span class="required">*</span></label>
                <input
                  type="date"
                  v-model="bulkForm.purchaseDate"
                  :class="{ 'input-error': bulkErrors.purchaseDate }"
                />
                <span v-if="bulkErrors.purchaseDate" class="field-error">{{
                  bulkErrors.purchaseDate
                }}</span>
              </div>
              <div class="form-group checkbox-group">
                <label>
                  <input type="checkbox" class="ms-checkbox" v-model="bulkForm.isReferenceOnly" />
                  <span
                    :class="{ selected: bulkForm.isReferenceOnly }"
                    class="ms-checkbox-custom"
                  ></span>
                  Chỉ tham khảo (không cho mượn)
                </label>
              </div>
            </div>
            <div class="bulk-preview" v-if="bulkForm.quantity > 0">
              <div class="preview-label">
                ✓ Sẽ tạo <strong>{{ bulkForm.quantity }}</strong> bản — barcode tự động sinh bởi
                server
              </div>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" @click="closeBulkModal">Huỷ</button>
            <button
              class="btn btn-primary"
              @click="submitBulk"
              :disabled="isSubmitting || !bulkForm.quantity"
            >
              {{ isSubmitting ? "Đang lưu..." : `Thêm ${bulkForm.quantity || 0} bản` }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Confirm delete -->
    <Teleport to="body">
      <div v-if="showDeleteConfirm" class="modal-overlay" @click.self="showDeleteConfirm = false">
        <div class="modal modal-sm">
          <div class="modal-header">
            <h3>Xác nhận xoá</h3>
            <button class="modal-close" @click="showDeleteConfirm = false">✕</button>
          </div>
          <div class="modal-body">
            <p>
              Bạn có chắc muốn xoá bản sao <strong>{{ deletingCopy?.barcode }}</strong
              >?
            </p>
            <p class="text-muted">Hành động này không thể hoàn tác.</p>
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
  </div>
</template>

<script setup>
import { ref, computed, reactive, onMounted } from "vue"
import api from "../../../services/api"
import { useToastMessageStore } from "../../../stores/toastMessage"
import { TOAST_MESSAGE_STATUS } from "../../../constants"

const props = defineProps({
  bookId: {
    type: Number,
    required: true,
  },
})

const toasMessageStore = useToastMessageStore()
const emit = defineEmits(["updated"])

// ---- State ----
const copies = ref([])
const isLoading = ref(false)
const loadError = ref("")
const warehouses = ref([])

// ---- Fetch on mount ----
onMounted(async () => {
  await Promise.all([fetchCopies(), fetchWarehouses()])
})

const fetchCopies = async () => {
  isLoading.value = true
  loadError.value = ""
  try {
    const res = await api.get(`/BookCopies?bookId=${props.bookId}`)
    if (res.status !== 200) throw new Error("Không thể tải danh sách bản sao")
    copies.value = res.data
  } catch (err) {
    loadError.value = err.message
  } finally {
    isLoading.value = false
  }
}

const fetchWarehouses = async () => {
  try {
    const res = await api.get("/Warehouses")
    if (res.status == 200) warehouses.value = res.data
  } catch {}
}

const warehouseName = (warehouseId) => {
  const warehouse = warehouses.value.find((r) => r.warehouseId === warehouseId)
  return warehouse ? warehouse.name : "—"
}
const activeFilter = ref("all")
const isSubmitting = ref(false)
const singleErrors = reactive({ barcode: "", bookCondition: "", warehouseId: "", purchaseDate: "" })
const bulkErrors = reactive({ quantity: "", warehouseId: "", purchaseDate: "" })

const showSingleModal = ref(false)
const showBulkModal = ref(false)
const showDeleteConfirm = ref(false)

const editingCopy = ref(null)
const deletingCopy = ref(null)

const defaultSingleForm = () => ({
  barcode: "",
  shelfLocation: "",
  bookCondition: "Mới",
  purchaseDate: "",
  isReferenceOnly: false,
  notes: "",
  warehouseId: null,
})

const singleForm = reactive(defaultSingleForm())
const bulkForm = reactive({
  quantity: 1,
  shelfLocation: "",
  bookCondition: "Mới",
  purchaseDate: "",
  isReferenceOnly: false,
  warehouseId: null,
})

// ---- Filters ----
const statusFilters = [
  { value: "all", label: "Tất cả", color: "dot-gray" },
  { value: "Available", label: "Có sẵn", color: "dot-green" },
  { value: "Borrowed", label: "Đang mượn", color: "dot-blue" },
  { value: "Lost", label: "Mất", color: "dot-red" },
  { value: "Damaged", label: "Hư hỏng", color: "dot-orange" },
]

const filteredCopies = computed(() => {
  if (activeFilter.value === "all") return copies.value
  return copies.value.filter((c) => c.status === activeFilter.value)
})

const countByStatus = (status) => {
  if (status === "all") return copies.value.length
  return copies.value.filter((c) => c.status === status).length
}

// ---- Helpers ----
const statusLabel = (status) => {
  const map = { Available: "Có sẵn", Borrowed: "Đang mượn", Lost: "Mất", Damaged: "Hư hỏng" }
  return map[status] || status
}

const statusClass = (status) => {
  const map = {
    Available: "status-green",
    Borrowed: "status-blue",
    Lost: "status-red",
    Damaged: "status-orange",
  }
  return map[status] || ""
}

const formatDate = (date) => {
  if (!date) return "—"
  return new Date(date).toLocaleDateString("vi-VN")
}

const generateBarcode = () => {
  const now = new Date()
  const pad = (n, len = 2) => String(n).padStart(len, "0")
  const datePart = `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}`
  const timePart = `${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`
  singleForm.barcode = `BC-${datePart}-${timePart}`
}

// ---- Modal controls ----
const openSingleModal = () => {
  editingCopy.value = null
  Object.assign(singleForm, defaultSingleForm())
  showSingleModal.value = true
}

const openEditModal = (copy) => {
  editingCopy.value = copy
  Object.assign(singleForm, {
    barcode: copy.barcode || "",
    shelfLocation: copy.shelfLocation || "",
    bookCondition: copy.bookCondition || "Mới",
    purchaseDate: copy.purchaseDate || "",
    isReferenceOnly: copy.isReferenceOnly || false,
    notes: copy.notes || "",
    warehouseId: copy.warehouseId || null,
  })
  showSingleModal.value = true
}

const closeSingleModal = () => {
  showSingleModal.value = false
  singleErrors.barcode = ""
  singleErrors.bookCondition = ""
  singleErrors.purchaseDate = ""
  singleErrors.warehouseId = ""
}

const openBulkModal = () => {
  Object.assign(bulkForm, {
    quantity: 1,
    shelfLocation: "",
    bookCondition: "Mới",
    purchaseDate: "",
    isReferenceOnly: false,
  })
  showBulkModal.value = true
}

const closeBulkModal = () => {
  showBulkModal.value = false
  bulkErrors.quantity = ""
  bulkErrors.warehouseId = ""
  bulkErrors.purchaseDate = ""
  bulkForm.warehouseId = null
}

const confirmDelete = (copy) => {
  deletingCopy.value = copy
  showDeleteConfirm.value = true
}
// ---- Validate ----
const validateSingle = () => {
  singleErrors.barcode = ""
  singleErrors.bookCondition = ""
  singleErrors.purchaseDate = ""
  singleErrors.warehouseId = ""

  if (!singleForm.bookCondition) singleErrors.bookCondition = "Vui lòng chọn tình trạng sách"

  if (!singleForm.warehouseId) singleErrors.warehouseId = "Vui lòng chọn kho"

  // Check barcode trùng trong danh sách hiện tại (client-side nhanh)
  if (singleForm.barcode) {
    const duplicate = copies.value.find(
      (c) => c.barcode === singleForm.barcode && c.copyId !== editingCopy.value?.copyId
    )
    if (duplicate) singleErrors.barcode = "Barcode này đã tồn tại"
  } else {
    singleErrors.barcode = "Vui lòng nhập barcode"
  }

  if (!singleForm.purchaseDate) singleErrors.purchaseDate = "Vui lòng chọn ngày nhập"

  return (
    !singleErrors.barcode &&
    !singleErrors.bookCondition &&
    !singleErrors.warehouseId &&
    !singleErrors.purchaseDate
  )
}

const validateBulk = () => {
  bulkErrors.quantity = ""
  bulkErrors.warehouseId = ""
  bulkErrors.purchaseDate = ""
  if (!bulkForm.quantity || bulkForm.quantity < 1) bulkErrors.quantity = "Số lượng phải lớn hơn 0"
  else if (bulkForm.quantity > 100) bulkErrors.quantity = "Số lượng tối đa là 100"

  if (!bulkForm.warehouseId) bulkErrors.warehouseId = "Vui lòng chọn kho"

  if (!bulkForm.purchaseDate) bulkErrors.purchaseDate = "Vui lòng chọn ngày nhập"
  return !bulkErrors.quantity && !bulkErrors.warehouseId && !bulkErrors.purchaseDate
}
// ---- API calls (thay URL theo project của bạn) ----
const submitSingle = async () => {
  if (!validateSingle()) return

  isSubmitting.value = true
  try {
    if (editingCopy.value) {
      const res = await api.put(`/BookCopies/${editingCopy.value.copyId}`, { ...singleForm })
      if (res.status === 400) {
        const err = res.data.message
        singleErrors.barcode = err.message
        return
      }
      if (res.status !== 200) throw new Error("Cập nhật thất bại")
      const updated = res.data
      const idx = copies.value.findIndex((c) => c.copyId === editingCopy.value.copyId)
      if (idx !== -1) copies.value[idx] = updated
    } else {
      const res = await api.post(`/BookCopies`, { ...singleForm, bookId: props.bookId })
      if (res.status === 400) {
        const err = res.data.message
        singleErrors.barcode = err.message
        return
      }
      if (res.status !== 200 && res.status !== 201) throw new Error("Thêm thất bại")
      const created = res.data
      copies.value.push(created)
    }
    closeSingleModal()
    emit("updated", copies.value)
  } catch (err) {
    toasMessageStore.showToastMessage(
      err?.response?.data?.message,
      TOAST_MESSAGE_STATUS.error,
      5000
    )
  } finally {
    isSubmitting.value = false
  }
}

const submitBulk = async () => {
  if (!validateBulk()) return

  isSubmitting.value = true
  try {
    const res = await api.post(`/BookCopies/bulk`, { ...bulkForm, bookId: props.bookId })
    if (res.status !== 200) throw new Error("Thêm nhiều bản thất bại")
    const created = res.data
    copies.value.push(...created)
    closeBulkModal()
    emit("updated", copies.value)
  } catch (err) {
    toasMessageStore.showToastMessage(
      err?.response?.data?.message,
      TOAST_MESSAGE_STATUS.error,
      5000
    )
  } finally {
    isSubmitting.value = false
  }
}

const deleteCopy = async () => {
  isSubmitting.value = true
  try {
    const res = await api.delete(`/BookCopies/${deletingCopy.value.copyId}`)
    if (res.status !== 200 && res.status !== 204) throw new Error("Xoá thất bại")
    else toasMessageStore.showToastMessage("Xoá thành công!", TOAST_MESSAGE_STATUS.success, 5000)
    copies.value = copies.value.filter((c) => c.copyId !== deletingCopy.value.copyId)
    showDeleteConfirm.value = false
    emit("updated", copies.value)
  } catch (err) {
    toasMessageStore.showToastMessage(
      err?.response?.data?.message,
      TOAST_MESSAGE_STATUS.error,
      5000
    )
  } finally {
    isSubmitting.value = false
  }
}
</script>

<style lang="scss" scoped>
.book-copy-manager {
  color: #1a1a2e;
  padding: 16px 24px;
  background: #ffffff;
  border-radius: 16px;
}

.manager-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
}

.section-title {
  font-size: 18px;
  font-weight: 700;
  margin: 0;
}

.copy-count {
  background: #e8eaf6;
  color: #435ebe;
  font-size: 12px;
  font-weight: 600;
  padding: 2px 10px;
  border-radius: 99px;
}

.header-actions {
  display: flex;
  gap: 8px;
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

.btn-icon {
  font-size: 16px;
  line-height: 1;
  margin-bottom: 1px;
}

.filter-bar {
  display: flex;
  gap: 8px;
  margin-bottom: 16px;
  flex-wrap: wrap;
}

.filter-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 5px 12px;
  border-radius: 99px;
  border: 1.5px solid #e0e0e0;
  background: #fff;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.15s;
  color: #555;
}

.filter-chip:hover {
  border-color: #435ebe;
  color: #435ebe;
}

.filter-chip.active {
  background: #435ebe;
  border-color: #435ebe;
  color: #fff;
}

.filter-chip.active .chip-dot {
  background: #fff !important;
}

.chip-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;
}

.dot-gray {
  background: #9e9e9e;
}

.dot-green {
  background: #43a047;
}

.dot-blue {
  background: #1e88e5;
}

.dot-red {
  background: #e53935;
}

.dot-orange {
  background: #fb8c00;
}

.chip-count {
  background: rgba(0, 0, 0, 0.08);
  border-radius: 99px;
  padding: 0 6px;
  font-size: 11px;
}

.filter-chip.active .chip-count {
  background: rgba(255, 255, 255, 0.25);
}

.table-wrapper {
  overflow-x: auto;
  border-radius: 10px;
  border: 1px solid #e0e0e0;
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
  padding: 32px;
}

.barcode-text {
  font-family: monospace;
  font-size: 13px;
  color: #3949ab;
  font-weight: 600;
}

.status-badge {
  display: inline-block;
  padding: 3px 10px;
  border-radius: 99px;
  font-size: 12px;
  font-weight: 600;
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

.badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 99px;
  font-size: 12px;
  font-weight: 500;
}

.badge-warning {
  background: #fff8e1;
  color: #f57f17;
}

.badge-neutral {
  background: #f5f5f5;
  color: #757575;
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

.action-btn:hover:not(:disabled) {
  background: #f0f0f0;
}

.action-btn:disabled {
  opacity: 0.3;
  cursor: not-allowed;
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
  max-width: 580px;
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
  line-height: 1;
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
  padding-bottom: 8px;

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
  transition: border-color 0.15s;
  background: #fff;
  color: #333333;
}

.form-group input:focus,
.form-group select:focus,
.form-group textarea:focus {
  border-color: #435ebe;
}

.form-group textarea {
  resize: vertical;
}

.required {
  color: #e53935;
}

.input-with-btn {
  display: flex;
  gap: 6px;
}

.input-with-btn input {
  flex: 1;
  min-width: 0;
}

.btn-gen {
  padding: 8px 12px;
  background: #e8eaf6;
  color: #435ebe;
  border: none;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  white-space: nowrap;
  flex-shrink: 0;
}

.btn-gen:hover {
  background: #c5cae9;
}

.checkbox-group label {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  font-weight: 500 !important;
}

.checkbox-group input[type="checkbox"] {
  width: 16px;
  height: 16px;
  cursor: pointer;
}

.bulk-preview {
  margin-top: 16px;
  padding: 12px 16px;
  background: #e8f5e9;
  border-radius: 8px;
  border-left: 3px solid #43a047;
}

.preview-label {
  font-size: 13px;
  color: #2e7d32;
}

.text-muted {
  color: #888;
  font-size: 13px;
  margin-top: 6px;
}

.field-error {
  color: #e53935;
  font-size: 12px;
  margin-top: 2px;
}

.input-error {
  border-color: #e53935 !important;
}

.input-error:focus {
  border-color: #e53935 !important;
}

.state-box {
  padding: 32px;
  text-align: center;
  color: #888;
  font-size: 14px;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  margin-bottom: 16px;
}

.state-error {
  color: #c62828;
  border-color: #ffcdd2;
  background: #ffebee;
}

@media (max-width: 600px) {
  .form-grid {
    grid-template-columns: 1fr;
  }

  .manager-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 10px;
  }
}
</style>