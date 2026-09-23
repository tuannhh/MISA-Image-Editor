# Kiểm thử và phát hành
Các số dưới là mục tiêu nghiệm thu ban đầu, chưa có kết quả đo.

## Luồng bắt buộc mỗi mốc
1. Import folder có file hợp lệ/lỗi/trùng tên, tên tiếng Việt, ảnh xoay dọc, ổ rời.
2. Tạo collection có ảnh đang chọn, đặt target; B nhiều lần không trùng; đóng/mở giữ target và last import.
3. Chỉnh ảnh A → copy các trường chọn → paste 200 ảnh → Undo/Redo → đóng/mở.
4. Export vào folder cha/con, resize, watermark; đối chiếu số file, màu, vị trí, EXIF orientation.
5. Crash/hủy giữa import, paste và export; catalog vẫn mở, item hoàn tất/lỗi phân biệt đúng.
6. File gốc có hash không đổi sau mọi thao tác.

## Bộ dữ liệu
Tập đại diện nhỏ dùng mỗi commit và tập lớn trước release: 1.000 ảnh JPEG/PNG, catalog 10.000 ảnh để đo UI, batch tối thiểu 200 ảnh, RAW 24–61 MP và HEIC thực tế.
RAW chia theo maker/model/firmware/chế độ nén/bit depth. Có sample khác nhau cho cùng đuôi, không chỉ một file mỗi hãng.
Nội dung: da, sân khấu LED, ánh sáng hỗn hợp, bầu trời, highlight gắt, tóc, nhóm người, texture, màu bão hòa; thêm chart/lens grid. Fixture có nguồn/quyền/hash, không đưa ảnh riêng tư vào Git.
Tách tập phát triển và tập nghiệm thu AI để tránh chỉ tối ưu ảnh đã biết.

## Chất lượng ảnh
- Golden images từ pipeline đã chốt và chart có chuẩn tham chiếu; không dùng ảnh Lightroom làm oracle duy nhất.
- Kiểm tra float math, monotonic curve, màu trung tính, highlight clipping, alpha, tile seams và hình học.
- Preview/export so ở cùng kích thước/output space, loại trừ khác biệt do màn hình; ngưỡng khởi điểm delta E 2000 p95 ≤ 2 trên patch kiểm thử, điều chỉnh có lý do sau P0.
- CPU/GPU so sai số numeric và perceptual; nếu vượt ngưỡng, không bật GPU mặc định.
- Mask phải khớp vị trí sau crop/rotate/transform và không tạo quầng thấy rõ ở zoom 100%.
- Auto straighten trên tập có đường chân trời rõ: mục tiêu sai lệch góc ≤ 0,5° ở ≥ 90% ảnh; ảnh khó phải có trạng thái cần chỉnh tay.
- AI mục tiêu ban đầu: ≥ 90% tập ảnh thông thường dùng được sau không quá 30 giây brush refine; IoU trung vị ≥ 0,90 trên tập có ground truth phù hợp, kèm chấm viền tóc/chi tiết bằng mắt. Không dùng IoU để che lỗi viền nghiêm trọng. Khóa tiêu chí trước nghiệm thu P6.

## Hiệu năng
Máy đo khởi điểm: Windows 11 x64, RAM 16 GB, SSD; ghi rõ CPU/GPU/driver/build ở từng report. Cấu hình 32 GB giúp thử batch/RAW lớn nhưng chưa coi là bắt buộc.
Mục tiêu warm cache: phản hồi UI ≤ 100 ms; preview 2048 px khi kéo tone ≤ 150 ms p95; refine ≤ 1 giây cho thao tác thông thường; paste recipe 200 ảnh ≤ 5 giây không gồm render/AI.
AI preview mục tiêu ≤ 5 giây trên GPU tham chiếu được chọn sau P0; CPU có progress/cancel, chưa hứa cùng tốc độ.
Export full RAW phải báo ảnh/phút và peak RAM theo độ phân giải, không đặt con số tùy ý trước benchmark.
Catalog 10.000 ảnh cuộn không treo; test thao tác 2 giờ; RAM sau lặp job phải quay về ngân sách cache, không tăng vô hạn.

## Release gate
Tất cả tính năng trong scope mốc có evidence; không còn lỗi mất dữ liệu/ảnh gốc bị đổi, crash luồng chính hoặc batch sai ảnh; lỗi nhỏ có severity và workaround.
Windows sạch cài/mở/export được; kiểm tra DPI/multi-monitor, GPU fallback, disk full/read-only folder, relink, upgrade và restore.
Khóa build tool/dependency/profile/model; build tái lập từ clean checkout; artifact hash, báo cáo test, release notes và handoff cùng tag.
Ký số installer là việc chuẩn bị khi phân phối; không ghi “đã ký” khi chưa có certificate/build thật. Ưu tiên installer chuẩn Windows, chọn MSIX/MSI sau thử deployment P0.
Stable chỉ cập nhật khi có cả bộ cài thực tế và handoff. Dùng catalog copy thử phiên bản mới trước.
