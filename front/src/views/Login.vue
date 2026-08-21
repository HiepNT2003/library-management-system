<template>
  <div class="login_wrapper">
    <div class="container">
      <div class="login_form">
        <h1>Xin chào</h1>
        <p class="login_desc">Đăng nhập với tài khoản đã được cấp</p>
        <div class="information mt-24">
          <div class="ipt input_name mt-24" :class="{ error: errorMessage }">
            <Icon class="icon" icon="f7:person" width="24" height="24" />
            <input class="ipt_name" type="text" :class="{ 'input-error': errors.login }"
              placeholder="Nhập tài khoản hoặc email" required v-model="form.login" />
            <span v-if="errors.login" class="field-error">{{ errors.login }}</span>
          </div>
          <div class="ipt input_password mt-24" :class="{ error: errorMessage }">
            <Icon class="icon" icon="mynaui:lock-password" width="24" height="24" />
            <input class="ipt_pass" type="password" :class="{ 'input-error': errors.password }"
              placeholder="Nhập mật khẩu" required v-model="form.password" />
            <span v-if="errors.password" class="field-error">{{ errors.password }}</span>
          </div>
          <div class="error-box" v-if="loginError">
            ⚠️ {{ loginError }}
          </div>
          <button class="btn_login mt-24" type="submit" @click="handleLogin">
            Đăng nhập
          </button>
          <div class="forgot-link">
            <router-link to="/forgot-password">Quên mật khẩu?</router-link>
          </div>
          <router-link to="/" class="skip_login">Bỏ qua đăng nhập
            <Icon icon="cil:arrow-right" width="12" height="12" />
          </router-link>
          <div class="others mt-24">
            <div class="line"></div>
            <div class="text">
              <span class="bold">Hướng dẫn</span> <span class="">đăng nhập:</span>
            </div>
            <div class="line"></div>
          </div>
          <div class="instruction">
            <p>- Sinh viên dùng Email và Mật khẩu được cấp để đăng nhập.</p>
            <p>- Cán bộ giảng viên dùng Email và Mật khẩu được cấp để đăng nhập.</p>
          </div>
        </div>
      </div>
      <img class="login_image" src="../assets/Images/BooksImage.png" alt="" srcset="" />
    </div>
    <img class="vector" src="../assets/Images/Vector.png" alt="" srcset="" />
  </div>
</template>
<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useToastMessageStore } from '../stores/toastMessage';
import { TOAST_MESSAGE_STATUS } from '../constants';
import api from '../services/api'
import { Icon } from '@iconify/vue';

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const toasMessageStore = useToastMessageStore()

const loginRef = ref(null)
const isLoading = ref(false)
const showPassword = ref(false)
const loginError = ref('')

const form = reactive({ login: '', password: '' })
const errors = reactive({ login: '', password: '' })

onMounted(async () => {
  if (authStore.token) {
    await this.handleLogout()
  }
})

const validate = () => {
  errors.login = ''
  errors.password = ''

  if (!form.login.trim()) errors.login = 'Vui lòng nhập email hoặc tên đăng nhập'
  if (!form.password.trim()) errors.password = 'Vui lòng nhập mật khẩu'

  return !errors.login && !errors.password
}

const handleLogin = async () => {
  if (!validate()) return

  authStore.setIsLoadingApi(true)
  loginError.value = ''

  try {
    const res = await api.post('/auth/login', {
      Login: form.login.trim(),
      Password: form.password,
    })
    if (res.status == 200) {
      authStore.setAuth(res.data.accessToken, res.data.user)
      toasMessageStore.showToastMessage("Đăng nhập thành công", TOAST_MESSAGE_STATUS.success, 3000)
      if (res.data.user.roles?.includes('Admin') || res.data.user.roles?.includes('Librarian')) {
        router.push({ name: 'dashboard' })
      } else {
        router.push({ name: 'Discover' })
      }
    } else {
      toasMessageStore.showToastMessage("Đăng nhập thất bại!", TOAST_MESSAGE_STATUS.error, 3000)
    }
  } catch (err) {
    const msg = err.response?.data?.message || ''
    loginError.value = mapErrorMessage(msg)
  } finally {
    authStore.setIsLoadingApi(false)
  }
}

const redirectAfterLogin = () => {
  const user = authStore.user
  const roles = user?.roles ?? []

  // Nếu có redirect query thì về đó
  if (route.query.redirect) {
    router.push(route.query.redirect)
    return
  }

  // Redirect theo role
  if (roles.includes('Admin') || roles.includes('Librarian')) {
    router.push('/admin')
  } else {
    router.push('/')
  }
}

const handleLogout = async () => {
  authStore.setIsLoadingApi(true)
  try {
    await api.post('/auth/logout')
    authStore.logout()
  } catch (error) {
    console.log(error)
  }
  authStore.setIsLoadingApi(false)
}

const mapErrorMessage = (msg) => {
  const map = {
    'Invalid credentials': 'Email hoặc mật khẩu không đúng',
    'Invalid username or password': 'Email hoặc mật khẩu không đúng',
    'Account is expired': 'Tài khoản đã hết hạn. Vui lòng liên hệ thư viện.',
    'Account is inactive': 'Tài khoản chưa được kích hoạt. Vui lòng liên hệ thư viện.',
    'Account is blocked': 'Tài khoản đã bị khóa. Vui lòng liên hệ thư viện.',
    'Account locked': 'Tài khoản bị khóa tạm thời do đăng nhập sai quá nhiều lần.',
    'Email not confirmed': 'Email chưa được xác nhận.',
  }
  return map[msg] ?? (msg || 'Đăng nhập thất bại. Vui lòng thử lại.')
}
</script>
<style lang="scss" scoped>
.login_wrapper {
  background: #ffffff;
  height: 100vh;
  position: relative;
  overflow: hidden;
  font-family: system-ui, Avenir, Helvetica, Arial, sans-serif;

  .container {
    max-width: 1360px;
    // height: 768px;
    display: grid;
    grid-template-columns: 1fr 1.5fr;
    padding: 34px;
    align-items: center;
    gap: 16px;
    margin: auto;
    background-color: #ffffff;
    position: relative;
    height: 100%;
  }

  .login_form {
    margin: auto;
    color: #333333;

    h1 {
      font-size: 84px;
      line-height: 74px;
      margin: 0;
      text-align: center;
    }

    .login_desc {
      text-align: center;
      font-size: 14px;
      margin: 0;
      margin-top: 10px;
      margin-bottom: 24px;
    }

    .ipt {
      width: fit-content;
      position: relative;
      margin-bottom: 16px;

      &.error>input {
        background: rgba(255, 230, 230, 0.2) !important;
        border: 1px solid #ef4e4e;
      }

      &.error>input:-webkit-autofill {
        -webkit-box-shadow: 0 0 0 1000px rgba(255, 230, 230, 0.2) inset !important;
      }

      .input-error {
        border: 1px solid #e53935;
      }
    }

    input {
      width: 364px;
      height: 52px;
      border-radius: 13px;
      background: #ffffff;
      padding: 14px 14px 14px 42px;
      border: none;
      color: #1c1c1c;
      border: 1px solid #f2f2f2;

      &:focus {
        outline: none;
      }
    }

    input:-webkit-autofill,
    input:-webkit-autofill:hover,
    input:-webkit-autofill:focus,
    input:-webkit-autofill:active {
      -webkit-box-shadow: none !important;
      -webkit-text-fill-color: black !important;
      transition: background-color 5000s ease-in-out 0s;
      outline: none;
    }

    .icon {
      width: 24px;
      height: 24px;
      position: absolute;
      left: 14px;
      top: 23%;
      z-index: 1;
    }

    .btn_login {
      width: 100%;
      background: #1c1c1c;
      color: #ffffff;
      text-align: center;
      height: 52px;
      border-radius: 13px;
      cursor: pointer;

      &.disable {
        background: #504f4f;
        cursor: default;
      }
    }

    .skip_login,
    .forgot-link {
      font-size: 14px;
      display: flex;
      gap: 4px;
      align-items: center;
      margin-bottom: 12px;
      margin-left: 4px;
    }

    .error_message {
      margin: 0;
      padding-top: 8px;
      font-size: 14px;
      color: rgb(250, 36, 36);
    }

    .others {
      display: flex;
      align-items: center;
      justify-content: center;

      .text {
        display: flex;
        flex-shrink: 0;
        padding: 0 6px;
        background: #ffffff;
        gap: 2px;
      }

      .line {
        height: 1px;
        background: #f0edff;
        width: 100%;
      }

      span {
        font-size: 16px;
      }
    }

    .others_login {
      width: 100%;
      background: transparent;
      border: 1px solid #1c1c1c;
      padding: 10px;
      border-radius: 13px;
      display: flex;
      gap: 8px;
      align-items: center;
      justify-content: center;
      cursor: pointer;

      .text {
        display: flex;
        gap: 2px;
      }
    }

    .login_fb {
      margin-top: 16px;
    }

    .icon_fb {
      color: #1877f2 !important;
    }
  }

  .field-error {
    color: #e53935;
    font-size: 12px;
    display: block;
  }

  .error-box {
    padding: 12px 14px;
    background: #ffebee;
    border-left: 3px solid #e53935;
    border-radius: 0 8px 8px 0;
    font-size: 13px;
    color: #c62828;
    line-height: 1.5;
    margin-bottom: 4px;
  }

  .login_image {
    justify-self: flex-end;
    max-width: 88%;
    border-radius: 38px;
  }

  .bold {
    font-weight: 700;
  }

  .vector {
    position: absolute;
    top: 2px;
    left: -60px;
  }

  .instruction {
    max-width: 364px;
    margin-top: 8px;
  }

  @media (max-width: 1204px) {
    .login_form {
      h1 {
        font-size: 56px;
        text-align: center;
      }
    }
  }

  @media (max-width: 1080px) {
    background-image: url('../assets/Images/Background_login.png');

    .container {
      grid-template-columns: 1fr;

      .login_image {
        display: none;
      }

      .login_form {
        padding: 48px;
        border: 2px solid #fb9b4e;
        border-radius: 20px;
      }
    }

    .vector {
      display: none;
    }
  }

  @media (max-width: 600px) {
    .container {
      grid-template-columns: 1fr;

      .login_image {
        display: none;
      }

      .login_form {
        padding: 30px;
        border: 2px solid #fb9b4e;
        border-radius: 20px;

        h1 {
          font-size: 40px;
          line-height: 52px;
        }

        .ipt {
          width: auto;
        }

        input {
          width: 100%;
        }
      }

      .error_message {
        font-size: 12px;
      }
    }
  }
}
</style>
