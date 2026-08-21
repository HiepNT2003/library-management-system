<template>
  <div class="reports-page">
    <div class="page-header">
      <div>
        <h1 class="page-title">Báo cáo</h1>
        <p class="page-desc">Thống kê và xuất báo cáo</p>
      </div>
    </div>

    <!-- Date filter -->
    <div class="filter-card">
      <div class="filter-row">
        <div class="filter-group">
          <label>Từ ngày</label>
          <input type="date" v-model="filters.from" :max="filters.to" />
        </div>
        <div class="filter-group">
          <label>Đến ngày</label>
          <input type="date" v-model="filters.to" :min="filters.from" :max="today" />
        </div>
        <div class="filter-quick">
          <button
            v-for="q in quickRanges"
            :key="q.label"
            class="quick-btn"
            :class="{ active: activeQuick === q.label }"
            @click="setQuickRange(q)"
          >
            {{ q.label }}
          </button>
        </div>
        <button class="btn btn-primary" @click="fetchSummary" :disabled="isLoading">
            <Icon v-if="!isLoading" icon="mdi:report-bar" width="24" height="24" />
          {{ isLoading ? "Đang tải..." : "Xem báo cáo" }}
        </button>
      </div>
    </div>

    <template v-if="summary">
      <!-- Overview stats -->
      <div class="stats-grid">
        <div class="stat-card stat-blue">
          <div class="stat-icon">📖</div>
          <div class="stat-body">
            <div class="stat-num">{{ totalBorrow }}</div>
            <div class="stat-label">Lượt mượn</div>
          </div>
        </div>
        <div class="stat-card stat-green">
          <div class="stat-icon">
            <Icon class="icon_tick" icon="charm:circle-tick" width="28" height="28" />
          </div>
          <div class="stat-body">
            <div class="stat-num">{{ totalReturn }}</div>
            <div class="stat-label">Lượt trả</div>
          </div>
        </div>
        <div class="stat-card stat-red">
          <div class="stat-icon"><i class="bi bi-exclamation-triangle-fill"></i></div>
          <div class="stat-body">
            <div class="stat-num">{{ totalOverdue }}</div>
            <div class="stat-label">Quá hạn</div>
          </div>
        </div>
        <div class="stat-card stat-orange">
          <div class="stat-icon">💰</div>
          <div class="stat-body">
            <div class="stat-num">{{ formatMoney(totalFineAmount) }}</div>
            <div class="stat-label">Tổng tiền phạt</div>
          </div>
        </div>
        <div class="stat-card">
          <div class="stat-icon">📋</div>
          <div class="stat-body">
            <div class="stat-num">{{ totalRequest }}</div>
            <div class="stat-label">Yêu cầu mượn</div>
          </div>
        </div>
      </div>

      <!-- Charts + Top books -->
      <div class="content-row">
        <!-- Monthly chart -->
        <div class="chart-card">
          <h3 class="card-title">Lượt mượn theo tháng</h3>
          <div class="bar-chart" v-if="summary.monthlyBorrow.length > 0">
            <div
              v-for="m in summary.monthlyBorrow"
              :key="`${m.year}-${m.month}`"
              class="bar-col"
              :title="`${m.month}/${m.year}: ${m.count} lượt`"
            >
              <div class="bar-wrap">
                <div class="bar" :style="{ height: barHeight(m.count) + 'px' }"></div>
              </div>
              <div class="bar-label">{{ m.month }}/{{ String(m.year).slice(-2) }}</div>
              <div class="bar-value">{{ m.count }}</div>
            </div>
          </div>
          <div v-else class="chart-empty">Không có dữ liệu trong kỳ này</div>
        </div>

        <!-- Top books -->
        <div class="top-card">
          <h3 class="card-title">Top sách mượn nhiều nhất</h3>
          <div class="top-list">
            <div v-if="summary.topBooks.length === 0" class="chart-empty">Không có dữ liệu</div>
            <div v-for="(book, idx) in summary.topBooks" :key="book.bookId" class="top-item">
              <div class="top-rank" :class="rankClass(idx)">{{ idx + 1 }}</div>
              <div class="top-info">
                <div class="top-title">{{ truncate(book.title, 40) }}</div>
                <div class="top-bar-wrap">
                  <div
                    class="top-bar"
                    :style="{
                      width: (book.borrowCount / summary.topBooks[0].borrowCount) * 100 + '%',
                    }"
                  ></div>
                </div>
              </div>
              <div class="top-count">{{ book.borrowCount }} lượt</div>
            </div>
          </div>
        </div>
      </div>

      <!-- Fine breakdown -->
      <div class="fine-card">
        <h3 class="card-title">Thống kê phiếu phạt</h3>
        <div class="fine-stats">
          <div v-for="f in summary.fines" :key="f.status" class="fine-stat">
            <div class="fine-stat-label">{{ fineStatusLabel(f.status) }}</div>
            <div class="fine-stat-count">{{ f.count }} phiếu</div>
            <div class="fine-stat-amount" :class="fineAmountClass(f.status)">
              {{ formatMoney(f.total) }}
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- Export section -->
    <div class="export-card">
      <h3 class="card-title">Xuất báo cáo Excel</h3>
      <div class="export-grid">
        <div class="export-item">
          <div class="export-icon">📋</div>
          <div class="export-info">
            <div class="export-title">Danh sách giao dịch</div>
            <div class="export-desc">Mượn trả trong kỳ đã chọn</div>
            <div class="export-filter">
              <select v-model="exportFilters.transactionStatus">
                <option value="">Tất cả trạng thái</option>
                <option value="Borrowed">Đang mượn</option>
                <option value="Returned">Đã trả</option>
                <option value="Overdue">Quá hạn</option>
              </select>
            </div>
          </div>
          <button
            class="btn-export"
            @click="exportTransactions"
            :disabled="isExporting.transactions"
          >
            <Icon
              v-if="!isExporting.transactions"
              class="icon_excel"
              icon="file-icons:microsoft-excel"
              width="14"
              height="14"
            />
            {{ isExporting.transactions ? "..." : " Xuất Excel" }}
          </button>
        </div>

        <div class="export-item">
          <div class="export-icon">💰</div>
          <div class="export-info">
            <div class="export-title">Danh sách phiếu phạt</div>
            <div class="export-desc">Phiếu phạt trong kỳ đã chọn</div>
            <div class="export-filter">
              <select v-model="exportFilters.fineStatus">
                <option value="">Tất cả trạng thái</option>
                <option value="Pending">Chưa thu</option>
                <option value="Paid">Đã thu</option>
                <option value="Waived">Đã miễn</option>
              </select>
            </div>
          </div>
          <button class="btn-export" @click="exportFines" :disabled="isExporting.fines">
            <Icon
              v-if="!isExporting.fines"
              class="icon_excel"
              icon="file-icons:microsoft-excel"
              width="14"
              height="14"
            />
            {{ isExporting.fines ? "..." : "Xuất Excel" }}
          </button>
        </div>

        <div class="export-item">
          <div class="export-icon">📚</div>
          <div class="export-info">
            <div class="export-title">Danh sách sách</div>
            <div class="export-desc">Tình trạng tất cả bản sao</div>
          </div>
          <button class="btn-export" @click="exportBooks" :disabled="isExporting.books">
            <Icon
              v-if="!isExporting.books"
              class="icon_excel"
              icon="file-icons:microsoft-excel"
              width="14"
              height="14"
            />
            {{ isExporting.books ? "..." : "Xuất Excel" }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from "vue"
import api from "../../services/api"
import { Icon } from "@iconify/vue"

const today = new Date().toISOString().slice(0, 10)
const isLoading = ref(false)
const summary = ref(null)
const activeQuick = ref("Tháng này")

const filters = reactive({
  from: new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().slice(0, 10),
  to: today,
})

const exportFilters = reactive({
  transactionStatus: "",
  fineStatus: "",
})

const isExporting = reactive({ transactions: false, fines: false, books: false })

const quickRanges = [
  {
    label: "Tháng này",
    from: () =>
      new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().slice(0, 10),
    to: () => today,
  },
  {
    label: "Tháng trước",
    from: () =>
      new Date(new Date().getFullYear(), new Date().getMonth() - 1, 1).toISOString().slice(0, 10),
    to: () =>
      new Date(new Date().getFullYear(), new Date().getMonth(), 0).toISOString().slice(0, 10),
  },
  {
    label: "Quý này",
    from: () => {
      const q = Math.floor(new Date().getMonth() / 3)
      return new Date(new Date().getFullYear(), q * 3, 1).toISOString().slice(0, 10)
    },
    to: () => today,
  },
  {
    label: "Năm nay",
    from: () => new Date(new Date().getFullYear(), 0, 1).toISOString().slice(0, 10),
    to: () => today,
  },
]

onMounted(() => fetchSummary())

const setQuickRange = (range) => {
  activeQuick.value = range.label
  filters.from = range.from()
  filters.to = range.to()
  fetchSummary()
}

const fetchSummary = async () => {
  isLoading.value = true
  try {
    const res = await api.get(`/Reports/summary?from=${filters.from}&to=${filters.to}`)
    if (res.status === 200) summary.value = res.data
  } catch {
  } finally {
    isLoading.value = false
  }
}

// Export
const downloadFile = (data, fileName) => {
  const url = window.URL.createObjectURL(new Blob([data]))
  const link = document.createElement("a")
  link.href = url
  link.setAttribute("download", fileName)
  document.body.appendChild(link)
  link.click()
  link.remove()
  window.URL.revokeObjectURL(url)
}

const exportTransactions = async () => {
  isExporting.transactions = true
  try {
    const params = new URLSearchParams({ from: filters.from, to: filters.to })
    if (exportFilters.transactionStatus) params.append("status", exportFilters.transactionStatus)

    const res = await api.get(`/Reports/transactions/export?${params}`, { responseType: "blob" })
    downloadFile(res.data, `GiaoDich_${filters.from}_${filters.to}.xlsx`)
  } catch {
    alert("Xuất thất bại")
  } finally {
    isExporting.transactions = false
  }
}

const exportFines = async () => {
  isExporting.fines = true
  try {
    const params = new URLSearchParams({ from: filters.from, to: filters.to })
    if (exportFilters.fineStatus) params.append("status", exportFilters.fineStatus)

    const res = await api.get(`/Reports/fines/export?${params}`, { responseType: "blob" })
    downloadFile(res.data, `PhieuPhat_${filters.from}_${filters.to}.xlsx`)
  } catch {
    alert("Xuất thất bại")
  } finally {
    isExporting.fines = false
  }
}

const exportBooks = async () => {
  isExporting.books = true
  try {
    const res = await api.get("/Reports/books/export", { responseType: "blob" })
    downloadFile(res.data, `DanhSachSach_${today}.xlsx`)
  } catch {
    alert("Xuất thất bại")
  } finally {
    isExporting.books = false
  }
}

// Computed
const totalBorrow = computed(
  () => summary.value?.transactions.find((t) => t.status === "Borrowed")?.count ?? 0
)
const totalReturn = computed(
  () => summary.value?.transactions.find((t) => t.status === "Returned")?.count ?? 0
)
const totalOverdue = computed(
  () => summary.value?.transactions.find((t) => t.status === "Overdue")?.count ?? 0
)
const totalFineAmount = computed(
  () => summary.value?.fines.reduce((sum, f) => sum + f.total, 0) ?? 0
)
const totalRequest = computed(
  () => summary.value?.requests.reduce((sum, r) => sum + r.count, 0) ?? 0
)

const maxMonthly = computed(() =>
  Math.max(...(summary.value?.monthlyBorrow.map((m) => m.count) ?? [1]))
)
const BAR_MAX = 120
const barHeight = (val) =>
  maxMonthly.value > 0 ? Math.max(4, Math.round((val / maxMonthly.value) * BAR_MAX)) : 4

const rankClass = (idx) => {
  if (idx === 0) return "rank-gold"
  if (idx === 1) return "rank-silver"
  if (idx === 2) return "rank-bronze"
  return "rank-default"
}

const fineStatusLabel = (s) => ({ Pending: "Chưa thu", Paid: "Đã thu", Waived: "Đã miễn" }[s] ?? s)
const fineAmountClass = (s) =>
  ({ Pending: "text-red", Paid: "text-green", Waived: "text-gray" }[s] ?? "")
const formatMoney = (n) =>
  n != null ? new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(n) : "—"
const truncate = (str, len) => (!str ? "" : str.length > len ? str.slice(0, len) + "..." : str)
</script>

<style lang="scss" scoped>
.reports-page {
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

// Filter card
.filter-card {
  background: #fff;
  border-radius: 12px;
  border: 1px solid #e0e0e0;
  padding: 20px;
}

.filter-row {
  display: flex;
  gap: 16px;
  align-items: flex-end;
  flex-wrap: wrap;
}

.filter-group {
  display: flex;
  flex-direction: column;
  gap: 6px;

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

    &:focus {
      border-color: #435ebe;
    }
  }
}

.filter-quick {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
  align-items: flex-end;
}

.quick-btn {
  padding: 8px 14px;
  background: #fff;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s;
  color: #555;

  &:hover {
    border-color: #435ebe;
    color: #435ebe;
  }

  &.active {
    background: #e8eaf6;
    border-color: #435ebe;
    color: #435ebe;
    font-weight: 700;
  }
}

// Stats grid
.stats-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 12px;

  @media (max-width: 900px) {
    grid-template-columns: repeat(3, 1fr);
  }
}

.stat-card {
  background: #fff;
  border-radius: 12px;
  border: 1.5px solid #e0e0e0;
  padding: 16px;
  display: flex;
  gap: 12px;
  align-items: center;

  &.stat-blue {
    border-color: #bbdefb;
    background: #f3f9ff;
  }

  &.stat-green {
    border-color: #c8e6c9;
    background: #f1fff3;

    .icon_tick {
      color: #43a047;
      margin-bottom: 2px;
      margin-right: 2px;
    }
  }

  &.stat-red {
    border-color: #ffcdd2;
    background: #fff8f8;
    .stat-icon {
      color: rgb(252, 202, 40);
    }
  }

  &.stat-orange {
    border-color: #ffe0b2;
    background: #fffbf5;
  }
}

.stat-icon {
  font-size: 28px;
}

.stat-num {
  font-size: 22px;
  font-weight: 800;
  line-height: 1;
}

.stat-label {
  font-size: 12px;
  color: #888;
  margin-top: 3px;
}

// Content row
.content-row {
  display: grid;
  grid-template-columns: 1fr 380px;
  gap: 16px;

  @media (max-width: 1000px) {
    grid-template-columns: 1fr;
  }
}

.card-title {
  font-size: 15px;
  font-weight: 700;
  margin: 0 0 16px;
}

// Chart card
.chart-card {
  background: #fff;
  border-radius: 12px;
  border: 1px solid #e0e0e0;
  padding: 20px;
}

.bar-chart {
  display: flex;
  align-items: flex-end;
  gap: 8px;
  height: 160px;
}

.bar-col {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
}

.bar-wrap {
  flex: 1;
  display: flex;
  align-items: flex-end;
  width: 100%;
}

.bar {
  width: 100%;
  background: #435ebe;
  border-radius: 4px 4px 0 0;
  min-height: 4px;
  transition: height 0.3s;
}

.bar-label {
  font-size: 10px;
  color: #aaa;
}

.bar-value {
  font-size: 11px;
  font-weight: 600;
  color: #435ebe;
}

.chart-empty {
  color: #aaa;
  font-size: 13px;
  text-align: center;
  padding: 40px 0;
}

// Top card
.top-card {
  background: #fff;
  border-radius: 12px;
  border: 1px solid #e0e0e0;
  padding: 20px;
}

.top-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.top-item {
  display: flex;
  align-items: center;
  gap: 10px;
}

.top-rank {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  font-weight: 800;

  &.rank-gold {
    background: #ffd700;
    color: #7d5a00;
  }

  &.rank-silver {
    background: #c0c0c0;
    color: #555;
  }

  &.rank-bronze {
    background: #cd7f32;
    color: #fff;
  }

  &.rank-default {
    background: #f0f0f0;
    color: #888;
  }
}

.top-info {
  flex: 1;
  min-width: 0;
}

.top-title {
  font-size: 13px;
  font-weight: 600;
  margin-bottom: 4px;
}

.top-bar-wrap {
  height: 4px;
  background: #f0f0f0;
  border-radius: 2px;
  overflow: hidden;
}

.top-bar {
  height: 100%;
  background: #435ebe;
  border-radius: 2px;
}

.top-count {
  font-size: 12px;
  color: #435ebe;
  font-weight: 700;
  white-space: nowrap;
}

// Fine card
.fine-card {
  background: #fff;
  border-radius: 12px;
  border: 1px solid #e0e0e0;
  padding: 20px;
}

.fine-stats {
  display: flex;
  gap: 20px;
  flex-wrap: wrap;
}

.fine-stat {
  flex: 1;
  min-width: 160px;
  padding: 16px;
  background: #f9f9f9;
  border-radius: 10px;
  border: 1px solid #e0e0e0;
}

.fine-stat-label {
  font-size: 13px;
  color: #888;
  margin-bottom: 6px;
}

.fine-stat-count {
  font-size: 18px;
  font-weight: 700;
  margin-bottom: 4px;
}

.fine-stat-amount {
  font-size: 15px;
  font-weight: 600;
}

.text-red {
  color: #c62828;
}

.text-green {
  color: #2e7d32;
}

.text-gray {
  color: #9e9e9e;
}

// Export card
.export-card {
  background: #fff;
  border-radius: 12px;
  border: 1px solid #e0e0e0;
  padding: 20px;
}

.export-grid {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.export-item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 14px 16px;
  background: #f9f9f9;
  border-radius: 10px;
  border: 1px solid #e8e8e8;
}

.export-icon {
  font-size: 28px;
  flex-shrink: 0;
}

.export-info {
  flex: 1;
}

.export-title {
  font-size: 14px;
  font-weight: 700;
  margin-bottom: 2px;
}

.export-desc {
  font-size: 13px;
  color: #888;
  margin-bottom: 8px;
}

.export-filter {
  select {
    padding: 5px 10px;
    border: 1.5px solid #e0e0e0;
    border-radius: 8px;
    font-size: 13px;
    background: #fff;
    outline: none;
    cursor: pointer;
    color: #333333;

    &:focus {
      border-color: #435ebe;
    }
  }
}

.btn-export {
  padding: 9px 18px;
  background: #435ebe;
  color: #fff;
  border: none;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  white-space: nowrap;
  transition: background 0.15s;
  flex-shrink: 0;
  .icon_excel {
    margin-bottom: 3px;
  }

  &:hover:not(:disabled) {
    background: #2c3a8c;
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
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
}
</style>