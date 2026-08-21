<template>
  <div class="notif-wrapper" ref="wrapperRef">

    <!-- Bell button -->
    <button class="bell-btn" @click="toggleDropdown" :class="{ active: showDropdown }">
      <svg class="bell-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"
        stroke-linecap="round" stroke-linejoin="round">
        <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
        <path d="M13.73 21a2 2 0 0 1-3.46 0" />
      </svg>
      <span class="badge" v-if="unreadCount > 0">
        {{ unreadCount > 99 ? '99+' : unreadCount }}
      </span>
    </button>

    <!-- Dropdown -->
    <Teleport to="body">
      <div v-if="showDropdown" class="notif-dropdown" :style="dropdownStyle" ref="dropdownRef">
        <!-- Header -->
        <div class="dropdown-header">
          <span class="dropdown-title">Thông báo</span>
          <div class="dropdown-actions">
            <button v-if="unreadCount > 0" class="btn-text" @click="markAllRead">
              Đánh dấu tất cả đã đọc
            </button>
            <button class="btn-text btn-clear" @click="clearRead" v-if="hasRead">
              Xoá đã đọc
            </button>
          </div>
        </div>

        <!-- Filter tabs -->
        <div class="filter-tabs">
          <button :class="{ active: filterUnread === false }" @click="filterUnread = false; fetchNotifs()">
            Tất cả
          </button>
          <button :class="{ active: filterUnread === true }" @click="filterUnread = true; fetchNotifs()">
            Chưa đọc
            <span class="tab-badge" v-if="unreadCount > 0">{{ unreadCount }}</span>
          </button>
        </div>

        <!-- List -->
        <div class="notif-list" ref="listRef" @scroll="onScroll">
          <div v-if="isLoading && items.length === 0" class="notif-loading">Đang tải...</div>

          <div v-else-if="items.length === 0" class="notif-empty">
            <span>🔕</span>
            <span>Không có thông báo nào</span>
          </div>

          <div v-for="notif in items" :key="notif.notificationId" class="notif-item" :class="{ unread: !notif.isRead }"
            @click="handleNotifClick(notif)">
            <div class="notif-icon">{{ typeIcon(notif.type) }}</div>
            <div class="notif-content">
              <div class="notif-title">{{ notif.title }}</div>
              <div class="notif-message" v-if="notif.message">{{ notif.message }}</div>
              <div class="notif-time">{{ formatRelative(notif.createdAt) }}</div>
            </div>
            <div class="notif-dot" v-if="!notif.isRead"></div>
            <button class="btn-delete" @click.stop="deleteNotif(notif.notificationId)" title="Xóa">✕</button>
          </div>

          <!-- Load more -->
          <div v-if="isLoadingMore" class="notif-loading-more">Đang tải thêm...</div>
        </div>

        <!-- Footer -->
        <div class="dropdown-footer">
          <button class="btn-view-all" @click="viewAll">Xem tất cả thông báo</button>
        </div>
      </div>
    </Teleport>

  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import api from '../../services/api'

const props = defineProps({
  isAdmin: { type: Boolean, default: false }
})

const router = useRouter()

// State
const showDropdown = ref(false)
const items = ref([])
const unreadCount = ref(0)
const isLoading = ref(false)
const isLoadingMore = ref(false)
const filterUnread = ref(false)
const currentPage = ref(1)
const totalPages = ref(1)
const wrapperRef = ref(null)
const dropdownRef = ref(null)
const listRef = ref(null)
const dropdownStyle = ref({})

let pollInterval = null

// Computed
const hasRead = computed(() => items.value.some(n => n.isRead))

// Lifecycle
onMounted(() => {
  fetchUnreadCount()
  // Poll unread count mỗi 30s
  pollInterval = setInterval(fetchUnreadCount, 60000)
  document.addEventListener('click', handleOutsideClick)
})

onUnmounted(() => {
  clearInterval(pollInterval)
  document.removeEventListener('click', handleOutsideClick)
})

// Fetch
const fetchUnreadCount = async () => {
  try {
    const res = await api.get('/Notifications/unread-count')
    if (res.status === 200) unreadCount.value = res.data.count
  } catch { }
}

const fetchNotifs = async (page = 1, append = false) => {
  if (page === 1) isLoading.value = true
  else isLoadingMore.value = true

  try {
    const params = new URLSearchParams({ page, pageSize: 15 })
    if (filterUnread.value) params.append('unreadOnly', 'true')

    const res = await api.get(`/Notifications?${params}`)
    if (res.status === 200) {
      if (append) items.value = [...items.value, ...res.data.items]
      else items.value = res.data.items

      totalPages.value = res.data.totalPages
      currentPage.value = page
      unreadCount.value = res.data.unreadCount
    }
  } catch { }
  finally {
    isLoading.value = false
    isLoadingMore.value = false
  }
}

// Toggle dropdown
const toggleDropdown = () => {
  showDropdown.value = !showDropdown.value
  if (showDropdown.value) {
    fetchNotifs()
    updateDropdownPosition()
  }
}

const updateDropdownPosition = () => {
  if (!wrapperRef.value) return
  const rect = wrapperRef.value.getBoundingClientRect()
  dropdownStyle.value = {
    position: 'fixed',
    top: rect.bottom + 8 + 'px',
    right: window.innerWidth - rect.right + 'px',
    zIndex: 9999
  }
}

// Outside click
const handleOutsideClick = (e) => {
  if (showDropdown.value &&
    !wrapperRef.value?.contains(e.target) &&
    !dropdownRef.value?.contains(e.target)) {
    showDropdown.value = false
  }
}

// Scroll to load more
const onScroll = () => {
  const el = listRef.value
  if (!el || isLoadingMore.value || currentPage.value >= totalPages.value) return
  if (el.scrollTop + el.clientHeight >= el.scrollHeight - 20) {
    fetchNotifs(currentPage.value + 1, true)
  }
}

// Actions
const handleNotifClick = async (notif) => {
  if (!notif.isRead) {
    await markRead(notif.notificationId)
    notif.isRead = true
    unreadCount.value = Math.max(0, unreadCount.value - 1)
  }
  showDropdown.value = false
  if (notif.link) router.push(props.isAdmin ? notif.link : `/user${notif.link}`)
}

const markRead = async (id) => {
  try { await api.patch(`/Notifications/${id}/read`) } catch { }
}

const markAllRead = async () => {
  try {
    await api.patch('/Notifications/read-all')
    items.value.forEach(n => { n.isRead = true })
    unreadCount.value = 0
  } catch { }
}

const deleteNotif = async (id) => {
  try {
    await api.delete(`/Notifications/${id}`)
    items.value = items.value.filter(n => n.notificationId !== id)
  } catch { }
}

const clearRead = async () => {
  try {
    await api.delete('/Notifications/clear')
    items.value = items.value.filter(n => !n.isRead)
  } catch { }
}

const viewAll = () => {
  showDropdown.value = false
  router.push(props.isAdmin ? '/admin/notifications' : '/user/notifications')
}

// Helpers
const typeIcon = (type) => {
  const map = {
    BorrowApproved: '✅', BorrowRejected: '❌', DueSoon: '⏰',
    Overdue: '⚠️', FineCreated: '💰', FineWaived: '🎉',
    ExtendSuccess: '📅', NewRequest: '📋', System: '📢'
  }
  return map[type] ?? '🔔'
}

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
.notif-wrapper {
  position: relative;
}

// Bell button
.bell-btn {
  position: relative;
  background: none;
  border: none;
  cursor: pointer;
  padding: 8px;
  border-radius: 8px;
  transition: background 0.15s;
  display: flex;
  align-items: center;
  width: 39px;

  &:hover,
  &.active {
    background: #f0f0f0;
  }
}

.bell-icon {
  font-size: 20px;
  color: #6b7280;
  width: 20px;
}

.badge {
  position: absolute;
  top: 2px;
  right: 2px;
  background: #e53935;
  color: #fff;
  font-size: 10px;
  font-weight: 800;
  min-width: 16px;
  height: 16px;
  border-radius: 99px;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 3px;
  border: 1.5px solid #fff;
}

// Dropdown
.notif-dropdown {
  width: 380px;
  background: #fff;
  border-radius: 14px;
  border: 1px solid #e0e0e0;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.14);
  overflow: hidden;
  display: flex;
  flex-direction: column;
  max-height: 520px;

  @media (max-width: 480px) {
    width: calc(100vw - 16px);
    right: 8px !important;
  }
}

.dropdown-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 16px 10px;
  border-bottom: 1px solid #f0f0f0;
  flex-shrink: 0;
}

.dropdown-title {
  font-size: 15px;
  font-weight: 800;
  color: #1a1a2e;
}

.dropdown-actions {
  display: flex;
  gap: 8px;
  align-items: center;
}

.btn-text {
  background: none;
  border: none;
  color: #3949ab;
  font-size: 12px;
  cursor: pointer;
  padding: 2px 4px;
  border-radius: 4px;

  &:hover {
    background: #e8eaf6;
  }

  &.btn-clear {
    color: #888;

    &:hover {
      background: #f5f5f5;
    }
  }
}

// Filter tabs
.filter-tabs {
  display: flex;
  padding: 8px 16px 0;
  gap: 4px;
  flex-shrink: 0;

  button {
    padding: 6px 12px;
    background: none;
    border: none;
    border-radius: 8px;
    font-size: 13px;
    font-weight: 500;
    color: #888;
    cursor: pointer;
    transition: all 0.15s;
    display: flex;
    align-items: center;
    gap: 6px;

    &:hover {
      background: #f5f5f5;
      color: #333;
    }

    &.active {
      background: #e8eaf6;
      color: #3949ab;
      font-weight: 700;
    }
  }
}

.tab-badge {
  background: #e53935;
  color: #fff;
  font-size: 10px;
  font-weight: 700;
  padding: 1px 5px;
  border-radius: 99px;
}

// List
.notif-list {
  flex: 1;
  overflow-y: auto;
  padding: 8px 0;
}

.notif-loading {
  padding: 20px;
  text-align: center;
  color: #aaa;
  font-size: 13px;
}

.notif-loading-more {
  padding: 10px;
  text-align: center;
  color: #aaa;
  font-size: 12px;
}

.notif-empty {
  padding: 32px 20px;
  text-align: center;
  color: #aaa;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;

  span:first-child {
    font-size: 28px;
  }

  span:last-child {
    font-size: 13px;
  }
}

.notif-item {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 10px 16px;
  cursor: pointer;
  transition: background 0.15s;
  position: relative;

  &:hover {
    background: #fafafa;

    .btn-delete {
      opacity: 1;
    }
  }

  &.unread {
    background: #f5f7ff;
  }
}

.notif-icon {
  font-size: 20px;
  flex-shrink: 0;
  margin-top: 1px;
}

.notif-content {
  flex: 1;
  min-width: 0;
  padding-right: 20px;
}

.notif-title {
  font-size: 13px;
  font-weight: 600;
  color: #1a1a2e;
  line-height: 1.3;
  margin-bottom: 3px;
}

.notif-message {
  font-size: 12px;
  color: #666;
  line-height: 1.4;
  margin-bottom: 4px;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.notif-time {
  font-size: 11px;
  color: #aaa;
}

.notif-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #3949ab;
  flex-shrink: 0;
  margin-top: 6px;
}

.btn-delete {
  position: absolute;
  top: 8px;
  right: 8px;
  background: none;
  border: none;
  color: #ccc;
  cursor: pointer;
  font-size: 12px;
  padding: 2px 4px;
  border-radius: 4px;
  opacity: 0;
  transition: opacity 0.15s;

  &:hover {
    color: #c62828;
    background: #ffebee;
  }
}

// Footer
.dropdown-footer {
  padding: 8px 16px 12px;
  border-top: 1px solid #f0f0f0;
  flex-shrink: 0;
}

.btn-view-all {
  width: 100%;
  padding: 8px;
  background: #f5f5f5;
  border: none;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 500;
  color: #3949ab;
  cursor: pointer;
  transition: background 0.15s;

  &:hover {
    background: #e8eaf6;
  }
}
</style>