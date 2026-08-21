<template>
    <div class="tab_wrap">
        <div class="tab" v-for="tab in options" :key="tab.id" @click="onChangeTab(tab)"
            :class="{ active: tab.id == selected.id }">
            <p class="title">{{ tab.title }}</p>
        </div>
    </div>
</template>
<script>
export default {
    props: {
        selected: {
            type: Object,
            default: () => ({})
        },
        options: {
            type: Array,
            default: () => ([])
        }
    },
    data() {
        return {
        }
    },
    methods: {
        onChangeTab(tab) {
            this.$emit('changeTab', tab)
        }
    },
}
</script>
<style lang="scss" scoped>
.tab_wrap {
    display: flex;
    width: fit-content;
    border: 1px solid #dcdcdc;
    border-radius: 8px;
    overflow: hidden;

    .tab {
        padding: 4px 12px;
        cursor: pointer;
        background: #ffffff;

        &:not(:last-child) {
            border-right: 1px solid #dcdcdc;
        }

        &.active {
            background-color: #435ebe;
            color: #ffffff
        }

        .title {
            margin-bottom: 0;

        }
    }
}
</style>