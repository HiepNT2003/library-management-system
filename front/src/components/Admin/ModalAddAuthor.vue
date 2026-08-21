<template>
  <Dialog
    v-model:visible="showModalAddAuthor"
    maximizable
    modal
    :header="`${isEdit ? 'Sửa' : 'Thêm'} tác giả`"
    :style="{ width: '50rem' }"
    :breakpoints="{ '1199px': '75vw', '575px': '90vw' }"
  >
    <div class="author_wrap">
      <div class="author_img">
        <img
          v-if="previewUrl || authorInfo"
          :src="previewUrl ? previewUrl : authorInfo.imageUrl"
          width="200"
        />
        <label class="upload-author">
          <EmptyImage v-if="!previewUrl && !(isEdit && authorInfo.imageUrl)" />
          <p class="choose_img">Chọn ảnh</p>
          <input type="file" accept="image/*" @change="handleFileChange" hidden />
        </label>
      </div>
      <div class="form">
        <div class="form_group">
          <p class="title">Thông tin tác giả</p>
          <div class="info_author">
            <div class="field author">
              <label for="name">Họ tên</label><span class="required">*</span>
              <input type="text" v-model="authorInfo.name" class="" name="name" />
              <p class="error" v-if="errorMessageName">{{ errorMessageName }}</p>
            </div>
            <div class="field bio">
              <label for="bio">Bio</label>
              <textarea v-model="authorInfo.bio" name="bio" id=""></textarea>
              <p class="word-count">{{ bioWordCount }}/200 kí tự</p>
              <p class="error" v-if="errorMessageBio">{{ errorMessageBio }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
    <div class="btn_group">
      <button class="btn_close" @click="closeModal">Đóng</button>
      <button class="btn_save" @click="submitAuthor">Lưu</button>
    </div>
  </Dialog>
</template>
<script setup>
import { watch, ref, computed } from "vue"
import Dialog from "primevue/dialog"
import EmptyImage from "../share/EmptyImage.vue"
import { Icon } from "@iconify/vue"
import api from "../../services/api"
import { useToastMessageStore } from "../../stores/toastMessage"
import { useAuthStore } from "../../stores/auth"
import { TOAST_MESSAGE_STATUS } from "../../constants"

const props = defineProps({
  isShowModalAddAuthor: Boolean,
  editInfo: Object,
})

const showModalAddAuthor = ref(false)
const authorInfo = ref({
  name: "",
  bio: "",
  imageUrl: "",
})
const errorMessageName = ref("")
const errorMessageBio = ref("")
const file = ref(null)
const previewUrl = ref("")
const imageUrl = ref("")

const bioWordCount = computed(() => authorInfo.value.bio.length)
const isEdit = computed(() => (props.editInfo?.name ? true : false))
const emit = defineEmits(["on:toogleModal", "update:listAuthors"])

function closeModal() {
  errorMessageName.value = ""
  errorMessageBio.value = ""
  file.value = null
  previewUrl.value = ""
  imageUrl.value = ""
  emit("on:toogleModal", false)
}

async function submitAuthor() {
  if (!authorInfo.value.name) {
    errorMessageName.value = "Vui lòng nhập tên tác giả"
  } else {
    errorMessageName.value = ""
  }
  if (authorInfo.value.bio.length > 200) {
    errorMessageBio.value = "Số kí tự vượt quá 200 kí tự"
  } else {
    errorMessageBio.value = ""
  }
  if (errorMessageName.value || errorMessageBio.value) return
  if(isEdit.value) await updateAuthor()
  else await saveAuthor()
}

const handleFileChange = (e) => {
  const selectedFile = e.target.files[0]
  if (!selectedFile) return
  file.value = selectedFile
  if (previewUrl.value) {
    URL.revokeObjectURL(previewUrl.value)
  }
  previewUrl.value = URL.createObjectURL(selectedFile)
}
async function uploadImage() {
  if (!file.value) return null

  const formData = new FormData()
  formData.append("file", file.value)
  formData.append("type", "author")

  const res = await api.post("/upload", formData, {
    headers: {
      "Content-Type": "multipart/form-data",
    },
  })
  imageUrl.value = res.data.url

  return imageUrl.value
}
async function saveAuthor() {
  const toasMessageStore = useToastMessageStore()
  const authStore = useAuthStore()
  authStore.setIsLoadingApi(true)
  try {
    const uploadedImage = await uploadImage()
    const params = {
      name: authorInfo.value.name,
      bio: authorInfo.value.bio,
      imageUrl: uploadedImage,
    }
    const res = await api.post("/authors", params)
    if (res.status == 200 || res.status == 201) {
      toasMessageStore.showToastMessage(res?.data?.message, TOAST_MESSAGE_STATUS.success, 2000)
      emit("update:listAuthors")
      closeModal()
      authStore.setIsLoadingApi(false)
    } else {
      toasMessageStore.showToastMessage(res?.data?.message, TOAST_MESSAGE_STATUS.error, 2000)
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
async function updateAuthor() {
  const toasMessageStore = useToastMessageStore()
  const authStore = useAuthStore()
  authStore.setIsLoadingApi(true)
  try {
    const uploadedImage = await uploadImage()
    const params = {
      name: authorInfo.value.name,
      bio: authorInfo.value.bio,
      imageUrl: uploadedImage ? uploadedImage : authorInfo.imageUrl,
    }
    const res = await api.put(`/authors/${props.editInfo?.id}`, params)
    if (res.status == 200 || res.status == 201) {
      toasMessageStore.showToastMessage(res?.data?.message, TOAST_MESSAGE_STATUS.success, 2000)
      emit("update:listAuthors")
      closeModal()
      authStore.setIsLoadingApi(false)
    } else {
      toasMessageStore.showToastMessage(res?.data?.message, TOAST_MESSAGE_STATUS.error, 2000)
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
watch(
  () => props.isShowModalAddAuthor,
  (val) => {
    showModalAddAuthor.value = val
  },
  { deep: true, immediate: true }
)
watch(
  () => props.editInfo?.id,
  (val) => {
    authorInfo.value = props.editInfo
  },
  { deep: true }
)
watch(showModalAddAuthor, (val) => {
  if (!val) emit("on:toogleModal", false)
})
</script>
<style lang="scss">
.p-dialog {
  background: #ffffff !important;
  color: #333333 !important;
}
.author_img {
  display: flex;
  flex-direction: column;
  text-align: center;

  img {
    display: block;
    margin-bottom: 4px;
  }
}

.upload-author {
  width: 100%;
  text-align: center;
  margin-top: 48px;

  .choose_img {
    text-align: center;
    cursor: pointer;
  }
}

.author_wrap {
  display: grid;
  grid-template-columns: 1fr 3fr;
  margin-bottom: 40px;
  gap: 12px;

  .info_author {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }
}

.form {
  .form_group {
    .field {
      margin-bottom: 4px;
    }

    .title {
      font-size: 18px;
      font-weight: 700;
    }

    .required {
      color: red;
    }

    label {
      margin-bottom: 4px;
    }

    input {
      border: 1px solid #a1afdf;
      border-radius: 4px;
      padding: 0.375rem 0.75rem;
      width: 100%;
    }
    .bio {
      display: flex;
      flex-direction: column;
      resize: none;
      textarea {
        height: 100px;
        background: transparent;
        border: 1px solid #a1afdf;
        border-radius: 4px;
        color: #333333;
        padding: 8px;
        resize: none;
        &:focus-visible {
          outline: none;
        }
      }
    }
    .author {
      input {
        background: transparent;
        border: 1px solid #a1afdf;
        color: #333333;
        &:focus-visible {
          outline: none;
        }
      }
    }
    .word-count {
      font-size: 12px;
      color: #8d9fda;
      margin-top: 4px;
      margin-bottom: 0;
    }
    .error {
      font-size: 12px;
      color: red;
      height: 18px;
      margin-bottom: 0;
    }
  }
}

.btn_group {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
  margin-top: 16px;
  position: absolute;
  bottom: 24px;
  right: 24px;
}

.btn_close,
.btn_save {
  border-radius: 4px;
  padding: 4px 16px;
  border: none;
}

.btn_close {
  border: 1px solid #dcdcdc;
  background: transparent;
  color: #333333;
}

.btn_save {
  background: #435ebe;
  color: #ffffff;
}
input {
  background: transparent;
  color: #333333;
}
</style>
