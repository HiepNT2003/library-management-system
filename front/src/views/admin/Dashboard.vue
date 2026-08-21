<template>
  <div class="dashboard">
    <div class="page-header">
      <div>
        <h1 class="page-title">Dashboard</h1>
        <p class="page-desc">Tổng quan hệ thống thư viện</p>
      </div>
      <div class="header-right">
        <span class="last-updated">Cập nhật: {{ formatDateTime(new Date()) }}</span>
        <button class="btn-refresh" @click="fetchData" :disabled="isLoading">
          <Icon icon="mingcute:refresh-3-line" width="18" height="18" />
        </button>
      </div>
    </div>

    <div v-if="isLoading" class="state-box">Đang tải...</div>
    <div v-else-if="loadError" class="state-box state-error">{{ loadError }}</div>

    <template v-else-if="data">
      <!-- Overview stats -->
      <div class="overview-grid">
        <div class="stat-card" @click="redirectList('booksManage')">
          <div class="stat-icon">
            <Icon icon="ph:books-duotone" width="28" height="28" />
          </div>
          <div class="stat-body">
            <div class="stat-num">{{ data.overview.totalBooks }}</div>
            <div class="stat-label">Đầu sách</div>
            <div class="stat-sub">
              {{ data.overview.totalCopies }} bản sao · {{ data.overview.availableCopies }} khả dụng
            </div>
          </div>
        </div>
        <div class="stat-card" @click="redirectList('userManagement')">
          <div class="stat-icon"><i class="bi bi-people"></i></div>
          <div class="stat-body">
            <div class="stat-num">{{ data.overview.totalUsers }}</div>
            <div class="stat-label">Bạn đọc</div>
            <div class="stat-sub">{{ data.overview.activeUsers }} đang hoạt động</div>
          </div>
        </div>
        <div class="stat-card stat-blue" @click="redirectList('transactionManagement')">
          <div class="stat-icon">📖</div>
          <div class="stat-body">
            <div class="stat-num text-blue">{{ data.overview.borrowing }}</div>
            <div class="stat-label">Đang mượn</div>
            <div class="stat-sub">{{ data.overview.pendingRequests }} yêu cầu chờ duyệt</div>
          </div>
        </div>
        <div class="stat-card" :class="data.overview.overdue > 0 ? 'stat-red' : ''"
          @click="redirectList('transactionManagement', { status: 'Overdue' })">
          <div class="stat-icon warning"><i class="bi bi-exclamation-triangle-fill"></i></div>
          <div class="stat-body">
            <div class="stat-num" :class="data.overview.overdue > 0 ? 'text-red' : ''">
              {{ data.overview.overdue }}
            </div>
            <div class="stat-label">Quá hạn</div>
            <div class="stat-sub">Cần xử lý ngay</div>
          </div>
        </div>
        <div class="stat-card" :class="data.overview.pendingFines > 0 ? 'stat-orange' : ''"
          @click="redirectList('fineManagement')">
          <div class="stat-icon">💰</div>
          <div class="stat-body">
            <div class="stat-num" :class="data.overview.pendingFines > 0 ? 'text-orange' : ''">
              {{ data.overview.pendingFines }}
            </div>
            <div class="stat-label">Phạt chưa thu</div>
            <div class="stat-sub">{{ formatMoney(data.overview.pendingFineAmount) }}</div>
          </div>
        </div>
      </div>

      <!-- Charts row -->
      <div class="charts-row">
        <!-- Daily chart -->
        <div class="chart-card">
          <div class="chart-header">
            <h3 class="chart-title">Mượn / Trả 30 ngày gần nhất</h3>
          </div>
          <apexchart type="area" height="260" :options="dailyChartOptions" :series="dailySeries" />
        </div>

        <!-- Biểu đồ tròn trạng thái -->
        <div class="chart-card chart-sm">
          <div class="chart-header">
            <h3 class="chart-title">Tình trạng giao dịch</h3>
          </div>
          <apexchart type="donut" height="260" :options="donutChartOptions" :series="donutSeries" />
        </div>
      </div>

      <!-- Charts row 2: Monthly + Top books -->
      <div class="charts-row">
        <!-- Biểu đồ lượt mượn theo tháng -->
        <div class="chart-card chart-md">
          <div class="chart-header">
            <h3 class="chart-title">Lượt mượn theo tháng ({{ new Date().getFullYear() }})</h3>
          </div>
          <apexchart type="bar" height="240" :options="monthlyChartOptions" :series="monthlySeries" />
        </div>

        <!-- Top sách mượn nhiều -->
        <div class="chart-card chart-md">
          <div class="chart-header">
            <h3 class="chart-title">Top 10 sách mượn nhiều nhất</h3>
          </div>
          <apexchart type="bar" height="240" :options="topBooksChartOptions" :series="topBooksSeries" />
        </div>
      </div>

      <!-- Alerts row -->
      <div class="alerts-row">
        <!-- Sách quá hạn -->
        <div class="alert-card">
          <div class="alert-header">
            <h3 class="alert-title">
              <i class="bi bi-exclamation-triangle-fill icon_warning"></i> Sách quá hạn
              <span class="alert-count red" v-if="data.alerts.overdueList.length > 0">
                {{ data.alerts.overdueList.length }}
              </span>
            </h3>
            <button class="btn-view-all" @click="$router.push('/admin/transactions?status=Overdue')">
              Xem tất cả
            </button>
          </div>
          <div v-if="data.alerts.overdueList.length === 0" class="alert-empty">
            <Icon class="icon_tick" icon="charm:circle-tick" width="16" height="16" /> Không có sách
            quá hạn
          </div>
          <div v-for="tx in data.alerts.overdueList.slice(0, 5)" :key="tx.transactionId" class="alert-item">
            <div class="alert-info">
              <div class="alert-name">{{ tx.userName }}</div>
              <div class="alert-sub">{{ tx.bookTitle }}</div>
            </div>
            <div class="alert-days red">+{{ tx.overdueDays }} ngày</div>
          </div>
        </div>

        <!-- Phiếu phạt chưa thu -->
        <div class="alert-card">
          <div class="alert-header">
            <h3 class="alert-title">
              💰 Phiếu phạt chưa thu
              <span class="alert-count orange" v-if="data.alerts.pendingFineList.length > 0">
                {{ data.alerts.pendingFineList.length }}
              </span>
            </h3>
            <button class="btn-view-all" @click="$router.push('/admin/fine-management')">Xem tất cả</button>
          </div>
          <div v-if="data.alerts.pendingFineList.length === 0" class="alert-empty">
            <Icon class="icon_tick" icon="charm:circle-tick" width="16" height="16" /> Không có
            phiếu phạt chưa thu
          </div>
          <div v-for="fine in data.alerts.pendingFineList.slice(0, 5)" :key="fine.fineId" class="alert-item">
            <div class="alert-info">
              <div class="alert-name">{{ fine.userName }}</div>
              <div class="alert-sub">{{ fine.bookTitle }}</div>
            </div>
            <div class="alert-days orange">{{ formatMoney(fine.amount) }}</div>
          </div>
        </div>

        <!-- Yêu cầu mượn chờ duyệt -->
        <div class="alert-card">
          <div class="alert-header">
            <h3 class="alert-title">
              📋 Yêu cầu chờ duyệt
              <span class="alert-count blue" v-if="data.alerts.pendingRequestList?.length > 0">
                {{ data.alerts.pendingRequestList.length }}
              </span>
            </h3>
            <button class="btn-view-all" @click="$router.push('/admin/borrow-requests')">
              Xem tất cả
            </button>
          </div>
          <div v-if="!data.alerts.pendingRequestList?.length" class="alert-empty">
            <Icon class="icon_tick" icon="charm:circle-tick" width="16" height="16" /> Không có yêu
            cầu chờ duyệt
          </div>
          <div v-for="req in data.alerts.pendingRequestList?.slice(0, 5)" :key="req.requestId" class="alert-item">
            <div class="alert-info">
              <div class="alert-name">{{ req.userName }}</div>
              <div class="alert-sub">{{ req.bookTitle }}</div>
            </div>
            <div class="alert-days blue">{{ formatDate(req.expectedBorrowDate) }}</div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from "vue"
import { useRouter } from "vue-router"
import api from "../../services/api"
import { Icon } from "@iconify/vue"

const router = useRouter()
const data = ref(null)
const isLoading = ref(false)
const loadError = ref("")

onMounted(() => fetchData())

const fetchData = async () => {
  isLoading.value = true
  loadError.value = ""
  try {
    const res = await api.get("/Dashboard")
    if (res.status === 200) data.value = res.data
  } catch (err) {
    loadError.value = "Không thể tải dữ liệu dashboard"
  } finally {
    isLoading.value = false
  }
}

// Redirect
const redirectList = (name, status) => {
  router.push({
    name: name,
    query: status ? status : null,
  })
}

// ─── ApexCharts: Daily area chart ─────────────────────────────────────────
const dailySeries = computed(() => [
  { name: "Mượn", data: data.value?.charts.daily.map((d) => d.borrow) ?? [] },
  { name: "Trả", data: data.value?.charts.daily.map((d) => d.return) ?? [] },
])

const dailyChartOptions = computed(() => ({
  chart: { toolbar: { show: false }, zoom: { enabled: false }, fontFamily: "Segoe UI, sans-serif" },
  colors: ["#3949AB", "#27AE60"],
  stroke: { curve: "smooth", width: 2.5 },
  fill: { type: "gradient", gradient: { opacityFrom: 0.4, opacityTo: 0.05 } },
  dataLabels: { enabled: false },
  xaxis: {
    categories: data.value?.charts.daily.map((d) => d.date) ?? [],
    labels: { rotate: -45, style: { fontSize: "10px" } },
    tickAmount: 7,
  },
  yaxis: { labels: { style: { fontSize: "11px" } } },
  tooltip: { x: { format: "dd/MM" } },
  legend: { position: "top", horizontalAlign: "right", fontSize: "12px" },
  grid: { borderColor: "#f0f0f0", strokeDashArray: 4 },
}))

// ─── ApexCharts: Donut chart ───────────────────────────────────────────────
const donutSeries = computed(() => [
  data.value?.overview.borrowing ?? 0,
  data.value?.overview.overdue ?? 0,
  (data.value?.overview.totalCopies ?? 0) -
  (data.value?.overview.borrowing ?? 0) -
  (data.value?.overview.overdue ?? 0),
])

const donutChartOptions = computed(() => ({
  chart: { fontFamily: "Segoe UI, sans-serif" },
  labels: ["Đang mượn", "Quá hạn", "Khả dụng"],
  colors: ["#3949AB", "#E74C3C", "#27AE60"],
  legend: { position: "bottom", fontSize: "12px" },
  plotOptions: {
    pie: {
      donut: {
        size: "65%",
        labels: { show: true, total: { show: true, label: "Tổng bản sao", fontSize: "12px" } },
      },
    },
  },
  dataLabels: { enabled: true, style: { fontSize: "11px" } },
  tooltip: { y: { formatter: (v) => `${v} bản sao` } },
}))

// ─── ApexCharts: Monthly bar chart ────────────────────────────────────────
const monthlySeries = computed(() => [
  {
    name: "Lượt mượn",
    data: data.value?.charts.monthly.map((m) => m.count) ?? [],
  },
])

const monthlyChartOptions = computed(() => ({
  chart: { toolbar: { show: false }, fontFamily: "Segoe UI, sans-serif" },
  colors: ["#3949AB"],
  plotOptions: { bar: { borderRadius: 4, columnWidth: "55%" } },
  dataLabels: { enabled: false },
  xaxis: {
    categories: data.value?.charts.monthly.map((m) => `T${m.label}`) ?? [],
    labels: { style: { fontSize: "11px" } },
  },
  yaxis: { labels: { style: { fontSize: "11px" } } },
  grid: { borderColor: "#f0f0f0", strokeDashArray: 4 },
  tooltip: { y: { formatter: (v) => `${v} lượt` } },
}))

// ─── ApexCharts: Top books horizontal bar ─────────────────────────────────
const topBooksSeries = computed(() => [
  {
    name: "Lượt mượn",
    data: [...(data.value?.topBooks ?? [])].reverse().map((b) => b.borrowCount),
  },
])

const topBooksChartOptions = computed(() => ({
  chart: { toolbar: { show: false }, fontFamily: "Segoe UI, sans-serif" },
  colors: ["#E67E22"],
  plotOptions: { bar: { borderRadius: 4, horizontal: true, barHeight: "55%" } },
  dataLabels: { enabled: true, style: { fontSize: "11px", colors: ["#fff"] } },
  xaxis: {
    labels: { style: { fontSize: "11px" } },
    categories: [...(data.value?.topBooks ?? [])]
      .reverse()
      .map((b) => (b.title?.length > 20 ? b.title.substring(0, 20) + "..." : b.title)),
  },
  yaxis: {
    labels: {
      style: { fontSize: "11px" },
      formatter: (v) => (v?.length > 20 ? v.substring(0, 20) + "..." : v),
    },
  },
  grid: { borderColor: "#f0f0f0", strokeDashArray: 4 },
  tooltip: { y: { formatter: (v) => `${v} lượt mượn` } },
}))

// ─── Helpers ──────────────────────────────────────────────────────────────
const formatDateTime = (d) => (d ? new Date(d).toLocaleString("vi-VN") : "")
const formatDate = (d) => (d ? new Date(d).toLocaleDateString("vi-VN") : "—")
const formatMoney = (n) =>
  n != null ? new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(n) : "—"
</script>

<style lang="scss" scoped>
.dashboard {
  display: flex;
  flex-direction: column;
  gap: 20px;
  font-family: "Segoe UI", sans-serif;
  color: #1a1a2e;
}

.page-header {
  display: flex;
  align-items: center;
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

.header-right {
  display: flex;
  align-items: center;
  gap: 10px;
}

.last-updated {
  font-size: 12px;
  color: #aaa;
}

.btn-refresh {
  padding: 8px 12px;
  background: #fff;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  cursor: pointer;
  font-size: 16px;
  color: #333333;

  &:hover:not(:disabled) {
    border-color: #3949ab;
  }

  &:disabled {
    opacity: 0.4;
  }
}

// Overview
.overview-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 12px;

  @media (max-width: 1100px) {
    grid-template-columns: repeat(3, 1fr);
  }

  @media (max-width: 700px) {
    grid-template-columns: repeat(2, 1fr);
  }
}

.stat-card {
  background: #fff;
  border-radius: 14px;
  border: 1.5px solid #e0e0e0;
  padding: 16px;
  display: flex;
  gap: 12px;
  align-items: center;
  cursor: pointer;
  transition: all 0.15s;

  &:hover {
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
    transform: translateY(-1px);
  }

  &.stat-blue {
    border-color: #bbdefb;
    background: #f3f9ff;
  }

  &.stat-red {
    border-color: #ffcdd2;
    background: #fff8f8;
  }

  &.stat-orange {
    border-color: #ffe0b2;
    background: #fffbf5;
  }
}

.stat-icon {
  font-size: 28px;
  flex-shrink: 0;

  &.warning {
    color: rgb(252, 202, 40);
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

  &.text-orange {
    color: #e65100;
  }
}

.stat-label {
  font-size: 13px;
  color: #555;
  margin-top: 3px;
  font-weight: 600;
}

.stat-sub {
  font-size: 12px;
  color: #888;
  margin-top: 2px;
}

// Charts
.charts-row {
  display: grid;
  gap: 16px;
  grid-template-columns: 1fr;

  &:has(.chart-lg) {
    grid-template-columns: 2fr 1fr;
  }

  &:has(.chart-md) {
    grid-template-columns: 1fr 1fr;
  }

  @media (max-width: 900px) {
    grid-template-columns: 1fr !important;
  }
}

.chart-card {
  background: #fff;
  border-radius: 14px;
  border: 1px solid #e0e0e0;
  padding: 20px;
  width: 100%;
  overflow: auto;
}

.chart-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}

.chart-title {
  font-size: 14px;
  font-weight: 700;
  margin: 0;
  color: #1a1a2e;
}

// Alerts
.alerts-row {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;

  @media (max-width: 900px) {
    grid-template-columns: 1fr;
  }
}

.alert-card {
  background: #fff;
  border-radius: 14px;
  border: 1px solid #e0e0e0;
  padding: 18px;
}

.alert-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.alert-title {
  font-size: 14px;
  font-weight: 700;
  margin: 0;
  display: flex;
  align-items: center;
  gap: 8px;

  .icon_warning {
    color: rgb(252, 202, 40);
  }
}

.alert-count {
  font-size: 11px;
  font-weight: 700;
  padding: 2px 7px;
  border-radius: 99px;

  &.red {
    background: #ffebee;
    color: #c62828;
  }

  &.orange {
    background: #fff3e0;
    color: #e65100;
  }

  &.blue {
    background: #e8eaf6;
    color: #3949ab;
  }
}

.btn-view-all {
  font-size: 12px;
  color: #3949ab;
  background: none;
  border: none;
  cursor: pointer;
  padding: 0;

  &:hover {
    text-decoration: underline;
  }
}

.alert-empty {
  font-size: 13px;
  color: #aaa;
  padding: 12px 0;
  text-align: center;
}

.alert-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 0;
  border-bottom: 1px solid #f5f5f5;

  &:last-child {
    border-bottom: none;
  }
}

.alert-name {
  font-size: 13px;
  font-weight: 600;
  color: #1a1a2e;
}

.alert-sub {
  font-size: 11px;
  color: #888;
  margin-top: 2px;
}

.alert-days {
  font-size: 13px;
  font-weight: 700;
  white-space: nowrap;

  &.red {
    color: #c62828;
  }

  &.orange {
    color: #e65100;
  }

  &.blue {
    color: #3949ab;
  }
}

.state-box {
  padding: 40px;
  text-align: center;
  color: #888;

  &.state-error {
    color: #c62828;
  }
}

.icon_tick {
  color: #43a047;
  margin-bottom: 2px;
  margin-right: 2px;
}
</style>