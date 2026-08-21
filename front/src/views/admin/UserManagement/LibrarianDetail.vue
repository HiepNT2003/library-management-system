<template>
  <div class="librarian-detail" v-if="user">

    <!-- Header -->
    <div class="page-header">
      <button class="btn-back" @click="$router.back()">← Quay lại</button>
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
      <div class="profile-main">
        <div class="profile-name">{{ user.fullName || '—' }}</div>
        <div class="profile-email">{{ user.email }}</div>
        <div class="profile-badges">
          <span class="role-badge" :class="roleClass">{{ roleLabel }}</span>
          <span class="status-badge" :class="statusClass">{{ statusLabel }}</span>
        </div>
      </div>
    </div>

    <!-- Info grid -->
    <div class="info-grid">
      <div class="info-card">
        <div class="info-card-title">Thông tin tài khoản</div>
        <div class="info-rows">
          <div class="info-row">
            <span class="info-label">Họ tên</span>
            <span class="info-value">{{ user.fullName || '—' }}</span>
          </div>
          <div class="info-row">
            <span class="info-label">Email</span>
            <span class="info-value">{{ user.email }}</span>
          </div>
          <div class="info-row">
            <span class="info-label">Tên đăng nhập</span>
            <span class="info-value">{{ user.userName }}</span>
          </div>
          <div class="info-row">
            <span class="info-label">Số điện thoại</span>
            <span class="info-value">{{ user.phoneNumber || '—' }}</span>
          </div>
        </div>
      </div>

      <div class="info-card">
        <div class="info-card-title">Hoạt động</div>
        <div class="info-rows">
          <div class="info-row">
            <span class="info-label">Ngày tạo</span>
            <span class="info-value">{{ formatDate(user.createdDate) || '—' }}</span>
          </div>
          <div class="info-row">
            <span class="info-label">Đăng nhập cuối</span>
            <span class="info-value">{{ formatDateTime(user.lastLogin) || 'Chưa đăng nhập' }}</span>
          </div>
          <div class="info-row">
            <span class="info-label">Trạng thái</span>
            <span class="status-badge" :class="statusClass">{{ statusLabel }}</span>
          </div>
          <div class="info-row">
            <span class="info-label">Vai trò</span>
            <span class="role-badge" :class="roleClass">{{ roleLabel }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Confirm toggle status -->
    <Teleport to="body">
      <div v-if="showConfirm" class="modal-overlay" @click.self="showConfirm = false">
        <div class="modal modal-sm">
          <div class="modal-header">
            <h3>{{ user.status === 2 || user.status === 'Blocked' ? 'Mở khóa' : 'Khóa' }} tài khoản</h3>
            <button class="modal-close" @click="showConfirm = false">✕</button>
          </div>
          <div class="modal-body">
            <p>
              {{ user.status === 2 || user.status === 'Blocked' ? 'Mở khóa' : 'Khóa' }} tài khoản
              <strong>{{ user.fullName }}</strong>?
            </p>
            <p v-if="user.status !== 2 && user.status !== 'Blocked'" class="text-muted">
              Tài khoản bị khóa sẽ không thể đăng nhập.
            </p>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" @click="showConfirm = false">Huỷ</button>
            <button
              class="btn"
              :class="user.status === 2 || user.status === 'Blocked' ? 'btn-primary' : 'btn-danger'"
              @click="submitToggle"
              :disabled="isSubmitting"
            >
              {{ isSubmitting ? 'Đang xử lý...' : (user.status === 2 || user.status === 'Blocked' ? 'Mở khóa' : 'Khóa') }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Modal edit -->
    <Teleport to="body">
      <div v-if="showEditModal" class="modal-overlay" @click.self="showEditModal = false">
        <div class="modal">
          <div class="modal-header">
            <h3>Chỉnh sửa thông tin</h3>
            <button class="modal-close" @click="showEditModal = false">✕</button>
          </div>
          <div class="modal-body">
            <div class="form-grid">
              <div class="form-group">
                <label>Họ tên <span class="required">*</span></label>
                <input v-model="editForm.fullName" placeholder="Nguyễn Văn A" />
                <span v-if="editErrors.fullName" class="field-error">{{ editErrors.fullName }}</span>
              </div>
              <div class="form-group">
                <label>Số điện thoại</label>
                <input v-model="editForm.phoneNumber" autocomplete="off" placeholder="0912345678" />
              </div>
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
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import api from '../../../services/api'
import { Icon } from '@iconify/vue'

const route = useRoute()
const authStore = useAuthStore()

const user = ref(null)
const loadError = ref('')
const isSubmitting = ref(false)
const showConfirm = ref(false)
const showEditModal = ref(false)
const isEditSubmitting = ref(false)

const editForm = reactive({ fullName: '', phoneNumber: '' })
const editErrors = reactive({})

onMounted(() => fetchUser())

const fetchUser = async () => {
  try {
    const res = await api.get(`/Users/${route.params.id}`)
    if (res.status === 200) user.value = res.data
  } catch (err) {
    loadError.value = err.response?.data?.message || 'Không thể tải thông tin'
  }
}

// ---- Edit ----
const openEdit = () => {
  Object.assign(editForm, {
    fullName:    user.value.fullName || '',
    phoneNumber: user.value.phoneNumber || ''
  })
  Object.keys(editErrors).forEach(k => delete editErrors[k])
  showEditModal.value = true
}

const submitEdit = async () => {
  Object.keys(editErrors).forEach(k => delete editErrors[k])
  if (!editForm.fullName.trim()) { editErrors.fullName = 'Vui lòng nhập họ tên'; return }
  isEditSubmitting.value = true
  try {
    const res = await api.put(`/Users/${user.value.id}`, {
      fullName:    editForm.fullName,
      phoneNumber: editForm.phoneNumber || null
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
    const isBlocked = user.value.status === 2 || user.value.status === 'Blocked'
    const newStatus = isBlocked ? 0 : 2
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

// ---- Helpers ----
const initials = computed(() => {
  if (!user.value?.fullName) return '?'
  return user.value.fullName.split(' ').map(w => w[0]).slice(-2).join('').toUpperCase()
})

const roleLabel = computed(() => {
  const roles = user.value?.roles
  if (roles?.includes('Admin')) return 'Admin'
  if (roles?.includes('Librarian')) return 'Thủ thư'
  return '—'
})

const roleClass = computed(() => {
  const roles = user.value?.roles
  if (roles?.includes('Admin')) return 'role-admin'
  return 'role-librarian'
})

const statusLabel = computed(() => {
  const map = { 0: 'Hoạt động', 1: 'Chưa kích hoạt', 2: 'Đã khóa',
                Active: 'Hoạt động', Inactive: 'Chưa kích hoạt', Blocked: 'Đã khóa' }
  return map[user.value?.status] ?? user.value?.status
})

const statusClass = computed(() => {
  const map = { 0: 'status-green', 1: 'status-gray', 2: 'status-red',
                Active: 'status-green', Inactive: 'status-gray', Blocked: 'status-red' }
  return map[user.value?.status] ?? ''
})

const formatDate = (date) => {
  if (!date) return null
  return new Date(date).toLocaleDateString('vi-VN')
}

const formatDateTime = (date) => {
  if (!date) return null
  return new Date(date).toLocaleString('vi-VN')
}
</script>

<style lang="scss" scoped>
.librarian-detail {
  display: flex;
  flex-direction: column;
  gap: 20px;
  font-family: 'Segoe UI', sans-serif;
  color: #1a1a2e;
}

.page-header {
  display: flex; align-items: center; justify-content: space-between; gap: 12px;
}
.btn-back {
  background: none; border: none; cursor: pointer;
  font-size: 14px; color: #3949ab; font-weight: 500; padding: 6px 0;
  &:hover { text-decoration: underline; }
}
.header-actions { display: flex; gap: 8px; }

.profile-card {
  background: #fff; border-radius: 14px; border: 1px solid #e0e0e0;
  padding: 28px; display: flex; align-items: center; gap: 20px;
}
.avatar {
  width: 72px; height: 72px; border-radius: 50%;
  background: #3949ab; color: #fff;
  font-size: 26px; font-weight: 700;
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.profile-main { flex: 1; }
.profile-name  { font-size: 22px; font-weight: 800; margin-bottom: 4px; }
.profile-email { font-size: 14px; color: #666; margin-bottom: 12px; }
.profile-badges { display: flex; gap: 8px; flex-wrap: wrap; }

.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 16px;
}
.info-card {
  background: #fff; border-radius: 12px; border: 1px solid #e0e0e0; padding: 20px;
}
.info-card-title {
  font-size: 13px; font-weight: 700; color: #3949ab;
  margin-bottom: 14px; padding-bottom: 8px; border-bottom: 1.5px solid #e8eaf6;
}
.info-rows { display: flex; flex-direction: column; gap: 12px; }
.info-row  { display: flex; justify-content: space-between; align-items: center; gap: 12px; }
.info-label { font-size: 13px; color: #888; }
.info-value { font-size: 13px; font-weight: 500; color: #1a1a2e; text-align: right; }

.role-badge {
  display: inline-block; padding: 2px 10px; border-radius: 99px; font-size: 12px; font-weight: 600;
  &.role-admin     { background: #fce4ec; color: #c2185b; }
  &.role-librarian { background: #e8eaf6; color: #3949ab; }
}
.status-badge {
  display: inline-block; padding: 2px 10px; border-radius: 99px; font-size: 12px; font-weight: 600;
  &.status-green { background: #e8f5e9; color: #2e7d32; }
  &.status-gray  { background: #f5f5f5; color: #757575; }
  &.status-red   { background: #ffebee; color: #c62828; }
}

.btn {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 9px 18px; border-radius: 8px; font-size: 14px;
  font-weight: 500; cursor: pointer; border: none; transition: all 0.15s;
  &:disabled { opacity: 0.5; cursor: not-allowed; }
  &.btn-primary { background: #3949ab; color: #fff; &:hover:not(:disabled) { background: #2c3a8c; } }
  &.btn-outline { background: #fff; color: #3949ab; border: 1.5px solid #3949ab; &:hover:not(:disabled) { background: #e8eaf6; } }
  &.btn-danger  { background: #e53935; color: #fff; &:hover:not(:disabled) { background: #c62828; } }
}

.modal-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,0.45);
  display: flex; align-items: center; justify-content: center; z-index: 1000; padding: 16px;
}
.modal {
  background: #fff; border-radius: 14px; width: 100%;
  max-width: 480px; max-height: 90vh; overflow-y: auto;
  box-shadow: 0 20px 60px rgba(0,0,0,0.2);
  display: block;
  height: unset;
  top: unset;
  left: unset;
}
.modal-sm { max-width: 400px; }
.modal-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 20px 24px 16px; border-bottom: 1px solid #f0f0f0;
  h3 { margin: 0; font-size: 17px; font-weight: 700; }
}
.modal-close {
  background: none; border: none; font-size: 18px;
  cursor: pointer; color: #aaa; padding: 4px 8px; border-radius: 6px;
  &:hover { background: #f0f0f0; }
}
.modal-body { padding: 20px 24px; p { margin: 0 0 8px; } }
.modal-footer {
  display: flex; justify-content: flex-end; gap: 8px;
  padding: 16px 24px 20px; border-top: 1px solid #f0f0f0;
}

.form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.form-group {
  display: flex; flex-direction: column; gap: 6px;
  label { font-size: 13px; font-weight: 600; color: #444; }
  input {
    padding: 8px 12px; border: 1.5px solid #e0e0e0;
    border-radius: 8px; font-size: 14px; outline: none; font-family: inherit;
    &:focus { border-color: #3949ab; }
  }
}
.required { color: #e53935; }
.field-error { color: #e53935; font-size: 12px; }
.text-muted { color: #888; font-size: 13px; }

.state-box {
  padding: 40px; text-align: center; color: #888; font-size: 14px;
  &.state-error { color: #c62828; }
}
</style>