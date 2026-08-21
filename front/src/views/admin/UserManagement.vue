<template>
    <div class="user_wrap">
        <TabSelect :selected="selectedTab" :options="!isAdmin ? options : [...options, optionLibrarian]" @changeTab="handleChangeTab" />
        <!-- Trong trang có 3 tab -->
        <UserTab v-if="selectedTab.value === 'student'"   role="Student"   />
        <UserTab v-if="selectedTab.value === 'staff'"     role="Staff"     />
        <UserTab v-if="selectedTab.value === 'librarian'" role="Librarian" />
    </div>
</template>
<script setup>
import { computed, ref } from 'vue';
import TabSelect from '../../components/share/TabSelect.vue';
import StudentManagement from '../../components/Admin/StudentManagement.vue';
import { useAuthStore } from '../../stores/auth';
import UserTab from '../../components/Admin/Users/UserTab.vue'

const authStore = useAuthStore()
const options = ref([{ id: 1, title: "Học sinh", value: 'student' }, { id: 2, title: "Giảng viên / cán bộ", value: 'staff' }]);
const optionLibrarian = {id: 3, title: "Thủ thư", value: 'librarian' }
const selectedTab = ref(options.value[0]);

const isAdmin = computed(() => authStore.isAdmin)


function handleChangeTab(tab) {
    selectedTab.value = tab
}

</script>
<style lang="scss" scoped></style>