<template>
    <div class="book-search">

        <!-- Search bar -->
        <div class="search-header">
            <div class="search-bar">
                <input v-model="keyword" class="search-input" placeholder="Tìm tên sách, tác giả, chủ đề..."
                    @keydown.enter="() => doSearch()" />
                <button class="search-btn" @click="() => doSearch()">
                    <Icon icon="ic:outline-search" width="18" height="18" /> Tìm kiếm
                </button>
            </div>
            <button class="btn-advanced" :class="{ active: showAdvanced }" @click="showAdvanced = !showAdvanced">
                <Icon icon="gala:settings" width="16" height="16" /> Tìm nâng cao {{ showAdvanced ? '▲' : '▼' }}
            </button>
        </div>

        <!-- Advanced search -->
        <div class="advanced-panel" v-if="showAdvanced">
            <div class="advanced-title">Tìm kiếm nâng cao</div>
            <div class="conditions">
                <div v-for="(cond, idx) in conditions" :key="idx" class="condition-row">
                    <!-- Operator (từ hàng 2 trở đi) -->
                    <select v-if="idx > 0" v-model="cond.operator" class="operator-select">
                        <option value="AND">VÀ (AND)</option>
                        <option value="OR">HOẶC (OR)</option>
                        <option value="NOT">KHÔNG (NOT)</option>
                    </select>
                    <div v-else class="operator-placeholder">Tìm theo</div>

                    <!-- Field -->
                    <select v-model="cond.field" class="field-select">
                        <option value="all">Tất cả các trường</option>
                        <option value="title">Nhan đề</option>
                        <option value="author">Tác giả</option>
                        <option value="category">Thể loại</option>
                        <option value="publisher">Nhà xuất bản</option>
                        <option value="ddc">Mã DDC</option>
                        <option value="year">Năm xuất bản</option>
                        <option value="language">Ngôn ngữ</option>
                        <option value="isbn">ISBN</option>
                    </select>

                    <!-- Value -->
                    <input v-model="cond.value" class="condition-input" :placeholder="condPlaceholder(cond.field)"
                        @keydown.enter="() => doAdvancedSearch()" />

                    <!-- Remove -->
                    <button v-if="conditions.length > 1" class="btn-remove-cond"
                        @click="removeCondition(idx)">✕</button>
                </div>
            </div>

            <div class="advanced-actions">
                <button class="btn-add-cond" @click="addCondition" :disabled="conditions.length >= 5">
                    + Thêm điều kiện
                </button>
                <button class="btn-search-advanced" @click="() => doAdvancedSearch()">
                    <Icon icon="ic:outline-search" width="18" height="18" /> Tìm kiếm nâng cao
                </button>
            </div>
        </div>

        <!-- Filters sidebar + Results -->
        <div class="search-layout">

            <!-- Sidebar filters -->
            <div class="sidebar">
                <div class="filter-section">
                    <div class="filter-title">Loại tài liệu</div>
                    <div class="filter-options">
                        <label v-for="dt in docTypes" :key="dt.id" class="filter-option">
                            <input type="radio" :value="dt.id" v-model="filters.documentTypeId"
                                @change="() => doSearch()" />
                            <Icon :icon="dt.icon" width="18" height="18" />
                            {{ dt.label }}
                        </label>
                        <label class="filter-option">
                            <input type="radio" :value="null" v-model="filters.documentTypeId"
                                @change="() => doSearch()" />
                            Tất cả
                        </label>
                    </div>
                </div>

                <div class="filter-section">
                    <div class="filter-title">Sắp xếp</div>
                    <select v-model="sort" class="filter-select-full" @change="() => doSearch()">
                        <option value="newest">Mới nhất</option>
                        <option value="oldest">Cũ nhất</option>
                        <option value="title">Tên A-Z</option>
                        <option value="popular">Phổ biến nhất</option>
                    </select>
                </div>

                <div class="filter-section">
                    <div class="filter-title">Năm xuất bản</div>
                    <div class="year-range">
                        <input type="number" v-model.number="filters.fromYear" placeholder="Từ năm"
                            @change="() => doSearch()" />
                        <span>—</span>
                        <input type="number" v-model.number="filters.toYear" placeholder="Đến năm"
                            @change="() => doSearch()" />
                    </div>
                </div>

                <div class="filter-section" v-if="filters.authorId || filters.categoryId">
                    <div class="filter-title">Đang lọc theo</div>
                    <div class="active-filters">
                        <div v-if="filters.authorId" class="active-filter">
                            <span>👤 {{ activeAuthorName || 'Tác giả' }}</span>
                            <button @click="filters.authorId = null; activeAuthorName = ''; doSearch()">✕</button>
                        </div>
                        <div v-if="filters.categoryId" class="active-filter">
                            <span>🏷️ {{ activeCategoryName || 'Thể loại' }}</span>
                            <button @click="filters.categoryId = null; activeCategoryName = ''; doSearch()">✕</button>
                        </div>
                    </div>
                </div>



                <button class="btn-reset" @click="resetFilters">↺ Xoá bộ lọc</button>
            </div>

            <!-- Results -->
            <div class="results">
                <!-- Header -->
                <div class="results-header" v-if="!isLoading">
                    <span class="results-count" v-if="total > 0">
                        Tìm thấy <strong>{{ total }}</strong> kết quả
                        <span v-if="keyword"> cho "<em>{{ keyword }}</em>"</span>
                    </span>
                    <span class="results-count" v-else-if="hasSearched">Không tìm thấy kết quả nào</span>
                </div>

                <!-- Loading -->
                <div v-if="isLoading" class="state-box">Đang tìm kiếm...</div>

                <!-- Empty -->
                <div v-else-if="hasSearched && items.length === 0" class="empty-results">
                    <div class="empty-icon">
                        <Icon icon="ic:outline-search" width="48" height="48" />
                    </div>
                    <div class="empty-title">Không tìm thấy kết quả</div>
                    <div class="empty-sub">Thử từ khóa khác hoặc bỏ bớt bộ lọc</div>
                </div>

                <!-- No search yet -->
                <div v-else-if="!hasSearched" class="no-search">
                    <div class="no-search-icon">📚</div>
                    <div class="no-search-title">Nhập từ khóa để tìm kiếm</div>
                    <div class="no-search-sub">Hoặc dùng tìm kiếm nâng cao để tìm chính xác hơn</div>
                </div>

                <!-- Results list -->
                <div v-else class="book-list">
                    <BookCard v-for="book in items" :key="book.bookId" :book="book" @borrow="openBorrowModal" />
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
                    <span class="page-info">{{ total }} kết quả</span>
                </div>
            </div>
        </div>

        <!-- Borrow modal -->
        <ModalBorrowRequest v-model="showBorrowModal" :book="borrowingBook" @success="onBorrowSuccess" />
    </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import BookCard from '../components/User/BookCard.vue'
import api from '../services/api'
import { useToastMessageStore } from '../stores/toastMessage'
import { TOAST_MESSAGE_STATUS } from '../constants'
import ModalBorrowRequest from '../components/User/ModalBorrowRequest.vue'
import { Icon } from '@iconify/vue'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

// ---- State ----
const keyword = ref('')
const showAdvanced = ref(false)
const isLoading = ref(false)
const hasSearched = ref(false)
const items = ref([])
const total = ref(0)
const totalPages = ref(1)
const currentPage = ref(1)
const pageSize = 20
const sort = ref('newest')
const isAdvanced = ref(false)
const activeCategoryName = ref(null)
const activeAuthorName = ref(null)

const filters = reactive({
    documentTypeId: null,
    fromYear: null,
    toYear: null,
    authorId: null,
    categoryId: null
})

const conditions = ref([{ operator: 'AND', field: 'all', value: '' }])

const showBorrowModal = ref(false)
const borrowingBook = ref(null)
const isSubmitting = ref(false)
const borrowForm = reactive({ expectedDate: '', note: '' })
const today = new Date().toISOString().slice(0, 10)
const isPublicPage = computed(() => route?.name?.includes("public"))

const docTypes = [
    { id: 1, icon: "ph:books-duotone", label: "Sách vật lý" },
    { id: 4, icon: "fluent:book-globe-24-regular", label: "Ebook" },
    { id: 3, icon: "emojione:graduation-cap", label: "Luận án" },
    { id: 2, icon: "ph:article-ny-times-fill", label: "Bài trích" },
]

// ---- Init from query params ----
onMounted(async () => {
    if (route.query.keyword) keyword.value = route.query.keyword
    if (route.query.documentTypeId) filters.documentTypeId = Number(route.query.documentTypeId)
    if (route.query.sort) sort.value = route.query.sort
    if (route.query.advanced) showAdvanced.value = true
    if (route.query.authorId) filters.authorId = Number(route.query.authorId)
    if (route.query.categoryId) filters.categoryId = Number(route.query.categoryId)

    if (filters.authorId) {
        try {
            const res = await api.get(`/Authors/${filters.authorId}`)
            if (res.status === 200) activeAuthorName.value = res.data.name
        } catch { }
    }
    if (filters.categoryId) {
        try {
            const res = await api.get(`/Categories/${filters.categoryId}`)
            if (res.status === 200) activeCategoryName.value = res.data.name
        } catch { }
    }
    if (keyword.value || filters.documentTypeId || filters.authorId || filters.categoryId) doSearch()
})

// ---- Search ----
const doSearch = async (page = 1) => {
    isAdvanced.value = false
    isLoading.value = true
    hasSearched.value = true
    try {
        const params = new URLSearchParams({ page, pageSize, sort: sort.value })
        if (keyword.value.trim()) params.append('keyword', keyword.value.trim())
        if (filters.documentTypeId) params.append('documentTypeId', filters.documentTypeId)
        if (filters.fromYear) params.append('fromYear', filters.fromYear)
        if (filters.toYear) params.append('toYear', filters.toYear)
        if (filters.authorId) params.append('authorId', filters.authorId)
        if (filters.categoryId) params.append('categoryId', filters.categoryId)

        const res = await api.get(`/BooksSearch?${params}`)
        if (res.status === 200) {
            items.value = res.data.items
            total.value = res.data.total
            totalPages.value = res.data.totalPages
            currentPage.value = res.data.page
        }
    } catch { }
    finally { isLoading.value = false }
}

const doAdvancedSearch = async (page = 1) => {
    const validConditions = conditions.value.filter(c => c.value.trim())
    if (!validConditions.length) return

    isAdvanced.value = true
    isLoading.value = true
    hasSearched.value = true
    try {
        const res = await api.post(
            `/BooksSearch/advanced-search?page=${page}&pageSize=${pageSize}&sort=${sort.value}`,
            { conditions: validConditions }
        )
        if (res.status === 200) {
            items.value = res.data.items
            total.value = res.data.total
            totalPages.value = res.data.totalPages
            currentPage.value = res.data.page
        }
    } catch { }
    finally { isLoading.value = false }
}

const goToPage = (page) => {
    if (page < 1 || page > totalPages.value) return
    if (isAdvanced.value) doAdvancedSearch(page)
    else doSearch(page)
    window.scrollTo({ top: 0, behavior: 'smooth' })
}

// ---- Conditions ----
const addCondition = () => {
    if (conditions.value.length < 5)
        conditions.value.push({ operator: 'AND', field: 'all', value: '' })
}
const removeCondition = (idx) => conditions.value.splice(idx, 1)

const condPlaceholder = (field) => {
    const map = {
        all: 'Nhập từ khóa...', title: 'Tên sách...', author: 'Tên tác giả...',
        category: 'Thể loại...', publisher: 'Nhà xuất bản...', ddc: 'VD: 600',
        year: 'VD: 2020', language: 'VD: vi hoặc Tiếng Việt', isbn: 'ISBN...'
    }
    return map[field] ?? 'Nhập từ khóa...'
}

const resetFilters = () => {
    filters.documentTypeId = null
    filters.fromYear = null
    filters.toYear = null
    filters.authorId = null
    filters.categoryId = null
    sort.value = 'newest'
    doSearch()
}

// ---- Borrow modal ----
const openBorrowModal = (book) => {
    if (!authStore.user || isPublicPage.value) {
        router.push("/login")
        return
    }
    if (book.documentTypeId == 4) {
        router.push(`${isPublicPage.value ? '' : '/user'}/books/${book.bookId}`)
        return
    }
    borrowingBook.value = book
    showBorrowModal.value = true
}

const onBorrowSuccess = () => {
    const toasMessageStore = useToastMessageStore()
    // hasPendingRequest.value = true
    toasMessageStore.showToastMessage('Gửi yêu cầu mượn thành công!', TOAST_MESSAGE_STATUS.success, 2000)
}

// ---- Computed ----
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
</script>

<style lang="scss" scoped>
.book-search {
    display: flex;
    flex-direction: column;
    gap: 20px;
    font-family: 'Segoe UI', sans-serif;
    color: #1a1a2e;
}

// Search bar
.search-header {
    display: flex;
    gap: 10px;
    align-items: center;
    flex-wrap: wrap;
}

.search-bar {
    display: flex;
    gap: 8px;
    flex: 1;
    min-width: 280px;
}

.search-input {
    flex: 1;
    padding: 11px 16px;
    border: 1.5px solid #e0e0e0;
    border-radius: 10px;
    font-size: 15px;
    outline: none;
    font-family: inherit;

    &:focus {
        border-color: #3949ab;
    }
}

.search-btn {
    padding: 11px 20px;
    background: #3949ab;
    color: #fff;
    border: none;
    border-radius: 10px;
    font-size: 15px;
    font-weight: 600;
    cursor: pointer;
    white-space: nowrap;

    &:hover {
        background: #2c3a8c;
    }

    svg {
        margin-bottom: -4px;
    }
}

.btn-advanced {
    padding: 11px 16px;
    background: #fff;
    border: 1.5px solid #e0e0e0;
    border-radius: 10px;
    font-size: 14px;
    font-weight: 500;
    cursor: pointer;
    white-space: nowrap;
    color: #555;
    transition: all 0.15s;
    display: flex;
    gap: 8px;
    align-items: center;

    &:hover,
    &.active {
        border-color: #3949ab;
        color: #3949ab;
        background: #f0f4ff;
    }
}

// Advanced panel
.advanced-panel {
    background: #ffffff;
    border: 1.5px solid #e8eaf6;
    border-radius: 12px;
    padding: 20px;
}

.advanced-title {
    font-size: 14px;
    font-weight: 700;
    color: #3949ab;
    margin-bottom: 14px;
}

.conditions {
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.condition-row {
    display: flex;
    gap: 8px;
    align-items: center;
    flex-wrap: wrap;
}

.operator-select,
.field-select {
    padding: 8px 10px;
    border: 1.5px solid #e0e0e0;
    border-radius: 8px;
    font-size: 13px;
    background: #fff;
    outline: none;
    font-family: inherit;

    &:focus {
        border-color: #3949ab;
    }
}

.operator-select {
    min-width: 130px;
}

.field-select {
    min-width: 160px;
}

.operator-placeholder {
    min-width: 130px;
    font-size: 13px;
    color: #888;
    padding: 8px 10px;
}

.condition-input {
    flex: 1;
    min-width: 160px;
    padding: 8px 12px;
    border: 1.5px solid #e0e0e0;
    border-radius: 8px;
    font-size: 14px;
    outline: none;
    font-family: inherit;

    &:focus {
        border-color: #3949ab;
    }
}

.btn-remove-cond {
    background: none;
    border: none;
    color: #c62828;
    cursor: pointer;
    font-size: 15px;
    padding: 4px 8px;
    border-radius: 6px;

    &:hover {
        background: #ffebee;
    }
}

.advanced-actions {
    display: flex;
    gap: 10px;
    margin-top: 14px;
    align-items: center;
}

.btn-add-cond {
    padding: 8px 14px;
    background: #fff;
    border: 1.5px solid #3949ab;
    color: #3949ab;
    border-radius: 8px;
    font-size: 13px;
    font-weight: 500;
    cursor: pointer;

    &:hover:not(:disabled) {
        background: #e8eaf6;
    }

    &:disabled {
        opacity: 0.4;
        cursor: not-allowed;
    }
}

.btn-search-advanced {
    padding: 8px 18px;
    background: #3949ab;
    color: #fff;
    border: none;
    border-radius: 8px;
    font-size: 14px;
    font-weight: 600;
    cursor: pointer;

    &:hover {
        background: #2c3a8c;
    }

    svg {
        margin-bottom: -4px;
    }
}

// Layout
.search-layout {
    display: grid;
    grid-template-columns: 220px 1fr;
    gap: 20px;
    align-items: start;
}

// Sidebar
.sidebar {
    background: #fff;
    border-radius: 12px;
    border: 1px solid #e0e0e0;
    padding: 16px;
    display: flex;
    flex-direction: column;
    gap: 16px;
    position: sticky;
    top: 70px;
}

.filter-section {
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.filter-title {
    font-size: 13px;
    font-weight: 700;
    color: #333;
}

.filter-options {
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.filter-option {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 13px;
    cursor: pointer;
    color: #555;

    input {
        cursor: pointer;
    }

    &:hover {
        color: #3949ab;
    }
}

.filter-select-full {
    width: 100%;
    padding: 8px 10px;
    border: 1.5px solid #e0e0e0;
    border-radius: 8px;
    font-size: 13px;
    background: #fff;
    outline: none;
    font-family: inherit;

    &:focus {
        border-color: #3949ab;
    }
}

.year-range {
    display: flex;
    align-items: center;
    gap: 6px;

    span {
        color: #aaa;
        font-size: 12px;
    }

    input {
        flex: 1;
        padding: 7px 8px;
        border: 1.5px solid #e0e0e0;
        border-radius: 8px;
        font-size: 13px;
        outline: none;
        width: 0;

        &:focus {
            border-color: #3949ab;
        }
    }
}

.active-filters {
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.active-filter {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 6px 10px;
    background: #e8eaf6;
    border-radius: 8px;
    font-size: 13px;
    color: #3949ab;

    button {
        background: none;
        border: none;
        cursor: pointer;
        color: #3949ab;
        font-size: 12px;
        padding: 0 2px;

        &:hover {
            color: #c62828;
        }
    }
}

.btn-reset {
    padding: 8px;
    background: none;
    border: 1.5px solid #e0e0e0;
    border-radius: 8px;
    font-size: 13px;
    color: #888;
    cursor: pointer;

    &:hover {
        border-color: #3949ab;
        color: #3949ab;
    }
}

// Results
.results {
    display: flex;
    flex-direction: column;
    gap: 14px;
}

.results-header {}

.results-count {
    font-size: 14px;
    color: #555;

    em {
        color: #3949ab;
        font-style: normal;
        font-weight: 600;
    }
}

.book-list {
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.empty-results,
.no-search {
    text-align: center;
    padding: 60px 20px;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 8px;
}

.empty-icon,
.no-search-icon {
    font-size: 48px;
}

.empty-title,
.no-search-title {
    font-size: 16px;
    font-weight: 700;
    color: #333;
}

.empty-sub,
.no-search-sub {
    font-size: 14px;
    color: #888;
}

.state-box {
    padding: 40px;
    text-align: center;
    color: #888;
    font-size: 14px;
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
    transition: all 0.15s;
    color: #333;

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

.page-info {
    margin-left: 8px;
    font-size: 13px;
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
    max-width: 480px;
    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.2);
    display: block;
    left: unset;
    height: unset;
    top: unset;
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

.borrow-book-preview {
    display: flex;
    gap: 12px;
    align-items: center;
    padding: 12px;
    background: #f9f9f9;
    border-radius: 10px;
}

.borrow-img {
    width: 48px;
    height: 64px;
    object-fit: cover;
    border-radius: 6px;
    flex-shrink: 0;
}

.borrow-title {
    font-size: 14px;
    font-weight: 700;
}

.borrow-author {
    font-size: 13px;
    color: #3949ab;
    margin-top: 4px;
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

    input {
        padding: 8px 12px;
        border: 1.5px solid #e0e0e0;
        border-radius: 8px;
        font-size: 14px;
        outline: none;
        font-family: inherit;

        &:focus {
            border-color: #3949ab;
        }
    }
}

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

@media (max-width: 768px) {
    .search-layout {
        grid-template-columns: 1fr;
    }

    .sidebar {
        position: static;
    }
}
</style>