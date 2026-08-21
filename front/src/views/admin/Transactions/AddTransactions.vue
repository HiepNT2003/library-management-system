<template>
  <div class="borrow-direct">
    <!-- Header -->
    <div class="page-header">
      <div>
        <h1 class="page-title">Cho mượn trực tiếp</h1>
        <p class="page-desc">Tạo giao dịch mượn sách không qua yêu cầu online</p>
      </div>
    </div>

    <div class="form-layout">
      <!-- Left: Form nhập -->
      <div class="form-card">
        <!-- Section: Bạn đọc -->
        <div class="form-section">
          <div class="section-title"><span class="section-num">1</span> Bạn đọc</div>

          <div class="search-group">
            <input
              v-model="userSearch"
              class="search-input"
              placeholder="Nhập tên, email hoặc mã sinh viên..."
              @input="onUserSearch"
              @keydown.enter="searchUser"
            />
            <button class="btn-search" @click="searchUser" :disabled="isSearchingUser">
              {{ isSearchingUser ? "..." : "🔍" }}
            </button>
          </div>

          <!-- User results -->
          <div class="search-results" v-if="userResults.length > 0 && !selectedUser">
            <div v-for="u in userResults" :key="u.id" class="result-item" @click="selectUser(u)">
              <div class="result-name">{{ u.fullName }}</div>
              <div class="result-sub">
                {{ u.studentProfile?.studentCode || u.staffProfile?.staffCode || u.email }}
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
            <div class="selected-meta">
              <span class="meta-badge" :class="userStatusClass">{{ userStatusLabel }}</span>
              <span
                v-if="selectedUser.expiredDate"
                class="meta-badge"
                :class="isExpired ? 'badge-red' : 'badge-gray'"
              >
                Thẻ hết hạn {{ formatDate(selectedUser.expiredDate) }}
              </span>
            </div>
            <button class="btn-clear" @click="clearUser">✕</button>
          </div>

          <!-- User warnings -->
          <div class="warn-box" v-if="userWarning">⚠️ {{ userWarning }}</div>
        </div>

        <!-- Section: Bản sao sách -->
        <div class="form-section">
          <div class="section-title"><span class="section-num">2</span> Bản sao sách</div>

          <div class="copy-tabs">
            <button
              class="copy-tab"
              :class="{ active: copyMode === 'barcode' }"
              @click="
                copyMode = 'barcode';
                clearCopy()
              "
            >
              Quét barcode
            </button>
            <button
              class="copy-tab"
              :class="{ active: copyMode === 'search' }"
              @click="
                copyMode = 'search';
                clearCopy()
              "
            >
              Tìm theo tên sách
            </button>
          </div>

          <!-- Barcode mode -->
          <div v-if="copyMode === 'barcode'" class="search-group">
            <input
              v-model="barcodeInput"
              ref="barcodeRef"
              class="search-input barcode-input"
              placeholder="Quét hoặc nhập barcode..."
              @keydown.enter="searchByBarcode"
            />
            <button class="btn-search" @click="searchByBarcode" :disabled="isSearchingCopy">
              <Icon v-if="!isSearchingCopy" icon="ic:sharp-search" width="24" height="24" />
              {{ isSearchingCopy ? "..." : "" }}
            </button>
          </div>

          <!-- Search by title mode -->
          <div v-if="copyMode === 'search'">
            <div class="search-group">
              <input
                v-model="bookSearch"
                class="search-input"
                placeholder="Nhập tên sách..."
                @input="onBookSearch"
              />
            </div>
            <div class="search-results" v-if="bookResults.length > 0 && !selectedCopy">
              <div
                v-for="copy in bookResults"
                :key="copy.copyId"
                class="result-item"
                @click="selectCopy(copy)"
              >
                <div class="result-name">{{ copy.bookTitle }}</div>
                <div class="result-sub">
                  Barcode: <span class="code-text">{{ copy.barcode }}</span> ·
                  {{ copy.warehouseName || copy.shelfLocation || "" }}
                </div>
              </div>
            </div>
          </div>

          <!-- Selected copy -->
          <div class="selected-card copy-card" v-if="selectedCopy">
            <div class="selected-info">
              <div class="copy-icon">📖</div>
              <div>
                <div class="selected-name">{{ selectedCopy.bookTitle }}</div>
                <div class="selected-sub">
                  Barcode: <span class="code-text">{{ selectedCopy.barcode }}</span>
                  <span v-if="selectedCopy.shelfLocation"> · {{ selectedCopy.shelfLocation }}</span>
                </div>
              </div>
            </div>
            <button class="btn-clear" @click="clearCopy">✕</button>
          </div>

          <div class="warn-box" v-if="copyWarning">⚠️ {{ copyWarning }}</div>
        </div>

        <!-- Section: Từ yêu cầu (tuỳ chọn) -->
        <div class="form-section">
          <div class="section-title">
            <span class="section-num">3</span> Từ yêu cầu đã duyệt
            <span class="optional-tag">Tuỳ chọn</span>
          </div>
          <div class="search-group">
            <input
              v-model="requestIdInput"
              class="search-input"
              placeholder="Nhập mã yêu cầu (RequestId)..."
              type="number"
            />
          </div>
        </div>

        <!-- Section: Ghi chú -->
        <div class="form-section">
          <div class="section-title">
            <span class="section-num">4</span> Ghi chú
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
            {{ isSubmitting ? "Đang xử lý..." : "✓ Xác nhận cho mượn" }}
          </button>
        </div>
      </div>

      <!-- Right: Preview thông tin -->
      <div class="preview-card">
        <div class="preview-title">Thông tin giao dịch</div>

        <div v-if="!selectedUser && !selectedCopy" class="preview-empty">
          Chọn bạn đọc và bản sao sách để xem thông tin giao dịch
        </div>

        <template v-else>
          <div class="preview-rows">
            <div class="preview-row">
              <span class="preview-label">Bạn đọc</span>
              <span class="preview-value">{{ selectedUser?.fullName || "—" }}</span>
            </div>
            <div class="preview-row">
              <span class="preview-label">Sách</span>
              <span class="preview-value">{{ selectedCopy?.bookTitle || "—" }}</span>
            </div>
            <div class="preview-row">
              <span class="preview-label">Barcode</span>
              <span class="preview-value code-text">{{ selectedCopy?.barcode || "—" }}</span>
            </div>
            <div class="preview-row">
              <span class="preview-label">Ngày mượn</span>
              <span class="preview-value">{{ formatDate(new Date()) }}</span>
            </div>
            <div class="preview-row" v-if="policy">
              <span class="preview-label">Hạn trả</span>
              <span class="preview-value due-date">
                {{ formatDate(dueDate) }}
                <span class="due-days">({{ policy.maxBorrowDays }} ngày)</span>
              </span>
            </div>
            <div class="preview-row" v-if="policy">
              <span class="preview-label">Đang mượn</span>
              <span class="preview-value"> {{ borrowingCount }} / {{ policy.maxBooks }} cuốn </span>
            </div>
            <div class="preview-row" v-if="autoRequestId">
              <span class="preview-label">Yêu cầu</span>
              <span class="preview-value">
                <span class="badge-green-sm">✓ Khớp yêu cầu #{{ autoRequestId }}</span>
              </span>
            </div>
          </div>

          <!-- Policy warning -->
          <div class="warn-box" v-if="policy && borrowingCount >= policy.maxBooks">
            ⚠️ Bạn đọc đã đạt giới hạn {{ policy.maxBooks }} cuốn
          </div>

          <div class="ready-box" v-if="canSubmit">✅ Sẵn sàng cho mượn</div>
        </template>
      </div>
    </div>

    <!-- Success modal -->
    <Teleport to="body">
      <div v-if="showSuccess" class="modal-overlay">
        <div class="modal modal-sm success-modal">
          <div class="success-icon">✅</div>
          <h3 class="success-title">Cho mượn thành công!</h3>
          <div class="success-rows">
            <div class="success-row">
              <span>Bạn đọc</span>
              <strong>{{ successData?.user }}</strong>
            </div>
            <div class="success-row">
              <span>Sách</span>
              <strong>{{ successData?.bookTitle }}</strong>
            </div>
            <div class="success-row">
              <span>Barcode</span>
              <span class="code-text">{{ successData?.barcode }}</span>
            </div>
            <div class="success-row">
              <span>Hạn trả</span>
              <strong class="due-date">{{ successData?.dueDate }}</strong>
            </div>
          </div>
          <div class="success-actions">
            <button class="btn btn-outline" @click="resetForm">Cho mượn tiếp</button>
            <button class="btn btn-primary" @click="$router.push('/admin/transactions')">
              Xem danh sách
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, nextTick } from "vue"
import { useRouter } from "vue-router"
import api from "../../../services/api"
import { Icon } from "@iconify/vue"

const router = useRouter()

// ---- State ----
const userSearch = ref("")
const userResults = ref([])
const selectedUser = ref(null)
const isSearchingUser = ref(false)
const userWarning = ref("")
const borrowingCount = ref(0)
const policy = ref(null)

const copyMode = ref("barcode")
const barcodeInput = ref("")
const bookSearch = ref("")
const bookResults = ref([])
const selectedCopy = ref(null)
const isSearchingCopy = ref(false)
const copyWarning = ref("")
const barcodeRef = ref(null)

const requestIdInput = ref("")
const notes = ref("")
const isSubmitting = ref(false)
const showSuccess = ref(false)
const successData = ref(null)

let userSearchTimer = null
let bookSearchTimer = null

// ---- Computed ----
const canSubmit = computed(() => {
  if (!selectedUser.value || !selectedCopy.value) return false
  if (userWarning.value) return false
  if (copyWarning.value) return false
  if (policy.value && borrowingCount.value >= policy.value.maxBooks) return false
  return true
})

const isExpired = computed(() => {
  if (!selectedUser.value?.expiredDate) return false
  return new Date(selectedUser.value.expiredDate) < new Date()
})

const userStatusLabel = computed(() => {
  const map = {
    0: "Hoạt động",
    1: "Chưa kích hoạt",
    2: "Đã khóa",
    Active: "Hoạt động",
    Blocked: "Đã khóa",
  }
  return map[selectedUser.value?.status] ?? ""
})

const userStatusClass = computed(() => {
  const s = selectedUser.value?.status
  if (s === 0 || s === "Active") return "badge-green"
  if (s === 2 || s === "Blocked") return "badge-red"
  return "badge-gray"
})

const dueDate = computed(() => {
  if (!policy.value) return null
  const d = new Date()
  d.setDate(d.getDate() + policy.value.maxBorrowDays)
  return d
})

// ---- User search ----
const onUserSearch = () => {
  clearTimeout(userSearchTimer)
  if (userSearch.value.trim().length < 2) {
    userResults.value = []
    return
  }
  userSearchTimer = setTimeout(searchUser, 400)
}

const searchUser = async () => {
  if (!userSearch.value.trim()) return
  isSearchingUser.value = true
  try {
    const res = await api.get(`/Users?search=${userSearch.value}&pageSize=5`)
    if (res.status === 200) userResults.value = res.data.items
  } catch {
  } finally {
    isSearchingUser.value = false
  }
}

const selectUser = async (user) => {
  selectedUser.value = user
  userResults.value = []
  userSearch.value = ""
  userWarning.value = ""

  // Kiểm tra tài khoản
  if (user.status === 2 || user.status === "Blocked") {
    userWarning.value = "Tài khoản đã bị khóa, không thể mượn sách"
    return
  }
  if (user.expiredDate && new Date(user.expiredDate) < new Date()) {
    userWarning.value = "Thẻ thư viện đã hết hạn"
    return
  }

  // Lấy số sách đang mượn
  borrowingCount.value = user.borrowingCount ?? 0

  // Focus vào barcode input
  await nextTick()
  barcodeRef.value?.focus()
}

const clearUser = () => {
  selectedUser.value = null
  userWarning.value = ""
  borrowingCount.value = 0
  policy.value = null
}

// ---- Copy search ----
const searchByBarcode = async () => {
  if (!barcodeInput.value.trim()) return
  isSearchingCopy.value = true
  copyWarning.value = ""
  try {
    const res = await api.get(`/BookCopies/all?search=${barcodeInput.value.trim()}&pageSize=1`)
    if (res.status === 200 && res.data.items.length > 0) {
      const copy = res.data.items[0]
      await selectCopy(copy)
    } else {
      copyWarning.value = `Không tìm thấy bản sao với barcode "${barcodeInput.value}"`
    }
  } catch {
    copyWarning.value = "Lỗi khi tìm bản sao"
  } finally {
    isSearchingCopy.value = false
  }
}

const onBookSearch = () => {
  clearTimeout(bookSearchTimer)
  if (bookSearch.value.trim().length < 2) {
    bookResults.value = []
    return
  }
  bookSearchTimer = setTimeout(async () => {
    try {
      const res = await api.get(
        `/BookCopies/all?search=${bookSearch.value}&status=Available&pageSize=10`
      )
      if (res.status === 200) bookResults.value = res.data.items
    } catch {}
  }, 400)
}

const selectCopy = async (copy) => {
  copyWarning.value = ""
  if (copy.status !== "Available" && copy.status !== 0) {
    copyWarning.value = `Bản sao này không khả dụng (${copy.status})`
    return
  }
  if (copy.isReferenceOnly) {
    copyWarning.value = "Bản sao này chỉ dùng để tham khảo, không cho mượn"
    return
  }
  selectedCopy.value = copy
  bookResults.value = []
  bookSearch.value = ""
  barcodeInput.value = ""

  if (selectedUser.value) {
    await fetchPolicy(copy)
    // Tự tìm request đã duyệt
    autoRequestId.value = await findApprovedRequest(selectedUser.value.id, copy.bookId)
  }
}

const autoRequestId = ref(null)

const findApprovedRequest = async (userId, bookId) => {
  try {
    const res = await api.get(`/BorrowRequests?status=1&search=${userId}&pageSize=5`)
    if (res.status === 200) {
      const match = res.data.items.find((r) => r.book?.bookId === bookId)
      return match?.requestId ?? null
    }
  } catch {}
  return null
}

const fetchPolicy = async (copy) => {
  try {
    const res = await api.get(
      `/BorrowRequests/policy?userId=${selectedUser.value.id}&documentTypeId=${
        copy.documentTypeId ?? ""
      }`
    )
    if (res.status === 200) policy.value = res.data
  } catch {
    policy.value = null
  }
}

const clearCopy = () => {
  selectedCopy.value = null
  copyWarning.value = ""
  barcodeInput.value = ""
  bookSearch.value = ""
  bookResults.value = []
  policy.value = null
}

// ---- Submit ----
const submitBorrow = async () => {
  if (!canSubmit.value) return
  isSubmitting.value = true
  try {
    const payload = {
      userId: selectedUser.value.id,
      copyId: selectedCopy.value.copyId,
      barcode: selectedCopy.value.barcode,
      requestId: autoRequestId.value, // tự động, không cần nhập tay
      notes: notes.value || null,
    }
    const res = await api.post("/Transactions", payload)
    if (res.status === 201) {
      successData.value = {
        user: selectedUser.value.fullName,
        bookTitle: selectedCopy.value.bookTitle,
        barcode: selectedCopy.value.barcode,
        dueDate: formatDate(new Date(res.data.dueDate)),
      }
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
  selectedUser.value = null
  selectedCopy.value = null
  userSearch.value = ""
  barcodeInput.value = ""
  bookSearch.value = ""
  userWarning.value = ""
  copyWarning.value = ""
  requestIdInput.value = ""
  notes.value = ""
  policy.value = null
  borrowingCount.value = 0
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
const formatDate = (d) => {
  if (!d) return "—"
  return new Date(d).toLocaleDateString("vi-VN")
}
</script>

<style lang="scss" scoped>
.borrow-direct {
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
}
.barcode-input {
  font-family: monospace;
  font-size: 15px;
  letter-spacing: 1px;
}
.btn-search {
  padding: 9px 14px;
  background: #435ebe;
  color: #fff;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 16px;
  &:hover:not(:disabled) {
    background: #435ebe;
  }
  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

// Search results dropdown
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

// Selected cards
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
.copy-icon {
  font-size: 28px;
}
.selected-name {
  font-size: 14px;
  font-weight: 700;
  color: #1a1a2e;
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
  padding: 2px 4px;
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
  &.badge-red {
    background: #ffebee;
    color: #c62828;
  }
  &.badge-gray {
    background: #f5f5f5;
    color: #757575;
  }
}

// Warnings
.warn-box {
  margin-top: 10px;
  padding: 10px 14px;
  background: #fff3e0;
  border-left: 3px solid #fb8c00;
  border-radius: 0 8px 8px 0;
  font-size: 13px;
  color: #e65100;
}

// Copy tabs
.copy-tabs {
  display: flex;
  gap: 0;
  margin-bottom: 14px;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  overflow: hidden;
}
.copy-tab {
  flex: 1;
  padding: 8px;
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
  background: transparent;
  color: #333333;
  &:focus {
    border-color: #435ebe;
  }
}

// Submit
.form-actions {
  padding: 20px 24px;
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
  align-items: flex-start;
  gap: 8px;
}
.preview-label {
  font-size: 13px;
  color: #888;
  white-space: nowrap;
}
.preview-value {
  font-size: 13px;
  font-weight: 500;
  text-align: right;
}
.due-date {
  color: #2e7d32;
  font-weight: 700;
}
.due-days {
  font-size: 11px;
  color: #999;
  font-weight: 400;
}
.code-text {
  font-family: monospace;
  color: #435ebe;
  font-weight: 600;
  font-size: 13px;
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
      background: #435ebe;
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
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.2);
}
.modal-sm {
  max-width: 400px;
}

// Success modal
.success-modal {
  padding: 36px 28px;
  text-align: center;
}
.success-icon {
  font-size: 52px;
  margin-bottom: 12px;
}
.success-title {
  font-size: 20px;
  font-weight: 800;
  margin: 0 0 20px;
  color: #2e7d32;
}
.success-rows {
  display: flex;
  flex-direction: column;
  gap: 10px;
  text-align: left;
  background: #f9f9f9;
  border-radius: 10px;
  padding: 14px 16px;
  margin-bottom: 20px;
}
.success-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  font-size: 14px;
  color: #555;
  strong {
    color: #1a1a2e;
  }
}
.success-actions {
  display: flex;
  gap: 10px;
  justify-content: center;
}
</style>