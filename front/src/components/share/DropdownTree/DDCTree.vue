<template>
  <div class="ddc-container">
    <div v-if="ddc.children.length" @click.stop="toggleDropdown" />
    <div class="ddc-wrapper">
      <div
        v-if="ddc.children.length"
        class="accordion-mark"
        :class="{ 'accordion-mark-close': !showChildren }"
        @click.stop="toggleChild()"
      ></div>
      <Checkbox v-model="isChecked" @change="selectDDC(ddc)" binary />
      <div class="ddc-name">
        {{ ddc.name }}
      </div>
      <div class="ddc-code">
        ({{ ddc.code }})
      </div>
    </div>
    <div v-if="showChildren" :style="childMargin">
      <DDCTree
        v-for="child in ddc.children"
        :key="child.code"
        :ddc="child"
        :margin="margin"
        :selectedDDC="selectedDDC"
        @selectDDC="selectDDC"
      >
      </DDCTree>
    </div>
  </div>
</template>
<script>
import Checkbox from "primevue/checkbox"
export default {
  name: "DDCTree",
  components: {
    Checkbox,
  },
  props: {
    ddc: {
      type: Object,
      required: true,
    },
    margin: {
      type: Number,
      default: 25,
    },
    selectedDDC: {
      type: Array,
      default: () => {},
    },
  },
  created() {
    if (this.ddc.children.length) {
      this.showChildren = this.checkHasSelectedChild(this.ddc.children)
    }
  },
  data() {
    return {
      showChildren: false,
      isChecked: false,
    }
  },
  computed: {
    existChildren() {
      return this.ddc.children
    },
    childMargin() {
      return `margin-left: ${this.margin}px`
    },
    selectedItem() {
      return this.selectedDDC?.code == this.ddc.code
    },
  },
  methods: {
    toggleDropdown() {
      this.showChildren = !this.showChildren
    },
    selectDDC(ddc) {
      if (ddc?.code == this.selectedDDC?.code) this.$emit("selectDDC", null)
      else this.$emit("selectDDC", ddc)
    },
    toggleChild() {
      this.showChildren = !this.showChildren
    },
    checkHasSelectedChild(children) {
      return children.some((child) => {
        if (child.code == this.selectedDDC?.code) {
          return true
        } else {
          if (child.children.length) {
            return this.checkHasSelectedChild(child.children)
          } else {
            return false
          }
        }
      })
    },
  },
  watch: {
    selectedItem: {
      handler(val) {
        this.isChecked = val ? true : false
      },
      deep: true,
      immediate: true,
    },
  },
}
</script>
<style lang="scss" scoped>
.original-parent-unit {
  &::before,
  &::after {
    display: none;
  }
}

.accordion-mark {
  float: left;
  height: 16px;
  width: 20px;
  margin-left: -30px;
  background: url("../../../assets/Images/d.png") no-repeat scroll 0 0 transparent;
  cursor: pointer;
  opacity: 1;
  margin-top: -5px;
  background-position: -13px 0;
  position: relative;
  z-index: 1;

  &-close {
    float: left;
    height: 16px;
    width: 20px;
    margin-left: -30px;
    background: url("../../../assets/Images/d.png") no-repeat scroll 0 0 transparent;
    cursor: pointer;
    opacity: 1;
    margin-top: 7px;
    background-position: -12px 0;
    position: relative;
    z-index: 1;
    transform: rotate(-45deg);
  }
}
.ddc-container {
  margin-top: 16px;
  position: relative;

  &::before {
    border-left: 1px dashed #dcdcdc;
    height: calc(100% + 18px);
    width: 1px;
    content: "";
    top: -21px;
    bottom: 0;
    left: -17px;
    position: absolute;
  }
  &:last-of-type {
    &::before {
      height: 31px;
    }
  }
  &::after {
    position: absolute;
    top: 12px;
    left: -16px;
    content: "";
    width: 16px;
    height: 1px;
    border-top: 1px dashed #dcdcdc;
  }

  .ddc-wrapper {
    display: flex;
    align-items: center;
    gap: 8px;
    width: fit-content;
    cursor: pointer;

    .ddc-name {
      color: #333;
      line-height: 22px;

      &-selected {
        color: #007bc3;
      }
    }
    .ddc-code {
      font-size: 14px;
    }

    .ddc-level {
      color: #fff;
      font-size: 12px;
      line-height: 20px;
      display: flex;
      justify-content: center;
      align-items: center;
      background: #48647f;
      border-radius: 3px;
      min-width: 18px;
      min-height: 18px;
    }
  }
}
.checkbox:focus {
  outline: none !important;
  -webkit-box-shadow: none !important;
  box-shadow: none !important;
}
</style>
