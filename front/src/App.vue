<script setup>
import { ref, onMounted, computed, watch } from "vue"
import Loading from "./components/Loading.vue"
import { useAuthStore } from "@/stores/auth"
import { useToastMessageStore } from "@/stores/toastMessage"
import LoadingApi from "./components/LoadingApi.vue"
import ToastMessage from "./components/ToastMessage.vue"

const isLoaded = ref(false)
const authStore = useAuthStore()
const toastMessageStore = useToastMessageStore()

onMounted(() => {
  if (document.readyState === "complete") {
    isLoaded.value = true
  } else {
    window.addEventListener("load", () => {
      setTimeout(() => {
        isLoaded.value = true
      }, 600)
    })
  }
})

const isLoadingApi = computed(() => authStore.getIsLoadingApi)
const toasMessageInfo = computed(() => toastMessageStore.getToastMessageInfo)
function setLoadedScreen(value) {
  isLoaded.value = value
}
</script>

<template>
  <div class="content-wrapper">
    <loading-api v-show="isLoadingApi" />
    <Loading v-show="!isLoaded" />
    <router-view @setLoaded="setLoadedScreen" v-show="isLoaded" />
    <div class="toast_wrap" v-show="toasMessageInfo.length">
      <ToastMessage v-for="toast in toasMessageInfo" :key="toast.id" :toast-message="toast" />
    </div>
  </div>
</template>
<style lang="scss" scoped>
.content-wrapper {
  height: 100%;
}
</style>
<style lang="scss">
@use "@/assets/scss/variables.scss" as V;

* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
  @include V.custom-scroll-bar;
}
</style>