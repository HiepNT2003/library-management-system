<template>
  <div class="loading_api">
    <div class="loading_icon"></div>
  </div>
</template>
<style lang="scss" scoped>
.loading_api {
  position: fixed;
  top: 0;
  bottom: 0;
  left: 0;
  right: 0;
  display: flex;
  justify-content: center;
  align-items: center;
  background-color: #dcdcdc;
  opacity: 0.4;
  z-index: 9999;
  .loading_icon {
    display: inline-block;
    background: url(../assets/Loading2.gif) no-repeat;
    width: 400px;
    height: 400px;
    background-size: contain;
  }
}
</style>