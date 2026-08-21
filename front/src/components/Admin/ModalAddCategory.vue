<template>
  <Dialog
    v-model:visible="showModalAddCategory"
    modal
    header="Thêm danh mục"
    :style="{ width: '25rem' }"
  >
    <div class="category_wrap">
      <div class="form">
        <div class="form_group">
          <div class="info_author">
            <div class="field author">
              <label for="name">Tên</label><span class="required">*</span>
              <input type="text" v-model="categoryInfo.name" class="" name="name" />
              <p class="error" v-if="errorMessageName">{{ errorMessageName }}</p>
            </div>
            <div class="field bio">
              <label for="desc">Mô tả</label>
              <textarea v-model="categoryInfo.description" name="desc" id=""></textarea>
              <p class="word-count">{{ descriptionWordCount }}/200 kí tự</p>
              <p class="error" v-if="errorMessageDescription">{{ errorMessageDescription }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
    <div class="btn_group">
      <button class="btn_close" @click="closeModal">Đóng</button>
      <button class="btn_save" @click="onAddCategory">Lưu</button>
    </div>
  </Dialog>
</template>
<script setup>
import { watch, ref, computed } from "vue"
import Dialog from "primevue/dialog"
import { Icon } from "@iconify/vue"
import api from "../../services/api"
import { useToastMessageStore } from "../../stores/toastMessage"
import { useAuthStore } from "../../stores/auth"
import { TOAST_MESSAGE_STATUS } from "../../constants"

const props = defineProps({
  isShowModalAddCategory: Boolean,
})

const showModalAddCategory = ref(false)
const categoryInfo = ref({
  name: "",
  description: "",
})
const errorMessageName = ref("")
const errorMessageDescription = ref("")

const descriptionWordCount = computed(() => categoryInfo.value.description.length)
const emit = defineEmits(["on:toogleModal", "update:listCategories"])

function closeModal() {
  emit("on:toogleModal", false)
  categoryInfo.value = {
    name: "",
    description: "",
  }
  errorMessageName.value = ""
  errorMessageDescription.value = ""
}

async function onAddCategory() {
  if (!categoryInfo.value.name) {
    errorMessageName.value = "Vui lòng nhập tên danh mục"
  } else {
    errorMessageName.value = ""
  }
  if (categoryInfo.value.description.length > 200) {
    errorMessageDescription.value = "Số kí tự vượt quá 200 kí tự"
  } else {
    errorMessageDescription.value = ""
  }
  if (errorMessageName.value || errorMessageDescription.value) return
  await saveCategory()
}

async function saveCategory() {
  const toasMessageStore = useToastMessageStore()
  const authStore = useAuthStore()
  authStore.setIsLoadingApi(true)
  try {
    const params = {
      name: categoryInfo.value.name,
      description: categoryInfo.value.description,
    }
    const res = await api.post("/categories", params)
    if (res.status == 200 || res.status == 201) {
      toasMessageStore.showToastMessage(res?.data?.message, TOAST_MESSAGE_STATUS.success, 2000)
      emit("update:listCategories")
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
  () => props.isShowModalAddCategory,
  (val) => {
    showModalAddCategory.value = val
  },
  { deep: true, immediate: true }
)
watch(showModalAddCategory, (val) => {
  if (!val) emit("on:toogleModal", false)
})
</script>
<style lang="scss">
.p-dialog {
  background: #ffffff !important;
  color: #333333 !important;
}

.category_wrap {
  margin-bottom: 40px;
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
</style>
