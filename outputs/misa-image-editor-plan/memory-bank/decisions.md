# Decision log
Tất cả đề xuất kỹ thuật phải kiểm chứng ở P0; phạm vi gốc theo yêu cầu người dùng.

| ID | Quyết định | Lý do / trạng thái |
|---|---|---|
| D01 | Giữ nguyên ảnh gốc; recipe có version | Ngăn mất dữ liệu; thiết kế nền tảng |
| D02 | Windows 11 x64, RAM ≥16 GB là baseline | Người dùng đồng ý giả định; chưa đo thực tế |
| D03 | C#/WPF + C++ worker + SQLite production; JSON versioned ở P0 | Đã chốt cho P0 trong ADR-0001; JSON chỉ là store smoke tạm thời để tránh khóa dependency catalog trước khi review migration/backup |
| D04 | Offline, chưa có backend cloud | Đủ luồng yêu cầu; là giả định sản phẩm |
| D05 | Copy snapshot/selected fields; Undo batch | Ưu tiên cao nhất từ người dùng |
| D06 | B thêm idempotent vào target | Diễn giải “đưa ảnh vào”; không toggle xóa |
| D07 | AI mask tái sinh trên từng ảnh đích | Không sao raster mask sai chủ thể |
| D08 | Background/hậu cảnh cùng vùng nền | Chưa yêu cầu depth segmentation |
| D09 | Profile phải đúng lens/mount và khả năng D/C/V | Tránh sửa sai hoặc hứa hỗ trợ giả |
| D10 | Cùng graph cho preview/export | Giảm lệch màu/kết quả |
| D11 | Stable tag + installer + rollback trước mốc mới | Yêu cầu handoff của người dùng |
| D12 | Preset MISA riêng, không hứa Adobe XMP tương đương | Engine có thông số/thuật toán khác |
| D13 | P0 dùng MODNet Photographic ONNX cho offline portrait/subject capability spike | Model 25.97 MB, ONNX Runtime, nguồn/Apache-2.0 được ghi trong `models/README.md`; chưa coi là general-scene/background quality acceptance; hash/provider/inference có verifier |
| D14 | Recipe và float graph hỗ trợ mask `rectangle` và `raster` path với `invert` tùy chọn | Cho phép output model đi vào local adjustment hoặc lấy complement làm background mà không ghi đè ảnh gốc; raster path/invert được lưu trong `MaskReference`, schema `recipe-v1`, và có test blend alpha |

Mỗi thay đổi mới ghi ngày, quyết định bị thay thế, lý do và ảnh hưởng dữ liệu/test. Không sửa lịch sử thành như đã chốt từ đầu.
