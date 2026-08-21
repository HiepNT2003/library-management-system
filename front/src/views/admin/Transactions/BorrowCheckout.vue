<template>
  <div class="borrow-checkout">
    <!-- Header -->
    <div class="page-header">
      <div>
        <h1 class="page-title">Cho mượn sách</h1>
        <p class="page-desc">Xử lý cho mượn từ yêu cầu đã duyệt hoặc trực tiếp</p>
      </div>
    </div>

    <div class="form-layout">
      <!-- Left: Form -->
      <div class="form-card">
        <!-- Section 1: Chọn bạn đọc -->
        <div class="form-section">
          <div class="section-title"><span class="section-num">1</span> Bạn đọc</div>

          <!-- Mode tabs -->
          <div class="mode-tabs">
            <button
              class="mode-tab"
              :class="{ active: userMode === 'request' }"
              @click="setUserMode('request')"
            >
              <Icon icon="mdi:line-scan" width="20" height="20" /> Quét mã yêu cầu
            </button>
            <button
              class="mode-tab"
              :class="{ active: userMode === 'manual' }"
              @click="setUserMode('manual')"
            >
              <Icon icon="ic:sharp-search" width="24" height="24" />
              Tìm bạn đọc
            </button>
          </div>

          <!-- Request mode -->
          <div v-if="userMode === 'request'">
            <div class="search-group">
              <input
                v-model="requestInput"
                class="search-input barcode-input"
                placeholder="Quét hoặc nhập mã yêu cầu..."
                @keydown.enter="loadFromRequest"
                ref="requestInputRef"
              />
              <button class="btn-search" @click="loadFromRequest" :disabled="isLoadingRequest">
                {{ isLoadingRequest ? '...' : '🔍' }}
              </button>
            </div>

            <QrScanner @scanned="onQRScanned" />

            <div class="hint">💡 Quét QR từ app sinh viên hoặc nhập mã thủ công</div>
          </div>

          <!-- Manual mode -->
          <div v-if="userMode === 'manual'">
            <div class="search-group">
              <input
                v-model="userSearch"
                class="search-input"
                placeholder="Nhập tên, email hoặc mã sinh viên..."
                @input="onUserSearch"
              />
            </div>
            <div class="search-results" v-if="userResults.length > 0 && !selectedUser">
              <div v-for="u in userResults" :key="u.id" class="result-item" @click="selectUser(u)">
                <div class="result-name">{{ u.fullName }}</div>
                <div class="result-sub">
                  {{ u.studentProfile?.studentCode || u.staffProfile?.staffCode || u.email }}
                </div>
              </div>
            </div>
          </div>

          <!-- Selected user -->
          <div class="selected-card user-card" v-if="selectedUser">
            <div class="selected-info">
              <div class="selected-avatar">{{ initials(selectedUser.fullName) }}</div>
              <div>
                <div class="selected-name">{{ selectedUser.fullName }}</div>
                <div class="selected-sub">
                  {{
                    selectedUser.studentProfile?.studentCode ||
                    selectedUser.staffProfile?.staffCode ||
                    selectedUser.email
                  }}
                </div>
              </div>
            </div>
            <div class="selected-meta" v-if="fromRequest">
              <span class="meta-badge badge-green">✓ Từ yêu cầu #{{ fromRequest }}</span>
            </div>
            <button class="btn-clear" @click="clearUser">✕</button>
          </div>

          <div class="warn-box" v-if="userWarning">⚠️ {{ userWarning }}</div>
        </div>

        <!-- Section 2: Danh sách sách mượn -->
        <div class="form-section">
          <div class="section-title">
            <span class="section-num">2</span> Bản sao sách
            <span class="count-pill" v-if="borrowList.length > 0">{{ borrowList.length }}</span>
          </div>

          <!-- Nếu có suggested book từ request -->
          <div class="suggested-book" v-if="suggestedBook">
            <div class="suggested-title">
              📖 Sách đã đăng ký: <strong>{{ suggestedBook.title }}</strong>
            </div>
            <div class="suggested-hint">Quét barcode của bản sao vật lý để thêm vào danh sách</div>
          </div>

          <!-- Barcode scan -->
          <div class="search-group">
            <input
              v-model="barcodeInput"
              ref="barcodeRef"
              class="search-input barcode-input"
              placeholder="Quét hoặc nhập barcode..."
              @keydown.enter="addByBarcode"
              :disabled="!selectedUser"
            />
            <button
              class="btn-search"
              @click="addByBarcode"
              :disabled="!selectedUser || isAddingCopy"
            >
              {{ isAddingCopy ? "..." : "+" }}
            </button>
          </div>

          <div class="warn-box" v-if="copyWarning">⚠️ {{ copyWarning }}</div>

          <!-- Borrow list -->
          <div class="borrow-list" v-if="borrowList.length > 0">
            <div v-for="(copy, idx) in borrowList" :key="copy.copyId" class="borrow-item">
              <div class="item-num">{{ idx + 1 }}</div>
              <div class="item-info">
                <div class="item-title">{{ copy.bookTitle }}</div>
                <div class="item-meta">
                  <span class="code-text">{{ copy.barcode }}</span>
                  <span v-if="copy.warehouseName"> · {{ copy.warehouseName }}</span>
                  <span v-if="copy.shelfLocation"> · {{ copy.shelfLocation }}</span>
                </div>
              </div>
              <button class="btn-remove" @click="removeFromList(idx)" title="Xoá">✕</button>
            </div>
          </div>

          <div class="empty-list" v-else-if="selectedUser">
            Chưa có sách nào. Quét barcode để thêm.
          </div>
        </div>

        <!-- Section 3: Ghi chú -->
        <div class="form-section">
          <div class="section-title">
            <span class="section-num">3</span> Ghi chú
            <span class="optional-tag">Tuỳ chọn</span>
          </div>
          <textarea
            v-model="notes"
            class="notes-input"
            rows="2"
            placeholder="Ghi chú thêm nếu có..."
          ></textarea>
        </div>

        <!-- Submit -->
        <div class="form-actions">
          <button
            class="btn btn-primary btn-submit"
            @click="submitBorrow"
            :disabled="!canSubmit || isSubmitting"
          >
            {{ isSubmitting ? "Đang xử lý..." : `✓ Xác nhận cho mượn ${borrowList.length} cuốn` }}
          </button>
        </div>
      </div>

      <!-- Right: Preview -->
      <div class="preview-card">
        <div class="preview-title">Thông tin giao dịch</div>

        <div v-if="!selectedUser" class="preview-empty">Chọn bạn đọc để bắt đầu</div>

        <template v-else>
          <div class="preview-rows">
            <div class="preview-row">
              <span class="preview-label">Bạn đọc</span>
              <span class="preview-value">{{ selectedUser.fullName }}</span>
            </div>
            <div class="preview-row">
              <span class="preview-label">Số cuốn</span>
              <span class="preview-value">
                <strong>{{ borrowList.length }}</strong> cuốn
              </span>
            </div>
            <div class="preview-row">
              <span class="preview-label">Ngày mượn</span>
              <span class="preview-value">{{ formatDate(new Date()) }}</span>
            </div>
          </div>

          <!-- Breakdown theo loại -->
          <div class="breakdown" v-if="borrowList.length > 0">
            <div class="breakdown-title">Chi tiết theo loại</div>
            <div v-for="group in groupByType" :key="group.docType" class="breakdown-row">
              <span class="breakdown-label">{{ docTypeLabel(group.docType) }}</span>
              <span class="breakdown-count">{{ group.count }} cuốn</span>
            </div>
          </div>

          <div class="ready-box" v-if="canSubmit"><Icon class="icon_success" icon="qlementine-icons:success-16" width="16" height="16" /> Sẵn sàng cho mượn</div>
        </template>
      </div>
    </div>

    <!-- Success modal -->
    <Teleport to="body">
      <div v-if="showSuccess" class="modal-overlay">
        <div class="modal modal-lg success-modal">
          <div class="success-icon"><Icon icon="qlementine-icons:success-16" width="56" height="56" /></div>
          <h3 class="success-title">Cho mượn thành công!</h3>
          <div class="success-desc">
            Đã tạo {{ successTransactions.length }} giao dịch cho <strong>{{ successUser }}</strong>
          </div>

          <div class="success-list">
            <div v-for="tx in successTransactions" :key="tx.transactionId" class="success-item">
              <div class="success-item-info">
                <div class="success-item-title">{{ tx.bookTitle }}</div>
                <div class="success-item-meta">
                  Barcode: <span class="code-text">{{ tx.copyBarcode }}</span>
                </div>
              </div>
              <div class="success-item-due">
                Hạn: <strong>{{ formatDate(tx.dueDate) }}</strong>
              </div>
            </div>
          </div>

          <div class="success-actions">
            <button class="btn btn-outline" @click="resetForm">Cho mượn tiếp</button>
            <button class="btn btn-primary" @click="redirectList">
              Xem danh sách
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, nextTick, onMounted } from "vue"
import { useRouter } from "vue-router"
import api from "../../../services/api"
import { Icon } from "@iconify/vue"
import QrScanner from "../../../components/share/QrScanner.vue"

const router = useRouter()

// ---- State ----
const userMode = ref("request") // 'request' | 'manual'
const requestInput = ref("")
const userSearch = ref("")
const userResults = ref([])
const selectedUser = ref(null)
const userWarning = ref("")
const fromRequest = ref(null)
const suggestedBook = ref(null)
const isLoadingRequest = ref(false)

const barcodeInput = ref("")
const copyWarning = ref("")
const borrowList = ref([])
const isAddingCopy = ref(false)

const notes = ref("")
const isSubmitting = ref(false)
const showSuccess = ref(false)
const successTransactions = ref([])
const successUser = ref("")

const requestInputRef = ref(null)
const barcodeRef = ref(null)
let userSearchTimer = null

// ---- Lifecycle ----
onMounted(() => {
  nextTick(() => requestInputRef.value?.focus())
})

// ---- Computed ----
const canSubmit = computed(() => {
  if (!selectedUser.value) return false
  if (borrowList.value.length === 0) return false
  if (userWarning.value) return false
  return true
})

const groupByType = computed(() => {
  const map = {}
  borrowList.value.forEach((c) => {
    const key = c.documentTypeId ?? 0
    if (!map[key]) map[key] = { docType: key, count: 0 }
    map[key].count++
  })
  return Object.values(map)
})

// ---- Mode switching ----
const setUserMode = (mode) => {
  userMode.value = mode
  clearUser()
  nextTick(() => {
    if (mode === "request") requestInputRef.value?.focus()
  })
}

// ---- Request mode: quét QR ----
const loadFromRequest = async () => {
  const id = requestInput.value.trim().replace(/^REQ-/i, "")
  if (!id) return
  isLoadingRequest.value = true
  try {
    const res = await api.get(`/BorrowRequests/${id}/for-checkout`)
    if (res.status === 200) {
      const data = res.data
      selectedUser.value = {
        id: data.user.id,
        fullName: data.user.fullName,
        email: data.user.email,
        status: data.user.status,
        expiredDate: data.user.expiredDate,
        studentProfile: data.user.studentProfile,
        staffProfile: data.user.staffProfile,
      }
      fromRequest.value = data.requestId
      suggestedBook.value = data.book
      userWarning.value = ""
      requestInput.value = ""

      // Check user status
      if (data.user.status === 2 || data.user.status === "Blocked") {
        userWarning.value = "Tài khoản đã bị khóa"
      } else if (data.user.expiredDate && new Date(data.user.expiredDate) < new Date()) {
        userWarning.value = "Thẻ thư viện đã hết hạn"
      }

      nextTick(() => barcodeRef.value?.focus())
    }
  } catch (err) {
    alert(err.response?.data?.message || "Không tìm thấy yêu cầu")
  } finally {
    isLoadingRequest.value = false
  }
}
const onQRScanned = (text) => {
  // QR chứa RequestId (VD: "123" hoặc "REQ-123")
  requestInput.value = text.replace(/^REQ-/i, '')
  loadFromRequest()
}

// ---- Manual mode: search user ----
const onUserSearch = () => {
  clearTimeout(userSearchTimer)
  if (userSearch.value.trim().length < 2) {
    userResults.value = []
    return
  }
  userSearchTimer = setTimeout(async () => {
    try {
      const res = await api.get(`/Users?search=${userSearch.value}&pageSize=5`)
      if (res.status === 200) userResults.value = res.data.items
    } catch {}
  }, 400)
}

const selectUser = async (user) => {
  selectedUser.value = user
  userResults.value = []
  userSearch.value = ""
  userWarning.value = ""
  fromRequest.value = null
  suggestedBook.value = null

  if (user.status === 2 || user.status === "Blocked") {
    userWarning.value = "Tài khoản đã bị khóa, không thể mượn sách"
    return
  }
  if (user.expiredDate && new Date(user.expiredDate) < new Date()) {
    userWarning.value = "Thẻ thư viện đã hết hạn"
    return
  }

  await nextTick()
  barcodeRef.value?.focus()
}

const clearUser = () => {
  selectedUser.value = null
  userWarning.value = ""
  fromRequest.value = null
  suggestedBook.value = null
  borrowList.value = []
  barcodeInput.value = ""
  requestInput.value = ""
  userSearch.value = ""
  userResults.value = []
}

// ---- Add copy to list ----
const addByBarcode = async () => {
  const barcode = barcodeInput.value.trim()
  if (!barcode || !selectedUser.value) return

  // Check trùng
  if (borrowList.value.find((c) => c.barcode === barcode)) {
    copyWarning.value = "Bản sao này đã có trong danh sách"
    barcodeInput.value = ""
    return
  }

  isAddingCopy.value = true
  copyWarning.value = ""
  try {
    const res = await api.get(`/BookCopies/all?search=${barcode}&pageSize=1`)
    if (res.status === 200 && res.data.items.length > 0) {
      const copy = res.data.items[0]
      if (copy.barcode !== barcode) {
        copyWarning.value = `Không tìm thấy bản sao với barcode chính xác "${barcode}"`
        return
      }
      if (copy.status !== "Available" && copy.status !== 0) {
        copyWarning.value = `Bản sao không khả dụng (${copy.status})`
        return
      }
      if (copy.isReferenceOnly) {
        copyWarning.value = "Bản sao này chỉ dùng để tham khảo"
        return
      }
      borrowList.value.push(copy)
      barcodeInput.value = ""
      nextTick(() => barcodeRef.value?.focus())
    } else {
      copyWarning.value = `Không tìm thấy bản sao với barcode "${barcode}"`
    }
  } catch {
    copyWarning.value = "Lỗi khi tìm bản sao"
  } finally {
    isAddingCopy.value = false
  }
}

const removeFromList = (idx) => {
  borrowList.value.splice(idx, 1)
}

// ---- Submit ----
const submitBorrow = async () => {
  if (!canSubmit.value) return
  isSubmitting.value = true
  try {
    const payload = {
      userId: selectedUser.value.id,
      requestId: fromRequest.value,
      notes: notes.value || null,
      copies: borrowList.value.map((c) => ({ copyId: c.copyId, barcode: c.barcode })),
    }
    const res = await api.post("/Transactions/batch", payload)
    if (res.status === 200) {
      successUser.value = selectedUser.value.fullName
      successTransactions.value = res.data.transactions
      showSuccess.value = true
    }
  } catch (err) {
    alert(err.response?.data?.message || "Cho mượn thất bại")
  } finally {
    isSubmitting.value = false
  }
}

// ---- Reset ----
const resetForm = () => {
  showSuccess.value = false
  clearUser()
  notes.value = ""
  successTransactions.value = []
  successUser.value = ""
  nextTick(() => {
    if (userMode.value === "request") requestInputRef.value?.focus()
  })
}

// ---- Helpers ----
const initials = (name) => {
  if (!name) return "?"
  return name
    .split(" ")
    .map((w) => w[0])
    .slice(-2)
    .join("")
    .toUpperCase()
}
const formatDate = (d) => (d ? new Date(d).toLocaleDateString("vi-VN") : "—")
const docTypeLabel = (id) => {
  const map = { 1: "Sách vật lý", 3: "Luận án" }
  return map[id] ?? "Khác"
}
const redirectList=()=>{
  router.push({
    name: "transactionManagement",
  })
}
</script>

<style lang="scss" scoped>
.borrow-checkout {
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

.form-layout {
  display: grid;
  grid-template-columns: 1fr 320px;
  gap: 20px;
  align-items: start;

  @media (max-width: 900px) {
    grid-template-columns: 1fr;
  }
}

// Form card
.form-card {
  background: #fff;
  border-radius: 14px;
  border: 1px solid #e0e0e0;
  overflow: hidden;
}

.form-section {
  padding: 20px 24px;
  border-bottom: 1px solid #f0f0f0;

  &:last-of-type {
    border-bottom: none;
  }
}

.section-title {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 14px;
  font-weight: 700;
  color: #1a1a2e;
  margin-bottom: 14px;
}

.section-num {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background: #435ebe;
  color: #fff;
  font-size: 12px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.optional-tag {
  font-size: 11px;
  font-weight: 500;
  color: #999;
  background: #f5f5f5;
  padding: 2px 8px;
  border-radius: 99px;
}

.count-pill {
  margin-left: auto;
  background: #435ebe;
  color: #fff;
  font-size: 12px;
  font-weight: 700;
  padding: 2px 10px;
  border-radius: 99px;
}

// Mode tabs
.mode-tabs {
  display: flex;
  gap: 0;
  margin-bottom: 14px;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  overflow: hidden;
}

.mode-tab {
  flex: 1;
  padding: 10px;
  background: #fff;
  border: none;
  cursor: pointer;
  font-size: 13px;
  font-weight: 500;
  color: #555;
  transition: all 0.15s;

  &:hover {
    background: #f5f5f5;
  }

  &.active {
    background: #435ebe;
    color: #fff;
  }
}

// Search
.search-group {
  display: flex;
  gap: 8px;
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
    border-color: #435ebe;
  }

  &:disabled {
    background: #f5f5f5;
  }
}

.barcode-input {
  font-family: monospace;
  font-size: 15px;
  letter-spacing: 1px;
}

.btn-search {
  padding: 8px 12px;
  background: #435ebe;
  color: #fff;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 16px;
  min-width: 44px;

  &:hover:not(:disabled) {
    background: #2c3a8c;
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

.hint {
  font-size: 12px;
  color: #666;
  margin-top: 6px;
}

// Search results
.search-results {
  margin-top: 6px;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  overflow: hidden;
}

.result-item {
  padding: 10px 14px;
  cursor: pointer;
  transition: background 0.1s;
  border-bottom: 1px solid #f0f0f0;

  &:last-child {
    border-bottom: none;
  }

  &:hover {
    background: #f5f6ff;
  }
}

.result-name {
  font-size: 14px;
  font-weight: 600;
}

.result-sub {
  font-size: 12px;
  color: #999;
  margin-top: 2px;
}

// Selected card
.selected-card {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-top: 8px;
  padding: 12px 14px;
  border-radius: 10px;
  border: 1.5px solid #c5cae9;
  background: #f0f4ff;
  position: relative;
}

.selected-info {
  display: flex;
  align-items: center;
  gap: 10px;
  flex: 1;
}

.selected-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: #435ebe;
  color: #fff;
  font-size: 13px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.selected-name {
  font-size: 14px;
  font-weight: 700;
}

.selected-sub {
  font-size: 12px;
  color: #666;
  margin-top: 2px;
}

.selected-meta {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}

.btn-clear {
  background: none;
  border: none;
  cursor: pointer;
  color: #aaa;
  font-size: 16px;
  padding: 2px 6px;
  border-radius: 4px;

  &:hover {
    color: #333;
    background: #e0e0e0;
  }
}

// Badges
.meta-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 99px;
  font-size: 11px;
  font-weight: 600;

  &.badge-green {
    background: #e8f5e9;
    color: #2e7d32;
  }
}

.warn-box {
  margin-top: 10px;
  padding: 10px 14px;
  background: #fff3e0;
  border-left: 3px solid #fb8c00;
  border-radius: 0 8px 8px 0;
  font-size: 13px;
  color: #e65100;
}

// Suggested book
.suggested-book {
  margin-bottom: 12px;
  padding: 10px 14px;
  background: #e3f2fd;
  border-left: 3px solid #1e88e5;
  border-radius: 0 8px 8px 0;
}

.suggested-title {
  font-size: 13px;
  color: #1565c0;
  margin-bottom: 4px;
}

.suggested-hint {
  font-size: 12px;
  color: #666;
}

// Borrow list
.borrow-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 12px;
}

.borrow-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 14px;
  background: #f9f9f9;
  border-radius: 8px;
  border: 1px solid #e8e8e8;
}

.item-num {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background: #fff;
  color: #666;
  font-size: 12px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  border: 1.5px solid #e0e0e0;
}

.item-info {
  flex: 1;
  min-width: 0;
}

.item-title {
  font-size: 14px;
  font-weight: 600;
  color: #1a1a2e;
}

.item-meta {
  font-size: 12px;
  color: #999;
  margin-top: 2px;
}

.code-text {
  font-family: monospace;
  color: #435ebe;
  font-weight: 600;
}

.btn-remove {
  background: none;
  border: none;
  cursor: pointer;
  color: #c62828;
  font-size: 14px;
  padding: 4px 8px;
  border-radius: 4px;

  &:hover {
    background: #ffebee;
  }
}

.empty-list {
  margin-top: 12px;
  padding: 20px;
  text-align: center;
  color: #aaa;
  background: #fafafa;
  border-radius: 8px;
  font-size: 13px;
}

// Notes
.notes-input {
  width: 100%;
  padding: 8px 12px;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  font-size: 14px;
  outline: none;
  font-family: inherit;
  resize: vertical;
  box-sizing: border-box;
  background: #ffffff;
  color: #333333;

  &:focus {
    border-color: #435ebe;
  }
}

// Submit
.form-actions {
  padding: 20px 24px;
  background: #fafafa;
}

.btn-submit {
  width: 100%;
  justify-content: center;
  font-size: 15px;
  padding: 12px;
}

// Preview card
.preview-card {
  background: #fff;
  border-radius: 14px;
  border: 1px solid #e0e0e0;
  padding: 20px;
  position: sticky;
  top: 20px;
}

.preview-title {
  font-size: 14px;
  font-weight: 700;
  color: #435ebe;
  margin-bottom: 16px;
  padding-bottom: 8px;
  border-bottom: 1.5px solid #e8eaf6;
}

.preview-empty {
  color: #aaa;
  font-size: 13px;
  text-align: center;
  padding: 20px 0;
}

.preview-rows {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.preview-row {
  display: flex;
  justify-content: space-between;
  gap: 8px;
}

.preview-label {
  font-size: 13px;
  color: #888;
}

.preview-value {
  font-size: 13px;
  font-weight: 500;
  text-align: right;
}

.breakdown {
  margin-top: 14px;
  padding-top: 14px;
  border-top: 1px solid #f0f0f0;
}

.breakdown-title {
  font-size: 12px;
  font-weight: 700;
  color: #666;
  margin-bottom: 8px;
  text-transform: uppercase;
}

.breakdown-row {
  display: flex;
  justify-content: space-between;
  padding: 6px 0;
  font-size: 13px;
}

.breakdown-label {
  color: #555;
}

.breakdown-count {
  color: #435ebe;
  font-weight: 600;
}

.ready-box {
  margin-top: 16px;
  padding: 10px 14px;
  background: #e8f5e9;
  border-radius: 8px;
  font-size: 13px;
  color: #2e7d32;
  font-weight: 600;
  text-align: center;
  .icon_success {
    margin-bottom: 2.5px;
    margin-right: 4px;
    color: #2e7d32;
  }
}

// Buttons
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

  &.btn-primary {
    background: #435ebe;
    color: #fff;

    &:hover:not(:disabled) {
      background: #2c3a8c;
    }
  }

  &.btn-outline {
    background: #fff;
    color: #435ebe;
    border: 1.5px solid #435ebe;

    &:hover:not(:disabled) {
      background: #e8eaf6;
    }
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
  max-width: 600px;
}

.success-modal {
  padding: 32px 28px;
  text-align: center;
}

.success-icon {
  font-size: 52px;
  margin-bottom: 10px;
  color: #2e7d32;
}

.success-title {
  font-size: 20px;
  font-weight: 800;
  margin: 0 0 6px;
  color: #2e7d32;
}

.success-desc {
  font-size: 14px;
  color: #666;
  margin-bottom: 20px;
}

.success-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  text-align: left;
  background: #f9f9f9;
  border-radius: 10px;
  padding: 12px;
  margin-bottom: 20px;
  max-height: 280px;
  overflow-y: auto;
}

.success-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 12px;
  background: #fff;
  border-radius: 8px;
}

.success-item-info {
  flex: 1;
  min-width: 0;
}

.success-item-title {
  font-size: 13px;
  font-weight: 600;
}

.success-item-meta {
  font-size: 12px;
  color: #999;
  margin-top: 2px;
}

.success-item-due {
  font-size: 12px;
  color: #2e7d32;
  font-weight: 600;
  white-space: nowrap;
}

.success-actions {
  display: flex;
  gap: 10px;
  justify-content: center;
}
</style>