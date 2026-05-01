#include "octaryn_client_renderdoc_capture.h"

#include <cstdint>
#include <cstdlib>

#if defined(_WIN32)
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windows.h>
#else
#include <dlfcn.h>
#endif

#include "octaryn_native_log.h"
#include "octaryn_native_profile.h"

namespace {

using renderdoc_trigger_capture_fn = void (*)();

struct renderdoc_api_v100
{
    void (*GetAPIVersion)(int*, int*, int*);
    void (*SetCaptureOptionU32)(int, std::uint32_t);
    void (*SetCaptureOptionF32)(int, float);
    std::uint32_t (*GetCaptureOptionU32)(int);
    float (*GetCaptureOptionF32)(int);
    void (*SetFocusToggleKeys)(void*, int);
    void (*SetCaptureKeys)(void*, int);
    std::uint32_t (*GetOverlayBits)();
    void (*MaskOverlayBits)(std::uint32_t, std::uint32_t);
    void (*Shutdown)();
    void (*UnloadCrashHandler)();
    void (*SetLogFilePathTemplate)(const char*);
    const char* (*GetLogFilePathTemplate)();
    std::uint32_t (*GetNumCaptures)();
    std::uint32_t (*GetCapture)(std::uint32_t, char*, std::uint32_t*, std::uint64_t*);
    renderdoc_trigger_capture_fn TriggerCapture;
};

using renderdoc_get_api_fn = int (*)(std::uint32_t, void**);

constexpr std::uint32_t RenderDocApiVersion100 = 10000u;
constexpr std::uint32_t DefaultCaptureFrame = 500u;
constexpr std::uint32_t MinimumCaptureFrame = 1u;
constexpr std::uint32_t MaximumCaptureFrame = 1000000u;

bool renderdoc_enabled = false;
bool renderdoc_configured = false;
bool renderdoc_unavailable_logged = false;
bool renderdoc_triggered = false;
std::uint32_t capture_frame = DefaultCaptureFrame;
std::uint32_t render_frame = 0u;
renderdoc_api_v100* renderdoc_api = nullptr;

auto ascii_lower(char value) -> char
{
    if (value >= 'A' && value <= 'Z')
    {
        return static_cast<char>(value - 'A' + 'a');
    }
    return value;
}

auto ascii_equal_ignore_case(const char* left, const char* right) -> bool
{
    if (left == nullptr || right == nullptr)
    {
        return false;
    }

    while (*left != '\0' && *right != '\0')
    {
        if (ascii_lower(*left) != ascii_lower(*right))
        {
            return false;
        }
        ++left;
        ++right;
    }

    return *left == '\0' && *right == '\0';
}

auto env_flag_enabled(const char* name) -> bool
{
    const char* value = std::getenv(name);
    if (value == nullptr || value[0] == '\0')
    {
        return false;
    }

    return !ascii_equal_ignore_case(value, "0") &&
        !ascii_equal_ignore_case(value, "false") &&
        !ascii_equal_ignore_case(value, "off") &&
        !ascii_equal_ignore_case(value, "no");
}

auto env_uint_value(
    const char* name,
    std::uint32_t fallback,
    std::uint32_t minimum,
    std::uint32_t maximum) -> std::uint32_t
{
    const char* value = std::getenv(name);
    if (value == nullptr || value[0] == '\0')
    {
        return fallback;
    }

    char* end = nullptr;
    const unsigned long parsed = std::strtoul(value, &end, 10);
    if (end == value)
    {
        return fallback;
    }
    if (parsed < static_cast<unsigned long>(minimum))
    {
        return minimum;
    }
    if (parsed > static_cast<unsigned long>(maximum))
    {
        return maximum;
    }
    return static_cast<std::uint32_t>(parsed);
}

auto find_renderdoc_api() -> renderdoc_api_v100*
{
#if defined(_WIN32)
    HMODULE renderdoc_module = GetModuleHandleA("renderdoc.dll");
    if (renderdoc_module == nullptr)
    {
        return nullptr;
    }
    FARPROC symbol = GetProcAddress(renderdoc_module, "RENDERDOC_GetAPI");
#else
    void* symbol = dlsym(RTLD_DEFAULT, "RENDERDOC_GetAPI");
#endif
    if (symbol == nullptr)
    {
        return nullptr;
    }

#if defined(_WIN32) && defined(__GNUC__)
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wcast-function-type"
#endif
    auto get_api = reinterpret_cast<renderdoc_get_api_fn>(symbol);
#if defined(_WIN32) && defined(__GNUC__)
#pragma GCC diagnostic pop
#endif

    void* api = nullptr;
    if (get_api(RenderDocApiVersion100, &api) != 1 || api == nullptr)
    {
        return nullptr;
    }
    return static_cast<renderdoc_api_v100*>(api);
}

} // namespace

void octaryn_client_renderdoc_capture_init(void)
{
    if (renderdoc_configured)
    {
        return;
    }

    renderdoc_configured = true;
    renderdoc_enabled = env_flag_enabled("OCTARYN_RENDERDOC_CAPTURE");
    if (!renderdoc_enabled)
    {
        return;
    }

    capture_frame =
        env_uint_value("OCTARYN_RENDERDOC_CAPTURE_FRAME", DefaultCaptureFrame, MinimumCaptureFrame, MaximumCaptureFrame);
    renderdoc_api = find_renderdoc_api();
    if (renderdoc_api == nullptr || renderdoc_api->TriggerCapture == nullptr)
    {
        octaryn_native_log_warnf("RenderDoc auto capture requested but RENDERDOC_GetAPI is unavailable");
        renderdoc_unavailable_logged = true;
        return;
    }

    octaryn_native_log_infof("RenderDoc auto capture enabled: frame=%u", capture_frame);
}

void octaryn_client_renderdoc_capture_before_render(void)
{
    if (!renderdoc_enabled || renderdoc_triggered)
    {
        return;
    }
    OCTARYN_NATIVE_PROFILE_ZONE("renderdoc_capture_before_render");

    ++render_frame;
    if (renderdoc_api == nullptr)
    {
        renderdoc_api = find_renderdoc_api();
        if (renderdoc_api == nullptr || renderdoc_api->TriggerCapture == nullptr)
        {
            if (!renderdoc_unavailable_logged)
            {
                octaryn_native_log_warnf("RenderDoc auto capture waiting for RENDERDOC_GetAPI");
                renderdoc_unavailable_logged = true;
            }
            return;
        }
        octaryn_native_log_infof("RenderDoc auto capture connected: frame=%u", capture_frame);
    }

    if (render_frame < capture_frame)
    {
        return;
    }

    renderdoc_api->TriggerCapture();
    renderdoc_triggered = true;
    octaryn_native_log_infof("RenderDoc auto capture triggered: frame=%u", render_frame);
}
