<template>
  <div class="my-reading">

    <div class="page-header">
      <div>
        <h1 class="page-title">Đang đọc</h1>
        <p class="page-desc">Các ebook bạn đang đọc dở</p>
      </div>
      <div class="total-count" v-if="total > 0">{{ total }} cuốn</div>
    </div>

    <div v-if="isLoading" class="state-box">Đang tải...</div>

    <div v-else-if="items.length === 0" class="empty-state">
      <div class="empty-icon">📖</div>
      <div class="empty-title">Chưa có ebook nào đang đọc</div>
      <div class="empty-sub">Tìm và đọc ebook để tiến độ xuất hiện ở đây</div>
      <button class="btn btn-primary" @click="$router.push('/user/search?documentTypeId=4')">
        <Icon icon="ic:outline-search" width="18" height="18" /> Tìm Ebook
      </button>
    </div>

    <div v-else class="reading-grid">
      <div v-for="item in items" :key="item.progressId" class="reading-card" @click="continueReading(item)">
        <!-- Cover -->
        <div class="card-cover">
          <img v-if="item.book.imageUrl" :src="item.book.imageUrl" :alt="item.book.title" />
          <div v-else class="cover-placeholder">📖</div>

          <!-- Progress overlay -->
          <div class="progress-overlay">
            <div class="progress-bar">
              <div class="progress-fill" :style="{ width: (item.percentRead || 0) + '%' }"></div>
            </div>
            <span class="progress-pct">{{ Math.round(item.percentRead || 0) }}%</span>
          </div>
        </div>

        <!-- Info -->
        <div class="card-info">
          <div class="card-title">{{ truncate(item.book.title, 50) }}</div>
          <div class="card-author" v-if="item.book.authors?.length">
            {{ [...item.book.authors].join(', ') }}
          </div>
          <div class="card-meta">
            <span class="page-badge">Trang {{ item.currentPage }}</span>
            <span class="last-read">{{ formatRelative(item.lastReadDate) }}</span>
          </div>
        </div>

        <!-- Continue button -->
        <div class="card-footer">
          <button class="btn-continue" @click.stop="continueReading(item)">
            ▶ Tiếp tục đọc
          </button>
        </div>
      </div>
    </div>

    <!-- Pagination -->
    <div class="pagination" v-if="totalPages > 1">
      <button class="page-btn" :disabled="currentPage === 1" @click="goToPage(currentPage - 1)">‹</button>
      <template v-for="p in visiblePages" :key="p">
        <span v-if="p === '...'" class="page-dots">...</span>
        <button v-else class="page-btn" :class="{ active: p === currentPage }" @click="goToPage(p)">{{ p }}</button>
      </template>
      <button class="page-btn" :disabled="currentPage === totalPages" @click="goToPage(currentPage + 1)">›</button>
    </div>

    <EbookReader v-if="selectedBook && selectedBook.filePath && viewEbook" :book-id="selectedBook.bookId"
      :fileUrl="selectedBook.filePath" :book-title="selectedBook.title" :save-progress="true" :default-fullscreen="true"
      :show-change-fullscreen="false" @on:closePDF="viewEbook = false; selectedBook = null" />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import api from '../services/api'
import EbookReader from '../components/share/EbookReader.vue'

const router = useRouter()

const items = ref([])
const isLoading = ref(false)
const total = ref(0)
const totalPages = ref(1)
const currentPage = ref(1)
const pageSize = 12
const viewEbook = ref(false)
const selectedBook = ref(null)

onMounted(() => fetchData())

const fetchData = async (page = 1) => {
  isLoading.value = true
  try {
    const res = await api.get(`/account/me/reading?page=${page}&pageSize=${pageSize}`)
    if (res.status === 200) {
      items.value = res.data.items
      total.value = res.data.total
      totalPages.value = res.data.totalPages
      currentPage.value = res.data.page
    }
  } catch { }
  finally { isLoading.value = false }
}

const continueReading = (item) => {
  viewEbook.value = true
  selectedBook.value = item.book
}

const goToPage = (page) => {
  if (page >= 1 && page <= totalPages.value) fetchData(page)
}

// Computed
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
const truncate = (str, len) => !str ? '' : str.length > len ? str.slice(0, len) + '...' : str

const formatRelative = (d) => {
  if (!d) return ''
  const diff = Date.now() - new Date(d).getTime()
  const mins = Math.floor(diff / 60000)
  const hours = Math.floor(diff / 3600000)
  const days = Math.floor(diff / 86400000)
  if (mins < 1) return 'Vừa xong'
  if (mins < 60) return `${mins} phút trước`
  if (hours < 24) return `${hours} giờ trước`
  if (days < 7) return `${days} ngày trước`
  return new Date(d).toLocaleDateString('vi-VN')
}
</script>

<style lang="scss" scoped>
.my-reading {
  display: flex;
  flex-direction: column;
  gap: 20px;
  font-family: 'Segoe UI', sans-serif;
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

.total-count {
  font-size: 14px;
  font-weight: 600;
  color: #3949ab;
  background: #e8eaf6;
  padding: 4px 12px;
  border-radius: 99px;
}

// Grid
.reading-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 16px;
}

.reading-card {
  background: #fff;
  border-radius: 14px;
  border: 1.5px solid #e0e0e0;
  overflow: hidden;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  flex-direction: column;

  &:hover {
    border-color: #3949ab;
    box-shadow: 0 8px 24px rgba(57, 73, 171, 0.12);
    transform: translateY(-3px);
  }
}

// Cover
.card-cover {
  position: relative;
  width: 100%;
  aspect-ratio: 3/4;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    display: block;
  }
}

.cover-placeholder {
  width: 100%;
  height: 100%;
  background: linear-gradient(135deg, #e8eaf6, #c5cae9);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 48px;
}

// Progress overlay
.progress-overlay {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  background: linear-gradient(transparent, rgba(0, 0, 0, 0.7));
  padding: 20px 10px 8px;
  display: flex;
  align-items: center;
  gap: 8px;
}

.progress-bar {
  flex: 1;
  height: 4px;
  background: rgba(255, 255, 255, 0.3);
  border-radius: 2px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: #3949ab;
  border-radius: 2px;
  transition: width 0.3s;
}

.progress-pct {
  font-size: 11px;
  font-weight: 700;
  color: #fff;
  white-space: nowrap;
}

// Info
.card-info {
  padding: 12px 14px 8px;
  flex: 1;
}

.card-title {
  font-size: 14px;
  font-weight: 700;
  color: #1a1a2e;
  line-height: 1.3;
  margin-bottom: 4px;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.card-author {
  font-size: 12px;
  color: #3949ab;
  margin-bottom: 8px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.card-meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 6px;
}

.page-badge {
  font-size: 11px;
  background: #e8eaf6;
  color: #3949ab;
  padding: 2px 8px;
  border-radius: 99px;
  font-weight: 600;
  white-space: nowrap;
}

.last-read {
  font-size: 11px;
  color: #aaa;
}

// Footer
.card-footer {
  padding: 0 14px 14px;
}

.btn-continue {
  width: 100%;
  padding: 8px;
  background: #3949ab;
  color: #fff;
  border: none;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.15s;

  &:hover {
    background: #2c3a8c;
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

.state-box {
  padding: 40px;
  text-align: center;
  color: #888;
}

// Button
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

  &.btn-primary {
    background: #3949ab;
    color: #fff;

    &:hover {
      background: #2c3a8c;
    }

    svg {
      margin-bottom: -4px;
    }
  }
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