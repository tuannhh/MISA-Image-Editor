# Handoff hiện tại — P0 candidate
Ngày: 17/09/2026 · Tài liệu: 0.0.1 · Phần mềm stable: CHƯA CÓ.

P0 technical prototype đã hoàn tất theo `P0-TECHNICAL-DECISION.md`; candidate vẫn không phải bản stable hay P3/P7.

## English handoff summary

P0 is technically complete under `P0-TECHNICAL-DECISION.md`; the candidate remains neither a stable release nor P3/P7. It is a framework-dependent .NET 10 WPF candidate for Windows x64. The active binary is published in `outputs/misa-image-editor-p0/artifacts/p0-candidate/app`; use the packaged ZIP and its SHA-256 file for a reviewable handoff.

The candidate supports the P0 Library-to-batch workflow and now includes a top-bar Vietnamese/English selector. The chosen language persists in `%LOCALAPPDATA%\MisaImageEditor\language.json`. This is an application-local WPF i18n component with no additional end-user dependency. The latest P0 gate passed Python 22/22, clean .NET Release/domain smoke, native CTest, and publish/ZIP startup smoke; re-run the same gates after packaging an updated candidate.

Do not call the result stable: actual user camera/lens samples, exact calibration for four requested lens variants, automatic-mask quality acceptance, native IPC, installer, and target-hardware acceptance are still missing. See `../BILINGUAL-DOCUMENTATION.md` for the active Vietnamese–English documentation policy.

## Cập nhật nghiệm thu candidate 22/09/2026

Candidate ZIP hiện tại: `outputs/misa-image-editor-p0/artifacts/misa-image-editor-p0-candidate.zip`, SHA-256 `168A0C4F3854E07961CC6C1107B69B6B3344E188BDEFDCAD1EE9A90BB6C1F64C`. Release build có 0 warning/0 error; C# domain smoke, Python 22/22, native CTest 1/1, startup smoke publish/ZIP đều pass. Thêm `LocalizationService` WPF và lựa chọn `Tiếng Việt`/`English`; lựa chọn được lưu tại `%LOCALAPPDATA%\MisaImageEditor\language.json`. Thêm `AppearanceService` với Sáng/Tối/Theo hệ thống, mặc định Sáng để chữ đen trên nền sáng; lựa chọn được lưu tại `%LOCALAPPDATA%\MisaImageEditor\theme.json` và áp dụng cả hộp thoại. Lỗi XAML parse do `I18n` resource trên Window title đã được phát hiện trong publish smoke và sửa trước khi đóng gói; publish/ZIP startup log chỉ còn 2 byte newline từ bước dọn log. `MISA_PYTHON` môi trường người dùng trỏ tới runtime đã cài rawpy, pillow-heif, ONNX Runtime và Lensfun. Candidate chưa phải stable vì camera matrix, 4 lens variant, AI quality acceptance, native IPC và installer vẫn chưa hoàn tất.

Chi tiết P0-01 đến P0-07 và quyết định platform nằm tại `handoff/P0-TECHNICAL-DECISION.md`. Sau cập nhật color baseline và RAW EXIF resolver, Python suite là 22/22; SHA của candidate không đổi vì đây là bổ sung harness/handoff ngoài gói app.

## Đã bàn giao
Plan/đặc tả, kiến trúc, ADR-0001, roadmap P0–P7, khảo sát profile, test/release gates, memory bank và mẫu bàn giao stable.
Đã bàn giao P0 Python prototype tại outputs/misa-image-editor-p0; smoke 20/20 qua, gồm manifest hash verification, recipe-v1 raster-mask contract với invert subject/background, catalog reopen/last-import semantics, empty-import preservation, re-import recipe preservation, SHA-256 fingerprint update, float RGB, EXIF normalization, lens resolver, HEIC 8/10-bit codec probe, một Canon PowerShot V1 HIF camera decode/render, mười RAW fixture decode/render (4 NEF, 2 CR2 upstream + 2 ARW/2 CR3/1 NRW public CC0), raster local-mask application, MODNet Photographic offline mask inference, rawpy capability path, ONNX capability probe và rectangular mask spike. Lensfun exact coverage 8/13 requested variants; nearest Sony 18-135 entry được ghi riêng, không thay thế profile f/4G. Solution Windows .NET 10 tại outputs/misa-image-editor-p0/windows gồm WPF desktop shell, collection dialog (include selected/set target), Library recent-import/all-catalog views, JPEG/PNG Basic preview feedback cho bảy trường tone, WPF raster-mask panel/preview (choose/clear, local exposure, invert), recipe JSON naming smoke khớp recipe-v1, C# recipe contract, versioned JSON catalog store và native worker contract validate/apply đủ bảy trường tone; C# catalog smoke qua, WPF clean Release build 0 warning/0 error, native CMake/MSVC build và ctest 1/1 passed, executable startup smoke đã qua, benchmark synthetic 12 MP và Canon HIF/CR3 thật đã ghi nhận. Chưa bàn giao installer, calibrated lens pack hay AI quality acceptance; brush/automatic subject refinement, HEIF/HEIC camera matrix đầy đủ và matrix camera người dùng còn thiếu. CUA connector không expose native app tree nên chưa click-through UI. Không có commit/tag để checkout.

## Người tiếp nhận cần biết
P0 Windows candidate đã publish tại `outputs/misa-image-editor-p0/artifacts/p0-candidate`; ZIP `misa-image-editor-p0-candidate.zip` có SHA-256 trong `artifacts/P0-CANDIDATE-SHA256.txt`. Đây là framework-dependent candidate cần .NET 10 Windows Desktop Runtime x64, chưa phải installer/stable.
Đọc README → PLAN → ARCHITECTURE → ROADMAP → LENS-AND-DEPENDENCIES → memory-bank. Quyết định ưu tiên là batch; bộ cài Windows và giữ nguyên ảnh gốc là yêu cầu nền tảng.
Các điểm chưa chắc: hardware/camera; model lens chưa rõ; profile GM II thiếu trong nguồn đã kiểm tra; chất lượng camera rendering; model AI và khả năng chạy offline.

## Hành động đầu tiên
Tiếp tục P0: thêm sample RAW/HEIC thật, hoàn thiện license/model review và chuyển dần native contract sang worker/IPC; giữ Python prototype như oracle contract nhỏ. Chạy P0 release gate, cập nhật estimate. Không gán phiên bản 1.0 hoặc stable chỉ vì prototype chạy.
Sau mỗi mốc, tạo handoff theo STABLE-TEMPLATE và cập nhật file này trỏ phiên bản stable thực tế. Giữ nguyên handoff cũ để truy vết.

## Giới hạn rollback hiện tại
Đây chỉ là tài liệu; chưa có dữ liệu ứng dụng hoặc migration để rollback.
