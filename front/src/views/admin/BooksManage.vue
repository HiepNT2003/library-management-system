<script setup>
import { ref, onMounted, watch, computed } from "vue"
import { storeToRefs } from "pinia"
import { Icon } from "@iconify/vue"
import TabSelect from "../../components/share/TabSelect.vue"
import BooksManageTitle from "../../components/Admin/BooksManageTitle.vue"
import { useToastMessageStore } from "../../stores/toastMessage"
import { useAuthStore } from "../../stores/auth"
import { TOAST_MESSAGE_STATUS } from "../../constants"
import api from "../../services/api"
import { useRoute, useRouter } from "vue-router"
import BookCopyManagement from "../../components/Admin/BookCopyManagement.vue"

const router = useRouter()
const route = useRoute()
const tabOptions = ref([
  { id: 2, title: "Xem theo số ĐKCB" },
  { id: 1, title: "Xem theo đầu mục" },
])
const selectedTab = ref(tabOptions.value[0])
const documentTypes = ref([])
const selectedDocumentType = ref({})
const isShowDocumentType = ref(true)

onMounted(() => {
  fetchDocumentType()
})

async function fetchDocumentType() {
  const toasMessageStore = useToastMessageStore()
  const authStore = useAuthStore()
  authStore.setIsLoadingApi(true)
  try {
    const res = await api.get("/documentTypes")
    if (res.status == 200) {
      documentTypes.value = res.data
      selectedDocumentType.value = route.query?.documentTypeId
        ? documentTypes.value.find((type) => type.documentTypeId == route.query?.documentTypeId)
        : documentTypes.value.find((type) => type.documentTypeId == 1)
    }
  } catch (error) {
    toasMessageStore.showToastMessage(
      error?.response?.data?.message,
      TOAST_MESSAGE_STATUS.error,
      5000
    )
    authStore.setIsLoadingApi(false)
  }
  authStore.setIsLoadingApi(false)
}
function handleChangeTab(tab) {
  selectedTab.value = tab
  router.push({
    query: "",
  })
}
function onChangeDocumentType(type) {
  selectedDocumentType.value = type
  router.push({
    query: { documentTypeId: type.documentTypeId },
  })
}
function toogleMenuDocumentType() {
  isShowDocumentType.value = !isShowDocumentType.value
}
watch(
  () => route.query?.tab,
  (val) => {
    if (val) {
      const selectTab = tabOptions.value.find((tab) => tab.id == val)
       selectedTab.value = selectTab
    }
  },
  { immediate: true }
)
</script>
<template>
  <div class="book_management">
    <TabSelect
      class="tab_select"
      :selected="selectedTab"
      :options="tabOptions"
      @changeTab="handleChangeTab"
    />
    <div class="data_wrapper" :class="{ title_wrapper: selectedTab.id == 1 }">
      <div class="document_type" v-if="isShowDocumentType && selectedTab.id == 1">
        <div
          class="types"
          :class="{ active: selectedDocumentType.documentTypeId == type.documentTypeId }"
          v-for="type in documentTypes"
          :key="type.documentTypeId"
          @click="onChangeDocumentType(type)"
        >
          <span>{{ type.name }}</span
          ><span class="total">({{ type.totalBooks }})</span>
        </div>
      </div>
      <Icon
        v-if="selectedTab.id == 1"
        class="toogle_type"
        :class="[isShowDocumentType ? 'open' : 'close']"
        icon="eva:arrow-left-fill"
        width="24"
        height="24"
        @click="toogleMenuDocumentType"
      />
      <BooksManageTitle
        v-if="selectedTab.id == 1"
        :selectedDocumentType="selectedDocumentType"
        :isShowDocumentType="isShowDocumentType"
      />
      <BookCopyManagement v-else />
    </div>
  </div>
</template>
<style lang="scss" scoped>
.tab_select {
  margin-bottom: 12px;
}

.data_wrapper {
  &.title_wrapper {
    display: flex;
    gap: 4px;
    position: relative;
  }

  .document_type {
    width: 170px;
    flex: 1;
    padding: 16px 4px;
    background: #ffffff;
    border-radius: 0.7rem;
    font-size: 14px;
    margin-bottom: 4rem;

    .types {
      display: flex;
      gap: 4px;
      margin-top: 8px;
      cursor: pointer;
      padding: 4px 8px;
      border-radius: 4px;

      &:hover,
      &.active {
        background-color: #f0f1f5;
      }

      .total {
        font-weight: 700;
      }
    }
  }

  .toogle_type {
    position: absolute;
    top: 44%;
    height: 40px;
    width: 18px;
    color: #ffffff;
    background: #435ebe;
    border-top-left-radius: 8px;
    border-bottom-left-radius: 8px;
    cursor: pointer;

    &.open {
      left: 148px;
    }

    &.close {
      left: -5px;
      transform: rotate(180deg);
    }
  }
}
</style>
<style lang="scss"></style>