import { defineStore } from "pinia";

export const useAuthStore = defineStore("auth", {
  state: () => ({
    token: null,
    user: null,
    isLoadingApi: false,
  }),
  actions: {
    setAuth(token, user) {
      this.token = token;
      this.user = user;
    },
    setToken(token) {
      this.token = token;
    },
    setUser(user){
      this.user = user;
    },
    logout() {
      this.token = null;
      this.user = null;
    },
    clear() {
      this.token = null;
    },
    setIsLoadingApi(isLoading) {
      this.isLoadingApi = isLoading;
    },
  },
  getters: {
    getUser(state) {
      return state.user;
    },
    getIsLoadingApi(state) {
      return state.isLoadingApi;
    },
    isAdmin: (state) => state.user?.roles?.includes('Admin') ?? false,
    isLibrarian: (state) => state.user?.roles?.includes('Librarian') ?? false,
    isAdminOrLibrarian: (state) => (state.user?.roles?.includes('Admin') || state.user?.roles?.includes('Librarian')) ?? false
  },
});
