<template>
  <div class="ebook-reader" :class="{ fullscreen: isFullscreen }">
    <!-- Top bar -->
    <div class="top-bar">
      <button class="btn-back" @click="handleBack">
        <Icon icon="line-md:arrow-left" width="16" height="16" />
        Trở lại
      </button>

      <div class="book-title-bar">{{ bookTitle }}</div>

      <div class="top-actions">
        <div class="progress-pill" v-if="totalPages > 0">
          <div class="progress-fill" :style="{ width: progressPercent + '%' }"></div>
          <span class="progress-text">{{ progressPercent }}%</span>
        </div>

        <div class="save-status" :class="saveStatusClass">{{ saveStatusText }}</div>

        <button
          v-if="showChangeFullscreen"
          class="btn-icon"
          @click="toggleFullscreen"
          :title="isFullscreen ? 'Thoát toàn màn' : 'Toàn màn hình'"
        >
          <Icon :icon="isFullscreen ? 'mdi:fullscreen-exit' : 'mdi:fullscreen'" width="20" />
        </button>
      </div>
    </div>

    <!-- Main reader -->
    <div class="reader-body">
      <!-- Sidebar -->
      <div class="reader-sidebar" v-if="showSidebar">
        <div class="sidebar-tabs">
          <button :class="{ active: sidebarTab === 'notes' }" @click="sidebarTab = 'notes'">
            Ghi chú
          </button>
          <button
            :class="{ active: sidebarTab === 'highlights' }"
            @click="sidebarTab = 'highlights'"
          >
            Đánh dấu
            <span class="tab-count" v-if="highlightsList.length > 0">{{
              highlightsList.length
            }}</span>
          </button>
        </div>

        <div v-if="sidebarTab === 'notes'" class="sidebar-content">
          <textarea
            v-model="notes"
            placeholder="Ghi chú của bạn..."
            class="notes-textarea"
            @input="onNotesChange"
          ></textarea>
        </div>

        <div v-if="sidebarTab === 'highlights'" class="sidebar-content">
          <div v-if="highlightsList.length === 0" class="sidebar-empty">
            Chọn văn bản trong trang để đánh dấu
          </div>
          <div
            v-for="(h, idx) in highlightsList"
            :key="idx"
            class="highlight-item"
            @click="goToPage(h.page)"
          >
            <div class="highlight-page">Trang {{ h.page }}</div>
            <div class="highlight-text">{{ h.text }}</div>
            <button class="btn-remove-highlight" @click.stop="removeHighlight(idx)">✕</button>
          </div>
        </div>
      </div>

      <!-- Canvas area -->
      <div class="canvas-area" ref="canvasArea">
        <!-- Highlight toolbar -->
        <div
          class="highlight-toolbar"
          v-if="showHighlightToolbar"
          :style="{ top: toolbarPos.y + 'px', left: toolbarPos.x + 'px' }"
        >
          <button class="hl-btn" @click="addHighlight">✏️ Đánh dấu</button>
          <button class="hl-btn" @click="cancelHighlight">✕</button>
        </div>

        <!-- Canvas wrapper -->
        <div class="canvas-wrapper" ref="canvasWrapper">
          <div v-if="isRendering" class="rendering-overlay">
            <div class="spinner"></div>
          </div>
          <canvas ref="canvasRef" class="pdf-canvas"></canvas>
          <div ref="textLayerRef" class="textLayer"></div>
        </div>
      </div>
    </div>

    <!-- Bottom toolbar -->
    <div class="toolbar">
      <button
        v-if="saveProgress"
        class="btn-sidebar"
        :class="{ active: showSidebar }"
        @click="showSidebar = !showSidebar"
      >
        <Icon icon="mdi:note-text-outline" width="18" />
      </button>
      <div v-else></div>

      <div class="nav-controls">
        <button class="btn-nav" @click="goToPage(1)" :disabled="currentPage === 1">
          <Icon icon="mdi:skip-previous" width="18" />
        </button>
        <button class="btn-nav" @click="prevPage" :disabled="currentPage === 1">
          <Icon icon="mdi:chevron-left" width="20" />
        </button>
        <div class="page-input-wrap">
          <input
            type="number"
            v-model.number="pageInput"
            class="page-input"
            :min="1"
            :max="totalPages"
            @keydown.enter="jumpToPage"
            @blur="jumpToPage"
          />
          <span class="page-total">/ {{ totalPages }}</span>
        </div>
        <button class="btn-nav" @click="nextPage" :disabled="currentPage === totalPages">
          <Icon icon="mdi:chevron-right" width="20" />
        </button>
        <button
          class="btn-nav"
          @click="goToPage(totalPages)"
          :disabled="currentPage === totalPages"
        >
          <Icon icon="mdi:skip-next" width="18" />
        </button>
      </div>

      <div class="zoom-controls">
        <button class="btn-zoom" @click="zoomOut" :disabled="scale <= 0.5">−</button>
        <span class="zoom-label">{{ Math.round(scale * 100) }}%</span>
        <button class="btn-zoom" @click="zoomIn" :disabled="scale >= 3">+</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch, shallowRef, nextTick } from "vue"
import * as pdfjsLib from "pdfjs-dist/legacy/build/pdf"
import { renderTextLayer } from "pdfjs-dist/legacy/build/pdf"
import pdfWorker from "pdfjs-dist/legacy/build/pdf.worker?url"
import { Icon } from "@iconify/vue"
import { useAuthStore } from "@/stores/auth"
import api from "../../services/api"

pdfjsLib.GlobalWorkerOptions.workerSrc = pdfWorker

const props = defineProps({
  fileUrl: { type: String, required: true },
  bookId: { type: Number, default: null },
  bookTitle: { type: String, default: "" },
  initialPage: { type: Number, default: 1 },
  saveProgress: { type: Boolean, default: true },
  defaultFullscreen: { type: Boolean, default: false },
  showChangeFullscreen: { type: Boolean, default: true },
})

const emit = defineEmits(["on:closePDF"])

const authStore = useAuthStore()

// Refs
const pdfDoc = shallowRef(null)
const canvasRef = ref(null)
const textLayerRef = ref(null)
const canvasArea = ref(null)
const canvasWrapper = ref(null)

// State
const currentPage = ref(props.initialPage)
const pageInput = ref(props.initialPage)
const totalPages = ref(0)
const scale = ref(1.5)
const isRendering = ref(false)
const isFullscreen = ref(props.defaultFullscreen)
const showSidebar = ref(false)
const sidebarTab = ref("notes")
const notes = ref("")
const highlightsList = ref([])

// Highlight toolbar
const showHighlightToolbar = ref(false)
const selectedText = ref("")
const toolbarPos = ref({ x: 0, y: 0 })

// Save
const saveStatus = ref("idle")
let saveTimer = null
let notesTimer = null

// Computed
const progressPercent = computed(() =>
  totalPages.value ? Math.round((currentPage.value / totalPages.value) * 100) : 0
)
const saveStatusText = computed(
  () =>
    ({ idle: "", saving: "Đang lưu...", saved: "✓ Đã lưu", error: "⚠ Lỗi lưu" }[saveStatus.value] ??
    "")
)
const saveStatusClass = computed(
  () =>
    ({ saving: "status-saving", saved: "status-saved", error: "status-error" }[saveStatus.value] ??
    "")
)

// Lifecycle
onMounted(async () => {
  await loadProgress()
  await loadPDF()
  document.addEventListener("keydown", handleKeyDown)
  document.addEventListener("mouseup", handleTextSelect)
  document.addEventListener("mousedown", handleMouseDown)
})

onUnmounted(() => {
  document.removeEventListener("keydown", handleKeyDown)
  document.removeEventListener("mouseup", handleTextSelect)
  document.removeEventListener("mousedown", handleMouseDown)
  clearTimeout(saveTimer)
  clearTimeout(notesTimer)
})

// Load PDF
const loadPDF = async () => {
  if (!props.fileUrl) return
  authStore.setIsLoadingApi(true)
  try {
    pdfDoc.value = await pdfjsLib.getDocument({
      url: new URL(props.fileUrl, window.location.href).toString(),
    }).promise
    totalPages.value = pdfDoc.value.numPages
    await renderPage(currentPage.value)
  } catch (err) {
    console.error("PDF load error:", err)
  } finally {
    authStore.setIsLoadingApi(false)
  }
}

// Render page
const renderPage = async (pageNum) => {
  if (!pdfDoc.value || !canvasRef.value) return
  isRendering.value = true
  try {
    const page = await pdfDoc.value.getPage(pageNum)
    const viewport = page.getViewport({ scale: scale.value })

    const canvas = canvasRef.value
    canvas.height = viewport.height
    canvas.width = viewport.width
    await page.render({ canvasContext: canvas.getContext("2d"), viewport }).promise

    await nextTick()
    await renderTextLayerForPage(page, viewport)
  } finally {
    isRendering.value = false
  }
}

const renderTextLayerForPage = async (page, viewport) => {
  const textLayer = textLayerRef.value
  if (!textLayer || !props.saveProgress) return

  textLayer.innerHTML    = ''
  textLayer.style.width  = viewport.width  + 'px'
  textLayer.style.height = viewport.height + 'px'

  textLayer.style.setProperty('--scale-factor', scale.value)

  const textContent = await page.getTextContent()
  renderTextLayer({
    textContentSource: textContent,
    container:         textLayer,
    viewport
  })
}

// Navigation
const goToPage = (page) => {
  if (page < 1 || page > totalPages.value) return
  currentPage.value = page
  pageInput.value = page
}
const nextPage = () => goToPage(currentPage.value + 1)
const prevPage = () => goToPage(currentPage.value - 1)
const jumpToPage = () => {
  const p = Number(pageInput.value)
  if (p >= 1 && p <= totalPages.value) goToPage(p)
  else pageInput.value = currentPage.value
}

// Zoom
const zoomIn = () => {
  scale.value = Math.min(3, +(scale.value + 0.25).toFixed(2))
  renderPage(currentPage.value)
}
const zoomOut = () => {
  scale.value = Math.max(0.5, +(scale.value - 0.25).toFixed(2))
  renderPage(currentPage.value)
}

// Keyboard
const handleKeyDown = (e) => {
  if (["INPUT", "TEXTAREA"].includes(e.target.tagName)) return
  if (e.key === "ArrowRight" || e.key === "ArrowDown") nextPage()
  if (e.key === "ArrowLeft" || e.key === "ArrowUp") prevPage()
  if (e.key === "f" || e.key === "F") toggleFullscreen()
  if (e.key === "Escape") {
    showHighlightToolbar.value = false
    window.getSelection()?.removeAllRanges()
  }
}

// Fullscreen
const toggleFullscreen = () => {
  isFullscreen.value = !isFullscreen.value
}

// Text selection & highlight
const handleTextSelect = (e) => {
  if (e.target.closest(".highlight-toolbar")) return

  const selection = window.getSelection()
  const text = selection?.toString().trim()

  if (!text || text.length < 2) {
    showHighlightToolbar.value = false
    return
  }

  const textLayer = textLayerRef.value
  if (!textLayer) return

  const range = selection.getRangeAt(0)
  if (!textLayer.contains(range.commonAncestorContainer)) {
    showHighlightToolbar.value = false
    return
  }

  const rect = range.getBoundingClientRect()
  const areaRect = canvasArea.value?.getBoundingClientRect() ?? { left: 0, top: 0 }
  const scrollTop = canvasArea.value?.scrollTop ?? 0
  const scrollLeft = canvasArea.value?.scrollLeft ?? 0

  selectedText.value = text
  toolbarPos.value = {
    x: rect.left - areaRect.left + scrollLeft + rect.width / 2 - 60,
    y: rect.top - areaRect.top + scrollTop - 48,
  }
  showHighlightToolbar.value = true
}

const handleMouseDown = (e) => {
  if (!e.target.closest(".highlight-toolbar") && !e.target.closest(".textLayer")) {
    showHighlightToolbar.value = false
  }
}

const addHighlight = () => {
  if (!selectedText.value) return
  const exists = highlightsList.value.some(
    (h) => h.page === currentPage.value && h.text === selectedText.value
  )
  if (!exists) {
    highlightsList.value.push({ page: currentPage.value, text: selectedText.value })
    scheduleSave()
    showSidebar.value = true
    sidebarTab.value = "highlights"
  }
  showHighlightToolbar.value = false
  window.getSelection()?.removeAllRanges()
}

const cancelHighlight = () => {
  showHighlightToolbar.value = false
  window.getSelection()?.removeAllRanges()
}

const removeHighlight = (idx) => {
  highlightsList.value.splice(idx, 1)
  scheduleSave()
}

const onNotesChange = () => {
  clearTimeout(notesTimer)
  notesTimer = setTimeout(scheduleSave, 800)
}

// Progress
const loadProgress = async () => {
  if (!props.saveProgress || !props.bookId || !authStore.user) return
  try {
    const res = await api.get(`/ReadingProgress/${props.bookId}`)
    if (res.status === 200) {
      if (res.data.currentPage > 1) {
        currentPage.value = res.data.currentPage
        pageInput.value = res.data.currentPage
      }
      notes.value = res.data.notes || ""
      try {
        highlightsList.value = JSON.parse(res.data.highlights || "[]")
      } catch {
        highlightsList.value = []
      }
    }
  } catch {}
}

const scheduleSave = () => {
  if (!props.saveProgress || !props.bookId || !authStore.user) return
  clearTimeout(saveTimer)
  saveStatus.value = "saving"
  saveTimer = setTimeout(callSaveProgress, 1500)
}

const callSaveProgress = async () => {
  if (!props.saveProgress || !props.bookId || !authStore.user) return
  try {
    await api.post("/ReadingProgress", {
      bookId: props.bookId,
      currentPage: currentPage.value,
      percentRead: progressPercent.value,
      highlights: JSON.stringify(highlightsList.value),
      notes: notes.value || null,
    })
    saveStatus.value = "saved"
    setTimeout(() => {
      saveStatus.value = "idle"
    }, 2000)
  } catch {
    saveStatus.value = "error"
  }
}

watch(currentPage, (page) => {
  renderPage(page)
  scheduleSave()
})

const handleBack = async () => {
  clearTimeout(saveTimer)
  await callSaveProgress()
  emit("on:closePDF")
}
</script>

<style lang="scss" scoped>
.ebook-reader {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background: #1a1a2e;
  color: #fff;
  font-family: "Segoe UI", sans-serif;
  &.fullscreen {
    position: fixed;
    inset: 0;
    z-index: 9999;
  }
}

.top-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 16px;
  background: rgba(255, 255, 255, 0.05);
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  gap: 12px;
  flex-shrink: 0;
}
.btn-back {
  display: flex;
  align-items: center;
  gap: 6px;
  background: none;
  border: none;
  color: #ccc;
  cursor: pointer;
  padding: 6px 10px;
  border-radius: 6px;
  font-size: 13px;
  white-space: nowrap;
  flex-shrink: 0;
  &:hover {
    background: rgba(255, 255, 255, 0.1);
    color: #fff;
  }
}
.book-title-bar {
  flex: 1;
  text-align: center;
  font-size: 14px;
  font-weight: 600;
  color: #e0e0e0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.top-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-shrink: 0;
}
.progress-pill {
  position: relative;
  width: 80px;
  height: 20px;
  background: rgba(255, 255, 255, 0.15);
  border-radius: 99px;
  overflow: hidden;
}
.progress-fill {
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  background: #3949ab;
  transition: width 0.3s;
}
.progress-text {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 11px;
  font-weight: 700;
  color: #fff;
  z-index: 1;
}
.save-status {
  font-size: 12px;
  color: #666;
  min-width: 70px;
  text-align: center;
  &.status-saving {
    color: #90caf9;
  }
  &.status-saved {
    color: #a5d6a7;
  }
  &.status-error {
    color: #ef9a9a;
  }
}
.btn-icon {
  background: none;
  border: none;
  color: #aaa;
  cursor: pointer;
  padding: 6px;
  border-radius: 6px;
  &:hover {
    background: rgba(255, 255, 255, 0.1);
    color: #fff;
  }
}

.reader-body {
  flex: 1;
  display: flex;
  overflow: hidden;
}

.reader-sidebar {
  width: 260px;
  flex-shrink: 0;
  background: rgba(255, 255, 255, 0.04);
  border-right: 1px solid rgba(255, 255, 255, 0.1);
  display: flex;
  flex-direction: column;
}
.sidebar-tabs {
  display: flex;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  flex-shrink: 0;
  button {
    flex: 1;
    padding: 10px 8px;
    background: none;
    border: none;
    color: #888;
    font-size: 13px;
    cursor: pointer;
    transition: all 0.15s;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 6px;
    &:hover {
      color: #fff;
    }
    &.active {
      color: #fff;
      border-bottom: 2px solid #3949ab;
    }
  }
}
.tab-count {
  background: #3949ab;
  color: #fff;
  font-size: 10px;
  font-weight: 700;
  padding: 1px 6px;
  border-radius: 99px;
}
.sidebar-content {
  flex: 1;
  overflow-y: auto;
  padding: 12px;
}
.sidebar-empty {
  color: #555;
  font-size: 13px;
  text-align: center;
  padding: 20px 0;
  line-height: 1.6;
}
.notes-textarea {
  width: 100%;
  min-height: 200px;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 8px;
  color: #e0e0e0;
  font-size: 13px;
  padding: 10px;
  resize: none;
  outline: none;
  font-family: inherit;
  line-height: 1.6;
  box-sizing: border-box;
  &:focus {
    border-color: #3949ab;
  }
}
.highlight-item {
  position: relative;
  padding: 8px 28px 8px 10px;
  background: rgba(255, 255, 255, 0.05);
  border-radius: 8px;
  margin-bottom: 8px;
  cursor: pointer;
  transition: background 0.15s;
  &:hover {
    background: rgba(255, 255, 255, 0.09);
  }
}
.highlight-page {
  font-size: 11px;
  color: #3949ab;
  font-weight: 600;
  margin-bottom: 4px;
}
.highlight-text {
  font-size: 13px;
  color: #e0e0e0;
  line-height: 1.4;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
.btn-remove-highlight {
  position: absolute;
  top: 8px;
  right: 8px;
  background: none;
  border: none;
  color: #555;
  cursor: pointer;
  font-size: 12px;
  &:hover {
    color: #ef9a9a;
  }
}

.canvas-area {
  flex: 1;
  overflow: auto;
  display: flex;
  justify-content: center;
  align-items: flex-start;
  padding: 24px;
  background: #2a2a40;
  position: relative;
}

.highlight-toolbar {
  position: absolute;
  z-index: 100;
  background: #1a1a2e;
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 8px;
  padding: 4px;
  display: flex;
  gap: 4px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.5);
}
.hl-btn {
  background: none;
  border: none;
  color: #e0e0e0;
  cursor: pointer;
  padding: 5px 12px;
  border-radius: 6px;
  font-size: 13px;
  transition: background 0.15s;
  &:first-child:hover {
    background: #3949ab;
  }
  &:last-child:hover {
    background: rgba(255, 255, 255, 0.1);
  }
}

.canvas-wrapper {
  position: relative;
  display: inline-block;
}

.rendering-overlay {
  position: absolute;
  inset: 0;
  background: rgba(26, 26, 46, 0.7);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 20;
}
.spinner {
  width: 36px;
  height: 36px;
  border: 3px solid rgba(255, 255, 255, 0.1);
  border-top-color: #3949ab;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}
@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.pdf-canvas {
  display: block;
  border-radius: 4px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
}

// Text layer — quan trọng: position absolute đè lên canvas
.textLayer {
  position: absolute;
  top: 0;
  left: 0;
  overflow: hidden;
  line-height: 1;
  :deep(span) {
    color: transparent;
    position: absolute;
    white-space: pre;
    cursor: text;
    transform-origin: 0% 0%;
    &::selection {
      background: rgba(57, 73, 171, 0.35);
      color: transparent;
    }
  }
  :deep(br) {
    display: none;
  }
}

.toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 16px;
  background: rgba(255, 255, 255, 0.05);
  border-top: 1px solid rgba(255, 255, 255, 0.1);
  flex-shrink: 0;
  gap: 12px;
}
.btn-sidebar {
  background: none;
  border: none;
  color: #aaa;
  cursor: pointer;
  padding: 7px 10px;
  border-radius: 8px;
  transition: all 0.15s;
  &:hover,
  &.active {
    background: rgba(255, 255, 255, 0.1);
    color: #fff;
  }
}
.nav-controls {
  display: flex;
  align-items: center;
  gap: 6px;
}
.btn-nav {
  background: none;
  border: none;
  color: #aaa;
  cursor: pointer;
  padding: 6px 8px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  transition: all 0.15s;
  &:hover:not(:disabled) {
    background: rgba(255, 255, 255, 0.1);
    color: #fff;
  }
  &:disabled {
    opacity: 0.3;
    cursor: not-allowed;
  }
}
.page-input-wrap {
  display: flex;
  align-items: center;
  gap: 6px;
}
.page-input {
  width: 52px;
  text-align: center;
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 6px;
  color: #fff;
  padding: 5px 6px;
  font-size: 14px;
  outline: none;
  &:focus {
    border-color: #3949ab;
  }
  &::-webkit-inner-spin-button,
  &::-webkit-outer-spin-button {
    -webkit-appearance: none;
  }
}
.page-total {
  font-size: 14px;
  color: #888;
  white-space: nowrap;
}
.zoom-controls {
  display: flex;
  align-items: center;
  gap: 6px;
}
.btn-zoom {
  width: 28px;
  height: 28px;
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 6px;
  color: #fff;
  cursor: pointer;
  font-size: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  &:hover:not(:disabled) {
    background: rgba(255, 255, 255, 0.2);
  }
  &:disabled {
    opacity: 0.3;
    cursor: not-allowed;
  }
}
.zoom-label {
  font-size: 13px;
  color: #888;
  min-width: 42px;
  text-align: center;
}
</style>