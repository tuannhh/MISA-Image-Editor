# P0.2 checkpoint — Import, RAW, Library UI

## Tiếng Việt

Ngày ghi nhận: 2026-09-23. Đây là checkpoint mã nguồn đang phát triển, chưa phải phiên bản ổn định đã nghiệm thu giao diện.

- Sửa giao thức JSON của codec bridge để không lỗi với đường dẫn tiếng Việt khi stdout dùng bảng mã Windows.
- Import ghi nhận metadata thay vì đọc toàn bộ file để tính hash; ảnh thu nhỏ và preview được xử lý nền. Fingerprint metadata không phải hash nội dung.
- Library có lưới ảnh riêng; Editor dùng preview tối đa 1440 px và debounce khi chỉnh slider.
- Collection lọc ảnh thật, hiển thị số lượng, cho đặt target; B được xử lý ở PreviewKeyDown và bỏ qua khi nhập văn bản.
- Cải thiện kiểu nút, tab, combo box và thẻ ảnh; giữ lựa chọn ngôn ngữ và giao diện.

Kiểm thử đã chạy: build thành công; domain smoke qua; desktop integration smoke với 28 ảnh (12 Sony ARW và 16 JPEG, gồm thư mục con) qua. Ghi nhận import 0,058 giây; tạo đủ thumbnail khi cache trống 13,937 giây; preview RAW trong Editor 0,452 giây. Đây là số đo trên máy phát triển, không phải cam kết cho mọi máy.

Còn lại: kiểm tra phím B bằng bàn phím thật và đánh giá giao diện cửa sổ thực bị gián đoạn trước khi hoàn thành; chưa đóng gói lại candidate. RAW vẫn dùng preview JPEG tối đa 2400 px của codec bridge cho pipeline P0, chưa phải xử lý/xuất RAW đầy đủ độ phân giải.

## English

Recorded: 2026-09-23. This is a development source checkpoint, not a stable release with completed visual acceptance.

- Fixed codec bridge JSON output for Vietnamese paths under Windows stdout encodings.
- Import registers metadata instead of hashing entire files; thumbnails and previews run in the background. Metadata fingerprints are not content hashes.
- Library has a dedicated image grid; Editor uses previews bounded to 1440 px and debounced slider rendering.
- Collections now filter photos, show counts and support target selection. B is handled through PreviewKeyDown and ignored during text entry.
- Updated buttons, tabs, combo boxes and image cards while retaining language and theme choices.

Validation completed: successful build, domain smoke and desktop integration smoke with 28 images (12 Sony ARW and 16 JPEG, including nested files). Measured import registration: 0.058 s; all thumbnails with an empty cache: 13.937 s; RAW Editor preview: 0.452 s. These are development-machine observations, not general performance guarantees.

Pending: physical-keyboard B verification and visual window review were interrupted; candidate packaging has not been refreshed. The P0 RAW pipeline still uses codec-bridge JPEG previews capped at 2400 px, rather than full-resolution RAW processing/export.
