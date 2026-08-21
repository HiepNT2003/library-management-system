<template>
  <div class="multi-select" :class="{ 'is-open': isOpen, 'has-error': errorMessage }" ref="dropdownRef">
    <!-- Trigger -->
    <div class="ms-trigger" @click="toggleDropdown">
      <div class="ms-selected-tags">
        <template v-if="selectedOptions.length > 0">
          <span
            v-for="opt in selectedOptions"
            :key="opt.value"
            class="ms-tag"
          >
            {{ opt.name }}
            <button class="ms-tag-remove" @click.stop="removeOption(opt)">×</button>
          </span>
        </template>
        <span v-else class="ms-placeholder">{{ placeholder }}</span>
      </div>
      <div class="ms-trigger-right">
        <span v-if="selectedOptions.length > 0" class="ms-count">{{ selectedOptions.length }}</span>
        <svg class="ms-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <polyline points="6 9 12 15 18 9" />
        </svg>
      </div>
    </div>

    <!-- Error message -->
    <transition name="error-slide">
      <div v-if="errorMessage" class="ms-error">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
        </svg>
        {{ errorMessage }}
      </div>
    </transition>

    <!-- Dropdown panel -->
    <transition name="dropdown-slide">
      <div v-if="isOpen" class="ms-panel">
        <!-- Search input -->
        <div class="ms-search-wrap">
          <svg class="ms-search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
          </svg>
          <input
            ref="searchInputRef"
            v-model="searchQuery"
            class="ms-search"
            type="text"
            placeholder="Tìm kiếm..."
            @click.stop
          />
          <button v-if="searchQuery" class="ms-search-clear" @click.stop="searchQuery = ''">×</button>
        </div>

        <!-- Option list -->
        <div class="ms-options-wrap">
          <div v-if="filteredOptions.length === 0" class="ms-empty">
            Không tìm thấy kết quả
          </div>
          <label
            v-for="opt in filteredOptions"
            :key="opt.value"
            class="ms-option"
            :class="{ 'is-selected': isSelected(opt), 'is-disabled': isAtMax && !isSelected(opt) }"
          >
            <input
              type="checkbox"
              class="ms-checkbox"
              :checked="isSelected(opt)"
              :disabled="isAtMax && !isSelected(opt)"
              @change="toggleOption(opt)"
              @click.stop
            />
            <span class="ms-checkbox-custom"></span>
            <span class="ms-option-label">{{ opt.name }}</span>
          </label>
        </div>

        <!-- Footer: counter + add button -->
        <div class="ms-footer">
          <span class="ms-counter">
            <span :class="{ 'over-limit': errorMessage }">{{ selectedOptions.length }}</span>
            <template v-if="min !== undefined || max !== undefined">
              /
              <template v-if="min !== undefined && max !== undefined">{{ min }}–{{ max }}</template>
              <template v-else-if="min !== undefined">tối thiểu {{ min }}</template>
              <template v-else-if="max !== undefined">tối đa {{ max }}</template>
            </template>
          </span>
          <button class="ms-add-btn" @click.stop="$emit('add-new')">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
            </svg>
            Thêm mới
          </button>
        </div>
      </div>
    </transition>
  </div>
</template>

<script>
export default {
  name: 'MultiSelect',

  props: {
    modelValue: {
      type: Array,
      default: () => []
    },
    options: {
      type: Array,
      default: () => []
      // Expected: [{ name: string, value: any }]
    },
    placeholder: {
      type: String,
      default: 'Chọn các mục...'
    },
    min: {
      type: Number,
      default: undefined
    },
    max: {
      type: Number,
      default: undefined
    },
    minErrorMessage: {
      type: String,
      default: undefined
    },
    maxErrorMessage: {
      type: String,
      default: undefined
    },
    errorMessage: {
      type: String,
      default: undefined
    }
  },

  emits: ['update:modelValue', 'add-new'],

  data() {
    return {
      isOpen: false,
      searchQuery: ''
    }
  },

  computed: {
    selectedOptions() {
      return this.options.filter(opt => this.modelValue.includes(opt.value))
    },

    filteredOptions() {
      const q = this.searchQuery.toLowerCase().trim()
      if (!q) return this.options
      return this.options.filter(opt => opt.name.toLowerCase().includes(q))
    },

    isAtMax() {
      return this.max !== undefined && this.selectedOptions.length >= this.max
    },

    errorMessage() {
      const count = this.selectedOptions.length
      if (this.max !== undefined && count > this.max) {
        return this.maxErrorMessage || `Chỉ được chọn tối đa ${this.max} mục`
      }
      if (this.min !== undefined && count > 0 && count < this.min) {
        return this.minErrorMessage || `Vui lòng chọn ít nhất ${this.min} mục`
      }
      return this.errorMessage ? this.errorMessage : null
    }
  },

  methods: {
    toggleDropdown() {
      this.isOpen = !this.isOpen
      if (this.isOpen) {
        this.$nextTick(() => this.$refs.searchInputRef?.focus())
      }
    },

    isSelected(opt) {
      return this.modelValue.includes(opt.value)
    },

    toggleOption(opt) {
      const current = [...this.modelValue]
      const idx = current.indexOf(opt.value)
      if (idx === -1) {
        // Adding — allow even if at max (error will show), or block if you prefer
        current.push(opt.value)
      } else {
        current.splice(idx, 1)
      }
      this.$emit('update:modelValue', current)
    },

    removeOption(opt) {
      const current = this.modelValue.filter(v => v !== opt.value)
      this.$emit('update:modelValue', current)
    },

    handleOutsideClick(e) {
      if (this.$refs.dropdownRef && !this.$refs.dropdownRef.contains(e.target)) {
        this.isOpen = false
      }
    }
  },

  mounted() {
    document.addEventListener('mousedown', this.handleOutsideClick)
  },

  beforeUnmount() {
    document.removeEventListener('mousedown', this.handleOutsideClick)
  }
}
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Be+Vietnam+Pro:wght@400;500;600&display=swap');

*, *::before, *::after { box-sizing: border-box; }

.multi-select {
  position: relative;
  width: 100%;
  font-family: 'Be Vietnam Pro', sans-serif;
  font-size: 14px;
  color: #1a1a2e;
}

/* ── Trigger ─────────────────────────────────────────── */
.ms-trigger {
  display: flex;
  align-items: center;
  gap: 8px;
  min-height: 44px;
  padding: 6px 10px 6px 8px;
  background: #fff;
  border: 1.5px solid #dde1ea;
  border-radius: 4px;
  cursor: pointer;
  transition: border-color 0.2s, box-shadow 0.2s;
  user-select: none;
}
.ms-trigger:hover,
.is-open .ms-trigger {
  border-color: #6366f1;
  box-shadow: 0 0 0 3px rgba(99,102,241,0.12);
}
.has-error .ms-trigger {
  border-color: #ef4444;
  box-shadow: 0 0 0 3px rgba(239,68,68,0.1);
}

.ms-selected-tags {
  flex: 1;
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  min-height: 28px;
  align-items: center;
}
.ms-placeholder {
  color: #94a3b8;
}

.ms-tag {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 2px 8px;
  background: #eef2ff;
  color: #4f46e5;
  border-radius: 6px;
  font-size: 12.5px;
  font-weight: 500;
}
.ms-tag-remove {
  background: none;
  border: none;
  padding: 0;
  line-height: 1;
  font-size: 15px;
  color: #818cf8;
  cursor: pointer;
  transition: color 0.15s;
}
.ms-tag-remove:hover { color: #4f46e5; }

.ms-trigger-right {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
}
.ms-count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 20px;
  height: 20px;
  padding: 0 5px;
  background: #6366f1;
  color: #fff;
  border-radius: 99px;
  font-size: 11px;
  font-weight: 600;
}
.ms-arrow {
  width: 18px;
  height: 18px;
  color: #94a3b8;
  transition: transform 0.25s ease;
}
.is-open .ms-arrow { transform: rotate(180deg); }

/* ── Error ───────────────────────────────────────────── */
.ms-error {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-top: 6px;
  padding: 7px 12px;
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 8px;
  color: #dc2626;
  font-size: 12.5px;
  font-weight: 500;
}
.ms-error svg { width: 14px; height: 14px; flex-shrink: 0; }

.error-slide-enter-active,
.error-slide-leave-active { transition: all 0.2s ease; }
.error-slide-enter-from,
.error-slide-leave-to { opacity: 0; transform: translateY(-4px); }

/* ── Panel ───────────────────────────────────────────── */
.ms-panel {
  position: absolute;
  top: calc(100% + 6px);
  left: 0;
  right: 0;
  background: #fff;
  border: 1.5px solid #dde1ea;
  border-radius: 12px;
  box-shadow: 0 8px 30px rgba(0,0,0,0.1);
  z-index: 1000;
  overflow: hidden;
}

.dropdown-slide-enter-active,
.dropdown-slide-leave-active { transition: all 0.2s cubic-bezier(0.16,1,0.3,1); }
.dropdown-slide-enter-from,
.dropdown-slide-leave-to { opacity: 0; transform: translateY(-8px) scale(0.98); }

/* ── Search ──────────────────────────────────────────── */
.ms-search-wrap {
  position: relative;
  padding: 10px 12px;
  border-bottom: 1px solid #f1f5f9;
}
.ms-search-icon {
  position: absolute;
  left: 22px;
  top: 50%;
  transform: translateY(-50%);
  width: 15px;
  height: 15px;
  color: #94a3b8;
  pointer-events: none;
}
.ms-search {
  width: 100%;
  padding: 8px 32px 8px 36px;
  border: 1.5px solid #e2e8f0;
  border-radius: 8px;
  background: #f8fafc;
  font-family: inherit;
  font-size: 13px;
  color: #1a1a2e;
  outline: none;
  transition: border-color 0.15s, background 0.15s;
}
.ms-search:focus {
  border-color: #6366f1;
  background: #fff;
}
.ms-search-clear {
  position: absolute;
  right: 22px;
  top: 50%;
  transform: translateY(-50%);
  background: none;
  border: none;
  font-size: 17px;
  line-height: 1;
  color: #94a3b8;
  cursor: pointer;
  padding: 0;
  transition: color 0.15s;
}
.ms-search-clear:hover { color: #475569; }

/* ── Options ─────────────────────────────────────────── */
.ms-options-wrap {
  max-height: 220px;
  overflow-y: auto;
  padding: 6px 0;
}
.ms-options-wrap::-webkit-scrollbar { width: 4px; }
.ms-options-wrap::-webkit-scrollbar-track { background: transparent; }
.ms-options-wrap::-webkit-scrollbar-thumb { background: #cbd5e1; border-radius: 4px; }

.ms-empty {
  padding: 20px;
  text-align: center;
  color: #94a3b8;
  font-size: 13px;
}

.ms-option {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 9px 14px;
  cursor: pointer;
  transition: background 0.12s;
  user-select: none;
}
.ms-option:hover:not(.is-disabled) { background: #f5f3ff; }
.ms-option.is-selected { background: #f5f3ff; }
.ms-option.is-disabled { opacity: 0.45; cursor: not-allowed; }

/* hide native checkbox */
.ms-checkbox { display: none; }

.ms-checkbox-custom {
  width: 18px;
  height: 18px;
  border: 1.5px solid #d1d5db;
  border-radius: 5px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: border-color 0.15s, background 0.15s;
  position: relative;
}
.ms-option.is-selected .ms-checkbox-custom {
  background: #6366f1;
  border-color: #6366f1;
}
.ms-option.is-selected .ms-checkbox-custom::after {
  content: '';
  width: 10px;
  height: 7px;
  border-left: 2px solid #fff;
  border-bottom: 2px solid #fff;
  transform: rotate(-45deg) translate(1px, -1px);
}

.ms-option-label {
  font-size: 13.5px;
  color: #334155;
  flex: 1;
}
.ms-option.is-selected .ms-option-label {
  color: #4f46e5;
  font-weight: 500;
}

/* ── Footer ──────────────────────────────────────────── */
.ms-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 14px 10px;
  border-top: 1px solid #f1f5f9;
  background: #fafafa;
}

.ms-counter {
  font-size: 12px;
  color: #94a3b8;
}
.ms-counter .over-limit { color: #ef4444; font-weight: 600; }

.ms-add-btn {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 6px 14px;
  background: #6366f1;
  color: #fff;
  border: none;
  border-radius: 8px;
  font-family: inherit;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.15s, transform 0.1s;
}
.ms-add-btn:hover { background: #4f46e5; }
.ms-add-btn:active { transform: scale(0.97); }
.ms-add-btn svg {
  width: 14px;
  height: 14px;
}
</style>