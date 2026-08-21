import { createRouter, createWebHistory } from "vue-router"

import Login from "../views/Login.vue"
// import BorrowHistory from "../views/BorrowHistory.vue";
import AdminLayout from "../views/admin/AdminLayout.vue"
import Dashboard from "../views/admin/Dashboard.vue"
import { useAuthStore } from "../stores/auth"
// import BookManage from "../views/admin/BookManage.vue";
// import BorrowManage from "../views/admin/BorrowManage.vue";
import api from "../services/api"
import UserLayout from "../views/UserLayout.vue"
import BooksManage from "../views/admin/BooksManage.vue"
import DiscoverView from "../views/DiscorverView.vue"
import BookDetails from "../views/BookDetails.vue"
import Page404 from "../components/Page404.vue"
import Page500 from "../components/Page500.vue"
import Page403 from "../components/Page403.vue"
import BookDetail from "../views/admin/BookManagement/BookDetail.vue"
import BorrowManagement from "../views/admin/BorrowManagement.vue"
import UserManagement from "../views/admin/UserManagement.vue"
import FavouriteBooks from "../views/FavouriteBooks.vue"
import AdvancedSearch from "../views/AdvancedSearch.vue"
import BookCopyImport from "../views/admin/BookManagement/BookCopyImport.vue"
import UserDetail from "../views/admin/UserDetail.vue"
import LibrarianDetail from "../views/admin/UserManagement/LibrarianDetail.vue"
import UserImport from "../views/admin/UserManagement/UserImport.vue"
import BorrowRequestsManagement from "../views/admin/BorrowRequests/BorrowRequestsManagement.vue"
import AddTransactions from "../views/admin/Transactions/AddTransactions.vue"
import BorrowCheckout from "../views/admin/Transactions/BorrowCheckout.vue"
import TransactionList from "../views/admin/Transactions/TransactionList.vue"
import ReturnBook from "../views/admin/Transactions/ReturnBook.vue"
import FineManagement from "../views/admin/FinesManagement/FineManagement.vue"
import CatalogSettings from "../views/admin/CatalogSettings.vue"
import DiscorverView from "../views/DiscorverView.vue"
import BookSearch from "../views/BookSearch.vue"
import UserProfile from "../views/UserProfile.vue"
import MyBooks from "../views/MyBooks.vue"
import MyRequests from "../views/MyRequests.vue"
import MyFines from "../views/MyFines.vue"
import MyFavorites from "../views/MyFavorites.vue"
import MyReading from "../views/MyReading.vue"
import NotificationPage from "../views/NotificationPage.vue"
import Reports from "../views/admin/Reports.vue"
import PublicLayout from "../views/public/PublicLayout.vue"
import AboutPage from "../views/public/AboutPage.vue"
import GuidePage from "../views/public/GuidePage.vue"
import RulesPage from "../views/public/RulesPage.vue"
import ContactPage from "../views/public/ContactPage.vue"
import ForgotPassword from "../views/ForgotPassword.vue"

const routes = [
  {
    path: "/:pathMatch(.*)*",
    name: "NotFound",
    component: Page404,
  },
  {
    path: "/forbidden",
    name: "Forbidden",
    component: Page403,
  },
  {
    path: "/error",
    name: "ServerError",
    component: Page500,
  },
  {
    path: "/login",
    name: "login",
    component: Login,
  },
  { path: "/forgot-password", component: ForgotPassword },
  {
    path: "/",
    component: PublicLayout,
    children: [
      { path: "", name: "public-discover", component: DiscorverView },
      { path: "search", name: "public-search", component: BookSearch },
      { path: "books/:id", name: "public-detail", component: BookDetails },
      { path: "about", name: "public-aboutPage", component: AboutPage },
      { path: "guide", name: "public-guidePage", component: GuidePage },
      { path: "rules", name: "public-rulePage", component: RulesPage },
      { path: "contact", name: "public-contacPage", component: ContactPage },
    ],
  },
  {
    path: "/user",
    component: UserLayout,
    meta: { requiresAuth: true, roles: ["Student", "Staff"] },
    children: [
      {
        path: "",
        redirect: { name: "Discover" },
      },
      {
        path: "discover",
        name: "Discover",
        component: DiscorverView,
      },
      { path: "search", component: BookSearch },
      { path: "books/:id", component: BookDetails },
      { path: "my-books", component: MyBooks },
      { path: "my-favorites", component: MyFavorites },
      { path: "my-requests", component: MyRequests },
      { path: "my-fines", component: MyFines },
      { path: "my-reading", component: MyReading },
      { path: "profile", component: UserProfile },
      { path: "notifications", component: NotificationPage },
    ],
  },
  {
    path: "/admin",
    component: AdminLayout,
    meta: { requiresAuth: true, roles: ["Admin", "Librarian"], theme: "admin" },
    children: [
      {
        path: "dashboard",
        name: "dashboard",
        component: Dashboard,
      },
      {
        path: "books",
        name: "booksManage",
        component: BooksManage,
      },
      {
        path: "books/:id",
        name: "bookDetail",
        component: BookDetail,
      },
      {
        path: "books/copy-import",
        name: "bookCopyImport",
        component: BookCopyImport,
      },
      {
        path: "user-management",
        name: "userManagement",
        component: UserManagement,
      },
      {
        path: "users/:id",
        name: "UserDetail",
        component: UserDetail,
      },
      {
        path: "librarian/:id",
        name: "LibrarianDetail",
        component: LibrarianDetail,
      },
      {
        path: "users/import",
        name: "UserImport",
        component: UserImport,
      },
      {
        path: "borrow-requests",
        name: "borrowRequestManagement",
        component: BorrowRequestsManagement,
      },
      {
        path: "transactions",
        name: "transactionManagement",
        component: TransactionList,
      },
      {
        path: "add-transactions",
        name: "addTransactions",
        component: BorrowCheckout,
      },
      {
        path: "return-transactions",
        name: "returnBook",
        component: ReturnBook,
      },
      {
        path: "fine-management",
        name: "fineManagement",
        component: FineManagement,
      },
      {
        path: "catalog-settings",
        name: "catalogSettings",
        component: CatalogSettings,
      },
      { path: "profile", name: "adminProfile", component: UserProfile },
      { path: "reports", name: "adminReports", component: Reports },
      { path: "notifications", component: NotificationPage },
    ],
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach(async (to, from, next) => {
  const auth = useAuthStore()
  if (to.meta.requiresAuth) {
    if (!auth.token) {
      try {
        const res = await api.post("/auth/refresh")
        auth.setToken(res.data.accessToken)
        if (auth.token && !auth.user) {
          const res = await api.get("/account/me")
          auth.setUser(res.data)
        }
        if (to.meta.roles && !to.meta.roles.includes(auth.user.roles[0])) {
          return next("/forbidden")
        }
        next()
      } catch {
        next("/login")
      }
    } else {
      next()
    }
  } else {
    next()
  }
})

export default router
