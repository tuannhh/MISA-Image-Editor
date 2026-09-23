# MISA Image Editor — P0 technical spike

Trạng thái: P0 prototype, chưa phải bản Windows stable.

Prototype kiểm chứng catalog/collection/target/B, recipe copy/paste, float RGB preview/export JPEG/PNG, EXIF orientation, rectangle/raster local-mask application và MODNet offline mask spike. Thư mục `windows/` bổ sung WPF desktop shell, C# recipe contract, versioned JSON catalog smoke và native worker Basic RGBA8 contract để tiếp tục phát triển trên Windows. HEIC 8-bit, synthetic HEIF 10-bit, một Canon PowerShot V1 HIF camera fixture và 10 RAW fixture (4 NEF, 2 CR2, 2 ARW, 2 CR3, 1 NRW) đã có harness; benchmark đã đo synthetic 12MP cùng Canon HIF/CR3 thật và lưu trong `fixtures/benchmark-*.json`; HEIF/HEIC camera matrix đầy đủ, matrix model RAW đầy đủ và AI quality acceptance vẫn chưa nghiệm thu.

Chạy:

    python -m unittest discover -s tests -v
    python tools/run_p0.py
    python tools/benchmark_p0.py

Danh sach phan mem va binding can cho may phat trien Windows nam trong `../misa-image-editor-plan/DEPENDENCIES-WINDOWS.md`; P0 da cai va kiem tra cac wheel Python trong `requirements-p0.txt`.

Các binding Python P0 được ghim trong `requirements-p0.txt`; model AI không được đóng gói trong mốc này.

Build Windows shell:

    & 'C:\Program Files\dotnet\dotnet.exe' build .\windows\MisaImageEditor.slnx --configuration Release

Xem P0-REPORT.md để biết bằng chứng và giới hạn môi trường.

## English summary

**Status: P0 technical prototype; it is not a stable Windows release.** This prototype validates the catalog/collection/target-collection shortcut `B`, selective recipe copy/paste, JPEG/PNG float-RGB preview/export, EXIF orientation, raster local masks, and an offline MODNet mask spike. The `windows/` directory contains the WPF desktop shell, C# recipe contract, versioned JSON catalog smoke, and native worker Basic RGBA8 contract.

Run the Python checks with `python -m unittest discover -s tests -v`, `python tools/run_p0.py`, and `python tools/benchmark_p0.py`. Build the Windows shell with `& 'C:\Program Files\dotnet\dotnet.exe' build .\windows\MisaImageEditor.slnx --configuration Release`. See `P0-REPORT.md` for evidence and environment limits, and see `../misa-image-editor-plan/BILINGUAL-DOCUMENTATION.md` for the Vietnamese–English documentation policy.
