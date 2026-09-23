# Roadmap và cổng nghiệm thu
Ngày: 17/09/2026. Chưa có mốc phần mềm nào hoàn tất.

Ước lượng theo tuần làm việc của nhóm 2 kỹ sư có kinh nghiệm, QA bán thời gian, có ảnh mẫu và phản hồi đều. Mỗi khoảng đã gồm kiểm thử mốc; tổng 21–34 tuần, dự phòng thêm 20–30%. Nếu chỉ một người triển khai, thời gian có thể dài hơn đáng kể. Chốt lại sau P0 bằng số liệu; không lấy các mốc này làm lời hứa giao hàng.

| Mốc | Thời lượng | Sản phẩm | Điều kiện qua mốc |
|---|---:|---|---|
| P0 / 0.0.x | 1–2 tuần | Prototype kỹ thuật, quyết định nền tảng | Mở RAW đại diện 3 hãng và HEIC; render/export không sai orientation; đo màu/tốc độ/RAM; thử một mask; build trên Windows sạch |
| P1 / 0.1.0 | 2–3 tuần | Library ổn định | Import JPEG/PNG, collection, target+B, last import, missing/relink; mở lại catalog đúng |
| P2 / 0.2.0 | 4–6 tuần | Bản dùng được đầu tiên cho JPEG/PNG | Basic tone/WB, Curve/HSL cơ bản, crop tay, copy có chọn trường, preset, batch undo, JPEG/PNG export+watermark |
| P3 / 0.3.0 | 3–5 tuần | RAW/HEIC và màu | Các model trong matrix đọc được RAW thật; pipeline màu/ICC, TIFF16, lỗi codec rõ ràng; preview/export nhất quán |
| P4 / 0.4.0 | 3–5 tuần | Edit và hình học hoàn chỉnh | Texture/Clarity/Dehaze, parametric/RGB curve, Point Color, lens profile/manual, Transform, auto straighten |
| P5 / 0.5.0 | 2–4 tuần | Manual mask | Brush mềm/cứng, add/subtract/invert; local tone/color; mask không lệch sau crop/transform; copy mask có kiểm soát |
| P6 / 0.6.0 | 4–6 tuần | Mask tự động | Subject/background, refine brush, tái tính trên ảnh đích khi paste; benchmark chất lượng/tốc độ/provider |
| P7 / 1.0.0 | 2–3 tuần | Bản phát hành phạm vi đã nghiệm thu | Soak test, cài/nâng cấp/rollback, tài liệu sử dụng, bộ cài và handoff đầy đủ |

Luồng dùng JPEG/PNG đầu tiên đạt sau P0–P2, khoảng 7–11 tuần. P0 đã kiểm tra các rủi ro RAW/AI từ sớm; việc xếp tích hợp sau không có nghĩa trì hoãn tìm hiểu rủi ro.

## Phụ thuộc và đường găng
P0 → P1 → P2 tạo nền tảng batch không phá ảnh. P3 khóa recipe/process và màu → P4 khóa hình học → P5 khóa hệ tọa độ mask → P6 tích hợp AI. Không xây AI đè lên hệ tọa độ còn thay đổi.
Có thể nghiên cứu model/profile trong lúc hoàn thiện UI, nhưng chưa đưa kết quả thử nghiệm vào stable.

## Quy tắc “ổn định trước, tính năng mới sau”
1. Hoàn tất phạm vi mốc và bộ regression của mốc cũ.
2. Đóng gói release candidate; người dùng thử luồng thật.
3. Sửa lỗi chặn; ghi giới hạn có bằng chứng.
4. Tạo tag bất biến, build artifact/checksum, backup catalog mẫu, handoff.
5. Cập nhật stable pointer và memory bank. Chỉ lúc này bắt đầu tính năng mốc tiếp.
Mốc internal stable chỉ ổn định trong phạm vi của nó, không được quảng cáo như v1 đầy đủ. Không gắn nhãn stable khi mới xong UI.

## Rủi ro ảnh hưởng tiến độ
- RAW: decoder thành công nhưng màu/da/highlight chưa đạt → dành spike pipeline hoặc đánh giá engine/SDK khác với giấy phép phù hợp.
- Profile thiếu: cần nguồn hợp lệ hoặc tự calibration bằng ảnh lens chart ở nhiều tiêu cự/khẩu độ. Thời gian lấy ảnh chưa nằm trong cam kết.
- Model AI: không đạt tốc độ/viền tóc → chọn model nhẹ hơn, tối ưu, hoặc giữ beta. Không bỏ yêu cầu auto mask mà vẫn tuyên bố hoàn thành.
- Lens chưa xác định đúng model → giữ manual fallback và trạng thái chưa chứng nhận; v1 chỉ “đủ toàn bộ yêu cầu” khi các mục này được giải quyết hoặc người dùng đồng ý giảm phạm vi.
- GPU/codec/màu đa màn hình → cần máy kiểm thử thực, CPU fallback và installer test.
- Phân phối rộng có thể cần công việc chứng thư ký số/giấy phép ngoài phát triển thuật toán.

## Backlog P0 có đầu ra cụ thể
- P0-01: tạo solution C#/C++, build debug/release x64, bản chạy sạch.
- P0-02: tập fixture có hash: JPEG/PNG, HEIC8/10bit, ARW/CR2/CR3/NEF theo model có thật.
- P0-03: đường đi source → float → preview/export; đo màu so với fixture chuẩn, không đòi giống Adobe.
- P0-04: profile resolve thử với EXIF thật, liệt kê thiếu.
- P0-05: thử segmentation và khả năng đóng gói model/provider offline.
- P0-06: báo cáo throughput/latency/peak RAM trên máy mục tiêu.
- P0-07: ADR chốt/đổi nền tảng, cập nhật ước lượng, handoff prototype có giới hạn.
