<template>
  <div id="error">
    <div class="error-page container">
      <div class="col-md-8 col-12 offset-md-2 wrap">
        <img class="img-error" src="../assets/Images/samples/error-404.png" alt="Not Found" />
        <div class="text-center">
          <h1 class="error-title">NOT FOUND</h1>
          <p class="fs-5 text-gray-600">Trang không tồn tại.</p>
          <button class="btn btn-lg btn-outline-primary mt-3" @click="goHome">Về trang chủ</button>
        </div>
      </div>
    </div>
  </div>
</template>
<script>
export default {
  mounted() {
    import("../assets/css/bootstrap.css")
    import("../assets/css/pages/error.css")
  },
  methods: {
    goHome() {
      this.$router.push({ name: "Discover" }).then(() => {
        window.location.reload()
      })
    },
  },
}
</script>
<style lang="scss" scoped>
.wrap {
  display: flex;
  flex-direction: column;
  align-items: center;

  img {
    width: 600px;
  }
}
</style>