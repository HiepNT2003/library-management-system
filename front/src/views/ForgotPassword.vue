<template>
    <div class="forgot-page">
        <div class="forgot-card">
            <div class="forgot-logo">
                <div class="logo-icon"><img src="../assets/Images/LogoUTC.png" alt="" srcset=""></div>
                <div class="logo-text">
                    <div class="logo-name">Thư viện UTC</div>
                    <div class="logo-sub">Đặt lại mật khẩu</div>
                </div>
            </div>

            <!-- Bước 1 -->
            <div v-if="step === 1">
                <h2 class="forgot-title">Quên mật khẩu?</h2>
                <p class="forgot-desc">
                    Nhập email và mã sinh viên/cán bộ để đặt lại mật khẩu về mặc định
                </p>
                <div class="form-group">
                    <label>Email</label>
                    <input v-model="form.email" type="email" placeholder="email@utc.edu.vn" />
                    <span class="field-error" v-if="errors.email">{{ errors.email }}</span>
                </div>
                <div class="form-group">
                    <label>Mã sinh viên / Mã cán bộ</label>
                    <input v-model="form.verifyCode" type="text" placeholder="VD: 5240083 hoặc CB001" />
                    <span class="field-error" v-if="errors.verifyCode">{{ errors.verifyCode }}</span>
                </div>
                <div class="error-box" v-if="errorMsg">⚠️ {{ errorMsg }}</div>
                <button class="btn-submit" @click="submit" :disabled="isLoading">
                    {{ isLoading ? 'Đang xử lý...' : 'Đặt lại mật khẩu' }}
                </button>
            </div>

            <!-- Bước 2: Thành công -->
            <div v-if="step === 2" class="success-box">
                <div class="success-icon">
                    <icon class="icon_success" icon="charm:circle-tick" width="56" height="56" />
                </div>
                <h2 class="forgot-title">Đặt lại thành công!</h2>
                <p class="forgot-desc">
                    Mật khẩu của bạn đã được đặt lại thành:
                </p>
                <div class="new-password">
                    <code>{{ newPassword }}</code>
                </div>
                <p class="forgot-desc warn">
                    ⚠️ Vui lòng đăng nhập và đổi mật khẩu ngay sau đó
                </p>
                <button class="btn-submit" @click="$router.push('/login')">
                    Đăng nhập ngay
                </button>
            </div>

            <div class="back-link">
                <router-link to="/login">← Quay lại đăng nhập</router-link>
            </div>
        </div>

        <!-- Background -->
        <div class="bg-decoration">
            <div class="bg-circle bg-circle-1"></div>
            <div class="bg-circle bg-circle-2"></div>
        </div>
    </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import api from '../services/api'
import { Icon } from '@iconify/vue'

const step = ref(1)
const isLoading = ref(false)
const errorMsg = ref('')
const newPassword = ref('')

const form = reactive({ email: '', verifyCode: '' })
const errors = reactive({ email: '', verifyCode: '' })

const validate = () => {
    errors.email = !form.email.trim() ? 'Vui lòng nhập email' : ''
    errors.verifyCode = !form.verifyCode.trim() ? 'Vui lòng nhập mã xác nhận' : ''
    return !errors.email && !errors.verifyCode
}

const submit = async () => {
    if (!validate()) return
    isLoading.value = true
    errorMsg.value = ''
    try {
        const res = await api.post('/auth/forgot-password', {
            email: form.email.trim(),
            verifyCode: form.verifyCode.trim()
        })
        if (res.status === 200) {
            newPassword.value = `${form.verifyCode.trim()}@Utc1`
            step.value = 2
        }
    } catch (err) {
        errorMsg.value = err.response?.data?.message || 'Đặt lại thất bại. Vui lòng thử lại.'
    } finally {
        isLoading.value = false
    }
}
</script>

<style lang="scss" scoped>
.forgot-page {
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    background: linear-gradient(135deg, #1F3864 0%, #283593 50%, #3949ab 100%);
    padding: 20px;
    position: relative;
    overflow: hidden;
    font-family: 'Segoe UI', sans-serif;
}

.bg-decoration {
    position: absolute;
    inset: 0;
    pointer-events: none;
}

.bg-circle {
    position: absolute;
    border-radius: 50%;
    background: rgba(255, 255, 255, 0.05);

    &.bg-circle-1 {
        width: 400px;
        height: 400px;
        top: -100px;
        right: -100px;
    }

    &.bg-circle-2 {
        width: 300px;
        height: 300px;
        bottom: -80px;
        left: -80px;
    }
}

.forgot-card {
    background: #fff;
    border-radius: 20px;
    padding: 40px;
    width: 100%;
    max-width: 420px;
    box-shadow: 0 24px 64px rgba(0, 0, 0, 0.2);
    position: relative;
    z-index: 1;
}

.forgot-logo {
    display: flex;
    align-items: center;
    gap: 12px;
    margin-bottom: 24px;
    justify-content: center;
}

.logo-icon {
    font-size: 32px;

    img {
        width: 32px;
    }
}

.logo-name {
    font-size: 15px;
    font-weight: 800;
    color: #3949ab;
}

.logo-sub {
    font-size: 11px;
    color: #888;
    margin-top: 2px;
}

.forgot-title {
    font-size: 20px;
    font-weight: 800;
    color: #1a1a2e;
    margin: 0 0 8px;
    text-align: center;
}

.forgot-desc {
    font-size: 13px;
    color: #888;
    text-align: center;
    margin: 0 0 20px;
    line-height: 1.6;
}

.form-group {
    display: flex;
    flex-direction: column;
    gap: 6px;
    margin-bottom: 16px;

    label {
        font-size: 13px;
        font-weight: 600;
        color: #444;
    }

    input {
        padding: 10px 14px;
        border: 1.5px solid #e0e0e0;
        border-radius: 10px;
        font-size: 14px;
        outline: none;
        font-family: inherit;

        &:focus {
            border-color: #3949ab;
        }
    }
}

.field-error {
    color: #e53935;
    font-size: 12px;
}

.error-box {
    padding: 10px 14px;
    background: #ffebee;
    border-left: 3px solid #e53935;
    border-radius: 0 8px 8px 0;
    font-size: 13px;
    color: #c62828;
    margin-bottom: 16px;
}

.btn-submit {
    width: 100%;
    padding: 12px;
    background: #3949ab;
    color: #fff;
    border: none;
    border-radius: 10px;
    font-size: 15px;
    font-weight: 700;
    cursor: pointer;
    transition: background 0.15s;

    &:hover:not(:disabled) {
        background: #2c3a8c;
    }

    &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
    }
}

.success-box {
    text-align: center;
}

.success-icon {
    font-size: 52px;
    margin-bottom: 12px;
    color: #2e7d32;
}

.new-password {
    margin: 12px 0;
    padding: 12px 20px;
    background: #e8eaf6;
    border-radius: 10px;
    display: inline-block;

    code {
        font-size: 20px;
        font-weight: 700;
        color: #3949ab;
        letter-spacing: 2px;
    }
}

.warn {
    color: #e65100 !important;
}

.back-link {
    text-align: center;
    margin-top: 20px;

    a {
        color: #3949ab;
        font-size: 13px;
        text-decoration: none;

        &:hover {
            text-decoration: underline;
        }
    }
}
</style>