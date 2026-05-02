#pragma once

#include <SDL3/SDL.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef enum octaryn_client_present_mode_policy
{
    OCTARYN_CLIENT_PRESENT_MODE_POLICY_AUTO = 0,
    OCTARYN_CLIENT_PRESENT_MODE_POLICY_IMMEDIATE = 1,
    OCTARYN_CLIENT_PRESENT_MODE_POLICY_MAILBOX = 2,
    OCTARYN_CLIENT_PRESENT_MODE_POLICY_VSYNC = 3,
}
octaryn_client_present_mode_policy;

typedef enum octaryn_client_swapchain_acquire_mode
{
    OCTARYN_CLIENT_SWAPCHAIN_ACQUIRE_EARLY = 0,
    OCTARYN_CLIENT_SWAPCHAIN_ACQUIRE_LATE = 1,
    OCTARYN_CLIENT_SWAPCHAIN_ACQUIRE_NONBLOCKING = 2,
}
octaryn_client_swapchain_acquire_mode;

typedef struct octaryn_client_frame_pacing
{
    octaryn_client_present_mode_policy requested_present_mode;
    SDL_GPUPresentMode actual_present_mode;
    octaryn_client_swapchain_acquire_mode acquire_mode;
    int fps_cap;
    int fps_cap_spin_us;
    int allowed_frames_in_flight;
    int swapchain_unavailable_sleep_us;
    Uint64 next_frame_target_ticks;
}
octaryn_client_frame_pacing;

void octaryn_client_frame_pacing_init(octaryn_client_frame_pacing* state);
void octaryn_client_frame_pacing_set_actual_present_mode(
    octaryn_client_frame_pacing* state,
    SDL_GPUPresentMode present_mode);
SDL_GPUPresentMode octaryn_client_frame_pacing_choose_present_mode(
    const octaryn_client_frame_pacing* state,
    SDL_GPUDevice* device,
    SDL_Window* window);
int octaryn_client_frame_pacing_should_defer_swapchain_acquire(
    const octaryn_client_frame_pacing* state);
int octaryn_client_frame_pacing_should_probe_swapchain_before_scene(
    const octaryn_client_frame_pacing* state);
float octaryn_client_frame_pacing_sleep_until_next_frame(
    octaryn_client_frame_pacing* state,
    Uint64 frame_start_ticks);
float octaryn_client_frame_pacing_sleep_after_swapchain_unavailable(
    const octaryn_client_frame_pacing* state);
const char* octaryn_client_frame_pacing_present_policy_name(
    octaryn_client_present_mode_policy policy);
const char* octaryn_client_frame_pacing_acquire_mode_name(
    octaryn_client_swapchain_acquire_mode mode);

#ifdef __cplusplus
}
#endif
