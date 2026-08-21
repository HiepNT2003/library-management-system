<template>
  <div class="input-tree-dropdown">
    <div class="container" v-click-outside="hideTree">
      <div class="input-wrapper" @click="showTree">
        <Icon
          class="search-icon"
          icon="ion:search-outline"
          v-if="!Object.keys(valueLocal).length"
          width="20"
          height="20"
        />
        <div class="item-selected">
          <div
            class="chip"
            :class="{ 'chip-disabled': valueLocal.enable === false }"
            v-if="valueLocal.code"
          >
            <div class="wrap-text">
              <div class="text">{{ getDDCName(valueLocal) }}</div>
              <span class="tooltip">{{ getDDCName(valueLocal) }}</span>
            </div>
            <div class="close-icon" @click.stop="removeSelectedDDC(valueLocal)">
              <Icon icon="mdi:remove" width="16" height="16" />
            </div>
          </div>
        </div>
        <input
          type="text"
          :placeholder="placeholder"
          v-model="keyword"
          class="input-dropdown"
          :class="{ 'input-dropdown-readonly': !useInput, 'pl-10': Object.keys(valueLocal).length }"
          :readonly="!useInput"
          @input="onInput"
        />
      </div>
      <div class="btn-clear icf-multiply" v-if="isShowClearBtn" @click="unselectAllDDC" />
      <div @click="toggleTree" class="icon-dropdown">
        <Icon icon="ep:arrow-down" width="16" height="16" />
      </div>
      <div class="ddc-search-list" v-show="isDropdownOpen" v-click-outside="hideDropdown">
        <div class="ddc-item" v-for="ddc in ddcs" :key="ddc.code" @click.stop="onChooseDDC(ddc)">
          {{ ddc.name }}
        </div>
      </div>
      <div class="dropdown-container" v-show="isTreeOpen">
        <div class="tree-container">
          <DDCTree
            v-for="ddc in ddcTree"
            :key="ddc.code"
            :ddc="ddc"
            @selectDDC="selectDDC"
            :selectedDDC="valueLocal"
          />
        </div>
      </div>
    </div>
  </div>
</template>
<script>
import DDCTree from "./DDCTree.vue"
import { debounce } from "lodash"
import { Icon } from "@iconify/vue"

export default {
  name: "InputTreeDropdown",
  components: {
    DDCTree,
    Icon,
  },
  props: {
    placeholder: {
      type: String,
      default: "",
    },
    useInput: {
      type: Boolean,
      default: true,
    },
    value: {
      type: Array,
      default: () => [],
    },
    ddcTree: {
      type: Array,
      default: () => [],
    },
  },
  data() {
    return {
      keyword: "",
      valueLocal: {},
      isTreeOpen: false,
      isDropdownOpen: false,
      ddcs: [],
    }
  },
  async created() {
    this.valueLocal = this.value
  },
  computed: {
    isShowClearBtn() {
      return this.valueLocal?.code
    },
  },
  methods: {
    removeSelectedDDC({ code }) {
      this.valueLocal = {}
      this.$emit("update:ddcVal", this.valueLocal)
    },
    unselectAllDDC() {
      this.valueLocal = {}
      this.$emit("update:ddcVal", this.valueLocal)
    },
    showTree() {
      this.isTreeOpen = true
    },
    hideTree() {
      this.isTreeOpen = false
    },
    toggleTree() {
      this.isTreeOpen = !this.isTreeOpen
    },
    showDropdown() {
      this.isDropdownOpen = true
    },
    hideDropdown() {
      this.isDropdownOpen = false
    },
    getDDCChildren(ddcs) {
      let result = []
      if (ddcs.length) {
        result = ddcs
        ddcs.forEach((c) => {
          if (c.children && c.children.length) {
            result = result.concat(this.getDDCChildren(c.children))
          }
        })
      }
      return result
    },
    isActiveDDC(ddc) {
      return this.valueLocal?.code == ddc.code
    },
    onChooseDDC(ddc) {
      if (this.isActiveDDC(ddc)) {
        this.keyword = ""
        this.hideDropdown()
        return
      }
      this.valueLocal = ddc
      this.keyword = ""
      this.$emit("update:ddcVal", this.valueLocal)
      this.hideDropdown()
    },
    selectDDC(ddc) {
      if (!ddc) this.valueLocal = {}
      else this.valueLocal = ddc
      this.keyword = ""
      this.$emit("update:ddcVal", this.valueLocal)
    },
    findTreeNodeFromId(currentTree, code) {
      for (let currentNode of currentTree) {
        if (currentNode.code == code) {
          return currentNode
        }
        if (Array.isArray(currentNode.children)) {
          const treeNode = this.findTreeNodeFromId(currentNode.children, code)
          if (treeNode) {
            return treeNode
          }
        }
      }
      return null
    },
    getDDCName(item) {
      if (item.name) {
        return item.name
      }
      const treeNode = this.findTreeNodeFromId(this.ddcTree, item.code)
      if (treeNode) {
        return treeNode.name
      }
      return ""
    },
    onInput() {
      debounce(this.getDepartmentList, 300)()
    },
    async getDepartmentList() {
      if (!this.keyword) {
        this.hideDropdown()
        this.ddcs = this.ddcTree
        return
      }
      const result = await this.searchFlat(this.flattenTree(this.ddcTree), this.keyword)
      this.ddcs = result ?? []
      this.hideTree()
      this.showDropdown()
    },
    searchFlat(list, keyword) {
      const lowerKeyword = keyword.toLowerCase()

      return list.filter(
        (item) => item.name.toLowerCase().includes(lowerKeyword) || item.code.includes(keyword)
      )
    },
    flattenTree(tree) {
      let result = []

      function traverse(nodes) {
        for (const node of nodes) {
          result.push(node)
          if (node.children) traverse(node.children)
        }
      }

      traverse(tree)
      return result
    },
  },
  watch: {
    value: {
      handler(val) {
        this.valueLocal = val
      },
      deep: true,
    },
  },
}
</script>
<style lang="scss" scoped>
@use "@/assets/scss/variables.scss" as V;

.input-tree-dropdown {
  display: flex;
  flex-direction: column;
  width: 100%;
  gap: 4px;
  .input-tree-title {
    color: #666;
    font-size: 12px;
    font-style: normal;
    font-weight: 400;
    line-height: 20px;
  }
  .container {
    width: 100%;
    display: flex;
    border: 1px solid #dbdbdb;
    align-items: center;
    border-radius: 4px;
    padding: 5px 6px;
    min-height: 40px;
    position: relative;
    cursor: pointer;
    input:focus-visible {
      outline: none;
    }

    .search-icon {
      margin-right: 10px;
      pointer-events: none;
    }

    .input-wrapper {
      display: flex;
      align-items: center;
      flex-wrap: wrap;
      flex: 1;

      .item-selected {
        display: flex;
        flex-wrap: wrap;
        gap: 4px;
        max-height: 120px;
        overflow: auto;

        .chip {
          display: flex;
          justify-content: center;
          align-items: center;
          gap: 8px;
          min-height: 32px;
          background: #435ebe;
          border: 1px solid #dcdcdc;
          border-radius: 4px;
          padding: 4px 8px;
          color: #ffffff;
          line-height: 20px;

          .wrap-text {
            &:hover {
              .tooltip {
                visibility: visible;
              }
            }
            .tooltip {
              visibility: hidden;
              min-width: 120px;
              max-width: 200px;
              background-color: #333333;
              color: #fff;
              text-align: center;
              border-radius: 6px;
              padding: 5px 0;
              position: absolute;
              z-index: 2;
            }
          }

          .text {
            white-space: nowrap;
            max-width: 300px;
            overflow: hidden;
            text-overflow: ellipsis;
          }

          &-disabled {
            color: #bfbfbf;
            background: #f7f7f7;
          }

          .close-icon {
            cursor: pointer;
            display: inline-flex;
            align-items: center;
            justify-content: center;
          }

          .icf-multiply::before {
            font-size: 10px;
            font-weight: 700;
          }
        }
      }

      .input-dropdown {
        border: none;
        flex: 1;
        padding-left: 0;
        width: fit-content;

        &-readonly {
          cursor: pointer;
        }
      }
    }

    .btn-clear {
      margin-right: 10px;
    }

    .dropdown-container {
      @include V.custom-scroll-bar;
      min-height: 225px;
      padding: 10px;
      position: absolute;
      top: 106%;
      left: 0;
      background: #fff;
      width: inherit;
      z-index: 1;
      border: 1px solid #dcdcdc;
      border-radius: 4px;
      box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
      cursor: initial;
      max-height: 320px;
      overflow: auto;

      .tree-container {
        padding-left: 20px;
      }
    }

    .icon-dropdown {
      margin-right: 3px;
    }
  }
}

.pl-10 {
  padding-left: 10px !important;
}

.ddc-search-list {
  position: absolute;
  background: #f0f4f8;
  border: 1px solid #dcdcdc;
  border-radius: 4px;
  width: calc(100% + 2px);
  top: 100%;
  left: -1px;
  overflow-y: auto;
  z-index: 2;
  max-height: 367px;
  .ddc-item {
    cursor: pointer;
    padding: 8px 10px;
    color: #486581;
    font-size: 16px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    &:hover {
      background: #627d98;
      color: #ffffff;
    }
  }
}
</style>
