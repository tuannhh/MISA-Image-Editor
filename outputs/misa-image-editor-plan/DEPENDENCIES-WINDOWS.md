# MISA Image Editor — dependencies Windows/P0

## Đã có trên máy kiểm thử

| Thành phần | Phiên bản/trạng thái | Vai trò |
|---|---|---|
| Windows x64 | Môi trường Windows hiện tại | Chạy WPF desktop và native worker |
| .NET SDK | 10.0.401 | Build WPF/Domain/smoke |
| .NET Windows Desktop Runtime | 10.0.12 | Chạy WPF khi publish framework-dependent |
| Visual Studio 2022 Build Tools | MSVC 14.44.35207 + MSBuild 17.14.60 | Biên dịch C++20/native worker |
| CMake | 4.4.3 | Configure/build/test native worker |
| Python bundled x64 | 3.12.14 | Chạy harness P0 và fixture verifier |
| Git | 2.55.0.windows.5 | Quản lý mã nguồn/fixture khi cần |

Các binding Python P0 đã được cài vào runtime bundled:

```text
numpy==2.5.3
Pillow==12.3.0
pillow-heif==1.7.0
rawpy==0.27.1
onnxruntime==1.30.0
lensfunpy==1.18.0
```

ONNX Runtime hiện load được `AzureExecutionProvider` và `CPUExecutionProvider`. CPU là đường chạy bắt buộc; GPU/DirectML chỉ là tối ưu sau này.

## Nếu cài máy phát triển mới

1. Windows 10/11 64-bit và Python 3.12 x64.
2. .NET 10 SDK; workload Windows Desktop/WPF.
3. Visual Studio 2022 Build Tools với **Desktop development with C++**, MSVC x64/x86 và Windows SDK.
4. CMake từ 3.24 trở lên.
5. Cài các binding Python theo `requirements-p0.txt`. Các wheel Windows x64 phải khớp Python/kiến trúc; không dùng Python 32-bit.
6. Lensfun database phải được đóng gói cùng ứng dụng hoặc cài vào thư mục dữ liệu ứng dụng trước khi bật profile lens. P0 đã kiểm tra được 9/13 profile yêu cầu; 4 biến thể vẫn thiếu dữ liệu exact.

LibRaw/libheif được dùng qua binding/wheel trong P0. Native production worker sẽ cần chốt cách đóng gói codec, Lensfun và LittleCMS trước khi tạo installer; không cần cài Adobe Lightroom hay Photoshop.

## Máy người dùng cuối

Python, CMake, Visual Studio và Git không nên là điều kiện chạy bản phát hành. Bản publish nên chọn self-contained WPF hoặc kèm .NET Windows Desktop Runtime; native worker phải đi cùng DLL/runtime C++ tương ứng. Model `models/modnet_photographic.onnx` chỉ cần đóng gói nếu bật subject-mask offline; manual/rectangle/raster mask không phụ thuộc model.

## Kết quả kiểm tra sau cài

- Python smoke: 20/20 test qua.
- .NET Release: 0 warning, 0 error; Domain catalog/recipe smoke qua.
- Native CMake/MSVC: ctest 1/1 qua.
- WPF startup smoke: cửa sổ `MISA Image Editor - P0` phản hồi.

Thông tin máy và phiên bản được ghi lại bằng `tools/verify_dependencies.py` tại `fixtures/dependency-verification.json`.

P0 vẫn là prototype, chưa phải installer ổn định: cần hoàn thiện UI mask, camera/HEIF matrix, lens profile còn thiếu, chất lượng AI trên bộ ảnh đại diện, IPC/catalog SQLite production và packaging.
