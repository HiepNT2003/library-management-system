<template>
  <aside class="sidebar" id="sidebar">
    <div class="sidebar-header">
      <img src="../assets/Images/Logo.png" alt="" srcset="" class="logo" />
      <span class="logo-text">UTC Library</span>
    </div>

    <ul class="nav-menu">
      <li class="nav-section" v-for="block in listMenus" :key="block.id">
        <span class="nav-section-title">{{ block.title }}</span>
        <ul>
          <li class="nav-item" v-for="item in block.items" :key="block.id + '_menu_' + item.id">
            <div class="nav-link" :class="{ active: item.id == selectedMenu.id }" @click="handleSelectMenu(item)">
              <Icon :icon="item.icon" width="24" height="24" />
              {{ item.name }}
            </div>
          </li>
        </ul>
      </li>
    </ul>

    <div class="sidebar-footer">
      <div class="user-profile">
        <div class="user-avatar">{{ avatarName }}</div>
        <div class="user-info" v-if="userInfo">
          <div class="user-name">{{ userInfo.userName }}</div>
          <div class="user-mail">{{ userInfo.email }}</div>
        </div>
      </div>
    </div>
  </aside>
</template>
<script>
import { useAuthStore } from "@/stores/auth";
import { Icon } from "@iconify/vue";

export default {
  components: { Icon },
  props: {
    listMenus: {
      type: Array,
      default: () => [],
    },
  },
  data() {
    return {
      selectedMenu: null,
    };
  },
  created() {
    const currentPage = this.listMenus.flatMap(item => item.items).find(item => item.router == this.$route.name)
    this.selectedMenu = currentPage ? currentPage : this.listMenus[0].items[0];
  },
  computed: {
    userInfo() {
      const authStore = useAuthStore();
      return authStore.user;
    },
    avatarName() {
      return this.userInfo?.userName[0];
    },
  },
  methods: {
    handleLogout() {
      this.$router.push({ name: "login" });
    },
    handleSelectMenu(menuItem) {
      this.selectedMenu = menuItem;
      this.$router.push({ name: menuItem.router });
      if (menuItem.id == 8) {
        this.handleLogout();
      }
    },
  },
};
</script>
<style lang="scss" scoped>
.sidebar {
  position: fixed;
  left: 0;
  top: 0;
  width: var(--sidebar-width);
  height: 100vh;
  background: var(--glass-bg);
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  border-right: 1px solid var(--glass-border);
  padding: 16px;
  z-index: 100;
  transition: all var(--transition-normal);
  overflow-y: auto;
}

/* Custom Scrollbar for Sidebar */
.sidebar::-webkit-scrollbar {
  width: 6px;
}

.sidebar::-webkit-scrollbar-track {
  background: transparent;
}

.sidebar::-webkit-scrollbar-thumb {
  background: var(--glass-border);
  border-radius: 3px;
}

.sidebar::-webkit-scrollbar-thumb:hover {
  background: var(--emerald-light);
}

.sidebar-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding-bottom: 24px;
  border-bottom: 1px solid var(--glass-border);
  margin-bottom: 24px;
}

.logo {
  width: 45px;
  //   display: flex;
  //   align-items: center;
  //   justify-content: center;
  //   font-weight: 700;
  //   font-size: 20px;
  //   box-shadow: 0 8px 32px rgba(5, 150, 105, 0.3);
}

.logo-text {
  font-size: 22px;
  font-weight: 600;
  background: linear-gradient(135deg, var(--emerald-light), var(--gold));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.nav-menu {
  list-style: none;
  flex: 1;
}

.nav-menu ul {
  list-style: none;
  padding: 0;
  margin: 0;
}

.nav-section {
  margin-bottom: 25px;
  list-style: none;
}

.nav-section-title {
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 1.5px;
  color: var(--text-muted);
  margin-bottom: 12px;
  padding-left: 15px;
}

.nav-item {
  margin-bottom: 5px;
  list-style: none;
  cursor: pointer;
}

.nav-link {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 14px 18px;
  color: var(--text-secondary);
  text-decoration: none;
  border-radius: 12px;
  transition: all var(--transition-fast);
  position: relative;
  overflow: hidden;
  font-size: 15px;
  font-weight: 500;
}

.nav-link:hover {
  background: var(--glass-hover);
  color: var(--text-primary);
}

.nav-link.active {
  background: var(--glass-hover);
  color: var(--text-primary);
}

.nav-icon {
  width: 22px;
  height: 22px;
  opacity: 0.8;
}

.nav-link.active .nav-icon,
.nav-link:hover .nav-icon {
  opacity: 1;
}

.nav-badge {
  margin-left: auto;
  background: linear-gradient(135deg, var(--gold), var(--amber));
  color: white;
  font-size: 11px;
  font-weight: 600;
  padding: 3px 8px;
  border-radius: 20px;
}

/* Sidebar User Profile */
.sidebar-footer {
  padding-top: 20px;
  padding-bottom: 10px;
  border-top: 1px solid var(--glass-border);
  position: absolute;
  bottom: 0;
}

.user-profile {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  border-radius: 12px;
  cursor: pointer;
  transition: background var(--transition-fast);
}

.user-profile:hover {
  background: var(--glass-hover);
}

.user-avatar {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  background: linear-gradient(135deg, var(--emerald), var(--gold));
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  font-size: 16px;
}

.user-info {
  flex: 1;
  max-width: 130px;
  overflow: hidden;
  text-overflow: ellipsis;
}

.user-name {
  font-weight: 500;
  font-size: 14px;
  text-overflow: ellipsis;
}

.user-mail {
  font-size: 12px;
  text-overflow: ellipsis;
  color: var(--text-muted);
}
</style>