#pragma once

#include "octaryn_client_app_settings.h"

#if defined(OCTARYN_CLIENT_DISPLAY_SETTINGS_USE_SDL3)
#include <SDL3/SDL.h>
#else
#include <stdint.h>

typedef uint32_t SDL_DisplayID;
typedef struct SDL_Window SDL_Window;
#endif

#ifdef __cplusplus
extern "C" {
#endif

int octaryn_client_display_settings_display_index(SDL_DisplayID display);
void octaryn_client_display_settings_capture(
    octaryn_client_app_settings* settings,
    SDL_Window* window);
SDL_DisplayID octaryn_client_display_settings_resolve_display(
    const octaryn_client_app_settings* settings);
int octaryn_client_display_settings_restore_window(
    SDL_Window* window,
    const octaryn_client_app_settings* settings);

#ifdef __cplusplus
}
#endif
