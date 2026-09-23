# ADR-0001: Nền tảng Windows và đường đi pixel P0

Ngày: 17/09/2026
Trạng thái: **Đã chốt cho P0, cần đánh giá lại trước P3**

## Bối cảnh

Ứng dụng chỉ chạy trên Windows, cần duyệt thư viện ảnh, chỉnh sửa không phá hủy, batch copy/paste recipe và xử lý ảnh nặng. P0 phải trả lời sớm các rủi ro RAW, HEIC, lens profile, mask và hiệu năng; không được biến prototype Python thành runtime bắt buộc của bản cài.

## Quyết định

- Giao diện và application layer dùng C#/.NET 10 WPF trên Windows x64.
- Lõi ảnh production dùng C++20/CMake, có ABI/IPC versioned; CPU là baseline kết quả.
- Python giữ vai trò oracle nghiên cứu và regression fixture ở P0, không đóng gói thành dependency bắt buộc của bản cài.
- Catalog production mục tiêu vẫn là SQLite theo kiến trúc; P0 dùng JSON store versioned để smoke persistence mà không kéo dependency native chưa được đánh giá giấy phép/bảo mật vào shell.
- RAW dùng adapter LibRaw/rawpy để spike decode/render và ghi rõ giới hạn; màu camera production, ICC, tile và export cần khóa ở các mốc sau.
- Preview và export phải dùng cùng recipe/process graph khi worker production được tích hợp. WPF P0 hiện chỉ preview Basic JPEG/PNG để kiểm tra đường nối UI → recipe.
- P0 mask capability dùng MODNet Photographic ONNX offline qua ONNX Runtime; recipe ghi nhận rectangle hoặc raster mask path với optional invert để biểu diễn subject/background complement. Đây là integration spike có provenance/hash, chưa là cam kết chất lượng subject/background cho mọi cảnh.

## Lý do

WPF đáp ứng phạm vi Windows và kiểm tra nhanh được dialog/shortcut/catalog. C++ phù hợp buffer, tile và codec nhưng vẫn có thể kiểm thử độc lập qua contract nhỏ. Tách oracle khỏi runtime giúp so sánh thuật toán trước khi khóa ABI. JSON P0 là lựa chọn tạm thời có thể đọc/di chuyển; không coi đó là schema production.

## Hệ quả và việc cần làm

- P0 đã có WPF shell, C# recipe/catalog contract, native validation + Basic RGBA8 smoke và Python image oracle.
- Trước P1/P2 phải chốt migration từ JSON sang SQLite, backup/migration và IPC worker.
- Trước P3 phải kiểm tra màu camera thật, ARW/CR3/NRW/HEIF 10-bit, ICC, export 16-bit và license/redistribution của codec/profile/model.
- Trước P5/P6 phải đo chất lượng mask trên bộ ảnh thật, bổ sung general-scene/background model nếu MODNet không đủ, rồi mới nối UI mask production và copy/recompute semantics.
- Thay đổi nền tảng sau P0 phải cập nhật ADR này, decision log và bộ regression; không đổi chỉ vì một fixture đơn lẻ.
