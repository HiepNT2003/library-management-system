import { defineStore } from "pinia"
import api from "../services/api"
import { useAuthStore } from "@/stores/auth"
import { useToastMessageStore } from "../stores/toastMessage"
import { TOAST_MESSAGE_STATUS } from "../constants"

export const useBookStore = defineStore("book", {
  state: () => ({
    documentTypes: [],
  }),
  actions: {
    async fetchDocumentType() {
      const toasMessageStore = useToastMessageStore()
      const authStore = useAuthStore()
      authStore.setIsLoadingApi(true)
      try {
        const res = await api.get("/documentTypes")
        if (res.status == 200) {
          this.documentTypes = res.data
        }
      } catch (error) {
        toasMessageStore.showToastMessage(
          error?.response?.data?.message,
          TOAST_MESSAGE_STATUS.error,
          5000,
        )
        authStore.setIsLoadingApi(false)
      }
      authStore.setIsLoadingApi(false)
    },
  },
  getters: {
    getDocumentTypes(state) {
      return state.documentTypes
    },
  },
})
