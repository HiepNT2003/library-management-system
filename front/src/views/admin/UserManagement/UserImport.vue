<template>
  <div class="import-page">
    <!-- Header -->
    <div class="page-header">
      <div>
        <h1 class="page-title">Nhập người dùng hàng loạt</h1>
        <p class="page-desc">Import sinh viên hoặc giảng viên/cán bộ từ file Excel</p>
      </div>
    </div>

    <!-- Steps -->
    <div class="steps">
      <div class="step" :class="{ active: step >= 1, done: step > 1 }">
        <div class="step-dot">{{ step > 1 ? "✓" : "1" }}</div>
        <span>Chọn loại & Upload</span>
      </div>
      <div class="step-line" :class="{ done: step > 1 }"></div>
      <div class="step" :class="{ active: step >= 2, done: step > 2 }">
        <div class="step-dot">{{ step > 2 ? "✓" : "2" }}</div>
        <span>Kiểm tra dữ liệu</span>
      </div>
      <div class="step-line" :class="{ done: step > 2 }"></div>
      <div class="step" :class="{ active: step >= 3 }">
        <div class="step-dot">3</div>
        <span>Hoàn tất</span>
      </div>
    </div>

    <!-- Step 1: Chọn loại + Upload -->
    <div v-if="step === 1" class="card">
      <div class="card-body">
        <!-- Role selector -->
        <div class="role-selector">
          <button
            class="role-btn"
            :class="{ active: selectedRole === 'Student' }"
            @click="selectedRole = 'Student'"
          >
            🎓 Sinh viên
          </button>
          <button
            class="role-btn"
            :class="{ active: selectedRole === 'Staff' }"
            @click="selectedRole = 'Staff'"
          >
            👨‍🏫 Giảng viên/Cán bộ
          </button>
        </div>

        <div class="form-group" v-if="selectedRole === 'Staff'" style="margin-bottom: 16px">
          <label class="field-label">Ngày hết hạn thẻ (áp dụng cho tất cả)</label>
          <input type="date" v-model="batchExpiredDate" class="date-input" />
          <span class="field-hint">Để trống nếu không giới hạn</span>
        </div>

        <div class="auto-expire-hint" v-if="selectedRole === 'Student'">
          💡 Ngày hết hạn thẻ sinh viên sẽ tự động tính là
          <strong>31/12 năm tốt nghiệp dự kiến</strong>
          (năm nhập học + 5 năm). Sinh viên không có năm nhập học sẽ không có ngày hết hạn.
        </div>

        <!-- Download template -->
        <div class="template-hint">
          <span
            >Tải file mẫu cho
            {{ selectedRole === "Student" ? "sinh viên" : "giảng viên/cán bộ" }}:</span
          >
          <button class="btn-link" @click="downloadTemplate">
            <Icon class="icon_excel" icon="file-icons:microsoft-excel" width="14" height="14" /> Tải
            file mẫu .xlsx
          </button>
        </div>

        <!-- Drop zone -->
        <div
          class="drop-zone"
          :class="{ dragging: isDragging, 'has-file': selectedFile }"
          @dragover.prevent="isDragging = true"
          @dragleave="isDragging = false"
          @drop.prevent="onDrop"
          @click="$refs.fileInput.click()"
        >
          <input
            ref="fileInput"
            type="file"
            accept=".xlsx,.xls,.csv"
            hidden
            @change="onFileChange"
          />
          <div v-if="!selectedFile" class="drop-content">
            <div class="drop-icon">📂</div>
            <div class="drop-text">
              Kéo thả file vào đây hoặc <span class="link">chọn file</span>
            </div>
            <div class="drop-hint">Hỗ trợ .xlsx, .xls, .csv — tối đa 5MB</div>
          </div>
          <div v-else class="file-selected">
            <div class="file-icon"><Icon class="icon_excel" icon="file-icons:microsoft-excel" width="34" height="34" /></div>
            <div>
              <div class="file-name">{{ selectedFile.name }}</div>
              <div class="file-size">{{ formatFileSize(selectedFile.size) }}</div>
            </div>
            <button class="btn-remove" @click.stop="clearFile">✕</button>
          </div>
        </div>
      </div>
      <div class="card-footer">
        <button class="btn btn-outline" @click="handleBack"><Icon icon="lsicon:arrow-left-filled" width="16" height="16" />Quay lại</button>
        <button class="btn btn-primary" :disabled="!selectedFile || isParsing" @click="parseFile">
          {{ isParsing ? "Đang đọc file..." : "Tiếp tục" }}
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
      </div>

      <div v-if="errorCount > 0" class="error-notice">
        ⚠️ Còn <strong>{{ errorCount }}</strong> dòng lỗi. Vui lòng sửa file và upload lại.
      </div>

      <!-- Table Student -->
      <div class="table-wrapper" v-if="selectedRole === 'Student'">
        <table class="preview-table">
          <thead>
            <tr>
              <th>#</th>
              <th>Họ tên *</th>
              <th>Email *</th>
              <th>Mã SV *</th>
              <th>Lớp</th>
              <th>Khoa</th>
              <th>Ngành</th>
              <th>Khóa</th>
              <th>Năm nhập học *</th>
              <th>SĐT</th>
              <th>Trạng thái</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(row, idx) in rows" :key="idx" :class="rowClass(row)">
              <td class="row-num">{{ idx + 1 }}</td>
              <td :class="{ 'cell-error': row.errors.fullName }">
                {{ row.fullName }}
                <div v-if="row.errors.fullName" class="cell-error-msg">
                  {{ row.errors.fullName }}
                </div>
              </td>
              <td :class="{ 'cell-error': row.errors.email }">
                {{ row.email }}
                <div v-if="row.errors.email" class="cell-error-msg">{{ row.errors.email }}</div>
              </td>
              <td :class="{ 'cell-error': row.errors.code }">
                <span class="code-text">{{ row.code }}</span>
                <div v-if="row.errors.code" class="cell-error-msg">{{ row.errors.code }}</div>
              </td>
              <td>{{ row.class || "—" }}</td>
              <td>{{ row.faculty || "—" }}</td>
              <td>{{ row.major || "—" }}</td>
              <td>{{ row.term || "—" }}</td>
              <td :class="{ 'cell-error': row.errors.admissionYear }">
                {{ row.admissionYear || "—" }}
                <div v-if="row.errors.admissionYear" class="cell-error-msg">
                  {{ row.errors.admissionYear }}
                </div>
              </td>
              <td>{{ row.phoneNumber || "—" }}</td>
              <td>
                <span class="row-status" :class="rowStatusClass(row)">{{
                  rowStatusLabel(row)
                }}</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Table Staff -->
      <div class="table-wrapper" v-else>
        <table class="preview-table">
          <thead>
            <tr>
              <th>#</th>
              <th>Họ tên *</th>
              <th>Email *</th>
              <th>Mã CB *</th>
              <th>Chức vụ</th>
              <th>Phòng ban</th>
              <th>SĐT</th>
              <th>Trạng thái</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(row, idx) in rows" :key="idx" :class="rowClass(row)">
              <td class="row-num">{{ idx + 1 }}</td>
              <td :class="{ 'cell-error': row.errors.fullName }">
                {{ row.fullName }}
                <div v-if="row.errors.fullName" class="cell-error-msg">
                  {{ row.errors.fullName }}
                </div>
              </td>
              <td :class="{ 'cell-error': row.errors.email }">
                {{ row.email }}
                <div v-if="row.errors.email" class="cell-error-msg">{{ row.errors.email }}</div>
              </td>
              <td :class="{ 'cell-error': row.errors.code }">
                <span class="code-text">{{ row.code }}</span>
                <div v-if="row.errors.code" class="cell-error-msg">{{ row.errors.code }}</div>
              </td>
              <td>{{ row.position || "—" }}</td>
              <td>{{ row.department || "—" }}</td>
              <td>{{ row.phoneNumber || "—" }}</td>
              <td>
                <span class="row-status" :class="rowStatusClass(row)">{{
                  rowStatusLabel(row)
                }}</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="card-footer">
        <button
          class="btn btn-outline"
          @click="
            step = 1;
            clearFile()
          "
        >
          <Icon icon="lsicon:arrow-left-filled" width="14" height="14" />Quay lại
        </button>
        <button class="btn btn-primary" :disabled="errorCount > 0 || isImporting" @click="doImport">
          {{ isImporting ? "Đang import..." : `Import ${validCount} người dùng` }}
        </button>
      </div>
    </div>

    <!-- Step 3: Result -->
    <div v-if="step === 3" class="card result-card">
      <div class="result-icon" v-if="importResult.failed > 0">⚠️</div>
      <div class="result-icon" v-else>
        <Icon class="icon_success" icon="charm:circle-tick" width="56" height="56" />
      </div>
      <h2 class="result-title">
        {{ importResult.failed > 0 ? "Import hoàn tất (có lỗi)" : "Import thành công" }}
      </h2>
      <div class="result-stats">
        <div class="result-stat result-ok">
          <strong>{{ importResult.success }}</strong> thành công
        </div>
        <div class="result-stat result-fail" v-if="importResult.failed > 0">
          <strong>{{ importResult.failed }}</strong> thất bại
        </div>
      </div>

      <!-- Failed rows -->
      <div v-if="importResult.failed > 0" class="failed-list">
        <div class="failed-title">Danh sách lỗi:</div>
        <div
          v-for="r in importResult.results?.filter((x) => !x.success)"
          :key="r.email"
          class="failed-item"
        >
          <span class="failed-name">{{ r.fullName }}</span>
          <span class="failed-email">{{ r.email }}</span>
          <span class="failed-error">{{ r.error }}</span>
        </div>
      </div>

      <div class="result-hint" v-if="selectedRole === 'Student'">
        💡 Mật khẩu mặc định: <strong>mã sinh viên + @Utc1</strong> (VD: <code>20123456@Utc1</code>)
      </div>
      <div class="result-hint" v-else>
        💡 Mật khẩu mặc định: <strong>mã cán bộ + @Utc1</strong> (VD: <code>CB001234@Utc1</code>)
      </div>

      <div class="result-actions">
        <button class="btn btn-outline" @click="resetAll">Import thêm</button>
        <button class="btn btn-primary" @click="handleBack">Xem danh sách</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from "vue"
import { useRoute, useRouter } from "vue-router"
import * as XLSX from "xlsx"
import api from "../../../services/api"
import { Icon } from "@iconify/vue"

const router = useRouter()
const route = useRoute()

const step = ref(1)
const selectedRole = ref(route.query.role || "Student")
const selectedFile = ref(null)
const isDragging = ref(false)
const isParsing = ref(false)
const isImporting = ref(false)
const rows = ref([])
const importResult = ref({ success: 0, failed: 0, results: [] })
const fileInput = ref(null)
const batchExpiredDate = ref("")

// ---- Computed ----
const validCount = computed(() => rows.value.filter((r) => !hasError(r)).length)
const errorCount = computed(() => rows.value.filter((r) => hasError(r)).length)
const hasError = (row) => Object.keys(row.errors).length > 0

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
  if (file.size > 5 * 1024 * 1024) {
    alert("File quá lớn, tối đa 5MB")
    return
  }
  selectedFile.value = file
}
const clearFile = () => {
  selectedFile.value = null
  rows.value = []
  if (fileInput.value) fileInput.value.value = ""
}

// ---- Parse file ----
const parseFile = async () => {
  isParsing.value = true
  try {
    const data = await readFile(selectedFile.value)
    const workbook = XLSX.read(data, { type: "array" })
    const sheet = workbook.Sheets[workbook.SheetNames[0]]
    const json = XLSX.utils.sheet_to_json(sheet, { defval: "" })

    rows.value = json.map((r) => {
      const row =
        selectedRole.value === "Student"
          ? {
              fullName: String(r["HoTen(*)"] || r["FullName"] || r["Họ tên"] || "").trim(),
              email: String(r["Email(*)"] || r["email"] || "").trim(),
              code: String(r["MaSV(*)"] || r["StudentCode"] || r["Mã SV"] || "").trim(),
              phoneNumber: String(r["SoDienThoai"] || r["PhoneNumber"] || r["SĐT"] || "").trim(),
              class: String(r["Lop"] || r["Class"] || r["Lớp"] || "").trim(),
              faculty: String(r["Khoa"] || r["Faculty"] || "").trim(),
              major: String(r["Nganh"] || r["Major"] || r["Ngành"] || "").trim(),
              term: String(r["Khoa_hoc"] || r["Term"] || r["Khóa"] || "").trim(),
              admissionYear: r["NamNhapHoc"] || r["AdmissionYear"] || r["Năm nhập học"] || null,
              errors: {},
            }
          : {
              fullName: String(r["HoTen(*)"] || r["FullName"] || r["Họ tên"] || "").trim(),
              email: String(r["Email(*)"] || r["email"] || "").trim(),
              code: String(r["MaCB(*)"] || r["StaffCode"] || r["Mã CB"] || "").trim(),
              phoneNumber: String(r["SoDienThoai"] || r["PhoneNumber"] || r["SĐT"] || "").trim(),
              position: String(r["ChucVu"] || r["Position"] || r["Chức vụ"] || "").trim(),
              department: String(r["PhongBan"] || r["Department"] || r["Phòng ban"] || "").trim(),
              errors: {},
            }
      validateRow(row)
      return row
    })

    step.value = 2
  } catch (err) {
    alert("Không đọc được file: " + err.message)
  } finally {
    isParsing.value = false
  }
}

const readFile = (file) =>
  new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = (e) => resolve(new Uint8Array(e.target.result))
    reader.onerror = reject
    reader.readAsArrayBuffer(file)
  })

// ---- Validate ----
const validateRow = (row) => {
  row.errors = {}
  if (!row.fullName) row.errors.fullName = "Họ tên không được để trống"
  if (!row.email) {
    row.errors.email = "Email không được để trống"
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(row.email)) {
    row.errors.email = "Email không hợp lệ"
  }
  if (!row.code) {
    row.errors.code =
      selectedRole.value === "Student"
        ? "Mã sinh viên không được để trống"
        : "Mã cán bộ không được để trống"
  }
  if (selectedRole.value === "Student") {
    if (!row.admissionYear) {
      row.errors.admissionYear = "Năm nhập học không được để trống"
    } else if (
      isNaN(Number(row.admissionYear)) ||
      Number(row.admissionYear) < 2000 ||
      Number(row.admissionYear) > new Date().getFullYear()
    ) {
      row.errors.admissionYear = "Năm nhập học không hợp lệ"
    }
  }
  // Check email trùng trong file
  const dupEmail = rows.value.filter((r) => r !== row && r.email === row.email && row.email)
  if (dupEmail.length > 0) row.errors.email = "Email trùng trong file"

  // Check mã trùng trong file
  const dupCode = rows.value.filter((r) => r !== row && r.code === row.code && row.code)
  if (dupCode.length > 0)
    row.errors.code =
      selectedRole.value === "Student" ? "Mã SV trùng trong file" : "Mã CB trùng trong file"
}

// ---- Import ----
const doImport = async () => {
  isImporting.value = true
  try {
    const payload = {
      role: selectedRole.value,
      batchExpiredDate: batchExpiredDate.value || null,
      users: rows.value.map((r) => ({
        fullName: r.fullName,
        email: r.email,
        code: r.code,
        phoneNumber: r.phoneNumber || null,
        class: r.class || null,
        faculty: r.faculty || null,
        major: r.major || null,
        term: r.term || null,
        admissionYear: r.admissionYear ? Number(r.admissionYear) : null,
        position: r.position || null,
        department: r.department || null,
      })),
    }
    const res = await api.post("/Users/import", payload)
    if (res.status === 200) {
      importResult.value = res.data
      step.value = 3
    }
  } catch (err) {
    alert("Import thất bại: " + (err.response?.data?.message || err.message))
  } finally {
    isImporting.value = false
  }
}

// ---- Template download ----
const downloadTemplate = () => {
  let headers, example1, example2
  if (selectedRole.value === "Student") {
    headers = [
      "HoTen(*)",
      "Email(*)",
      "MaSV(*)",
      "Lop",
      "Khoa",
      "Nganh",
      "Khoa_hoc",
      "NamNhapHoc(*)",
      "SoDienThoai",
    ]
    example1 = [
      "Nguyễn Văn A",
      "nguyenvana@lms.utc.edu.vn",
      "201234001",
      "KTXD62-DH1",
      "Khoa Công trình",
      "Kỹ thuật xây dựng",
      "K62",
      2020,
      "0912345678",
    ]
    example2 = [
      "Trần Thị B",
      "tranthib@lms.utc.edu.vn",
      "201234002",
      "CNTT61-DH2",
      "Khoa CNTT",
      "Kỹ thuật phần mềm",
      "K61",
      2021,
      "",
    ]
  } else {
    headers = ["HoTen(*)", "Email(*)", "MaCB(*)", "ChucVu", "PhongBan", "SoDienThoai"]
    example1 = [
      "Nguyễn Văn C",
      "nguyenvanc@utc.edu.vn",
      "CB001234",
      "Giảng viên",
      "Khoa Công trình",
      "0912345678",
    ]
    example2 = ["Lê Thị D", "lethid@utc.edu.vn", "CB001235", "Trưởng khoa", "Khoa CNTT", ""]
  }

  const ws = XLSX.utils.aoa_to_sheet([headers, example1, example2])
  ws["!cols"] = headers.map(() => ({ wch: 20 }))
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, selectedRole.value === "Student" ? "SinhVien" : "GiangVien")
  XLSX.writeFile(
    wb,
    `mau-import-${selectedRole.value === "Student" ? "sinh-vien" : "giang-vien"}.xlsx`
  )
}

// ---- Helpers ----
const rowClass = (row) => (hasError(row) ? "row-error" : "row-ok")
const rowStatusClass = (row) => (hasError(row) ? "status-error" : "status-ok")
const rowStatusLabel = (row) => (hasError(row) ? "✕ Lỗi" : "✓ Hợp lệ")
const formatFileSize = (bytes) => {
  if (bytes < 1024) return bytes + " B"
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + " KB"
  return (bytes / 1024 / 1024).toFixed(1) + " MB"
}
const resetAll = () => {
  step.value = 1
  rows.value = []
  selectedFile.value = null
  importResult.value = { success: 0, failed: 0, results: [] }
}
const handleBack = () => {
  router.push({
    name: "userManagement",
  })
}
</script>

<style lang="scss" scoped>
.import-page {
  display: flex;
  flex-direction: column;
  gap: 20px;
  font-family: "Segoe UI", sans-serif;
  color: #1a1a2e;
  padding: 16px 24px;
  background: #ffffff;
  border-radius: 12px;
  margin-top: 16px;
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

// Steps
.steps {
  display: flex;
  align-items: center;
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
.card-body {
  padding: 20px;
}
.card-footer {
  padding: 16px 20px;
  border-top: 1px solid #f0f0f0;
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}

// Role selector
.role-selector {
  display: flex;
  gap: 12px;
  margin-bottom: 20px;
}
.role-btn {
  flex: 1;
  padding: 14px;
  border-radius: 10px;
  border: 2px solid #e0e0e0;
  background: #fff;
  font-size: 15px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s;
  color: #555;
  &:hover {
    border-color: #435ebe;
    color: #435ebe;
  }
  &.active {
    border-color: #435ebe;
    background: #e8eaf6;
    color: #435ebe;
  }
}

.template-hint {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 13px;
  color: #666;
  margin-bottom: 16px;
}
.btn-link {
  background: none;
  border: none;
  color: #435ebe;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  text-decoration: underline;
  padding: 0;
  .icon_excel {
    margin-bottom: 3px;
  }
}

// Drop zone
.drop-zone {
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
  color: #2e7d32;
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
}
.error-notice {
  margin: 16px 20px 0;
  padding: 12px 16px;
  background: #ffebee;
  border-left: 3px solid #e53935;
  border-radius: 0 8px 8px 0;
  font-size: 13px;
  color: #c62828;
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
  .row-num {
    color: #aaa;
    font-size: 12px;
  }
  .cell-error {
    color: #c62828;
  }
  .cell-error-msg {
    font-size: 11px;
    color: #e53935;
    margin-top: 2px;
  }
  .code-text {
    font-family: monospace;
    font-size: 13px;
    color: #435ebe;
    font-weight: 600;
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
}

.field-label {
  font-size: 13px;
  font-weight: 600;
  color: #444;
  display: block;
  margin-bottom: 6px;
}
.date-input {
  padding: 8px 12px;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  font-size: 14px;
  outline: none;
  font-family: inherit;
  width: 200px;
  &:focus {
    border-color: #3949ab;
  }
}
.field-hint {
  font-size: 12px;
  color: #999;
  margin-top: 4px;
  display: block;
}
.auto-expire-hint {
  font-size: 13px;
  color: #555;
  background: #e8f5e9;
  border-radius: 8px;
  padding: 10px 14px;
  margin-bottom: 16px;
  border-left: 3px solid #43a047;
}

// Result
.result-card {
  text-align: center;
  padding: 48px 24px;
}
.result-icon {
  font-size: 56px;
  margin-bottom: 16px;
  color: #2e7d32 ;
}
.result-title {
  font-size: 22px;
  font-weight: 800;
  margin: 0 0 16px;
}
.result-stats {
  display: flex;
  gap: 16px;
  justify-content: center;
  margin-bottom: 20px;
}
.result-stat {
  padding: 8px 20px;
  border-radius: 99px;
  font-size: 14px;
  strong {
    font-size: 18px;
    margin-right: 4px;
  }
  &.result-ok {
    background: #e8f5e9;
    color: #2e7d32;
  }
  &.result-fail {
    background: #ffebee;
    color: #c62828;
  }
}
.failed-list {
  text-align: left;
  max-width: 560px;
  margin: 0 auto 20px;
  background: #fff8f8;
  border-radius: 8px;
  border: 1px solid #ffcdd2;
  overflow: hidden;
}
.failed-title {
  padding: 10px 16px;
  font-size: 13px;
  font-weight: 700;
  color: #c62828;
  border-bottom: 1px solid #ffcdd2;
}
.failed-item {
  display: flex;
  gap: 12px;
  padding: 8px 16px;
  border-bottom: 1px solid #ffeef0;
  font-size: 13px;
  align-items: baseline;
  flex-wrap: wrap;
  &:last-child {
    border-bottom: none;
  }
}
.failed-name {
  font-weight: 600;
  color: #333;
}
.failed-email {
  color: #888;
  font-size: 12px;
}
.failed-error {
  color: #e53935;
  font-size: 12px;
  margin-left: auto;
}

.result-hint {
  font-size: 13px;
  color: #555;
  background: #fff8e1;
  border-radius: 8px;
  padding: 10px 16px;
  max-width: 480px;
  margin: 0 auto 24px;
}
.result-actions {
  display: flex;
  gap: 12px;
  justify-content: center;
}
</style>