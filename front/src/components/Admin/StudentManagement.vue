<template>
    <div class="page-heading">
        <section class="section">
            <div class="card">
                <div class="card-body">
                    <div class="dataTable-wrapper dataTable-loading no-footer sortable searchable fixed-columns">
                        <div class="dataTable-top">
                            <div class="dataTable-dropdown">
                                <select class="dataTable-selector form-select" fdprocessedid="qpf37"
                                    @change="selectItemPerPage($event)">
                                    <option :value="value" :selected="value == itemPerPage"
                                        v-for="value in listItemsPerPage" :key="value">
                                        {{ value }}
                                    </option>
                                </select><label>mục trên trang</label>
                            </div>
                            <div class="dataTable-search">
                                <input class="user-search" placeholder="Tìm kiếm theo tên hoặc mã sinh viên..."
                                    type="text" v-model="searchKeyword" @input="handleChangeKeyword" />
                                <div class="btn-refresh">
                                    <button class="btn btn-outline-secondary" @click="handleRefreshFilter">
                                        <Icon icon="mi:refresh" width="24" height="24" />
                                    </button>
                                </div>
                                <div class="btn-add">
                                    <button class="btn icon icon-left btn-primary" @click="visible = true">
                                        <Icon icon="line-md:plus" width="24" height="24" />
                                        Thêm bạn đọc
                                    </button>
                                </div>
                                <div class="other_menu">
                                    <Button class="button_other" type="button" icon="pi pi-ellipsis-v" @click="toggle"
                                        aria-haspopup="true" aria-controls="overlay_menu" />
                                    <Menu class="menu" ref="menu" id="overlay_menu" :model="items" :popup="true" />
                                </div>
                            </div>
                        </div>
                        <div class="dataTable-container" v-if="students.length">
                            <table class="table table-striped dataTable-table" id="table1">
                                <thead>
                                    <tr>
                                        <th>
                                            <Checkbox v-model="checkAll" binary />
                                        </th>
                                        <th v-for="title in headerLabel" :key="title.id">
                                            <p class="table_header" :style="{ width: title.width }"
                                                v-if="!title.canSort">
                                                {{ title.title }}
                                            </p>
                                            <div class="header_sort" :style="{ width: title.width }" v-else>
                                                <p class="table_header">{{ title.title }}</p>
                                                <span class="sort-icon-up"
                                                    :class="{ active: sortOrder == 'asc' && sortBy == title.key }"
                                                    @click.stop="sortAsc(title)"></span>
                                                <span class="sort-icon-down"
                                                    :class="{ active: sortOrder == 'desc' && sortBy == title.key }"
                                                    @click.stop="sortDesc(title)"></span>
                                            </div>
                                        </th>
                                        <th class="action-label">Action</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr v-for="student in students" :key="student.id">
                                        <td>
                                        <th>
                                            <Checkbox v-model="student.isChecked" binary />
                                        </th>
                                        </td>
                                        <td>
                                            <p class="data_title">{{ student.studentCode }}</p>
                                        </td>
                                        <td>
                                            <p class="data_title">{{ student.fullName }}</p>
                                        </td>
                                        <td>
                                            <p class="data_title">{{ formatDate(student.expiredDate) }}</p>
                                        </td>
                                        <td>
                                            {{ student.term }}
                                        </td>
                                        <td>{{ student.class }}</td>
                                        <td>{{ student.faculty }}</td>
                                        <td>{{ student.status }}</td>
                                        <td>
                                            <div class="action">
                                                <span @click="editUser(student)" class="btn icon btn-primary"><i
                                                        class="bi bi-pencil"></i></span>
                                            </div>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                        <NotFound />
                        <Pagination v-if="totalPages" :page="page" :item-per-page="itemPerPage"
                            :total-record="totalRecord" :total-pages="totalPages" @changePage="handleChangePage" />
                    </div>
                </div>
            </div>
        </section>
        <Dialog v-model:visible="visible" maximizable modal header="Thêm bạn đọc - Sinh viên"
            :style="{ width: '50rem' }" :breakpoints="{ '1199px': '75vw', '575px': '90vw' }">
            <div class="form_wrap">
                <div class="form">
                    <div class="form_group">
                        <p class="title">Thông tin bạn đọc</p>
                        <div class="info_wrap">
                            <div class="left">
                                <div class="field">
                                    <label for="fullName">Họ và tên</label><span class="required">*</span>
                                    <input type="text" v-model="studentInfo.fullName" class="" name="fullName">
                                    <p class="error">{{ errorMessage.fullName }}</p>
                                </div>
                                <div class="field">
                                    <label for="email">Email</label><span class="required">*</span>
                                    <input type="email" v-model="studentInfo.email" class="" name="email"
                                        @blur="validateEmail">
                                    <p class="error">{{ errorMessage.email }}</p>
                                </div>
                                <div class="field">
                                    <label for="phoneNumber">Số điện thoại</label>
                                    <input type="tel" v-model="studentInfo.phoneNumber" class="" name="phoneNumber">
                                    <p class="error">{{ errorMessage.phoneNumber }}
                                    </p>
                                </div>
                            </div>
                            <div class="right">
                                <div class="field">
                                    <label for="faculty">Chuyên ngành</label><span class="required">*</span>
                                    <input type="text" v-model="studentInfo.faculty" class="" name="faculty">
                                    <p class="error">{{ errorMessage.faculty }}</p>
                                </div>
                                <div class="field">
                                    <label for="faculty">Khóa</label><span class="required">*</span>
                                    <input type="text" v-model="studentInfo.term" class="" name="faculty">
                                    <p class="error">{{ errorMessage.term }}</p>
                                </div>
                                <div class="field">
                                    <label for="class">Lớp</label><span class="required">*</span>
                                    <input type="text" v-model="studentInfo.class" class="" name="class">
                                    <p class="error">{{ errorMessage.class }}</p>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="form_group">
                        <p class="title">Thông tin thẻ</p>
                        <div class="info_wrap">
                            <div class="left">
                                <div class="field">
                                    <label for="studentCode">Mã sinh viên</label><span class="required">*</span>
                                    <input type="number" v-model="studentInfo.studentCode" class="" name="studentCode">
                                    <p class="error">{{ errorMessage.studentCode }}
                                    </p>
                                </div>
                                <div class="field field_status">
                                    <label for="studentCode">Trạng thái</label>
                                    <Select v-model="studentInfo.status" :options="listUserStatus" optionLabel="name"
                                        class="w-full md:w-56" />
                                </div>
                            </div>
                            <div class="right">
                                <div class="field">
                                    <label for="expiredDate">Ngày hết hạn</label>
                                    <input type="date" v-model="studentInfo.expiredDate" class="" name="expiredDate">
                                    <p class="error">{{ errorMessage.expiredDate }}
                                    </p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div class="btn_group">
                <button class="btn_close" @click="visible = false">Đóng</button>
                <button class="btn_save" @click="onAddStudent">Lưu</button>
            </div>
        </Dialog>
    </div>
</template>
<script setup>
import Dialog from 'primevue/dialog';
import { ref, watch, onMounted } from "vue";
import Select from 'primevue/select';
import _ from 'lodash';
import api from "../../services/api.js"
import { useAuthStore } from "@/stores/auth"
import { useToastMessageStore } from "../../stores/toastMessage"
import { TOAST_MESSAGE_STATUS } from "../../constants"
import Menu from "primevue/menu"
import Button from "primevue/button"
import Checkbox from "primevue/checkbox"
import { Icon } from "@iconify/vue"
import debounce from "lodash/debounce"

const visible = ref(false);
const listUserStatus = ref([{ id: 0, name: "Đang hoạt động" }, { id: 1, name: "Dừng hoạt động" }, { id: 2, name: "Chặn hoạt đông" }])
const studentInfo = ref({
    fullName: "",
    faculty: "",
    email: "",
    phoneNumber: "",
    class: "",
    studentCode: "",
    status: listUserStatus.value[0],
    expiredDate: new Date().toISOString(),
    term: "",
})
const errorMessage = ref({
    fullName: "",
    faculty: "",
    email: "",
    phoneNumber: "",
    class: "",
    studentCode: "",
    expiredDate: "",
    term: ""
})
const students = ref([])
const listItemsPerPage = ref([5, 10, 15, 20, 25])
const headerLabel = ref([
    {
        id: 1,
        title: "Mã sinh viên",
        key: "studentCode",
        canSort: true,
        width: "100px",
    },
    {
        id: 2,
        title: "Họ Tên",
        key: "fullName",
        canSort: true,
        width: "160px",
    },
    {
        id: 3,
        title: "Ngày hết hạn",
        key: "expiredDate",
        canSort: false,
        width: "140px",
    },
    {
        id: 4,
        title: "Khóa",
        key: "term",
        canSort: true,
    },
    {
        id: 5,
        title: "Lớp",
        key: "class",
        canSort: true,
        width: "180px"
    },
    {
        id: 6,
        title: "Chuyên ngành",
        key: "faculty",
        canSort: true,
        width: "160px"
    },
    {
        id: 7,
        title: "Trạng thái",
        key: "status",
        canSort: true,
        width: "120px"
    },
])
const itemPerPage = ref(10)
const page = ref(1)
const totalPages = ref(0)
const totalRecord = ref(0)
const sortOrder = ref("")
const sortBy = ref("")
const searchKeyword = ref("")
const selectedUser = ref([])
// const router = useRouter()

watch(
    () => _.cloneDeep(studentInfo.value),
    (newVal, oldVal) => {
        if (oldVal.fullName !== newVal.fullName) {
            if (!studentInfo.value.fullName) {
                errorMessage.value.fullName = "Vui lòng nhập họ và tên"
            } else {
                errorMessage.value.fullName = ""
            }
        }
        if (oldVal.faculty !== newVal.faculty) {
            if (!studentInfo.value.faculty) {
                errorMessage.value.faculty = "Vui lòng nhập chuyên ngành"
            } else {
                errorMessage.value.faculty = ""
            }
        }
        if (oldVal.class !== newVal.class) {
            if (!studentInfo.value.class) {
                errorMessage.value.class = "Vui lòng nhập lớp"
            } else {
                errorMessage.value.class = ""
            }
        }
        if (oldVal.studentCode !== newVal.studentCode) {
            if (!studentInfo.value.studentCode) {
                errorMessage.value.studentCode = "Vui lòng nhập mã sinh viên"
            } else {
                errorMessage.value.studentCode = ""
            }
        }
        if (oldVal.term !== newVal.term) {
            if (!studentInfo.value.term) {
                errorMessage.value.term = "Vui lòng nhập khóa học"
            } else {
                errorMessage.value.term = ""
            }
        }
    }
);
watch(
    () => _.cloneDeep(visible.value),
    (newVal) => {
        if (!newVal.value) {
            studentInfo.value = {
                fullName: "",
                faculty: "",
                email: "",
                phoneNumber: "",
                class: "",
                studentCode: "",
                status: listUserStatus.value[0],
                expiredDate: new Date().toISOString(),
                term: "",
            }
            errorMessage.value = {
                fullName: "",
                faculty: "",
                email: "",
                phoneNumber: "",
                class: "",
                studentCode: "",
                expiredDate: "",
                term: ""
            }
        }
    }
);
watch(searchKeyword, () => {
    debouncedSearch()
})
watch(
    [page, itemPerPage, sortBy, sortOrder],
    async ([newPage, newItemPerPage, newSortBy, newSortOrder]) => {
        await handleGetStudents(searchKeyword.value, newPage, newItemPerPage, newSortBy, newSortOrder)
    }
)
onMounted(async () => {
    await handleGetStudents(
        searchKeyword.value,
        page.value,
        itemPerPage.value,
        sortBy.value,
        sortOrder.value
    )
})
let controller = null
async function handleGetStudents(
    search,
    curentPage,
    currentItemPerPage,
    currentSortBy,
    currentSortOrder
) {
    if (controller) {
        controller.abort()
    }
    controller = new AbortController()
    const authStore = useAuthStore()
    authStore.setIsLoadingApi(true)
    try {
        const res = await api.get("/account/students", {
            params: {
                search,
                page: curentPage,
                pageSize: currentItemPerPage,
                sortBy: currentSortBy,
                sortOrder: currentSortOrder,
            },
            signal: controller.signal,
        })
        if (res.status == 200) {
            students.value = res.data.data.map((student) => ({ ...student, isChecked: false }))
            totalPages.value = res.data.meta.totalPages
            totalRecord.value = res.data.meta.totalRecords
        }
    } catch (error) {
        authStore.setIsLoadingApi(false)
    }
    authStore.setIsLoadingApi(false)
}
const debouncedSearch = debounce(async () => {
    page.value = 1
    await handleGetStudents(
        searchKeyword.value,
        page.value,
        itemPerPage.value,
        sortBy.value,
        sortOrder.value
    )
}, 500)
function handleRefreshFilter() {
    searchKeyword.value = ""
    itemPerPage.value = 10
    sortBy.value = ""
    sortOrder.value = ""
}
function handleChangeKeyword(event) {
    searchKeyword.value = event.target.value
}
function selectItemPerPage(event) {
    page.value = 1
    itemPerPage.value = event?.target?.value
}
function onAddStudent() {
    checkValidForm()
    validateEmail()
    if (errorMessage.value.studentCode || errorMessage.value.class || errorMessage.value.faculty || errorMessage.value.fullName || errorMessage.value.email || errorMessage.value.term) return
    addStudent()
}
async function addStudent() {
    const toasMessageStore = useToastMessageStore()
    const authStore = useAuthStore()
    authStore.setIsLoadingApi(true)
    try {
        const params = {
            FullName: studentInfo.value.fullName.toString(),
            Faculty: studentInfo.value.faculty.toString(),
            Class: studentInfo.value.class.toString(),
            Email: studentInfo.value.email.toString(),
            StudentCode: studentInfo.value.studentCode.toString(),
            Term: studentInfo.value.term.toString(),
            Status: studentInfo.value.status.id,
            PhoneNumber: studentInfo.value.phoneNumber.toString(),
            ExpiredDate: studentInfo.value.expiredDate
        }
        const res = await api.post("/account/students", params)
        if (res.status == 200 || res.status == 201) {
            toasMessageStore.showToastMessage(
                res?.data?.message,
                TOAST_MESSAGE_STATUS.success,
                2000,
            )
            visible.value = false
        } else {
            toasMessageStore.showToastMessage(
                res?.data?.message,
                TOAST_MESSAGE_STATUS.error,
                2000,
            )
        }
    } catch (error) {
        toasMessageStore.showToastMessage(
            error?.response?.data?.message,
            TOAST_MESSAGE_STATUS.error,
            5000,
        )
        authStore.setIsLoadingApi(false)
    }
    authStore.setIsLoadingApi(false)
}
const checkValidForm = () => {
    if (!studentInfo.value.fullName) {
        errorMessage.value.fullName = "Vui lòng nhập họ và tên"
    } else {
        errorMessage.value.fullName = ""
    }
    if (!studentInfo.value.faculty) {
        errorMessage.value.faculty = "Vui lòng nhập chuyên ngành"
    } else {
        errorMessage.value.faculty = ""
    }
    if (!studentInfo.value.class) {
        errorMessage.value.class = "Vui lòng nhập lớp"
    } else {
        errorMessage.value.class = ""
    }
    if (!studentInfo.value.studentCode) {
        errorMessage.value.studentCode = "Vui lòng nhập mã sinh viên"
    } else {
        errorMessage.value.studentCode = ""
    }
    if (!studentInfo.value.term) {
        errorMessage.value.term = "Vui lòng nhập khóa học"
    } else {
        errorMessage.value.term = ""
    }
}
const validateEmail = () => {
    if (!studentInfo.value.email) {
        errorMessage.value.email = 'Vui lòng nhập email';
    } else if (!isValidEmail(studentInfo.value.email)) {
        errorMessage.value.email = 'Email sai định dạng';
    } else {
        errorMessage.value.email = '';
    }
};
const isValidEmail = (email) => {
    const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return regex.test(email);
};
const formatDate = (dateString) => {
    if (!dateString) return ''

    const date = new Date(dateString)

    return date.toLocaleDateString('vi-VN')
}
</script>
<style lang="scss" scoped>
.btn-add {
    width: fit-content;
}

.dataTable-top {
    margin-bottom: 16px;
    display: flex;
    justify-content: space-between;

    .dataTable-dropdown {
        display: flex;
        align-items: center;

        .form-select {
            padding: 0.3rem 1.6rem 0.3rem 0.5rem;
            font-size: 14px;
            background-position: right 0.3rem center;
            width: fit-content;

            &:focus {
                box-shadow: none;
            }
        }

        label {
            font-size: 14px;
            margin-left: 8px;
        }
    }
}

.dataTable-search {
    display: flex;
    gap: 8px;
    align-items: center;

    .btn-refresh {
        height: 100%;

        button {
            width: 34px;
            height: 34px;
            padding: 0;
            padding-bottom: 3px;
            border-color: #dce7f1;
        }
    }

    .btn-add button {
        align-items: center;
        justify-content: center;
        display: flex;
        height: 34px;
    }

    .other_menu {
        .button_other {
            width: 34px;
            height: 34px;
            background: #6c757d;
            border: 1px solid #dcdcdc;
            color: #ffffff;

            &:focus {
                outline: none;
            }
        }
    }
}

.action {
    width: max-content;
    display: flex;
    gap: 12px;
}

.user-search {
    border: 1px solid #dce7f1;
    height: 34px;
    border-radius: 8px;
    padding: 8px;
    width: 300px;
    background: transparent;
    color: #333333;
    font-size: 14px;

    &:focus-visible {
        outline: none;
    }
}

.data_title {
    margin-bottom: 0;
}
</style>
<style lang="scss">
.student_img {
    display: flex;
    flex-direction: column;
    text-align: center;

    img {
        display: block;
        margin-bottom: 4px;
    }
}

.upload-btn {
    width: 100%;
    text-align: center;
    margin-top: 48px;

    .choose_img {
        text-align: center;
        cursor: pointer;
    }
}

.form_wrap {
    // display: grid;
    // grid-template-columns: 1fr 3fr;
    margin-bottom: 40px;

    .info_wrap {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 12px;
    }
}

.form {
    .form_group {
        .field {
            margin-bottom: 4px;
        }

        .field_status {
            display: flex;
            gap: 8px;
            align-items: center;
        }

        .title {
            font-size: 18px;
            font-weight: 700;
        }

        .required {
            color: red;
        }

        label {
            margin-bottom: 4px;
        }

        input {
            border: 1px solid #a1afdf;
            border-radius: 4px;
            padding: .375rem .75rem;
            width: 100%;
        }

        .error {
            font-size: 12px;
            color: red;
            height: 18px;
            margin-bottom: 0;
        }
    }
}

.btn_group {
    display: flex;
    gap: 8px;
    justify-content: flex-end;
    margin-top: 16px;
    position: absolute;
    bottom: 24px;
    right: 24px;
}

.btn_close,
.btn_save {
    border-radius: 4px;
    padding: 4px 16px;
    border: none;
}

.btn_close {
    border: 1px solid #dcdcdc;
    background: transparent;
}

.btn_save {
    background: #435ebe;
}
</style>