<script setup>
import { ref } from "vue"
import { storeToRefs } from "pinia"
import { useBookStore } from "../stores/books"
import Select from "primevue/select"

const bookStore = useBookStore()

const { getDocumentTypes: documentTypes } = storeToRefs(bookStore)

const isFavourite = ref(false)

function changeFavourite() {
  isFavourite.value = !isFavourite.value
}
</script>
<template>
  <div class="advanced_search_wrap">
    <div class="page_title">Tìm kiếm nâng cao</div>
    <div class="search_wrap">
      <div class="document_type">
        <div class="dropdown_wrap">
          <Select
            v-model="selectedDocumentsType"
            :options="documentTypes"
            optionLabel="name"
            placeholder="Chọn loại tài liệu"
            class="document-search w-full md:w-56"
          />
        </div>
      </div>
    </div>
  </div>
</template>
<style lang="scss">
.advanced_search_wrap {
  background: #ffffff;
  border-radius: 36px;
  height: 90%;
  overflow: auto;
  padding: 24px;
  .page_title {
    font-size: 20px;
    text-align: center;
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