<template>
  <div class="static-page">
    <div class="page-hero hero-orange">
      <div class="hero-icon">📋</div>
      <h1>Nội quy mượn trả sách</h1>
      <p>Vui lòng đọc kỹ nội quy trước khi sử dụng dịch vụ thư viện</p>
    </div>

    <div class="content-grid">
      <!-- Quy định mượn -->
      <div class="content-card">
        <h2 class="card-title">📚 Quy định mượn sách</h2>
        <div class="rule-list">
          <div class="rule-item">
            <div class="rule-num">1</div>
            <div>
              <div class="rule-title">Đối tượng được mượn</div>
              <div class="rule-desc">
                Tài liệu được mượn về nhà bao gồm giáo trình, tài liệu tham khảo. Đối tượng được
                mượn là người học có thẻ đa năng và cán bộ giảng viên của trường.
              </div>
            </div>
          </div>
          <div class="rule-item">
            <div class="rule-num">2</div>
            <div>
              <div class="rule-title">Số lượng mượn</div>
              <div class="rule-desc">
                Tại một thời điểm, số lượng cuốn tài liệu đang ghi mượn của một bạn đọc có thẻ đa
                năng không được quá <strong>10 giáo trình</strong>,
                <strong>02 tài liệu tham khảo</strong>; với cán bộ, công nhân viên, giảng viên trong
                trường là không quá <strong>07 giáo trình</strong>,
                <strong>03 tài liệu tham khảo</strong>.
              </div>
            </div>
          </div>
          <div class="rule-item">
            <div class="rule-num">3</div>
            <div>
              <div class="rule-title">Thời hạn mượn</div>
              <div class="rule-desc">
                Đối với <strong> giáo trình</strong> thời gian mượn <strong>1 học kì</strong> (150
                ngày). Bạn đọc phải hoàn thành thủ tục trả sách cho thư viện sau môn thi cuối cùng
                của học kì đó trong vòng 10 ngày. Đối với <strong>sách tham khảo</strong>, thời gian
                mượn <strong>15 ngày</strong> kể từ ngày mượn .
              </div>
            </div>
          </div>
          <div class="rule-item">
            <div class="rule-num">4</div>
            <div>
              <div class="rule-title">Đặt mượn trước</div>
              <div class="rule-desc">
                Sinh viên có thể đặt mượn trực tuyến. Sách được giữ tối đa
                <strong>7 ngày</strong> sau khi được duyệt. Sau thời hạn này yêu cầu sẽ tự động bị
                huỷ.
              </div>
            </div>
          </div>
          <div class="rule-item">
            <div class="rule-num">5</div>
            <div>
              <div class="rule-title">Quy trình</div>
              <div class="rule-desc">
                Sinh viên có thể đặt mượn trực tuyến. Sách được giữ tối đa
                <strong>7 ngày</strong> sau khi được duyệt. Sau thời hạn này yêu cầu sẽ tự động bị
                huỷ.
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Quy định trả -->
      <div class="content-card">
        <h2 class="card-title"><Icon icon="mingcute:refresh-3-line" width="20" height="20" /> Quy định trả sách</h2>
        <div class="rule-list">
          <div class="rule-item">
            <div class="rule-num">1</div>
            <div>
              <div class="rule-title">Trả đúng hạn</div>
              <div class="rule-desc">
                Trả sách trước hoặc đúng ngày trên phiếu mượn. Có thể gia hạn online nếu chưa quá
                hạn và không có phiếu phạt.
              </div>
            </div>
          </div>
          <div class="rule-item">
            <div class="rule-num">2</div>
            <div>
              <div class="rule-title">Tình trạng sách khi trả</div>
              <div class="rule-desc">
                Trả sách trong tình trạng nguyên vẹn như khi mượn. Không được tẩy xóa, xé rách, viết
                vào sách.
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Phạt -->
      <div class="content-card">
        <h2 class="card-title">💰 Quy định phạt</h2>
        <div class="fine-table">
          <div class="fine-header">
            <span>Loại vi phạm</span>
            <span>Mức phạt</span>
          </div>
          <div class="fine-row">
            <span>Trả sách quá hạn</span>
            <span class="fine-amount">2.000đ/ngày/cuốn</span>
          </div>
          <div class="fine-row">
            <span>Sách bị hư hỏng</span>
            <span class="fine-amount">Theo % thiệt hại × giá sách</span>
          </div>
          <div class="fine-row">
            <span>Làm mất sách</span>
            <span class="fine-amount">100% giá sách</span>
          </div>
          <div class="fine-row fine-row-note">
            <span colspan="2"
              >⚠️ Có phiếu phạt quá <strong>7 ngày</strong> chưa thanh toán sẽ không được mượn/đặt
              sách.</span
            >
          </div>
        </div>
      </div>

      <!-- Quy định khác -->
      <div class="content-card">
        <h2 class="card-title">⚠️ Quy định khác</h2>
        <div class="rule-list">
          <div class="rule-item">
            <div class="rule-num icon">🚫</div>
            <div>
              <div class="rule-title">Không được phép</div>
              <div class="rule-desc">
                Mang đồ ăn, thức uống vào phòng đọc. Gây ồn ào ảnh hưởng đến người khác. Sử dụng
                điện thoại to tiếng.
              </div>
            </div>
          </div>
          <div class="rule-item">
            <div class="rule-num icon">📵</div>
            <div>
              <div class="rule-title">Tắt/để im lặng điện thoại</div>
              <div class="rule-desc">
                Điện thoại phải để chế độ im lặng hoặc rung trong toàn bộ khu vực thư viện.
              </div>
            </div>
          </div>
          <div class="rule-item">
            <div class="rule-num icon">🎒</div>
            <div>
              <div class="rule-title">Gửi đồ</div>
              <div class="rule-desc">
                Sinh viên gửi cặp túi tại tủ đồ trước khi vào phòng đọc. Thư viện không chịu trách
                nhiệm về tài sản cá nhân.
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { Icon } from '@iconify/vue';

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
  &.hero-orange {
    background: linear-gradient(135deg, #e65100, #fb8c00);
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
  display: flex;
  gap: 8px;
  align-items: center;
}

.rule-list {
  display: flex;
  flex-direction: column;
  gap: 14px;
}
.rule-item {
  display: flex;
  gap: 14px;
  align-items: flex-start;
}
.rule-num {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: #3949ab;
  color: #fff;
  font-size: 13px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  margin-top: 2px;
  &.icon {
    background: #f5f5f5;
    font-size: 16px;
  }
}
.rule-title {
  font-size: 14px;
  font-weight: 700;
  margin-bottom: 4px;
}
.rule-desc {
  font-size: 13px;
  color: #555;
  line-height: 1.6;
}

.fine-table {
  border-radius: 10px;
  overflow: hidden;
  border: 1px solid #e0e0e0;
}
.fine-header {
  display: flex;
  justify-content: space-between;
  padding: 10px 16px;
  background: #f5f5f5;
  font-size: 13px;
  font-weight: 700;
  color: #555;
}
.fine-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 16px;
  border-top: 1px solid #f0f0f0;
  font-size: 14px;
  color: #333;
  &.fine-row-note {
    background: #fff8e1;
    color: #e65100;
    font-size: 13px;
    justify-content: flex-start;
  }
}
.fine-amount {
  font-weight: 700;
  color: #c62828;
}
</style>