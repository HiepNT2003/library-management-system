<template>
  <Teleport to="body">
    <div v-if="modelValue" class="modal-overlay" @click.self="$emit('update:modelValue', false)">
      <div class="modal">
        <div class="modal-header">
          <h3>Đặt mượn sách</h3>
          <button class="modal-close" @click="$emit('update:modelValue', false)">✕</button>
        </div>

        <div class="modal-body">
          <!-- Book preview -->
          <div class="book-preview" v-if="book">
            <img v-if="book.imageUrl" :src="book.imageUrl" class="preview-img" />
            <div class="preview-no-img" v-else>📖</div>
            <div class="preview-info">
              <div class="preview-title">{{ book.title }}</div>
              <div class="preview-author" v-if="book.authors?.length">
                {{ book.authors.map(a => a.name || a).join(', ') }}
              </div>
              <div class="preview-avail" v-if="book.availableCopies != null">
                Còn <strong class="text-green">{{ book.availableCopies }}</strong> bản sao có sẵn
              </div>
            </div>
          </div>

          <!-- Form -->
          <div class="form-group">
            <label>
              Ngày dự kiến đến lấy
              <span class="optional">tuỳ chọn</span>
            </label>
            <input
              type="date"
              v-model="form.expectedDate"
              :min="minDate"
              :max="maxDate"
              :class="{ 'input-error': errors.expectedDate }"
              @change="validateDate"
            />
            <span class="field-error" v-if="errors.expectedDate">
              {{ errors.expectedDate }}
            </span>
            <span class="field-hint" v-else>
              Chọn ngày trong vòng 7 ngày tới (từ ngày mai)
            </span>
          </div>

          <div class="form-group">
            <label>
              Ghi chú
              <span class="optional">tuỳ chọn</span>
            </label>
            <textarea
              v-model="form.note"
              rows="2"
              placeholder="Ghi chú thêm nếu có..."
            ></textarea>
          </div>

          <!-- Submit error -->
          <div class="submit-error" v-if="errors.submit">
            ❌ {{ errors.submit }}
          </div>
        </div>

        <div class="modal-footer">
          <button class="btn btn-outline" @click="$emit('update:modelValue', false)">
            Huỷ
          </button>
          <button
            class="btn btn-primary"
            @click="submit"
            :disabled="isSubmitting || !!errors.expectedDate"
          >
            {{ isSubmitting ? 'Đang gửi...' : '📚 Gửi yêu cầu mượn' }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { ref, reactive, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import api from '../../services/api'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  book:       { type: Object, default: null   }
})

const emit = defineEmits(['update:modelValue', 'success'])

const router    = useRouter()
const authStore = useAuthStore()

const isSubmitting = ref(false)
const form  = reactive({ expectedDate: '', note: '' })
const errors = reactive({})

// Reset form khi mở modal
watch(() => props.modelValue, (val) => {
  if (val) {
    form.expectedDate = ''
    form.note         = ''
    Object.keys(errors).forEach(k => delete errors[k])
  }
})

// Giới hạn ngày
const minDate = computed(() => {
  const d = new Date()
  d.setDate(d.getDate() + 1)
  return d.toISOString().slice(0, 10)
})

const maxDate = computed(() => {
  const d = new Date()
  d.setDate(d.getDate() + 7)
  return d.toISOString().slice(0, 10)
})

const validateDate = () => {
  delete errors.expectedDate
  if (!form.expectedDate) return true

  const selected = new Date(form.expectedDate)
  const min      = new Date(minDate.value)
  const max      = new Date(maxDate.value)

  selected.setHours(0, 0, 0, 0)
  min.setHours(0, 0, 0, 0)
  max.setHours(0, 0, 0, 0)

  if (selected < min) {
    errors.expectedDate = 'Ngày lấy sách phải từ ngày mai trở đi'
    return false
  }
  if (selected > max) {
    errors.expectedDate = 'Ngày lấy sách không được quá 7 ngày kể từ hôm nay'
    return false
  }
  return true
}

const submit = async () => {
  if (!authStore.user) {
    router.push('/login')
    return
  }
  if (!validateDate()) return

  delete errors.submit
  isSubmitting.value = true
  authStore.setIsLoadingApi(true)
  try {
    const res = await api.post('/BorrowRequests', {
      bookId:             props.book?.bookId,
      expectedBorrowDate: form.expectedDate || null,
      note:               form.note || null
    })
    if (res.status === 200 || res.status === 201) {
      emit('update:modelValue', false)
      emit('success', res.data)
    }
  } catch (err) {
    errors.submit = err.response?.data?.message || 'Gửi yêu cầu thất bại'
  } finally {
    isSubmitting.value = false
  }
  authStore.setIsLoadingApi(false)
}
</script>

<style lang="scss" scoped>
.modal-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,0.45);
  display: flex; align-items: center; justify-content: center;
  z-index: 1000; padding: 16px;
}

.modal {
  background: #fff; border-radius: 14px; width: 100%;
  max-width: 480px; box-shadow: 0 20px 60px rgba(0,0,0,0.2);
  font-family: 'Segoe UI', sans-serif;
  top: unset;
  left: unset;
  display: block;
  height: unset;
}

.modal-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 20px 24px 16px; border-bottom: 1px solid #f0f0f0;
  h3 { margin: 0; font-size: 17px; font-weight: 700; color: #1a1a2e; }
}

.modal-close {
  background: none; border: none; font-size: 18px; cursor: pointer;
  color: #aaa; padding: 4px 8px; border-radius: 6px;
  &:hover { background: #f0f0f0; }
}

.modal-body {
  padding: 20px 24px;
  display: flex; flex-direction: column; gap: 16px;
}

.modal-footer {
  display: flex; justify-content: flex-end; gap: 8px;
  padding: 16px 24px 20px; border-top: 1px solid #f0f0f0;
}

// Book preview
.book-preview {
  display: flex; gap: 12px; align-items: center;
  padding: 12px 14px; background: #f9f9f9; border-radius: 10px;
}

.preview-img {
  width: 52px; height: 68px; object-fit: cover;
  border-radius: 6px; flex-shrink: 0; border: 1px solid #e0e0e0;
}

.preview-no-img {
  width: 52px; height: 68px; flex-shrink: 0;
  background: linear-gradient(135deg, #e8eaf6, #c5cae9);
  border-radius: 6px; display: flex; align-items: center;
  justify-content: center; font-size: 24px;
}

.preview-info { flex: 1; min-width: 0; }
.preview-title  { font-size: 14px; font-weight: 700; line-height: 1.3; margin-bottom: 4px; color: #1a1a2e; }
.preview-author { font-size: 13px; color: #3949ab; margin-bottom: 4px; }
.preview-avail  { font-size: 13px; color: #555; }
.text-green     { color: #2e7d32; font-weight: 700; }

// Form
.form-group {
  display: flex; flex-direction: column; gap: 6px;

  label {
    font-size: 13px; font-weight: 600; color: #444;
    display: flex; align-items: center; gap: 6px;
  }

  input[type="date"], textarea {
    padding: 9px 12px; border: 1.5px solid #e0e0e0;
    border-radius: 8px; font-size: 14px; outline: none;
    font-family: inherit; transition: border-color 0.15s;
    &:focus { border-color: #3949ab; }
    &.input-error { border-color: #e53935; }
  }

  textarea { resize: vertical; background: transparent; color: #333333; }
}

.optional    { font-size: 11px; color: #aaa; font-weight: 400; }
.field-hint  { font-size: 12px; color: #888; }
.field-error { font-size: 12px; color: #e53935; font-weight: 500; }

.submit-error {
  padding: 10px 14px; background: #ffebee;
  border-radius: 8px; font-size: 13px; color: #c62828;
}

// Buttons
.btn {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 9px 20px; border-radius: 8px; font-size: 14px;
  font-weight: 600; cursor: pointer; border: none; transition: all 0.15s;

  &:disabled { opacity: 0.5; cursor: not-allowed; }
  &.btn-primary {
    background: #3949ab; color: #fff;
    &:hover:not(:disabled) { background: #2c3a8c; }
  }
  &.btn-outline {
    background: #fff; color: #3949ab; border: 1.5px solid #3949ab;
    &:hover:not(:disabled) { background: #e8eaf6; }
  }
}
</style>