<script setup>
import { ref } from "vue"
import Button from "primevue/button"
import { Icon } from "@iconify/vue"

const isFavourite = ref(false)

function changeFavourite() {
  isFavourite.value = !isFavourite.value
}
</script>
<template>
  <div class="favourite_wrap">
    <div class="page_title">Sách yêu thích</div>
    <Button
      class="favourite"
      severity="help"
      variant="text"
      raised
      rounded
      aria-label="Favorite"
      @click="changeFavourite"
      ><Icon
        :icon="isFavourite ? 'fluent:heart-16-filled' : 'fluent:heart-16-regular'"
        width="24"
        height="24"
    /></Button>
  </div>
</template>
<style lang="scss">
.favourite_wrap {
  background: #ffffff;
  border-radius: 36px;
  height: 90%;
  overflow: auto;
  padding: 24px;
  .page_title {
    font-size: 20px;
  }
}
.favourite {
  background: #ffffffbe;
  padding: 7px;
  color: rgb(238, 122, 161) !important;
  &:hover {
    background: #ffffff !important;
    color: rgb(238, 122, 161) !important;
  }
}
@media (max-width: 768px) {
  .detail_wrap {
    grid-template-columns: 1fr;
    grid-template-areas:
      "right"
      "left";
  }
}
</style>