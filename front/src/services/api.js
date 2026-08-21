import axios from "axios";
import { useAuthStore } from "@/stores/auth";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  withCredentials: true,
});

api.interceptors.request.use((config) => {
  const auth = useAuthStore();
  if (auth.token) {
    config.headers.Authorization = `Bearer ${auth.token}`;
  }
  return config;
});

api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const auth = useAuthStore();
    const originalRequest = error.config;

    if (!error.response || error.response.status !== 401)
      return Promise.reject(error);

    if (
      originalRequest.url.includes("/auth/login") ||
      originalRequest.url.includes("/auth/refresh") ||
      !originalRequest.headers.Authorization
    ) {
      return Promise.reject(error);
    }

    try {
      const res = await api.post("/auth/refresh");
      auth.setToken(res.data.accessToken);

      originalRequest.headers.Authorization = `Bearer ${res.data.accessToken}`;

      return api(originalRequest);
    } catch {
      auth.clear();
      window.location.href = "/login";
    }
  },
);

export default api;
