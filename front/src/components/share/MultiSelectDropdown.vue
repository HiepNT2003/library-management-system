<template>
  <div class="card flex justify-center">
    <MultiSelect
      v-model="selectedOption"
      :options="listOptions"
      optionLabel="name"
      filter
      :placeholder="placeholder"
      display="chip"
      class="w-full md:w-80"
      :maxSelectedLabels="3"
      :class="{ input_error: errorMessage }"
      :showClear="showClear"
    >
      <template #option="slotProps">
        <div class="flex items-center">
          <input
            type="text"
            name=""
            id=""
            v-if="slotProps.option.type && slotProps.option.type == 'input'"
          />
          <div v-else>{{ slotProps.option.name }}</div>
        </div>
      </template>
      <template #dropdownicon>
        <Icon icon="ep:arrow-down" width="16" height="16" />
      </template>
      <template #filtericon>
        <Icon icon="ion:search-outline" width="20" height="20" />
      </template>
      <template #header>
        <div class="font-medium px-3 py-2">{{ dropdownTitle }}</div>
      </template>
      <template #footer v-if="showAddButton">
        <div class="p-3 flex justify-between">
          <Button
            label="Thêm mới"
            severity="secondary"
            variant="text"
            size="small"
            icon="pi pi-plus"
            @click="addItem"
          />
          <!-- <Button
            label="Xóa tất cả"
            severity="danger"
            variant="text"
            size="small"
            icon="pi pi-times"
            @click="removeAll"
          /> -->
        </div>
      </template>
    </MultiSelect>
    <p class="error">{{ errorMessage }}</p>
  </div>
</template>

<script setup>
import { watch, ref, onMounted } from "vue"
import MultiSelect from "primevue/multiselect"
import Button from "primevue/button"
import { Icon } from "@iconify/vue"

const props = defineProps({
  placeholder: String,
  listOptions: Array,
  dropdownTitle: String,
  errorMessage: String,
  showClear: Boolean,
  showAddButton: Boolean,
  selectedValue: Array,
  modelValue: Array,
})

const emit = defineEmits(["update:modelValue", "on:addItem"])

const selectedOption = ref([])

watch(selectedOption, () => {
  emit("update:modelValue", selectedOption.value)
})
watch(
  () => props.selectedValue,
  () => {
    selectedOption.value = props.selectedValue
  },
  { deep: true }
)
const addItem = () => {
  emit("on:addItem")
}
</script>
<style lang="scss" scoped>
.card {
  border: none;
  border-radius: 0.25rem;
  margin-bottom: 0;
}
.input-search {
  background: #ffffff;
}
.input_error {
  border: 1px solid rgb(252, 91, 91) !important;
}
.error {
  margin-bottom: 0;
  margin-top: 4px;
  margin-left: 4px;
  font-size: 12px;
  color: red;
}
</style>
<style lang="scss">
.p-multiselect-overlay {
  background: #ffffff !important;
  color: #333333 !important;
  border: 1px solid #dce7f1 !important;
  .p-inputtext {
    background: transparent;
    border: 1px solid #dce7f1;
    color: #333333;
    padding: 8px 12px;
    &:hover {
      border-color: #dce7f1 !important;
    }
    &:focus {
      border-color: #dce7f1 !important;
      box-shadow: none !important;
      outline: none !important;
    }
  }
  .p-inputicon {
    top: 40%;
  }
}
.p-multiselect {
  background: transparent !important;
  border: 1px solid #dce7f1 !important;
  border-radius: 0.25rem !important;
  .p-multiselect-label {
    padding: 8px 12px;
    .p-chip {
      background: #435ebe;
      .p-chip-label {
        max-width: 160px;
        overflow: hidden;
        text-overflow: ellipsis;
      }
    }
  }
  &:hover {
    border-color: #dce7f1 !important;
  }
  &.p-focus {
    box-shadow: none !important;
  }
}
.p-multiselect-list-container {
  .p-multiselect-option {
    color: #333333;
  }
  &::-webkit-scrollbar-track:vertical {
    background-color: transparent;
    position: relative;
  }
  &::-webkit-scrollbar {
    width: 8px;
    height: 8px;
    position: relative;
  }
  &::-webkit-scrollbar-thumb {
    border-radius: 4px;
    background-color: #8f8e8e;
  }
}
.p-button-secondary {
  color: #435ebe !important;
  &:hover {
    background: #435ebe !important;
    color: #ffffff !important;
  }
  &:focus {
    outline: none;
  }
}
.p-multiselect-label {
  color: #607080 !important;
}
</style> 
