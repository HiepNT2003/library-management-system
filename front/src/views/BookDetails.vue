<template>
  <div class="book-detail">
    <div v-if="isLoading" class="state-box">Đang tải...</div>
    <div v-else-if="loadError" class="state-box state-error">{{ loadError }}</div>

    <template v-else-if="book">
      <!-- Breadcrumb -->
      <div class="breadcrumb">
        <router-link :to="isPublicPage ? '' : '/user'">Trang chủ</router-link>
        <span>›</span>
        <router-link :to="`${isPublicPage ? '' : '/user'}/search`">Tìm kiếm</router-link>
        <span>›</span>
        <span class="breadcrumb-current">{{ truncate(book.title, 40) }}</span>
      </div>

      <!-- Main info -->
      <div class="book-main">
        <!-- Cover -->
        <div class="book-cover-section">
          <div class="book-cover">
            <img v-if="book.imageUrl" :src="book.imageUrl" :alt="book.title" />
            <div v-else class="cover-placeholder">
              <span>📖</span>
            </div>
          </div>

          <!-- Action buttons -->
          <div class="book-actions">
            <button v-if="canRead" class="btn btn-primary btn-full" @click="viewEbook = true">
              📖 Đọc online
            </button>
            <button
              v-if="canDownload"
              class="btn btn-download btn-full"
              @click="downloadBook"
              :disabled="isDownloading"
            >
              {{ isDownloading ? "⏳ Đang tải..." : "⬇ Tải về PDF" }}
            </button>

            <div
              class="login-hint"
              v-else-if="book?.filePath && book?.documentTypeId === 4 && !authStore.user"
            >
              <router-link to="/login">Đăng nhập</router-link> để tải tài liệu
            </div>
            <button
              v-else-if="
                book.isBorrowable && (book.documentTypeId === 1 || book.documentTypeId === 3)
              "
              class="btn btn-primary btn-full"
              :disabled="book.availableCopies === 0 || hasPendingRequest"
              @click="openBorrowModal"
            >
              {{
                hasPendingRequest
                  ? "✓ Đã gửi yêu cầu"
                  : book.availableCopies > 0
                  ? "📚 Đặt mượn"
                  : "📚 Hết bản sao"
              }}
            </button>

            <button
              class="btn btn-full"
              :class="isFavorite ? 'btn-fav-active' : 'btn-fav'"
              @click="toggleFavorite"
              :disabled="!authStore.user"
            >
              {{ isFavorite ? "❤️ Đã yêu thích" : "🤍 Yêu thích" }}
            </button>
          </div>

          <!-- Availability -->
          <div
            class="availability-card"
            v-if="book.documentTypeId == 1 || book.documentTypeId == 3"
          >
            <div class="avail-title">Tình trạng</div>
            <div class="avail-row">
              <span>Có sẵn</span>
              <span class="avail-num" :class="book.availableCopies > 0 ? 'text-green' : 'text-red'">
                {{ book.availableCopies }} / {{ book.totalCopies }}
              </span>
            </div>
          </div>
        </div>

        <!-- Info -->
        <div class="book-info-section">
          <!-- Type tag -->
          <div class="type-tag" :class="typeClass">{{ typeLabel }}</div>

          <!-- Title -->
          <h1 class="book-title">{{ book.title }}</h1>

          <!-- Authors -->
          <div class="book-authors" v-if="book.authors?.length">
            <span
              v-for="author in book.authors"
              :key="author.authorId"
              class="author-link"
              @click="
                $router.push(`${isPublicPage ? '' : '/user'}/search?authorId=${author.authorId}`)
              "
            >
              {{ author.name }}
            </span>
          </div>

          <!-- Meta row -->
          <div class="meta-row">
            <div class="meta-item" v-if="book.publishedYear">
              <span class="meta-label">Năm XB</span>
              <span class="meta-value">{{ book.publishedYear }}</span>
            </div>
            <div class="meta-item" v-if="book.publisher">
              <span class="meta-label">NXB</span>
              <span class="meta-value">{{ book.publisher }}</span>
            </div>
            <div class="meta-item" v-if="book.totalPages">
              <span class="meta-label">Số trang</span>
              <span class="meta-value">{{ book.totalPages }}</span>
            </div>
            <div class="meta-item" v-if="book.isbn">
              <span class="meta-label">ISBN</span>
              <span class="meta-value code-text">{{ book.isbn }}</span>
            </div>
            <div class="meta-item" v-if="book.ddc">
              <span class="meta-label">DDC</span>
              <span class="meta-value code-text">{{ book.ddc.code }}</span>
            </div>
          </div>

          <!-- Categories + Languages -->
          <div class="tags-row" v-if="book.categories?.length || book.languages?.length">
            <span
              v-for="cat in book.categories"
              :key="cat.categoryId"
              class="tag tag-blue"
              @click="
                $router.push(`${isPublicPage ? '' : '/user'}/search?categoryId=${cat.categoryId}`)
              "
            >
              {{ cat.name }}
            </span>
            <span v-for="lang in book.languages" :key="lang.languageId" class="tag tag-gray">
              {{ lang.name }}
            </span>
          </div>

          <!-- Book specific -->
          <div class="article-info" v-if="book.documentTypeId === 1">
            <div class="info-row" v-if="book.price">
              <span>Giá:</span> <strong>{{ book.price }} VNĐ</strong>
            </div>
          </div>

          <!-- Article specific -->
          <div class="article-info" v-if="book.documentTypeId === 2">
            <div class="info-row" v-if="book.source">
              <span>Nguồn:</span> <strong>{{ book.source }}</strong>
            </div>
            <div class="info-row" v-if="book.startPage || book.endPage">
              <span>Trang:</span> <strong>{{ book.startPage }} ~ {{ book.endPage }}</strong>
            </div>
          </div>

          <!-- Ebook specific -->
          <div class="article-info" v-if="book.documentTypeId === 4">
            <div class="info-row" v-if="book.downloadCount">
              <span>Lượt download:</span> <strong>{{ book.downloadCount }}</strong>
            </div>
          </div>

          <!-- Thesis specific -->
          <div class="thesis-info" v-if="book.documentTypeId === 3">
            <div class="info-row" v-if="book.university">
              <span>Trường:</span> <strong>{{ book.university }}</strong>
            </div>
            <div class="info-row" v-if="book.faculty">
              <span>Khoa:</span> <strong>{{ book.faculty }}</strong>
            </div>
            <div class="info-row" v-if="book.degree">
              <span>Bằng cấp:</span> <strong>{{ book.degree }}</strong>
            </div>
            <div class="info-row" v-if="book.defenseYear">
              <span>Năm bảo vệ:</span> <strong>{{ book.defenseYear }}</strong>
            </div>
          </div>

          <!-- Description -->
          <div class="book-description" v-if="book.description">
            <div class="desc-title">Giới thiệu</div>
            <div class="desc-text" :class="{ 'expanded-desc': descExpanded }">
              <QuillEditor
                v-model:content="book.description"
                :readOnly="true"
                contentType="html"
                theme="bubble"
              />
            </div>
            <button
              v-if="book.description.length > 300"
              class="btn-expand"
              @click="descExpanded = !descExpanded"
            >
              {{ descExpanded ? "Thu gọn ▲" : "Xem thêm ▼" }}
            </button>
          </div>

          <!-- Authors detail -->
          <div class="authors-detail" v-if="book.authors?.some((a) => a.bio)">
            <div class="desc-title">Về tác giả</div>
            <div
              v-for="author in book.authors.filter((a) => a.bio)"
              :key="author.authorId"
              class="author-card"
            >
              <img v-if="author.imageUrl" :src="author.imageUrl" class="author-img" />
              <div class="author-no-img" v-else>{{ author.name[0] }}</div>
              <div>
                <div class="author-name">{{ author.name }}</div>
                <div class="author-bio">{{ author.bio }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Copies table -->
      <div class="copies-section" v-if="bookCopies?.length">
        <h3 class="section-title">📋 Danh sách bản sao ({{ bookCopies.length }})</h3>
        <div class="table-wrapper">
          <table class="copies-table">
            <thead>
              <tr>
                <th>Barcode</th>
                <th>Kho / Vị trí</th>
                <th>Tình trạng</th>
                <th>Ghi chú</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="copy in bookCopies" :key="copy.copyId">
                <td>
                  <span class="code-text">{{ copy.barcode }}</span>
                </td>
                <td>
                  <div>{{ copy.warehouseName || "—" }}</div>
                  <div v-if="copy.shelfLocation" class="shelf-loc">{{ copy.shelfLocation }}</div>
                </td>
                <td>
                  <span class="copy-status" :class="copyStatusClass(copy.status)">
                    {{ copyStatusLabel(copy.status) }}
                  </span>
                </td>
                <td>
                  <span v-if="copy.isReferenceOnly" class="ref-only">Chỉ đọc tại chỗ</span>
                  <span v-else class="text-muted">—</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Recommendations -->
      <div class="recs-section" v-if="recommendations.length">
        <h3 class="section-title">💡 Sách liên quan</h3>
        <div class="recs-scroll">
          <div
            v-for="rec in recommendations"
            :key="rec.bookId"
            class="rec-card"
            @click="$router.push(`${isPublicPage ? '' : '/user'}/books/${rec.bookId}`)"
          >
            <div class="rec-cover">
              <img v-if="rec.imageUrl" :src="rec.imageUrl" />
              <div v-else class="rec-placeholder">📖</div>
            </div>
            <div>
              <div class="rec-title">{{ rec.title }}</div>
              <div class="rec-author">{{ rec.authors?.slice(0, 1).join("") }}</div>
            </div>
          </div>
        </div>
      </div>
    </template>
    <ModalBorrowRequest
      v-model="showBorrowModal"
      :book="borrowingBook"
      @success="onBorrowSuccess"
    />
    <EbookReader
      v-if="book && book.filePath && viewEbook"
      :book-id="book.bookId"
      :fileUrl="book.filePath"
      :book-title="book.title"
      :save-progress="isPublicPage ? false : true"
      :default-fullscreen="true"
      :show-change-fullscreen="false"
      @on:closePDF="viewEbook = false"
    />
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useAuthStore } from "@/stores/auth"
import api from "../services/api"
import ModalBorrowRequest from "../components/User/ModalBorrowRequest.vue"
import { QuillEditor } from "@vueup/vue-quill"
import { TOAST_MESSAGE_STATUS } from "../constants"
import { useToastMessageStore } from "../stores/toastMessage"
import EbookReader from "../components/share/EbookReader.vue"

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const book = ref(null)
const bookCopies = ref(null)
const isLoading = ref(false)
const loadError = ref("")
const isFavorite = ref(false)
const hasPendingRequest = ref(false)
const recommendations = ref([])
const descExpanded = ref(false)
const isDownloading = ref(false)

const showBorrowModal = ref(false)
const borrowingBook = ref(null)
const viewEbook = ref(false)

const isPublicPage = computed(() => route?.name?.includes("public"))
const canRead = computed(() => {
  if (!book.value?.filePath) return false
  if (book.value.isPublic) return true
  return !!authStore.user
})

const canDownload = computed(() => {
  if (!book.value?.filePath) return false
  if (!book.value.isPublic) return false
  return !!authStore.user
})

onMounted(fetchAll)
watch(() => route.params.id, fetchAll)

async function fetchAll() {
  const id = route.params.id
  if (!id) return

  authStore.setIsLoadingApi(true)
  isLoading.value = true
  loadError.value = ""
  try {
    const res = await api.get(`/Books/user/${id}`)
    if (res.status === 200) {
      book.value = res.data
      // Load thêm data song song
      await Promise.all([
        fetchRecommendations(id),
        fetchBookCopies(id),
        authStore.user ? checkFavorite(id) : Promise.resolve(),
        authStore.user ? checkPendingRequest(id) : Promise.resolve(),
      ])
    }
  } catch (err) {
    loadError.value =
      err.response?.status === 404 ? "Không tìm thấy sách" : "Không thể tải thông tin sách"
  } finally {
    isLoading.value = false
  }
  authStore.setIsLoadingApi(false)
}

const downloadBook = async () => {
  if (!book.value?.isPublic && !authStore.user) {
    router.push('/login')
    return
  }
  isDownloading.value = true
  try {
    const res = await api.get(`/upload/download/${book.value.bookId}`, {
      responseType: 'blob'
    })
    const url  = window.URL.createObjectURL(new Blob([res.data]))
    const link = document.createElement('a')
    link.href  = url
    link.setAttribute('download', `${book.value.title}.pdf`)
    document.body.appendChild(link)
    link.click()
    link.remove()
    window.URL.revokeObjectURL(url)
  } catch (err) {
    const msg = err.response?.data?.message || 'Tải thất bại'
    alert(msg)
  } finally {
    isDownloading.value = false
  }
}

const fetchRecommendations = async (id) => {
  try {
    const res = await api.get(`/Books/${id}/recommendations`)
    if (res.status === 200) recommendations.value = res.data
  } catch {}
}

const fetchBookCopies = async (id) => {
  if (book.documentTypeId == 2 || book.documentTypeId == 4) return
  try {
    const res = await api.get(`/BookCopies?bookId=${id}`)
    if (res.status === 200) bookCopies.value = res.data
  } catch {}
}

const checkFavorite = async (id) => {
  try {
    const res = await api.get(`/Favorites/${id}/favorite`)
    if (res.status === 200) isFavorite.value = res.data.isFavorite
  } catch {}
}

const checkPendingRequest = async (id) => {
  try {
    const res = await api.get(`/account/me/requests?bookId=${id}&status=Pending&pageSize=1`)
    if (res.status === 200) hasPendingRequest.value = res.data.total > 0
  } catch {}
}

const toggleFavorite = async () => {
  if (!authStore.user) {
    router.push("/login")
    return
  }
  const id = route.params.id
  try {
    if (isFavorite.value) {
      await api.delete(`/Favorites/${id}/favorite`)
      isFavorite.value = false
    } else {
      await api.post(`/Favorites/${id}/favorite`)
      isFavorite.value = true
    }
  } catch (err) {
    alert(err.response?.data?.message || "Thao tác thất bại")
  }
}

const openBorrowModal = () => {
  if (!authStore.user) {
    router.push("/login")
    return
  }
  borrowingBook.value = book.value
  showBorrowModal.value = true
}

const onBorrowSuccess = () => {
  const toasMessageStore = useToastMessageStore()
  hasPendingRequest.value = true
  toasMessageStore.showToastMessage(
    "Gửi yêu cầu mượn thành công!",
    TOAST_MESSAGE_STATUS.success,
    2000
  )
}

// Helpers
const typeLabel = computed(() => {
  const map = { 1: "Sách vật lý", 2: "Bài trích", 3: "Luận án", 4: "Ebook" }
  return map[book.value?.documentTypeId] ?? "Tài liệu"
})
const typeClass = computed(() => {
  const map = { 1: "type-book", 2: "type-article", 3: "type-thesis", 4: "type-ebook" }
  return map[book.value?.documentTypeId] ?? ""
})
const copyStatusLabel = (s) => {
  const map = {
    Available: "Có sẵn",
    Borrowed: "Đang mượn",
    Damaged: "Hư hỏng",
    Lost: "Mất",
    0: "Có sẵn",
    1: "Đang mượn",
    2: "Hư hỏng",
    3: "Mất",
  }
  return map[s] ?? s
}
const copyStatusClass = (s) => {
  const map = {
    Available: "cs-green",
    Borrowed: "cs-blue",
    Damaged: "cs-orange",
    Lost: "cs-red",
    0: "cs-green",
    1: "cs-blue",
    2: "cs-orange",
    3: "cs-red",
  }
  return map[s] ?? ""
}
const truncate = (str, len) => (!str ? "" : str.length > len ? str.slice(0, len) + "..." : str)
</script>

<style lang="scss" scoped>
.book-detail {
  display: flex;
  flex-direction: column;
  gap: 28px;
  color: #1a1a2e;
  max-width: 1100px;
}

.breadcrumb {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  color: #888;

  a {
    color: #3949ab;
    text-decoration: none;

    &:hover {
      text-decoration: underline;
    }
  }

  .breadcrumb-current {
    color: #333;
  }
}

// Main layout
.book-main {
  display: grid;
  grid-template-columns: 260px 1fr;
  gap: 32px;
  align-items: start;

  @media (max-width: 768px) {
    grid-template-columns: 1fr;
  }
}

// Cover
.book-cover-section {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.book-cover {
  img {
    width: 100%;
    aspect-ratio: 3/4;
    object-fit: cover;
    border-radius: 12px;
    border: 1px solid #e0e0e0;
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
    min-height: 200px;
    font-size: 14px;
    padding: 4px;
  }
}

.cover-placeholder {
  width: 100%;
  aspect-ratio: 3/4;
  background: linear-gradient(135deg, #e8eaf6, #c5cae9);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 56px;
}

.book-actions {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.btn-full {
  width: 100%;
  justify-content: center;
}

.btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 10px 18px;
  border-radius: 10px;
  font-size: 14px;
  font-weight: 600;
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

  &.btn-fav {
    background: #fff;
    color: #888;
    border: 1.5px solid #e0e0e0;

    &:hover:not(:disabled) {
      border-color: #e53935;
      color: #e53935;
      background: #fff8f8;
    }
  }

  &.btn-fav-active {
    background: #fff8f8;
    color: #e53935;
    border: 1.5px solid #ef9a9a;
  }
}

.btn-download {
  background: #fff;
  color: #3949ab;
  border: 1.5px solid #3949ab;
  &:hover:not(:disabled) {
    background: #e8eaf6;
  }
  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}
.login-hint {
  font-size: 13px;
  color: #888;
  text-align: center;
  padding: 8px;
  a {
    color: #3949ab;
    font-weight: 600;
    &:hover {
      text-decoration: underline;
    }
  }
}

.availability-card {
  background: #f9f9f9;
  border-radius: 10px;
  border: 1px solid #e0e0e0;
  padding: 14px;
}

.avail-title {
  font-size: 12px;
  font-weight: 700;
  color: #888;
  text-transform: uppercase;
  margin-bottom: 8px;
}

.avail-row {
  display: flex;
  justify-content: space-between;
  font-size: 14px;
}

.avail-num {
  font-weight: 700;

  &.text-green {
    color: #2e7d32;
  }

  &.text-red {
    color: #c62828;
  }
}

// Info section
.book-info-section {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.type-tag {
  display: inline-block;
  padding: 4px 12px;
  border-radius: 99px;
  font-size: 12px;
  font-weight: 700;
  width: fit-content;

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

.book-title {
  font-size: 26px;
  font-weight: 800;
  line-height: 1.3;
  margin: 0;
  color: #1a1a2e;
}

.book-authors {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.author-link {
  font-size: 15px;
  color: #3949ab;
  font-weight: 500;
  cursor: pointer;

  &:hover {
    text-decoration: underline;
  }
}

.meta-row {
  display: flex;
  gap: 20px;
  flex-wrap: wrap;
  padding: 14px 0;
  border-top: 1px solid #f0f0f0;
  border-bottom: 1px solid #f0f0f0;
}

.meta-item {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.meta-label {
  font-size: 11px;
  color: #aaa;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.meta-value {
  font-size: 14px;
  font-weight: 600;
}

.code-text {
  font-family: monospace;
  color: #3949ab;
  font-size: 13px;
}

.tags-row {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}

.tag {
  padding: 4px 12px;
  border-radius: 99px;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;

  &.tag-blue {
    background: #e8eaf6;
    color: #3949ab;

    &:hover {
      background: #c5cae9;
    }
  }

  &.tag-gray {
    background: #f5f5f5;
    color: #666;
  }
}

.thesis-info {
  background: #f5f6ff;
  border-radius: 10px;
  padding: 14px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.info-row {
  font-size: 13px;
  color: #555;

  span {
    color: #888;
    margin-right: 6px;
  }
}

.book-description {
}

.desc-title {
  font-size: 15px;
  font-weight: 700;
  margin-bottom: 8px;
}

.desc-text {
  font-size: 14px;
  color: #444;
  line-height: 1.7;
  overflow: hidden;
  max-height: 100px;
  transition: max-height 0.3s;

  &.expanded-desc {
    max-height: 1000px;
  }
}

.btn-expand {
  background: none;
  border: none;
  color: #3949ab;
  font-size: 13px;
  cursor: pointer;
  padding: 4px 0;
  margin-top: 4px;

  &:hover {
    text-decoration: underline;
  }
}

.authors-detail {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.author-card {
  display: flex;
  gap: 12px;
  align-items: flex-start;
}

.author-img {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  object-fit: cover;
  flex-shrink: 0;
  border: 2px solid #e0e0e0;
}

.author-no-img {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: #3949ab;
  color: #fff;
  font-size: 18px;
  font-weight: 700;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.author-name {
  font-size: 14px;
  font-weight: 700;
  margin-bottom: 4px;
}

.author-bio {
  font-size: 13px;
  color: #666;
  line-height: 1.5;
}

// Copies
.copies-section {
}

.section-title {
  font-size: 17px;
  font-weight: 800;
  margin: 0 0 14px;
}

.table-wrapper {
  border-radius: 10px;
  border: 1px solid #e0e0e0;
  max-height: 360px;
  overflow: auto;
}

.copies-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 14px;
  max-height: 352px;
  overflow: auto;

  thead tr {
    background: #f5f5f5;
    position: sticky;
    top: 0;
  }

  th {
    padding: 10px 14px;
    text-align: left;
    font-weight: 600;
    color: #555;
    border-bottom: 1px solid #e0e0e0;
  }

  td {
    padding: 10px 14px;
    border-bottom: 1px solid #f0f0f0;
    vertical-align: middle;
  }

  tr:last-child td {
    border-bottom: none;
  }
}

.shelf-loc {
  font-size: 12px;
  color: #888;
  margin-top: 2px;
}

.copy-status {
  display: inline-block;
  padding: 2px 10px;
  border-radius: 99px;
  font-size: 12px;
  font-weight: 600;

  &.cs-green {
    background: #e8f5e9;
    color: #2e7d32;
  }

  &.cs-blue {
    background: #e3f2fd;
    color: #1565c0;
  }

  &.cs-orange {
    background: #fff3e0;
    color: #e65100;
  }

  &.cs-red {
    background: #ffebee;
    color: #c62828;
  }
}

.ref-only {
  font-size: 12px;
  color: #888;
  font-style: italic;
}

.text-muted {
  color: #ccc;
}

// Recommendations
.recs-section {
}

.recs-scroll {
  overflow-x: auto;
  padding-bottom: 8px;
}

.recs-scroll > div {
  display: flex;
  gap: 14px;
  width: max-content;
}

.rec-card {
  width: 120px;
  cursor: pointer;
  transition: transform 0.15s;

  &:hover {
    transform: translateY(-3px);
  }
}

.rec-cover {
  width: 120px;
  height: 160px;
  margin-bottom: 8px;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    border-radius: 8px;
    border: 1px solid #e0e0e0;
  }
}

.rec-placeholder {
  width: 100%;
  height: 100%;
  background: linear-gradient(135deg, #e8eaf6, #c5cae9);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 32px;
}

.rec-title {
  font-size: 12px;
  font-weight: 600;
  color: #1a1a2e;
  line-height: 1.3;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.rec-author {
  font-size: 11px;
  color: #888;
  margin-top: 2px;
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
  width: 56px;
  height: 72px;
  object-fit: cover;
  border-radius: 6px;
  flex-shrink: 0;
  border: 1px solid #e0e0e0;
}

.borrow-title {
  font-size: 14px;
  font-weight: 700;
  line-height: 1.3;
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

.state-box {
  padding: 60px;
  text-align: center;
  color: #888;

  &.state-error {
    color: #c62828;
  }
}
</style>