<template>
    <div class="borrow_wrap">
        <TabSelect :selected="selectedTab" :options="options" @changeTab="handleChangeTab" />
    </div>
</template>
<script setup>
import { ref } from 'vue';
import TabSelect from '../../components/share/TabSelect.vue';

const options = ref([{ id: 1, title: "Theo phiếu mượn" }, { id: 2, title: "Theo đầu sách" }]);
const selectedTab = ref(options.value[0]);

function handleChangeTab(tab) {
    selectedTab.value = tab
}

</script>
<style lang="scss" scoped></style>