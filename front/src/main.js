import { createApp } from "vue"
import { createPinia } from "pinia"
import App from "./App.vue"
import router from "./router"
import clickOutside from "./directives/clickOutside"
import PrimeVue from "primevue/config"
import Select from "primevue/select"
import "primeicons/primeicons.css"
import Lara from "@primevue/themes/lara"
import 'pdfjs-dist/web/pdf_viewer.css'
import './style.scss'
import VueApexCharts from 'vue3-apexcharts'

async function bootstrap() {
  const app = createApp(App)
  const pinia = createPinia()

  app.use(pinia)
  app.use(router)
  app.use(PrimeVue, {
    theme: {
      preset: Lara,
    },
  })
  app.use(VueApexCharts)
  app.component("Select", Select)
  app.directive("click-outside", clickOutside)

  await router.isReady()

  app.mount("#app")
}

bootstrap()
