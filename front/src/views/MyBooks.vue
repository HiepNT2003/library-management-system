<template>
    <div class="my-books">

        <div class="page-header">
            <div>
                <h1 class="page-title">Sách đang mượn</h1>
                <p class="page-desc">Danh sách sách bạn đang mượn và lịch sử mượn trả</p>
            </div>
        </div>

        <!-- Stats -->
        <div class="stats-row">
            <div v-for="s in statsDisplay" :key="s.status" class="stat-card"
                :class="{ active: activeStatus === s.status }" @click="setStatus(s.status)">
                <div class="stat-num" :class="s.color">{{ s.count }}</div>
                <div class="stat-label">{{ s.label }}</div>
            </div>
        </div>

        <!-- List -->
        <div v-if="isLoading" class="state-box">Đang tải...</div>

        <div v-else-if="items.length === 0" class="empty-state">
            <div class="empty-icon">📚</div>
            <div class="empty-title">
                {{ activeStatus === 'Borrowed' ? 'Bạn không có sách nào đang mượn' : 'Không có dữ liệu' }}
            </div>
            <button class="btn btn-primary" @click="$router.push('/user/search')">Tìm sách để mượn</button>
        </div>

        <div v-else class="book-list">
            <div v-for="tx in items" :key="tx.transactionId" class="borrow-card"
                :class="{ 'card-overdue': isOverdue(tx) }">
                <!-- Cover -->
                <div class="card-cover" @click="$router.push(`/user/books/${tx.book?.bookId}`)">
                    <img v-if="tx.book?.imageUrl" :src="tx.book.imageUrl" :alt="tx.book.title" />
                    <div v-else class="cover-placeholder">📖</div>
                </div>

                <!-- Info -->
                <div class="card-info">
                    <div class="card-top">
                        <div>
                            <h3 class="card-title" @click="$router.push(`/user/books/${tx.book?.bookId}`)">{{ tx.book?.title
                                || '—' }}</h3>
                            <div class="card-author" v-if="tx.book?.authors?.length">
                                {{ tx.book.authors.join(', ') }}
                            </div>
                        </div>
                        <span class="tx-status" :class="txStatusClass(tx.status, isOverdue(tx))">
                            {{ txStatusLabel(tx.status, isOverdue(tx)) }}
                        </span>
                    </div>

                    <!-- Dates -->
                    <div class="dates-row">
                        <div class="date-item">
                            <span class="date-label">Ngày mượn</span>
                            <span class="date-value">{{ formatDate(tx.borrowDate) }}</span>
                        </div>
                        <div class="date-item">
                            <span class="date-label">Hạn trả</span>
                            <span class="date-value"
                                :class="isOverdue(tx) ? 'text-red' : isDueSoon(tx) ? 'text-orange' : ''">
                                {{ formatDate(tx.dueDate) }}
                            </span>
                        </div>
                        <div class="date-item" v-if="tx.returnDate">
                            <span class="date-label">Ngày trả</span>
                            <span class="date-value">{{ formatDate(tx.returnDate) }}</span>
                        </div>
                    </div>

                    <!-- Overdue warning -->
                    <div class="overdue-warning" v-if="isOverdue(tx)">
                        ⚠️ Quá hạn <strong>{{ tx.overdueDays }} ngày</strong>
                        — Vui lòng đến thư viện để trả sách
                    </div>

                    <!-- Due soon warning -->
                    <div class="due-soon-warning" v-else-if="isDueSoon(tx) && isActive(tx)">
                        ⏰ Còn <strong>{{ daysUntilDue(tx) }} ngày</strong> nữa đến hạn trả
                    </div>

                    <!-- Pending fine -->
                    <div class="fine-warning" v-if="tx.hasPendingFine">
                        💰 Phiếu phạt chưa thu: <strong>{{ formatMoney(tx.pendingFines) }}</strong>
                        <router-link to="/user/my-fines"> Xem chi tiết →</router-link>
                    </div>

                    <!-- Actions -->
                    <div class="card-actions" v-if="isActive(tx)">
                        <div class="extension-info" v-if="tx.extensionCount > 0">
                            Đã gia hạn {{ tx.extensionCount }} lần
                        </div>
                        <button class="btn btn-extend" @click="openExtendModal(tx)"
                            :disabled="isOverdue(tx) || tx.hasPendingFine"
                            :title="isOverdue(tx) ? 'Không thể gia hạn khi quá hạn' : tx.hasPendingFine ? 'Cần thanh toán phạt trước' : ''">
                            📅 Gia hạn
                        </button>
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

        <!-- Extend Modal -->
        <Teleport to="body">
            <div v-if="showExtendModal" class="modal-overlay" @click.self="showExtendModal = false">
                <div class="modal">
                    <div class="modal-header">
                        <h3>Gia hạn mượn sách</h3>
                        <button class="modal-close" @click="showExtendModal = false">✕</button>
                    </div>
                    <div class="modal-body" v-if="extendingTx">
                        <div class="extend-book-preview">
                            <img v-if="extendingTx.book?.imageUrl" :src="extendingTx.book.imageUrl"
                                class="extend-img" />
                            <div>
                                <div class="extend-title">{{ extendingTx.book?.title }}</div>
                                <div class="extend-meta">
                                    Hạn hiện tại: <strong>{{ formatDate(extendingTx.dueDate) }}</strong>
                                </div>
                            </div>
                        </div>
                        <div class="extend-info-box">
                            <div class="extend-info-row">
                                <span>Hạn hiện tại</span>
                                <strong>{{ formatDate(extendingTx.dueDate) }}</strong>
                            </div>
                            <div class="extend-info-row">
                                <span>Hạn sau khi gia hạn</span>
                                <strong class="text-green">{{ newDueDate }}</strong>
                            </div>
                            <div class="extend-info-row">
                                <span>Đã gia hạn</span>
                                <strong>{{ extendingTx.extensionCount }} lần</strong>
                            </div>
                        </div>
                        <div class="extend-note">
                            📌 Sau khi gia hạn sẽ không thể hoàn tác. Nếu còn phiếu phạt chưa thu sẽ không thể gia hạn.
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-outline" @click="showExtendModal = false">Huỷ</button>
                        <button class="btn btn-primary" @click="submitExtend" :disabled="isExtending">
                            {{ isExtending ? 'Đang gia hạn...' : '📅 Xác nhận gia hạn' }}
                        </button>
                    </div>
                </div>
            </div>
        </Teleport>

    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import api from '../services/api'

const router = useRouter()

const items = ref([])
const isLoading = ref(false)
const total = ref(0)
const totalPages = ref(1)
const currentPage = ref(1)
const pageSize = 10
const activeStatus = ref('Borrowed')

const showExtendModal = ref(false)
const extendingTx = ref(null)
const isExtending = ref(false)

// Stats
const statsCount = ref({ Borrowed: 0, Overdue: 0, Returned: 0 })

onMounted(() => fetchData())

const fetchData = async (page = 1) => {
    isLoading.value = true
    try {
        // Fetch current status
        const params = new URLSearchParams({ page, pageSize })
        if (activeStatus.value) params.append('status', activeStatus.value)

        const res = await api.get(`/account/me/transactions?${params}`)
        if (res.status === 200) {
            items.value = res.data.items
            total.value = res.data.total
            totalPages.value = res.data.totalPages
            currentPage.value = res.data.page
        }

        // Fetch stats (chỉ lần đầu)
        if (page === 1) await fetchStats()
    } catch { }
    finally { isLoading.value = false }
}

const fetchStats = async () => {
    try {
        const [b, o, r] = await Promise.all([
            api.get('/account/me/transactions?status=Borrowed&pageSize=1'),
            api.get('/account/me/transactions?status=Overdue&pageSize=1'),
            api.get('/account/me/transactions?status=Returned&pageSize=1'),
        ])
        statsCount.value = {
            Borrowed: b.data.total,
            Overdue: o.data.total,
            Returned: r.data.total
        }
    } catch { }
}

const setStatus = (status) => {
    activeStatus.value = status
    fetchData(1)
}

const goToPage = (page) => {
    if (page >= 1 && page <= totalPages.value) fetchData(page)
}

// Extend
const openExtendModal = (tx) => {
    extendingTx.value = tx
    showExtendModal.value = true
}

const submitExtend = async () => {
    if (!extendingTx.value) return
    isExtending.value = true
    try {
        const res = await api.post(`/Transactions/${extendingTx.value.transactionId}/extend`)
        if (res.status === 200) {
            showExtendModal.value = false
            alert(`Gia hạn thành công! Hạn trả mới: ${formatDate(res.data.newDueDate)}`)
            await fetchData(currentPage.value)
        }
    } catch (err) {
        alert(err.response?.data?.message || 'Gia hạn thất bại')
    } finally {
        isExtending.value = false
    }
}

// Computed
const newDueDate = computed(() => {
    if (!extendingTx.value) return '—'
    // Tính tạm thời +14 ngày, BE sẽ tính chính xác theo policy
    const d = new Date(extendingTx.value.dueDate)
    d.setDate(d.getDate() + 14)
    return formatDate(d)
})

const statsDisplay = computed(() => [
    { status: 'Borrowed', label: 'Đang mượn', count: statsCount.value.Borrowed, color: 'text-blue' },
    { status: 'Overdue', label: 'Quá hạn', count: statsCount.value.Overdue, color: 'text-red' },
    { status: 'Returned', label: 'Đã trả', count: statsCount.value.Returned, color: 'text-green' },
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
const isOverdue = (tx) => tx.status === 'Overdue' || tx.status === 2
const isActive = (tx) => tx.status === 'Borrowed' || tx.status === 0 || tx.status === 'Overdue' || tx.status === 2
const isDueSoon = (tx) => {
    if (!isActive(tx) || isOverdue(tx)) return false
    return daysUntilDue(tx) <= 3
}
const daysUntilDue = (tx) => {
    const diff = new Date(tx.dueDate) - new Date()
    return Math.ceil(diff / (1000 * 60 * 60 * 24))
}

const txStatusLabel = (s, overdue) => {
    if (overdue) return 'Quá hạn'
    const map = { Borrowed: 'Đang mượn', Returned: 'Đã trả', Overdue: 'Quá hạn', Cancelled: 'Đã huỷ', 0: 'Đang mượn', 1: 'Đã trả', 2: 'Quá hạn', 3: 'Đã huỷ' }
    return map[s] ?? s
}
const txStatusClass = (s, overdue) => {
    if (overdue || s === 'Overdue' || s === 2) return 'status-red'
    const map = { Borrowed: 'status-blue', Returned: 'status-green', Cancelled: 'status-gray', 0: 'status-blue', 1: 'status-green', 3: 'status-gray' }
    return map[s] ?? ''
}

const formatDate = (d) => d ? new Date(d).toLocaleDateString('vi-VN') : '—'
const formatMoney = (n) => n ? new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(n) : '0đ'
</script>

<style lang="scss" scoped>
.my-books {
    display: flex;
    flex-direction: column;
    gap: 20px;
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

    &.text-blue {
        color: #1565c0;
    }

    &.text-red {
        color: #c62828;
    }

    &.text-green {
        color: #2e7d32;
    }
}

.stat-label {
    font-size: 12px;
    color: #888;
    margin-top: 4px;
}

// Book list
.book-list {
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.borrow-card {
    display: flex;
    gap: 16px;
    background: #fff;
    border-radius: 14px;
    border: 1.5px solid #e0e0e0;
    padding: 16px;
    transition: all 0.15s;

    &:hover {
        border-color: #c5cae9;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06);
    }

    &.card-overdue {
        border-color: #ef9a9a;
        background: #fff8f8;
    }
}

.card-cover {
    flex-shrink: 0;
    cursor: pointer;

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

.card-title {
    font-size: 16px;
    font-weight: 700;
    color: #1a1a2e;
    cursor: pointer;
    margin: 0 0 4px;

    &:hover {
        color: #3949ab;
    }
}

.card-author {
    font-size: 13px;
    color: #3949ab;
}

.tx-status {
    display: inline-block;
    padding: 3px 10px;
    border-radius: 99px;
    font-size: 12px;
    font-weight: 600;
    white-space: nowrap;
    flex-shrink: 0;

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

.dates-row {
    display: flex;
    gap: 20px;
    flex-wrap: wrap;
}

.date-item {
    display: flex;
    flex-direction: column;
    gap: 2px;
}

.date-label {
    font-size: 11px;
    color: #aaa;
    text-transform: uppercase;
    letter-spacing: 0.5px;
}

.date-value {
    font-size: 13px;
    font-weight: 600;
}

.text-red {
    color: #c62828;
}

.text-orange {
    color: #e65100;
}

.text-green {
    color: #2e7d32;
}

.overdue-warning {
    padding: 8px 12px;
    background: #ffebee;
    border-left: 3px solid #e53935;
    border-radius: 0 8px 8px 0;
    font-size: 13px;
    color: #c62828;
}

.due-soon-warning {
    padding: 8px 12px;
    background: #fff3e0;
    border-left: 3px solid #fb8c00;
    border-radius: 0 8px 8px 0;
    font-size: 13px;
    color: #e65100;
}

.fine-warning {
    padding: 8px 12px;
    background: #fff8e1;
    border-left: 3px solid #fbc02d;
    border-radius: 0 8px 8px 0;
    font-size: 13px;
    color: #f57f17;

    a {
        color: #f57f17;
        font-weight: 600;
        margin-left: 4px;
    }
}

.card-actions {
    display: flex;
    align-items: center;
    justify-content: space-between;
}

.extension-info {
    font-size: 12px;
    color: #888;
}

.btn-extend {
    padding: 7px 16px;
    background: #e8eaf6;
    color: #3949ab;
    border: 1.5px solid #c5cae9;
    border-radius: 8px;
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.15s;

    &:hover:not(:disabled) {
        background: #c5cae9;
    }

    &:disabled {
        opacity: 0.4;
        cursor: not-allowed;
    }
}

// Empty
.empty-state {
    text-align: center;
    padding: 60px 20px;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 12px;
}

.empty-icon {
    font-size: 52px;
}

.empty-title {
    font-size: 16px;
    font-weight: 700;
    color: #333;
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
    max-width: 440px;
    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.2);
    color: #333333;
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

.modal-body {
    padding: 20px 24px;
    display: flex;
    flex-direction: column;
    gap: 14px;
}

.modal-footer {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
    padding: 16px 24px 20px;
    border-top: 1px solid #f0f0f0;
}

.extend-book-preview {
    display: flex;
    gap: 12px;
    align-items: center;
    padding: 12px;
    background: #f9f9f9;
    border-radius: 10px;
}

.extend-img {
    width: 48px;
    height: 64px;
    object-fit: cover;
    border-radius: 6px;
    flex-shrink: 0;
}

.extend-title {
    font-size: 14px;
    font-weight: 700;
    line-height: 1.3;
    margin-bottom: 4px;
}

.extend-meta {
    font-size: 13px;
    color: #666;
}

.extend-info-box {
    background: #f5f6ff;
    border-radius: 10px;
    padding: 14px;
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.extend-info-row {
    display: flex;
    justify-content: space-between;
    font-size: 14px;

    span {
        color: #888;
    }

    .text-green {
        color: #2e7d32;
    }
}

.extend-note {
    font-size: 12px;
    color: #888;
    line-height: 1.5;
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
        background: #3949ab;
        color: #fff;

        &:hover:not(:disabled) {
            background: #2c3a8c;
        }
    }

    &.btn-outline {
        background: #fff;
        color: #3949ab;
        border: 1.5px solid #3949ab;

        &:hover:not(:disabled) {
            background: #e8eaf6;
        }
    }
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