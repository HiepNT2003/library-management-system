<template>
  <div class="admin_layout">
    <AdminSideBar :listMenus="listMenus" />
    <AdminHeader />
    <div id="main">
      <router-view></router-view>
    </div>
  </div>
</template>
<script>
import AdminHeader from "../../components/Admin/AdminHeader.vue"
import AdminSideBar from "../../components/Admin/AdminSideBar.vue"

export default {
  components: { AdminSideBar, AdminHeader },
  async mounted() {
    import("../../assets/vendors/perfect-scrollbar/perfect-scrollbar.min.js")
    import("../../assets/js/bootstrap.bundle.min.js")
    import("../../assets/css/bootstrap.css")
    import("../../assets/vendors/iconly/bold.css")
    import("../../assets/vendors/perfect-scrollbar/perfect-scrollbar.css")
    import("../../assets/vendors/bootstrap-icons/bootstrap-icons.css")
    import("../../assets/css/app.css")
  },
}
</script>
<style lang="scss" scoped>
.admin_layout {
  height: 100vh;
  overflow: auto;
}

#main {
  margin-left: 220px;
  padding: 1rem;
}

@media screen and (max-width: 1199px) {
  #main {
    margin-left: 0;
  }
}
</style>