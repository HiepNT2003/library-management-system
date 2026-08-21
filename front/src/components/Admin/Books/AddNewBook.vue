<script setup>
import { ref, onMounted, computed, watch } from "vue"
import { QuillEditor } from "@vueup/vue-quill"
import "@vueup/vue-quill/dist/vue-quill.bubble.css"
import debounce from "lodash/debounce"
import MultiSelectDropdown from "../../../components/share/MultiSelectDropdown.vue"
import MultiSelect from "../../../components/share/MultiSelect.vue"
import api from "../../../services/api"
import ModalAddAuthor from "../../../components/Admin/ModalAddAuthor.vue"
import ModalAddCategory from "../../../components/Admin/ModalAddCategory.vue"
import EmptyImage from "../../../components/share/EmptyImage.vue"
import _ from "lodash"
import { useToastMessageStore } from "../../../stores/toastMessage"
import { useAuthStore } from "../../../stores/auth"
import {
  ARTICLE_FIELD,
  BOOK_FIELD,
  COMMON_FIELD,
  EBOOK_FIELD,
  THESIS_FIELD,
  TOAST_MESSAGE_STATUS,
} from "../../../constants"
import InputTreeDropdown from "../../../components/share/DropdownTree/InputTreeDropdown.vue"
import InputGroup from "primevue/inputgroup"
import InputGroupAddon from "primevue/inputgroupaddon"
import InputNumber from "primevue/inputnumber"
import ModalAddLanguage from "../../../components/Admin/ModalAddLanguage.vue"
import EbookReader from "../../share/EbookReader.vue"
import ToggleSwitch from "primevue/toggleswitch"
import { useRouter } from "vue-router"

const router = useRouter()

const props = defineProps({
  documentType: Number,
  detailBook: Object,
})
const emit = defineEmits(["on:changeStep"])

const isShowModalAddAuthor = ref(false)
const isShowModalAddCategory = ref(false)
const isShowModalAddLanguage = ref(false)
const maxSelectAuthor = 5
const maxSelectCategory = 5
const maxSelectLanguage = 3

const commonField = ref(_.cloneDeep(COMMON_FIELD))
const bookField = ref(_.cloneDeep(BOOK_FIELD))
const articleField = ref(_.cloneDeep(ARTICLE_FIELD))
const thesisField = ref(_.cloneDeep(THESIS_FIELD))
const ebookField = ref(_.cloneDeep(EBOOK_FIELD))
const ddcTree = ref({})
const listAuthorSuggestions = ref([])
const listCategories = ref([])
const languages = ref([])
const errorDDC = ref("")
const errorAuthor = ref("")
const errorTitle = ref("")
const errorSourceTitle = ref("")
const errorUniversity = ref("")
const errorFilePath = ref("")

const imageFile = ref(null)
const previewUrl = ref("")
const ebookFile = ref(null)
const previewEbook = ref({})
const viewEbook = ref(false)

const isEditBook = computed(() => (props.detailBook?.bookId ? true : false))
const authorSelectError = computed(() =>
  commonField.value.authors.length > maxSelectAuthor ? "Chọn tối đa 5 tác giả" : ""
)
const categorySelectError = computed(() =>
  commonField.value.categories.length > maxSelectCategory ? "Chọn tối đa 5 danh mục" : ""
)
const languageSelectError = computed(() =>
  commonField.value.languages.length > maxSelectLanguage ? "Chọn tối đa 3 ngôn ngữ" : ""
)
const rangePageError = computed(() => {
  return articleField.value.startPage > articleField.value.endPage ? "Nhập sai khoảng" : ""
})
const pageTitle = computed(() => {
  switch (props.documentType) {
    case "1":
      return `${isEditBook.value ? "Sửa thông tin" : "Thêm mới"} sách`
    case "2":
      return `${isEditBook.value ? "Sửa thông tin" : "Thêm mới"} bài trích`
    case "3":
      return `${isEditBook.value ? "Sửa thông tin" : "Thêm mới"} tài liệu luận án, luận văn`
    case "4":
      return `${isEditBook.value ? "Sửa thông tin" : "Thêm mới"} sách điện tử`
    default:
      return ""
  }
})

const downloadBook = async (id, title) => {
  const authStore = useAuthStore()
  authStore.setIsLoadingApi(true)
  try {
    const res = await api.get(`/upload/download/${id}`, {
      responseType: "blob",
    })
    const url = window.URL.createObjectURL(new Blob([res.data]))
    const link = document.createElement("a")
    link.href = url
    link.setAttribute("download", `${title}.pdf`)
    document.body.appendChild(link)
    link.click()
    link.remove()
    window.URL.revokeObjectURL(url)
  } catch (err) {
    const msg = err.response?.data?.message || "Tải thất bại"
    alert(msg)
  }
  authStore.setIsLoadingApi(false)
}

async function fetchDDCTree() {
  const toasMessageStore = useToastMessageStore()
  try {
    const res = await api.get("/DDC/tree")
    if (res.status == 200) {
      ddcTree.value = res.data
    }
  } catch (error) {
    toasMessageStore.showToastMessage(
      error?.response?.data?.message,
      TOAST_MESSAGE_STATUS.error,
      5000
    )
  }
}
async function fetchLanguages() {
  const toasMessageStore = useToastMessageStore()
  try {
    const res = await api.get("/language")
    if (res.status == 200) {
      languages.value = res.data.map((lang) => ({
        languageId: lang.languageId,
        code: lang.code,
        name: lang.name,
      }))
    }
  } catch (error) {
    toasMessageStore.showToastMessage(
      error?.response?.data?.message,
      TOAST_MESSAGE_STATUS.error,
      5000
    )
  }
}
const handleSelectDDC = (ddc) => {
  commonField.value.ddc = ddc
}
const handleEbookFileChange = (e) => {
  const selectedFile = e.target.files[0]
  if (!selectedFile) return

  const allowedExtensions = ["pdf", "epub"]
  const ext = selectedFile.name.split(".").pop().toLowerCase()

  if (!allowedExtensions.includes(ext)) {
    errorFilePath.value = "Chỉ cho phép file PDF hoặc EPUB"
    e.target.value = ""
    return
  }

  if (selectedFile.size > 50 * 1024 * 1024) {
    errorFilePath.value = "File phải nhỏ hơn 50MB"
    e.target.value = "" // reset input
    return
  }

  ebookFile.value = selectedFile
  previewEbook.value.fileName = selectedFile.name
  previewEbook.value.fileSize = (selectedFile.size / 1024 / 1024).toFixed(2)
}
const handleUploadEbookFile = async () => {
  if (!ebookFile.value) return null

  const formData = new FormData()
  formData.append("file", ebookFile.value)

  try {
    const res = await api.post("/upload/file", formData)

    ebookField.value.filePath = res.data.filePath
    ebookField.value.fileSize = res.data.fileSize

    return res.data.filePath
  } catch (error) {
    errorFilePath.value = "Upload thất bại"
    return null
  }
}
const handleFileChange = (e) => {
  const selectedFile = e.target.files[0]
  if (!selectedFile) return
  imageFile.value = selectedFile
  if (previewUrl.value) {
    URL.revokeObjectURL(previewUrl.value)
  }
  previewUrl.value = URL.createObjectURL(selectedFile)
}
const uploadImage = async () => {
  if (!imageFile.value) return null

  const formData = new FormData()
  formData.append("file", imageFile.value)
  formData.append("type", "book")

  const res = await api.post("/upload", formData, {
    headers: {
      "Content-Type": "multipart/form-data",
    },
  })

  commonField.value.imageUrl = res.data.url

  return commonField.value.imageUrl
}
const submitBook = () => {
  errorTitle.value = !commonField.value.title ? "Vui lòng nhập tên sách" : ""
  errorDDC.value = !Object.keys(commonField.value.ddc).length ? "Vui lòng chọn DDC" : ""
  errorAuthor.value = !commonField.value.authors.length
    ? "Vui lòng chọn tác giả"
    : authorSelectError.value
    ? authorSelectError.value
    : ""
  if (props.documentType == 2) {
    errorSourceTitle.value = !articleField.value.source ? "Vui lòng nhập nguồn trích" : ""
  }
  if (props.documentType == 3) {
    errorUniversity.value = !thesisField.value.university ? "Vui lòng nhập tên trường " : ""
  }
  if (props.documentType == 4) {
    errorFilePath.value =
      !previewEbook.value?.fileName && !ebookField.value.filePath ? "Vui lòng nhập đường dẫn" : ""
  }
  if (
    errorTitle.value ||
    errorDDC.value ||
    errorAuthor.value ||
    errorSourceTitle.value ||
    errorUniversity.value ||
    errorFilePath.value ||
    rangePageError.value ||
    categorySelectError.value ||
    languageSelectError.value
  )
    return
  if (isEditBook.value) {
    updateBook()
  } else {
    createBook()
  }
}
const createBook = async () => {
  const toasMessageStore = useToastMessageStore()
  const authStore = useAuthStore()
  authStore.setIsLoadingApi(true)
  try {
    const uploadedImage = await uploadImage()
    const params = {
      title: commonField.value?.title?.trim() || "",
      publisher: commonField.value.publisher?.trim() || null,
      publishedYear: commonField.value?.publishedYear || null,
      description: commonField.value.description?.trim() || null,
      imageUrl: uploadedImage,
      documentTypeId: props.documentType,
      DDCCode: commonField.value.ddc.code,
      authorIds: commonField.value.authors,
      categoryIds: commonField.value.categories,
      languageIds: commonField.value.languages,
    }
    if (props.documentType != 2 && ebookFile.value) {
      await handleUploadEbookFile()
      params.filePath = ebookField.value.filePath
      params.fileSize = ebookField.value.fileSize
      params.isPublic = ebookField.value.isPublic
    }
    let res = {}
    if (props.documentType == 1) {
      params.isbn = bookField.value.isbn?.trim() || null
      params.totalPages = bookField.value.totalPages
      params.isBorrowable = bookField.value.isBorrowable
      params.price = bookField.value.price
      res = await api.post("/books/book", params)
    }
    if (props.documentType == 2) {
      params.source = articleField.value.source
      params.startPage = articleField.value.startPage
      params.endPage = articleField.value.endPage
      res = await api.post("/books/article", params)
    }
    if (props.documentType == 3) {
      params.university = thesisField.value.university
      params.faculty = thesisField.value.faculty
      params.advisor = thesisField.value.advisor
      params.degree = thesisField.value.degree
      params.defenseYear = thesisField.value.defenseYear
      res = await api.post("/books/thesis", params)
    }
    if (props.documentType == 4) {
      res = await api.post("/books/ebook", params)
    }

    if (res.status == 200 || res.status == 201) {
      toasMessageStore.showToastMessage(res?.data?.message, TOAST_MESSAGE_STATUS.success, 3000)
      authStore.setIsLoadingApi(false)
      router.push({
        name: "bookDetail",
        params: { id: res.data },
      })
    } else {
      toasMessageStore.showToastMessage(res?.data?.message, TOAST_MESSAGE_STATUS.error)
      authStore.setIsLoadingApi(false)
    }
  } catch (error) {
    toasMessageStore.showToastMessage(
      error?.response?.data?.message,
      TOAST_MESSAGE_STATUS.error,
      5000
    )
    authStore.setIsLoadingApi(false)
  }
}
const updateBook = async () => {
  const toasMessageStore = useToastMessageStore()
  const authStore = useAuthStore()
  authStore.setIsLoadingApi(true)
  try {
    const uploadedImage = await uploadImage()
    const params = {
      title: commonField.value?.title?.trim() || "",
      publisher: commonField.value.publisher?.trim() || null,
      publishedYear: commonField.value?.publishedYear || null,
      description: commonField.value.description?.trim() || null,
      imageUrl: uploadedImage ? uploadedImage : commonField.value?.imageUrl,
      documentTypeId: Number(props.documentType),
      DDCCode: commonField.value.ddc.code,
      authorIds: commonField.value.authors,
      categoryIds: commonField.value.categories,
      languageIds: commonField.value.languages,
    }
    if (props.documentType != 2 && ebookFile.value) {
      await handleUploadEbookFile()
      params.filePath = ebookField.value.filePath
      params.fileSize = ebookField.value.fileSize
      params.isPublic = ebookField.value.isPublic
    }
    let res = {}
    if (props.documentType == 1) {
      params.isbn = bookField.value.isbn?.trim() || null
      params.totalPages = bookField.value.totalPages
      params.isBorrowable = bookField.value.isBorrowable
      params.price = bookField.value.price
      res = await api.put(`/books/${props.detailBook?.bookId}`, params)
    }
    if (props.documentType == 2) {
      params.source = articleField.value.source
      params.startPage = articleField.value.startPage
      params.endPage = articleField.value.endPage
      res = await api.put(`/books/${props.detailBook?.bookId}`, params)
    }
    if (props.documentType == 3) {
      params.university = thesisField.value.university
      params.faculty = thesisField.value.faculty
      params.advisor = thesisField.value.advisor
      params.degree = thesisField.value.degree
      params.defenseYear = thesisField.value.defenseYear
      res = await api.put(`/books/${props.detailBook?.bookId}`, params)
    }
    if (props.documentType == 4) {
      params.filePath = ebookField.value.filePath
      params.fileSize = ebookField.value.fileSize
      params.isPublic = ebookField.value.isPublic
      res = await api.put(`/books/${props.detailBook?.bookId}`, params)
    }

    if (res.status == 200 || res.status == 201) {
      toasMessageStore.showToastMessage(res?.data?.message, TOAST_MESSAGE_STATUS.success, 3000)
      authStore.setIsLoadingApi(false)
    } else {
      toasMessageStore.showToastMessage(res?.data?.message, TOAST_MESSAGE_STATUS.error)
      authStore.setIsLoadingApi(false)
    }
  } catch (error) {
    toasMessageStore.showToastMessage(
      error?.response?.data?.message,
      TOAST_MESSAGE_STATUS.error,
      5000
    )
    authStore.setIsLoadingApi(false)
  }
}
const searchAuthor = debounce(async (value) => {
  const res = await api.get("/authors", {
    params: { search: value },
  })
  listAuthorSuggestions.value = res.data
}, 300)

const getListCategories = async () => {
  const res = await api.get("/categories")
  listCategories.value = res.data
}

const handleSelectAuthor = (val) => {
  commonField.value.authors = val
}
const handleSelectCategory = (val) => {
  commonField.value.categories = val
}
const handleSelectLanguage = (val) => {
  commonField.value.languages = val
}
const toogleModalAuthor = (val) => {
  isShowModalAddAuthor.value = val
}
const toogleModalCategory = (val) => {
  isShowModalAddCategory.value = val
}
const toogleModalLanguage = (val) => {
  isShowModalAddLanguage.value = val
}
const handleBack = () => {
  router.push({
    name: "booksManage",
    query: { documentTypeId: props.documentType, tab: 1 },
  })
}
const resetData = () => {
  commonField.value = isEditBook.value
    ? {
        ..._.cloneDeep(COMMON_FIELD),
        ...props.detailBook,
      }
    : _.cloneDeep(COMMON_FIELD)
  bookField.value =
    isEditBook.value && props.detailBook.documentTypeId == 1
      ? { ..._.cloneDeep(BOOK_FIELD), ...props.detailBook }
      : _.cloneDeep(BOOK_FIELD)
  articleField.value =
    isEditBook.value && props.detailBook.documentTypeId == 2
      ? { ..._.cloneDeep(ARTICLE_FIELD), ...props.detailBook }
      : _.cloneDeep(ARTICLE_FIELD)
  thesisField.value =
    isEditBook.value && props.detailBook.documentTypeId == 3
      ? { ..._.cloneDeep(THESIS_FIELD), ...props.detailBook }
      : _.cloneDeep(THESIS_FIELD)
  ebookField.value =
    isEditBook.value && props.detailBook.documentTypeId != 2
      ? { ..._.cloneDeep(EBOOK_FIELD), ...props.detailBook }
      : _.cloneDeep(EBOOK_FIELD)
  ddcTree.value = {}
  listAuthorSuggestions.value = []
  listCategories.value = []
  languages.value = []
  errorDDC.value = ""
  errorAuthor.value = ""
  errorTitle.value = ""
  errorSourceTitle.value = ""
  errorUniversity.value = ""
  errorFilePath.value = ""

  imageFile.value = null
  previewUrl.value = ""
  ebookFile.value = null
}
watch(
  () => props.detailBook,
  (val) => {
    if (val.bookId) {
      resetData()
    }
  },
  { deep: true }
)
onMounted(async () => {
  resetData()
  const authStore = useAuthStore()
  authStore.setIsLoadingApi(true)
  try {
    await Promise.all([searchAuthor(), getListCategories(), fetchDDCTree(), fetchLanguages()])
  } catch (error) {
    authStore.setIsLoadingApi(false)
  }
  authStore.setIsLoadingApi(false)
})
</script>
<template>
  <div class="match-height">
    <div class="col-12" v-if="!viewEbook">
      <div class="card">
        <div class="card-header">
          <h4 class="card-title">{{ pageTitle }}</h4>
        </div>
        <div class="card-content">
          <div class="card-body">
            <div class="book_form">
              <div class="book_img">
                <img
                  v-if="previewUrl || commonField.imageUrl"
                  :src="previewUrl ? previewUrl : commonField.imageUrl"
                  width="200"
                />
                <label class="upload-book">
                  <EmptyImage v-if="!previewUrl && !(isEditBook && commonField.imageUrl)" />
                  <p class="choose_img">Chọn ảnh</p>
                  <input type="file" accept="image/*" @change="handleFileChange" hidden />
                </label>
              </div>

              <div class="book_info">
                <p class="field_title">Thông tin cơ bản</p>
                <div class="field">
                  <label>Nhan đề <span class="required">*</span></label>
                  <input
                    :class="{ error_field: errorTitle }"
                    type="text"
                    v-model="commonField.title"
                    class="input_text"
                    name="title"
                    placeholder="Nhập nhan đề"
                  />
                  <p class="error" v-if="errorTitle">{{ errorTitle }}</p>
                </div>
                <div class="field">
                  <label>DDC<span class="required">*</span></label>
                  <InputTreeDropdown
                    v-if="ddcTree.length"
                    :value="commonField.ddc"
                    :class="{ error_ddc: errorDDC }"
                    :ddcTree="ddcTree"
                    @update:ddcVal="handleSelectDDC"
                  />
                  <p class="error" v-if="errorDDC">{{ errorDDC }}</p>
                </div>
                <div class="field">
                  <label>Tác giả<span class="required">*</span></label>
                  <MultiSelect
                    v-model="commonField.authors"
                    :options="
                      listAuthorSuggestions.map((item) => ({
                        name: item.name,
                        value: item.authorId,
                      }))
                    "
                    :min="1"
                    :max="maxSelectAuthor"
                    :errorMessage="authorSelectError || errorAuthor"
                    placeholder="Chọn tác giả..."
                    @update:modelValue="handleSelectAuthor"
                    @add-new="isShowModalAddAuthor = true"
                  />
                </div>
                <div class="field">
                  <label>Danh mục</label>
                  <MultiSelect
                    v-model="commonField.categories"
                    :options="
                      listCategories.map((item) => ({ name: item.name, value: item.categoryId }))
                    "
                    :min="0"
                    :max="maxSelectCategory"
                    placeholder="Chọn danh mục..."
                    @update:modelValue="handleSelectCategory"
                    @add-new="isShowModalAddCategory = true"
                  />
                </div>
                <div class="field">
                  <label>Ngôn Ngữ</label>
                  <MultiSelect
                    v-model="commonField.languages"
                    :options="
                      languages.map((item) => ({ name: item.name, value: item.languageId }))
                    "
                    :min="0"
                    :max="maxSelectLanguage"
                    placeholder="Chọn ngôn ngữ..."
                    @update:modelValue="handleSelectLanguage"
                    @add-new="isShowModalAddLanguage = true"
                  />
                </div>
              </div>
            </div>
            <div class="book_other_info">
              <p class="field_title">Thông tin riêng</p>
              <div class="other_field_wrap">
                <template v-if="documentType == 1">
                  <div class="field switch_field">
                    <label>Cho phép mượn</label>
                    <ToggleSwitch v-model="bookField.isBorrowable" />
                  </div>
                  <div class="field">
                    <label>ISBN</label>
                    <input
                      type="text"
                      v-model="bookField.isbn"
                      class="input_text"
                      name="ISBN"
                      placeholder="ISBN"
                    />
                  </div>
                  <div class="field">
                    <label>Tổng số trang</label>
                    <InputGroup class="sm:!w-96">
                      <InputNumber
                        v-model="bookField.totalPages"
                        placeholder="Tổng số trang"
                        inputId="minmaxfraction"
                        :minFractionDigits="0"
                        :maxFractionDigits="0"
                        fluid
                      />
                      <InputGroupAddon>Trang</InputGroupAddon>
                    </InputGroup>
                  </div>
                  <div class="field">
                    <label>Giá bìa</label>
                    <InputGroup class="sm:!w-96">
                      <InputNumber
                        v-model="bookField.price"
                        placeholder="Giá bìa"
                        inputId="minmaxfraction"
                        :minFractionDigits="0"
                        :maxFractionDigits="3"
                        fluid
                      />
                      <InputGroupAddon>VNĐ</InputGroupAddon>
                    </InputGroup>
                  </div>
                </template>
                <template v-if="documentType == 2">
                  <div class="field">
                    <label>Nguồn trích<span class="required">*</span></label>
                    <input
                      :class="{ error_field: errorSourceTitle }"
                      type="text"
                      v-model="articleField.source"
                      class="input_text"
                      placeholder="Nhập nguồn trích"
                    />
                    <p class="error" v-if="errorSourceTitle">{{ errorSourceTitle }}</p>
                  </div>
                  <div class="field">
                    <label>Khoảng trang</label>
                    <div class="page_range">
                      <div class="ipt_wrap">
                        <InputNumber
                          v-model="articleField.startPage"
                          placeholder="Từ"
                          inputId="minmaxfraction"
                          :minFractionDigits="0"
                          :maxFractionDigits="0"
                          fluid
                        />
                      </div>
                      -
                      <div class="ipt_wrap">
                        <InputNumber
                          v-model="articleField.endPage"
                          placeholder="Đến"
                          inputId="minmaxfraction"
                          :minFractionDigits="0"
                          :maxFractionDigits="0"
                          fluid
                        />
                      </div>
                    </div>
                    <p class="error" v-if="rangePageError">{{ rangePageError }}</p>
                  </div>
                </template>
                <template v-if="documentType == 3">
                  <div class="field">
                    <label>Trường<span class="required">*</span></label>
                    <input
                      :class="{ error_field: errorUniversity }"
                      type="text"
                      v-model="thesisField.university"
                      class="input_text"
                      placeholder="Nhập tên trường"
                    />
                    <p class="error" v-if="errorUniversity">{{ errorUniversity }}</p>
                  </div>
                  <div class="field">
                    <label>Chuyên ngành</label>
                    <input
                      type="text"
                      v-model="thesisField.faculty"
                      class="input_text"
                      placeholder="Nhập chuyên ngành"
                    />
                  </div>
                  <div class="field">
                    <label>Giảng viên hướng dẫn</label>
                    <input
                      type="text"
                      v-model="thesisField.advisor"
                      class="input_text"
                      placeholder="Nhập giảng viên hướng dẫn"
                    />
                  </div>
                  <div class="field">
                    <label>Bằng cấp</label>
                    <input
                      type="text"
                      v-model="thesisField.degree"
                      class="input_text"
                      placeholder="Nhập bằng cấp"
                    />
                  </div>
                  <div class="field">
                    <label>Năm bảo vệ</label>
                    <input
                      type="number"
                      v-model="thesisField.defenseYear"
                      class="input_text"
                      placeholder="Năm bảo vệ"
                    />
                  </div>
                </template>
                <template v-if="documentType != 2">
                  <div class="field switch_field">
                    <label>Công khai PDF</label>
                    <ToggleSwitch v-model="ebookField.isPublic" />
                  </div>
                  <div class="field ebook_field">
                    <label
                      >Tải lên file {{ documentType == 4 ? "ebook" : "PDF"
                      }}<span v-if="documentType == 4" class="required">*</span></label
                    >
                    <div v-if="isEditBook && ebookField.filePath">
                      <p>📄 {{ "Ebook file" }} ({{ ebookField.fileSize }} MB)</p>
                      <button class="view_file" @click="viewEbook = true">Xem file</button>
                      <button
                        class="btn-download"
                        @click="downloadBook(commonField.bookId, commonField.title)"
                      >
                        Download
                      </button>
                    </div>
                    <input type="file" accept=".pdf,.epub" @change="handleEbookFileChange" v-else />
                    <p class="error" v-if="errorFilePath">{{ errorFilePath }}</p>
                  </div>
                </template>
                <div class="field">
                  <label>Nhà xuất bản</label>
                  <input
                    type="text"
                    v-model="commonField.publisher"
                    class="input_text"
                    name="publisher"
                    placeholder="Nhà xuất bản"
                  />
                </div>
                <div class="field">
                  <label>Năm xuất bản</label>
                  <InputNumber
                    v-model="commonField.publishedYear"
                    placeholder="Năm xuất bản"
                    inputId="withoutgrouping"
                    :useGrouping="false"
                    :max="3000"
                    fluid
                  />
                </div>
              </div>
              <div class="field">
                <label>Mô tả</label>
                <QuillEditor
                  v-model:content="commonField.description"
                  contentType="html"
                  theme="bubble"
                  style="height: 150px; border: 1px solid #dce7f1; border-radius: 0.25rem"
                />
              </div>
            </div>
            <div class="col-sm-12 d-flex justify-content-end action_group">
              <button
                type="submit"
                class="btn btn-primary me-1 mb-1"
                @click="submitBook"
                v-if="isEditBook"
              >
                Cập nhật
              </button>
              <button type="submit" class="btn btn-primary me-1 mb-1" @click="submitBook" v-else>
                Thêm mới
              </button>
              <button type="reset" class="btn btn-light-secondary me-1 mb-1" @click="handleBack">
                Trở lại
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
    <EbookReader
      v-if="ebookField.filePath && viewEbook"
      :fileUrl="ebookField.filePath"
      :book-title="commonField.title"
      :save-progress="false"
      :default-fullscreen="true"
      @on:closePDF="viewEbook = false"
    />
    <ModalAddAuthor
      :isShowModalAddAuthor="isShowModalAddAuthor"
      @on:toogleModal="toogleModalAuthor"
      @update:listAuthors="searchAuthor"
    />
    <ModalAddCategory
      :isShowModalAddCategory="isShowModalAddCategory"
      @on:toogleModal="toogleModalCategory"
      @update:listCategories="getListCategories"
    />
    <ModalAddLanguage
      :isShowModalAddLanguage="isShowModalAddLanguage"
      @on:toogleModal="toogleModalLanguage"
      @update:listLanguages="fetchLanguages"
    />
  </div>
</template>
<style lang="scss" scoped>
.card-content {
  margin-bottom: 8px;

  .error {
    font-size: 12px;
    color: red;
  }

  .error_field {
    border: 1px solid rgb(252, 91, 91) !important;
  }

  .field_title {
    font-size: 16px;
    font-weight: 700;
    text-decoration: underline;
  }

  .view_file {
    color: #a855f7;
    border: none;
    padding: 4px 8px;
    border-radius: 8px;
    background: transparent;
    margin-right: 4px;

    &:hover {
      background: #faf5ff;
    }
  }

  .book_form {
    margin-bottom: 24px;

    .error_ddc {
      :deep(.container) {
        border: 1px solid rgb(252, 91, 91) !important;
      }
    }
  }

  .book_form,
  .other_field_wrap {
    display: grid;
    grid-template-columns: 1fr 3fr;
    gap: 12px;

    .book_img {
      display: flex;
      flex-direction: column;
      text-align: center;
      align-items: center;

      img {
        display: block;
        margin-bottom: 4px;
      }
    }

    .upload-book {
      text-align: center;
      margin-top: 48px;

      .choose_img {
        text-align: center;
        cursor: pointer;
        margin-bottom: 0;
      }
    }

    .other_field_wrap {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
    }

    .field {
      margin-bottom: 10px;

      label {
        color: #333333;
        margin-bottom: 2px;
      }

      input {
        height: 40px;
      }

      .page_range {
        display: flex;
        gap: 8px;
        align-items: center;

        .ipt_wrap {
          width: 120px;
        }
      }
    }

    .ebook_field {
      display: flex;
      flex-direction: column;

      input {
        width: fit-content;
        cursor: pointer;
      }
      .btn-download {
        background: #fff;
        color: #3949ab;
        border: 1.5px solid #3949ab;
        border: none;
        padding: 4px 8px;
        border-radius: 8px;
        &:hover:not(:disabled) {
          background: #e8eaf6;
        }
        &:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }
      }
    }

    .switch_field {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .document_type {
      label {
        margin-right: 12px;
      }
    }
  }
}

.upload-btn {
  display: inline-block;
  padding: 0.375rem 0.75rem;
  background: #6c757d;
  color: white;
  border-radius: 6px;
  cursor: pointer;
  transition: 0.2s;
}

.upload-btn:hover {
  background: #6c757d;
}

.required {
  color: red;
}

.action_group {
  margin-top: 20px;
}
</style>

<style lang="scss">
.p-select {
  background: #ffffff !important;
  box-shadow: none !important;
  height: 38px;
  width: 100%;
}

.p-select-label {
  padding: 0 !important;
  color: #607080 !important;
}

.p-select-list-container {
  .p-select-option {
    color: #333333;

    &:hover {
      color: #333333 !important;
    }
  }
}

.p-inputgroup {
  height: 40px;
}

.p-inputtext {
  background: transparent !important;
  color: #333333 !important;
  border-color: #dce7f1 !important;

  &:focus {
    border-color: #dce7f1 !important;
    box-shadow: none !important;
    outline: none !important;
  }

  &:hover {
    border-color: #dce7f1 !important;
  }
}

.p-inputgroupaddon {
  background: #ffffff !important;
  color: #333333 !important;
  border-color: #dce7f1 !important;
}

.p-autocomplete-option {
  color: #333333;
}

.p-toggleswitch {
  width: 2.5rem !important;
  height: 1.25rem !important;

  .p-toggleswitch-handle {
    width: 0.9rem;
    height: 0.9rem;
    top: 62.4%;
    background: #ffffff;
    inset-inline-start: 0.15rem;
  }

  .p-toggleswitch-slider {
    background: #cbd5e1 !important;
  }

  &.p-toggleswitch-checked {
    .p-toggleswitch-slider {
      background: #435ebe !important;
    }

    .p-toggleswitch-handle {
      inset-inline-start: 1.4rem !important;
      background: #ffffff !important;
    }
  }
}

.p-toggleswitch:not(.p-disabled):has(.p-toggleswitch-input:hover) .p-toggleswitch-handle {
  background: #ffffff !important;
}

.p-inputnumber {
  height: 40px;
}

input:-webkit-autofill,
input:-webkit-autofill:hover,
input:-webkit-autofill:focus,
input:-webkit-autofill:active {
  -webkit-box-shadow: 0 0 0px 1000px #fff inset !important;
  -webkit-text-fill-color: #333333 !important;
  transition: background-color 9999s ease-in-out 0s;
}

.input_text {
  display: block;
  width: 100%;
  padding: 0.375rem 0.75rem;
  font-size: 16px;
  line-height: 1.5;
  color: #607080;
  background-color: #fff;
  background-clip: padding-box;
  border: 1px solid #dce7f1;
  appearance: none;
  border-radius: 0.25rem;
  transition: border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;

  &:focus {
    color: #607080;
    background-color: #fff;
    border-color: #a1afdf;
    outline: 0;
  }
}
</style>
