#include "misa/image_worker.h"

#include <algorithm>
#include <cmath>

namespace misa::image {

bool validate_render_plan(const RenderPlan& plan) noexcept {
    if (plan.schema_version != 1 || plan.process_version != 1) return false;
    if (!std::isfinite(plan.exposure) || !std::isfinite(plan.contrast) ||
        !std::isfinite(plan.highlights) || !std::isfinite(plan.shadows) ||
        !std::isfinite(plan.whites) || !std::isfinite(plan.blacks) ||
        !std::isfinite(plan.saturation)) return false;
    return plan.exposure >= -5.0F && plan.exposure <= 5.0F &&
           plan.contrast >= -100.0F && plan.contrast <= 100.0F &&
           plan.highlights >= -100.0F && plan.highlights <= 100.0F &&
           plan.shadows >= -100.0F && plan.shadows <= 100.0F &&
           plan.whites >= -100.0F && plan.whites <= 100.0F &&
           plan.blacks >= -100.0F && plan.blacks <= 100.0F &&
           plan.saturation >= -100.0F && plan.saturation <= 100.0F;
}

bool apply_basic_rgba8(const RenderPlan& plan, std::uint8_t* rgba, std::size_t count) noexcept {
    if (!validate_render_plan(plan) || (count > 0 && rgba == nullptr)) return false;

    const float exposure_gain = std::exp2(plan.exposure);
    const float contrast_gain = (plan.contrast + 100.0F) / 100.0F;
    const float saturation_gain = (plan.saturation + 100.0F) / 100.0F;
    const float shadows_amount = plan.shadows / 100.0F;
    const float highlights_amount = plan.highlights / 100.0F;
    const float whites_amount = plan.whites / 100.0F;
    const float blacks_amount = plan.blacks / 100.0F;
    for (std::size_t i = 0; i < count; ++i) {
        auto* pixel = rgba + i * 4;
        float r = static_cast<float>(pixel[0]) / 255.0F;
        float g = static_cast<float>(pixel[1]) / 255.0F;
        float b = static_cast<float>(pixel[2]) / 255.0F;

        r *= exposure_gain;
        g *= exposure_gain;
        b *= exposure_gain;
        r = (r - 0.5F) * contrast_gain + 0.5F;
        g = (g - 0.5F) * contrast_gain + 0.5F;
        b = (b - 0.5F) * contrast_gain + 0.5F;
        const float luma = 0.2126F * r + 0.7152F * g + 0.0722F * b;
        const float shadow_weight = std::clamp(1.0F - luma * 2.0F, 0.0F, 1.0F);
        const float highlight_weight = std::clamp((luma - 0.5F) * 2.0F, 0.0F, 1.0F);
        const float white_weight = std::clamp((luma - 0.65F) / 0.35F, 0.0F, 1.0F);
        const float black_weight = std::clamp((0.35F - luma) / 0.35F, 0.0F, 1.0F);
        const float tone_delta = shadow_weight * shadows_amount * 0.35F +
                                 highlight_weight * highlights_amount * 0.35F +
                                 white_weight * whites_amount * 0.25F +
                                 black_weight * blacks_amount * 0.25F;
        r += tone_delta;
        g += tone_delta;
        b += tone_delta;
        r = luma + (r - luma) * saturation_gain;
        g = luma + (g - luma) * saturation_gain;
        b = luma + (b - luma) * saturation_gain;

        pixel[0] = static_cast<std::uint8_t>(std::lround(std::clamp(r, 0.0F, 1.0F) * 255.0F));
        pixel[1] = static_cast<std::uint8_t>(std::lround(std::clamp(g, 0.0F, 1.0F) * 255.0F));
        pixel[2] = static_cast<std::uint8_t>(std::lround(std::clamp(b, 0.0F, 1.0F) * 255.0F));
    }
    return true;
}

const char* engine_version() noexcept { return "p0-contract-1"; }

} // namespace misa::image
