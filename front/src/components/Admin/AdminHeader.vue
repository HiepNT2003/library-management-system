<template>
  <header class="admin-header">
    <!-- Logo -->
    <router-link to="/" class="header-logo">
      <div class="logo-icon">
        <img src="../../assets/Images/Logo.png" alt="Logo" srcset="" />
      </div>
      <span class="logo-text">{{ appName }}</span>
    </router-link>
    <header>
      <a href="#" class="burger-btn d-block d-xl-none">
        <i class="bi bi-justify"></i>
      </a>
    </header>
    <div class="header-divider" />

    <!-- Breadcrumb -->
    <nav class="header-breadcrumb" aria-label="Breadcrumb">
      <template v-for="(crumb, index) in breadcrumbs" :key="index">
        <router-link v-if="crumb.to && index < breadcrumbs.length - 1" :to="crumb.to" class="crumb-link">
          {{ crumb.label }}
        </router-link>
        <span v-else :class="['crumb-item', { 'is-current': index === breadcrumbs.length - 1 }]">
          {{ crumb.label }}
        </span>
        <span v-if="index < breadcrumbs.length - 1" class="crumb-sep">/</span>
      </template>
    </nav>

    <div class="header-spacer" />

    <!-- Actions -->
    <div class="header-actions">
      <!-- Notifications -->
      <NotificationBell :is-admin="true" />

      <!-- Settings -->
      <button class="icon-btn" title="Cài đặt" @click="$router.push('/admin/catalog-settings')">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"
          stroke-linejoin="round">
          <circle cx="12" cy="12" r="3" />
          <path
            d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z" />
        </svg>
      </button>

      <div class="header-divider" />

      <!-- User avatar dropdown -->
      <div class="user-wrap" ref="userDropdownRef">
        <button class="user-btn" @click="toggleUserMenu">
          <div class="user-avatar">
            <img v-if="userInfo.avatar" :src="userInfo.avatar" :alt="userInfo.userName" class="avatar-img" />
            <span v-else>{{ userInitials }}</span>
          </div>
          <div class="user-info">
            <span class="user-name">{{ userInfo.userName }}</span>
            <span class="user-role">{{ userInfo.roles[0] }}</span>
          </div>
          <svg class="chevron-icon" :class="{ 'is-open': userMenuOpen }" viewBox="0 0 24 24" fill="none"
            stroke="currentColor" stroke-width="2">
            <polyline points="6 9 12 15 18 9" />
          </svg>
        </button>

        <!-- User dropdown menu -->
        <transition name="menu-drop">
          <div v-if="userMenuOpen" class="user-menu">
            <div class="menu-header">
              <div class="menu-avatar">
                <img v-if="userInfo.avatar" :src="userInfo.avatar" :alt="userInfo.userName" class="avatar-img" />
                <span v-else>{{ userInitials }}</span>
              </div>
              <div>
                <div class="menu-name">{{ userInfo.userName }}</div>
                <div class="menu-email">{{ userInfo.email }}</div>
              </div>
            </div>
            <div class="menu-divider" />
            <button class="menu-item" @click="handleMenuItem('adminProfile')">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                <circle cx="12" cy="7" r="4" />
              </svg>
              Hồ sơ cá nhân
            </button>
            <button class="menu-item" @click="handleMenuItem('adminProfile', { screen: 'change-password' })">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
                <path d="M7 11V7a5 5 0 0 1 10 0v4" />
              </svg>
              Đổi mật khẩu
            </button>
            <div class="menu-divider" />
            <button class="menu-item is-danger" @click="handleLogout">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
                <polyline points="16 17 21 12 16 7" />
                <line x1="21" y1="12" x2="9" y2="12" />
              </svg>
              Đăng xuất
            </button>
          </div>
        </transition>
      </div>
    </div>
  </header>
</template>

<script>
import { useAuthStore } from "../../stores/auth"
import NotificationBell from "../share/NotificationBell.vue";
export default {
  name: "AdminHeader",

  props: {
    appName: {
      type: String,
      default: "Thư Viện UTC",
    },
    breadcrumbs: {
      type: Array,
      default: () => [],
      // [{ label: 'Dashboard', to: '/' }, { label: 'Người dùng', to: '/users' }, { label: 'Danh sách' }]
    },
    user: {
      type: Object,
      default: () => ({
        name: "Admin",
        email: "admin@example.com",
        role: "Super Admin",
        avatar: null,
      }),
    },
    notificationCount: {
      type: Number,
      default: 0,
    },
    searchPlaceholder: {
      type: String,
      default: "Tìm kiếm...",
    },
  },
  components: { NotificationBell },

  emits: ["search", "open-notifications", "open-settings", "menu-action"],

  data() {
    return {
      searchQuery: "",
      userMenuOpen: false,
    }
  },

  computed: {
    userInitials() {
      return this.userInfo.userName
        .split(" ")
        .map((w) => w[0])
        .slice(-2)
        .join("")
        .toUpperCase()
    },
    userInfo() {
      const authStore = useAuthStore()
      return authStore.user
    },
    avatarName() {
      return this.userInfo?.userName[0]
    },
  },

  methods: {
    handleSearch() {
      this.$emit("search", this.searchQuery)
    },

    toggleUserMenu() {
      this.userMenuOpen = !this.userMenuOpen
    },

    handleMenuItem(action, param = null) {
      this.userMenuOpen = false
      this.$router.push({
        name: action,
        query: param,
      })
    },

    handleLogout() {
      this.$router.push({ name: "login" })
    },

    handleOutsideClick(e) {
      if (this.$refs.userDropdownRef && !this.$refs.userDropdownRef.contains(e.target)) {
        this.userMenuOpen = false
      }
    },
  },

  mounted() {
    document.addEventListener("mousedown", this.handleOutsideClick)
  },

  beforeUnmount() {
    document.removeEventListener("mousedown", this.handleOutsideClick)
  },
}
</script>

<style lang="scss" scoped>
@import url("https://fonts.googleapis.com/css2?family=Be+Vietnam+Pro:wght@400;500;600&display=swap");

*,
*::before,
*::after {
  box-sizing: border-box;
}

.admin-header {
  display: flex;
  align-items: center;
  height: 60px;
  padding: 0 20px;
  background: #ffffff;
  border-bottom: 1px solid #e9ecef;
  gap: 16px;
  font-family: 'Nunito';
  position: sticky;
  top: 0;
  z-index: 100;
}

/* ── Logo ─────────────────────────────────────────────── */
.header-logo {
  display: flex;
  align-items: center;
  gap: 8px;
  text-decoration: none;
  flex-shrink: 0;
}

.logo-icon {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;

  img {
    width: 32px;
    margin-bottom: 4px;
  }
}

.logo-icon svg {
  width: 16px;
  height: 16px;
  stroke: #fff;
}

.logo-text {
  font-size: 15px;
  font-weight: 700;
  color: #111827;
  letter-spacing: -0.3px;
}

.burger-btn {
  font-size: 20px;
  margin-top: 4px;
}

/* ── Divider ──────────────────────────────────────────── */
.header-divider {
  width: 1px;
  height: 28px;
  background: #e9ecef;
  flex-shrink: 0;
}

.header-spacer {
  flex: 1;
}

/* ── Breadcrumb ───────────────────────────────────────── */
.header-breadcrumb {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 13px;
  flex-shrink: 0;
}

.crumb-link {
  color: #6b7280;
  text-decoration: none;
  transition: color 0.15s;
}

.crumb-link:hover {
  color: #4f46e5;
}

.crumb-item {
  color: #6b7280;
}

.crumb-item.is-current {
  color: #111827;
  font-weight: 600;
}

.crumb-sep {
  color: #d1d5db;
  padding: 0 2px;
}

/* ── Actions ──────────────────────────────────────────── */
.header-actions {
  display: flex;
  align-items: center;
  gap: 4px;
}

/* ── Search ───────────────────────────────────────────── */
.search-wrap {
  position: relative;
  margin-right: 4px;
}

.search-icon {
  position: absolute;
  left: 9px;
  top: 50%;
  transform: translateY(-50%);
  width: 14px;
  height: 14px;
  color: #9ca3af;
  pointer-events: none;
}

.search-input {
  height: 34px;
  width: 200px;
  padding: 0 28px 0 32px;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  background: #f9fafb;
  font-size: 13px;
  color: #111827;
  outline: none;
  transition: border-color 0.15s, width 0.25s ease, background 0.15s;
}

.search-input::placeholder {
  color: #9ca3af;
}

.search-input:focus {
  border-color: #6366f1;
  width: 260px;
  background: #fff;
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
}

.search-clear {
  position: absolute;
  right: 8px;
  top: 50%;
  transform: translateY(-50%);
  background: none;
  border: none;
  font-size: 16px;
  color: #9ca3af;
  cursor: pointer;
  padding: 0;
  line-height: 1;
  transition: color 0.15s;
}

.search-clear:hover {
  color: #374151;
}

/* ── Icon buttons ─────────────────────────────────────── */
.icon-btn {
  position: relative;
  width: 36px;
  height: 36px;
  border-radius: 8px;
  border: none;
  background: transparent;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #6b7280;
  transition: background 0.15s, color 0.15s;
}

.icon-btn:hover {
  background: #f3f4f6;
  color: #111827;
}

.icon-btn svg {
  width: 18px;
  height: 18px;
}

.notif-badge {
  position: absolute;
  top: 4px;
  right: 4px;
  min-width: 16px;
  height: 16px;
  padding: 0 3px;
  border-radius: 99px;
  background: #ef4444;
  color: #fff;
  font-size: 10px;
  font-weight: 600;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1.5px solid #fff;
  line-height: 1;
}

/* ── User menu ────────────────────────────────────────── */
.user-wrap {
  position: relative;
  margin-left: 4px;

  @media (max-width: 600px) {
    .user-info {
      display: none;
    }
  }
}

.user-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 8px 4px 4px;
  border-radius: 10px;
  border: 1px solid #e5e7eb;
  background: transparent;
  cursor: pointer;
  transition: background 0.15s, border-color 0.15s;
}

.user-btn:hover {
  background: #f9fafb;
  border-color: #d1d5db;
}

.user-avatar {
  width: 30px;
  height: 30px;
  border-radius: 50%;
  background: #eef2ff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 11px;
  font-weight: 600;
  color: #4f46e5;
  flex-shrink: 0;
  overflow: hidden;
}

.avatar-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.user-info {
  display: flex;
  flex-direction: column;
  text-align: left;
}

.user-name {
  font-size: 13px;
  font-weight: 600;
  color: #111827;
  line-height: 1.3;
  white-space: nowrap;
}

.user-role {
  font-size: 11px;
  color: #9ca3af;
  line-height: 1.3;
}

.chevron-icon {
  width: 14px;
  height: 14px;
  color: #9ca3af;
  transition: transform 0.2s;
}

.chevron-icon.is-open {
  transform: rotate(180deg);
}

/* ── Dropdown menu ────────────────────────────────────── */
.user-menu {
  position: absolute;
  top: calc(100% + 6px);
  right: 0;
  width: 220px;
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.08);
  overflow: hidden;
  z-index: 200;
}

.menu-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 14px 12px;
}

.menu-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: #eef2ff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
  font-weight: 600;
  color: #4f46e5;
  flex-shrink: 0;
  overflow: hidden;
}

.menu-name {
  font-size: 13.5px;
  font-weight: 500;
  color: #111827;
}

.menu-email {
  font-size: 12px;
  color: #9ca3af;
  margin-top: 1px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 140px;
}

.menu-divider {
  height: 1px;
  background: #f3f4f6;
  margin: 2px 0;
}

.menu-item {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  padding: 9px 14px;
  background: none;
  border: none;
  font-size: 13.5px;
  color: #374151;
  cursor: pointer;
  text-align: left;
  transition: background 0.12s;
}

.menu-item:hover {
  background: #f9fafb;
}

.menu-item.is-danger {
  color: #dc2626;
}

.menu-item.is-danger:hover {
  background: #fef2f2;
}

.menu-item svg {
  width: 15px;
  height: 15px;
  flex-shrink: 0;
}

/* ── Transitions ──────────────────────────────────────── */
.menu-drop-enter-active,
.menu-drop-leave-active {
  transition: all 0.18s cubic-bezier(0.16, 1, 0.3, 1);
}

.menu-drop-enter-from,
.menu-drop-leave-to {
  opacity: 0;
  transform: translateY(-6px) scale(0.97);
}
</style>