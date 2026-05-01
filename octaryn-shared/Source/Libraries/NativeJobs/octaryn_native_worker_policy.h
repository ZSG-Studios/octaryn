#pragma once

#ifdef __cplusplus
extern "C" {
#endif

typedef enum octaryn_native_worker_mode
{
    OCTARYN_NATIVE_WORKER_MODE_AUTO,
    OCTARYN_NATIVE_WORKER_MODE_MANUAL,
}
octaryn_native_worker_mode;

typedef struct octaryn_native_worker_policy_sample
{
    int logical_cores;
    int render_pressure;
    int job_backlog;
    int upload_pressure;
    int simulation_pressure;
    int current_workers;
    int minimum_workers;
    int maximum_workers;
    int target_workers;
}
octaryn_native_worker_policy_sample;

int octaryn_native_worker_policy_minimum_workers(int logical_cores);
int octaryn_native_worker_policy_maximum_workers(int logical_cores, int configured_worker_limit);
int octaryn_native_worker_policy_target_workers(const octaryn_native_worker_policy_sample* sample);
octaryn_native_worker_policy_sample octaryn_native_worker_policy_sample_create(
    int logical_cores,
    int configured_worker_limit,
    int current_workers,
    int render_pressure,
    int job_backlog,
    int upload_pressure,
    int simulation_pressure);
octaryn_native_worker_mode octaryn_native_worker_mode_from_string(
    const char* value,
    octaryn_native_worker_mode fallback);
const char* octaryn_native_worker_mode_name(octaryn_native_worker_mode mode);

#ifdef __cplusplus
}
#endif
