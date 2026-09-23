#pragma once

#include <cstddef>
#include <cstdint>

namespace misa::image {

struct RenderPlan {
    std::int32_t schema_version;
    std::int32_t process_version;
    float exposure;
    float contrast;
    float highlights;
    float shadows;
    float whites;
    float blacks;
    float saturation;
};

// P0 ABI boundary: validation plus a small deterministic Basic pixel path.
bool validate_render_plan(const RenderPlan& plan) noexcept;
// Applies the P0 Basic tone fields to premultiplied-independent RGBA8 pixels.
// The buffer is interleaved RGBA, count is the number of pixels, and alpha is preserved.
bool apply_basic_rgba8(const RenderPlan& plan, std::uint8_t* rgba, std::size_t count) noexcept;
const char* engine_version() noexcept;

} // namespace misa::image
