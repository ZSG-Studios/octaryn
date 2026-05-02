#pragma once

#include <SDL3/SDL.h>

#include "octaryn_client_frame_pacing.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef struct octaryn_client_swapchain_state
{
    SDL_GPUPresentMode present_mode;
}
octaryn_client_swapchain_state;

void octaryn_client_swapchain_state_init(octaryn_client_swapchain_state* state);
int octaryn_client_swapchain_configure(
    octaryn_client_swapchain_state* state,
    SDL_GPUDevice* device,
    SDL_Window* window,
    octaryn_client_frame_pacing* frame_pacing);
const char* octaryn_client_swapchain_present_mode_name(
    const octaryn_client_swapchain_state* state);
const char* octaryn_client_swapchain_present_mode_value_name(
    SDL_GPUPresentMode present_mode);

#ifdef __cplusplus
}
#endif
