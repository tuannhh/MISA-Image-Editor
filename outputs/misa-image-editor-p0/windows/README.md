# MISA Image Editor — Windows P0 shell

This folder contains the first Windows desktop shell for the P0 contract. It is intentionally a small, reviewable slice:

- WPF `Library` and `Editor` tabs with folder import, image thumbnails, recent-import list, collection dialog (include selected/set target), seven Basic tone sliders with JPEG/PNG float-style preview feedback, copy/paste affordances, and deferred panels for Tone Curve, Color Mixer, Lens Correction, Crop/Transform, and Mask.
- C# recipe records and a versioned JSON catalog store shared by the desktop layer and future workers. The store persists assets, target collection, last import, and recipes without adding a vulnerable native SQLite package to the WPF shell.
- A native C++20 worker contract under `native/ImageWorker`, with recipe validation and a deterministic seven-field Basic RGBA8 pixel path covered by a CMake/MSVC smoke target.

## Build

Use the installed .NET SDK directly when `dotnet` is not on the current shell PATH:

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' build .\MisaImageEditor.slnx --configuration Release
```

The expected result is 0 warnings and 0 errors. The executable is written to:

`src\MisaImageEditor.Desktop\bin\Release\net10.0-windows\MisaImageEditor.Desktop.exe`

The solution also includes `MisaImageEditor.Domain.Smoke`; run it with:

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' run --project .\src\MisaImageEditor.Domain.Smoke\MisaImageEditor.Domain.Smoke.csproj --configuration Release --no-build
```

Build the native contract from a Visual Studio Developer Command Prompt (or call `VsDevCmd.bat` first):

```powershell
cmake -S .\native\ImageWorker -B .\native\ImageWorker\build-msvc -G "NMake Makefiles" -DCMAKE_BUILD_TYPE=Release
cmake --build .\native\ImageWorker\build-msvc
ctest --test-dir .\native\ImageWorker\build-msvc --output-on-failure
```

## P0 boundary

JPEG and PNG previews are supported in the shell. HEIC and camera RAW extensions are recognized and shown as deferred codec work in this WPF slice; the Python oracle has separate HEIC8/HEIF10 codec evidence plus ten external RAW decode/render fixtures (4 NEF, 2 CR2, 2 ARW, 2 CR3, 1 NRW). The native seven-field Basic function is a contract smoke, not production decode/render. Lens calibration, AI masks, installer packaging, and a stable release remain later gates in the roadmap.
