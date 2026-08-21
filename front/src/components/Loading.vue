<template>
  <div class="loading">
    <div class="loading_icon"></div>
  </div>
</template>
<style lang="scss" scoped>
.loading {
  height: 100vh;
  display: flex;
  justify-content: center;
  align-items: center;
  background-color: #fff;
  .loading_icon {
	display: inline-block;
	background: url(../assets/Loading.gif) no-repeat;
	width: 100px;
	height: 100px;
    background-size: contain;
}
}
</style>