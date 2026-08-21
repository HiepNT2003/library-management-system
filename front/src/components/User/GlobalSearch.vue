<script setup>
import { ref, onMounted, nextTick } from "vue"
import api from "../../services/api"
import { useAuthStore } from "@/stores/auth"
import { useToastMessageStore } from "../../stores/toastMessage"
import { TOAST_MESSAGE_STATUS } from "../../constants"
import { Icon } from "@iconify/vue"
import Select from "primevue/select"
import { useBookStore } from "../../stores/books"
import { storeToRefs } from 'pinia';

const bookStore = useBookStore()

const {
  getDocumentTypes: documentTypes,
} = storeToRefs(bookStore);

const books = ref([])
const isShowDocumentsType = ref(false)
const selectedDocumentsType = documentTypes.value[0]
const triggerRef = ref(null)
const dropdownRef = ref(null)

onMounted(async () => {
  const toasMessageStore = useToastMessageStore()
  const authStore = useAuthStore()
  authStore.setIsLoadingApi(true)
  try {
    const res = await api.get("/books")
    if (res.status == 200) {
      books.value = res.data
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
})

function toggleDropdownDocuments() {
  isShowDocumentsType.value = !isShowDocumentsType.value
  if (isShowDocumentsType.value) openDropdown()
}
const openDropdown = async () => {
  await nextTick()

  const pos = getDropdownPosition(triggerRef.value, dropdownRef.value)

  Object.assign(dropdownRef.value.style, {
    position: "fixed",
    top: pos.top + "px",
    left: pos.left + "px",
  })
}

function getDropdownPosition(triggerEl, dropdownEl) {
  const triggerRect = triggerEl.getBoundingClientRect()
  const dropdownRect = dropdownEl.getBoundingClientRect()

  const viewportWidth = window.innerWidth
  const viewportHeight = window.innerHeight

  let position = {
    top: 0,
    left: 0,
    placement: "bottom",
  }
  position.top = triggerRect.bottom
  position.left = triggerRect.left
  if (triggerRect.bottom + dropdownRect.height > viewportHeight) {
    position.top = triggerRect.top - dropdownRect.height
    position.placement = "top"
  }
  if (triggerRect.left + dropdownRect.width > viewportWidth) {
    position.left = viewportWidth - dropdownRect.width - 8 // padding 8px
  }
  if (position.left < 0) {
    position.left = 8
  }

  return position
}
</script>

<template>
  <div class="search-box search-input">
    <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <circle cx="11" cy="11" r="8" />
      <line x1="21" y1="21" x2="16.65" y2="16.65" />
    </svg>
    <input type="text" class="text_search" placeholder="Nhập nhan đề, tên tác giả" />
    <div class="document_type">
      <div class="dropdown_wrap">
        <Icon icon="proicons:document" class="document_icon" width="20" height="20" />
        <Select
          v-model="selectedDocumentsType"
          :options="documentTypes"
          optionLabel="name"
          placeholder="Chọn loại tài liệu"
          class="document-search w-full md:w-56"
        />
      </div>
    </div>
    <button class="btn-search">Tìm kiếm</button>
  </div>
</template>
<style lang="scss" scoped>
.search-box {
  width: 80%;
  padding: 8px 12px 8px 48px;
  border-radius: 36px;
  display: flex;
  gap: 8px;
  position: relative;
  background: #ffffff;
  .text_search {
    height: 36px;
    border: none;
    flex-grow: 1;
    color: #333333;
    &:focus-visible {
      outline: none;
    }
  }
  .document-search {
    padding: 8px 16px;
    border-radius: 36px;
    display: flex;
    gap: 12px;
    align-items: center;
    background: transparent;
    border: none;
    box-shadow: none;
    font-size: 16px;
    :deep(.p-select-label) {
      padding: 0;
      font-size: 14px;
      color: #333333;
      margin-left: 24px;
    }
    :deep(.p-select-dropdown) {
      width: 12px;
      svg {
        width: 12px;
        color: #333333;
      }
    }
  }
  .document_type {
    height: 36px;
    border-left: 1px solid #dcdcdc;
    border-right: 1px solid #dcdcdc;
    padding: 0 4px;
    .selected {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 6px;
    }
    .title {
      display: flex;
      flex: 1;
      margin: 0;
      min-width: 100px;
    }
    .dropdown_wrap {
      display: flex;
      gap: 6px;
      position: relative;
      cursor: pointer;
      border-radius: 36px;
      .document_icon {
        position: absolute;
        top: 25%;
        left: 14px;
      }
      &:hover {
        background: #f2f4f5;
      }
      .dropdown_list {
        position: absolute;
        top: 36px;
        background: #ffffff;
      }
    }
  }
  .btn-search {
    height: 36px;
    border-radius: 36px;
    padding: 6px 16px;
    color: #ffffff;
  }
}
</style>
