<template>
  <div class="user-detail" v-if="user">

    <!-- Back + Header -->
    <div class="page-header">
      <button class="btn-back" @click="$router.back()"><Icon icon="lsicon:arrow-left-filled" width="14" height="14" /> Quay lại</button>
      <div class="header-actions">
        <button class="btn btn-outline" @click="openEdit">
          <Icon icon="iconamoon:edit-light" width="16" height="16" /> Chỉnh sửa
        </button>
        <button v-if="canChangeStatus" class="btn" :class="user.status === 2 ? 'btn-primary' : 'btn-danger'"
          @click="showConfirm = true">
          <Icon v-if="user.status === 2" icon="si:unlock-line" width="16" height="16" />
          <Icon v-else icon="si:lock-line" width="16" height="16" />
          {{ user.status === 2 ? 'Mở khóa' : 'Khóa tài khoản' }}
        </button>
      </div>
    </div>

    <!-- Profile card -->
    <div class="profile-card">
      <div class="avatar">{{ initials }}</div>
      <div class="profile-info">
        <div class="profile-name">{{ user.fullName || '—' }}</div>
        <div class="profile-email">{{ user.email }}</div>
        <div class="profile-badges">
          <span class="role-badge" :class="roleClass">{{ roleLabel }}</span>
          <span class="status-badge" :class="statusClass">{{ statusLabel }}</span>
          <span v-if="isExpired" class="status-badge status-orange">Thẻ hết hạn</span>
        </div>
      </div>
      <div class="profile-meta">
        <div class="meta-item">
          <span class="meta-label">Ngày tạo</span>
          <span class="meta-value">{{ formatDate(user.createdDate) }}</span>
        </div>
        <div class="meta-item">
          <span class="meta-label">Đăng nhập cuối</span>
          <span class="meta-value">{{ formatDate(user.lastLogin) || 'Chưa đăng nhập' }}</span>
        </div>
        <div class="meta-item">
          <span class="meta-label">Hết hạn thẻ</span>
          <span class="meta-value" :class="{ 'text-red': isExpired }">
            {{ formatDate(user.expiredDate) || 'Không giới hạn' }}
          </span>
        </div>
        <div class="meta-item" v-if="user.phoneNumber">
          <span class="meta-label">Số điện thoại</span>
          <span class="meta-value">{{ user.phoneNumber }}</span>
        </div>
      </div>
    </div>

    <!-- Profile details -->
    <div class="detail-grid">
      <!-- Student profile -->
      <div class="detail-card" v-if="user.studentProfile">
        <div class="detail-card-title">Thông tin sinh viên</div>
        <div class="detail-rows">
          <div class="detail-row">
            <span class="detail-label">Mã sinh viên</span>
            <span class="detail-value code-text">{{ user.studentProfile.studentCode }}</span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Lớp</span>
            <span class="detail-value">{{ user.studentProfile.class || '—' }}</span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Khoa</span>
            <span class="detail-value">{{ user.studentProfile.faculty || '—' }}</span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Ngành</span>
            <span class="detail-value">{{ user.studentProfile.major || '—' }}</span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Khóa</span>
            <span class="detail-value">{{ user.studentProfile.term || '—' }}</span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Năm nhập học</span>
            <span class="detail-value">{{ user.studentProfile.admissionYear || '—' }}</span>
          </div>
        </div>
      </div>

      <!-- Staff profile -->
      <div class="detail-card" v-if="user.staffProfile">
        <div class="detail-card-title">Thông tin cán bộ</div>
        <div class="detail-rows">
          <div class="detail-row">
            <span class="detail-label">Mã cán bộ</span>
            <span class="detail-value code-text">{{ user.staffProfile.staffCode }}</span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Chức vụ</span>
            <span class="detail-value">{{ user.staffProfile.position || '—' }}</span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Phòng ban</span>
            <span class="detail-value">{{ user.staffProfile.department || '—' }}</span>
          </div>
        </div>
      </div>

      <!-- Stats -->
      <div class="detail-card stats-card">
        <div class="detail-card-title">Thống kê mượn sách</div>
        <div class="stats-grid">
          <div class="stat-item" v-for="s in statsDisplay" :key="s.label">
            <div class="stat-num" :class="s.color">{{ s.count }}</div>
            <div class="stat-label">{{ s.label }}</div>
          </div>
        </div>
      </div>
    </div>

    <!-- Transaction history -->
    <div class="section-card">
      <div class="section-header">
        <h3 class="section-title">Lịch sử mượn sách</h3>
        <div class="section-filter">
          <select v-model="txFilter" class="filter-select" @change="fetchTransactions(1)">
            <option value="">Tất cả</option>
            <option value="Borrowed">Đang mượn</option>
            <option value="Returned">Đã trả</option>
            <option value="Overdue">Quá hạn</option>
            <option value="Cancelled">Đã huỷ</option>
          </select>
        </div>
      </div>

      <div v-if="txLoading" class="state-box">Đang tải...</div>
      <div v-else-if="transactions.length === 0" class="state-box">Chưa có lịch sử mượn</div>
      <table v-else class="tx-table">
        <thead>
          <tr>
            <th>Tên sách</th>
            <th>Barcode</th>
            <th>Ngày mượn</th>
            <th>Hạn trả</th>
            <th>Ngày trả</th>
            <th>Trạng thái</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="tx in transactions" :key="tx.transactionId" class="tx-row">
            <td>
              <span class="book-title" :title="tx.bookTitle">{{ truncate(tx.bookTitle, 45) }}</span>
            </td>
            <td><span class="code-text">{{ tx.barcode || '—' }}</span></td>
            <td>{{ formatDate(tx.borrowDate) }}</td>
            <td :class="{ 'text-red': tx.isOverdue }">{{ formatDate(tx.dueDate) }}</td>
            <td>{{ formatDate(tx.returnDate) || '—' }}</td>
            <td>
              <span class="tx-status" :class="txStatusClass(tx.status)">
                {{ txStatusLabel(tx.status, tx.isOverdue) }}
              </span>
            </td>
          </tr>
        </tbody>
      </table>

      <!-- Pagination -->
      <div class="pagination" v-if="txTotalPages > 1">
        <button class="page-btn" :disabled="txPage === 1" @click="fetchTransactions(txPage - 1)">‹</button>
        <template v-for="p in txVisiblePages" :key="p">
          <span v-if="p === '...'" class="page-dots">...</span>
          <button v-else class="page-btn" :class="{ active: p === txPage }" @click="fetchTransactions(p)">{{ p
          }}</button>
        </template>
        <button class="page-btn" :disabled="txPage === txTotalPages" @click="fetchTransactions(txPage + 1)">›</button>
        <span class="page-info">{{ txTotal }} giao dịch</span>
      </div>
    </div>

    <!-- Confirm toggle status -->
    <Teleport to="body">
      <div v-if="showConfirm" class="modal-overlay" @click.self="showConfirm = false">
        <div class="modal modal-sm">
          <div class="modal-header">
            <h3>{{ user.status === 'Blocked' ? 'Mở khóa' : 'Khóa' }} tài khoản</h3>
            <button class="modal-close" @click="showConfirm = false">✕</button>
          </div>
          <div class="modal-body">
            <p>{{ user.status === 'Blocked' ? 'Mở khóa' : 'Khóa' }} tài khoản <strong>{{ user.fullName }}</strong>?</p>
            <p v-if="user.status !== 'Blocked'" class="text-muted">Tài khoản bị khóa sẽ không thể đăng nhập.</p>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" @click="showConfirm = false">Huỷ</button>
            <button class="btn" :class="user.status === 'Blocked' ? 'btn-primary' : 'btn-danger'" @click="submitToggle"
              :disabled="isSubmitting">
              {{ isSubmitting ? 'Đang xử lý...' : (user.status === 'Blocked' ? 'Mở khóa' : 'Khóa') }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <Teleport to="body">
      <div v-if="showEditModal" class="modal-overlay" @click.self="showEditModal = false">
        <div class="modal">
          <div class="modal-header">
            <h3>Chỉnh sửa thông tin</h3>
            <button class="modal-close" @click="showEditModal = false">✕</button>
          </div>
          <div class="modal-body">
            <div class="form-grid">
              <div class="form-group full-width">
                <div class="form-section-title">Thông tin chung</div>
              </div>
              <div class="form-group">
                <label>Họ tên <span class="required">*</span></label>
                <input v-model="editForm.fullName" />
                <span v-if="editErrors.fullName" class="field-error">{{ editErrors.fullName }}</span>
              </div>
              <div class="form-group">
                <label>Số điện thoại</label>
                <input v-model="editForm.phoneNumber" autocomplete="off" />
              </div>
              <div class="form-group">
                <label>Ngày hết hạn thẻ</label>
                <input type="date" v-model="editForm.expiredDate" />
              </div>

              <template v-if="user.studentProfile">
                <div class="form-group full-width">
                  <div class="form-section-title">Thông tin sinh viên</div>
                </div>
                <div class="form-group">
                  <label>Mã sinh viên</label>
                  <input v-model="editForm.studentProfile.studentCode" />
                </div>
                <div class="form-group">
                  <label>Lớp</label>
                  <input v-model="editForm.studentProfile.class" />
                </div>
                <div class="form-group">
                  <label>Khoa</label>
                  <input v-model="editForm.studentProfile.faculty" />
                </div>
                <div class="form-group">
                  <label>Ngành</label>
                  <input v-model="editForm.studentProfile.major" />
                </div>
                <div class="form-group">
                  <label>Khóa</label>
                  <input v-model="editForm.studentProfile.term" />
                </div>
                <div class="form-group">
                  <label>Năm nhập học</label>
                  <input type="number" v-model.number="editForm.studentProfile.admissionYear" />
                </div>
              </template>

              <template v-if="user.staffProfile">
                <div class="form-group full-width">
                  <div class="form-section-title">Thông tin cán bộ</div>
                </div>
                <div class="form-group">
                  <label>Mã cán bộ</label>
                  <input v-model="editForm.staffProfile.staffCode" />
                </div>
                <div class="form-group">
                  <label>Chức vụ</label>
                  <input v-model="editForm.staffProfile.position" />
                </div>
                <div class="form-group">
                  <label>Phòng ban</label>
                  <input v-model="editForm.staffProfile.department" />
                </div>
              </template>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" @click="showEditModal = false">Huỷ</button>
            <button class="btn btn-primary" @click="submitEdit" :disabled="isEditSubmitting">
              {{ isEditSubmitting ? 'Đang lưu...' : 'Cập nhật' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

  </div>

  <div v-else-if="loadError" class="state-box state-error">{{ loadError }}</div>
  <div v-else class="state-box">Đang tải...</div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import api from '../../services/api'
import { Icon } from '@iconify/vue'
import { reactive } from 'vue'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const user = ref(null)
const loadError = ref('')
const isSubmitting = ref(false)
const showConfirm = ref(false)
const txFilter = ref('')

const transactions = ref([])
const txLoading = ref(false)
const txPage = ref(1)
const txTotalPages = ref(1)
const txTotal = ref(0)
const txPageSize = 10

// ---- Fetch user ----
onMounted(async () => {
  await Promise.all([fetchUser(), fetchTransactions(1)])
})

const fetchUser = async () => {
  try {
    const res = await api.get(`/Users/${route.params.id}`)
    if (res.status === 200) user.value = res.data
  } catch (err) {
    loadError.value = err.response?.data?.message || 'Không thể tải thông tin'
  }
}

const fetchTransactions = async (page = 1) => {
  txLoading.value = true
  try {
    const params = new URLSearchParams({ page, pageSize: txPageSize })
    if (txFilter.value) params.append('status', txFilter.value)
    const res = await api.get(`/Users/${route.params.id}/transactions?${params}`)
    if (res.status === 200) {
      transactions.value = res.data.items
      txPage.value = res.data.page
      txTotalPages.value = res.data.totalPages
      txTotal.value = res.data.total
    }
  } catch { }
  finally { txLoading.value = false }
}

// ---- Toggle status ----
const canChangeStatus = computed(() => {
  if (!user.value) return false
  if (user.value.id === authStore.getUser?.id) return false
  if (!authStore.isAdmin && (user.value.roles?.includes('Admin') || user.value.roles?.includes('Librarian'))) return false
  return true
})

const submitToggle = async () => {
  isSubmitting.value = true
  try {
    const newStatus = user.value.status === 2 || user.value.status === 'Blocked'
      ? 0  // Active
      : 2  // Blocked
    const res = await api.patch(`/Users/${user.value.id}/status`, { status: newStatus })
    if (res.status === 200) {
      user.value.status = newStatus
      showConfirm.value = false
    }
  } catch (err) {
    alert(err.response?.data?.message || 'Thao tác thất bại')
  } finally {
    isSubmitting.value = false
  }
}

// ---- Computed ----
const initials = computed(() => {
  if (!user.value?.fullName) return '?'
  return user.value.fullName.split(' ').map(w => w[0]).slice(-2).join('').toUpperCase()
})

const roleLabel = computed(() => {
  const roles = user.value?.roles
  if (roles?.includes('Admin')) return 'Admin'
  if (roles?.includes('Librarian')) return 'Thủ thư'
  if (roles?.includes('Staff')) return 'Giảng viên/CB'
  return 'Sinh viên'
})

const roleClass = computed(() => {
  const roles = user.value?.roles
  if (roles?.includes('Admin')) return 'role-admin'
  if (roles?.includes('Librarian')) return 'role-librarian'
  if (roles?.includes('Staff')) return 'role-staff'
  return 'role-student'
})

const statusLabel = computed(() => {
  const map = {
    0: 'Hoạt động', 1: 'Chưa kích hoạt', 2: 'Đã khóa',
    'Active': 'Hoạt động', 'Inactive': 'Chưa kích hoạt', 'Blocked': 'Đã khóa'
  }
  return map[user.value?.status] || user.value?.status
})

const statusClass = computed(() => {
  const map = {
    0: 'status-green', 1: 'status-gray', 2: 'status-red',
    'Active': 'status-green', 'Inactive': 'status-gray', 'Blocked': 'status-red'
  }
  return map[user.value?.status] || ''
})

const isExpired = computed(() => {
  if (!user.value?.expiredDate) return false
  return new Date(user.value.expiredDate) < new Date()
})

const statsDisplay = computed(() => {
  const stats = user.value?.stats || []
  const get = (s) => stats.find(x => x.status === s)?.count ?? 0
  return [
    { label: 'Tổng mượn', count: stats.reduce((a, b) => a + b.count, 0), color: 'text-blue' },
    { label: 'Đang mượn', count: get('Borrowed'), color: 'text-indigo' },
    { label: 'Quá hạn', count: get('Overdue'), color: 'text-red' },
    { label: 'Đã trả', count: get('Returned'), color: 'text-green' },
    { label: 'Đã huỷ', count: get('Cancelled'), color: 'text-gray' },
  ]
})

const txStatusLabel = (status, isOverdue) => {
  if (isOverdue) return 'Quá hạn'
  const map = { Borrowed: 'Đang mượn', Returned: 'Đã trả', Overdue: 'Quá hạn', Cancelled: 'Đã huỷ' }
  return map[status] || status
}

const txStatusClass = (status) => {
  const map = { Borrowed: 'tx-blue', Returned: 'tx-green', Overdue: 'tx-red', Cancelled: 'tx-gray' }
  return map[status] || ''
}

const formatDate = (date) => {
  if (!date) return null
  return new Date(date).toLocaleDateString('vi-VN')
}

const truncate = (str, len) => {
  if (!str) return '—'
  return str.length > len ? str.slice(0, len) + '...' : str
}

const txVisiblePages = computed(() => {
  const pages = []
  const t = txTotalPages.value
  const cur = txPage.value
  if (t <= 7) { for (let i = 1; i <= t; i++) pages.push(i) }
  else {
    pages.push(1)
    if (cur > 3) pages.push('...')
    for (let i = Math.max(2, cur - 1); i <= Math.min(t - 1, cur + 1); i++) pages.push(i)
    if (cur < t - 2) pages.push('...')
    pages.push(t)
  }
  return pages
})

const showEditModal = ref(false)
const editForm = reactive({
  fullName: '', phoneNumber: '', expiredDate: '',
  studentProfile: { studentCode: '', class: '', faculty: '', major: '', term: '', admissionYear: null },
  staffProfile: { staffCode: '', position: '', department: '' }
})
const editErrors = reactive({})
const isEditSubmitting = ref(false)

const openEdit = () => {
  Object.assign(editForm, {
    fullName: user.value.fullName || '',
    phoneNumber: user.value.phoneNumber || '',
    expiredDate: user.value.expiredDate ? user.value.expiredDate.slice(0, 10) : '',
    studentProfile: user.value.studentProfile ? { ...user.value.studentProfile } : editForm.studentProfile,
    staffProfile: user.value.staffProfile ? { ...user.value.staffProfile } : editForm.staffProfile
  })
  Object.keys(editErrors).forEach(k => delete editErrors[k])
  showEditModal.value = true
}

const submitEdit = async () => {
  if (!editForm.fullName.trim()) { editErrors.fullName = 'Vui lòng nhập họ tên'; return }
  isEditSubmitting.value = true
  try {
    const res = await api.put(`/Users/${user.value.id}`, {
      fullName: editForm.fullName,
      phoneNumber: editForm.phoneNumber || null,
      expiredDate: editForm.expiredDate || null,
      studentProfile: user.value.studentProfile ? editForm.studentProfile : null,
      staffProfile: user.value.staffProfile ? editForm.staffProfile : null
    })
    if (res.status === 200) {
      showEditModal.value = false
      await fetchUser()
    }
  } catch (err) {
    alert(err.response?.data?.message || 'Cập nhật thất bại')
  } finally {
    isEditSubmitting.value = false
  }
}
</script>

<style lang="scss" scoped>
@use "@/assets/scss/variables.scss" as V;

.user-detail {
  display: flex;
  flex-direction: column;
  gap: 20px;
  font-family: 'Segoe UI', sans-serif;
  color: #1a1a2e;
}

// Header
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.btn-back {
  background: none;
  border: none;
  cursor: pointer;
  font-size: 14px;
  color: #3949ab;
  font-weight: 500;
  padding: 6px 0;
  svg {
    margin-bottom: 2px;
  }
  &:hover {
    text-decoration: underline;
  }
}

.header-actions {
  display: flex;
  gap: 8px;
}

// Profile card
.profile-card {
  background: #fff;
  border-radius: 14px;
  border: 1px solid #e0e0e0;
  padding: 24px;
  display: flex;
  align-items: flex-start;
  gap: 20px;
}

.avatar {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  background: #3949ab;
  color: #fff;
  font-size: 22px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.profile-info {
  flex: 1;
}

.profile-name {
  font-size: 20px;
  font-weight: 800;
  margin-bottom: 4px;
}

.profile-email {
  font-size: 14px;
  color: #666;
  margin-bottom: 10px;
}

.profile-badges {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.profile-meta {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 200px;
}

.meta-item {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.meta-label {
  font-size: 11px;
  color: #999;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.meta-value {
  font-size: 13px;
  font-weight: 500;
  color: #333;
}

// Detail grid
.detail-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 16px;
}

.detail-card {
  background: #fff;
  border-radius: 12px;
  border: 1px solid #e0e0e0;
  padding: 20px;
}

.detail-card-title {
  font-size: 13px;
  font-weight: 700;
  color: #3949ab;
  margin-bottom: 14px;
  padding-bottom: 8px;
  border-bottom: 1.5px solid #e8eaf6;
}

.detail-rows {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.detail-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
}

.detail-label {
  font-size: 13px;
  color: #888;
}

.detail-value {
  font-size: 13px;
  font-weight: 500;
  color: #1a1a2e;
  text-align: right;
}

// Stats
.stats-card {}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 8px;

  @media (max-width: 600px) {
    grid-template-columns: repeat(3, 1fr);
  }
}

.stat-item {
  text-align: center;
}

.stat-num {
  font-size: 24px;
  font-weight: 800;
  line-height: 1;

  &.text-blue {
    color: #1565c0;
  }

  &.text-indigo {
    color: #3949ab;
  }

  &.text-red {
    color: #c62828;
  }

  &.text-green {
    color: #2e7d32;
  }

  &.text-gray {
    color: #9e9e9e;
  }
}

.stat-label {
  font-size: 11px;
  color: #999;
  margin-top: 4px;
}

// Transactions
.section-card {
  background: #fff;
  border-radius: 12px;
  border: 1px solid #e0e0e0;
  overflow: hidden;
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  border-bottom: 1px solid #f0f0f0;
}

.section-title {
  font-size: 15px;
  font-weight: 700;
  margin: 0;
}

.filter-select {
  padding: 7px 12px;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  font-size: 13px;
  background: #fff;
  outline: none;
  cursor: pointer;
  font-family: inherit;
  color: #333333;

  &:focus {
    border-color: #3949ab;
  }
}

.tx-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 14px;

  thead tr {
    background: #f5f5f5;
  }

  th {
    padding: 10px 16px;
    text-align: left;
    font-weight: 600;
    color: #555;
    white-space: nowrap;
    border-bottom: 1px solid #e0e0e0;
  }

  td {
    padding: 10px 16px;
    border-bottom: 1px solid #f0f0f0;
  }
}

.tx-row {
  &:last-child td {
    border-bottom: none;
  }

  &:hover {
    background: #fafafa;
  }
}

.book-title {
  color: #333;
}

.code-text {
  font-family: monospace;
  font-size: 13px;
  color: #3949ab;
  font-weight: 600;
}

.text-red {
  color: #c62828;
  font-weight: 600;
}

.tx-status {
  display: inline-block;
  padding: 2px 10px;
  border-radius: 99px;
  font-size: 12px;
  font-weight: 600;

  &.tx-blue {
    background: #e3f2fd;
    color: #1565c0;
  }

  &.tx-green {
    background: #e8f5e9;
    color: #2e7d32;
  }

  &.tx-red {
    background: #ffebee;
    color: #c62828;
  }

  &.tx-gray {
    background: #f5f5f5;
    color: #757575;
  }
}

// Badges
.role-badge {
  display: inline-block;
  padding: 2px 10px;
  border-radius: 99px;
  font-size: 12px;
  font-weight: 600;

  &.role-admin {
    background: #fce4ec;
    color: #c2185b;
  }

  &.role-librarian {
    background: #e8eaf6;
    color: #3949ab;
  }

  &.role-staff {
    background: #e0f2f1;
    color: #00695c;
  }

  &.role-student {
    background: #fff8e1;
    color: #f57f17;
  }
}

.status-badge {
  display: inline-block;
  padding: 2px 10px;
  border-radius: 99px;
  font-size: 12px;
  font-weight: 600;

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

  &.status-orange {
    background: #fff3e0;
    color: #e65100;
  }
}

// Buttons
.btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
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

// Modal
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
  max-width: 540px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.2);
  display: block;
  top: unset;
  left: unset;
  height: unset;
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

  p {
    margin: 0 0 8px;
  }
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding: 16px 24px 20px;
  border-top: 1px solid #f0f0f0;
}

.text-muted {
  color: #888;
  font-size: 13px;
}

// State
.state-box {
  padding: 40px;
  text-align: center;
  color: #888;
  font-size: 14px;

  &.state-error {
    color: #c62828;
  }
}

// Pagination
.pagination {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 12px 16px;
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

  input {
    padding: 8px 12px;
    border: 1.5px solid #e0e0e0;
    border-radius: 8px;
    font-size: 14px;
    outline: none;
    font-family: inherit;

    &:focus {
      border-color: #3949ab;
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
</style>