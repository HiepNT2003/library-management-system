<template>
    <div class="catalog-settings">

        <div class="page-header">
            <div>
                <h1 class="page-title">Cài đặt danh mục</h1>
                <p class="page-desc">Quản lý các danh mục dùng chung trong hệ thống</p>
            </div>
        </div>

        <!-- Tabs -->
        <div class="tabs">
            <button v-for="tab in tabs" :key="tab.key" class="tab-btn" :class="{ active: activeTab === tab.key }"
                @click="switchTab(tab.key)">
                {{ tab.label }}
            </button>
        </div>

        <!-- Simple catalog tabs: Authors, Categories, Languages -->
        <div v-if="['authors', 'categories', 'languages'].includes(activeTab)" class="tab-content">
            <div class="toolbar">
                <input v-model="search" class="search-input" :placeholder="`Tìm ${currentTab?.label.toLowerCase()}...`"
                    @input="onSearch" />
                <button class="btn btn-primary" @click="openCreate">+ Thêm mới</button>
            </div>

            <div class="table-wrapper">
                <div v-if="isLoading" class="state-box">Đang tải...</div>
                <table v-else class="data-table">
                    <thead>
                        <tr>
                            <th>Tên</th>
                            <th v-if="activeTab === 'languages'">Mã</th>
                            <th v-if="activeTab !== 'languages'">Mô tả</th>
                            <th>Số sách</th>
                            <th>Thao tác</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-if="items.length === 0">
                            <td colspan="4" class="empty-row">Chưa có dữ liệu</td>
                        </tr>
                        <tr v-for="item in items" :key="item[idField]" class="data-row">
                            <td class="item-name">{{ item.name }}</td>
                            <td v-if="activeTab === 'languages'">
                                <span class="code-text">{{ item.code }}</span>
                            </td>
                            <td v-if="activeTab !== 'languages'" class="item-desc">{{ item.description || '—' }}</td>
                            <td><span class="count-badge">{{ item.bookCount }} sách</span></td>
                            <td>
                                <div class="action-buttons">
                                    <button class="action-btn" @click="openEdit(item)"><i
                                            class="bi bi-pencil"></i></button>
                                    <button class="action-btn delete" @click="confirmDelete(item)"
                                        :disabled="item.bookCount > 0"
                                        :title="item.bookCount > 0 ? 'Không thể xóa khi đang có sách' : 'Xóa'"><i
                                            class="bi bi-trash"></i></button>
                                </div>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>

        <!-- DDC tab -->
        <div v-if="activeTab === 'ddc'" class="tab-content">
            <div class="toolbar">
                <input v-model="search" class="search-input" placeholder="Tìm mã hoặc tên DDC..." @input="onSearch" />
                <button class="btn btn-primary" @click="openCreate">+ Thêm mới</button>
            </div>
            <div class="table-wrapper">
                <div v-if="isLoading" class="state-box">Đang tải...</div>
                <table v-else class="data-table">
                    <thead>
                        <tr>
                            <th>Mã DDC</th>
                            <th>Tên</th>
                            <th>Mã cha</th>
                            <th>Số sách</th>
                            <th>Thao tác</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-if="items.length === 0">
                            <td colspan="5" class="empty-row">Chưa có dữ liệu</td>
                        </tr>
                        <tr v-for="item in items" :key="item.dDCId" class="data-row">
                            <td><span class="code-text">{{ item.code }}</span></td>
                            <td class="item-name">{{ item.name }}</td>
                            <td>
                                <span v-if="item.parentCode" class="code-text">{{ item.parentCode }}</span>
                                <span v-else class="text-muted">—</span>
                            </td>
                            <td><span class="count-badge">{{ item.bookCount }} sách</span></td>
                            <td>
                                <div class="action-buttons">
                                    <button class="action-btn" @click="openEdit(item)"><i
                                            class="bi bi-pencil"></i></button>
                                    <button class="action-btn delete" @click="confirmDelete(item)"
                                        :disabled="item.bookCount > 0"><i class="bi bi-trash"></i></button>
                                </div>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>

        <!-- Warehouses tab -->
        <div v-if="activeTab === 'warehouses'" class="tab-content">
            <div class="toolbar">
                <div class="toolbar-left"></div>
                <button class="btn btn-primary" @click="openCreate">+ Thêm kho</button>
            </div>
            <div class="table-wrapper">
                <div v-if="isLoading" class="state-box">Đang tải...</div>
                <table v-else class="data-table">
                    <thead>
                        <tr>
                            <th>Mã</th>
                            <th>Tên kho</th>
                            <th>Vị trí</th>
                            <th>Số bản sao</th>
                            <th>Thao tác</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-if="items.length === 0">
                            <td colspan="5" class="empty-row">Chưa có kho nào</td>
                        </tr>
                        <tr v-for="item in items" :key="item.warehouseId" class="data-row">
                            <td><span class="code-text">{{ item.code }}</span></td>
                            <td class="item-name">{{ item.name }}</td>
                            <td class="item-location">{{ item.location }}</td>
                            <td><span class="count-badge">{{ item.copyCount }} bản</span></td>
                            <td>
                                <div class="action-buttons">
                                    <button class="action-btn" @click="openEdit(item)"><i
                                            class="bi bi-pencil"></i></button>
                                    <button class="action-btn delete" @click="confirmDelete(item)"
                                        :disabled="item.copyCount > 0"><i class="bi bi-trash"></i></button>
                                </div>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>

        <!-- BorrowPolicy tab -->
        <div v-if="activeTab === 'policy'" class="tab-content">
            <div class="toolbar">
                <div class="toolbar-left">
                    <span class="policy-hint">Cấu hình chính sách mượn sách theo vai trò và loại tài liệu</span>
                </div>
                <button class="btn btn-primary" @click="openCreate">+ Thêm chính sách</button>
            </div>
            <div class="table-wrapper">
                <div v-if="isLoading" class="state-box">Đang tải...</div>
                <table v-else class="data-table">
                    <thead>
                        <tr>
                            <th>Vai trò</th>
                            <th>Loại tài liệu</th>
                            <th>Số ngày mượn</th>
                            <th>Tối đa cuốn</th>
                            <th>Lượt gia hạn</th>
                            <th>Phạt/ngày</th>
                            <th>Hạn TT phạt</th>
                            <th>Thao tác</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-if="!items || items.length === 0">
                            <td colspan="7" class="empty-row">Chưa có chính sách nào</td>
                        </tr>
                        <tr v-for="item in items" :key="item.id" class="data-row">
                            <td><span class="role-badge">{{ item.role?.name }}</span></td>
                            <td>{{ item.documentType?.name }}</td>
                            <td>{{ item.maxBorrowDays }} ngày</td>
                            <td>{{ item.maxBooks }} cuốn</td>
                            <td>{{ item.maxExtention }} lần</td>
                            <td>{{ formatMoney(item.finePerDay) }}</td>
                            <td>{{ item.finePaymentDeadlineDays }} ngày</td>
                            <td>
                                <div class="action-buttons">
                                    <button class="action-btn" @click="openEdit(item)"><i
                                            class="bi bi-pencil"></i></button>
                                    <button class="action-btn delete" @click="confirmDelete(item)"><i
                                            class="bi bi-trash"></i></button>
                                </div>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>

        <!-- Modal thêm/sửa -->
        <Teleport to="body">
            <div v-if="showFormModal" class="modal-overlay" @click.self="showFormModal = false">
                <div class="modal">
                    <div class="modal-header">
                        <h3>{{ editingItem ? 'Chỉnh sửa' : 'Thêm mới' }} {{ currentTab?.label }}</h3>
                        <button class="modal-close" @click="showFormModal = false">✕</button>
                    </div>
                    <div class="modal-body">
                        <div class="form-rows">

                            <!-- DDC form -->
                            <template v-if="activeTab === 'ddc'">
                                <div class="form-group">
                                    <label>Mã DDC <span class="required">*</span></label>
                                    <input v-model="form.code" placeholder="VD: 400" :disabled="!!editingItem" />
                                    <span v-if="formErrors.code" class="field-error">{{ formErrors.code }}</span>
                                </div>
                                <div class="form-group">
                                    <label>Tên <span class="required">*</span></label>
                                    <input v-model="form.name" placeholder="VD: Ngôn ngữ" />
                                    <span v-if="formErrors.name" class="field-error">{{ formErrors.name }}</span>
                                </div>
                                <div class="form-group">
                                    <label>Mã DDC cha</label>
                                    <select v-model="form.parentCode">
                                        <option value="">-- Không có (cấp gốc) --</option>
                                        <option v-for="d in items.filter(d => !d.parentCode && d.code !== form.code)"
                                            :key="d.code" :value="d.code">
                                            {{ d.code }} — {{ d.name }}
                                        </option>
                                    </select>
                                </div>
                            </template>

                            <!-- Warehouse form -->
                            <template v-else-if="activeTab === 'warehouses'">
                                <div class="form-group">
                                    <label>Mã kho <span class="required">*</span></label>
                                    <input v-model="form.code" placeholder="VD: PM" :disabled="!!editingItem" />
                                    <span v-if="formErrors.code" class="field-error">{{ formErrors.code }}</span>
                                </div>
                                <div class="form-group">
                                    <label>Tên kho <span class="required">*</span></label>
                                    <input v-model="form.name" placeholder="VD: Phòng Mượn" />
                                    <span v-if="formErrors.name" class="field-error">{{ formErrors.name }}</span>
                                </div>
                                <div class="form-group">
                                    <label>Vị trí</label>
                                    <input v-model="form.location" />
                                </div>
                                <div class="form-group">
                                    <label>Mô tả</label>
                                    <input v-model="form.description" />
                                </div>
                            </template>

                            <!-- BorrowPolicy form -->
                            <template v-else-if="activeTab === 'policy'">
                                <div class="form-group">
                                    <label>Vai trò <span class="required">*</span></label>
                                    <select v-model="form.roleId" :disabled="!!editingItem">
                                        <option value="">-- Chọn vai trò --</option>
                                        <option v-for="r in policyRoles" :key="r.id" :value="r.id">{{ r.name }}</option>
                                    </select>
                                    <span v-if="formErrors.roleId" class="field-error">{{ formErrors.roleId }}</span>
                                </div>
                                <div class="form-group">
                                    <label>Loại tài liệu <span class="required">*</span></label>
                                    <select v-model="form.documentTypeId" :disabled="!!editingItem">
                                        <option value="">-- Chọn loại --</option>
                                        <option v-for="dt in policyDocTypes" :key="dt.documentTypeId"
                                            :value="dt.documentTypeId">
                                            {{ dt.name }}
                                        </option>
                                    </select>
                                    <span v-if="formErrors.documentTypeId" class="field-error">{{
                                        formErrors.documentTypeId }}</span>
                                </div>
                                <div class="form-group">
                                    <label>Số ngày mượn tối đa <span class="required">*</span></label>
                                    <input type="number" v-model.number="form.maxBorrowDays" min="1"
                                        placeholder="VD: 14" />
                                </div>
                                <div class="form-group">
                                    <label>Số cuốn mượn tối đa <span class="required">*</span></label>
                                    <input type="number" v-model.number="form.maxBooks" min="1" placeholder="VD: 3" />
                                </div>
                                <div class="form-group">
                                    <label>Số lần gia hạn tối đa <span class="required">*</span></label>
                                    <input type="number" v-model.number="form.maxExtention" min="0"
                                        placeholder="VD: 1" />
                                </div>
                                <div class="form-group">
                                    <label>Hạn thanh toán phạt (ngày) <span class="required">*</span></label>
                                    <input type="number" v-model.number="form.finePaymentDeadlineDays" min="1"
                                        placeholder="VD: 7" />
                                    <span class="field-hint">Số ngày cho phép trước khi chặn mượn/đặt sách</span>
                                </div>
                                <div class="form-group">
                                    <label>Tiền phạt/ngày (VNĐ) <span class="required">*</span></label>
                                    <input type="number" v-model.number="form.finePerDay" min="0"
                                        placeholder="VD: 2000" />
                                </div>
                            </template>

                            <!-- Authors form -->
                            <!-- <template v-else-if="activeTab === 'authors'">
                                <div class="form-group">
                                    <label>Tên tác giả <span class="required">*</span></label>
                                    <input v-model="form.name" placeholder="Nguyễn Văn A" />
                                    <span v-if="formErrors.name" class="field-error">{{ formErrors.name }}</span>
                                </div>
                                <div class="form-group">
                                    <label>Tiểu sử</label>
                                    <textarea v-model="form.bio" rows="3" placeholder="Mô tả về tác giả..." />
                                </div>
                                <div class="form-group">
                                    <label>Ảnh đại diện (URL)</label>
                                    <input v-model="form.imageUrl" placeholder="https://..." />
                                    <img v-if="form.imageUrl" :src="form.imageUrl" class="img-preview" />
                                </div>
                            </template> -->
                            <!-- Languages form-->
                            <template v-else-if="activeTab === 'languages'">
                                <div class="form-group">
                                    <label>Mã ngôn ngữ <span class="required">*</span></label>
                                    <input v-model="form.code" placeholder="VD: vi, en, fr" :disabled="!!editingItem"
                                        style="text-transform: lowercase" />
                                    <span class="field-hint">Mã ISO 639-1 (2 ký tự)</span>
                                    <span v-if="formErrors.code" class="field-error">{{ formErrors.code }}</span>
                                </div>
                                <div class="form-group">
                                    <label>Tên ngôn ngữ <span class="required">*</span></label>
                                    <input v-model="form.name" placeholder="VD: Tiếng Việt, English..." />
                                    <span v-if="formErrors.name" class="field-error">{{ formErrors.name }}</span>
                                </div>
                            </template>
                            <!-- Simple catalog form (Authors, Categories, Languages) -->
                            <template v-else>
                                <div class="form-group">
                                    <label>Tên <span class="required">*</span></label>
                                    <input v-model="form.name"
                                        :placeholder="`Nhập tên ${currentTab?.label.toLowerCase()}...`" />
                                    <span v-if="formErrors.name" class="field-error">{{ formErrors.name }}</span>
                                </div>
                                <div class="form-group" v-if="activeTab !== 'languages'">
                                    <label>Mô tả</label>
                                    <input v-model="form.description" placeholder="Mô tả thêm (tuỳ chọn)" />
                                </div>
                            </template>

                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-outline" @click="showFormModal = false">Huỷ</button>
                        <button class="btn btn-primary" @click="submitForm" :disabled="isSubmitting">
                            {{ isSubmitting ? 'Đang lưu...' : (editingItem ? 'Cập nhật' : 'Thêm mới') }}
                        </button>
                    </div>
                </div>
            </div>
        </Teleport>

        <ModalAddAuthor :isShowModalAddAuthor="isShowModalAddAuthor" :editInfo="authorEditInfo"
            @on:toogleModal="toogleModalAuthor" @update:listAuthors="fetchData" />
        <!-- Confirm delete -->
        <Teleport to="body">
            <div v-if="showDeleteConfirm" class="modal-overlay" @click.self="showDeleteConfirm = false">
                <div class="modal modal-sm">
                    <div class="modal-header">
                        <h3>Xác nhận xóa</h3>
                        <button class="modal-close" @click="showDeleteConfirm = false">✕</button>
                    </div>
                    <div class="modal-body">
                        <p>Xóa <strong>{{ deletingItem?.name || deletingItem?.code }}</strong>?</p>
                        <p class="text-muted">Hành động này không thể hoàn tác.</p>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-outline" @click="showDeleteConfirm = false">Huỷ</button>
                        <button class="btn btn-danger" @click="submitDelete" :disabled="isSubmitting">
                            {{ isSubmitting ? 'Đang xóa...' : 'Xóa' }}
                        </button>
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, watch } from 'vue'
import api from '../../services/api'
import { useAuthStore } from '../../stores/auth'
import ModalAddAuthor from '../../components/Admin/ModalAddAuthor.vue'

const authStore = useAuthStore()


const activeTab = ref('authors')
const items = ref([])
const isLoading = ref(false)
const search = ref('')
const isSubmitting = ref(false)

const showFormModal = ref(false)
const showDeleteConfirm = ref(false)
const isShowModalAddAuthor = ref(false)
const authorEditInfo = ref(null)
const editingItem = ref(null)
const deletingItem = ref(null)
const form = reactive({})
const formErrors = reactive({})

// For BorrowPolicy
const policyRoles = ref([])
const policyDocTypes = ref([])

let searchTimer = null

const tabs = computed(() => [
    { key: 'authors', label: 'Tác giả', endpoint: 'Authors' },
    { key: 'categories', label: 'Danh mục', endpoint: 'Categories' },
    { key: 'languages', label: 'Ngôn ngữ', endpoint: 'Language' },
    { key: 'ddc', label: 'Phân loại DDC', endpoint: 'DDC' },
    ...(authStore.isAdmin
        ? [{ key: 'warehouses', label: 'Kho sách', endpoint: 'Warehouses' }, { key: 'policy', label: 'Chính sách mượn', endpoint: 'BorrowPolicies' }]
        : [])
])

const currentTab = computed(() => tabs.value.find(t => t.key === activeTab.value))

const idField = computed(() => {
    const map = {
        authors: 'authorId', categories: 'categoryId', languages: 'languageId',
        ddc: 'code',
        warehouses: 'warehouseId', policy: 'id'
    }
    return map[activeTab.value] ?? 'id'
})

onMounted(() => fetchData())

watch(activeTab, () => { search.value = ''; fetchData() })

const switchTab = (key) => { activeTab.value = key }

const fetchData = async () => {
    isLoading.value = true
    try {
        const endpoint = currentTab.value?.endpoint
        const params = search.value.trim() ? `?search=${search.value.trim()}` : ''
        const res = await api.get(`/${endpoint}${params}`)
        if (res.status === 200) {
            if (activeTab.value === 'policy') {
                items.value = res.data.policies
                policyRoles.value = res.data.roles
                policyDocTypes.value = res.data.documentTypes
            } else {
                items.value = res.data
            }
        }
    } catch { }
    finally { isLoading.value = false }
}

const onSearch = () => {
    clearTimeout(searchTimer)
    searchTimer = setTimeout(fetchData, 400)
}

const toogleModalAuthor = (val) => {
    isShowModalAddAuthor.value = val
    if (!val) {
        authorEditInfo.value = {}
    }
}

// ---- Form ----
const defaultForm = () => ({
    name: '', description: '', bio: '', imageUrl: '', location: '',
    code: '', allowBorrow: false,
    roleId: '', documentTypeId: '',
    maxBorrowDays: 14, maxBooks: 3, maxExtention: 1, finePerDay: 2000, finePaymentDeadlineDays: 7,
    parentCode: ''
})

const openCreate = () => {
    if (activeTab.value == 'authors') {
        isShowModalAddAuthor.value = true
        return
    }
    editingItem.value = null
    Object.assign(form, defaultForm())
    Object.keys(formErrors).forEach(k => delete formErrors[k])
    showFormModal.value = true
}

const openEdit = (item) => {
    if (activeTab.value == 'authors') {
        isShowModalAddAuthor.value = true
        authorEditInfo.value = {
            id: item.authorId,
            name: item.name || "",
            bio: item.bio || "",
            imageUrl: item.imageUrl || ""
        }
        return
    }
    editingItem.value = item
    Object.assign(form, {
        name: item.name || '',
        description: item.description || '',
        location: item.location || '',
        code: item.code || '',
        parentCode: item.parentCode || '',
        allowBorrow: item.allowBorrow ?? false,
        roleId: item.role?.id || '',
        documentTypeId: item.documentType?.documentTypeId || '',
        maxBorrowDays: item.maxBorrowDays || 14,
        maxBooks: item.maxBooks || 3,
        maxExtention: item.maxExtention ?? 1,
        finePerDay: item.finePerDay || 2000,
        bio: item.bio || '',
        imageUrl: item.imageUrl || '',
        finePaymentDeadlineDays: item.finePaymentDeadlineDays || 7
    })
    Object.keys(formErrors).forEach(k => delete formErrors[k])
    showFormModal.value = true
}

const validateForm = () => {
    Object.keys(formErrors).forEach(k => delete formErrors[k])

    if (activeTab.value === 'policy') {
        if (!form.roleId) formErrors.roleId = 'Vui lòng chọn vai trò'
        if (!form.documentTypeId) formErrors.documentTypeId = 'Vui lòng chọn loại tài liệu'
    } else if (activeTab.value === 'ddc') {
        if (!form.code?.trim()) formErrors.code = 'Vui lòng nhập mã DDC'
        if (!form.name?.trim()) formErrors.name = 'Vui lòng nhập tên'
    } else if (activeTab.value === 'warehouses') {
        if (!form.code?.trim()) formErrors.code = 'Vui lòng nhập mã kho'
        if (!form.name?.trim()) formErrors.name = 'Vui lòng nhập tên kho'
    } else if (activeTab.value === 'languages') {
        if (!form.code?.trim()) formErrors.code = 'Vui lòng nhập mã ngôn ngữ'
        if (!form.name?.trim()) formErrors.name = 'Vui lòng nhập tên ngôn ngữ'
    } else {
        if (!form.name?.trim()) formErrors.name = 'Vui lòng nhập tên'
    }

    return Object.keys(formErrors).length === 0
}

const submitForm = async () => {
    if (!validateForm()) return
    isSubmitting.value = true

    try {
        const endpoint = currentTab.value?.endpoint
        let payload = {}

        if (activeTab.value === 'policy') {
            payload = {
                roleId: form.roleId, documentTypeId: form.documentTypeId,
                maxBorrowDays: form.maxBorrowDays, maxBooks: form.maxBooks,
                maxExtention: form.maxExtention, finePerDay: form.finePerDay, finePaymentDeadlineDays: form.finePaymentDeadlineDays
            }
        } else if (activeTab.value === 'ddc') {
            payload = { code: form.code, name: form.name, parentCode: form.parentCode || null }
        } else if (activeTab.value === 'warehouses') {
            payload = { code: form.code, name: form.name, location: form.location || null, description: form.description || null }
        } else if (activeTab.value === 'authors') {
            payload = {
                name: form.name,
                bio: form.bio || null,
                imageUrl: form.imageUrl || null
            }
        } else if (activeTab.value === 'languages') {
            payload = { code: form.code.trim().toLowerCase(), name: form.name.trim() }
        } else {
            payload = { name: form.name, description: form.description || null }
        }

        const id = editingItem.value?.[idField.value]
        const res = editingItem.value
            ? await api.put(`/${endpoint}/${id}`, payload)
            : await api.post(`/${endpoint}`, payload)

        if (res.status === 200 || res.status === 201) {
            showFormModal.value = false
            await fetchData()
        }
    } catch (err) {
        const msg = err.response?.data?.message || 'Thao tác thất bại'
        if (msg.toLowerCase().includes('mã') || msg.toLowerCase().includes('code'))
            formErrors.code = msg
        else
            alert(msg)
    } finally {
        isSubmitting.value = false
    }
}

// ---- Delete ----
const confirmDelete = (item) => {
    deletingItem.value = item
    showDeleteConfirm.value = true
}

const submitDelete = async () => {
    isSubmitting.value = true
    try {
        const endpoint = currentTab.value?.endpoint
        const id = deletingItem.value[idField.value]
        const res = await api.delete(`/${endpoint}/${id}`)
        if (res.status === 204) {
            showDeleteConfirm.value = false
            await fetchData()
        }
    } catch (err) {
        alert(err.response?.data?.message || 'Xóa thất bại')
    } finally {
        isSubmitting.value = false
    }
}

const formatMoney = (n) => n != null
    ? new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(n) : '—'
</script>

<style lang="scss" scoped>
@use "@/assets/scss/variables.scss" as V;

.catalog-settings {
    display: flex;
    flex-direction: column;
    gap: 20px;
    font-family: 'Segoe UI', sans-serif;
    color: #1a1a2e;
    padding: 16px 24px;
    background: #ffffff;
    border-radius: 12px;
}

.page-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
}

.page-title {
    font-size: 22px;
    font-weight: 800;
    margin: 0 0 4px;
}

.page-desc {
    font-size: 14px;
    color: #666;
    margin: 0;
}

// Tabs
.tabs {
    display: flex;
    gap: 4px;
    border-bottom: 2px solid #e0e0e0;
    flex-wrap: wrap;
}

.tab-btn {
    padding: 10px 16px;
    background: none;
    border: none;
    cursor: pointer;
    font-size: 14px;
    font-weight: 500;
    color: #666;
    border-bottom: 2px solid transparent;
    margin-bottom: -2px;
    transition: all 0.15s;
    border-radius: 6px 6px 0 0;

    &:hover {
        color: #435ebe;
        background: #f5f6ff;
    }

    &.active {
        color: #435ebe;
        border-bottom-color: #435ebe;
        font-weight: 700;
    }
}

// Tab content
.tab-content {
    display: flex;
    flex-direction: column;
    gap: 14px;
}

.toolbar {
    display: flex;
    align-items: center;
    gap: 10px;
    justify-content: space-between;
}

.toolbar-left {
    flex: 1;
}

.search-input {
    flex: 1;
    max-width: 320px;
    padding: 9px 14px;
    border: 1.5px solid #e0e0e0;
    border-radius: 8px;
    font-size: 14px;
    outline: none;
    font-family: inherit;

    &:focus {
        border-color: #435ebe;
    }
}

.policy-hint {
    font-size: 13px;
    color: #666;
}

// Table
.table-wrapper {
    border-radius: 10px;
    border: 1px solid #e0e0e0;
    overflow: auto;
    max-height: 610px;
    @include V.custom-scroll-bar;
}

.data-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 14px;

    thead {
        position: sticky;
        top: 0;
    }

    thead tr {
        background: #f5f5f5;
    }

    th {
        padding: 10px 14px;
        text-align: left;
        font-weight: 600;
        color: #555;
        border-bottom: 1px solid #e0e0e0;
        white-space: nowrap;
    }

    td {
        padding: 10px 14px;
        border-bottom: 1px solid #f0f0f0;
        vertical-align: middle;
    }
}

.data-row {
    &:last-child td {
        border-bottom: none;
    }

    &:hover {
        background: #fafafa;
    }
}

.item-name {
    font-weight: 600;
}

.item-desc {
    color: #888;
    font-size: 13px;
    max-width: 200px;
}

.code-text {
    font-family: monospace;
    color: #435ebe;
    font-weight: 600;
}

.empty-row {
    text-align: center;
    color: #aaa;
    padding: 32px;
}

.count-badge {
    display: inline-block;
    padding: 2px 8px;
    background: #e8eaf6;
    color: #435ebe;
    border-radius: 99px;
    font-size: 12px;
    font-weight: 500;
}

.badge {
    display: inline-block;
    padding: 2px 8px;
    border-radius: 99px;
    font-size: 12px;
    font-weight: 600;

    &.badge-green {
        background: #e8f5e9;
        color: #2e7d32;
    }

    &.badge-gray {
        background: #f5f5f5;
        color: #757575;
    }
}

.role-badge {
    display: inline-block;
    padding: 2px 10px;
    background: #e8eaf6;
    color: #435ebe;
    border-radius: 99px;
    font-size: 12px;
    font-weight: 600;
}

.action-buttons {
    display: flex;
    gap: 4px;

    button {
        color: #435ebe;
    }
}

.action-btn {
    background: none;
    border: none;
    cursor: pointer;
    padding: 4px 6px;
    border-radius: 6px;
    font-size: 15px;
    transition: background 0.15s;

    &:hover:not(:disabled) {
        background: #f0f0f0;
    }

    &:disabled {
        opacity: 0.3;
        cursor: not-allowed;
    }
}

// Modal
.modal-overlay {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.45);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
    padding: 16px;
}

.modal {
    background: #fff;
    border-radius: 14px;
    width: 100%;
    max-width: 480px;
    max-height: 90vh;
    overflow-y: auto;
    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.2);
    display: block;
    height: unset;
    top: unset;
    left: unset;
}

.modal-sm {
    max-width: 380px;
}

.modal-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 20px 24px 16px;
    border-bottom: 1px solid #f0f0f0;

    h3 {
        margin: 0;
        font-size: 17px;
        font-weight: 700;
    }
}

.modal-close {
    background: none;
    border: none;
    font-size: 18px;
    cursor: pointer;
    color: #aaa;
    padding: 4px 8px;
    border-radius: 6px;

    &:hover {
        background: #f0f0f0;
    }
}

.modal-body {
    max-height: 460px;
    overflow: auto;
    padding: 20px 24px;
}

.modal-footer {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
    padding: 16px 24px 20px;
    border-top: 1px solid #f0f0f0;
}

.form-rows {
    display: flex;
    flex-direction: column;
    gap: 14px;
}

.form-group {
    display: flex;
    flex-direction: column;
    gap: 6px;

    label {
        font-size: 13px;
        font-weight: 600;
        color: #444;
    }

    input,
    select {
        padding: 8px 12px;
        border: 1.5px solid #e0e0e0;
        border-radius: 8px;
        font-size: 14px;
        outline: none;
        font-family: inherit;
        background: #fff;
        color: #333333;

        &:focus {
            border-color: #435ebe;
        }

        &:disabled {
            background: #f5f5f5;
            color: #999;
        }
    }
}

.checkbox-label {
    display: flex !important;
    flex-direction: row !important;
    align-items: center;
    gap: 8px;
    cursor: pointer;
    font-weight: 500 !important;

    input {
        width: 16px;
        height: 16px;
        cursor: pointer;
    }
}

.required {
    color: #e53935;
}

.field-error {
    color: #e53935;
    font-size: 12px;
}

.text-muted {
    color: #888;
    font-size: 13px;
    margin-top: 4px;
}

.btn {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 9px 18px;
    border-radius: 8px;
    font-size: 14px;
    font-weight: 500;
    cursor: pointer;
    border: none;
    transition: all 0.15s;

    &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
    }

    &.btn-primary {
        background: #435ebe;
        color: #fff;

        &:hover:not(:disabled) {
            background: #2c3a8c;
        }
    }

    &.btn-outline {
        background: #fff;
        color: #435ebe;
        border: 1.5px solid #435ebe;

        &:hover:not(:disabled) {
            background: #e8eaf6;
        }
    }

    &.btn-danger {
        background: #e53935;
        color: #fff;

        &:hover:not(:disabled) {
            background: #c62828;
        }
    }
}

.state-box {
    padding: 32px;
    text-align: center;
    color: #888;
    font-size: 14px;
}

.img-preview {
    width: 80px;
    height: 80px;
    object-fit: cover;
    border-radius: 8px;
    border: 1px solid #e0e0e0;
    margin-top: 6px;
}

textarea {
    padding: 8px 12px;
    border: 1.5px solid #e0e0e0;
    border-radius: 8px;
    font-size: 14px;
    outline: none;
    font-family: inherit;
    resize: vertical;

    &:focus {
        border-color: #435ebe;
    }
}

.field-hint {
    font-size: 12px;
    color: #999;
    margin-top: 2px;
}
</style>