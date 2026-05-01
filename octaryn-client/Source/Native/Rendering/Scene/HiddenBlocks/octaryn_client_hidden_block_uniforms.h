#pragma once

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

#define OCTARYN_CLIENT_HIDDEN_BLOCK_CAPACITY 32u

typedef struct octaryn_client_hidden_block_position
{
    int32_t x;
    int32_t y;
    int32_t z;
} octaryn_client_hidden_block_position;

typedef struct octaryn_client_hidden_block_uniforms
{
    uint32_t count;
    int32_t pad[3];
    int32_t blocks[OCTARYN_CLIENT_HIDDEN_BLOCK_CAPACITY][4];
} octaryn_client_hidden_block_uniforms;

void octaryn_client_hidden_block_uniforms_clear(octaryn_client_hidden_block_uniforms* uniforms);
void octaryn_client_hidden_block_uniforms_fill(
    octaryn_client_hidden_block_uniforms* uniforms,
    const octaryn_client_hidden_block_position* positions,
    uint32_t position_count);

#ifdef __cplusplus
}
#endif
