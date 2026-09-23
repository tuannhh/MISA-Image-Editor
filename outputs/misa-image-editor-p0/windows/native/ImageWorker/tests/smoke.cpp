#include "misa/image_worker.h"

#include <cassert>
#include <cstdint>
#include <string_view>

int main() {
    const misa::image::RenderPlan valid{1, 1, 0.5F, 10.0F, 20.0F, -15.0F, 25.0F, -20.0F, -5.0F};
    const misa::image::RenderPlan invalid{1, 1, 0.0F, 0.0F, 101.0F, 0.0F, 0.0F, 0.0F, 0.0F};
    assert(misa::image::validate_render_plan(valid));
    assert(!misa::image::validate_render_plan(invalid));
    std::uint8_t pixels[] = {64, 128, 192, 77, 255, 0, 0, 33};
    assert(misa::image::apply_basic_rgba8(valid, pixels, 2));
    assert(pixels[3] == 77 && pixels[7] == 33);
    assert(pixels[0] != 64 || pixels[1] != 128 || pixels[2] != 192);
    assert(!misa::image::apply_basic_rgba8(invalid, pixels, 2));
    assert(!misa::image::apply_basic_rgba8(valid, nullptr, 1));
    assert(std::string_view(misa::image::engine_version()) == "p0-contract-1");
    return 0;
}
