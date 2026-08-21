<template>
    <div class="dataTable-bottom">
        <div class="dataTable-info">Hiển thị từ {{ (page - 1) * itemPerPage + 1 }} đến {{
            page == totalPages ? totalRecord : page * itemPerPage }} trong tổng
            số {{ totalRecord }} sách</div>
        <ul class="pagination pagination-primary float-end dataTable-pagination">
            <li class="page-item pager" v-if="totalPages > 1" @click="prevPage">
                <p class="page-link">‹</p>
            </li>
            <li class="page-item" :class="{ active: page == pageShow }" @click="changePage(pageShow)"
                v-for="pageShow in listShowPage">
                <p class="page-link">{{ pageShow }}</p>
            </li>
            <li class="page-item pager" v-if="totalPages > 1" @click="nextPage">
                <p class="page-link">›</p>
            </li>
        </ul>
    </div>
</template>
<script>
export default {
    props: {
        page: {
            type: [Number, String],
            default: 1
        },
        itemPerPage: {
            type: [Number, String],
            default: 10
        },
        totalRecord: {
            type: [Number, String],
            default: 10
        },
        totalPages: {
            type: [Number, String],
            default: 1
        },
    },
    computed: {
        listShowPage() {
            return this.totalPages <= 1 ? [1] : this.totalPages == 2 ? [1, 2] : [this.page == 1 ? this.page : this.page == this.totalPages ? this.page - 2 : this.page - 1, this.page == 1 ? 2 : this.page == this.totalPages ? this.page - 1 : this.page, this.page == 1 ? this.page + 2 : this.page == this.totalPages ? this.page : this.page + 1]
        }
    },
    methods: {
        prevPage() {
            if (this.page <= 1) return
            this.$emit('changePage', this.page - 1)
        },
        nextPage() {
            if (this.page >= this.totalPages) return
            this.$emit('changePage', this.page + 1)
        },
        changePage(page) {
            this.$emit('changePage', page)
        },
    }
}
</script>
<style lang="scss" scoped>
.page-item {
    cursor: pointer;
}

.page-link {
    border: none !important;
}
</style>