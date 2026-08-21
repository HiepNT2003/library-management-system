<template>
  <div class="notification-page">

    <div class="page-header">
      <div>
        <h1 class="page-title">Thông báo</h1>
        <p class="page-desc">Tất cả thông báo của bạn</p>
      </div>
      <div class="header-actions">
        <button class="btn btn-outline" @click="markAllRead" v-if="unreadCount > 0">
          ✓ Đánh dấu tất cả đã đọc
        </button>
        <button class="btn btn-outline btn-clear" @click="clearRead" v-if="hasRead">
          <Icon icon="mynaui:trash" width="24" height="24" /> Xoá đã đọc
        </button>
      </div>
    </div>

    <!-- Filter -->
    <div class="filter-bar">
      <button
        v-for="f in filters"
        :key="f.type"
        class="filter-pill"
        :class="{ active: activeType === f.type }"
        @click="setType(f.type)"
      >
        {{ f.icon }} {{ f.label }}
      </button>
    </div>

    <!-- Unread only toggle -->
    <div class="toolbar">
      <label class="toggle-label">
        <input type="checkbox" v-model="unreadOnly" @change="fetchData(1)" />
        Chỉ xem chưa đọc
        <span class="unread-badge" v-if="unreadCount > 0">{{ unreadCount }}</span>
      </label>
    </div>

    <div v-if="isLoading" class="state-box">Đang tải...</div>

    <div v-else-if="items.length === 0" class="empty-state">
      <div class="empty-icon">🔕</div>
      <div class="empty-title">Không có thông báo nào</div>
    </div>

    <div v-else class="notif-list">
      <div
        v-for="notif in items"
        :key="notif.notificationId"
        class="notif-item"
        :class="{ unread: !notif.isRead }"
        @click="handleClick(notif)"
      >
        <div class="notif-icon">{{ typeIcon(notif.type) }}</div>
        <div class="notif-content">
          <div class="notif-title">{{ notif.title }}</div>
          <div class="notif-message" v-if="notif.message">{{ notif.message }}</div>
          <div class="notif-time">{{ formatRelative(notif.createdAt) }}</div>
        </div>
        <div class="notif-actions">
          <div class="unread-dot" v-if="!notif.isRead"></div>
          <button class="btn-delete" @click.stop="deleteNotif(notif)" title="Xóa"> <Icon icon="mynaui:trash" width="24" height="24" /></button>
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
      <span class="page-info">{{ total }} thông báo</span>
    </div>

  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import api from '../services/api'
import { Icon } from "@iconify/vue";

const router = useRouter()

const items        = ref([])
const isLoading    = ref(false)
const total        = ref(0)
const totalPages   = ref(1)
const currentPage  = ref(1)
const pageSize     = 20
const unreadCount  = ref(0)
const unreadOnly   = ref(false)
const activeType   = ref('')

const filters = [
  { type: '',               icon: '🔔', label: 'Tất cả'       },
  { type: 'BorrowApproved', icon: '✅', label: 'Được duyệt'   },
  { type: 'BorrowRejected', icon: '❌', label: 'Từ chối'      },
  { type: 'DueSoon',        icon: '⏰', label: 'Sắp đến hạn' },
  { type: 'Overdue',        icon: '⚠️', label: 'Quá hạn'     },
  { type: 'FineCreated',    icon: '💰', label: 'Phiếu phạt'  },
  { type: 'NewRequest',     icon: '📋', label: 'Yêu cầu mới' },
]

const hasRead = computed(() => items.value.some(n => n.isRead))

onMounted(() => fetchData())

const fetchData = async (page = 1) => {
  isLoading.value = true
  try {
    const params = new URLSearchParams({ page, pageSize })
    if (unreadOnly.value)        params.append('unreadOnly', 'true')
    if (activeType.value)        params.append('type', activeType.value)

    const res = await api.get(`/Notifications?${params}`)
    if (res.status === 200) {
      items.value       = res.data.items
      total.value       = res.data.total
      totalPages.value  = res.data.totalPages
      currentPage.value = res.data.page
      unreadCount.value = res.data.unreadCount
    }
  } catch {}
  finally { isLoading.value = false }
}

const setType = (type) => {
  activeType.value = activeType.value === type ? '' : type
  fetchData(1)
}

const goToPage = (page) => {
  if (page >= 1 && page <= totalPages.value) fetchData(page)
}

const handleClick = async (notif) => {
  if (!notif.isRead) {
    try {
      await api.patch(`/Notifications/${notif.notificationId}/read`)
      notif.isRead = true
      unreadCount.value = Math.max(0, unreadCount.value - 1)
    } catch {}
  }
  if (notif.link) router.push(`/user${notif.link}`)
}

const deleteNotif = async (notif) => {
  try {
    await api.delete(`/Notifications/${notif.notificationId}`)
    items.value = items.value.filter(n => n.notificationId !== notif.notificationId)
    total.value--
  } catch {}
}

const markAllRead = async () => {
  try {
    await api.patch('/Notifications/read-all')
    items.value.forEach(n => { n.isRead = true })
    unreadCount.value = 0
  } catch {}
}

const clearRead = async () => {
  try {
    await api.delete('/Notifications/clear')
    items.value = items.value.filter(n => !n.isRead)
    await fetchData(1)
  } catch {}
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
const typeIcon = (type) => {
  const map = {
    BorrowApproved: '✅', BorrowRejected: '❌', DueSoon:     '⏰',
    Overdue:        '⚠️', FineCreated:   '💰', FineWaived:  '🎉',
    ExtendSuccess:  '📅', NewRequest:    '📋', System:      '📢'
  }
  return map[type] ?? '🔔'
}

const formatRelative = (d) => {
  if (!d) return ''
  const diff  = Date.now() - new Date(d).getTime()
  const mins  = Math.floor(diff / 60000)
  const hours = Math.floor(diff / 3600000)
  const days  = Math.floor(diff / 86400000)
  if (mins < 1)   return 'Vừa xong'
  if (mins < 60)  return `${mins} phút trước`
  if (hours < 24) return `${hours} giờ trước`
  if (days < 7)   return `${days} ngày trước`
  return new Date(d).toLocaleDateString('vi-VN')
}
</script>

<style lang="scss" scoped>
.notification-page {
  display: flex; flex-direction: column; gap: 16px;
  font-family: 'Segoe UI', sans-serif; color: #1a1a2e;
}
.page-header {
  display: flex; align-items: flex-start; justify-content: space-between; gap: 12px; flex-wrap: wrap;
}
.page-title  { font-size: 22px; font-weight: 800; margin: 0 0 4px; }
.page-desc   { font-size: 14px; color: #666; margin: 0; }
.header-actions { display: flex; gap: 8px; flex-wrap: wrap; }

// Filter
.filter-bar { display: flex; gap: 8px; flex-wrap: wrap; }
.filter-pill {
  padding: 6px 14px; background: #fff; border: 1.5px solid #e0e0e0;
  border-radius: 99px; font-size: 13px; font-weight: 500; cursor: pointer;
  transition: all 0.15s; color: #555;
  &:hover  { border-color: #3949ab; color: #3949ab; }
  &.active { background: #e8eaf6; border-color: #3949ab; color: #3949ab; font-weight: 700; }
}

.toolbar { display: flex; align-items: center; }
.toggle-label {
  display: flex; align-items: center; gap: 8px; font-size: 14px; cursor: pointer;
  input { cursor: pointer; }
}
.unread-badge {
  background: #e53935; color: #fff; font-size: 11px; font-weight: 700;
  padding: 1px 7px; border-radius: 99px;
}

// Notification list
.notif-list { display: flex; flex-direction: column; gap: 8px; }
.notif-item {
  display: flex; align-items: flex-start; gap: 14px;
  background: #fff; border-radius: 12px; border: 1.5px solid #e0e0e0;
  padding: 14px 16px; cursor: pointer; transition: all 0.15s;
  &:hover { border-color: #c5cae9; box-shadow: 0 2px 8px rgba(0,0,0,0.06); }
  &.unread { background: #f5f7ff; border-color: #c5cae9; }
}
.notif-icon    { font-size: 22px; flex-shrink: 0; margin-top: 1px; }
.notif-content { flex: 1; min-width: 0; }
.notif-title   { font-size: 14px; font-weight: 700; color: #1a1a2e; margin-bottom: 4px; line-height: 1.3; }
.notif-message { font-size: 13px; color: #555; margin-bottom: 6px; line-height: 1.5; }
.notif-time    { font-size: 12px; color: #aaa; }

.notif-actions { display: flex; flex-direction: column; align-items: center; gap: 8px; flex-shrink: 0; }
.unread-dot {
  width: 8px; height: 8px; border-radius: 50%; background: #3949ab; flex-shrink: 0;
}
.btn-delete {
  background: none; border: none; cursor: pointer; font-size: 14px; padding: 4px;
  border-radius: 6px; color: #a19f9f; transition: all 0.15s;
  &:hover { color: #c62828; background: #ffebee; }
}

// Empty
.empty-state {
  text-align: center; padding: 60px 20px;
  display: flex; flex-direction: column; align-items: center; gap: 10px;
}
.empty-icon  { font-size: 52px; }
.empty-title { font-size: 16px; font-weight: 700; color: #333; }

.state-box { padding: 40px; text-align: center; color: #888; }

// Buttons
.btn {
  display: inline-flex; align-items: center; gap: 6px; padding: 8px 16px;
  border-radius: 8px; font-size: 13px; font-weight: 500; cursor: pointer; border: none; transition: all 0.15s;
  &.btn-outline {
    background: #fff; color: #3949ab; border: 1.5px solid #3949ab;
    &:hover { background: #e8eaf6; }
    &.btn-clear { color: #888; border-color: #e0e0e0; &:hover { background: #f5f5f5; } }
  }
}

// Pagination
.pagination { display: flex; align-items: center; gap: 4px; justify-content: center; }
.page-btn {
  min-width: 34px; height: 34px; padding: 0 8px; border: 1.5px solid #e0e0e0;
  border-radius: 8px; background: #fff; font-size: 14px; cursor: pointer; color: #333; transition: all 0.15s;
  &:hover:not(:disabled) { border-color: #3949ab; color: #3949ab; }
  &.active { background: #3949ab; border-color: #3949ab; color: #fff; font-weight: 700; }
  &:disabled { opacity: 0.4; cursor: not-allowed; }
}
.page-dots { padding: 0 4px; color: #aaa; }
.page-info { margin-left: 8px; font-size: 13px; color: #888; }
</style>