<template>
  <div class="my-favorites">
    <div class="page-header">
      <div>
        <h1 class="page-title">Sách yêu thích</h1>
        <p class="page-desc">Danh sách sách bạn đã đánh dấu yêu thích</p>
      </div>
      <div class="total-count" v-if="total > 0">{{ total }} cuốn</div>
    </div>

    <div v-if="isLoading" class="state-box">Đang tải...</div>

    <div v-else-if="items.length === 0" class="empty-state">
      <div class="empty-icon">🤍</div>
      <div class="empty-title">Chưa có sách yêu thích</div>
      <div class="empty-sub">Bấm ❤️ trên trang chi tiết sách để thêm vào đây</div>
      <button class="btn btn-primary" @click="$router.push('/user/search')">🔍 Khám phá sách</button>
    </div>

    <div v-else class="book-list">
      <div v-for="item in items" :key="item.id" class="fav-card">
        <!-- Cover -->
        <div class="card-cover" @click="$router.push(`/user/books/${item.book.bookId}`)">
          <img v-if="item.book.imageUrl" :src="item.book.imageUrl" :alt="item.book.title" />
          <div v-else class="cover-placeholder">📖</div>
          <div class="avail-badge" v-if="item.book.availableCopies > 0">
            {{ item.book.availableCopies }} có sẵn
          </div>
          <div class="avail-badge badge-unavail" v-else-if="item.book.totalCopies > 0">
            Hết bản sao
          </div>
        </div>

        <!-- Info -->
        <div class="card-info">
          <div class="card-main">
            <div>
              <div class="type-tag" :class="typeClass(item.book.documentTypeId)">
                {{ typeLabel(item.book.documentTypeId) }}
              </div>
              <h3 class="card-title" @click="$router.push(`/user/books/${item.book.bookId}`)">
                {{ item.book.title }}
              </h3>
              <div class="card-authors" v-if="item.book.authors?.length">
                {{ item.book.authors.map((a) => a.name).join(", ") }}
              </div>
              <div class="card-meta">
                <span v-if="item.book.publishedYear">{{ item.book.publishedYear }}</span>
                <span v-if="item.book.publisher" class="separator">·</span>
                <span v-if="item.book.publisher">{{ truncate(item.book.publisher, 30) }}</span>
              </div>
              <div class="card-categories" v-if="item.book.categories?.length">
                <span
                  v-for="cat in item.book.categories.slice(0, 2)"
                  :key="cat.categoryId"
                  class="cat-tag"
                  >{{ cat.name }}</span
                >
              </div>
            </div>
            <div class="card-added">Đã thêm {{ formatDate(item.createdDate) }}</div>
          </div>

          <!-- Actions -->
          <div class="card-actions">
            <button
              class="btn btn-primary btn-sm"
              v-if="item.book.availableCopies > 0"
              @click="openBorrowModal(item.book)"
            >
              📚 Đặt mượn
            </button>
            <button
              class="btn btn-outline btn-sm"
              @click="$router.push(`/user/books/${item.book.bookId}`)"
            >
              <Icon icon="fluent:notebook-eye-20-filled" width="20" height="20" /> Xem chi tiết
            </button>
            <button
              class="btn btn-remove"
              @click="removeFavorite(item)"
              :disabled="isRemoving === item.id"
              title="Bỏ yêu thích"
            >
            <Icon v-if="!isRemoving" icon="icon-park-outline:unlike" width="20" height="20" />
              {{ isRemoving === item.id ? "..." : "" }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Pagination -->
    <div class="pagination" v-if="totalPages > 1">
      <button class="page-btn" :disabled="currentPage === 1" @click="goToPage(currentPage - 1)">
        ‹
      </button>
      <template v-for="p in visiblePages" :key="p">
        <span v-if="p === '...'" class="page-dots">...</span>
        <button v-else class="page-btn" :class="{ active: p === currentPage }" @click="goToPage(p)">
          {{ p }}
        </button>
      </template>
      <button
        class="page-btn"
        :disabled="currentPage === totalPages"
        @click="goToPage(currentPage + 1)"
      >
        ›
      </button>
      <span class="page-info">{{ total }} cuốn</span>
    </div>

    <!-- Borrow Modal -->
    <ModalBorrowRequest
      v-model="showBorrowModal"
      :book="borrowingBook"
      @success="onBorrowSuccess"
    />
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from "vue"
import { useRouter } from "vue-router"
import api from "../services/api"
import { useToastMessageStore } from "../stores/toastMessage"
import { TOAST_MESSAGE_STATUS } from "../constants"
import ModalBorrowRequest from "../components/User/ModalBorrowRequest.vue"
import { Icon } from "@iconify/vue"

const router = useRouter()

const items = ref([])
const isLoading = ref(false)
const total = ref(0)
const totalPages = ref(1)
const currentPage = ref(1)
const pageSize = 12
const isRemoving = ref(null)

const showBorrowModal = ref(false)
const borrowingBook = ref(null)
const isSubmitting = ref(false)
const borrowForm = reactive({ expectedDate: "", note: "" })
const today = new Date().toISOString().slice(0, 10)

onMounted(() => fetchData())

const fetchData = async (page = 1) => {
  isLoading.value = true
  try {
    const res = await api.get(`/account/me/favorites?page=${page}&pageSize=${pageSize}`)
    if (res.status === 200) {
      items.value = res.data.items
      total.value = res.data.total
      totalPages.value = res.data.totalPages
      currentPage.value = res.data.page
    }
  } catch {
  } finally {
    isLoading.value = false
  }
}

const removeFavorite = async (item) => {
  isRemoving.value = item.id
  try {
    const res = await api.delete(`/Favorites/${item.book.bookId}/favorite`)
    if (res.status === 200) {
      items.value = items.value.filter((i) => i.id !== item.id)
      total.value--
    }
  } catch (err) {
    alert(err.response?.data?.message || "Xoá thất bại")
  } finally {
    isRemoving.value = null
  }
}

const openBorrowModal = (book) => {
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

const goToPage = (page) => {
  if (page >= 1 && page <= totalPages.value) fetchData(page)
}

// Computed
const visiblePages = computed(() => {
  const pages = [],
    t = totalPages.value,
    cur = currentPage.value
  if (t <= 7) {
    for (let i = 1; i <= t; i++) pages.push(i)
  } else {
    pages.push(1)
    if (cur > 3) pages.push("...")
    for (let i = Math.max(2, cur - 1); i <= Math.min(t - 1, cur + 1); i++) pages.push(i)
    if (cur < t - 2) pages.push("...")
    pages.push(t)
  }
  return pages
})

// Helpers
const typeLabel = (id) =>
  ({ 1: "Sách", 2: "Bài trích", 3: "Luận án", 4: "Ebook" }[id] ?? "Tài liệu")
const typeClass = (id) =>
  ({ 1: "type-book", 2: "type-article", 3: "type-thesis", 4: "type-ebook" }[id] ?? "")
const formatDate = (d) => (d ? new Date(d).toLocaleDateString("vi-VN") : "—")
const truncate = (str, len) => (!str ? "" : str.length > len ? str.slice(0, len) + "..." : str)
</script>

<style lang="scss" scoped>
.my-favorites {
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

.total-count {
  font-size: 14px;
  font-weight: 600;
  color: #3949ab;
  background: #e8eaf6;
  padding: 4px 12px;
  border-radius: 99px;
}

// Book list
.book-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.fav-card {
  display: flex;
  gap: 16px;
  background: #fff;
  border-radius: 14px;
  border: 1.5px solid #e0e0e0;
  padding: 16px;
  transition: all 0.2s;

  &:hover {
    border-color: #c5cae9;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06);
  }
}

.card-cover {
  position: relative;
  flex-shrink: 0;
  cursor: pointer;
  height: fit-content;
  margin: auto 0;

  img,
  .cover-placeholder {
    width: 80px;
    height: 108px;
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
    font-size: 30px;
  }
}

.avail-badge {
  position: absolute;
  bottom: 4px;
  left: 0;
  right: 0;
  text-align: center;
  background: #2e7d32;
  color: #fff;
  font-size: 10px;
  font-weight: 700;
  padding: 2px 4px;
  border-radius: 0 0 8px 8px;

  &.badge-unavail {
    background: #757575;
  }
}

.card-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  gap: 10px;
  min-width: 0;
}

.card-main {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;
}

.type-tag {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 99px;
  font-size: 11px;
  font-weight: 700;
  margin-bottom: 6px;

  &.type-book {
    background: #e8eaf6;
    color: #3949ab;
  }

  &.type-article {
    background: #e0f2f1;
    color: #00695c;
  }

  &.type-thesis {
    background: #fce4ec;
    color: #c2185b;
  }

  &.type-ebook {
    background: #fff3e0;
    color: #e65100;
  }
}

.card-title {
  font-size: 16px;
  font-weight: 700;
  margin: 0 0 4px;
  cursor: pointer;
  line-height: 1.3;

  &:hover {
    color: #3949ab;
  }
}

.card-authors {
  font-size: 13px;
  color: #3949ab;
  font-weight: 500;
  margin-bottom: 4px;
}

.card-meta {
  font-size: 12px;
  color: #888;
  margin-bottom: 6px;

  .separator {
    margin: 0 4px;
  }
}

.card-categories {
  display: flex;
  gap: 4px;
  flex-wrap: wrap;
}

.cat-tag {
  font-size: 11px;
  padding: 2px 8px;
  background: #f5f5f5;
  color: #666;
  border-radius: 99px;
}

.card-added {
  font-size: 12px;
  color: #aaa;
  white-space: nowrap;
  flex-shrink: 0;
}

.card-actions {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-wrap: wrap;
}

.btn {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 7px 14px;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  border: none;
  transition: all 0.15s;

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  &.btn-sm {
    padding: 7px 14px;
    font-size: 13px;
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

.btn-remove {
  margin-left: auto;
  background: none;
  border: none;
  cursor: pointer;
  font-size: 16px;
  padding: 6px 8px;
  border-radius: 8px;
  color: #aaa;
  transition: all 0.15s;

  &:hover:not(:disabled) {
    background: #ffebee;
    color: #c62828;
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

.empty-sub {
  font-size: 14px;
  color: #888;
}

.state-box {
  padding: 40px;
  text-align: center;
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

.borrow-preview {
  display: flex;
  gap: 12px;
  align-items: center;
  padding: 12px;
  background: #f9f9f9;
  border-radius: 10px;
}

.borrow-img {
  width: 52px;
  height: 68px;
  object-fit: cover;
  border-radius: 6px;
  flex-shrink: 0;
}

.borrow-title {
  font-size: 14px;
  font-weight: 700;
  margin-bottom: 4px;
}

.borrow-author {
  font-size: 13px;
  color: #3949ab;
  margin-bottom: 4px;
}

.borrow-avail {
  font-size: 13px;
  color: #555;

  .text-green {
    color: #2e7d32;
  }
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;

  label {
    font-size: 13px;
    font-weight: 600;
    color: #444;
    display: flex;
    align-items: center;
    gap: 6px;
  }

  input,
  textarea {
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

  textarea {
    resize: vertical;
  }
}

.optional {
  font-size: 11px;
  color: #aaa;
  font-weight: 400;
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

.page-info {
  margin-left: 8px;
  font-size: 13px;
  color: #888;
}
</style>