#pragma once

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct octaryn_client_lighting_settings
{
    uint8_t fog_enabled;
    float fog_distance;
    float skylight_floor;
    float ambient_strength;
    float sun_strength;
    float sun_fallback_strength;
} octaryn_client_lighting_settings;

void octaryn_client_lighting_settings_default(octaryn_client_lighting_settings* settings);
octaryn_client_lighting_settings octaryn_client_lighting_settings_default_value(void);
int octaryn_client_lighting_settings_sanitize(octaryn_client_lighting_settings* settings);

#ifdef __cplusplus
}
#endif
