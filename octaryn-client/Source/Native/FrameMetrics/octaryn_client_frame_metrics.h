#pragma once

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

#define OCTARYN_CLIENT_FRAME_METRICS_HISTOGRAM_BINS 1024u

typedef struct octaryn_client_frame_metric_pair
{
    float ms;
    float fps;
} octaryn_client_frame_metric_pair;

typedef struct octaryn_client_frame_metrics_snapshot
{
    octaryn_client_frame_metric_pair current;
    octaryn_client_frame_metric_pair average;
    octaryn_client_frame_metric_pair low_1pct;
    octaryn_client_frame_metric_pair low_0_1pct;
    octaryn_client_frame_metric_pair confirmed_low_5;
    octaryn_client_frame_metric_pair confirmed_low_10;
    octaryn_client_frame_metric_pair worst;
    float warmup_elapsed_seconds;
    float warmup_seconds;
    uint64_t sample_count;
    uint32_t confirmed_low_5_hits;
    uint32_t confirmed_low_10_hits;
    uint8_t warmup_complete;
} octaryn_client_frame_metrics_snapshot;

typedef struct octaryn_client_frame_metrics
{
    uint64_t first_sample_ticks;
    uint64_t last_sample_ticks;
    uint64_t current_window_start_ticks;
    uint64_t current_window_sample_count;
    double current_window_total_ms;
    octaryn_client_frame_metric_pair current;
    uint64_t sample_count;
    double total_ms;
    float worst_ms;
    uint32_t histogram[OCTARYN_CLIENT_FRAME_METRICS_HISTOGRAM_BINS];
} octaryn_client_frame_metrics;

void octaryn_client_frame_metrics_init(octaryn_client_frame_metrics* metrics);
void octaryn_client_frame_metrics_record(
    octaryn_client_frame_metrics* metrics,
    float frame_ms,
    uint64_t sample_ticks);
octaryn_client_frame_metrics_snapshot octaryn_client_frame_metrics_snapshot_value(
    const octaryn_client_frame_metrics* metrics,
    uint64_t now_ticks);

#ifdef __cplusplus
}
#endif
