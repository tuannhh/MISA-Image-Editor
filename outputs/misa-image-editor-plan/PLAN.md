# Plan và đặc tả sản phẩm
Ngày: 17/09/2026 · Các mốc trong tài liệu là mục tiêu; P0 hiện có prototype kiểm chứng một phần, P1–P7 chưa bắt đầu.

## 1. Phạm vi và cách dùng ảnh tham chiếu
Mục tiêu là quy trình chỉnh ảnh sự kiện/album trên Windows, có thao tác tương tự Lightroom Classic ở các nhóm đã yêu cầu. Không đặt mục tiêu sao chép thuật toán, tài nguyên hoặc toàn bộ Lightroom.

Ánh xạ 14 ảnh: ảnh 1 import; ảnh 2 tạo collection; ảnh 3 Basic; ảnh 4 Tone Curve; ảnh 5 Color Mixer/Point Color; ảnh 6–11 Lens Corrections; ảnh 12 Transform; ảnh 13 Crop; ảnh 14 Copy Settings. Những tùy chọn ngoài yêu cầu trong ảnh 14 như Remove, Lens Blur, HDR không tự động được thêm. B&W/Profile mức cơ bản nằm trong thiết kế editor; HDR workflow, panorama, tethering, cloud sync, generative remove, AI denoise và nhận diện danh tính khuôn mặt nằm ngoài v1.

## 2. Library
- Chọn file hoặc trỏ folder, có tùy chọn quét folder con; xem thumbnail, chọn/bỏ chọn trước import.
- Mặc định Add: catalog tham chiếu ảnh tại vị trí hiện tại. Không di chuyển hoặc sửa file gốc. Copy vào thư mục quản lý là mở rộng sau v1 nếu cần.
- Import JPEG/PNG trước; HEIC/HEIF, Sony ARW, Canon CR2/CR3, Nikon NEF/NRW theo matrix model máy ảnh và kiểu nén đã thử. Không suy diễn “mọi file RAW” từ đuôi file.
- Phát hiện file đã có theo đường dẫn chuẩn hóa + fingerprint; hash khi cần xác minh. Không loại nhầm hai file chỉ vì cùng tên.
- Một import session lưu thời điểm, nguồn, số thành công/lỗi/hủy. “Lần import gần nhất” hiển thị tập ảnh mới thực sự thêm ở session gần nhất có ảnh thành công; lần rỗng không xóa tập này. Lịch sử import vẫn lưu lần rỗng/lỗi.
- Collection là nhóm tham chiếu, một ảnh có thể thuộc nhiều nhóm. Tạo collection gồm tên, “Thêm các ảnh đang chọn”, “Đặt làm target collection”.
- Mỗi catalog có một target đang hoạt động, được lưu qua lần mở ứng dụng. Collection mới không được tick không thay target cũ.
- B thêm toàn bộ ảnh đang chọn vào target, thao tác lặp không thêm trùng; không tự bỏ ảnh đã có. Đây là mặc định theo yêu cầu “đưa vào”. Lệnh bỏ khỏi collection tách riêng và có Undo.
- Nếu chưa có target, cho chọn target; không tự đoán. Xóa target chỉ xóa nhóm tham chiếu và yêu cầu đặt target khác.
- B chỉ hoạt động khi vùng chọn ảnh có focus, không kích hoạt lúc gõ tên hoặc dùng brush. Trong Mask dùng nút Brush hoặc phím riêng.
- Chọn nhiều bằng Ctrl/Shift, Ctrl+A trong tập đang xem; phân biệt ảnh active với ảnh selected. Không áp batch lên ảnh ngoài tập chọn.
- File bị chuyển/ổ ngoài ngắt: đánh dấu thiếu và cho Relink folder/file; vẫn giữ preset/lịch sử. Xóa khỏi catalog không xóa ảnh trên đĩa.

## 3. Editor / Edit
Cấu trúc: danh sách bên trái; canvas trung tâm; histogram và panel Edit/Crop/Mask bên phải; filmstrip dưới. Giao diện tiếng Việt, kèm thuật ngữ quen thuộc khi cần. Nền trung tính, không nhuộm màu canvas. Hỗ trợ DPI 100–200%, cửa sổ thu nhỏ hợp lý, thao tác bàn phím.

Basic:
- White Balance: As Shot cho RAW, Temperature/Tint, eyedropper; JPEG/HEIC dùng điều chỉnh tương đối, không giả lập lại dữ liệu RAW đã mất.
- Exposure, Contrast, Highlights, Shadows, Whites, Blacks.
- Texture, Clarity, Dehaze, Vibrance, Saturation. Nhóm texture/clarity/dehaze hoàn thiện sau bộ tone cơ bản; không bỏ khỏi phạm vi cuối.
- Reset theo nhóm/toàn ảnh; Before/After; Undo/Redo; Fit và 100%.

Tone Curve: đường cong tổng và RGB, thêm/kéo/xóa điểm; nội suy tránh dao động; parametric highlights/lights/darks/shadows có vùng chuyển tiếp.

Color Mixer: HSL theo 8 dải màu; Point Color dùng eyedropper chọn màu, điều chỉnh Hue/Saturation/Luminance, độ rộng/độ mềm vùng màu, visualize selection. Ảnh 5 khiến Point Color được đưa vào phạm vi v1.

Lens: nhận camera/lens qua metadata; profile đúng mount/model; sửa distortion/TCA/vignette theo dữ liệu thực có. Có chỉnh tay distortion, vignette/midpoint, defringe tím/xanh và constrain crop. Không coi TCA và defringe là cùng một thuật toán.

Transform: Vertical, Horizontal, Rotate, Aspect, Scale, X/Y offset, Constrain Crop; chế độ Off/Level/Vertical/Full/Auto và Guided bằng đường tham chiếu. Auto chỉ đề xuất khi đủ tin cậy, có thể Reset.

## 4. Crop
Tự do hoặc khóa tỷ lệ Original, 1:1, 4:3, 3:2, 16:9 và Custom; đổi ngang/dọc, kéo khung, xoay góc, grid overlay, reset, constrain to image.
Auto Straighten đề xuất góc theo đường chân trời/đường ngang chính; người dùng xem trước rồi áp dụng. Không có bằng chứng rõ thì giữ nguyên và cho vẽ đường hai điểm. Không cam kết tự đúng với mọi ảnh sân khấu hoặc kiến trúc.
Crop là metadata có thể sửa lại; ảnh xuất mới bị cắt.

## 5. Mask
- Brush add/subtract/erase, size, hardness/feather, opacity/flow; overlay, invert, bật/tắt, tên mask.
- Linear/radial gradient là bổ sung nhỏ hữu ích trong mốc manual, có thể hoãn nếu ảnh hưởng thời gian.
- Điều chỉnh cục bộ: exposure, contrast, highlights/shadows, whites/blacks, temperature/tint tương đối, saturation; clarity/texture/dehaze khi engine hỗ trợ.
- Auto subject và background: xác định chủ thể, phần nền là nghịch đảo tập chủ thể đã chọn. Với nhiều người phải cho chọn chủ thể hoặc toàn nhóm.
- “Background/hậu cảnh” được hiểu là cùng lớp nền trong v1. Nếu cần phân vùng theo độ sâu gần/xa thì đó là yêu cầu khác, cần mô hình/kiểm thử riêng.
- Mọi mask AI đều sửa được bằng brush; tóc, vật trong suốt, nền lẫn màu có thể cần sửa.
- Khi copy sang ảnh khác: AI phải tính lại vùng theo ảnh đích; manual mask copy theo tọa độ chuẩn hóa chỉ khi bật chủ động và có preview, vì bố cục có thể khác.

## 6. Copy / Paste hàng loạt — ưu tiên cao nhất
- Ctrl+Shift+C mở Copy Settings từ ảnh active: checkbox nhóm và từng trường (WB, Exposure...); Check All/None; crop/mask/lens tắt mặc định để tránh áp nhầm.
- Copy giữ snapshot bất biến tại thời điểm copy. Chỉnh ảnh nguồn sau đó không làm clipboard đổi.
- Ctrl+Shift+V áp snapshot vào các ảnh selected. Không bắt buộc lưu preset.
- “Lưu preset” lưu tên, phiên bản recipe, các trường được chọn; đổi tên/xóa/export/import preset riêng của MISA.
- Paste chỉ thay các trường đã tick, giữ nguyên trường còn lại, không cộng dồn exposure khi paste lặp.
- Preset có thể chọn WB cố định hoặc As Shot; As Shot đọc lại từng ảnh đích. Từ RAW sang JPEG phải báo trường WB không tương thích và cho bỏ qua/chuyển tương đối có chủ đích.
- Lens Auto resolve lại cho từng ảnh; không khóa profile của ống kính nguồn lên toàn bộ album.
- Batch có tiến độ, số thành công/bỏ qua/lỗi, Cancel và Retry. Giao dịch theo chunk có journal; hủy giữ các ảnh đã hoàn thành và thông báo rõ.
- Undo batch khôi phục tất cả mục đã áp thành công. Nếu có chỉnh mới chồng lên sau batch, không âm thầm ghi đè: báo conflict và cho khôi phục có chọn lọc.
- Previous: lấy thiết lập của ảnh chỉnh trước theo cùng quy tắc chọn trường; không mơ hồ “ảnh đứng bên trái”.
- Không cam kết dùng trực tiếp preset Adobe .xmp/.lrtemplate hoặc cho màu giống Adobe; bộ thông số/thuật toán khác nhau.

Nghiệm thu lõi: chỉnh A, copy Exposure + Color Mixer, chọn 200 ảnh, paste; 200 ảnh nhận đúng 2 nhóm, crop/WB giữ nguyên; Undo phục hồi; khởi động lại vẫn đúng; xuất batch dùng đúng thiết lập.

## 7. Export
- Chọn folder cha và tên folder con hoặc cấu trúc theo collection/ngày; xem trước đường dẫn kết quả.
- JPEG chất lượng tùy chọn, PNG; TIFF 16-bit ở mốc RAW. V1 chưa cần xuất RAW hoặc HEIC.
- Giữ kích thước hoặc đặt cạnh dài/rộng/cao, khóa tỷ lệ, tùy chọn không phóng lớn.
- Watermark chữ hoặc PNG alpha; chọn font, size, 9 điểm neo/kéo vị trí, lề, opacity; lưu cấu hình và preview.
- Size/lề watermark theo tỷ lệ ảnh đầu ra hoặc pixel, không theo độ zoom canvas. Watermark áp sau resize.
- Mặc định sRGB có nhúng profile. PNG giữ alpha theo tùy chọn; JPEG chọn màu nền khi cần flatten.
- Tên trùng: mặc định thêm hậu tố; có Skip/Replace khi người dùng chủ động chọn. Không ghi đè ảnh gốc kể cả trùng đường dẫn.
- Chọn giữ metadata cơ bản/copyright; GPS là tùy chọn minh bạch. Orientation chuẩn hóa để ảnh không xoay hai lần.
- Job lưu snapshot recipe, kích thước, watermark asset và profile tại thời điểm tạo. Chỉnh tiếp ảnh không đổi job đang chạy.
- Hết dung lượng, mất kết nối ổ đĩa, file lỗi: báo theo từng ảnh, tiếp tục ảnh hợp lệ, cho retry và mở folder kết quả.

## 8. Chưa xác nhận, không cản việc lập kế hoạch
Máy ảnh/model/kiểu nén thực dùng; lens Canon EF/RF và Sigma DG HSM/DG DN; chính xác 18–135/18–105 và 100–400 đời mới; GPU; nhu cầu Windows 10; bộ watermark; sử dụng nội bộ hay phân phối rộng.
