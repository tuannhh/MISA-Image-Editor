# Handoff stable — mẫu chưa điền
Không dùng mẫu này như bằng chứng đã phát hành.

## Nhận dạng
- Version / ngày / người nghiệm thu:
- Git tag bất biến / commit SHA / repository:
- Installer path / SHA-256 / trạng thái chữ ký:
- Windows, CPU/GPU/RAM, driver đã kiểm thử:
- SDK/toolchain/dependency lock / lệnh build từ clean checkout:
- SchemaVersion / processVersion / lens DB hash / model hash:

## Phạm vi đã nghiệm thu
- Luồng mới hoạt động:
- Tính năng cũ regression pass:
- File test report + fixture manifest/hash:
- Lỗi còn lại: severity, cách tái hiện, workaround:
- Camera/lens/codec đã chứng nhận và phần chưa hỗ trợ:
- Số đo latency/throughput/peak RAM và chất lượng ảnh/mask:

## Cài đặt và dữ liệu
- Cách cài/mở/uninstall:
- Vị trí catalog/durable assets/cache/log:
- Backup trước upgrade, cách kiểm tra restore:
- Migration từ phiên bản nào, thời gian, thay đổi không tương thích:
- Catalog backup có/không gồm ảnh gốc:

## Quay về bản trước
1. Đóng app mới, sao lưu riêng dữ liệu hiện tại để giữ chỉnh sửa mới.
2. Dùng installer bản stable trước có hash xác minh.
3. Restore bản catalog + assets/profile/model tương ứng trước migration.
4. Relink nguồn nếu cần; kiểm tra library, preset, batch và export mẫu.
5. Ghi rõ chỉnh sửa sau upgrade nào không có trong backup; không hứa downgrade tự bảo toàn chúng.
Không chạy app cũ ghi vào schema mới. Không rollback chỉ bằng copy .exe.

## Điều kiện bắt đầu tính năng tiếp
- Stable artifact có thể tải/mở và đã thử trên máy sạch.
- Không còn lỗi chặn; người dùng đã thử luồng thực.
- CURRENT, progress, activeContext và decision log đã cập nhật.
- Nhánh tính năng mới tách từ tag này, dùng catalog test copy.
- Scope mốc tiếp / việc đầu tiên / rủi ro / người chịu trách nhiệm:
