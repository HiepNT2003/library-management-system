import { defineStore } from 'pinia'

export const useToastMessageStore = defineStore('toastMessage', {
  state: () => ({
    toastMessage: []
  }),
  actions: {
    showToastMessage(message, status, showTime, title) {
      const id = Date.now().toString(36) + Math.random().toString(36).substring(2);
      this.toastMessage = [...this.toastMessage, {
        id,
        status,
        message,
        showTime,
        title: title? title: status
      }]
    },
    removeToastMessage(id){
      this.toastMessage = this.toastMessage.filter(toast => toast.id !== id)
    }
  },
  getters: {
    getToastMessageInfo(state) {
      return state.toastMessage
    },
  },
})
