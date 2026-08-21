<template>
  <Dialog
    v-model:visible="showModalAddLanguage"
    modal
    header="Thêm ngôn ngữ"
    :style="{ width: '25rem' }"
  >
    <div class="language_wrap">
      <div class="form">
        <div class="form_group">
          <div class="info_language">
            <div class="field name">
              <label for="name">Tên</label><span class="required">*</span>
              <input
                type="text"
                v-model="languageInfo.name"
                class=""
                name="name"
                @change="handleChangeName"
                placeholder="VD: 'English'"
              />
              <p class="error" v-if="errorMessageName">{{ errorMessageName }}</p>
            </div>
            <div class="field name">
              <label for="desc">Mã<span class="required">*</span></label>
              <input
                type="text"
                v-model="languageInfo.code"
                class=""
                name="name"
                placeholder="VD: 'en'"
              />
              <span class="field-hint">Mã ISO 639-1 (2 ký tự)</span>
              <p class="error" v-if="errorMessageCode">{{ errorMessageCode }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
    <div class="btn_group">
      <button class="btn_close" @click="closeModal">Đóng</button>
      <button class="btn_save" @click="onAddLanguage">Lưu</button>
    </div>
  </Dialog>
</template>
<script setup>
import { watch, ref } from "vue"
import Dialog from "primevue/dialog"
import api from "../../services/api"
import { useToastMessageStore } from "../../stores/toastMessage"
import { useAuthStore } from "../../stores/auth"
import { TOAST_MESSAGE_STATUS } from "../../constants"

const props = defineProps({
  isShowModalAddLanguage: Boolean,
})

const showModalAddLanguage = ref(false)
const languageInfo = ref({
  code: "",
  name: "",
})
const errorMessageName = ref("")
const errorMessageCode = ref("")

const emit = defineEmits(["on:toogleModal", "update:listCategories"])

function closeModal() {
  emit("on:toogleModal", false)
  languageInfo.value = {
    code: "",
    name: "",
  }
  errorMessageName.value = ""
  errorMessageCode.value = ""
}

async function onAddLanguage() {
  if (!languageInfo.value.name) {
    errorMessageName.value = "Vui lòng nhập tên ngôn ngữ"
  } else {
    errorMessageName.value = ""
  }
  if (!languageInfo.value.code) {
    errorMessageCode.value = "Vui lòng nhập mã ngôn ngữ"
  } else {
    errorMessageCode.value = ""
  }
  if (errorMessageName.value || errorMessageCode.value) return
  await saveLanguage()
}

async function saveLanguage() {
  const toasMessageStore = useToastMessageStore()
  const authStore = useAuthStore()
  authStore.setIsLoadingApi(true)
  try {
    const params = {
      name: languageInfo.value.name,
      code: languageInfo.value.code,
    }
    const res = await api.post("/language", params)
    if (res.status == 200 || res.status == 201) {
      toasMessageStore.showToastMessage(res?.data?.message, TOAST_MESSAGE_STATUS.success, 3000)
      emit("update:listLanguages")
      closeModal()
      authStore.setIsLoadingApi(false)
    } else {
      toasMessageStore.showToastMessage(res?.data?.message, TOAST_MESSAGE_STATUS.error, 3000)
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
function generateCode(name) {
  const normalized = name
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/\s+/g, "")

  return normalized.substring(0, 2)
}

watch(
  () => props.isShowModalAddLanguage,
  (val) => {
    showModalAddLanguage.value = val
  },
  { deep: true, immediate: true }
)

watch(showModalAddLanguage, (val) => {
  if (!val) emit("on:toogleModal", false)
})
watch(
  () => languageInfo.value.name,
  (val, old) => {
    if (val !== old)
      languageInfo.value.code = generateCode(val)
  },
  { deep: true }
)
</script>
<style lang="scss">
.p-dialog {
  background: #ffffff !important;
  color: #333333 !important;
}

.language_wrap {
  margin-bottom: 40px;
  .info_language {
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
    
    .field-hint {
      font-size: 12px;
      color: #999;
      margin-top: 2px;
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
    .name,
    .code {
      input:not(.disabled) {
        background: transparent;
        border: 1px solid #a1afdf;
        color: #333333;
        &:focus-visible {
          outline: none;
        }
      }
      input:disabled {
        background-color: #dcdcdc;
        border: 1px solid #dcdcdc;
        cursor: not-allowed;
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
}
</style>
