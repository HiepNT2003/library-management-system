<template>
  <div class="static-page">
    <div class="page-hero hero-purple">
      <div class="hero-icon"><Icon icon="ic:round-contact-support" width="58" height="58" /></div>
      <h1>Liên hệ & Giờ mở cửa</h1>
      <p>Thông tin liên hệ và lịch làm việc của thư viện</p>
    </div>

    <div class="content-grid">
      <div class="info-row">
        <!-- Giờ mở cửa -->
        <div class="content-card">
          <h2 class="card-title">🕐 Giờ mở cửa</h2>
          <div class="hours-list">
            <div class="normal-item">
              <span class="day">Thứ 2 — Thứ 6</span>
              <div class="room">
                <span class="pos">Phòng đọc Tầng 6:</span>
                <span class="time open">08:00 — 17:45</span>
              </div>
              <div class="room">
                <span class="pos">Phòng đọc Tầng 7, Phòng Mượn:</span>
                <div class="times">
                  <span class="time open">08:00 — 11:30</span>
                  <span class="time open">13:15 — 16:45</span>
                </div>
              </div>
            </div>
            <div class="hours-item">
              <span class="day">Thứ 7, Chủ nhật</span>
              <span class="time closed">Nghỉ</span>
            </div>
            <div class="hours-item">
              <span class="day">Ngày lễ</span>
              <span class="time closed">Nghỉ</span>
            </div>
          </div>
          <div class="hours-note">
            💡 Ngoài các ngày nghỉ lễ Quốc gia theo quy định, khi có lịch nghỉ nội dịch của nhà
            trường, Trung tâm thông tin - Thư viện sẽ thông báo đến bạn đọc trên website, trên trang
            fanpage và tại mỗi phòng của thư viện, bạn đọc cần lưu ý để nắm được lịch phục vụ.
          </div>
        </div>

        <!-- Thông tin liên hệ -->
        <div class="content-card">
          <h2 class="card-title">📌 Thông tin liên hệ</h2>
          <div class="contact-list">
            <div class="contact-item">
              <span class="contact-icon">📍</span>
              <div>
                <div class="contact-label">Địa chỉ</div>
                <div class="contact-value">Số 3 Cầu Giấy, Láng Thượng, Đống Đa, Hà Nội</div>
              </div>
            </div>
            <div class="contact-item">
              <span class="contact-icon">📞</span>
              <div>
                <div class="contact-label">Điện thoại</div>
                <div class="contact-value">
                  <a href="tel:02437668271">(024) 3766 8271</a>
                </div>
              </div>
            </div>
            <div class="contact-item">
              <span class="contact-icon">📧</span>
              <div>
                <div class="contact-label">Email</div>
                <div class="contact-value">
                  <a href="mailto:thuvien@utc.edu.vn">thuvien@utc.edu.vn</a>
                </div>
              </div>
            </div>
            <div class="contact-item">
              <span class="contact-icon">🌐</span>
              <div>
                <div class="contact-label">Website</div>
                <div class="contact-value">
                  <a href="https://utc.edu.vn" target="_blank">utc.edu.vn</a>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Phòng đọc -->
      <div class="content-card">
        <h2 class="card-title">🏢 Các phòng đọc</h2>
        <div class="rooms-grid">
          <div class="room-item">
            <div class="room-name">📚 Phòng Mượn</div>
            <div class="room-desc">
              Tầng 7 nhà A8 — Mượn và trả sách, giáo trình, tài liệu tham khảo về nhà.
            </div>
          </div>
          <div class="room-item">
            <div class="room-name">🔇 Phòng đọc sách tiếng Việt</div>
            <div class="room-desc">
              Tầng 7 nhà A8 — Đọc tài liệu tiếng Việt, nghiên cứu, 150 chỗ ngồi.
            </div>
          </div>
          <div class="room-item">
            <div class="room-name">📰 Phòng Nghiệp vụ - Tổng hợp</div>
            <div class="room-desc">
              Tầng 7 nhà A8 — Ngoài các nhiệm vụ chuyên môn, Phòng Nghiệp vụ - Tổng hợp còn là nơi
              tiếp nhận luận văn, luận án, đề tài nghiên cứu khoa học các cấp, sách và tài liệu biếu
              tặng v.v...
            </div>
          </div>
          <div class="room-item">
            <div class="room-name">🎓 Phòng đọc ngoại văn, Luận văn</div>
            <div class="room-desc">
              Tầng 6 — Phòng đọc tài liệu tiếng nước ngoài, luận văn, luận án, báo và tạp chí: 180
              chỗ ngồi.
            </div>
          </div>
        </div>
      </div>

      <!-- Map -->
      <div class="content-card">
        <h2 class="card-title">🗺️ Bản đồ</h2>
        <div class="map-wrapper">
          <iframe
            src="https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3724.113241057333!2d105.80084557613085!3d21.02815448062114!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x3135ab424a50fff9%3A0xbe3a7f3670c0a45f!2sUniversity%20of%20Transport%20and%20Communications!5e0!3m2!1sen!2s!4v1777975327512!5m2!1sen!2s"
            width="100%"
            height="360"
            style="border: 0"
            allowfullscreen=""
            loading="lazy"
            referrerpolicy="no-referrer-when-downgrade"
          ></iframe>
        </div>
        <div class="map-address">
          📍 Số 3 Cầu Giấy, Láng Thượng, Đống Đa, Hà Nội
          <a
            href="https://maps.google.com/?q=Trường+Đại+học+Giao+thông+Vận+tải,+Hà+Nội"
            target="_blank"
            class="map-link"
            >Mở trong Google Maps →</a
          >
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { Icon } from "@iconify/vue"
</script>

<style lang="scss" scoped>
.static-page {
  display: flex;
  flex-direction: column;
  gap: 24px;
  font-family: "Segoe UI", sans-serif;
  color: #1a1a2e;
}

.page-hero {
  border-radius: 16px;
  padding: 40px;
  text-align: center;
  color: #fff;
  &.hero-purple {
    background: linear-gradient(135deg, #4a148c, #7b1fa2);
  }
  .hero-icon {
    font-size: 52px;
    margin-bottom: 12px;
  }
  h1 {
    font-size: 26px;
    font-weight: 800;
    margin: 0 0 8px;
  }
  p {
    font-size: 15px;
    opacity: 0.9;
    margin: 0;
  }
}

.content-grid {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.content-card {
  background: #fff;
  border-radius: 14px;
  border: 1px solid #e0e0e0;
  padding: 24px;
}
.card-title {
  font-size: 18px;
  font-weight: 800;
  margin: 0 0 16px;
}

.info-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
  @media (max-width: 700px) {
    grid-template-columns: 1fr;
  }
}

// Hours
.hours-list {
  display: flex;
  flex-direction: column;
  gap: 0;
  margin-bottom: 14px;
}
.normal-item {
  display: flex;
  justify-content: space-between;
  align-items: start;
  flex-direction: column;
  align-items: center;
  padding: 12px 0;
  border-bottom: 1px solid #f5f5f5;
  > * {
    width: 100%;
  }
  .room {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 6px 3px 4px;
    font-size: 14px;
    .times {
      display: flex;
      gap: 4px;
    }
  }
  &:last-child {
    border-bottom: none;
  }
}
.hours-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 0;
  border-bottom: 1px solid #f5f5f5;
  &:last-child {
    border-bottom: none;
  }
}
.day {
  font-size: 14px;
  color: #444;
  font-weight: 500;
}
.time {
  font-size: 14px;
  font-weight: 700;
  padding: 3px 12px;
  border-radius: 99px;
  &.open {
    background: #e8f5e9;
    color: #2e7d32;
  }
  &.closed {
    background: #f5f5f5;
    color: #999;
  }
}
.hours-note {
  font-size: 13px;
  color: #888;
  padding: 10px 12px;
  background: #fff8e1;
  border-radius: 8px;
  line-height: 1.5;
}

// Contact
.contact-list {
  display: flex;
  flex-direction: column;
  gap: 14px;
}
.contact-item {
  display: flex;
  gap: 14px;
  align-items: flex-start;
}
.contact-icon {
  font-size: 20px;
  flex-shrink: 0;
  margin-top: 1px;
}
.contact-label {
  font-size: 12px;
  color: #aaa;
  margin-bottom: 3px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}
.contact-value {
  font-size: 14px;
  font-weight: 500;
  color: #333;
  a {
    color: #3949ab;
    text-decoration: none;
    &:hover {
      text-decoration: underline;
    }
  }
}

// Rooms
.rooms-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
  @media (max-width: 600px) {
    grid-template-columns: 1fr;
  }
}
.room-item {
  padding: 14px 16px;
  background: #f9f9f9;
  border-radius: 10px;
  border: 1px solid #e8e8e8;
}
.room-name {
  font-size: 14px;
  font-weight: 700;
  margin-bottom: 6px;
}
.room-desc {
  font-size: 13px;
  color: #666;
  line-height: 1.5;
}

// Map
.map-wrapper {
  border-radius: 10px;
  overflow: hidden;
  border: 1px solid #e0e0e0;
  margin-bottom: 12px;
}
.map-address {
  font-size: 14px;
  color: #555;
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}
.map-link {
  color: #3949ab;
  text-decoration: none;
  font-weight: 500;
  font-size: 13px;
  &:hover {
    text-decoration: underline;
  }
}
</style>