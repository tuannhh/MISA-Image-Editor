# Tài liệu song ngữ / Bilingual documentation

## Mục đích / Purpose

Từ P0-i18n, các tài liệu đang hoạt động của MISA Image Editor được viết và duy trì bằng tiếng Việt và tiếng Anh. Mỗi tài liệu mới cần có hai phần tương đương, hoặc một bảng thuật ngữ và phần tóm tắt song ngữ nếu tài liệu đó là log kiểm thử dài.

From P0-i18n, active MISA Image Editor documentation is written and maintained in Vietnamese and English. Each new document must contain equivalent sections in both languages, or a bilingual summary and terminology table when it is a long test log.

## Tài liệu đang dùng / Active documents

| Tài liệu tiếng Việt | English purpose |
|---|---|
| `README.md` | Project scope, the active document map, assumptions, and the planning estimate. |
| `PLAN.md` | Product behaviour and acceptance criteria. |
| `ARCHITECTURE.md` | Desktop architecture, recipe/catalog boundary, rendering pipeline, and replaceable P0 adapters. |
| `ROADMAP.md` | P0–P7 delivery sequence, dependencies, and stable-handoff gates. |
| `TEST-AND-RELEASE.md` | Regression, quality, packaging, and release evidence. |
| `DEPENDENCIES-WINDOWS.md` | Windows development/runtime dependencies and licence checks. |
| `handoff/CURRENT.md` | The current candidate, verification evidence, limits, and the next handoff action. |
| `memory-bank/README.md` | How to keep decisions, progress, and context durable between work sessions. |

## Quy tắc thuật ngữ / Terminology rules

| Tiếng Việt | English | Cách dùng / Usage |
|---|---|---|
| Thư viện | Library | Tab quản lý ảnh và catalog. / The photo and catalog tab. |
| Chỉnh sửa | Editor | Tab chỉnh không phá hủy ảnh gốc. / The non-destructive editing tab. |
| Thiết lập chỉnh ảnh | Recipe / edit settings | Dữ liệu chỉnh lưu riêng khỏi ảnh gốc. / Edits stored separately from the source file. |
| Bộ sưu tập | Collection | Nhóm logic ảnh trong catalog. / A logical image group in the catalog. |
| Bộ sưu tập đích | Target collection | Đích của phím `B`. / Destination of shortcut `B`. |
| Mặt nạ | Mask | Vùng áp dụng chỉnh cục bộ. / Region receiving local adjustments. |
| Bản thử nghiệm | Candidate / prototype | Có thể kiểm tra nhưng chưa là stable release. / Testable but not a stable release. |
| Bàn giao ổn định | Stable handoff | Chỉ tạo sau khi đạt cổng nghiệm thu đã định. / Created only after the defined acceptance gate passes. |

## Phạm vi P0 hiện tại / Current P0 scope

P0 candidate đã có Library import/catalog, collection/target shortcut `B`, xem lần import gần nhất, recipe copy/paste theo nhóm trường, preset JSON, JPEG/PNG Basic preview/export, Tone Curve, HSL Color Mixer, metadata ống kính và hiệu chỉnh thủ công, crop/auto-straighten, brush mask, offline subject mask và watermark. RAW/HEIF dùng bridge Python cục bộ và vẫn là đường thử nghiệm.

The P0 candidate includes Library import/catalog, collection/target shortcut `B`, last-import view, field-group recipe copy/paste, JSON presets, JPEG/PNG Basic preview/export, Tone Curve, HSL Color Mixer, lens metadata and manual correction, crop/auto-straighten, brush masks, offline subject masks, and watermarking. RAW/HEIF uses a local Python bridge and remains a prototype path.

## Ngôn ngữ trong ứng dụng / In-app language

Ứng dụng có chọn `Tiếng Việt` hoặc `English` trên thanh đầu trang. `LocalizationService` lưu lựa chọn vào `%LOCALAPPDATA%\MisaImageEditor\language.json`; vì vậy lần khởi động sau giữ lại ngôn ngữ đã chọn. Thành phần này dùng cơ chế binding WPF nội bộ, không thêm một thư viện runtime bên ngoài hay yêu cầu cài đặt mới cho người dùng. Cùng vị trí có lựa chọn giao diện `Sáng`, `Tối`, hoặc `Theo hệ thống`; mặc định là Sáng để chữ đen trên nền sáng dễ đọc. `AppearanceService` lưu lựa chọn tại `%LOCALAPPDATA%\MisaImageEditor\theme.json` và áp dụng palette cho cửa sổ chính lẫn các hộp thoại.

The app provides `Tiếng Việt` and `English` in the top-bar language selector. `LocalizationService` saves the selection in `%LOCALAPPDATA%\MisaImageEditor\language.json`, so the next launch retains the chosen language. It uses an internal WPF binding-based i18n component; no external runtime library or new end-user installation is required. The same bar provides `Light`, `Dark`, and `System setting` appearance modes; Light is the default for readable black text on a bright background. `AppearanceService` saves this choice in `%LOCALAPPDATA%\MisaImageEditor\theme.json` and applies its palette to the main window and dialogs.

## Cách đọc bằng hai ngôn ngữ / Reading in both languages

Các báo cáo lịch sử P0 được giữ nguyên làm bằng chứng kỹ thuật. `README.md`, `P0-REPORT.md`, candidate README và `handoff/CURRENT.md` có phần tóm tắt song ngữ để bắt đầu; các tài liệu mới cần tuân thủ quy tắc ở trên. Khi cập nhật chi tiết dài trong một ngôn ngữ, cập nhật phần tương ứng ở ngôn ngữ kia trong cùng thay đổi.

Historical P0 reports remain intact as technical evidence. `README.md`, `P0-REPORT.md`, the candidate README, and `handoff/CURRENT.md` contain bilingual starting summaries; new documents must follow the rule above. When a detailed section is changed in one language, update the corresponding section in the other language in the same change.
