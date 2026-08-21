<template>
  <div class="user-layout">

    <!-- Header -->
    <header class="header">
      <div class="header-left">
        <button class="sidebar-toggle" @click="sidebarOpen = !sidebarOpen">
          <span></span><span></span><span></span>
        </button>
        <router-link to="/" class="logo">
          <div class="logo-icon"><img src="../assets/Images/LogoUTC.png" alt="" srcset=""></div>
          <div class="logo-text">
            <div class="logo-name">Thư viện UTC</div>
            <div class="logo-sub">ĐH Giao thông Vận tải</div>
          </div>
        </router-link>
      </div>

      <div class="header-center">
        <div class="header-search">
          <input v-model="searchQuery" placeholder="Tìm sách, tác giả..." @keydown.enter="goSearch" />
          <button @click="goSearch">
            <Icon icon="ic:outline-search" width="18" height="18" />
          </button>
        </div>
      </div>

      <div class="header-right">
        <notification-bell :is-admin="false" />

        <!-- User avatar -->
        <div class="user-menu" @click="toggleUserMenu" ref="userMenuBtn">
          <div class="user-avatar">{{ initials }}</div>
          <div class="user-info">
            <div class="user-name">{{ userName || 'Bạn đọc' }}</div>
            <div class="user-code">{{ userCode }}</div>
          </div>
          <span class="chevron">▾</span>
        </div>

        <!-- User dropdown -->
        <div class="dropdown user-dropdown" v-if="showUserMenu" ref="userDropdown">
          <router-link to="/user/profile" class="dropdown-item" @click="showUserMenu = false">
            <Icon icon="gg:profile" width="20" height="20" /> Thông tin cá nhân
          </router-link>
          <div class="dropdown-divider"></div>
          <button class="dropdown-item text-red" @click="logout">
            <Icon icon="humbleicons:logout" width="20" height="20" /> Đăng xuất
          </button>
        </div>
      </div>
    </header>

    <div class="layout-body">
      <!-- Overlay for mobile -->
      <div class="sidebar-overlay" v-if="sidebarOpen" @click="sidebarOpen = false"></div>

      <!-- Sidebar -->
      <aside class="sidebar" :class="{ open: sidebarOpen }">
        <nav class="sidebar-nav">

          <router-link to="/user/" class="nav-item" @click="closeSidebar">
            <span class="nav-icon">
              <Icon icon="mi:home" width="20" height="20" />
            </span>
            <span class="nav-label">Trang chủ</span>
          </router-link>

          <router-link :to="`${isPublicPage ? '' : '/user'}/search`" class="nav-item" @click="closeSidebar">
            <span class="nav-icon">
              <Icon icon="ic:outline-search" width="20" height="20" />
            </span>
            <span class="nav-label">Tìm kiếm sách</span>
          </router-link>

          <div class="nav-group-title">Tài khoản</div>

          <router-link to="/user/profile" class="nav-item" @click="closeSidebar">
            <span class="nav-icon">
              <Icon icon="gg:profile" width="20" height="20" />
            </span>
            <span class="nav-label">Thông tin cá nhân</span>
          </router-link>

          <router-link to="/user/my-books" class="nav-item" @click="closeSidebar">
            <span class="nav-icon">📖</span>
            <span class="nav-label">Sách đang mượn</span>
            <span class="nav-badge" v-if="myStats.borrowing > 0">
              {{ myStats.borrowing }}
            </span>
          </router-link>

          <router-link to="/user/my-reading" class="nav-item" @click="closeSidebar">
            <span class="nav-icon">
              <Icon icon="fluent:book-clock-20-filled" width="20" height="20" />
            </span>
            <span class="nav-label">Sách đang đọc</span>
          </router-link>

          <router-link to="/user/my-favorites" class="nav-item">
            <span class="nav-icon">❤️</span>
            <span class="nav-label">Sách yêu thích</span>
          </router-link>

          <router-link to="/user/my-requests" class="nav-item" @click="closeSidebar">
            <span class="nav-icon">⏰</span>
            <span class="nav-label">Yêu cầu mượn</span>
            <span class="nav-badge badge-yellow" v-if="myStats.pendingRequests > 0">
              {{ myStats.pendingRequests }}
            </span>
          </router-link>

          <router-link to="/user/my-fines" class="nav-item" @click="closeSidebar">
            <span class="nav-icon">💰</span>
            <span class="nav-label">Phiếu phạt</span>
            <span class="nav-badge badge-red" v-if="myStats.pendingFines > 0">
              {{ myStats.pendingFines }}
            </span>
          </router-link>

        </nav>

        <!-- User info bottom -->
        <div class="sidebar-footer">
          <div class="sidebar-user">
            <div class="sidebar-avatar">{{ initials }}</div>
            <div>
              <div class="sidebar-name">{{ userName || '—' }}</div>
              <div class="sidebar-code">{{ userCode }}</div>
            </div>
          </div>
        </div>
      </aside>

      <!-- Main content -->
      <main class="main-content">
        <!-- Overdue warning -->
        <div class="overdue-warning" v-if="myStats.overdue > 0">
          ⚠️ Bạn có <strong>{{ myStats.overdue }}</strong> sách quá hạn.
          <router-link to="/user/my-books">Xem ngay
            <Icon icon="bi:arrow-right" width="16" height="10" />
          </router-link>
        </div>

        <router-view />
      </main>
    </div>

  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import api from '../services/api'
import { Icon } from '@iconify/vue'
import NotificationBell from '../components/share/NotificationBell.vue'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const sidebarOpen = ref(false)
const searchQuery = ref('')
const showUserMenu = ref(false)
const showNotif = ref(false)
const unreadCount = ref(0)

const myStats = ref({
  borrowing: 0,
  overdue: 0,
  pendingRequests: 0,
  pendingFines: 0
})

const initials = computed(() => {
  const name = authStore.user?.userName || ''
  return name.split(' ').map(w => w[0]).slice(-2).join('').toUpperCase() || '?'
})

const userCode = computed(() => {
  const u = authStore.user
  return u?.userCode || u?.email || ''
})
const userName = computed(() => {
  const u = authStore.user
  return u.userName || ''
})

const isPublicPage = computed(() => route?.name?.includes("public"))

onMounted(async () => {
  await fetchMyStats()
  document.addEventListener('click', handleOutsideClick)
})

onUnmounted(() => {
  document.removeEventListener('click', handleOutsideClick)
})

const fetchMyStats = async () => {
  try {
    const res = await api.get('/account/my-stats')
    if (res.status === 200) myStats.value = res.data
  } catch { }
}

const goSearch = () => {
  if (searchQuery.value.trim())
    router.push(`${isPublicPage.value ? '' : '/user'}/search?keyword=${encodeURIComponent(searchQuery.value.trim())}`)
  else
    router.push(`${isPublicPage.value ? '' : '/user'}/search`)
  searchQuery.value = ''
}

const closeSidebar = () => { sidebarOpen.value = false }

const toggleUserMenu = () => {
  showUserMenu.value = !showUserMenu.value
  showNotif.value = false
}

const toggleNotif = () => {
  showNotif.value = !showNotif.value
  showUserMenu.value = false
}

const handleOutsideClick = (e) => {
  const userMenuBtn = document.querySelector('.user-menu')
  const userDropdown = document.querySelector('.user-dropdown')
  if (userMenuBtn && !userMenuBtn.contains(e.target) &&
    userDropdown && !userDropdown.contains(e.target)) {
    showUserMenu.value = false
  }
}

const logout = async () => {
  await authStore.logout()
  router.push('/login')
}
</script>

<style lang="scss" scoped>
$header-h: 60px;
$sidebar-w: 240px;
$primary: #3949ab;
$primary-dark: #2c3a8c;

.user-layout {
  min-height: 100vh;
  background: #f5f6fa;
  font-family: 'Segoe UI', sans-serif;
}

// Header
.header {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 100;
  height: $header-h;
  background: #fff;
  border-bottom: 1px solid #e8e8e8;
  display: flex;
  align-items: center;
  padding: 0 16px;
  gap: 12px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.06);
}

.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-shrink: 0;
}

.sidebar-toggle {
  display: none;
  flex-direction: column;
  gap: 4px;
  background: none;
  border: none;
  cursor: pointer;
  padding: 6px;
  border-radius: 6px;

  span {
    display: block;
    width: 20px;
    height: 2px;
    background: #555;
    border-radius: 2px;
    transition: all 0.2s;
  }

  &:hover {
    background: #f0f0f0;
  }

  @media (max-width: 768px) {
    display: flex;
  }
}

.logo {
  display: flex;
  align-items: center;
  gap: 10px;
  text-decoration: none;
  color: inherit;
}

.logo-icon {
  display: inline-flex;

  img {
    width: 30px;
  }
}

.logo-name {
  font-size: 14px;
  font-weight: 800;
  color: $primary;
  line-height: 1.2;
}

.logo-sub {
  font-size: 10px;
  color: #888;
}

.header-center {
  flex: 1;
  max-width: 480px;
  margin: 0 auto;
}

.header-search {
  display: flex;
  gap: 0;
  border: 1.5px solid #e0e0e0;
  border-radius: 99px;
  overflow: hidden;

  @media (max-width: 500px) {
    flex: none;
  }

  &:focus-within {
    border-color: $primary;
  }

  input {
    flex: 1;
    padding: 8px 16px;
    border: none;
    outline: none;
    font-size: 14px;
    font-family: inherit;
    background: transparent;

    @media (max-width: 500px) {
      display: none;
    }
  }

  button {
    padding: 8px 14px;
    background: none;
    border: none;
    cursor: pointer;
    font-size: 15px;
    color: #888;

    &:hover {
      color: $primary;
    }

    svg {
      margin-bottom: -4px;
    }

    @media (max-width: 500px) {
      padding: 8px;
    }
  }
}

.header-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
  position: relative;
}

.notif-btn {
  position: relative;
  cursor: pointer;
  padding: 8px;
  border-radius: 8px;
  transition: background 0.15s;

  &:hover {
    background: #f0f0f0;
  }

  .bell {
    font-size: 18px;
  }
}

.notif-badge {
  position: absolute;
  top: 4px;
  right: 4px;
  background: #e53935;
  color: #fff;
  font-size: 10px;
  font-weight: 700;
  width: 16px;
  height: 16px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.user-menu {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  padding: 6px 10px;
  border-radius: 8px;
  transition: background 0.15s;

  &:hover {
    background: #f0f0f0;
  }
}

.user-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: $primary;
  color: #fff;
  font-size: 12px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.user-info {
  @media (max-width: 600px) {
    display: none;
  }
}

.user-name {
  font-size: 13px;
  font-weight: 600;
  line-height: 1.2;
  color: #333333;
}

.user-code {
  font-size: 11px;
  color: #888;
  font-family: monospace;
}

.chevron {
  font-size: 11px;
  color: #aaa;
}

// Dropdown
.dropdown {
  position: absolute;
  top: calc(100% + 8px);
  right: 0;
  z-index: 200;
  background: #fff;
  border-radius: 10px;
  border: 1px solid #e0e0e0;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
  min-width: 180px;
  overflow: hidden;
}

.dropdown-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 16px;
  font-size: 14px;
  color: #333;
  text-decoration: none;
  cursor: pointer;
  background: none;
  border: none;
  width: 100%;
  text-align: left;
  transition: background 0.1s;

  &:hover {
    background: #f5f5f5;
  }

  &.text-red {
    color: #c62828;
  }
}

.dropdown-divider {
  height: 1px;
  background: #f0f0f0;
}

// Layout body
.layout-body {
  display: flex;
  padding-top: $header-h;
  min-height: 100vh;
}

// Sidebar overlay (mobile)
.sidebar-overlay {
  display: none;

  @media (max-width: 768px) {
    display: block;
    position: fixed;
    inset: 0;
    z-index: 98;
    background: rgba(0, 0, 0, 0.4);
  }
}

// Sidebar
.sidebar {
  width: $sidebar-w;
  flex-shrink: 0;
  background: #fff;
  border-right: 1px solid #e8e8e8;
  position: fixed;
  top: $header-h;
  left: 0;
  bottom: 0;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  z-index: 99;
  transition: transform 0.25s;

  @media (max-width: 768px) {
    transform: translateX(-100%);

    &.open {
      transform: translateX(0);
    }
  }
}

.sidebar-nav {
  flex: 1;
  padding: 12px 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.nav-group-title {
  font-size: 11px;
  font-weight: 700;
  color: #aaa;
  text-transform: uppercase;
  letter-spacing: 0.8px;
  padding: 12px 16px 4px;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 16px;
  border-radius: 0;
  text-decoration: none;
  color: #444;
  font-size: 14px;
  font-weight: 500;
  transition: all 0.15s;
  position: relative;
  margin: 0 8px;
  border-radius: 8px;

  &:hover {
    background: #f5f6ff;
    color: $primary;
  }

  &.router-link-active {
    background: #e8eaf6;
    color: $primary;
    font-weight: 700;

    .nav-icon {
      filter: none;
    }
  }
}

.nav-icon {
  font-size: 16px;
  flex-shrink: 0;
  display: inline-flex;
}

.nav-label {
  flex: 1;
}

.nav-badge {
  background: $primary;
  color: #fff;
  font-size: 11px;
  font-weight: 700;
  padding: 1px 7px;
  border-radius: 99px;
  flex-shrink: 0;

  &.badge-yellow {
    background: #f57f17;
  }

  &.badge-red {
    background: #e53935;
  }
}

.sidebar-footer {
  padding: 16px;
  border-top: 1px solid #f0f0f0;
}

.sidebar-user {
  display: flex;
  align-items: center;
  gap: 10px;
}

.sidebar-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: $primary;
  color: #fff;
  font-size: 12px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.sidebar-name {
  font-size: 13px;
  font-weight: 600;
  color: #333333;
}

.sidebar-code {
  font-size: 11px;
  color: #888;
  font-family: monospace;
  margin-top: 1px;
}

// Main content
.main-content {
  flex: 1;
  margin-left: $sidebar-w;
  padding: 24px;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 20px;

  @media (max-width: 768px) {
    margin-left: 0;
    padding: 16px;
  }
}

// Overdue warning
.overdue-warning {
  padding: 12px 16px;
  background: #fff3e0;
  border-left: 3px solid #fb8c00;
  border-radius: 0 8px 8px 0;
  font-size: 14px;
  color: #e65100;

  a {
    color: #e65100;
    font-weight: 700;
    margin-left: 6px;

    &:hover {
      text-decoration: underline;
    }
  }
}
</style>