#pragma once

#ifdef __cplusplus
extern "C" {
#endif

int octaryn_client_render_distance_option_count(void);
const int* octaryn_client_render_distance_options(void);
int octaryn_client_render_distance_sanitize(int distance);
int octaryn_client_render_distance_next_step(int current_distance, int target_distance);

#ifdef __cplusplus
}
#endif
