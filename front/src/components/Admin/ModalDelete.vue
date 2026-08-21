<template>
    <div>
        <div class="modal text-left" id="danger" tabindex="-1" role="dialog"
            aria-labelledby="myModalLabel120" aria-hidden="true">
            <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable" role="document">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="myModalLabel120">
                            {{ title }}
                        </h5>
                    </div>
                    <div class="modal-body">
                        <p class="delete_msg">Xóa <span class="selected">{{ description }}</span> khỏi thư viện ?</p>
                        <span class="desc">Hành động này không thể hoàn tác.</span>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-light-secondary" data-bs-dismiss="modal">
                            <i class="bx bx-x d-block d-sm-none"></i>
                            <span class="d-none d-sm-block" @click="closeModal">Đóng</span>
                        </button>
                        <button type="button" class="btn btn-danger ml-1" data-bs-dismiss="modal">
                            <i class="bx bx-check d-block d-sm-none"></i>
                            <span class="d-none d-sm-block" @click="confirmDeleteBook">Xác nhận</span>
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div class="modal-backdrop"></div>
    </div>
</template>
<script>
export default {
    props: {
        title: {
            type: String,
            default: ''
        },
        description: {
            type: String,
            default: ''
        }
    },
    methods: {
        closeModal() {
            this.$emit('closeModal')
        },
        confirmDeleteBook() {
            this.$emit('confirmDelete')
        }
    }
}
</script>
<style lang="scss" scoped>
.modal {
    display: block !important;
    .delete_msg {
        margin-bottom: 4px;
    }
    .selected {
        font-weight: 700;
        font-size: 14px;
    }
    .desc {
        font-size: 12px;
    }
}

.modal-backdrop {
    position: fixed;
    top: 0;
    left: 0;
    z-index: 1040;
    width: 100vw;
    height: 100vh;
    background-color: #000;
    opacity: 0.5;
}
</style>