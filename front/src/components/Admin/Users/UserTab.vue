<template>
  <div class="user-tab">
    <!-- Toolbar -->
    <div class="toolbar">
      <input
        v-model="filters.search"
        class="search-input"
        :placeholder="`Tìm tên, email, ${
          role === 'Student' ? 'mã sinh viên' : role === 'Staff' ? 'mã cán bộ' : 'email'
        }...`"
        @input="onSearchInput"
      />
      <select v-model="filters.status" class="filter-select" @change="onFilterChange">
        <option value="">Tất cả trạng thái</option>
        <option value="0">Hoạt động</option>
        <option value="1">Chưa kích hoạt</option>
        <option value="2">Đã khóa</option>
      </select>
      <div class="toolbar-right">
        <button v-if="role !== 'Librarian'" class="btn btn-outline" @click="openImport">
          <Icon class="icon_import" icon="lets-icons:arhive-import" width="14" height="14" />
          Nhập hàng loạt
        </button>
        <button class="btn btn-primary" @click="openCreate">+ Thêm mới</button>
      </div>
    </div>

    <!-- Table -->
    <div class="table-wrapper">
      <div v-if="isLoading" class="state-box">Đang tải...</div>
      <div v-else-if="loadError" class="state-box state-error">{{ loadError }}</div>
      <table v-else class="data-table">
        <thead>
          <tr>
            <th>Họ tên</th>
            <th>Email</th>
            <th v-if="role === 'Student'">Mã SV</th>
            <th v-if="role === 'Student'">Lớp</th>
            <th v-if="role === 'Student'">Khoa</th>
            <th v-if="role === 'Student'">Khóa</th>
            <th v-if="role === 'Staff'">Mã CB</th>
            <th v-if="role === 'Staff'">Chức vụ</th>
            <th v-if="role === 'Staff'">Phòng ban</th>
            <th v-if="role !== 'Librarian'">Đang mượn</th>
            <th v-if="role !== 'Librarian'">Quá hạn</th>
            <th>Hết hạn</th>
            <th>Trạng thái</th>
            <th>Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="items.length === 0">
            <td :colspan="colSpan" class="empty-row">Chưa có dữ liệu</td>
          </tr>
          <tr v-for="user in items" :key="user.id" class="data-row">
            <td>
              <div class="user-name">{{ user.fullName || "—" }}</div>
              <div class="user-sub">{{ user.phoneNumber || "" }}</div>
            </td>
            <td>{{ user.email }}</td>
            <td v-if="role === 'Student'">
              <span class="code-text">{{ user.studentProfile?.studentCode || "—" }}</span>
            </td>
            <td v-if="role === 'Student'">{{ user.studentProfile?.class || "—" }}</td>
            <td v-if="role === 'Student'">{{ user.studentProfile?.faculty || "—" }}</td>
            <td v-if="role === 'Student'">{{ user.studentProfile?.term || "—" }}</td>
            <td v-if="role === 'Staff'">
              <span class="code-text">{{ user.staffProfile?.staffCode || "—" }}</span>
            </td>
            <td v-if="role === 'Staff'">{{ user.staffProfile?.position || "—" }}</td>
            <td v-if="role === 'Staff'">{{ user.staffProfile?.department || "—" }}</td>
            <td v-if="role !== 'Librarian'">
              <span v-if="user.borrowingCount > 0" class="count-badge count-blue">{{
                user.borrowingCount
              }}</span>
              <span v-else class="count-zero">0</span>
            </td>
            <td v-if="role !== 'Librarian'">
              <span v-if="user.overdueCount > 0" class="count-badge count-red">{{
                user.overdueCount
              }}</span>
              <span v-else class="count-zero">0</span>
            </td>
            <td>{{ formatDate(user.expiredDate) }}</td>
            <td>
              <span class="status-badge" :class="statusClass(user.status)">
                {{ statusLabel(user.status) }}
              </span>
            </td>
            <td>
              <div class="action-buttons">
                <button class="action-btn" @click="goToDetail(user)" title="Chi tiết">
                  <Icon icon="carbon:task-view" width="16" height="16" />
                </button>
                <button class="action-btn" @click="openEdit(user)" title="Chỉnh sửa">
                  <Icon icon="iconamoon:edit-light" width="16" height="16" />
                </button>
                <button
                  v-if="canChangeStatus(user)"
                  class="action-btn"
                  @click="confirmToggle(user)"
                  :title="user.status === 2 ? 'Mở khóa' : 'Khóa'"
                >
                  <Icon v-if="user.status === 2" icon="si:unlock-line" width="16" height="16" />
                  <Icon v-else icon="si:lock-line" width="16" height="16" />
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    <div class="pagination" v-if="totalPages > 1">
      <button class="page-btn" :disabled="currentPage === 1" @click="goToPage(currentPage - 1)">
        ‹
      </button>
      <template v-for="p in visiblePages" :key="p">
        <span v-if="p === '...'" class="page-dots">...</span>
        <button v-else class="page-btn" :class="{ active: p === currentPage }" @click="goToPage(p)">
          {{ p }}
        </button>
      </template>
      <button
        class="page-btn"
        :disabled="currentPage === totalPages"
        @click="goToPage(currentPage + 1)"
      >
        ›
      </button>
      <span class="page-info"
        >{{ (currentPage - 1) * pageSize + 1 }}–{{ Math.min(currentPage * pageSize, total) }} /
        {{ total }}</span
      >
    </div>

    <!-- Modal thêm/sửa -->
    <Teleport to="body">
      <div v-if="showFormModal" class="modal-overlay" @click.self="showFormModal = false">
        <div class="modal">
          <div class="modal-header">
            <h3>{{ editingUser ? "Chỉnh sửa" : "Thêm" }} {{ roleTitle }}</h3>
            <button class="modal-close" @click="showFormModal = false">✕</button>
          </div>
          <div class="modal-body">
            <div class="form-grid">
              <!-- Thông tin chung -->
              <div class="form-group full-width">
                <div class="form-section-title">Thông tin chung</div>
              </div>
              <div class="form-group">
                <label>Họ tên <span class="required">*</span></label>
                <input v-model="form.fullName" placeholder="Nguyễn Văn A" />
                <span v-if="formErrors.fullName" class="field-error">{{
                  formErrors.fullName
                }}</span>
              </div>
              <div class="form-group">
                <label>Email <span class="required">*</span></label>
                <input
                  v-model="form.email"
                  type="email"
                  placeholder="example@utc.edu.vn"
                  :disabled="!!editingUser"
                />
                <span v-if="formErrors.email" class="field-error">{{ formErrors.email }}</span>
              </div>
              <div class="form-group">
                <label>Số điện thoại</label>
                <input v-model="form.phoneNumber" autocomplete="off" placeholder="0912345678" />
              </div>
              <div class="form-group">
                <label>Ngày hết hạn thẻ</label>
                <input type="date" v-model="form.expiredDate" />
              </div>
              <div class="form-group" v-if="!editingUser && showFormModal">
                <label>Mật khẩu <span class="required">*</span></label>
                <input
                  :key="formKey"
                  v-model="form.password"
                  type="password"
                  placeholder="Tối thiểu 6 ký tự"
                  autocomplete="new-password"
                />
                <span v-if="formErrors.password" class="field-error">{{
                  formErrors.password
                }}</span>
              </div>
              <!-- Student profile -->
              <template v-if="role === 'Student'">
                <div class="form-group full-width">
                  <div class="form-section-title">Thông tin sinh viên</div>
                </div>
                <div class="form-group">
                  <label>Mã sinh viên <span class="required">*</span></label>
                  <input v-model="form.studentProfile.studentCode" placeholder="VD: 201234567" />
                  <span v-if="formErrors.studentCode" class="field-error">{{
                    formErrors.studentCode
                  }}</span>
                  <div
                    class="code-warn"
                    v-if="form.studentCode !== originalStudentCode && form.studentCode"
                  >
                    ⚠️ Mật khẩu sẽ được reset thành
                    <strong class="code-preview">{{ form.studentCode }}@Utc1</strong>
                  </div>
                </div>
                <div class="form-group">
                  <label>Lớp</label>
                  <input v-model="form.studentProfile.class" placeholder="VD: KTXD62-DH1" />
                </div>
                <div class="form-group">
                  <label>Khoa</label>
                  <input v-model="form.studentProfile.faculty" placeholder="VD: Khoa Công trình" />
                </div>
                <div class="form-group">
                  <label>Ngành</label>
                  <input v-model="form.studentProfile.major" placeholder="VD: Kỹ thuật xây dựng" />
                </div>
                <div class="form-group">
                  <label>Khóa</label>
                  <input v-model="form.studentProfile.term" placeholder="VD: K62" />
                </div>
                <div class="form-group">
                  <label>Năm nhập học</label>
                  <input
                    type="number"
                    v-model.number="form.studentProfile.admissionYear"
                    placeholder="VD: 2022"
                  />
                </div>
              </template>

              <!-- Staff profile -->
              <template v-if="role === 'Staff'">
                <div class="form-group full-width">
                  <div class="form-section-title">Thông tin giảng viên/cán bộ</div>
                </div>
                <div class="form-group">
                  <label>Mã cán bộ <span class="required">*</span></label>
                  <input v-model="form.staffProfile.staffCode" placeholder="VD: CB001234" />
                  <span v-if="formErrors.staffCode" class="field-error">{{
                    formErrors.staffCode
                  }}</span>
                  <div
                    class="code-warn"
                    v-if="form.staffCode !== originalStaffCode && form.staffCode"
                  >
                    ⚠️ Mật khẩu sẽ được reset thành
                    <strong class="code-preview">{{ form.staffCode }}@Utc1</strong>
                  </div>
                </div>
                <div class="form-group">
                  <label>Chức vụ</label>
                  <input v-model="form.staffProfile.position" placeholder="VD: Giảng viên" />
                </div>
                <div class="form-group">
                  <label>Phòng ban/Khoa</label>
                  <input v-model="form.staffProfile.department" placeholder="VD: Khoa Công trình" />
                </div>
              </template>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" @click="showFormModal = false">Huỷ</button>
            <button class="btn btn-primary" @click="submitForm" :disabled="isSubmitting">
              {{ isSubmitting ? "Đang lưu..." : editingUser ? "Cập nhật" : "Tạo tài khoản" }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Confirm khóa/mở khóa -->
    <Teleport to="body">
      <div v-if="showConfirm" class="modal-overlay" @click.self="showConfirm = false">
        <div class="modal modal-sm">
          <div class="modal-header">
            <h3>{{ pendingUser?.status === "Blocked" ? "Mở khóa" : "Khóa" }} tài khoản</h3>
            <button class="modal-close" @click="showConfirm = false">✕</button>
          </div>
          <div class="modal-body">
            <p>
              {{ pendingUser?.status === "Blocked" ? "Mở khóa" : "Khóa" }} tài khoản
              <strong>{{ pendingUser?.fullName }}</strong
              >?
            </p>
            <p v-if="pendingUser?.status !== 'Blocked'" class="text-muted">
              Tài khoản bị khóa sẽ không thể đăng nhập.
            </p>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" @click="showConfirm = false">Huỷ</button>
            <button
              class="btn"
              :class="pendingUser?.status === 'Blocked' ? 'btn-primary' : 'btn-danger'"
              @click="submitToggle"
              :disabled="isSubmitting"
            >
              {{
                isSubmitting
                  ? "Đang xử lý..."
                  : pendingUser?.status === "Blocked"
                  ? "Mở khóa"
                  : "Khóa"
              }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, watch } from "vue"
import { useRouter } from "vue-router"
import { useAuthStore } from "@/stores/auth"
import api from "../../../services/api"
import { Icon } from "@iconify/vue"
import { nextTick } from "vue"

const props = defineProps({
  role: { type: String, required: true }, // 'Student' | 'Staff' | 'Librarian'
})

const router = useRouter()
const authStore = useAuthStore()

// ---- State ----
const items = ref([])
const isLoading = ref(false)
const loadError = ref("")
const total = ref(0)
const totalPages = ref(1)
const currentPage = ref(1)
const pageSize = 20
const isSubmitting = ref(false)

const showFormModal = ref(false)
const showConfirm = ref(false)
const editingUser = ref(null)
const pendingUser = ref(null)
const originalStudentCode = ref("")
const originalStaffCode = ref("")

const filters = reactive({ search: "", status: "" })
let searchTimer = null

const defaultForm = () => ({
  fullName: "",
  email: "",
  password: "",
  phoneNumber: "",
  expiredDate: "",
  studentProfile: {
    studentCode: "",
    class: "",
    faculty: "",
    major: "",
    term: "",
    admissionYear: null,
  },
  staffProfile: { staffCode: "", position: "", department: "" },
})

const form = reactive(defaultForm())
const formErrors = reactive({})

// ---- Computed ----
const roleTitle = computed(() => {
  const map = { Student: "Sinh viên", Staff: "Giảng viên/Cán bộ", Librarian: "Thủ thư" }
  return map[props.role] || ""
})

const colSpan = computed(() => {
  if (props.role === "Student") return 11
  if (props.role === "Staff") return 10
  return 6
})

// ---- Lifecycle ----
onMounted(() => fetchData())
watch(
  () => props.role,
  () => {
    filters.search = ""
    filters.status = ""
    fetchData(1)
  }
)

// ---- Fetch ----
const fetchData = async (page = 1) => {
  isLoading.value = true
  loadError.value = ""
  try {
    const params = new URLSearchParams({ page, pageSize, role: props.role })
    if (filters.status) params.append("status", filters.status)
    if (filters.search.trim()) params.append("search", filters.search.trim())

    const res = await api.get(`/Users?${params}`)
    if (res.status === 200) {
      items.value = res.data.items
      total.value = res.data.total
      totalPages.value = res.data.totalPages
      currentPage.value = res.data.page
    }
  } catch (err) {
    loadError.value = err.response?.data?.message || "Không thể tải dữ liệu"
  } finally {
    isLoading.value = false
  }
}

// ---- Events ----
const onSearchInput = () => {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => fetchData(1), 400)
}
const onFilterChange = () => fetchData(1)
const goToPage = (page) => {
  if (page >= 1 && page <= totalPages.value) fetchData(page)
}
const openImport = () => {
  router.push({
    name: "UserImport",
    query: {
      role: props.role,
    },
  })
}
const goToDetail = (user) => {
  if (user.roles?.includes("Librarian") || user.roles?.includes("Admin")) {
    router.push({
      name: "LibrarianDetail",
      params: { id: user.id },
    })
  } else {
    router.push({
      name: "UserDetail",
      params: { id: user.id },
    })
  }
}
// ---- Form ----
const formKey = ref(0)

const openCreate = async () => {
  editingUser.value = null
  Object.assign(form, defaultForm())
  Object.keys(formErrors).forEach((k) => delete formErrors[k])
  formKey.value++ // force re-render input
  showFormModal.value = true
}

const openEdit = (user) => {
  editingUser.value = user
  Object.assign(form, {
    fullName: user.fullName || "",
    email: user.email || "",
    password: "",
    phoneNumber: user.phoneNumber || "",
    expiredDate: user.expiredDate ? user.expiredDate.slice(0, 10) : "",
    studentProfile: user.studentProfile ? { ...user.studentProfile } : defaultForm().studentProfile,
    staffProfile: user.staffProfile ? { ...user.staffProfile } : defaultForm().staffProfile,
  })
  originalStudentCode.value = user.studentProfile ? user.studentProfile.studentCode || "" : ""
  originalStaffCode.value = user.staffProfile ? user.staffProfile.staffCode || "" : ""
  Object.keys(formErrors).forEach((k) => delete formErrors[k])
  showFormModal.value = true
}

const validateForm = () => {
  Object.keys(formErrors).forEach((k) => delete formErrors[k])
  if (!form.fullName.trim()) formErrors.fullName = "Vui lòng nhập họ tên"
  if (!form.email.trim()) formErrors.email = "Vui lòng nhập email"
  if (!editingUser.value && !form.password) formErrors.password = "Vui lòng nhập mật khẩu"
  if (!editingUser.value && form.password && form.password.length < 6)
    formErrors.password = "Mật khẩu tối thiểu 6 ký tự"
  if (props.role === "Student" && !form.studentProfile.studentCode)
    formErrors.studentCode = "Vui lòng nhập mã sinh viên"
  if (props.role === "Staff" && !form.staffProfile.staffCode)
    formErrors.staffCode = "Vui lòng nhập mã cán bộ"
  return Object.keys(formErrors).length === 0
}

const submitForm = async () => {
  if (!validateForm()) return
  isSubmitting.value = true
  try {
    const payload = {
      fullName: form.fullName,
      email: form.email,
      password: form.password,
      phoneNumber: form.phoneNumber || null,
      expiredDate: form.expiredDate || null,
      role: props.role,
      studentProfile: props.role === "Student" ? form.studentProfile : null,
      staffProfile: props.role === "Staff" ? form.staffProfile : null,
    }

    if (editingUser.value) {
      const studentCodeChanged =
        form.studentProfile?.studentCode !== originalStudentCode.value &&
        form.studentProfile.studentCode
      const staffCodeChanged =
        form.staffProfile?.staffCode !== originalStaffCode.value && form.staffProfile.staffCode

      // Confirm nếu có thay đổi mã
      if (studentCodeChanged || staffCodeChanged) {
        const newCode = studentCodeChanged
          ? form.studentProfile.studentCode
          : form.staffProfile.staffCode
        const ok = confirm(
          `Bạn đã thay đổi mã. Mật khẩu sẽ được reset thành "${newCode}@Utc1".\n\nXác nhận tiếp tục?`
        )
        if (!ok) return
      }
      const res = await api.put(`/Users/${editingUser.value.id}`, payload)
      if (res.status === 200) {
        if (res.data.codeChanged) {
          originalStudentCode.value = ""
          originalStaffCode.value = ""
          alert(
            `✅ Cập nhật thành công!\n🔑 Mật khẩu đã reset thành: ${
              form.studentProfile.studentCode || form.staffProfile.staffCode
            }@Utc1`
          )
        } else {
          alert("✅ Cập nhật thành công!")
        }
        showFormModal.value = false
        await fetchData(currentPage.value)
      }
    } else {
      const res = await api.post("/Users", payload)
      if (res.status === 201) {
        showFormModal.value = false
        await fetchData(1)
      }
    }
  } catch (err) {
    const msg = err.response?.data?.message || "Thao tác thất bại"
    if (msg.toLowerCase().includes("email")) formErrors.email = msg
    else alert(msg)
  } finally {
    isSubmitting.value = false
  }
}

// ---- Toggle status ----
const canChangeStatus = (user) => {
  if (user.id === authStore.getUser?.id) return false
  if (!authStore.isAdmin && (user.roles?.includes("Admin") || user.roles?.includes("Librarian")))
    return false
  return true
}

const confirmToggle = (user) => {
  pendingUser.value = user
  showConfirm.value = true
}

const submitToggle = async () => {
  isSubmitting.value = true
  try {
    const newStatus =
      pendingUser.value.status === 2 || pendingUser.value.status === "Blocked"
        ? 0 // Active
        : 2 // Blocked
    const res = await api.patch(`/Users/${pendingUser.value.id}/status`, { status: newStatus })
    if (res.status === 200) {
      pendingUser.value.status = newStatus
      showConfirm.value = false
    }
  } catch (err) {
    alert(err.response?.data?.message || "Thao tác thất bại")
  } finally {
    isSubmitting.value = false
  }
}

// ---- Helpers ----
const statusLabel = (status) => {
  const map = {
    0: "Hoạt động",
    1: "Chưa kích hoạt",
    2: "Đã khóa",
    Active: "Hoạt động",
    Inactive: "Chưa kích hoạt",
    Blocked: "Đã khóa",
  }
  return map[status] ?? status
}

const statusClass = (status) => {
  const map = {
    0: "status-green",
    1: "status-gray",
    2: "status-red",
    Active: "status-green",
    Inactive: "status-gray",
    Blocked: "status-red",
  }
  return map[status] ?? ""
}
const formatDate = (date) => {
  if (!date) return "—"
  return new Date(date).toLocaleDateString("vi-VN")
}

const visiblePages = computed(() => {
  const pages = []
  const t = totalPages.value
  const cur = currentPage.value
  if (t <= 7) {
    for (let i = 1; i <= t; i++) pages.push(i)
  } else {
    pages.push(1)
    if (cur > 3) pages.push("...")
    for (let i = Math.max(2, cur - 1); i <= Math.min(t - 1, cur + 1); i++) pages.push(i)
    if (cur < t - 2) pages.push("...")
    pages.push(t)
  }
  return pages
})
</script>

<style lang="scss" scoped>
@use "@/assets/scss/variables.scss" as V;

.user-tab {
  display: flex;
  flex-direction: column;
  gap: 16px;
  color: #1a1a2e;
  padding: 16px 24px;
  background: #ffffff;
  border-radius: 12px;
  margin-top: 8px;
}

.toolbar {
  display: flex;
  gap: 10px;
  align-items: center;
  flex-wrap: wrap;
}

.toolbar-right {
  display: flex;
  gap: 8px;
  margin-left: auto;
}

.search-input {
  flex: 1;
  min-width: 220px;
  padding: 9px 14px;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  font-size: 14px;
  outline: none;

  &:focus {
    border-color: #3949ab;
  }
}

.filter-select {
  padding: 9px 12px;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  font-size: 14px;
  background: #fff;
  outline: none;
  cursor: pointer;
  color: #333333;

  &:focus {
    border-color: #3949ab;
  }
}

.table-wrapper {
  overflow-x: auto;
  border-radius: 10px;
  border: 1px solid #e0e0e0;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 14px;

  thead tr {
    background: #f5f5f5;
  }

  th {
    padding: 10px 14px;
    text-align: left;
    font-weight: 700;
    color: #555;
    white-space: nowrap;
    border-bottom: 1px solid #e0e0e0;
  }

  td {
    padding: 10px 14px;
    border-bottom: 1px solid #f0f0f0;
    vertical-align: middle;
  }
}

.data-row {
  &:last-child td {
    border-bottom: none;
  }

  &:hover {
    background: #fafafa;
  }
}

.user-name {
  font-weight: 700;
}

.user-sub {
  font-size: 12px;
  color: #999;
  margin-top: 2px;
}

.code-text {
  font-family: monospace;
  font-size: 13px;
  color: #3949ab;
  font-weight: 600;
}

.empty-row {
  text-align: center;
  color: #aaa;
  padding: 40px;
}

.status-badge {
  display: inline-block;
  padding: 3px 10px;
  border-radius: 99px;
  font-size: 12px;
  font-weight: 700;
  width: max-content;

  &.status-green {
    background: #e8f5e9;
    color: #2e7d32;
  }

  &.status-gray {
    background: #f5f5f5;
    color: #757575;
  }

  &.status-red {
    background: #ffebee;
    color: #c62828;
  }
}

.count-badge {
  display: inline-block;
  padding: 2px 10px;
  border-radius: 99px;
  font-size: 12px;
  font-weight: 700;

  &.count-blue {
    background: #e3f2fd;
    color: #1565c0;
  }

  &.count-red {
    background: #ffebee;
    color: #c62828;
  }
}

.count-zero {
  color: #ccc;
  font-size: 13px;
}

.action-buttons {
  display: flex;
  gap: 4px;
}

.action-btn {
  background: none;
  border: none;
  cursor: pointer;
  padding: 4px 6px;
  border-radius: 6px;
  font-size: 15px;
  transition: background 0.15s;
  color: #3949ab;

  &:hover {
    background: #f0f0f0;
  }
}

.btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 9px 18px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 700;
  cursor: pointer;
  border: none;
  transition: all 0.15s;

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  &.btn-primary {
    background: #3949ab;
    color: #fff;

    &:hover:not(:disabled) {
      background: #2c3a8c;
    }
  }

  &.btn-outline {
    background: #fff;
    color: #3949ab;
    border: 1.5px solid #3949ab;
    padding: 8px 17px;

    &:hover:not(:disabled) {
      background: #e8eaf6;
    }
  }

  &.btn-danger {
    background: #e53935;
    color: #fff;

    &:hover:not(:disabled) {
      background: #c62828;
    }
  }
}

.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 16px;
}

.modal {
  background: #fff;
  border-radius: 14px;
  width: 100%;
  max-width: 580px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.2);
  display: block;
  height: unset;
  top: unset;
  left: unset;
}

.modal-sm {
  max-width: 400px;
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 24px 16px;
  border-bottom: 1px solid #f0f0f0;

  h3 {
    margin: 0;
    font-size: 17px;
    font-weight: 700;
  }
}

.modal-close {
  background: none;
  border: none;
  font-size: 18px;
  cursor: pointer;
  color: #aaa;
  padding: 4px 8px;
  border-radius: 6px;

  &:hover {
    background: #f0f0f0;
  }
}

.modal-body {
  padding: 20px 24px;
  max-height: 69vh;
  overflow: auto;
  @include V.custom-scroll-bar;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding: 16px 24px 20px;
  border-top: 1px solid #f0f0f0;
}

.form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;

  &.full-width {
    grid-column: 1 / -1;
  }

  label {
    font-size: 13px;
    font-weight: 600;
    color: #444;
  }

  input,
  select {
    padding: 8px 12px;
    border: 1.5px solid #e0e0e0;
    border-radius: 8px;
    font-size: 14px;
    outline: none;

    &:focus {
      border-color: #3949ab;
    }

    &:disabled {
      background: #f5f5f5;
      color: #999;
    }
  }
}

.form-section-title {
  font-size: 13px;
  font-weight: 700;
  color: #3949ab;
  padding-bottom: 4px;
  border-bottom: 1.5px solid #e8eaf6;
}

.required {
  color: #e53935;
}

.field-error {
  color: #e53935;
  font-size: 12px;
}

.text-muted {
  color: #888;
  font-size: 13px;
  margin-top: 4px;
}

.state-box {
  padding: 40px;
  text-align: center;
  color: #888;
  font-size: 14px;

  &.state-error {
    color: #c62828;
  }
}

.pagination {
  display: flex;
  align-items: center;
  gap: 4px;
  justify-content: center;
}

.page-btn {
  min-width: 34px;
  height: 34px;
  padding: 0 8px;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  background: #fff;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.15s;
  color: #333;

  &:hover:not(:disabled) {
    border-color: #3949ab;
    color: #3949ab;
  }

  &.active {
    background: #3949ab;
    border-color: #3949ab;
    color: #fff;
    font-weight: 700;
  }

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }
}

.page-dots {
  padding: 0 4px;
  color: #aaa;
}

.page-info {
  margin-left: 8px;
  font-size: 13px;
  color: #888;
}

.code-warn {
  margin-top: 6px;
  padding: 8px 12px;
  background: #fff3e0;
  border-left: 3px solid #fb8c00;
  border-radius: 0 8px 8px 0;
  font-size: 13px;
  color: #e65100;
}
.code-preview {
  font-family: monospace;
  font-size: 14px;
  color: #1a1a2e;
}
</style>