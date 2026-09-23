# MISA Image Editor — bộ kế hoạch triển khai
Ngày lập: 17/09/2026 · Phiên bản tài liệu: 0.0.1 · Trạng thái: Planning baseline.

**Có thể phát triển ứng dụng Windows theo phạm vi này.** Tôi có thể hỗ trợ thiết kế, viết mã, kiểm thử và đóng gói theo từng mốc. Hiện đã có prototype P0 và shell Windows để kiểm chứng đường đi chính; chưa có bộ cài hoặc bản phần mềm ổn định. Chất lượng màu RAW, tốc độ và mask AI phải được chứng minh bằng prototype và ảnh thật, không thể cam kết giống Lightroom từng pixel.

Ưu tiên sản phẩm: import → chọn ảnh → B vào target collection → chỉnh một ảnh → copy thiết lập → paste hàng loạt → xuất ảnh có watermark. Mỗi phiên bản phải giữ được luồng đã hoạt động.

## Cách đọc
1. [PLAN.md](PLAN.md): phạm vi, hành vi, tiêu chí nghiệm thu.
2. [ARCHITECTURE.md](ARCHITECTURE.md): nền tảng, xử lý ảnh, dữ liệu, batch, AI.
3. [ROADMAP.md](ROADMAP.md): mốc phát triển, phụ thuộc, thời lượng ước lượng.
4. [ADR-0001-platform-and-pipeline.md](ADR-0001-platform-and-pipeline.md): quyết định nền tảng và ranh giới P0.
5. [LENS-AND-DEPENDENCIES.md](LENS-AND-DEPENDENCIES.md): profile, nguồn kỹ thuật, phần còn thiếu.
6. [TEST-AND-RELEASE.md](TEST-AND-RELEASE.md): kiểm chứng chất lượng và phát hành.
7. [memory-bank/README.md](memory-bank/README.md): trạng thái và cách duy trì trí nhớ dự án.
8. [handoff/CURRENT.md](handoff/CURRENT.md): bàn giao hiện tại.
9. [handoff/STABLE-TEMPLATE.md](handoff/STABLE-TEMPLATE.md): mẫu bắt buộc cho bản ổn định.

## Định hướng đã dùng để lập kế hoạch
- Desktop Windows, làm việc cục bộ; chưa cần máy chủ/tài khoản/cloud.
- Windows 11 x64, RAM từ 16 GB là cấu hình mục tiêu tạm thời được người dùng đồng ý dùng để lập kế hoạch. Chưa biết phần cứng thực tế; GPU và máy ảnh chưa được xác nhận.
- C# / WPF / .NET 10 LTS cho giao diện; C++ cho lõi ảnh; SQLite là catalog production mục tiêu (P0 dùng JSON versioned để smoke).
- Ảnh gốc được giữ nguyên. Bộ thiết lập chỉnh ảnh và lịch sử lưu tách biệt.
- Bản dùng được đầu tiên tập trung JPEG/PNG và batch; RAW, HEIC, quang học, mask theo sau nhưng có thử nghiệm khả thi từ mốc đầu.
- Ảnh Lightroom đính kèm là tham chiếu thao tác. Các mục xuất hiện trong ảnh nhưng không được yêu cầu không tự động trở thành phạm vi.
- ADR-0001 đã chốt nền tảng cho P0; trước P3 phải đánh giá lại bằng benchmark camera thật, màu và chi phí migration.

## Ước lượng
Với 2 kỹ sư có kinh nghiệm desktop/xử lý ảnh, QA bán thời gian và người dùng cung cấp phản hồi: bản làm việc JPEG/PNG đầu tiên khoảng 7–11 tuần; toàn bộ phạm vi khoảng 21–34 tuần phát triển, thêm 20–30% dự phòng. Đây là ước lượng để lập kế hoạch, không phải cam kết tiến độ của một phiên chat. Cần điều chỉnh sau P0, đặc biệt nếu phải tự hiệu chuẩn ống kính hoặc cải tiến mô hình AI.

## Việc tiếp theo
Thực hiện P0: skeleton Windows, mở/xuất ảnh thật qua cùng engine, spike RAW/HEIC, spike AI, đo hiệu năng và kiểm tra việc đóng gói thư viện. Chỉ sau khi có bằng chứng mới đưa chức năng vào trạng thái “đã hỗ trợ”.

## English summary

**The Windows application can be developed within this scope.** The product priority is: import → select photos → press `B` to add them to a target collection → edit one photo → copy settings → paste them to many photos → export with a watermark. Each delivery must retain that working path.

The planned production direction is a local Windows desktop application: C# / WPF / .NET 10 for the UI, a native C++ image-processing boundary, and SQLite for the production catalog. P0 uses a versioned JSON catalog only for smoke validation. Original photos remain unchanged; edit recipes and history are stored separately. The current P0 candidate proves the technical route but is not an installer or stable release. Read the document map above, `BILINGUAL-DOCUMENTATION.md`, and `handoff/CURRENT.md` before continuing work.
