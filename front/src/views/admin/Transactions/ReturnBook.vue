<template>
  <div class="return-book">
    <!-- Header -->
    <div class="page-header">
      <div>
        <h1 class="page-title">Trả sách</h1>
        <p class="page-desc">Xử lý nhận trả sách từ bạn đọc</p>
      </div>
      <button class="btn btn-outline" @click="backToList">
        <Icon icon="mingcute:arrow-left-line" width="20" height="20" /> Danh sách mượn trả
      </button>
    </div>

    <div class="form-layout">
      <!-- Left: Tìm giao dịch -->
      <div class="form-card">
        <!-- Quét barcode -->
        <div class="form-section">
          <div class="section-title"><span class="section-num">1</span> Quét barcode sách trả</div>
          <div class="search-group">
            <input v-model="barcodeInput" ref="barcodeRef" class="search-input barcode-input"
              placeholder="Quét hoặc nhập barcode sách..." @keydown.enter="searchTransaction" />
            <button class="btn-search" @click="searchTransaction" :disabled="isSearching">
              <Icon v-if="!isSearching" icon="ic:sharp-search" width="24" height="24" />
              {{ isSearching ? "..." : "" }}
            </button>
          </div>
          <div class="warn-box" v-if="searchError">⚠️ {{ searchError }}</div>
        </div>

        <!-- Thông tin giao dịch -->
        <template v-if="transaction">
          <div class="form-section">
            <div class="section-title"><span class="section-num">2</span> Thông tin giao dịch</div>

            <div class="tx-info">
              <!-- User -->
              <div class="tx-user">
                <div class="tx-avatar">{{ initials(transaction.user?.fullName) }}</div>
                <div>
                  <div class="tx-user-name">{{ transaction.user?.fullName }}</div>
                  <div class="tx-user-code">
                    {{ transaction.user?.studentCode || transaction.user?.email }}
                  </div>
                </div>
              </div>

              <!-- Book -->
              <div class="tx-book">
                <div class="tx-book-title">{{ transaction.copy?.bookTitle }}</div>
                <div class="tx-book-meta">
                  Barcode: <span class="code-text">{{ transaction.copy?.barcode }}</span>
                  <span v-if="transaction.copy?.shelfLocation">
                    · {{ transaction.copy.shelfLocation }}</span>
                </div>
              </div>

              <!-- Dates -->
              <div class="tx-dates">
                <div class="date-item">
                  <span class="date-label">Ngày mượn</span>
                  <span class="date-value">{{ formatDate(transaction.borrowDate) }}</span>
                </div>
                <div class="date-item">
                  <span class="date-label">Hạn trả</span>
                  <span class="date-value" :class="{ 'text-red': transaction.overdueDays > 0 }">
                    {{ formatDate(transaction.dueDate) }}
                  </span>
                </div>
                <div class="date-item">
                  <span class="date-label">Ngày trả</span>
                  <span class="date-value text-green">{{ formatDate(new Date()) }}</span>
                </div>
              </div>

              <!-- Overdue warning -->
              <div class="overdue-box" v-if="transaction.overdueDays > 0">
                <div class="overdue-title">
                  <Icon icon="pajamas:calendar-overdue" width="16" height="16" /> Quá hạn
                  {{ transaction.overdueDays }} ngày
                </div>
                <div class="overdue-fine">
                  Phạt dự kiến: <strong>{{ formatMoney(estimatedOverdueFine) }}</strong>
                  <span class="fine-calc">({{ transaction.overdueDays }} ×
                    {{ formatMoney(transaction.finePerDay) }}/ngày)</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Return condition -->
          <div class="form-section">
            <div class="section-title">
              <span class="section-num">3</span> Tình trạng sách khi trả
            </div>
            <div class="condition-grid">
              <div v-for="c in conditions" :key="c.value" class="condition-card"
                :class="{ active: returnForm.returnCondition === c.value, [c.color]: true }"
                @click="returnForm.returnCondition = c.value">
                <Icon class="condition-icon" :class="c.color" :icon="c.icon" width="36" height="36" />
                <span class="condition-label">{{ c.label }}</span>
              </div>
            </div>

            <!-- Damage/Lost details -->
            <div class="fine-detail" v-if="returnForm.returnCondition === 'Hư hỏng'">
              <label>Mô tả hư hỏng</label>
              <input v-model="returnForm.damageNote" placeholder="VD: Rách bìa, ướt trang..." />

              <label>Cách tính phạt</label>
              <div class="calc-mode">
                <label>
                  <input type="radio" v-model="damageCalcMode" value="percent" />
                  Theo % hư hỏng
                </label>
                <label>
                  <input type="radio" v-model="damageCalcMode" value="manual" />
                  Nhập số tiền
                </label>
              </div>

              <template v-if="damageCalcMode === 'percent'">
                <label>% Hư hỏng</label>
                <div class="percent-row">
                  <input type="number" v-model.number="damagePercent" min="1" max="100" placeholder="VD: 30"
                    @input="calcDamageFine" />
                  <span>%</span>
                  <span class="calc-result" v-if="transaction?.bookPrice">
                    = {{ formatMoney(returnForm.damageFineAmount) }}
                  </span>
                </div>
                <div class="book-price-hint" v-if="transaction?.bookPrice">
                  Giá sách: {{ formatMoney(transaction.bookPrice) }}
                </div>
                <div class="warn-box" v-else>
                  ⚠️ Sách chưa có giá — vui lòng nhập số tiền thủ công
                </div>
              </template>

              <template v-if="damageCalcMode === 'manual'">
                <label>Số tiền phạt</label>
                <input type="number" v-model.number="returnForm.damageFineAmount" placeholder="Nhập số tiền (VNĐ)" />
              </template>
            </div>

            <div class="fine-detail" v-if="returnForm.returnCondition === 'Mất'">
              <label>Ghi chú</label>
              <input v-model="returnForm.lostNote" placeholder="VD: Người mượn khai mất..." />

              <div class="lost-fine-auto" v-if="transaction?.bookPrice">
                <div class="lost-fine-label">Tiền phạt (100% giá sách)</div>
                <div class="lost-fine-amount">{{ formatMoney(returnForm.lostFineAmount) }}</div>
                <button class="btn-link" @click="returnForm.lostFineAmount = null">
                  Nhập thủ công
                </button>
              </div>

              <template v-if="!transaction?.bookPrice || returnForm.lostFineAmount === null">
                <label>Số tiền phạt</label>
                <input type="number" v-model.number="returnForm.lostFineAmount" placeholder="Nhập số tiền (VNĐ)" />
              </template>
            </div>
          </div>

          <!-- Overdue fine config -->
          <div class="form-section" v-if="transaction.overdueDays > 0">
            <div class="section-title"><span class="section-num">4</span> Phạt quá hạn</div>
            <div class="overdue-fine-config">
              <label class="toggle-label">
                <input type="checkbox" v-model="returnForm.createOverdueFine" />
                Tạo phiếu phạt quá hạn
              </label>
              <div v-if="returnForm.createOverdueFine" class="fine-amount-row">
                <span>Số tiền:</span>
                <input type="number" v-model.number="returnForm.overdueFineAmount" class="fine-input" />
                <span>VNĐ</span>
                <button class="btn-reset-fine" @click="resetOverdueFine">Tính lại</button>
              </div>
            </div>
          </div>

          <!-- Notes -->
          <div class="form-section">
            <div class="section-title">
              <span class="section-num">{{ transaction.overdueDays > 0 ? 5 : 4 }}</span> Ghi chú
              <span class="optional-tag">Tuỳ chọn</span>
            </div>
            <textarea v-model="returnForm.notes" class="notes-input" rows="2"
              placeholder="Ghi chú thêm nếu có..."></textarea>
          </div>

          <!-- Submit -->
          <div class="form-actions">
            <div class="extend-option" v-if="transaction && !isOverdue(transaction)">
              <button class="btn btn-extend-full" @click="extendInstead" :disabled="isExtending">
                {{ isExtending ? 'Đang gia hạn...' : '📅 Gia hạn thay vì trả' }}
              </button>
            </div>
            <button class="btn btn-primary btn-submit" @click="submitReturn" :disabled="isSubmitting">
              {{ isSubmitting ? "Đang xử lý..." : "✓ Xác nhận trả sách" }}
            </button>
          </div>
        </template>
      </div>

      <!-- Right: Tóm tắt -->
      <div class="preview-card">
        <div class="preview-title">Tóm tắt xử lý</div>

        <div v-if="!transaction" class="preview-empty">Quét barcode sách để bắt đầu</div>

        <template v-else>
          <div class="preview-rows">
            <div class="preview-row">
              <span class="preview-label">Tình trạng</span>
              <span class="preview-value">{{ returnForm.returnCondition }}</span>
            </div>
            <div class="preview-row" v-if="transaction.overdueDays > 0">
              <span class="preview-label">Quá hạn</span>
              <span class="preview-value text-red">{{ transaction.overdueDays }} ngày</span>
            </div>
          </div>

          <!-- Fines summary -->
          <div class="fines-summary">
            <div class="fines-summary-title">Tiền phạt</div>
            <div class="fine-row" v-if="returnForm.createOverdueFine && transaction.overdueDays > 0">
              <span>Phạt quá hạn</span>
              <strong>{{ formatMoney(returnForm.overdueFineAmount) }}</strong>
            </div>
            <div class="fine-row" v-if="returnForm.returnCondition === 'Hư hỏng' && returnForm.damageFineAmount">
              <span>Phạt hư hỏng</span>
              <strong>{{ formatMoney(returnForm.damageFineAmount) }}</strong>
            </div>
            <div class="fine-row" v-if="returnForm.returnCondition === 'Mất' && returnForm.lostFineAmount">
              <span>Phạt mất sách</span>
              <strong>{{ formatMoney(returnForm.lostFineAmount) }}</strong>
            </div>
            <div class="fine-total" v-if="totalFine > 0">
              <span>Tổng phạt</span>
              <strong class="text-red">{{ formatMoney(totalFine) }}</strong>
            </div>
            <div class="fine-zero" v-else>Không có tiền phạt</div>
          </div>
        </template>
      </div>
    </div>

    <!-- Success modal -->
    <Teleport to="body">
      <div v-if="showSuccess" class="modal-overlay">
        <div class="modal success-modal">
          <div class="success-icon">
            <Icon icon="qlementine-icons:success-16" width="56" height="56" />
          </div>
          <h3 class="success-title">Trả sách thành công!</h3>
          <div class="success-rows">
            <div class="success-row">
              <span>Bạn đọc</span>
              <strong>{{ successData?.user }}</strong>
            </div>
            <div class="success-row">
              <span>Sách</span>
              <strong>{{ successData?.bookTitle }}</strong>
            </div>
            <div class="success-row" v-if="successData?.totalFine > 0">
              <span>Tổng phạt</span>
              <strong class="text-red">{{ formatMoney(successData?.totalFine) }}</strong>
            </div>
          </div>
          <div class="success-actions">
            <button class="btn btn-outline" @click="resetForm">Trả sách tiếp</button>
            <button class="btn btn-primary" @click="backToList">Xem danh sách</button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, nextTick, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import api from "../../../services/api"
import { Icon } from "@iconify/vue"

const route = useRoute()
const router = useRouter()

const barcodeInput = ref("")
const barcodeRef = ref(null)
const isSearching = ref(false)
const searchError = ref("")
const transaction = ref(null)
const isSubmitting = ref(false)
const showSuccess = ref(false)
const successData = ref(null)

const returnForm = reactive({
  returnCondition: "Tốt",
  createOverdueFine: true,
  overdueFineAmount: 0,
  damageFineAmount: null,
  damageNote: "",
  lostFineAmount: null,
  lostNote: "",
  notes: "",
})

const conditions = [
  { value: "Tốt", label: "Tốt", icon: "qlementine-icons:success-16", color: "cond-green" },
  { value: "Bình thường", label: "Bình thường", icon: "boxicons:book-filled", color: "cond-blue" },
  { value: "Hư hỏng", label: "Hư hỏng", icon: "typcn:warning", color: "cond-orange" },
  { value: "Mất", label: "Mất", icon: "line-md:file-remove-filled", color: "cond-red" },
]

onMounted(async () => {
  await nextTick()
  barcodeRef.value?.focus()
  // Nếu có barcode từ query param (từ nút trả sách trên danh sách)
  if (route.query.barcode) {
    barcodeInput.value = route.query.barcode
    await searchTransaction()
  }
})

const searchTransaction = async () => {
  const barcode = barcodeInput.value.trim()
  if (!barcode) return
  isSearching.value = true
  searchError.value = ""
  transaction.value = null
  try {
    // Tìm transaction đang mượn theo barcode
    const res = await api.get(`/Transactions?search=${barcode}&status=Borrowed&pageSize=1`)
    if (res.status === 200 && res.data.items.length > 0) {
      const tx = res.data.items[0]
      // Lấy chi tiết đầy đủ
      const detailRes = await api.get(`/Transactions/${tx.transactionId}`)
      if (detailRes.status === 200) {
        transaction.value = detailRes.data
        // Set overdue fine amount
        returnForm.overdueFineAmount = detailRes.data.estimatedFine ?? 0
        returnForm.createOverdueFine = detailRes.data.overdueDays > 0
      }
    } else {
      // Thử tìm cả Overdue
      const res2 = await api.get(`/Transactions?search=${barcode}&status=Overdue&pageSize=1`)
      if (res2.status === 200 && res2.data.items.length > 0) {
        const tx = res2.data.items[0]
        const detailRes = await api.get(`/Transactions/${tx.transactionId}`)
        if (detailRes.status === 200) {
          transaction.value = detailRes.data
          returnForm.overdueFineAmount = detailRes.data.estimatedFine ?? 0
          returnForm.createOverdueFine = true
        }
      } else {
        searchError.value = `Không tìm thấy giao dịch đang mượn với barcode "${barcode}"`
      }
    }
  } catch {
    searchError.value = "Lỗi khi tìm giao dịch"
  } finally {
    isSearching.value = false
  }
}

const resetOverdueFine = () => {
  returnForm.overdueFineAmount = transaction.value?.estimatedFine ?? 0
}

const isExtending = ref(false)

const isOverdue = (tx) => tx.status === 'Overdue' || tx.status === 2

const extendInstead = async () => {
  if (!transaction.value) return
  isExtending.value = true
  try {
    const res = await api.post(`/Transactions/${transaction.value.transactionId}/extend`)
    if (res.status === 200) {
      alert(`Gia hạn thành công! Hạn mới: ${new Date(res.data.newDueDate).toLocaleDateString('vi-VN')}`)
      // Reset form
      transaction.value = null
      barcodeInput.value = ''
      await nextTick()
      barcodeRef.value?.focus()
    }
  } catch (err) {
    alert(err.response?.data?.message || 'Gia hạn thất bại')
  } finally {
    isExtending.value = false
  }
}

const submitReturn = async () => {
  isSubmitting.value = true
  try {
    const payload = {
      returnCondition: returnForm.returnCondition,
      notes: returnForm.notes || null,
      createOverdueFine: returnForm.createOverdueFine,
      overdueFineAmount: returnForm.createOverdueFine ? returnForm.overdueFineAmount : null,
      damageFineAmount:
        returnForm.returnCondition === "Hư hỏng" ? returnForm.damageFineAmount : null,
      damageNote: returnForm.returnCondition === "Hư hỏng" ? returnForm.damageNote : null,
      lostFineAmount: returnForm.returnCondition === "Mất" ? returnForm.lostFineAmount : null,
      lostNote: returnForm.returnCondition === "Mất" ? returnForm.lostNote : null,
    }
    const res = await api.post(`/Transactions/${transaction.value.transactionId}/return`, payload)
    if (res.status === 200) {
      successData.value = {
        user: transaction.value.user?.fullName,
        bookTitle: transaction.value.copy?.bookTitle,
        totalFine: res.data.totalFineAmount,
      }
      showSuccess.value = true
    }
  } catch (err) {
    alert(err.response?.data?.message || "Trả sách thất bại")
  } finally {
    isSubmitting.value = false
  }
}

const resetForm = () => {
  showSuccess.value = false
  transaction.value = null
  barcodeInput.value = ""
  searchError.value = ""
  Object.assign(returnForm, {
    returnCondition: "Tốt",
    createOverdueFine: true,
    overdueFineAmount: 0,
    damageFineAmount: null,
    damageNote: "",
    lostFineAmount: null,
    lostNote: "",
    notes: "",
  })
  nextTick(() => barcodeRef.value?.focus())
}

// Computed
const estimatedOverdueFine = computed(() => returnForm.overdueFineAmount)

const totalFine = computed(() => {
  let total = 0
  if (returnForm.createOverdueFine && transaction.value?.overdueDays > 0)
    total += returnForm.overdueFineAmount || 0
  if (returnForm.returnCondition === "Hư hỏng" && returnForm.damageFineAmount)
    total += returnForm.damageFineAmount
  if (returnForm.returnCondition === "Mất" && returnForm.lostFineAmount)
    total += returnForm.lostFineAmount
  return total
})

const damageCalcMode = ref("percent")
const damagePercent = ref(null)

// Tính tiền phạt hư hỏng theo %
const calcDamageFine = () => {
  if (damagePercent.value && transaction.value?.bookPrice) {
    returnForm.damageFineAmount = Math.round(
      (damagePercent.value / 100) * transaction.value.bookPrice
    )
  }
}

// Khi tìm thấy transaction, tự set tiền phạt mất sách
watch(transaction, (tx) => {
  if (tx?.bookPrice) {
    returnForm.lostFineAmount = tx.bookPrice // 100%
  }
})

const backToList = () => {
  router.push({
    name: "transactionManagement",
  })
}
// Helpers
const initials = (name) =>
  !name
    ? "?"
    : name
      .split(" ")
      .map((w) => w[0])
      .slice(-2)
      .join("")
      .toUpperCase()
const formatDate = (d) => (d ? new Date(d).toLocaleDateString("vi-VN") : "—")
const formatMoney = (n) =>
  n ? new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(n) : "0đ"
</script>

<style lang="scss" scoped>
.return-book {
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

.form-layout {
  display: grid;
  grid-template-columns: 1fr 300px;
  gap: 20px;
  align-items: start;

  @media (max-width: 900px) {
    grid-template-columns: 1fr;
  }
}

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
  color: #999;
  background: #f5f5f5;
  padding: 2px 8px;
  border-radius: 99px;
}

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
  padding: 8px 12px;
  background: #435ebe;
  color: #fff;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 16px;

  &:hover:not(:disabled) {
    background: #2c3a8c;
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
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

// Transaction info
.tx-info {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.tx-user {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 14px;
  background: #f5f6ff;
  border-radius: 10px;
}

.tx-avatar {
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

.tx-user-name {
  font-size: 14px;
  font-weight: 700;
}

.tx-user-code {
  font-size: 12px;
  color: #666;
  font-family: monospace;
  margin-top: 2px;
}

.tx-book {
  padding: 12px 14px;
  background: #f9f9f9;
  border-radius: 10px;
}

.tx-book-title {
  font-size: 14px;
  font-weight: 700;
  margin-bottom: 4px;
}

.tx-book-meta {
  font-size: 12px;
  color: #888;
}

.code-text {
  font-family: monospace;
  color: #435ebe;
  font-weight: 600;
}

.tx-dates {
  display: flex;
  gap: 16px;
  flex-wrap: wrap;
  padding: 12px 14px;
  background: #f9f9f9;
  border-radius: 10px;
}

.date-item {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.date-label {
  font-size: 11px;
  color: #999;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.date-value {
  font-size: 14px;
  font-weight: 600;
}

.text-red {
  color: #c62828;
}

.text-green {
  color: #2e7d32;
}

.overdue-box {
  padding: 12px 16px;
  background: #ffebee;
  border-left: 3px solid #e53935;
  border-radius: 0 10px 10px 0;
}

.overdue-title {
  font-size: 14px;
  font-weight: 700;
  color: #c62828;
  margin-bottom: 4px;
}

.overdue-fine {
  font-size: 13px;
  color: #e53935;
}

.fine-calc {
  font-size: 12px;
  color: #999;
  margin-left: 6px;
}

.calc-mode {
  display: flex;
  gap: 16px;
  font-size: 14px;

  label {
    display: flex;
    align-items: center;
    gap: 6px;
    cursor: pointer;
  }
}

.percent-row {
  display: flex;
  align-items: center;
  gap: 8px;

  input {
    width: 84px;
  }
}

.calc-result {
  font-size: 14px;
  font-weight: 700;
  color: #3949ab;
}

.book-price-hint {
  font-size: 12px;
  color: #888;
}

.lost-fine-auto {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 14px;
  background: #ffebee;
  border-radius: 8px;
}

.lost-fine-label {
  font-size: 13px;
  color: #c62828;
  flex: 1;
}

.lost-fine-amount {
  font-size: 16px;
  font-weight: 800;
  color: #c62828;
}

.btn-link {
  background: none;
  border: none;
  color: #3949ab;
  font-size: 12px;
  cursor: pointer;
  text-decoration: underline;
  padding: 0;
  white-space: nowrap;
}

// Condition grid
.condition-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 10px;
  margin-bottom: 14px;

  @media (max-width: 600px) {
    grid-template-columns: repeat(2, 1fr);
  }
}

.condition-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  padding: 14px 8px;
  border-radius: 10px;
  border: 2px solid #e0e0e0;
  cursor: pointer;
  transition: all 0.15s;
  background: #fff;

  &:hover {
    border-color: #aaa;
  }

  &.active {
    border-width: 2px;

    &.cond-green {
      border-color: #43a047;
      background: #e8f5e9;
    }

    &.cond-blue {
      border-color: #1e88e5;
      background: #e3f2fd;
    }

    &.cond-orange {
      border-color: #fb8c00;
      background: #fff3e0;
    }

    &.cond-red {
      border-color: #e53935;
      background: #ffebee;
    }
  }
}

.condition-icon {
  font-size: 22px;
  color: #333333;

  &.cond-green {
    color: #2e7d32;
  }

  &.cond-blue {
    color: #1e88e5;
  }

  &.cond-orange {
    color: #fb8c00;
  }

  &.cond-red {
    color: #e53935;
  }
}

.condition-label {
  font-size: 13px;
  font-weight: 600;
  color: #333;
}

.fine-detail {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 12px 14px;
  background: #fafafa;
  border-radius: 8px;

  label {
    font-size: 13px;
    font-weight: 600;
    color: #444;
  }

  input {
    padding: 8px 12px;
    border: 1.5px solid #e0e0e0;
    border-radius: 8px;
    font-size: 14px;
    outline: none;
    font-family: inherit;

    &:focus {
      border-color: #435ebe;
    }
  }
}

.overdue-fine-config {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.toggle-label {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;

  input {
    cursor: pointer;
  }
}

.fine-amount-row {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
}

.fine-input {
  width: 140px;
  padding: 7px 10px;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  font-size: 14px;
  outline: none;

  &:focus {
    border-color: #435ebe;
  }
}

.btn-reset-fine {
  padding: 7px 12px;
  background: #f0f0f0;
  border: none;
  border-radius: 8px;
  font-size: 13px;
  cursor: pointer;
  color: #555;

  &:hover {
    background: #e0e0e0;
  }
}

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
  gap: 10px;
  margin-bottom: 16px;
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
}

.fines-summary {
  border-top: 1px solid #f0f0f0;
  padding-top: 14px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.fines-summary-title {
  font-size: 12px;
  font-weight: 700;
  color: #666;
  text-transform: uppercase;
  margin-bottom: 4px;
}

.fine-row {
  display: flex;
  justify-content: space-between;
  font-size: 13px;
  color: #555;

  strong {
    color: #c62828;
  }
}

.fine-total {
  display: flex;
  justify-content: space-between;
  padding-top: 8px;
  border-top: 1px solid #f0f0f0;
  font-size: 14px;
  font-weight: 700;

  strong {
    color: #c62828;
    font-size: 16px;
  }
}

.fine-zero {
  font-size: 13px;
  color: #aaa;
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
  max-width: 420px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.2);
  display: block;
  height: unset;
  top: unset;
  left: unset;
}

.success-modal {
  padding: 36px 28px;
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
  font-size: 14px;

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