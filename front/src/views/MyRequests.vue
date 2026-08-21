<template>
    <div class="my-requests">

        <div class="page-header">
            <div>
                <h1 class="page-title">Yêu cầu mượn sách</h1>
                <p class="page-desc">Danh sách yêu cầu mượn sách của bạn</p>
            </div>
            <button class="btn btn-primary" @click="$router.push('/user/search')">
                + Đặt mượn sách mới
            </button>
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
            <div class="empty-icon">📋</div>
            <div class="empty-title">Chưa có yêu cầu mượn nào</div>
            <div class="empty-sub">Tìm sách và đặt mượn để bắt đầu</div>
            <button class="btn btn-primary" @click="$router.push('/user/search')">
                <Icon icon="ic:outline-search" width="18" height="18" /> Tìm sách
            </button>
        </div>

        <div v-else class="request-list">
            <div v-for="req in items" :key="req.requestId" class="request-card" :class="cardClass(req.status)">
                <!-- Cover -->
                <div class="card-cover" @click="$router.push(`/user/books/${req.book?.bookId}`)">
                    <img v-if="req.book?.imageUrl" :src="req.book.imageUrl" :alt="req.book.title" />
                    <div v-else class="cover-placeholder">📖</div>
                </div>

                <!-- Info -->
                <div class="card-info">
                    <div class="card-top">
                        <div class="card-left">
                            <h3 class="card-title" @click="$router.push(`/user/books/${req.book?.bookId}`)">{{
                                req.book?.title }}</h3>
                            <div class="card-author" v-if="req.book?.authors?.length">
                                {{ [...req.book.authors].join(', ') }}
                            </div>
                        </div>
                        <span class="req-status" :class="reqStatusClass(req.status)">
                            {{ reqStatusLabel(req.status) }}
                        </span>
                    </div>

                    <!-- Dates -->
                    <div class="dates-row">
                        <div class="date-item">
                            <span class="date-label">Ngày đặt</span>
                            <span class="date-value">{{ formatDateTime(req.requestDate) }}</span>
                        </div>
                        <div class="date-item" v-if="req.expectedBorrowDate">
                            <span class="date-label">Ngày dự kiến lấy</span>
                            <span class="date-value">{{ formatDate(req.expectedBorrowDate) }}</span>
                        </div>
                        <div class="date-item" v-if="req.approvedDate">
                            <span class="date-label">Ngày duyệt</span>
                            <span class="date-value">{{ formatDate(req.approvedDate) }}</span>
                        </div>
                    </div>

                    <!-- Note -->
                    <div class="req-note" v-if="req.note">
                        📝 {{ req.note }}
                    </div>

                    <!-- Approved info -->
                    <!-- Thêm vào approved-box -->
                    <div class="approved-box" v-if="req.status === 1 || req.status === 'Approved'">
                        <div class="approved-text">
                            <Icon class="icon_success" icon="charm:circle-tick" width="16" height="16" /> Yêu cầu đã
                            được duyệt. Đến thư viện lấy sách trước
                            <strong v-if="req.expectedBorrowDate">{{ formatDate(req.expectedBorrowDate) }}</strong>.
                        </div>
                        <!-- QR Code -->
                        <div class="qr-section">
                            <div class="qr-label">Mã yêu cầu — đưa cho thủ thư quét</div>
                            <!-- Template — thay canvas bằng img -->

                            <div v-if="qrImages[req.requestId]" class="qr-wrapper" @click="openQR(req)">
                                <img :src="qrImages[req.requestId]" class="qr-img" />
                                <div class="qr-hint">🔍 Click để phóng to</div>
                            </div>
                            <div v-else class="qr-loading">Đang tạo QR...</div>
                            <div class="qr-id">#{{ req.requestId }}</div>
                        </div>
                    </div>

                    <!-- Rejected reason -->
                    <div class="rejected-box" v-if="req.status === 2 || req.status === 'Rejected'">
                        ❌ Lý do từ chối: <strong>{{ req.rejectedReason || 'Không có lý do cụ thể' }}</strong>
                    </div>

                    <!-- Actions -->
                    <div class="card-actions">
                        <button v-if="req.status === 0 || req.status === 'Pending'" class="btn btn-cancel"
                            @click="confirmCancel(req)">
                            ✕ Huỷ yêu cầu
                        </button>
                        <button v-if="req.status === 2 || req.status === 'Rejected'" class="btn btn-outline-sm"
                            @click="$router.push(`/user/books/${req.book?.bookId}`)">
                            <Icon icon="mingcute:refresh-3-line" width="18" height="18" /> Đặt lại
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

        <!-- Cancel confirm modal -->
        <Teleport to="body">
            <div v-if="showCancelModal" class="modal-overlay" @click.self="showCancelModal = false">
                <div class="modal">
                    <div class="modal-header">
                        <h3>Huỷ yêu cầu mượn</h3>
                        <button class="modal-close" @click="showCancelModal = false">✕</button>
                    </div>
                    <div class="modal-body">
                        <div class="cancel-preview">
                            <img v-if="cancellingReq?.book?.imageUrl" :src="cancellingReq.book.imageUrl"
                                class="cancel-img" />
                            <div>
                                <div class="cancel-title">{{ cancellingReq?.book?.title }}</div>
                                <div class="cancel-date">Đặt ngày {{ formatDateTime(cancellingReq?.requestDate) }}</div>
                            </div>
                        </div>
                        <p class="cancel-confirm-text">
                            Bạn có chắc muốn huỷ yêu cầu này không? Hành động này không thể hoàn tác.
                        </p>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-outline" @click="showCancelModal = false">Không</button>
                        <button class="btn btn-danger" @click="submitCancel" :disabled="isCancelling">
                            {{ isCancelling ? 'Đang huỷ...' : '✕ Xác nhận huỷ' }}
                        </button>
                    </div>
                </div>
            </div>
        </Teleport>

        <Teleport to="body">
            <div v-if="showQRModal" class="modal-overlay" @click.self="showQRModal = false">
                <div class="qr-modal">
                    <div class="qr-modal-header">
                        <h3>Mã QR xác nhận</h3>
                        <button @click="showQRModal = false">✕</button>
                    </div>
                    <div class="qr-modal-body">
                        <img v-if="selectedReq" :src="qrImages[selectedReq.requestId]" class="qr-img" />
                        <div class="qr-book-title">{{ selectedReq?.book?.title }}</div>
                        <div class="qr-id">Mã yêu cầu: #{{ selectedReq?.requestId }}</div>
                        <div class="qr-desc">Xuất trình mã này cho thủ thư khi đến lấy sách</div>
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>

<script setup>
import QRCode from 'qrcode'
import { ref, computed, onMounted, nextTick, watch } from 'vue'
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
const activeStatus = ref('')

const showCancelModal = ref(false)
const cancellingReq = ref(null)
const isCancelling = ref(false)
const showQRModal = ref(false)
const selectedReq = ref(null)

const statsCount = ref({ Pending: 0, Approved: 0, Rejected: 0, Cancelled: 0, Completed: 0 })

onMounted(() => fetchAll())

const fetchAll = async () => {
    await Promise.all([fetchData(), fetchStats()])
}

const qrImages = ref({})

const generateQRCodes = async () => {
    await nextTick()

    for (const item of items.value) {
        const isApproved = item.status === 1 || item.status === 'Approved'
        if (!isApproved || qrImages.value[item.requestId]) continue

        try {
            qrImages.value[item.requestId] = await QRCode.toDataURL(
                String(item.requestId),
                { width: 140, margin: 2, color: { dark: '#1a1a2e', light: '#ffffff' } }
            )
        } catch (err) {
            console.error('QR error:', err)
        }
    }
}

const openQR = async (req) => {
    selectedReq.value = req
    showQRModal.value = true
}

const fetchData = async (page = 1) => {
    isLoading.value = true
    try {
        const params = new URLSearchParams({ page, pageSize })
        if (activeStatus.value) params.append('status', activeStatus.value)

        const res = await api.get(`/account/me/requests?${params}`)
        if (res.status === 200) {
            items.value = res.data.items
            total.value = res.data.total
            totalPages.value = res.data.totalPages
            currentPage.value = res.data.page
            await generateQRCodes()
        }
    } catch { }
    finally { isLoading.value = false }
}

const fetchStats = async () => {
    try {
        const [p, a, r, c, d] = await Promise.all([
            api.get('/account/me/requests?status=Pending&pageSize=1'),
            api.get('/account/me/requests?status=Approved&pageSize=1'),
            api.get('/account/me/requests?status=Rejected&pageSize=1'),
            api.get('/account/me/requests?status=Cancelled&pageSize=1'),
            api.get('/account/me/requests?status=Completed&pageSize=1'),
        ])
        statsCount.value = {
            Pending: p.data.total,
            Approved: a.data.total,
            Rejected: r.data.total,
            Cancelled: c.data.total,
            Completed: d.data.total,
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

// Cancel
const confirmCancel = (req) => {
    cancellingReq.value = req
    showCancelModal.value = true
}

const submitCancel = async () => {
    if (!cancellingReq.value) return
    isCancelling.value = true
    try {
        const res = await api.delete(`/account/me/requests/${cancellingReq.value.requestId}`)
        if (res.status === 200) {
            showCancelModal.value = false
            await fetchAll()
        }
    } catch (err) {
        alert(err.response?.data?.message || 'Huỷ thất bại')
    } finally {
        isCancelling.value = false
    }
}

// Computed
const statsDisplay = computed(() => [
    { status: 'Pending', label: 'Chờ duyệt', count: statsCount.value.Pending, color: 'text-yellow' },
    { status: 'Approved', label: 'Đã duyệt', count: statsCount.value.Approved, color: 'text-green' },
    { status: 'Rejected', label: 'Từ chối', count: statsCount.value.Rejected, color: 'text-red' },
    { status: 'Cancelled', label: 'Đã huỷ', count: statsCount.value.Cancelled, color: 'text-gray' },
    { status: 'Completed', label: 'hoàn thành', count: statsCount.value.Completed, color: 'text-blue' },
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
const reqStatusLabel = (s) => {
    const map = {
        0: 'Chờ duyệt', 1: 'Đã duyệt', 2: 'Từ chối', 3: 'Đã huỷ', 4: 'Đã lấy sách',
        Pending: 'Chờ duyệt', Approved: 'Đã duyệt', Rejected: 'Từ chối',
        Cancelled: 'Đã huỷ', Completed: 'Đã lấy sách'
    }
    return map[s] ?? s
}
const reqStatusClass = (s) => {
    const map = {
        0: 'status-yellow', 1: 'status-green', 2: 'status-red', 3: 'status-gray', 4: 'status-blue',
        Pending: 'status-yellow', Approved: 'status-green', Rejected: 'status-red',
        Cancelled: 'status-gray', Completed: 'status-blue'
    }
    return map[s] ?? ''
}
const cardClass = (s) => {
    const map = { 1: 'card-approved', 'Approved': 'card-approved', 2: 'card-rejected', 'Rejected': 'card-rejected' }
    return map[s] ?? ''
}
const formatDate = (d) => d ? new Date(d).toLocaleDateString('vi-VN') : '—'
const formatDateTime = (d) => d ? new Date(d).toLocaleString('vi-VN') : '—'
</script>

<style lang="scss" scoped>
.my-requests {
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
    gap: 12px;
    flex-wrap: wrap;
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
}

// List
.request-list {
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.request-card {
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

    &.card-approved {
        border-color: #a5d6a7;
        background: #f9fff9;
    }

    &.card-rejected {
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

.card-left {
    flex: 1;
    min-width: 0;
}

.card-title {
    font-size: 16px;
    font-weight: 700;
    margin: 0 0 4px;
    cursor: pointer;

    &:hover {
        color: #3949ab;
    }
}

.card-author {
    font-size: 13px;
    color: #3949ab;
}

.req-status {
    display: inline-block;
    padding: 3px 10px;
    border-radius: 99px;
    font-size: 12px;
    font-weight: 600;
    white-space: nowrap;
    flex-shrink: 0;

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

.req-note {
    font-size: 13px;
    color: #666;
    font-style: italic;
    padding: 6px 10px;
    background: #f9f9f9;
    border-radius: 6px;
}

.approved-box {
    padding: 10px 14px;
    background: #e8f5e9;
    border-left: 3px solid #43a047;
    border-radius: 0 8px 8px 0;
    font-size: 13px;
    color: #2e7d32;
    line-height: 1.6;

    .approved-text {
        display: flex;
        align-items: center;
        gap: 4px;
    }

    .req-id {
        font-family: monospace;
        font-size: 14px;
        color: #1b5e20;
    }
}

.rejected-box {
    padding: 10px 14px;
    background: #ffebee;
    border-left: 3px solid #e53935;
    border-radius: 0 8px 8px 0;
    font-size: 13px;
    color: #c62828;
}

.card-actions {
    display: flex;
    gap: 8px;
}

.btn-cancel {
    padding: 6px 14px;
    background: #fff;
    color: #c62828;
    border: 1.5px solid #ef9a9a;
    border-radius: 8px;
    font-size: 13px;
    font-weight: 500;
    cursor: pointer;
    transition: all 0.15s;

    &:hover {
        background: #ffebee;
    }
}

.btn-outline-sm {
    padding: 6px 14px;
    background: #fff;
    color: #3949ab;
    border: 1.5px solid #3949ab;
    border-radius: 8px;
    font-size: 13px;
    font-weight: 500;
    cursor: pointer;
    transition: all 0.15s;

    &:hover {
        background: #e8eaf6;
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

.empty-sub {
    font-size: 14px;
    color: #888;
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

.cancel-preview {
    display: flex;
    gap: 12px;
    align-items: center;
    padding: 12px;
    background: #f9f9f9;
    border-radius: 10px;
}

.cancel-img {
    width: 48px;
    height: 64px;
    object-fit: cover;
    border-radius: 6px;
    flex-shrink: 0;
}

.cancel-title {
    font-size: 14px;
    font-weight: 700;
    line-height: 1.3;
    margin-bottom: 4px;
}

.cancel-date {
    font-size: 12px;
    color: #888;
}

.cancel-confirm-text {
    font-size: 14px;
    color: #555;
    margin: 0;
    line-height: 1.6;
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

    &.btn-danger {
        background: #e53935;
        color: #fff;

        &:hover:not(:disabled) {
            background: #c62828;
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

.qr-section {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 6px;
    margin-top: 12px;
    padding: 14px;
    background: #fff;
    border-radius: 10px;
    border: 1px solid #a5d6a7;
    width: fit-content;
}

.qr-canvas {
    border-radius: 6px;
}

.qr-label {
    font-size: 12px;
    color: #2e7d32;
    font-weight: 600;
}

.qr-id {
    font-size: 16px;
    font-weight: 800;
    color: #1a1a2e;
    font-family: monospace;
}

.qr-img {
    width: 140px;
    height: 140px;
    border-radius: 6px;
}

.qr-loading {
    width: 140px;
    height: 140px;
    background: #f5f5f5;
    border-radius: 6px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 12px;
    color: #aaa;
}

.qr-wrapper {
    cursor: pointer;
    display: inline-block;

    &:hover .qr-hint {
        opacity: 1;
    }
}

.qr-small {
    width: 80px;
    height: 80px;
    display: block;
}

.qr-hint {
    font-size: 11px;
    color: #3949ab;
    text-align: center;
    opacity: 0;
    transition: opacity 0.15s;
    margin-top: 3px;
}

.qr-modal {
    background: #fff;
    border-radius: 16px;
    padding: 0;
    width: 360px;
    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.2);
    overflow: hidden;
}

.qr-modal-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 16px 20px;
    border-bottom: 1px solid #f0f0f0;
    color: #333333;

    h3 {
        margin: 0;
        font-size: 16px;
        font-weight: 700;
    }

    button {
        background: none;
        border: none;
        font-size: 18px;
        cursor: pointer;
        color: #aaa;
    }
}

.qr-modal-body {
    padding: 24px;
    text-align: center;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 10px;

    .qr-img {
        width: 240px;
        height: 240px;
        border-radius: 6px;
    }
}

.qr-large {
    border-radius: 8px;
}

.qr-book-title {
    font-size: 15px;
    font-weight: 700;
    color: #1a1a2e;
}

.qr-id {
    font-size: 13px;
    color: #888;
}

.qr-desc {
    font-size: 13px;
    color: #e65100;
    padding: 8px 16px;
    background: #fff3e0;
    border-radius: 8px;
    width: 100%;
    box-sizing: border-box;
}
</style>