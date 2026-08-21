<template>
  <div class="qr-scanner">

    <!-- Toggle button -->
    <button class="btn-toggle-camera" :class="{ active: isOpen }" @click="toggleCamera">
      {{ isOpen ? "📷 Tắt camera" : "📷 Quét QR" }}
    </button>

    <!-- Scanner -->
    <div class="scanner-wrapper" v-if="isOpen">
      <div class="scanner-box">
        <!-- Video feed -->
        <video ref="videoRef" class="scanner-video" autoplay muted playsinline></video>

        <!-- Scan overlay -->
        <div class="scan-overlay">
          <div class="scan-frame">
            <div class="scan-corner tl"></div>
            <div class="scan-corner tr"></div>
            <div class="scan-corner bl"></div>
            <div class="scan-corner br"></div>
            <div class="scan-line" :class="{ scanning: isScanning }"></div>
          </div>
        </div>

        <!-- Close button -->
        <button class="btn-close-scanner" @click="stopCamera">✕</button>
      </div>

      <!-- Camera selector -->
      <div class="camera-select-row" v-if="cameras.length > 1">
        <label>Camera:</label>
        <select v-model="selectedCamera" @change="switchCamera" class="camera-select">
          <option v-for="cam in cameras" :key="cam.deviceId" :value="cam.deviceId">
            {{ cam.label || `Camera ${cameras.indexOf(cam) + 1}` }}
          </option>
        </select>
      </div>

      <!-- Status -->
      <div class="scanner-status">
        <div v-if="scanError" class="status-error">⚠️ {{ scanError }}</div>
        <div v-else-if="isScanning" class="status-scanning">🔍 Đang quét...</div>
        <div v-else class="status-hint">Hướng camera vào mã QR</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onUnmounted } from "vue"
import { BrowserMultiFormatReader } from "@zxing/browser"
import { NotFoundException } from "@zxing/library"

const emit = defineEmits(["scanned"])

const isOpen = ref(false)
const isScanning = ref(false)
const scanError = ref("")
const videoRef = ref(null)
const cameras = ref([])
const selectedCamera = ref("")

let reader = null
let controls = null

const toggleCamera = async () => {
  if (isOpen.value) {
    stopCamera()
  } else {
    await startCamera()
  }
}

const startCamera = async () => {
  scanError.value = ""
  isOpen.value = true

  try {
    reader = new BrowserMultiFormatReader()

    // Lấy danh sách camera
    const devices = await navigator.mediaDevices.enumerateDevices()
    const videoDevices = devices.filter((d) => d.kind === "videoinput")
    cameras.value = videoDevices
    if (devices.length === 0) {
      scanError.value = "Không tìm thấy camera"
      isOpen.value = false
      return
    }

    // Ưu tiên camera sau (back camera trên mobile)
    const backCamera = videoDevices.find(
      (d) =>
        (d.label || "").toLowerCase().includes("back") ||
        (d.label || "").toLowerCase().includes("rear") ||
        (d.label || "").toLowerCase().includes("environment")
    )
    selectedCamera.value = backCamera?.deviceId || devices[0].deviceId

    await startDecode()
  } catch (err) {
    if (err.name === "NotAllowedError") {
      scanError.value = "Trình duyệt chưa được cấp quyền truy cập camera"
    } else {
      scanError.value = "Không thể mở camera: " + err.message
    }
    isOpen.value = false
  }
}

const startDecode = async () => {
  if (!reader || !videoRef.value) return
  isScanning.value = true
  scanError.value = ""

  try {
    controls = await reader.decodeFromVideoDevice(
      selectedCamera.value || undefined,
      videoRef.value,
      (result, error) => {
        if (result) {
          const text = result.getText()
          // Phát ra kết quả và dừng camera
          emit("scanned", text)
          stopCamera()
        }
        if (error && !(error instanceof NotFoundException)) {
          console.warn("QR scan error:", error)
        }
      }
    )
  } catch (err) {
    scanError.value = "Lỗi khi khởi động camera"
    isScanning.value = false
  }
}

const switchCamera = async () => {
  if (controls) {
    controls.stop()
    controls = null
  }
  await startDecode()
}

const stopCamera = () => {
  if (controls) {
    controls.stop()
    controls = null
  }

  if (videoRef.value?.srcObject) {
    const tracks = videoRef.value.srcObject.getTracks()
    tracks.forEach((track) => track.stop())
    videoRef.value.srcObject = null
  }

  isScanning.value = false
  isOpen.value = false
  scanError.value = ""
}

onUnmounted(() => {
  stopCamera()
})
</script>

<style lang="scss" scoped>
.qr-scanner {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-top: 8px;
}

.btn-toggle-camera {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 8px 14px;
  background: #fff;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  color: #555;
  transition: all 0.15s;
  width: fit-content;
  &:hover,
  &.active {
    border-color: #3949ab;
    color: #3949ab;
    background: #f0f4ff;
  }
}

.scanner-wrapper {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.scanner-box {
  position: relative;
  width: 100%;
  max-width: 340px;
  border-radius: 12px;
  overflow: hidden;
  background: #000;
  aspect-ratio: 1;
}

.scanner-video {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

// Overlay
.scan-overlay {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.4);
}

.scan-frame {
  position: relative;
  width: 200px;
  height: 200px;
  background: transparent;
}

// Corner decorations
.scan-corner {
  position: absolute;
  width: 24px;
  height: 24px;
  border-color: #fff;
  border-style: solid;
  &.tl {
    top: 0;
    left: 0;
    border-width: 3px 0 0 3px;
    border-radius: 4px 0 0 0;
  }
  &.tr {
    top: 0;
    right: 0;
    border-width: 3px 3px 0 0;
    border-radius: 0 4px 0 0;
  }
  &.bl {
    bottom: 0;
    left: 0;
    border-width: 0 0 3px 3px;
    border-radius: 0 0 0 4px;
  }
  &.br {
    bottom: 0;
    right: 0;
    border-width: 0 3px 3px 0;
    border-radius: 0 0 4px 0;
  }
}

// Scan line animation
.scan-line {
  position: absolute;
  left: 4px;
  right: 4px;
  height: 2px;
  background: linear-gradient(90deg, transparent, #3949ab, #90caf9, #3949ab, transparent);
  top: 0;
  border-radius: 99px;
  &.scanning {
    animation: scanMove 2s linear infinite;
  }
}

@keyframes scanMove {
  0% {
    top: 4px;
    opacity: 1;
  }
  95% {
    top: calc(100% - 4px);
    opacity: 1;
  }
  100% {
    top: calc(100% - 4px);
    opacity: 0;
  }
}

// Close button
.btn-close-scanner {
  position: absolute;
  top: 8px;
  right: 8px;
  width: 28px;
  height: 28px;
  background: rgba(0, 0, 0, 0.6);
  border: none;
  border-radius: 50%;
  color: #fff;
  font-size: 13px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  &:hover {
    background: rgba(0, 0, 0, 0.85);
  }
}

// Camera select
.camera-select-row {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: #555;
}
.camera-select {
  flex: 1;
  padding: 6px 10px;
  border: 1.5px solid #e0e0e0;
  border-radius: 8px;
  font-size: 13px;
  outline: none;
  background: #fff;
  &:focus {
    border-color: #3949ab;
  }
}

// Status
.scanner-status {
  font-size: 13px;
}
.status-scanning {
  color: #3949ab;
  font-weight: 500;
}
.status-hint {
  color: #888;
}
.status-error {
  color: #c62828;
  font-weight: 500;
}
</style>