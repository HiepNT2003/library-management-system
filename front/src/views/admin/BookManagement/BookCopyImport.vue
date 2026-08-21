<template>
    <div class="import-page">

        <!-- Header -->
        <div class="page-header">
            <div>
                <h1 class="page-title">Nhập bản sao hàng loạt</h1>
                <p class="page-desc">Upload file Excel chứa danh sách bản sao cần nhập</p>
            </div>
            <button class="btn-download" @click="downloadTemplate">
                <icon class="icon_excel" icon="file-icons:microsoft-excel" width="14" height="14" /> Tải file mẫu
            </button>
        </div>

        <!-- Step indicator -->
        <div class="steps">
            <div class="step" :class="{ active: step >= 1, done: step > 1 }">
                <div class="step-dot">{{ step > 1 ? '✓' : '1' }}</div>
                <span>Upload file</span>
            </div>
            <div class="step-line" :class="{ done: step > 1 }"></div>
            <div class="step" :class="{ active: step >= 2, done: step > 2 }">
                <div class="step-dot">{{ step > 2 ? '✓' : '2' }}</div>
                <span>Kiểm tra dữ liệu</span>
            </div>
            <div class="step-line" :class="{ done: step > 2 }"></div>
            <div class="step" :class="{ active: step >= 3 }">
                <div class="step-dot">3</div>
                <span>Hoàn tất</span>
            </div>
        </div>

        <!-- Step 1: Upload -->
        <div v-if="step === 1" class="card">
            <div class="drop-zone" :class="{ dragging: isDragging, 'has-file': selectedFile }"
                @dragover.prevent="isDragging = true" @dragleave="isDragging = false" @drop.prevent="onDrop"
                @click="$refs.fileInput.click()">
                <input ref="fileInput" type="file" accept=".xlsx,.xls,.csv" hidden @change="onFileChange" />
                <div v-if="!selectedFile" class="drop-content">
                    <div class="drop-icon">📂</div>
                    <div class="drop-text">Kéo thả file vào đây hoặc <span class="link">chọn file</span></div>
                    <div class="drop-hint">Hỗ trợ .xlsx, .xls, .csv — tối đa 5MB</div>
                </div>
                <div v-else class="file-selected">
                    <div class="file-icon">📄</div>
                    <div>
                        <div class="file-name">{{ selectedFile.name }}</div>
                        <div class="file-size">{{ formatFileSize(selectedFile.size) }}</div>
                    </div>
                    <button class="btn-remove" @click.stop="clearFile">✕</button>
                </div>
            </div>

            <div class="card-footer">
                <button class="btn btn-outline" @click="backToList"><Icon icon="lsicon:arrow-left-filled" width="16" height="16" />Quay lại</button>
                <button class="btn btn-primary" :disabled="!selectedFile || isParsing" @click="parseFile">
                    {{ isParsing ? 'Đang đọc file...' : 'Tiếp tục' }}
                    <Icon v-if="!isParsing" icon="humbleicons:arrow-right" width="16" height="16" />
                </button>
            </div>
        </div>

        <!-- Step 2: Preview & Validate -->
        <div v-if="step === 2" class="card">
            <!-- Summary -->
            <div class="summary-bar">
                <div class="summary-item summary-total">
                    <strong>{{ rows.length }}</strong> dòng
                </div>
                <div class="summary-item summary-ok">
                    <strong>{{ validCount }}</strong> hợp lệ
                </div>
                <div class="summary-item summary-error" v-if="errorCount > 0">
                    <strong>{{ errorCount }}</strong> lỗi
                </div>
                <div class="summary-item summary-warning" v-if="warningCount > 0">
                    <strong>{{ warningCount }}</strong> cảnh báo
                </div>
            </div>

            <div v-if="errorCount > 0" class="error-notice">
                ⚠️ Còn <strong>{{ errorCount }}</strong> dòng lỗi. Vui lòng sửa file và upload lại trước khi import.
            </div>

            <!-- Table -->
            <div class="table-wrapper">
                <table class="preview-table">
                    <thead>
                        <tr>
                            <th>#</th>
                            <th>BookId <span class="required">*</span></th>
                            <th>Barcode</th>
                            <th>Kho <span class="required">*</span></th>
                            <th>Vị trí kệ</th>
                            <th>Tình trạng</th>
                            <th>Ngày nhập</th>
                            <th>Chỉ tham khảo</th>
                            <th>Ghi chú</th>
                            <th>Trạng thái</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="(row, idx) in rows" :key="idx" :class="rowClass(row)">
                            <td class="row-num">{{ idx + 1 }}</td>
                            <td :class="{ 'cell-error': row.errors.bookId }">
                                {{ row.bookId }}
                                <div v-if="row.errors.bookId" class="cell-error-msg">{{ row.errors.bookId }}</div>
                            </td>
                            <td :class="{ 'cell-warning': row.warnings?.barcode }">
                                {{ row.barcode }}<span v-if="!row.barcode" class="auto-gen"> tự sinh </span>
                                <div v-if="row.warnings?.barcode" class="cell-warn-msg">{{ row.warnings.barcode }}</div>
                            </td>
                            <td :class="{ 'cell-error': row.errors.warehouseId }">
                                {{ warehouseName(row.warehouseId) || row.warehouseId }}
                                <div v-if="row.errors.warehouseId" class="cell-error-msg">{{ row.errors.warehouseId }}
                                </div>
                            </td>
                            <td>{{ row.shelfLocation || '—' }}</td>
                            <td>{{ row.bookCondition || '—' }}</td>
                            <td>{{ row.purchaseDate || '—' }}</td>
                            <td>{{ row.isReferenceOnly ? 'Có' : 'Không' }}</td>
                            <td>{{ row.notes || '—' }}</td>
                            <td>
                                <span class="row-status" :class="rowStatusClass(row)">
                                    {{ rowStatusLabel(row) }}
                                </span>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>

            <div class="card-footer">
                <button class="btn btn-outline" @click="step = 1; clearFile()"><Icon icon="lsicon:arrow-left-filled" width="14" height="14" />Quay lại</button>
                <button class="btn btn-primary" :disabled="errorCount > 0 || isImporting" @click="doImport">
                    {{ isImporting ? 'Đang import...' : `Import ${validCount} bản sao` }}
                </button>
            </div>
        </div>

        <!-- Step 3: Result -->
        <div v-if="step === 3" class="card result-card">
            <div class="result-icon"><Icon class="icon_success" icon="charm:circle-tick" width="56" height="56" /></div>
            <h2 class="result-title">Import thành công</h2>
            <p class="result-desc">Đã thêm <strong>{{ importResult.success }}</strong> bản sao vào hệ thống.</p>
            <div class="result-actions">
                <button class="btn btn-outline" @click="resetAll">Import thêm</button>
                <router-link to="/admin/books" class="btn btn-primary">Xem danh sách</router-link>
            </div>
        </div>

    </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import * as XLSX from 'xlsx'
import api from '../../../services/api'
import { Icon } from '@iconify/vue'
import { useRouter } from 'vue-router'

const router = useRouter()

// ---- State ----
const step = ref(1)
const selectedFile = ref(null)
const isDragging = ref(false)
const isParsing = ref(false)
const isImporting = ref(false)
const rows = ref([])
const importResult = ref({ success: 0 })
const warehouses = ref([])

// Load warehouses on setup
const loadWarehouses = async () => {
    try {
        const res = await api.get('/Warehouses')
        if (res.status === 200) warehouses.value = res.data
    } catch { }
}
loadWarehouses()

// ---- Computed ----
const validCount = computed(() => rows.value.filter(r => !hasError(r)).length)
const errorCount = computed(() => rows.value.filter(r => hasError(r)).length)
const warningCount = computed(() => rows.value.filter(r => !hasError(r) && hasWarning(r)).length)

const hasError = (row) => Object.keys(row.errors).length > 0
const hasWarning = (row) => row.warnings && Object.keys(row.warnings).length > 0

// ---- File handling ----
const onDrop = (e) => {
    isDragging.value = false
    const file = e.dataTransfer.files[0]
    if (file) setFile(file)
}

const onFileChange = (e) => {
    const file = e.target.files[0]
    if (file) setFile(file)
}

const setFile = (file) => {
    const allowed = ['application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        'application/vnd.ms-excel', 'text/csv']
    if (!allowed.includes(file.type) && !file.name.match(/\.(xlsx|xls|csv)$/i)) {
        alert('Chỉ chấp nhận file .xlsx, .xls hoặc .csv')
        return
    }
    if (file.size > 5 * 1024 * 1024) {
        alert('File quá lớn, tối đa 5MB')
        return
    }
    selectedFile.value = file
}

const clearFile = () => {
    selectedFile.value = null
    if (fileInput.value) fileInput.value.value = ''
}

const fileInput = ref(null)

// ---- Parse file ----
const parseFile = async () => {
    isParsing.value = true
    try {
        const data = await readFile(selectedFile.value)
        const workbook = XLSX.read(data, { type: 'array' })
        const sheet = workbook.Sheets[workbook.SheetNames[0]]
        const json = XLSX.utils.sheet_to_json(sheet, { defval: '' })

        rows.value = json.map(r => {
            const row = {
                bookId: r['BookId'] || r['bookId'] || '',
                barcode: r['Barcode'] || r['barcode'] || '',
                warehouseId: r['WarehouseId'] || r['warehouseId'] || '',
                shelfLocation: r['ShelfLocation'] || r['shelfLocation'] || '',
                bookCondition: r['BookCondition'] || r['bookCondition'] || '',
                purchaseDate: r['PurchaseDate'] || r['purchaseDate'] || '',
                isReferenceOnly: ['true', '1', 'có', 'yes'].includes(
                    String(r['IsReferenceOnly'] || r['isReferenceOnly'] || '').toLowerCase()
                ),
                notes: r['Notes'] || r['notes'] || '',
                errors: {},
                warnings: {}
            }
            validateRow(row)
            return row
        })

        step.value = 2
    } catch (err) {
        alert('Không đọc được file: ' + err.message)
    } finally {
        isParsing.value = false
    }
}

const readFile = (file) => new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = (e) => resolve(new Uint8Array(e.target.result))
    reader.onerror = reject
    reader.readAsArrayBuffer(file)
})

// ---- Validate ----
const validateRow = (row) => {
    row.errors = {}
    row.warnings = {}

    if (!row.bookId || isNaN(Number(row.bookId)))
        row.errors.bookId = 'BookId không hợp lệ'

    if (!row.warehouseId || isNaN(Number(row.warehouseId)))
        row.errors.warehouseId = 'WarehouseId không hợp lệ'
    else if (!warehouses.value.find(w => w.warehouseId == row.warehouseId))
        row.errors.warehouseId = `Không tìm thấy kho id=${row.warehouseId}`

    if (row.barcode) {
        const duplicate = rows.value.filter(r => r !== row && r.barcode === row.barcode)
        if (duplicate.length > 0)
            row.errors.barcode = 'Barcode trùng trong file'
    } else {
        row.warnings.barcode = 'Sẽ tự sinh barcode'
    }

    if (row.purchaseDate && isNaN(Date.parse(row.purchaseDate)))
        row.errors.purchaseDate = 'Ngày không hợp lệ'
}

// ---- Import ----
const doImport = async () => {
    isImporting.value = true
    try {
        const payload = rows.value.map(r => ({
            bookId: Number(r.bookId),
            barcode: r.barcode || null,
            warehouseId: Number(r.warehouseId),
            shelfLocation: r.shelfLocation || null,
            bookCondition: r.bookCondition || null,
            purchaseDate: r.purchaseDate || null,
            isReferenceOnly: r.isReferenceOnly,
            notes: r.notes || null
        }))

        const res = await api.post('/BookCopies/import', payload)
        if (res.status === 200) {
            importResult.value = res.data
            step.value = 3
        }
    } catch (err) {
        alert('Import thất bại: ' + (err.response?.data?.message || err.message))
    } finally {
        isImporting.value = false
    }
}

// ---- Template download ----
const downloadTemplate = () => {
    const ws = XLSX.utils.aoa_to_sheet([
        ['BookId(*)', 'Barcode', 'WarehouseId(*)', 'ShelfLocation', 'BookCondition', 'PurchaseDate', 'IsReferenceOnly', 'Notes'],
        [5, '', 1, 'A1-03', 'Mới', '2025-04-14', false, ''],
        [5, '', 1, 'A1-03', 'Tốt', '', false, 'Ghi chú mẫu']
    ])
    ws['!cols'] = [10, 22, 12, 14, 14, 14, 16, 20].map(w => ({ wch: w }))
    const wb = XLSX.utils.book_new()
    XLSX.utils.book_append_sheet(wb, ws, 'BookCopies')
    XLSX.writeFile(wb, 'mau-nhap-ban-sao.xlsx')
}

// ---- Helpers ----
const warehouseName = (id) => {
    const w = warehouses.value.find(w => w.warehouseId == id)
    return w ? w.name : null
}

const formatFileSize = (bytes) => {
    if (bytes < 1024) return bytes + ' B'
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
    return (bytes / 1024 / 1024).toFixed(1) + ' MB'
}

const rowClass = (row) => {
    if (hasError(row)) return 'row-error'
    if (hasWarning(row)) return 'row-warning'
    return 'row-ok'
}

const rowStatusClass = (row) => {
    if (hasError(row)) return 'status-error'
    if (hasWarning(row)) return 'status-warning'
    return 'status-ok'
}

const rowStatusLabel = (row) => {
    if (hasError(row)) return '✕ Lỗi'
    if (hasWarning(row)) return '⚠ Cảnh báo'
    return '✓ Hợp lệ'
}

const resetAll = () => {
    step.value = 1
    rows.value = []
    selectedFile.value = null
    importResult.value = { success: 0 }
}

const backToList = () => {
  router.push({
    name: "booksManage",
  })
}
</script>

<style lang="scss" scoped>
.import-page {
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
    gap: 16px;
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

.btn-download {
    padding: 9px 16px;
    background: #fff;
    border: 1.5px solid #435ebe;
    color: #435ebe;
    border-radius: 8px;
    font-size: 14px;
    font-weight: 500;
    cursor: pointer;
    white-space: nowrap;

    &:hover {
        background: #e8eaf6;
    }
    .icon_excel {
        margin-bottom: 2px;
        margin-right: 2px;
    }
}

// Steps
.steps {
    display: flex;
    align-items: center;
    gap: 0;
}

.step {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 13px;
    font-weight: 500;
    color: #aaa;

    &.active {
        color: #435ebe;
    }

    &.done {
        color: #2e7d32;
    }
}

.step-dot {
    width: 28px;
    height: 28px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 12px;
    font-weight: 700;
    background: #e0e0e0;
    color: #888;

    .active & {
        background: #435ebe;
        color: #fff;
    }

    .done & {
        background: #2e7d32;
        color: #fff;
    }
}

.step-line {
    flex: 1;
    height: 2px;
    background: #e0e0e0;
    margin: 0 8px;

    &.done {
        background: #2e7d32;
    }
}

// Card
.card {
    background: #fff;
    border-radius: 12px;
    border: 1px solid #e0e0e0;
    overflow: hidden;
}

.card-footer {
    padding: 16px 20px;
    border-top: 1px solid #f0f0f0;
    display: flex;
    justify-content: flex-end;
    gap: 10px;
}

// Drop zone
.drop-zone {
    margin: 20px;
    border: 2px dashed #d0d0e0;
    border-radius: 12px;
    padding: 48px 24px;
    text-align: center;
    cursor: pointer;
    transition: all 0.2s;

    &:hover,
    &.dragging {
        border-color: #435ebe;
        background: #f5f6ff;
    }

    &.has-file {
        border-style: solid;
        border-color: #435ebe;
        background: #f5f6ff;
    }
}

.drop-icon {
    font-size: 40px;
    margin-bottom: 12px;
}

.drop-text {
    font-size: 15px;
    font-weight: 500;
    color: #333;
}

.drop-hint {
    font-size: 13px;
    color: #999;
    margin-top: 6px;
}

.link {
    color: #435ebe;
    text-decoration: underline;
}

.file-selected {
    display: flex;
    align-items: center;
    gap: 16px;
    justify-content: center;
}

.file-icon {
    font-size: 36px;
}

.file-name {
    font-size: 15px;
    font-weight: 600;
    color: #1a1a2e;
    text-align: left;
}

.file-size {
    font-size: 12px;
    color: #888;
    text-align: left;
}

.btn-remove {
    background: #ffebee;
    border: none;
    color: #c62828;
    border-radius: 50%;
    width: 28px;
    height: 28px;
    cursor: pointer;
    font-size: 13px;
    font-weight: 700;

    &:hover {
        background: #ffcdd2;
    }
}

// Summary
.summary-bar {
    display: flex;
    gap: 12px;
    padding: 16px 20px;
    border-bottom: 1px solid #f0f0f0;
    flex-wrap: wrap;
}

.summary-item {
    padding: 6px 14px;
    border-radius: 99px;
    font-size: 13px;

    strong {
        font-size: 16px;
        margin-right: 4px;
    }

    &.summary-total {
        background: #f0f0f5;
        color: #333;
    }

    &.summary-ok {
        background: #e8f5e9;
        color: #2e7d32;
    }

    &.summary-error {
        background: #ffebee;
        color: #c62828;
    }

    &.summary-warning {
        background: #fff8e1;
        color: #f57f17;
    }
}

.error-notice {
    margin: 0 20px 0;
    padding: 12px 16px;
    background: #ffebee;
    border-left: 3px solid #e53935;
    border-radius: 0 8px 8px 0;
    font-size: 13px;
    color: #c62828;
    margin-top: 16px;
}

// Table
.table-wrapper {
    overflow-x: auto;
    margin: 16px 20px;
    border-radius: 8px;
    border: 1px solid #e0e0e0;
}

.preview-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 13px;

    thead tr {
        background: #f5f5f5;
    }

    th {
        padding: 9px 12px;
        text-align: left;
        font-weight: 600;
        color: #555;
        white-space: nowrap;
        border-bottom: 1px solid #e0e0e0;
    }

    td {
        padding: 9px 12px;
        border-bottom: 1px solid #f0f0f0;
        vertical-align: top;
    }

    .row-ok {
        background: #fff;
    }

    .row-error {
        background: #fff8f8;
    }

    .row-warning {
        background: #fffdf0;
    }

    .row-num {
        color: #aaa;
        font-size: 12px;
    }

    .cell-error {
        color: #c62828;
    }

    .cell-warning {
        color: #e65100;
    }

    .cell-error-msg {
        font-size: 11px;
        color: #e53935;
        margin-top: 2px;
    }

    .cell-warn-msg {
        font-size: 11px;
        color: #fb8c00;
        margin-top: 2px;
    }

    .auto-gen {
        color: #aaa;
        font-style: italic;
        font-size: 12px;
    }

    .required {
        color: #e53935;
    }
}

.row-status {
    display: inline-block;
    padding: 2px 10px;
    border-radius: 99px;
    font-size: 12px;
    font-weight: 600;
    white-space: nowrap;

    &.status-ok {
        background: #e8f5e9;
        color: #2e7d32;
    }

    &.status-error {
        background: #ffebee;
        color: #c62828;
    }

    &.status-warning {
        background: #fff8e1;
        color: #f57f17;
    }
}

// Buttons
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
    text-decoration: none;

    &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
    }

    &.btn-primary {
        background: #435ebe;
        color: #fff;
        width: fit-content;

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
}

// Result
.result-card {
    text-align: center;
    padding: 60px 24px;
}

.result-icon {
    font-size: 56px;
    margin-bottom: 16px;
    color: #2e7d32 ;
}

.result-title {
    font-size: 22px;
    font-weight: 800;
    margin: 0 0 8px;
}

.result-desc {
    font-size: 15px;
    color: #555;
    margin: 0 0 28px;
}

.result-actions {
    display: flex;
    gap: 12px;
    justify-content: center;
}
</style>