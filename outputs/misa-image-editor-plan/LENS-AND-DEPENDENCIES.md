# Khảo sát profile và dependency
Kiểm tra nguồn ngày 17/09/2026. “Có dữ liệu” không có nghĩa đã tích hợp hoặc nghiệm thu trên MISA.

## Profile đã tra cứu
D = distortion, C = viền màu ngang/TCA, V = vignette; dấu — là chưa thấy dữ liệu tương ứng. Matrix dưới dựa trên [danh sách Lensfun](https://lensfun.github.io/lenslist/), bản trang ghi ngày 16/09/2026. Cần pin snapshot khi build.

| Lens | D/C/V |
|---|---|
| Canon EF 24–70 f/2.8L I | ✓/—/✓ |
| Canon EF 24–70 f/2.8L II | ✓/✓/✓ |
| Sony E 18–135 f/3.5–5.6 OSS | ✓/✓/✓ |
| Sony E PZ 18–105 f/4 G | ✓/✓/— |
| Sony 24–70 f/2.8 GM I | ✓/✓/✓ |
| Sony 24–70 f/2.8 GM II | ✓/✓/— |
| Sony 70–200 f/2.8 GM I | ✓/—/— |
| Sony 70–200 f/2.8 GM II | Chưa thấy |
| Sony 16–35 f/2.8 GM I | ✓/✓/✓ |
| Sony 16–35 f/2.8 GM II | Chưa thấy |
| Sony 100–400 f/4.5–5.6 GM | ✓/✓/✓ |
| Tamron 17–70 f/2.8 | ✓/✓/✓ |
| Sigma 24–70 f/2.8 DG DN Art I | ✓/✓/— |
| Sigma 24–70 f/2.8 DG DN Art II | ✓/✓/— |

“Chưa thấy” chỉ là kết quả khảo sát nguồn trên, không chứng minh mọi nguồn đều không có.

## Các tên phải xác minh trước khi map tự động
- “18–135 f4G” có thể nhầm hai model khác nhau ở bảng; cần mã lens/EXIF thật.
- Canon 24–70 phải phân biệt EF I/II và RF; bảng trên không chứng nhận RF.
- Sigma phải phân biệt DG HSM với DG DN, mount và đời II; không dùng chung theo tiêu cự.
- “100–400 GM I & II”: nguồn Sony hiện nêu lens FE 100–400 F4.5 GM OSS mới, mã SEL100400MC, khác F4.5–5.6 cũ; không tự đặt tên GM II hoặc tái dùng profile cũ. Chưa xác minh profile cho model mới. [Thông báo Sony 14/05/2026](https://www.sony.com.sg/pressrelease?prName=sony-electronics-accelerates-high-resolution-photography-with-the-alpha-7r-vi-2).

## Cách xử lý thiếu profile
Resolve theo camera maker/model, lens ID/name, mount, crop, focal length, aperture và extender; mơ hồ thì cho chọn tay. Dữ liệu chỉ có D thì chỉ bật D. Không tạo checkbox hiệu chỉnh giả.
Có thể dùng correction metadata nhúng nếu decoder cung cấp và thuật toán được xác minh; không mặc nhiên mọi hãng đều đọc được.
Thứ tự: profile phù hợp đã kiểm thử → metadata hỗ trợ đã kiểm thử → calibration bổ sung → chỉnh tay. Không thay profile đời II bằng đời I.
Lưu profile id/hash, phiên bản database và correction đã áp; cập nhật profile không làm ảnh cũ đổi màu/hình học âm thầm. Phân biệt JPEG đã được máy sửa lens với RAW để tránh sửa hai lần.
Calibration cần ảnh chart/grid, bề mặt sáng đều ở nhiều khẩu/tiêu cự và kiểm thử độc lập. Chốt phạm vi đạt cho từng loại D/C/V.

## Thư viện và việc phải xác nhận khi đóng gói
| Thành phần | Vai trò | Điều kiện kỹ thuật/phát hành |
|---|---|---|
| LibRaw | Đọc RAW và metadata | Test theo camera + kiểu nén; không dùng thumbnail thay dữ liệu RAW |
| Lensfun | Quang học | Pin data/library; kiểm tra phạm vi hiệu chỉnh từng profile |
| libheif + decoder HEVC | Đọc HEIC | Test codec trên máy sạch, đủ bit depth; chốt license từng dependency |
| LittleCMS | ICC | Kiểm tra màn hình/output profile, không áp transform hai lần |
| ONNX Runtime / WinML | Chạy model | Pin opset/provider; test CPU và GPU mục tiêu |
| Model segmentation | Sinh mask | Chốt cả code/weights, chất lượng subject/group/hair và tốc độ |

LibRaw cung cấp lựa chọn LGPL 2.1 hoặc CDDL 1.0; bản convert đi kèm không đặt mục tiêu production rendering. [LibRaw](https://www.libraw.org/). Camera coverage theo phiên bản/compile flags và từng model. [Danh sách camera](https://www.libraw.org/supported-cameras).

Lensfun phân biệt license thư viện LGPLv3, ứng dụng GPLv3 và database CC BY-SA 3.0. Chỉ tích hợp các phần cần thiết, lưu notices và nghĩa vụ phân phối tương ứng; đây chưa phải kết luận pháp lý cho toàn bộ bản cài. [License upstream](https://github.com/lensfun/lensfun).

libheif cần decoder riêng cho HEIC; không tự động kéo encoder không dùng. Kiểm tra license thực tế của bản build và điều kiện codec trước phân phối. [libheif](https://github.com/strukturag/libheif).

SAM 2 là ứng viên phân vùng có gợi ý; upstream ghi code/checkpoint theo Apache 2.0, nhưng điều này không tự chứng minh model chạy ONNX tốt hoặc nhận đúng chủ thể chính. [SAM 2](https://github.com/facebookresearch/sam2).

Bảng phát hành phải có dependency version, source URL/commit, checksum, license, notices và nơi lưu source/patch khi cần. Chưa tải hoặc tích hợp bất kỳ thư viện/model/profile nào trong phiên lập kế hoạch.
