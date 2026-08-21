<template>
  <div class="static-page">
    <div class="page-hero hero-green">
      <div class="hero-icon">📖</div>
      <h1>Hướng dẫn sử dụng</h1>
      <p>Hướng dẫn chi tiết các chức năng của hệ thống thư viện trực tuyến</p>
    </div>

    <div class="content-grid">
      <!-- Đăng ký tài khoản -->
      <div class="content-card">
        <h2 class="card-title">
          <Icon icon="glyphs-poly:user" width="24" height="24" /> Đăng ký & Đăng nhập
        </h2>
        <div class="steps">
          <div class="step">
            <div class="step-num">1</div>
            <div class="step-content">
              <div class="step-title">Nhận tài khoản</div>
              <div class="step-desc">
                Tài khoản thư viện được cấp tự động khi bạn nhập học. Liên hệ phòng thư viện nếu
                chưa có tài khoản.
              </div>
            </div>
          </div>
          <div class="step">
            <div class="step-num">2</div>
            <div class="step-content">
              <div class="step-title">Đăng nhập</div>
              <div class="step-desc">
                Sử dụng email trường (<strong>@lms.utc.edu.vn</strong>) hoặc mã sinh viên và mật
                khẩu được cấp để đăng nhập.
              </div>
            </div>
          </div>
          <div class="step">
            <div class="step-num">3</div>
            <div class="step-content">
              <div class="step-title">Đổi mật khẩu</div>
              <div class="step-desc">
                Vào
                <strong
                  >Thông tin cá nhân
                  <Icon icon="wordpress:arrow-right" width="20" height="20" /> Đổi mật khẩu</strong
                >
                để thay đổi mật khẩu lần đầu đăng nhập.
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Tìm kiếm sách -->
      <div class="content-card">
        <h2 class="card-title">🔍 Tìm kiếm sách</h2>
        <div class="steps">
          <div class="step">
            <div class="step-num">1</div>
            <div class="step-content">
              <div class="step-title">Tìm kiếm cơ bản</div>
              <div class="step-desc">
                Nhập từ khóa vào ô tìm kiếm trên đầu trang. Hệ thống sẽ tìm theo tên sách, tác giả,
                nhà xuất bản.
              </div>
            </div>
          </div>
          <div class="step">
            <div class="step-num">2</div>
            <div class="step-content">
              <div class="step-title">Tìm kiếm nâng cao</div>
              <div class="step-desc">
                Nhấn <strong>"Tìm kiếm nâng cao"</strong> để tìm theo nhiều tiêu chí kết hợp với
                toán tử AND/OR/NOT. Có thể lọc theo thể loại, năm xuất bản, ngôn ngữ, DDC.
              </div>
            </div>
          </div>
          <div class="step">
            <div class="step-num">3</div>
            <div class="step-content">
              <div class="step-title">Xem chi tiết sách</div>
              <div class="step-desc">
                Nhấn vào tên sách để xem thông tin chi tiết, danh sách bản sao và tình trạng hiện
                có.
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Đặt mượn online -->
      <div class="content-card">
        <h2 class="card-title">📚 Đặt mượn sách trực tuyến</h2>
        <div class="steps">
          <div class="step">
            <div class="step-num">1</div>
            <div class="step-content">
              <div class="step-title">Tìm sách muốn mượn</div>
              <div class="step-desc">
                Tìm kiếm và vào trang chi tiết sách. Kiểm tra số bản sao còn khả dụng.
              </div>
            </div>
          </div>
          <div class="step">
            <div class="step-num">2</div>
            <div class="step-content">
              <div class="step-title">Nhấn "Đặt mượn"</div>
              <div class="step-desc">
                Chọn ngày dự kiến đến lấy sách (trong vòng 7 ngày). Thêm ghi chú nếu cần và xác nhận
                gửi yêu cầu.
              </div>
            </div>
          </div>
          <div class="step">
            <div class="step-num">3</div>
            <div class="step-content">
              <div class="step-title">Chờ duyệt</div>
              <div class="step-desc">
                Thủ thư sẽ xem xét và duyệt yêu cầu. Bạn sẽ nhận được thông báo khi yêu cầu được
                duyệt hoặc từ chối.
              </div>
            </div>
          </div>
          <div class="step">
            <div class="step-num">4</div>
            <div class="step-content">
              <div class="step-title">Đến lấy sách</div>
              <div class="step-desc">
                Khi yêu cầu được duyệt, đến quầy thư viện và xuất trình
                <strong>mã QR hoặc mã yêu cầu</strong> trong mục <strong>Yêu cầu mượn</strong> để lấy sách.
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Gia hạn -->
      <div class="content-card">
        <h2 class="card-title">📅 Gia hạn mượn sách</h2>
        <div class="steps">
          <div class="step">
            <div class="step-num">1</div>
            <div class="step-content">
              <div class="step-title">Vào mục Sách đang mượn</div>
              <div class="step-desc">
                Đăng nhập và vào <strong>Sách đang mượn</strong> để xem danh sách sách hiện tại.
              </div>
            </div>
          </div>
          <div class="step">
            <div class="step-num">2</div>
            <div class="step-content">
              <div class="step-title">Nhấn "Gia hạn"</div>
              <div class="step-desc">
                Nhấn nút <strong>Gia hạn</strong> trên cuốn sách muốn gia hạn. Lưu ý: chỉ gia hạn
                được khi chưa quá hạn và không có phiếu phạt.
              </div>
            </div>
          </div>
          <div class="step">
            <div class="step-num">3</div>
            <div class="step-content">
              <div class="step-title">Xác nhận</div>
              <div class="step-desc">
                Kiểm tra hạn trả mới và xác nhận gia hạn. Mỗi cuốn sách chỉ được gia hạn
                <strong>1 ~ 3 lần</strong> tùy theo quy định.
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Đọc ebook -->
      <div class="content-card">
        <h2 class="card-title">💻 Đọc ebook trực tuyến</h2>
        <div class="steps">
          <div class="step">
            <div class="step-num">1</div>
            <div class="step-content">
              <div class="step-title">Tìm ebook</div>
              <div class="step-desc">
                Vào trang tìm kiếm, chọn bộ lọc <strong>"Ebook"</strong> để lọc riêng tài liệu điện
                tử.
              </div>
            </div>
          </div>
          <div class="step">
            <div class="step-num">2</div>
            <div class="step-content">
              <div class="step-title">Nhấn "Đọc online"</div>
              <div class="step-desc">
                Trên trang chi tiết ebook, nhấn nút <strong>Đọc online</strong> để mở trình đọc PDF
                tích hợp.
              </div>
            </div>
          </div>
          <div class="step">
            <div class="step-num">3</div>
            <div class="step-content">
              <div class="step-title">Lưu tiến độ tự động</div>
              <div class="step-desc">
                Hệ thống tự động lưu trang đang đọc. Lần sau mở lại sẽ tiếp tục từ trang bạn đã đọc.
              </div>
            </div>
          </div>
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
  &.hero-green {
    background: linear-gradient(135deg, #1b5e20, #2e7d32);
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
  align-items: center;
  gap: 4px;
}

.steps {
  display: flex;
  flex-direction: column;
  gap: 0;
}
.step {
  display: flex;
  gap: 16px;
  align-items: flex-start;
  padding: 14px 0;
  border-bottom: 1px solid #f5f5f5;
  &:last-child {
    border-bottom: none;
    padding-bottom: 0;
  }
  &:first-child {
    padding-top: 0;
  }
}
.step-num {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: linear-gradient(135deg, #3949ab, #5c6bc0);
  color: #fff;
  font-size: 14px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  margin-top: 1px;
}
.step-title {
  font-size: 14px;
  font-weight: 700;
  margin-bottom: 5px;
}
.step-desc {
  font-size: 13px;
  color: #555;
  line-height: 1.6;
  strong {
    display: inline-flex;
    align-items: center;
  }
}
</style>