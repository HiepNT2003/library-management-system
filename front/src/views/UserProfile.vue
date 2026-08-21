<template>
    <div class="user-profile">

        <div class="page-header">
            <div>
                <h1 class="page-title">Thông tin cá nhân</h1>
                <p class="page-desc">Xem và cập nhật thông tin tài khoản của bạn</p>
            </div>
        </div>

        <div v-if="isLoading" class="state-box">Đang tải...</div>

        <template v-else-if="profile">
            <div class="profile-layout">

                <!-- Left: Avatar + basic info -->
                <div class="profile-sidebar">
                    <div class="avatar-section">
                        <div class="avatar-wrapper">
                            <img v-if="profile.avatarUrl" :src="profile.avatarUrl" class="avatar-img" />
                            <div v-else class="avatar-placeholder">{{ initials }}</div>
                        </div>
                        <div class="profile-name">{{ profile.fullName }}</div>
                        <div class="profile-email">{{ profile.email }}</div>
                        <div class="role-badges">
                            <span v-for="role in profile.roles" :key="role" class="role-badge" :class="roleClass(role)">
                                {{ roleLabel(role) }}
                            </span>
                        </div>
                    </div>

                    <!-- Card info -->
                    <div class="card-info">
                        <div class="card-info-title">Thẻ thư viện</div>
                        <div class="card-info-row">
                            <span>Trạng thái</span>
                            <span class="status-badge" :class="statusClass">{{ statusLabel }}</span>
                        </div>
                        <div class="card-info-row" v-if="profile.expiredDate">
                            <span>Hạn thẻ</span>
                            <span :class="isExpired ? 'text-red' : ''">{{ formatDate(profile.expiredDate) }}</span>
                        </div>
                    </div>

                    <!-- Student/Staff profile -->
                    <div class="profile-code-card" v-if="profile.studentProfile">
                        <div class="code-label">Mã sinh viên</div>
                        <div class="code-value">{{ profile.studentProfile.studentCode }}</div>
                        <div class="code-details">
                            <div v-if="profile.studentProfile.faculty">{{ profile.studentProfile.faculty }}</div>
                            <div v-if="profile.studentProfile.class">Lớp {{ profile.studentProfile.class }}</div>
                            <div v-if="profile.studentProfile.major">{{ profile.studentProfile.major }}</div>
                        </div>
                    </div>

                    <div class="profile-code-card" v-else-if="profile.staffProfile">
                        <div class="code-label">Mã nhân viên</div>
                        <div class="code-value">{{ profile.staffProfile.staffCode }}</div>
                        <div class="code-details">
                            <div v-if="profile.staffProfile.department">{{ profile.staffProfile.department }}</div>
                            <div v-if="profile.staffProfile.position">{{ profile.staffProfile.position }}</div>
                        </div>
                    </div>
                </div>

                <!-- Right: Tabs -->
                <div class="profile-main">
                    <div class="tabs">
                        <button v-for="tab in tabs" :key="tab.key" class="tab-btn"
                            :class="{ active: activeTab === tab.key }" @click="activeTab = tab.key">
                            <Icon :icon="tab.icon" width="20" height="20" />
                            {{ tab.label }}
                        </button>
                    </div>

                    <!-- Tab: Thông tin cá nhân -->
                    <div v-if="activeTab === 'info'" class="tab-content">
                        <div class="form-grid">
                            <div class="form-group">
                                <label>Họ và tên</label>
                                <input v-model="editForm.fullName" placeholder="Họ và tên..." />
                            </div>
                            <div class="form-group">
                                <label>Email</label>
                                <input :value="profile.email" disabled class="input-disabled" />
                            </div>
                            <div class="form-group">
                                <label>Số điện thoại</label>
                                <input v-model="editForm.phoneNumber" placeholder="0xxx..." />
                            </div>
                        </div>

                        <div class="form-actions">
                            <button class="btn btn-outline" @click="resetForm">Huỷ thay đổi</button>
                            <button class="btn btn-primary" @click="saveInfo" :disabled="isSaving">
                                <Icon v-if="!isSaving" icon="proicons:save" width="20" height="20" />
                                {{ isSaving ? 'Đang lưu...' : 'Lưu thay đổi' }}
                            </button>
                        </div>

                        <div class="success-msg" v-if="infoSuccess">
                            <Icon class="icon_tick" icon="charm:circle-tick" width="16" height="16" /> {{ infoSuccess }}
                        </div>
                    </div>

                    <!-- Tab: Đổi mật khẩu -->
                    <div v-if="activeTab === 'password'" class="tab-content">
                        <div class="password-form">
                            <div class="form-group">
                                <label>Mật khẩu hiện tại <span class="required">*</span></label>
                                <div class="password-input-wrap">
                                    <input :type="showCurrentPwd ? 'text' : 'password'"
                                        v-model="pwdForm.currentPassword" placeholder="Nhập mật khẩu hiện tại..."
                                        :class="{ 'input-error': pwdErrors.currentPassword }" />
                                    <button class="toggle-pwd" @click="showCurrentPwd = !showCurrentPwd">
                                        <Icon v-if="showCurrentPwd" icon="fluent:eye-off-16-filled" width="20" height="20" />
                                        <Icon v-else icon="fluent:eye-16-filled" width="20" height="20" />
                                    </button>
                                </div>
                                <span v-if="pwdErrors.currentPassword" class="field-error">{{ pwdErrors.currentPassword
                                    }}</span>
                            </div>

                            <div class="form-group">
                                <label>Mật khẩu mới <span class="required">*</span></label>
                                <div class="password-input-wrap">
                                    <input :type="showNewPwd ? 'text' : 'password'" v-model="pwdForm.newPassword"
                                        placeholder="Tối thiểu 6 ký tự..."
                                        :class="{ 'input-error': pwdErrors.newPassword }" />
                                    <button class="toggle-pwd" @click="showNewPwd = !showNewPwd">
                                        <Icon v-if="showNewPwd" icon="fluent:eye-off-16-filled" width="20" height="20" />
                                        <Icon v-else icon="fluent:eye-16-filled" width="20" height="20" />
                                    </button>
                                </div>
                                <div class="password-strength" v-if="pwdForm.newPassword">
                                    <div class="strength-bar">
                                        <div class="strength-fill" :class="strengthClass"
                                            :style="{ width: strengthWidth }"></div>
                                    </div>
                                    <span class="strength-label" :class="strengthClass">{{ strengthLabel }}</span>
                                </div>
                                <span v-if="pwdErrors.newPassword" class="field-error">{{ pwdErrors.newPassword
                                    }}</span>
                            </div>

                            <div class="form-group">
                                <label>Xác nhận mật khẩu mới <span class="required">*</span></label>
                                <div class="password-input-wrap">
                                    <input :type="showConfirmPwd ? 'text' : 'password'"
                                        v-model="pwdForm.confirmPassword" placeholder="Nhập lại mật khẩu mới..."
                                        :class="{ 'input-error': pwdErrors.confirmPassword }" />
                                    <button class="toggle-pwd" @click="showConfirmPwd = !showConfirmPwd">
                                        <Icon v-if="showConfirmPwd" icon="fluent:eye-off-16-filled" width="20" height="20" />
                                        <Icon v-else icon="fluent:eye-16-filled" width="20" height="20" />
                                    </button>
                                </div>
                                <span v-if="pwdErrors.confirmPassword" class="field-error">{{ pwdErrors.confirmPassword
                                    }}</span>
                            </div>

                            <div class="form-actions">
                                <button class="btn btn-primary" @click="changePassword" :disabled="isChangingPwd">
                                    <Icon v-if="!isChangingPwd" icon="si:lock-line" width="20" height="20" />
                                    {{ isChangingPwd ? 'Đang xử lý...' : 'Đổi mật khẩu' }}
                                </button>
                            </div>

                            <div class="success-msg" v-if="pwdSuccess">
                                <Icon class="icon_tick" icon="charm:circle-tick" width="16" height="16" /> {{ pwdSuccess
                                }}
                            </div>
                            <div class="error-msg" v-if="pwdError">❌ {{ pwdError }}</div>
                        </div>
                    </div>

                </div>
            </div>
        </template>

    </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import api from '../services/api'
import { Icon } from '@iconify/vue'
import { useRoute } from 'vue-router'

const isLoading = ref(false)
const isSaving = ref(false)
const profile = ref(null)
const activeTab = ref('info')
const infoSuccess = ref('')

const tabs = [
    { key: 'info', label: 'Thông tin cá nhân', icon: 'gg:profile' },
    { key: 'password', label: 'Đổi mật khẩu', icon: 'si:lock-line'},
]

const editForm = reactive({
    fullName: '', phoneNumber: '', avatarUrl: ''
})

// Password
const pwdForm = reactive({ currentPassword: '', newPassword: '', confirmPassword: '' })
const pwdErrors = reactive({})
const pwdSuccess = ref('')
const pwdError = ref('')
const isChangingPwd = ref(false)
const showCurrentPwd = ref(false)
const showNewPwd = ref(false)
const showConfirmPwd = ref(false)

const fetchProfile = async () => {
    isLoading.value = true
    try {
        const res = await api.get('/account/profile')
        if (res.status === 200) {
            profile.value = res.data
            Object.assign(editForm, {
                fullName: res.data.fullName || '',
                phoneNumber: res.data.phoneNumber || '',
                avatarUrl: res.data.avatarUrl || ''
            })
        }
    } catch { }
    finally { isLoading.value = false }
}

const resetForm = () => {
    if (!profile.value) return
    Object.assign(editForm, {
        fullName: profile.value.fullName || '',
        phoneNumber: profile.value.phoneNumber || '',
    })
    infoSuccess.value = ''
}

const saveInfo = async () => {
    isSaving.value = true
    infoSuccess.value = ''
    try {
        const res = await api.put('/account/me', editForm)
        if (res.status === 200) {
            infoSuccess.value = 'Cập nhật thông tin thành công'
            await fetchProfile()
        }
    } catch (err) {
        alert(err.response?.data?.message || 'Cập nhật thất bại')
    } finally {
        isSaving.value = false
    }
}

const changePassword = async () => {
    // Validate
    Object.keys(pwdErrors).forEach(k => delete pwdErrors[k])
    pwdSuccess.value = ''
    pwdError.value = ''

    if (!pwdForm.currentPassword) pwdErrors.currentPassword = 'Vui lòng nhập mật khẩu hiện tại'
    if (!pwdForm.newPassword) pwdErrors.newPassword = 'Vui lòng nhập mật khẩu mới'
    else if (pwdForm.newPassword.length < 6) pwdErrors.newPassword = 'Mật khẩu tối thiểu 6 ký tự'
    if (!pwdForm.confirmPassword) pwdErrors.confirmPassword = 'Vui lòng xác nhận mật khẩu'
    else if (pwdForm.newPassword !== pwdForm.confirmPassword) pwdErrors.confirmPassword = 'Mật khẩu không khớp'

    if (Object.keys(pwdErrors).length > 0) return

    isChangingPwd.value = true
    try {
        const res = await api.post('/account/me/change-password', {
            currentPassword: pwdForm.currentPassword,
            newPassword: pwdForm.newPassword
        })
        if (res.status === 200) {
            pwdSuccess.value = 'Đổi mật khẩu thành công'
            pwdForm.currentPassword = ''
            pwdForm.newPassword = ''
            pwdForm.confirmPassword = ''
        }
    } catch (err) {
        pwdError.value = err.response?.data?.message || 'Đổi mật khẩu thất bại'
    } finally {
        isChangingPwd.value = false
    }
}

// Computed
const initials = computed(() => {
    const name = profile.value?.fullName || ''
    return name.split(' ').map(w => w[0]).slice(-2).join('').toUpperCase() || '?'
})

const isExpired = computed(() => {
    if (!profile.value?.expiredDate) return false
    return new Date(profile.value.expiredDate) < new Date()
})

const statusLabel = computed(() => {
    const map = { 0: 'Hoạt động', 1: 'Chưa kích hoạt', 2: 'Đã khóa' }
    return map[profile.value?.status] ?? '—'
})

const statusClass = computed(() => {
    const map = { 0: 'status-green', 1: 'status-gray', 2: 'status-red' }
    return map[profile.value?.status] ?? ''
})

const passwordStrength = computed(() => {
    const pwd = pwdForm.newPassword
    if (!pwd) return 0
    let score = 0
    if (pwd.length >= 6) score++
    if (pwd.length >= 10) score++
    if (/[A-Z]/.test(pwd)) score++
    if (/[0-9]/.test(pwd)) score++
    if (/[^A-Za-z0-9]/.test(pwd)) score++
    return score
})

const strengthClass = computed(() => {
    const s = passwordStrength.value
    if (s <= 1) return 'strength-weak'
    if (s <= 3) return 'strength-medium'
    return 'strength-strong'
})

const strengthWidth = computed(() => `${(passwordStrength.value / 5) * 100}%`)

const strengthLabel = computed(() => {
    const s = passwordStrength.value
    if (s <= 1) return 'Yếu'
    if (s <= 3) return 'Trung bình'
    return 'Mạnh'
})

const roleLabel = (r) => ({ Admin: 'Quản trị', Librarian: 'Thủ thư', Student: 'Sinh viên', Staff: 'Nhân viên' })[r] ?? r
const roleClass = (r) => ({ Admin: 'role-admin', Librarian: 'role-librarian', Student: 'role-student', Staff: 'role-staff' })[r] ?? ''
const formatDate = (d) => d ? new Date(d).toLocaleDateString('vi-VN') : '—'

onMounted(() => {
    const route = useRoute()
    if (route.query.screen == 'change-password') {
        activeTab.value = 'password'
    } else {
        activeTab.value = 'info'
    }
    fetchProfile()
})

</script>

<style lang="scss" scoped>
.user-profile {
    display: flex;
    flex-direction: column;
    gap: 20px;
    font-family: 'Segoe UI', sans-serif;
    color: #1a1a2e;
}

.page-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
}

.page-title {
    font-size: 22px;
    font-weight: 800;
    margin: 0 0 4px;
}

.page-desc {
    font-size: 14px;
    color: #666;
    margin: 0;
}

.profile-layout {
    display: grid;
    grid-template-columns: 280px 1fr;
    gap: 20px;
    align-items: start;

    @media (max-width: 768px) {
        grid-template-columns: 1fr;
    }
}

// Sidebar
.profile-sidebar {
    display: flex;
    flex-direction: column;
    gap: 14px;
}

.avatar-section {
    background: #fff;
    border-radius: 14px;
    border: 1px solid #e0e0e0;
    padding: 24px 20px;
    text-align: center;
}

.avatar-wrapper {
    margin-bottom: 14px;
    display: flex;
    justify-content: center;
}

.avatar-img {
    width: 80px;
    height: 80px;
    border-radius: 50%;
    object-fit: cover;
    border: 3px solid #e8eaf6;
}

.avatar-placeholder {
    width: 80px;
    height: 80px;
    border-radius: 50%;
    background: #3949ab;
    color: #fff;
    font-size: 24px;
    font-weight: 700;
    display: flex;
    align-items: center;
    justify-content: center;
}

.profile-name {
    font-size: 16px;
    font-weight: 800;
    margin-bottom: 4px;
}

.profile-email {
    font-size: 13px;
    color: #888;
    margin-bottom: 10px;
}

.role-badges {
    display: flex;
    gap: 6px;
    justify-content: center;
    flex-wrap: wrap;
}

.role-badge {
    display: inline-block;
    padding: 3px 10px;
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

    &.role-student {
        background: #e8f5e9;
        color: #2e7d32;
    }

    &.role-staff {
        background: #fff3e0;
        color: #e65100;
    }
}

.card-info {
    background: #fff;
    border-radius: 12px;
    border: 1px solid #e0e0e0;
    padding: 16px;
}

.card-info-title {
    font-size: 12px;
    font-weight: 700;
    color: #888;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    margin-bottom: 10px;
}

.card-info-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 13px;
    padding: 6px 0;
    border-bottom: 1px solid #f5f5f5;

    &:last-child {
        border-bottom: none;
    }

    span:first-child {
        color: #888;
    }
}

.status-badge {
    display: inline-block;
    padding: 2px 8px;
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
}

.text-red {
    color: #c62828;
    font-weight: 600;
}

.profile-code-card {
    background: #f0f4ff;
    border-radius: 12px;
    border: 1.5px solid #c5cae9;
    padding: 16px;
}

.code-label {
    font-size: 11px;
    font-weight: 700;
    color: #3949ab;
    text-transform: uppercase;
    margin-bottom: 4px;
}

.code-value {
    font-size: 20px;
    font-weight: 800;
    color: #3949ab;
    font-family: monospace;
    margin-bottom: 8px;
}

.code-details {
    font-size: 13px;
    color: #555;
    line-height: 1.6;
}

// Main
.profile-main {
    background: #fff;
    border-radius: 14px;
    border: 1px solid #e0e0e0;
    overflow: hidden;
}

.tabs {
    display: flex;
    border-bottom: 1.5px solid #e0e0e0;
    background: #fafafa;
}

.tab-btn {
    padding: 14px 20px;
    background: none;
    border: none;
    cursor: pointer;
    font-size: 14px;
    font-weight: 500;
    color: #666;
    border-bottom: 2px solid transparent;
    margin-bottom: -1.5px;
    transition: all 0.15s;
    display: flex;
    gap: 8px;
    align-items: center;

    &:hover {
        color: #3949ab;
    }

    &.active {
        color: #3949ab;
        border-bottom-color: #3949ab;
        font-weight: 700;
        background: #fff;
    }
}

.tab-content {
    padding: 24px;
}

.form-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 16px;
    margin-bottom: 20px;

    @media (max-width: 600px) {
        grid-template-columns: 1fr;
    }
}

.form-group {
    display: flex;
    flex-direction: column;
    gap: 6px;

    &.form-full {
        grid-column: 1 / -1;
    }

    label {
        font-size: 13px;
        font-weight: 600;
        color: #444;
    }

    input,
    select {
        padding: 9px 12px;
        border: 1.5px solid #e0e0e0;
        border-radius: 8px;
        font-size: 14px;
        outline: none;
        font-family: inherit;
        background: #fff;

        &:focus {
            border-color: #3949ab;
        }

        &.input-error {
            border-color: #e53935;
        }
    }
}

.input-disabled {
    background: #f5f5f5 !important;
    color: #888;
    cursor: not-allowed;
}

.avatar-preview {
    width: 60px;
    height: 60px;
    border-radius: 50%;
    object-fit: cover;
    border: 2px solid #e0e0e0;
    margin-top: 6px;
}

.form-actions {
    display: flex;
    gap: 10px;
    justify-content: flex-end;
}

.success-msg {
    display: flex;
    gap: 6px;
    align-items: center;
    margin-top: 12px;
    padding: 10px 14px;
    background: #e8f5e9;
    border-radius: 8px;
    font-size: 13px;
    color: #2e7d32;
}

.error-msg {
    margin-top: 12px;
    padding: 10px 14px;
    background: #ffebee;
    border-radius: 8px;
    font-size: 13px;
    color: #c62828;
}

// Password form
.password-form {
    max-width: 440px;
    display: flex;
    flex-direction: column;
    gap: 16px;
}

.password-input-wrap {
    position: relative;

    input {
        width: 100%;
        box-sizing: border-box;
        padding-right: 40px;
    }
}

.toggle-pwd {
    position: absolute;
    right: 8px;
    top: 50%;
    transform: translateY(-50%);
    background: none;
    border: none;
    cursor: pointer;
    font-size: 16px;
    padding: 4px;
}

.required {
    color: #e53935;
}

.field-error {
    color: #e53935;
    font-size: 12px;
}

.password-strength {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-top: 6px;
}

.strength-bar {
    flex: 1;
    height: 4px;
    background: #e0e0e0;
    border-radius: 2px;
    overflow: hidden;
}

.strength-fill {
    height: 100%;
    border-radius: 2px;
    transition: width 0.3s, background 0.3s;

    &.strength-weak {
        background: #e53935;
    }

    &.strength-medium {
        background: #fb8c00;
    }

    &.strength-strong {
        background: #2e7d32;
    }
}

.strength-label {
    font-size: 12px;
    font-weight: 600;
    white-space: nowrap;

    &.strength-weak {
        color: #e53935;
    }

    &.strength-medium {
        color: #fb8c00;
    }

    &.strength-strong {
        color: #2e7d32;
    }
}

// Buttons
.btn {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 9px 18px;
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
}

.state-box {
    padding: 40px;
    text-align: center;
    color: #888;
}
</style>