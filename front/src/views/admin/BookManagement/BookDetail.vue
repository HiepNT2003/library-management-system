<script setup>
import { useRoute, useRouter } from "vue-router"
import AddNewBook from "../../../components/Admin/Books/AddNewBook.vue"
import Stepper from "primevue/stepper"
import StepList from "primevue/steplist"
import Step from "primevue/step"
import { computed, onMounted, ref, watch } from "vue"
import { useToastMessageStore } from "../../../stores/toastMessage"
import { useAuthStore } from "../../../stores/auth"
import { TOAST_MESSAGE_STATUS } from "../../../constants"
import api from "../../../services/api"
import BooksManageCopy from "../../../components/Admin/Books/BooksDetailCopy.vue"

const route = useRoute()
const router = useRouter()

const isEditBook = computed(() => route.params.id && route.params.id !== "new")
const documentType = ref(route.query.documentType)
const stepVal = ref("1")
const detailBook = ref({})

function handleChangeStep(val) {
  stepVal.value = val
}

async function fetDetailBook(bookId) {
  const toasMessageStore = useToastMessageStore()
  const authStore = useAuthStore()
  authStore.setIsLoadingApi(true)
  try {
    const res = await api.get(`/books/${bookId}`)
    if (res.status == 200) {
      detailBook.value = res.data
    }
  } catch (error) {
    toasMessageStore.showToastMessage(
      error?.response?.data?.message,
      TOAST_MESSAGE_STATUS.error,
      5000
    )
    router.push({
      name: "ServerError",
    })
    authStore.setIsLoadingApi(false)
  }
  authStore.setIsLoadingApi(false)
}
watch(
  () => route.params.id,
  async (val) => {
    if (!val || val === "new") {
      detailBook.value = {}
      return
    }

    await fetDetailBook(val)

    documentType.value = detailBook.value?.documentTypeId?.toString()
    stepVal.value = '1'
  },
  { immediate: true } 
)
</script>
<template>
  <div class="detail_wrapper">
    <div class="flex justify-center" v-if="(documentType == 1 || documentType == 3) && isEditBook">
      <Stepper v-model:value="stepVal" class="basis-[50rem]">
        <StepList>
          <Step value="1">Đầu sách</Step>
          <Step value="2">Bản sách</Step>
        </StepList>
      </Stepper>
    </div>
    <AddNewBook
      v-if="isEditBook ? detailBook.bookId && stepVal == 1 : stepVal == 1"
      :documentType="documentType"
      :detailBook="detailBook"
      @on:changeStep="handleChangeStep"
    />
    <BooksManageCopy
      v-if="isEditBook && detailBook.bookId && stepVal == 2"
      :bookId="detailBook.bookId"
      @updated="handleCopiesUpdated"
    />
  </div>
</template>
<style lang="scss">
.p-stepper {
  width: 50%;
  margin: auto;

  button:focus {
    outline: none;
    box-shadow: none;
  }

  .p-stepper-separator {
    background: #dcdcdc;
  }

  .p-step:has(~ .p-step-active) .p-stepper-separator {
    background: #435ebe;
  }

  .p-step-number {
    color: #64748b;
    border: 2px solid #e2e8f0;
    background: #ffffff;
  }

  .p-step-active {
    .p-step-number {
      background: #435ebe;
      border-color: #435ebe;
      color: #ffffff;
    }

    .p-step-title {
      color: #435ebe;
    }
  }
}
</style>
