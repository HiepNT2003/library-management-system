<template>
  <div id="sidebar" class="active">
    <div class="sidebar-wrapper active">
      <div class="sidebar-header">
        <div class="d-flex justify-content-between">
          <div class="logo">
            <div>
              <img src="../../assets/Images/Logo.png" alt="Logo" srcset="" />
              <h3>UTC Lib</h3>
            </div>
          </div>
          <div class="toggler">
            <a href="#" class="sidebar-hide d-xl-none d-block"><i class="bi bi-x bi-middle"></i></a>
          </div>
        </div>
      </div>
      <div class="sidebar-menu">
        <ul class="menu">
          <template v-for="item in listMenus" :key="item.id">
            <li class="sidebar-item" :class="{
              active: menuItem.id == selectedMenu.id || hasSubMenuSelected(menuItem),
            }" v-for="menuItem in item.items" :key="menuItem.id">
              <div class="sidebar-link" @click="handleSelectMenu(menuItem)">
                <Icon :icon="menuItem.icon" width="24" height="24" />
                <span>{{ menuItem.name }}</span>
                <Icon class="icon_toogle" :class="{ toogleUp: menuItem.isShowSub }" v-if="menuItem.items?.length"
                  icon="iconamoon:arrow-down-2" width="20" height="20" />
              </div>
              <ul class="submenu" v-show="menuItem.isShowSub">
                <li class="submenu-item" v-for="sub in menuItem.items" :key="sub.id">
                  <div class="sub_item" :class="{ active: sub.id == selectedMenu.id }" @click="handleSelectMenu(sub)">
                    <span>{{ sub.name }}</span>
                  </div>
                </li>
              </ul>
            </li>
          </template>
        </ul>
      </div>
      <button class="sidebar-toggler btn x"><i data-feather="x"></i></button>
    </div>
    <div class="sidebar-backdrop"></div>
  </div>
</template>
<script>
import { Icon } from "@iconify/vue"
export default {
  components: { Icon },
  data() {
    return {
      selectedMenu: null,
      listMenus: [
        {
          id: 1,
          title: "Main menu",
          items: [
            {
              id: 1,
              name: "Tổng quan",
              icon: "mage:dashboard-chart",
              router: "dashboard",
            },
            {
              id: 2,
              name: "Sách",
              icon: "ph:books-duotone",
              router: "booksManage",
            },
            {
              id: 3,
              name: "Yêu Cầu Mượn",
              icon: "codicon:git-pull-request-go-to-changes",
              router: "borrowRequestManagement",
            },
            {
              id: 4,
              name: "Mượn trả",
              icon: "carbon:connection-two-way",
              isShowSub: false,
              items: [
                {
                  id: 10,
                  name: "Quản lý mượn trả",
                  icon: "glyphs:book-atlas-bold",
                  router: "transactionManagement",
                },
                {
                  id: 11,
                  name: "Phiếu mượn",
                  icon: "glyphs:book-atlas-bold",
                  router: "addTransactions",
                },
                {
                  id: 12,
                  name: "Phiếu trả",
                  icon: "glyphs:book-atlas-bold",
                  router: "returnBook",
                },
              ],
            },
            {
              id: 5,
              name: "Người dùng",
              icon: "ph:users-four-light",
              router: "userManagement",
            },
            {
              id: 6,
              name: "Quản lý phạt",
              icon: "roentgen:money",
              router: "fineManagement",
            },
            {
              id: 7,
              name: "Báo cáo",
              icon: "iconoir:reports",
              router: "adminReports",
            },
          ],
        },
        {
          id: 2,
          title: "Account",
          items: [
            {
              id: 8,
              name: "Hệ thống",
              icon: "weui:setting-outlined",
              router: "catalogSettings"
            },
            {
              id: 9,
              name: "Logout",
              icon: "mynaui:logout",
            },
          ],
        },
      ],
    }
  },
  created() {
    const currentPage = this.listMenus
      .flatMap((item) => item.items)
      .flatMap((item) => (item?.items ? item.items : item))
      .find((item) => item?.router == this.$route.name)
    this.selectedMenu = currentPage ? currentPage : this.listMenus[0].items[0]
  },
  async mounted() {
    await import("../../assets/js/main.js")
  },
  computed: {
    routerName() {
      return this.$route.name
    },
  },
  methods: {
    handleLogout() {
      this.$router.push({ name: "login" })
    },

    handleSelectMenu(menuItem) {
      if (menuItem.items?.length) {
        this.listMenus = this.listMenus.map((item) => ({
          ...item,
          items: item.items.map((menu) =>
            menu.id == menuItem.id ? { ...menu, isShowSub: !menuItem.isShowSub } : menu
          ),
        }))
        return
      }
      this.selectedMenu = menuItem
      this.$router.push({ name: menuItem.router })
      if (menuItem.id == 9) {
        this.handleLogout()
      }
    },
    hasSubItem(menu) {
      return menu?.items?.length
    },
    hasSubMenuSelected(menu) {
      return menu?.items?.find((item) => item.id == this.selectedMenu.id) ? true : false
    },
  },
  watch: {
    routerName() {
      const currentPage = this.listMenus
        .flatMap((item) => item.items)
        .flatMap((item) => (item?.items ? item.items : item))
        .find((item) => item?.router == this.routerName)
      this.selectedMenu = currentPage ? currentPage : this.selectedMenu
    },
  },
}
</script>
<style lang="scss" scoped>
.sidebar-backdrop {
  display: none;
}

#sidebar {
  &.active {
    .sidebar-backdrop {
      display: block;
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(0, 0, 0, 0.5);
      z-index: 9;
    }
  }
}

.sidebar-wrapper {
  width: 220px;

  .sidebar-header {
    padding: 10px 30px 0px;

    .logo>div {
      display: flex;
      gap: 8px;
      align-items: end;

      h3 {
        margin: 0;
        font-size: 20px;
      }
    }

    img {
      height: 3rem !important;
    }
  }

  .sidebar-menu {

    max-height: calc(100% - 59px);
    overflow: auto;

    .menu {
      margin-top: 20px;
      padding: 0 14px;
    }

    .icon_toogle {
      margin-left: 16px;
      transition: all 0.3s;

      &.toogleUp {
        transition: all 0.3s;
        transform: rotate(180deg);
      }
    }

    .submenu {
      display: block;
      max-height: 999px;
      list-style: none;
      transition: max-height 2s cubic-bezier(0, 0.55, 0.45, 1);
      overflow: hidden;
      padding-left: 12px;

      .sub_item {
        padding: 0.7rem 1rem;
        display: flex;
        color: #25396f;
        font-size: 0.85rem;
        font-weight: 600;
        letter-spacing: 0.5px;
        transition: all 0.3s;
        align-items: center;
        cursor: pointer;

        span {
          margin-left: 0.7rem;
        }

        &.active {
          color: #435ebe;
          font-weight: 700;
        }

        &:hover {
          margin-left: 6px;
        }
      }
    }
  }
}

@media (min-width: 1200px) {
  .sidebar-backdrop {
    display: none !important;
  }
}

.sidebar-link {
  cursor: pointer;
}
</style>