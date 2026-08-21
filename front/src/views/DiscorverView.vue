<template>
  <div class="home-page">
    <!-- Banner + Quick Search -->
    <div class="hero">
      <div class="hero-content">
        <div class="hero-text">
          <h1 class="hero-title">
            Thư viện <span class="hero-accent">ĐH Giao thông Vận tải</span>
          </h1>
          <p class="hero-desc">
            Khám phá kho tàng tri thức với hàng nghìn đầu sách, tài liệu và luận án
          </p>
        </div>
        <div class="hero-search">
          <div class="search-bar">
            <input v-model="quickSearch" class="search-input" placeholder="Tìm tên sách, tác giả, chủ đề..."
              @keydown.enter="goSearch" />
            <button class="search-btn" @click="goSearch">
              <Icon icon="ic:outline-search" width="20" height="20" /> Tìm kiếm
            </button>
          </div>
          <div class="search-links">
            <button class="link-btn" @click="$router.push(`${isPublicPage ? '' : '/user'}/search?advanced=1`)">
              Tìm kiếm nâng cao
              <Icon icon="cil:arrow-right" width="12" height="12" />
            </button>
          </div>
        </div>
      </div>
      <div class="hero-stats">
        <div class="hero-stat">
          <div class="hero-stat-num">{{ stats.totalBooks?.toLocaleString() || "—" }}</div>
          <div class="hero-stat-label">Đầu sách</div>
        </div>
        <div class="hero-stat">
          <div class="hero-stat-num">{{ stats.totalCopies?.toLocaleString() || "—" }}</div>
          <div class="hero-stat-label">Bản sao</div>
        </div>
        <div class="hero-stat">
          <div class="hero-stat-num">{{ stats.availableCopies?.toLocaleString() || "—" }}</div>
          <div class="hero-stat-label">Có thể mượn</div>
        </div>
      </div>
    </div>

    <!-- Quick filters -->
    <div class="quick-filters">
      <button v-for="f in docTypeFilters" :key="f.id" class="filter-pill"
        @click="$router.push(`${isPublicPage ? '' : '/user'}/search?documentTypeId=${f.id}`)">
        <Icon :icon="f.icon" width="18" height="18" /> {{ f.label }}
      </button>
    </div>

    <!-- Gợi ý cá nhân (nếu đã đăng nhập) -->
    <section v-if="authStore.getUser && personalRecs.length > 0 && !isPublicPage" class="book-section">
      <div class="section-header">
        <h2 class="section-title">✨ Gợi ý cho bạn</h2>
        <span class="section-sub">Dựa theo lịch sử mượn của bạn</span>
      </div>
      <div class="book-list">
        <BookCard v-for="book in personalRecs" :key="book.bookId || book.book.bookId"
          :book="book.book ? book.book : book" @borrow="openBorrowModal" />
      </div>
    </section>

    <!-- Sách mới nhập -->
    <section class="book-section">
      <div class="section-header">
        <h2 class="section-title">
          <Icon class="title_icon" icon="lsicon:badge-new-filled" width="24" height="24" /> Sách mới
          nhập
        </h2>
        <button class="btn-view-all" @click="$router.push(`${isPublicPage ? '' : '/user'}/search?sort=newest`)">
          Xem tất cả
          <Icon icon="humbleicons:arrow-right" width="16" height="16" />
        </button>
      </div>
      <div v-if="loadingNew" class="section-loading">Đang tải...</div>
      <div v-else class="book-scroll">
        <div class="book-scroll-inner">
          <div v-for="book in newArrivals" :key="book.bookId" class="book-thumb-card"
            @click="$router.push(`${isPublicPage ? '' : '/user'}/books/${book.bookId}`)">
            <div class="thumb-cover">
              <img v-if="book.imageUrl" :src="book.imageUrl" :alt="book.title" />
              <div v-else class="thumb-placeholder">📖</div>
              <div class="thumb-available" v-if="book.availableCopies > 0">✓</div>
            </div>
            <div class="thumb-title">{{ truncate(book.title, 35) }}</div>
            <div class="thumb-author">{{ book.authors?.slice(0, 1).join("") || "" }}</div>
          </div>
        </div>
      </div>
    </section>

    <!-- Sách phổ biến -->
    <section class="book-section">
      <div class="section-header">
        <h2 class="section-title">🔥 Được mượn nhiều nhất</h2>
        <button class="btn-view-all" @click="$router.push(`${isPublicPage ? '' : '/user'}/search?sort=popular`)">
          Xem tất cả
          <Icon icon="humbleicons:arrow-right" width="16" height="16" />
        </button>
      </div>
      <div v-if="loadingPopular" class="section-loading">Đang tải...</div>
      <div v-else class="book-list">
        <BookCard v-for="book in popularBooks.slice(0, 6)" :key="book.bookId" :book="book" @borrow="openBorrowModal" />
      </div>
    </section>
    <ModalBorrowRequest v-model="showBorrowModal" :book="borrowingBook" @success="onBorrowSuccess" />
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useAuthStore } from "@/stores/auth"
import BookCard from "../components/User/BookCard.vue"
import api from "../services/api"
import ModalBorrowRequest from "../components/User/ModalBorrowRequest.vue"
import { Icon } from "@iconify/vue"
import { useToastMessageStore } from "../stores/toastMessage"
import { TOAST_MESSAGE_STATUS } from "../constants"

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const quickSearch = ref("")
const newArrivals = ref([])
const popularBooks = ref([])
const personalRecs = ref([])
const loadingNew = ref(false)
const loadingPopular = ref(false)
const stats = ref({})

const showBorrowModal = ref(false)
const borrowingBook = ref(null)

const docTypeFilters = [
  { id: 1, icon: "ph:books-duotone", label: "Sách vật lý" },
  { id: 4, icon: "fluent:book-globe-24-regular", label: "Ebook" },
  { id: 3, icon: "emojione:graduation-cap", label: "Luận án" },
  { id: 2, icon: "ph:article-ny-times-fill", label: "Bài trích" },
]

const isPublicPage = computed(() => route?.name?.includes("public"))

onMounted(async () => {
  authStore.setIsLoadingApi(true)
  await Promise.all([
    fetchNewArrivals(),
    fetchPopular(),
    fetchStats(),
    authStore.getUser && !isPublicPage.value ? fetchPersonalRecs() : Promise.resolve(),
  ])
  authStore.setIsLoadingApi(false)
})

const fetchStats = async () => {
  try {
    const res = await api.get("/BooksSearch/total")
    if (res.status === 200) stats.value = res.data
  } catch { }
}

const fetchNewArrivals = async () => {
  loadingNew.value = true
  try {
    const res = await api.get("/Books/new-arrivals")
    if (res.status === 200) newArrivals.value = res.data
  } catch {
  } finally {
    loadingNew.value = false
  }
}

const fetchPopular = async () => {
  loadingPopular.value = true
  try {
    const res = await api.get("/popular")
    if (res.status === 200) popularBooks.value = res.data
  } catch {
  } finally {
    loadingPopular.value = false
  }
}

const fetchPersonalRecs = async () => {
  try {
    const res = await api.get("/recommendation/personal")
    if (res.status === 200) personalRecs.value = res.data
  } catch { }
}

const goSearch = () => {
  if (quickSearch.value.trim())
    router.push(
      `${isPublicPage.value ? "" : "/user"}/search?keyword=${encodeURIComponent(
        quickSearch.value.trim()
      )}`
    )
  else router.push(`${isPublicPage.value ? "" : "/user"}/search`)
}

const openBorrowModal = (book) => {
  if (!authStore.getUser || isPublicPage.value) {
    router.push("/login")
    return
  }
  borrowingBook.value = book
  showBorrowModal.value = true
}

const onBorrowSuccess = () => {
  const toasMessageStore = useToastMessageStore()
  // hasPendingRequest.value = true
  toasMessageStore.showToastMessage(
    "Gửi yêu cầu mượn thành công!",
    TOAST_MESSAGE_STATUS.success,
    2000
  )
}

const truncate = (str, len) => (!str ? "" : str.length > len ? str.slice(0, len) + "..." : str)
</script>

<style lang="scss" scoped>
.home-page {
  display: flex;
  flex-direction: column;
  gap: 32px;
  color: #1a1a2e;
}

// Hero
.hero {
  background: linear-gradient(135deg, #1a237e 0%, #283593 50%, #3949ab 100%);
  border-radius: 20px;
  padding: 40px;
  color: #fff;
  display: flex;
  flex-direction: column;
  gap: 28px;
}

.hero-title {
  font-size: 28px;
  font-weight: 800;
  margin: 0 0 8px;
  line-height: 1.3;
  color: #ffffff;
}

.hero-accent {
  color: #90caf9;
}

.hero-desc {
  font-size: 15px;
  opacity: 0.85;
  margin: 0;
}

.search-bar {
  display: flex;
  gap: 8px;
  margin-top: 16px;
  flex-wrap: wrap;
}

.search-input {
  flex: 1;
  padding: 12px 18px;
  border-radius: 10px;
  border: none;
  font-size: 15px;
  outline: none;
  background: rgba(255, 255, 255, 0.95);
  color: #1a1a2e;

  &::placeholder {
    color: #aaa;
  }
}

.search-btn {
  padding: 12px 22px;
  background: #fbcb00;
  color: #fff;
  border: none;
  border-radius: 10px;
  font-size: 15px;
  font-weight: 700;
  cursor: pointer;
  white-space: nowrap;
  transition: background 0.15s;
  display: inline-flex;
  align-items: center;
  gap: 8px;

  &:hover {
    background: #e65100;
  }
}

.search-links {
  margin-top: 8px;
}

.link-btn {
  background: none;
  border: none;
  color: rgba(255, 255, 255, 0.75);
  font-size: 13px;
  cursor: pointer;
  padding: 0;
  display: inline-flex;
  gap: 4px;
  align-items: center;

  &:hover {
    color: #fff;
    text-decoration: underline;
  }
}

.hero-stats {
  display: flex;
  gap: 32px;
  border-top: 1px solid rgba(255, 255, 255, 0.2);
  padding-top: 20px;
}

.hero-stat-num {
  font-size: 24px;
  font-weight: 800;
}

.hero-stat-label {
  font-size: 12px;
  opacity: 0.7;
  margin-top: 2px;
}

// Quick filters
.quick-filters {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.filter-pill {
  padding: 8px 18px;
  background: #fff;
  border: 1.5px solid #e0e0e0;
  border-radius: 99px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s;
  color: #333;
  display: flex;
  gap: 8px;
  align-items: center;

  &:hover {
    border-color: #3949ab;
    color: #3949ab;
    background: #f0f4ff;
  }
}

// Sections
.book-section {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.section-title {
  font-size: 18px;
  font-weight: 800;
  margin: 0;
  display: flex;
  gap: 8px;
  align-items: center;

  .title_icon {
    color: #c9ba0a;
  }
}

.section-sub {
  font-size: 13px;
  color: #888;
}

.btn-view-all {
  background: none;
  border: none;
  color: #3949ab;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  padding: 0;
  display: flex;
  align-items: center;
  gap: 4px;

  &:hover {
    text-decoration: underline;
  }
}

.section-loading {
  color: #aaa;
  font-size: 14px;
  padding: 20px 0;
}

// Book list (vertical list)
.book-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

// Horizontal scroll for new arrivals
.book-scroll {
  overflow-x: auto;
  padding-bottom: 8px;
}

.book-scroll-inner {
  display: flex;
  gap: 14px;
  width: max-content;
  padding: 4px 2px;
}

.book-thumb-card {
  width: 120px;
  cursor: pointer;
  transition: transform 0.15s;

  &:hover {
    transform: translateY(-3px);
  }
}

.thumb-cover {
  position: relative;
  width: 120px;
  height: 160px;
  margin-bottom: 8px;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    border-radius: 8px;
    border: 1px solid #e0e0e0;
    font-size: 12px;
    overflow: hidden;
    max-width: 120px;
    max-height: 160px;
    min-height: 160px;
  }
}

.thumb-placeholder {
  width: 100%;
  height: 100%;
  background: linear-gradient(135deg, #e8eaf6, #c5cae9);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 32px;
}

.thumb-available {
  position: absolute;
  top: 6px;
  right: 6px;
  background: #2e7d32;
  color: #fff;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 11px;
  font-weight: 700;
}

.thumb-title {
  font-size: 12px;
  font-weight: 600;
  color: #1a1a2e;
  line-height: 1.3;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.thumb-author {
  font-size: 11px;
  color: #888;
  margin-top: 2px;
}
</style>