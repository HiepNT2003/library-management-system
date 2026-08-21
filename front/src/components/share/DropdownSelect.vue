<script setup>
import { ref, watch } from "vue"
import Select from "primevue/select"

const props = defineProps({
  defaultSelected: {},
  placeholder: "",
  listOptions: [],
})

const emit = defineEmits(["update:selected"])

const selectedItem = ref({})
function onChange(event) {
  emit("update:selected", event.value)
}
watch(
  () => props.defaultSelected,
  (value) => {
    selectedItem.value = value
  },
  { immediate: true }
)
</script>

<template>
  <Select
    v-model="selectedItem"
    :options="listOptions"
    optionLabel="name"
    :placeholder="placeholder"
    @change="onChange($event)"
    class="w-full md:w-56"
  />
</template>
<style lang="scss">
.p-select-overlay {
  border: 1px solid #e2e8f0 !important;
  border-radius: 4px !important;
  overflow: hidden;
  margin-top: 2px;
}

.p-select {
  border: 1px solid #dce7f1 !important;
  padding: 8px 10px;
  border-radius: 4px !important;
  gap: 8px;
}

.p-select-list {
  padding: 4px;
}

.p-select-option {
  padding: 7px 11px !important;

  &:hover:not(.p-select-option-selected) {
    background: #f1f5f9 !important;
    border-radius: 4px !important;
  }
}

.p-select-option-selected {
  background: #000 !important;
  border-radius: 4px !important;
  color: #ffffff !important;

  &:hover {
    background: #334155 !important;
    color: #ffffff !important;

    .p-select-option-label {
      color: #ffffff !important;
    }
  }
}

.p-select-list-container {
  background: #ffffff;
}
</style>