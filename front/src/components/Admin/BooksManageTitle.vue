<script setup>
import { ref, onMounted, watch, computed } from "vue"
import debounce from "lodash/debounce"
import api from "../../services/api"
import { useAuthStore } from "@/stores/auth"
import { useToastMessageStore } from "../../stores/toastMessage"
import { TOAST_MESSAGE_STATUS } from "../../constants"
import ModalDelete from "./ModalDelete.vue"
import Pagination from "./Pagination.vue"
import NotFound from "../NotFound.vue"
import { Icon } from "@iconify/vue"
import { useRouter } from "vue-router"
import Menu from "primevue/menu"
import Button from "primevue/button"
import Checkbox from "primevue/checkbox"

const props = defineProps({
  selectedDocumentType: Array,
  isShowDocumentType: Boolean,
})

const books = ref([])
const listItemsPerPage = ref([5, 10, 15, 20, 25])
const commonField = ref([
  {
    id: 1,
    title: "Nhan đề",
    key: "title",
    canSort: true,
    width: "320px",
  },
  {
    id: 2,
    title: "Tác giả",
    key: "authors",
    canSort: true,
  },
  {
    id: 3,
    title: "Nhà xuất bản",
    key: "publisher",
    canSort: false,
  },
  {
    id: 4,
    title: "Năm xuất bản",
    key: "publishedYear",
    canSort: true,
  },
  {
    id: 5,
    title: "Thể loại",
    key: "categories",
    canSort: false,
  },
])
const bookField = ref([
  {
    id: 6,
    title: "Tổng số lượng",
    key: "totalCopies",
    canSort: true,
  },
  {
    id: 7,
    title: "Số lượng có sẵn",
    key: "availableCopies",
    canSort: true,
  },
  {
    id: 8,
    title: "Giá",
    key: "price",
    canSort: true,
  },
])
const articleField = ref([
  {
    id: 9,
    title: "Nguồn trích",
    key: "source",
    canSort: false,
  },
])
const thesisField = ref([
  {
    id: 11,
    title: "Trường",
    key: "university",
    canSort: false,
  },
  {
    id: 12,
    title: "Khoa",
    key: "faculty",
    canSort: false,
  },
  {
    id: 13,
    title: "Năm bảo vệ",
    key: "defenseYear",
    canSort: false,
  },
])
const ebookField = ref([
  {
    id: 14,
    title: "Dung lượng",
    key: "fileSize",
    canSort: false,
  },
])
const itemPerPage = ref(10)
const page = ref(1)
const totalPages = ref(0)
const totalRecord = ref(0)
const sortOrder = ref("")
const sortBy = ref("")
const searchKeyword = ref("")
const isShowModalDelete = ref(false)
const selectedBook = ref(null)
const router = useRouter()
const menu = ref()
const items = ref([
  {
    label: "Options",
    items: [
      {
        label: "Refresh",
        icon: "pi pi-refresh",
      },
      {
        label: "Export",
        icon: "pi pi-upload",
      },
    ],
  },
])
const checkAll = ref(false)

const listCheckedBooks = computed(() => books.value.filter((book) => book.isChecked))
const headerLabel = computed(() => {
  switch (props.selectedDocumentType.documentTypeId) {
    case 1:
      return [...commonField.value, ...bookField.value]
    case 2:
      return [...commonField.value, ...articleField.value]
    case 3:
      return [...commonField.value, ...thesisField.value]
    case 4:
      return [...commonField.value, ...ebookField.value]
    default:
      return commonField
  }
})

onMounted(async () => {
  await handleGetBooks(
    searchKeyword.value,
    page.value,
    itemPerPage.value,
    sortBy.value,
    sortOrder.value
  )
})
let controller = null
async function handleGetBooks(
  search,
  curentPage,
  currentItemPerPage,
  currentSortBy,
  currentSortOrder
) {
  if (controller) {
    controller.abort()
  }
  controller = new AbortController()
  const authStore = useAuthStore()
  authStore.setIsLoadingApi(true)
  try {
    const res = await api.get("/books", {
      params: {
        search,
        page: curentPage,
        pageSize: currentItemPerPage,
        sortBy: currentSortBy,
        sortOrder: currentSortOrder,
        documentTypeId: props.selectedDocumentType.documentTypeId,
      },
      signal: controller.signal,
    })
    if (res.status == 200) {
      books.value = res.data.data
      totalPages.value = res.data.meta.totalPages
      totalRecord.value = res.data.meta.totalRecords
    }
  } catch (error) {
    authStore.setIsLoadingApi(false)
  }
  authStore.setIsLoadingApi(false)
}
const toggle = (event) => {
  menu.value.toggle(event)
}
function handleChangeKeyword(event) {
  searchKeyword.value = event.target.value
}
function getCategoryName(categories) {
  if (!categories || categories.length === 0) return ""
  return categories.map((item) => item.name).join(", ")
}
function getAuthorName(authors) {
  if (!authors || authors.length === 0) return ""
  return authors.map((item) => item.name).join(", ")
}
function selectItemPerPage(event) {
  page.value = 1
  itemPerPage.value = event?.target?.value
}
function sortAsc(sort) {
  page.value = 1
  sortOrder.value = "asc"
  sortBy.value = sort.key
}
function sortDesc(sort) {
  page.value = 1
  sortOrder.value = "desc"
  sortBy.value = sort.key
}
function openModalDelete(book) {
  if (
    (props.selectedDocumentType.documentTypeId == 1 ||
      props.selectedDocumentType.documentTypeId == 3) &&
    book.totalCopies
  )
    return
  isShowModalDelete.value = true
  selectedBook.value = book
}
function closeModalDelete() {
  isShowModalDelete.value = false
  selectedBook.value = null
}
function editBook(book) {
  router.push({
    name: "bookDetail",
    params: { id: book.bookId },
  })
}
async function handleDeleteBook() {
  const authStore = useAuthStore()
  authStore.setIsLoadingApi(true)
  try {
    const res = await api.delete(`/books/${selectedBook.value.bookId}`)
    if (res.status == 200) {
      await handleGetBooks(
        searchKeyword.value,
        page.value,
        itemPerPage.value,
        sortBy.value,
        sortOrder.value
      )
    }
  } catch (error) {
    authStore.setIsLoadingApi(false)
  }
  authStore.setIsLoadingApi(false)
  closeModalDelete()
}
function handleChangePage(curPage) {
  page.value = curPage
  checkAll.value = false
  selectedBook.value = null
}
const debouncedSearch = debounce(async () => {
  page.value = 1
  await handleGetBooks(
    searchKeyword.value,
    page.value,
    itemPerPage.value,
    sortBy.value,
    sortOrder.value
  )
}, 500)

function handleRefreshFilter() {
  searchKeyword.value = ""
  itemPerPage.value = 10
  sortBy.value = ""
  sortOrder.value = ""
}

function onAddBook() {
  router.push({
    name: "bookDetail",
    params: { id: "new" },
    query: { documentType: props.selectedDocumentType?.documentTypeId },
  })
}

function formatFileSize(bytes) {
  return (bytes / (1024 * 1024)).toFixed(2) + " MB"
}

function formatVND(amount) {
  if (amount == null) return "0 ₫"

  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
  }).format(amount)
}

watch(searchKeyword, () => {
  debouncedSearch()
})
watch(
  [page, itemPerPage, sortBy, sortOrder],
  async ([newPage, newItemPerPage, newSortBy, newSortOrder]) => {
    await handleGetBooks(searchKeyword.value, newPage, newItemPerPage, newSortBy, newSortOrder)
  }
)
watch(checkAll, () => {
  books.value = books.value.map((item) => ({ ...item, isChecked: checkAll.value }))
})
watch(
  () => props.selectedDocumentType,
  async () => {
    searchKeyword.value = ""
    page.value = 1
    itemPerPage.value = 10
    sortBy.value = ""
    sortOrder.value = ""
    await handleGetBooks(
      searchKeyword.value,
      page.value,
      itemPerPage.value,
      sortBy.value,
      sortOrder.value
    )
  }
)
</script>
<template>
  <div class="page-heading" :class="{ show_sidebar: isShowDocumentType }">
    <section class="section">
      <div class="card">
        <div class="card-body">
          <div
            class="dataTable-wrapper dataTable-loading no-footer sortable searchable fixed-columns"
          >
            <div class="dataTable-top">
              <div class="dataTable-dropdown">
                <select
                  class="dataTable-selector form-select"
                  fdprocessedid="qpf37"
                  @change="selectItemPerPage($event)"
                >
                  <option
                    :value="value"
                    :selected="value == itemPerPage"
                    v-for="value in listItemsPerPage"
                    :key="value"
                  >
                    {{ value }}
                  </option></select
                ><label>sách trên trang</label>
              </div>
              <div class="dataTable-search">
                <input
                  class="book-search"
                  placeholder="Tìm kiếm theo tên sách hoặc ISBN..."
                  type="text"
                  v-model="searchKeyword"
                  @input="handleChangeKeyword"
                />
                <div class="btn-refresh">
                  <button class="btn btn-outline-secondary" @click="handleRefreshFilter">
                    <Icon icon="mi:refresh" width="24" height="24" />
                  </button>
                </div>
                <div class="btn-add">
                  <button class="btn icon icon-left btn-primary" @click="onAddBook">
                    <Icon icon="line-md:plus" width="24" height="24" />
                    Thêm mới
                  </button>
                </div>
              </div>
            </div>
            <div class="dataTable-container" v-if="books.length">
              <table class="table table-striped dataTable-table" id="table1">
                <thead>
                  <tr>
                    <th>
                      <!-- <Checkbox v-model="checkAll" binary /> -->
                      ID
                    </th>
                    <th v-for="title in headerLabel" :key="title.id">
                      <p class="table_header" :style="{ width: title.width }" v-if="!title.canSort">
                        {{ title.title }}
                      </p>
                      <div class="header_sort" :style="{ width: title.width }" v-else>
                        <p class="table_header">{{ title.title }}</p>
                        <span
                          class="sort-icon-up"
                          :class="{ active: sortOrder == 'asc' && sortBy == title.key }"
                          @click.stop="sortAsc(title)"
                        ></span>
                        <span
                          class="sort-icon-down"
                          :class="{ active: sortOrder == 'desc' && sortBy == title.key }"
                          @click.stop="sortDesc(title)"
                        ></span>
                      </div>
                    </th>
                    <th class="action-label sticky_right">Action</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="book in books" :key="book.id">
                    <td>
                      {{ book.bookId }}
                    </td>
                    <td v-for="field in headerLabel" :key="field.id">
                      <p class="data_title" v-if="field.key == 'authors'">
                        {{ getAuthorName(book.authors) }}
                      </p>
                      <p class="data_title" v-else-if="field.key == 'categories'">
                        {{ getCategoryName(book.categories) }}
                      </p>
                      <p v-else-if="field.key == 'price'">
                        {{ formatVND(book.price) }}
                      </p>
                      <p v-else-if="field.key == 'fileSize'">{{ formatFileSize(book.fileSize) }}</p>
                      <p class="data_title" v-else>{{ book[field.key] }}</p>
                    </td>
                    <td class="sticky_right">
                      <div class="action">
                        <span @click="editBook(book)" class="btn icon btn-primary"
                          ><i class="bi bi-pencil"></i></span
                        ><span
                          @click="openModalDelete(book)"
                          class="btn icon btn-danger"
                          :class="{
                            disable:
                              (selectedDocumentType.documentTypeId == 1 ||
                                selectedDocumentType.documentTypeId == 3) &&
                              book.totalCopies,
                          }"
                          ><i class="bi bi-trash"></i
                        ></span>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <NotFound v-else />
            <Pagination
              v-if="totalPages"
              :page="page"
              :item-per-page="itemPerPage"
              :total-record="totalRecord"
              :total-pages="totalPages"
              @changePage="handleChangePage"
            />
          </div>
        </div>
      </div>
    </section>
    <ModalDelete
      v-if="isShowModalDelete && selectedBook"
      :title="'Xác nhận xóa ?'"
      :description="selectedBook.title"
      @closeModal="closeModalDelete"
      @confirmDelete="handleDeleteBook"
    />
  </div>
</template>
<style lang="scss" scoped>
.page-heading {
  width: calc(100% - 16px);
  margin-left: 16px;
}
.show_sidebar {
  width: calc(100% - 170px);
  margin-left: 0;
}
.card-body {
  padding: 0.5rem 1rem;
}
.table {
  .table_header {
    margin-bottom: 0;
  }
  td:not(:first-child) {
    min-width: 200px;
  }

  .action-label {
    text-align: center;
  }

  .action {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    .btn {
      width: 28px;
      height: 28px;
      font-size: 14px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
    }
    .btn-danger {
      background: #6c757d;
      border: 1px solid #6c757d;
    }
    .disable {
      opacity: 0.5;
      cursor: not-allowed;
    }
  }

  .header_sort {
    position: relative;
  }

  .sort-icon-up::before,
  .sort-icon-down::after {
    content: "";
    height: 0;
    width: 0;
    position: absolute;
    right: 4px;
    border-left: 4px solid transparent;
    border-right: 4px solid transparent;
    opacity: 0.2;
  }

  .sort-icon-up,
  .sort-icon-down {
    cursor: pointer;

    &.active::before,
    &.active::after {
      opacity: 0.6;
    }
  }

  .sort-icon-up::before {
    border-top: 4px solid #000;
    bottom: 0px;
  }

  .sort-icon-down::after {
    border-bottom: 4px solid #000;
    border-top: 4px solid transparent;
    top: 0px;
  }
  .data_title {
    display: -webkit-inline-box;
    -webkit-line-clamp: 1;
    -webkit-box-orient: vertical;
    text-overflow: ellipsis;
    overflow: hidden;
    margin-bottom: 0;
  }
  tbody td {
    font-size: 14px;
  }
}

.dataTable-top {
  margin-bottom: 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 8px;
  .dataTable-dropdown {
    display: flex;
    align-items: center;
    justify-content: center;
    .form-select {
      padding: 0.3rem 1.6rem 0.3rem 0.5rem;
      font-size: 14px;
      background-position: right 0.3rem center;
      width: fit-content;
      &:focus {
        box-shadow: none;
      }
    }
    label {
      font-size: 14px;
      margin-left: 8px;
    }
  }
}

.dataTable-search {
  display: flex;
  gap: 8px;
  align-items: center;

  .btn-refresh {
    height: 100%;

    button {
      width: 34px;
      height: 34px;
      padding: 0;
      padding-bottom: 3px;
      border-color: #dce7f1;
    }
  }
  .btn-add button {
    align-items: center;
    justify-content: center;
    display: flex;
    height: 34px;
  }
  .other_menu {
    .button_other {
      width: 34px;
      height: 34px;
      background: #6c757d;
      border: 1px solid #dcdcdc;
      color: #ffffff;
      &:focus {
        outline: none;
      }
    }
  }
}

.sticky_right {
  position: sticky;
  right: 0;
  background: #ffffff;
  min-width: 120px !important;
}

.book-search {
  border: 1px solid #dce7f1;
  height: 34px;
  border-radius: 8px;
  padding: 8px;
  width: 300px;
  background: transparent;
  color: #333333;
  font-size: 14px;
  &:focus-visible {
    outline: none;
  }
}

.dataTable-container {
  &::-webkit-scrollbar-track:vertical {
    background-color: transparent;
    position: relative;
  }

  &::-webkit-scrollbar {
    width: 8px;
    height: 8px;
    position: relative;
  }

  &::-webkit-scrollbar-thumb {
    border-radius: 4px;
    background-color: #8f8e8e;
  }
}
.inpt_checkbox {
  width: 16px;
  height: 16px;
  background: transparent;
}
</style>
<style lang="scss">
.p-menu {
  background: #ffffff !important;
  color: #333333 !important;
  border: 1px solid #dcdcdc !important;
  min-width: 10rem !important;
  .p-menu-submenu-label {
    color: #333333;
    font-size: 14px;
  }
  .p-menu-item-content {
    color: #333333;
    font-size: 14px;
    a:hover {
      color: #ffffff;
    }
  }
}
.p-checkbox {
  align-items: center;
  .p-checkbox-box {
    background: transparent;
    width: 16px;
    height: 16px;
  }
  &:has(.p-checkbox-input:hover) .p-checkbox-box {
    border-color: #435ebe !important;
  }
}
.p-checkbox-checked {
  .p-checkbox-box {
    border-color: #435ebe !important;
    background: #435ebe !important;
    .p-checkbox-icon {
      color: #ffffff;
      width: 10px;
    }
  }
  &:has(.p-checkbox-input:hover) .p-checkbox-box {
    .p-checkbox-icon {
      color: #ffffff !important;
    }
  }
}
</style>