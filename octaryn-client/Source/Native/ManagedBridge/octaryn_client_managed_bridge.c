#define OCTARYN_ABI_BUILD
#include "octaryn_client_host_exports.h"

#include <coreclr_delegates.h>
#include <dlfcn.h>
#include <hostfxr.h>
#include <stddef.h>
#include <string.h>

enum {
    OCTARYN_CLIENT_BRIDGE_LOAD_FAILED = -100
};

typedef int (OCTARYN_ABI_CALL* octaryn_client_initialize_fn)(octaryn_client_native_host_api* native_api);
typedef int (OCTARYN_ABI_CALL* octaryn_client_tick_fn)(octaryn_host_frame_snapshot* frame_snapshot);
typedef void (OCTARYN_ABI_CALL* octaryn_client_shutdown_fn)(void);

static octaryn_client_initialize_fn s_initialize;
static octaryn_client_tick_fn s_tick;
static octaryn_client_shutdown_fn s_shutdown;
static int s_load_result;

static void* octaryn_load_symbol(void* library, const char* symbol)
{
    return dlsym(library, symbol);
}

static int octaryn_load_hostfxr_symbol(void* library, const char* symbol, void* target, size_t target_size)
{
    void* address = octaryn_load_symbol(library, symbol);
    if (address == NULL) {
        return 0;
    }

    memcpy(target, &address, target_size);
    return 1;
}

static int octaryn_resolve_managed_method(
    load_assembly_and_get_function_pointer_fn load_assembly,
    const char_t* type_name,
    const char_t* method_name,
    void** target)
{
    return load_assembly(
        OCTARYN_CLIENT_MANAGED_ASSEMBLY_PATH,
        type_name,
        method_name,
        UNMANAGEDCALLERSONLY_METHOD,
        NULL,
        target);
}

static int octaryn_client_load_managed_exports(void)
{
    if (s_initialize != NULL && s_tick != NULL && s_shutdown != NULL) {
        return 0;
    }

    if (s_load_result != 0) {
        return s_load_result;
    }

    void* hostfxr = dlopen(OCTARYN_DOTNET_HOSTFXR_PATH, RTLD_NOW | RTLD_LOCAL);
    if (hostfxr == NULL) {
        s_load_result = OCTARYN_CLIENT_BRIDGE_LOAD_FAILED;
        return s_load_result;
    }

    hostfxr_initialize_for_runtime_config_fn initialize_for_runtime_config = NULL;
    hostfxr_get_runtime_delegate_fn get_runtime_delegate = NULL;
    hostfxr_close_fn close_host_context = NULL;

    octaryn_load_hostfxr_symbol(
        hostfxr,
        "hostfxr_initialize_for_runtime_config",
        &initialize_for_runtime_config,
        sizeof(initialize_for_runtime_config));
    octaryn_load_hostfxr_symbol(
        hostfxr,
        "hostfxr_get_runtime_delegate",
        &get_runtime_delegate,
        sizeof(get_runtime_delegate));
    octaryn_load_hostfxr_symbol(
        hostfxr,
        "hostfxr_close",
        &close_host_context,
        sizeof(close_host_context));

    if (initialize_for_runtime_config == NULL || get_runtime_delegate == NULL || close_host_context == NULL) {
        s_load_result = OCTARYN_CLIENT_BRIDGE_LOAD_FAILED;
        return s_load_result;
    }

    hostfxr_handle host_context = NULL;
    int result = initialize_for_runtime_config(OCTARYN_CLIENT_RUNTIME_CONFIG_PATH, NULL, &host_context);
    if (result < 0 || host_context == NULL) {
        s_load_result = result < 0 ? result : OCTARYN_CLIENT_BRIDGE_LOAD_FAILED;
        return s_load_result;
    }

    load_assembly_and_get_function_pointer_fn load_assembly = NULL;
    result = get_runtime_delegate(
        host_context,
        hdt_load_assembly_and_get_function_pointer,
        (void**)&load_assembly);
    close_host_context(host_context);
    if (result < 0 || load_assembly == NULL) {
        s_load_result = result < 0 ? result : OCTARYN_CLIENT_BRIDGE_LOAD_FAILED;
        return s_load_result;
    }

    result = octaryn_resolve_managed_method(
        load_assembly,
        "Octaryn.Client.ClientHostExports, Octaryn.Client",
        "Initialize",
        (void**)&s_initialize);
    if (result < 0 || s_initialize == NULL) {
        s_load_result = result < 0 ? result : OCTARYN_CLIENT_BRIDGE_LOAD_FAILED;
        return s_load_result;
    }

    result = octaryn_resolve_managed_method(
        load_assembly,
        "Octaryn.Client.ClientHostExports, Octaryn.Client",
        "Tick",
        (void**)&s_tick);
    if (result < 0 || s_tick == NULL) {
        s_load_result = result < 0 ? result : OCTARYN_CLIENT_BRIDGE_LOAD_FAILED;
        return s_load_result;
    }

    result = octaryn_resolve_managed_method(
        load_assembly,
        "Octaryn.Client.ClientHostExports, Octaryn.Client",
        "Shutdown",
        (void**)&s_shutdown);
    if (result < 0 || s_shutdown == NULL) {
        s_load_result = result < 0 ? result : OCTARYN_CLIENT_BRIDGE_LOAD_FAILED;
        return s_load_result;
    }

    return 0;
}

int OCTARYN_ABI_CALL octaryn_client_initialize(octaryn_client_native_host_api* native_api)
{
    int result = octaryn_client_load_managed_exports();
    if (result < 0) {
        return result;
    }

    return s_initialize(native_api);
}

int OCTARYN_ABI_CALL octaryn_client_tick(octaryn_host_frame_snapshot* frame_snapshot)
{
    int result = octaryn_client_load_managed_exports();
    if (result < 0) {
        return result;
    }

    return s_tick(frame_snapshot);
}

void OCTARYN_ABI_CALL octaryn_client_shutdown(void)
{
    if (s_shutdown != NULL) {
        s_shutdown();
    }
}
