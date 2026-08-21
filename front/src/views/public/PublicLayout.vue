<template>
  <div class="public-layout">
    <!-- Header -->
    <header class="header">
      <div class="header-inner">
        <button class="sidebar-toggle" @click="sidebarOpen = !sidebarOpen">
          <span></span><span></span><span></span>
        </button>

        <router-link to="/" class="logo">
          <div class="logo-icon"><img src="../../assets/Images/LogoUTC.png" alt="" srcset=""></div>
          <div class="logo-text">
            <div class="logo-name">Thư viện UTC</div>
            <div class="logo-sub">ĐH Giao thông Vận tải</div>
          </div>
        </router-link>

        <div class="header-search">
          <input v-model="searchQuery" placeholder="Tìm sách, tác giả, chủ đề..." @keydown.enter="goSearch" />
          <button @click="goSearch">
            <Icon icon="ic:outline-search" width="18" height="18" />
          </button>
        </div>

        <div class="header-right">
          <router-link to="/login" class="btn-login">Đăng nhập</router-link>
        </div>
      </div>
    </header>

    <div class="layout-body">
      <div class="sidebar-overlay" v-if="sidebarOpen" @click="sidebarOpen = false"></div>

      <aside class="sidebar" :class="{ open: sidebarOpen }">
        <nav class="sidebar-nav">
          <div class="nav-group-title">Thư viện</div>

          <router-link to="/" class="nav-item" exact @click="sidebarOpen = false">
            <span class="nav-icon">
              <Icon icon="flat-color-icons:home" width="18" height="18" />
            </span>
            <span class="nav-label">Trang chủ</span>
          </router-link>

          <router-link to="/search" class="nav-item" @click="sidebarOpen = false">
            <span class="nav-icon">
              <Icon icon="ic:outline-search" width="18" height="18" />
            </span>
            <span class="nav-label">Tìm kiếm sách</span>
          </router-link>

          <div class="nav-group-title">Thông tin</div>

          <router-link to="/about" class="nav-item" @click="sidebarOpen = false">
            <span class="nav-icon">
              <Icon icon="icomoon-free:library" width="17" height="15" />
            </span>
            <span class="nav-label">Giới thiệu</span>
          </router-link>

          <router-link to="/guide" class="nav-item" @click="sidebarOpen = false">
            <span class="nav-icon">
              <Icon icon="material-symbols:developer-guide-outline-rounded" width="18" height="18" />
            </span>
            <span class="nav-label">Hướng dẫn sử dụng</span>
          </router-link>

          <router-link to="/rules" class="nav-item" @click="sidebarOpen = false">
            <span class="nav-icon">
              <Icon icon="material-symbols:rule-rounded" width="18" height="18" />
            </span>
            <span class="nav-label">Nội quy mượn trả</span>
          </router-link>

          <router-link to="/contact" class="nav-item" @click="sidebarOpen = false">
            <span class="nav-icon">
              <Icon icon="ic:round-contact-support" width="18" height="18" />
            </span>
            <span class="nav-label">Liên hệ</span>
          </router-link>
        </nav>

        <div class="sidebar-footer">
          <router-link to="/login" class="btn-login-sidebar">
            <icon icon="gg:profile" width="20" height="20" /> Đăng nhập để đặt mượn
          </router-link>
        </div>
      </aside>

      <main class="main-content">
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from "vue"
import { useRouter } from "vue-router"
import { useAuthStore } from "@/stores/auth"
import { Icon } from "@iconify/vue"

const router = useRouter()
const authStore = useAuthStore()

const searchQuery = ref("")
const sidebarOpen = ref(false)
const showUserMenu = ref(false)

const initials = computed(() => {
  const name = authStore.user?.fullName || ""
  return (
    name
      .split(" ")
      .map((w) => w[0])
      .slice(-2)
      .join("")
      .toUpperCase() || "?"
  )
})

const goSearch = () => {
  if (searchQuery.value.trim())
    router.push(`/search?keyword=${encodeURIComponent(searchQuery.value.trim())}`)
  else router.push("/search")
  searchQuery.value = ""
  sidebarOpen.value = false
}

const toggleUserMenu = () => {
  showUserMenu.value = !showUserMenu.value
}

const logout = async () => {
  await authStore.logout()
  showUserMenu.value = false
  router.push("/login")
}

const handleOutsideClick = (e) => {
  if (!e.target.closest(".user-menu") && !e.target.closest(".dropdown")) showUserMenu.value = false
}

onMounted(() => document.addEventListener("click", handleOutsideClick))
onUnmounted(() => document.removeEventListener("click", handleOutsideClick))
</script>

<style lang="scss" scoped>
$header-h: 60px;
$sidebar-w: 240px;

.public-layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  font-family: "Segoe UI", sans-serif;
  background: #f5f6fa;
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
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.06);
}

.header-inner {
  height: 100%;
  display: flex;
  align-items: center;
  padding: 0 16px;
  gap: 12px;
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
  flex-shrink: 0;

  span {
    display: block;
    width: 20px;
    height: 2px;
    background: #555;
    border-radius: 2px;
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
  flex-shrink: 0;
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
  color: #3949ab;
  line-height: 1.2;
}

.logo-sub {
  font-size: 10px;
  color: #888;
}

.header-search {
  flex: 1;
  max-width: 440px;
  display: flex;
  border: 1.5px solid #e0e0e0;
  border-radius: 99px;
  overflow: hidden;
  margin-left: auto;

  @media (max-width: 500px) {
    flex: none;
  }

  &:focus-within {
    border-color: #3949ab;
  }

  input {
    flex: 1;
    padding: 8px 16px;
    border: none;
    outline: none;
    font-size: 14px;
    font-family: inherit;
    background: transparent;

    &::placeholder {
      color: #aaa;
    }

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
      color: #3949ab;
    }

    @media (max-width: 500px) {
      padding: 8px;
    }

    svg {
      margin-bottom: -4px;
    }
  }
}

.header-right {
  margin-left: auto;
  position: relative;
  flex-shrink: 0;
}

.btn-login {
  display: inline-flex;
  align-items: center;
  padding: 7px 16px;
  background: #3949ab;
  color: #fff;
  border-radius: 8px;
  text-decoration: none;
  font-size: 14px;
  font-weight: 600;
  transition: background 0.15s;

  &:hover {
    background: #2c3a8c;
  }
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
  width: 30px;
  height: 30px;
  border-radius: 50%;
  background: #3949ab;
  color: #fff;
  font-size: 12px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
}

.user-name {
  font-size: 13px;
  font-weight: 600;

  @media (max-width: 600px) {
    display: none;
  }
}

.chevron {
  font-size: 11px;
  color: #aaa;
}

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
  padding: 10px 16px 4px;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 16px;
  margin: 0 8px;
  border-radius: 8px;
  text-decoration: none;
  color: #444;
  font-size: 14px;
  font-weight: 500;
  transition: all 0.15s;

  &:hover {
    background: #f5f6ff;
    color: #3949ab;
  }

  &.router-link-active {
    background: #e8eaf6;
    color: #3949ab;
    font-weight: 700;
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

.sidebar-footer {
  padding: 16px;
  border-top: 1px solid #f0f0f0;
  flex-shrink: 0;
}

.btn-login-sidebar {
  display: block;
  text-align: center;
  padding: 10px;
  background: #3949ab;
  color: #fff;
  border-radius: 10px;
  text-decoration: none;
  font-size: 13px;
  font-weight: 600;
  transition: background 0.15s;
  display: flex;
  gap: 8px;
  align-items: center;

  &:hover {
    background: #2c3a8c;
  }
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
  background: #3949ab;
  color: #fff;
  font-size: 12px;
  font-weight: 700;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.sidebar-name {
  font-size: 13px;
  font-weight: 600;
}

.btn-logout {
  background: none;
  border: none;
  color: #888;
  font-size: 12px;
  cursor: pointer;
  padding: 0;
  margin-top: 2px;

  &:hover {
    color: #c62828;
  }
}

// Main
.main-content {
  flex: 1;
  margin-left: $sidebar-w;
  padding: 28px 24px;
  min-width: 0;

  @media (max-width: 768px) {
    margin-left: 0;
    padding: 16px;
  }
}
</style>