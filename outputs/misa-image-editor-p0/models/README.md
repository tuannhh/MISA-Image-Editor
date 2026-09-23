# P0 offline mask model

`modnet_photographic.onnx` is the MODNet Photographic ONNX weight published by the `yakhyo/modnet` project.

- Source repository: https://github.com/yakhyo/modnet
- Release URL: https://github.com/yakhyo/modnet/releases/download/weights/modnet_photographic.onnx
- Source documentation: the repository documents ONNX Runtime inference and Apache License 2.0 model weights.
- File size: 25,969,398 bytes
- SHA-256: `5069a5e306b9f5e9f4f2b0360264c9f8ea13b257c7c39943c7cf6a2ec3a102ae`

The P0 spike uses this model for offline portrait/subject matting through ONNX Runtime. It is a capability and integration check, not evidence that every scene or background is segmented accurately. Production quality acceptance still needs a representative image set and a separately recorded quality threshold.
