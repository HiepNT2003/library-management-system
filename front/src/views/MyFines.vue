<template>
    <div class="my-fines">

        <div class="page-header">
            <div>
                <h1 class="page-title">Phiếu phạt</h1>
                <p class="page-desc">Danh sách phiếu phạt của bạn</p>
            </div>
        </div>

        <!-- Pending summary banner -->
        <div class="pending-banner" v-if="totalPending > 0">
            <div class="banner-left">
                <div class="banner-icon">💰</div>
                <div>
                    <div class="banner-title">Bạn có phiếu phạt chưa thanh toán</div>
                    <div class="banner-sub">Vui lòng đến thư viện để thanh toán</div>
                </div>
            </div>
            <div class="banner-amount">{{ formatMoney(totalPending) }}</div>
        </div>

        <!-- Stats -->
        <div class="stats-row">
            <div v-for="s in statsDisplay" :key="s.status" class="stat-card"
                :class="{ active: activeStatus === s.status }" @click="setStatus(s.status)">
                <div class="stat-num" :class="s.color">{{ s.count }}</div>
                <div class="stat-label">{{ s.label }}</div>
                <div class="stat-amount" v-if="s.amount > 0">{{ formatMoney(s.amount) }}</div>
            </div>
        </div>

        <!-- List -->
        <div v-if="isLoading" class="state-box">Đang tải...</div>

        <div v-else-if="items.length === 0" class="empty-state">
            <div class="empty-icon"><Icon class="icon_success" icon="charm:circle-tick" width="56" height="56" /></div>
            <div class="empty-title">Không có phiếu phạt nào</div>
            <div class="empty-sub">Bạn chưa có phiếu phạt nào trong hệ thống</div>
        </div>

        <div v-else class="fine-list">
            <div v-for="fine in items" :key="fine.fineId" class="fine-card" :class="fineCardClass(fine.status)">
                <!-- Cover -->
                <div class="card-cover" @click="$router.push(`/user/books/${fine.book?.bookId}`)">
                    <img v-if="fine.book?.imageUrl" :src="fine.book.imageUrl" :alt="fine.book?.title" />
                    <div v-else class="cover-placeholder">📖</div>
                </div>

                <!-- Info -->
                <div class="card-info">
                    <div class="card-top">
                        <div class="card-left">
                            <h3 class="card-title" @click="$router.push(`/user/books/${fine.book?.bookId}`)">{{
                                fine.book?.title || '—' }}</h3>
                            <div class="fine-reason">{{ fine.reason }}</div>
                        </div>
                        <div class="card-right">
                            <div class="fine-amount"
                                :class="fine.status === 0 || fine.status === 'Pending' ? 'amount-red' : 'amount-gray'">
                                {{ formatMoney(fine.amount) }}
                            </div>
                            <span class="fine-status" :class="fineStatusClass(fine.status)">
                                {{ fineStatusLabel(fine.status) }}
                            </span>
                        </div>
                    </div>

                    <!-- Transaction info -->
                    <div class="tx-info">
                        <div class="tx-info-item" v-if="fine.transaction?.borrowDate">
                            <span class="tx-label">Mượn</span>
                            <span>{{ formatDate(fine.transaction.borrowDate) }}</span>
                        </div>
                        <div class="tx-info-item" v-if="fine.transaction?.dueDate">
                            <span class="tx-label">Hạn trả</span>
                            <span>{{ formatDate(fine.transaction.dueDate) }}</span>
                        </div>
                        <div class="tx-info-item" v-if="fine.transaction?.returnDate">
                            <span class="tx-label">Ngày trả</span>
                            <span>{{ formatDate(fine.transaction.returnDate) }}</span>
                        </div>
                        <div class="tx-info-item">
                            <span class="tx-label">Ngày phạt</span>
                            <span>{{ formatDate(fine.createdDate) }}</span>
                        </div>
                        <div class="tx-info-item" v-if="fine.paidDate">
                            <span class="tx-label">Ngày thu</span>
                            <span>{{ formatDate(fine.paidDate) }}</span>
                        </div>
                    </div>

                    <!-- Note (waive reason) -->
                    <div class="fine-note" v-if="fine.note && (fine.status === 2 || fine.status === 'Waived')">
                        ✓ Lý do miễn: {{ fine.note }}
                    </div>

                    <!-- Pending instruction -->
                    <div class="pending-instruction" v-if="fine.status === 0 || fine.status === 'Pending'">
                        📍 Vui lòng đến quầy thư viện để thanh toán phiếu phạt này
                    </div>
                </div>
            </div>
        </div>

        <!-- Pagination -->
        <div class="pagination" v-if="totalPages > 1">
            <button class="page-btn" :disabled="currentPage === 1" @click="goToPage(currentPage - 1)">‹</button>
            <template v-for="p in visiblePages" :key="p">
                <span v-if="p === '...'" class="page-dots">...</span>
                <button v-else class="page-btn" :class="{ active: p === currentPage }" @click="goToPage(p)">{{ p
                }}</button>
            </template>
            <button class="page-btn" :disabled="currentPage === totalPages"
                @click="goToPage(currentPage + 1)">›</button>
        </div>

    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import api from '../services/api'
import { Icon } from '@iconify/vue'

const router = useRouter()

const items = ref([])
const isLoading = ref(false)
const total = ref(0)
const totalPages = ref(1)
const currentPage = ref(1)
const pageSize = 10
const totalPending = ref(0)
const activeStatus = ref('')

const statsCount = ref({ Pending: 0, Paid: 0, Waived: 0 })
const statsAmount = ref({ Pending: 0, Paid: 0, Waived: 0 })

onMounted(() => fetchAll())

const fetchAll = async () => {
    await Promise.all([fetchData(), fetchStats()])
}

const fetchData = async (page = 1) => {
    isLoading.value = true
    try {
        const params = new URLSearchParams({ page, pageSize })
        if (activeStatus.value) params.append('status', activeStatus.value)

        const res = await api.get(`/account/me/fines?${params}`)
        if (res.status === 200) {
            items.value = res.data.items
            total.value = res.data.total
            totalPages.value = res.data.totalPages
            currentPage.value = res.data.page
            totalPending.value = res.data.totalPending
        }
    } catch { }
    finally { isLoading.value = false }
}

const fetchStats = async () => {
    try {
        const [p, pa, w] = await Promise.all([
            api.get('/account/me/fines?status=Pending&pageSize=1'),
            api.get('/account/me/fines?status=Paid&pageSize=1'),
            api.get('/account/me/fines?status=Waived&pageSize=1'),
        ])
        statsCount.value = {
            Pending: p.data.total,
            Paid: pa.data.total,
            Waived: w.data.total
        }
        statsAmount.value = {
            Pending: p.data.totalPending || 0,
            Paid: pa.data.totalPending || 0,
            Waived: w.data.totalPending || 0
        }
    } catch { }
}

const setStatus = (status) => {
    activeStatus.value = activeStatus.value === status ? '' : status
    fetchData(1)
}

const goToPage = (page) => {
    if (page >= 1 && page <= totalPages.value) fetchData(page)
}

// Computed
const statsDisplay = computed(() => [
    { status: 'Pending', label: 'Chưa thu', count: statsCount.value.Pending, amount: statsAmount.value.Pending, color: 'text-red' },
    { status: 'Paid', label: 'Đã thu', count: statsCount.value.Paid, amount: 0, color: 'text-green' },
    { status: 'Waived', label: 'Đã miễn', count: statsCount.value.Waived, amount: 0, color: 'text-gray' },
])

const visiblePages = computed(() => {
    const pages = [], t = totalPages.value, cur = currentPage.value
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

// Helpers
const fineStatusLabel = (s) => {
    const map = { 0: 'Chưa thu', 1: 'Đã thu', 2: 'Đã miễn', Pending: 'Chưa thu', Paid: 'Đã thu', Waived: 'Đã miễn' }
    return map[s] ?? s
}
const fineStatusClass = (s) => {
    const map = { 0: 'status-red', 1: 'status-green', 2: 'status-gray', Pending: 'status-red', Paid: 'status-green', Waived: 'status-gray' }
    return map[s] ?? ''
}
const fineCardClass = (s) => {
    const map = { 0: 'card-pending', 'Pending': 'card-pending' }
    return map[s] ?? ''
}
const formatDate = (d) => d ? new Date(d).toLocaleDateString('vi-VN') : '—'
const formatMoney = (n) => n != null ? new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(n) : '—'
</script>

<style lang="scss" scoped>
.my-fines {
    display: flex;
    flex-direction: column;
    gap: 20px;
    font-family: 'Segoe UI', sans-serif;
    color: #1a1a2e;
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

// Banner
.pending-banner {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 16px 20px;
    background: linear-gradient(135deg, #ffebee, #fff8f8);
    border: 1.5px solid #ef9a9a;
    border-radius: 12px;
    gap: 16px;
}

.banner-left {
    display: flex;
    align-items: center;
    gap: 14px;
}

.banner-icon {
    font-size: 32px;
}

.banner-title {
    font-size: 15px;
    font-weight: 700;
    color: #c62828;
}

.banner-sub {
    font-size: 13px;
    color: #e53935;
    margin-top: 2px;
}

.banner-amount {
    font-size: 24px;
    font-weight: 800;
    color: #c62828;
    white-space: nowrap;
}

// Stats
.stats-row {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 12px;
}

.stat-card {
    background: #fff;
    border-radius: 12px;
    border: 1.5px solid #e0e0e0;
    padding: 14px 16px;
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
    font-size: 26px;
    font-weight: 800;
    line-height: 1;

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

.stat-amount {
    font-size: 13px;
    font-weight: 600;
    color: #c62828;
    margin-top: 2px;
}

// Fine list
.fine-list {
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.fine-card {
    display: flex;
    gap: 16px;
    background: #fff;
    border-radius: 14px;
    border: 1.5px solid #e0e0e0;
    padding: 16px;
    transition: all 0.15s;

    &:hover {
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06);
    }

    &.card-pending {
        border-color: #ef9a9a;
        background: #fff8f8;
    }
}

.card-cover {
    flex-shrink: 0;
    cursor: pointer;
    height: 96px;

    img,
    .cover-placeholder {
        width: 72px;
        height: 96px;
        border-radius: 8px;
        object-fit: cover;
        border: 1px solid #e0e0e0;
        max-width: 72px;
        font-size: 12px;
    }
    img {
        max-height: 96px;
    }

    .cover-placeholder {
        background: linear-gradient(135deg, #e8eaf6, #c5cae9);
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 28px;
    }
}

.card-info {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 10px;
    min-width: 0;
}

.card-top {
    display: flex;
    justify-content: space-between;
    gap: 12px;
    align-items: flex-start;
}

.card-left {
    flex: 1;
    min-width: 0;
}

.card-right {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    gap: 6px;
    flex-shrink: 0;
}

.card-title {
    font-size: 15px;
    font-weight: 700;
    margin: 0 0 4px;
    cursor: pointer;

    &:hover {
        color: #3949ab;
    }
}

.fine-reason {
    font-size: 13px;
    color: #666;
}

.fine-amount {
    font-size: 20px;
    font-weight: 800;

    &.amount-red {
        color: #c62828;
    }

    &.amount-gray {
        color: #888;
        text-decoration: line-through;
    }
}

.fine-status {
    display: inline-block;
    padding: 3px 10px;
    border-radius: 99px;
    font-size: 12px;
    font-weight: 600;

    &.status-red {
        background: #ffebee;
        color: #c62828;
    }

    &.status-green {
        background: #e8f5e9;
        color: #2e7d32;
    }

    &.status-gray {
        background: #f5f5f5;
        color: #757575;
    }
}

.tx-info {
    display: flex;
    gap: 16px;
    flex-wrap: wrap;
}

.tx-info-item {
    display: flex;
    gap: 4px;
    font-size: 13px;
}

.tx-label {
    color: #aaa;
}

.fine-note {
    font-size: 13px;
    color: #2e7d32;
    font-style: italic;
    padding: 6px 10px;
    background: #f1f8e9;
    border-radius: 6px;
}

.pending-instruction {
    font-size: 13px;
    color: #e65100;
    padding: 8px 12px;
    background: #fff3e0;
    border-radius: 8px;
}

// Empty
.empty-state {
    text-align: center;
    padding: 60px 20px;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 10px;
    .icon_success {
        color: #2e7d32;
    }
}

.empty-icon {
    font-size: 52px;
}

.empty-title {
    font-size: 16px;
    font-weight: 700;
    color: #333;
}

.empty-sub {
    font-size: 14px;
    color: #888;
}

.state-box {
    padding: 40px;
    text-align: center;
    color: #888;
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
    color: #333;
    transition: all 0.15s;

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
</style>