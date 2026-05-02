#pragma once

#ifdef __cplusplus
extern "C" {
#endif

typedef enum octaryn_client_camera_projection
{
    OCTARYN_CLIENT_CAMERA_PROJECTION_ORTHOGRAPHIC = 0,
    OCTARYN_CLIENT_CAMERA_PROJECTION_PERSPECTIVE = 1,
}
octaryn_client_camera_projection;

typedef struct octaryn_client_camera
{
    octaryn_client_camera_projection projection_mode;
    float view[4][4];
    float projection[4][4];
    float view_projection[4][4];
    float frustum_planes[6][4];
    float relative_frustum_planes[6][4];
    float position[3];
    float pitch_radians;
    float yaw_radians;
    int viewport_width;
    int viewport_height;
    float vertical_field_of_view_radians;
    int zoom_step;
    float near_plane;
    float far_plane;
    float orthographic_size;
}
octaryn_client_camera;

void octaryn_client_camera_init(
    octaryn_client_camera* camera,
    octaryn_client_camera_projection projection_mode);
void octaryn_client_camera_update(octaryn_client_camera* camera);
void octaryn_client_camera_move(octaryn_client_camera* camera, float x, float y, float z);
void octaryn_client_camera_resize(octaryn_client_camera* camera, int width, int height);
void octaryn_client_camera_rotate_degrees(octaryn_client_camera* camera, float pitch, float yaw);
void octaryn_client_camera_cycle_zoom(octaryn_client_camera* camera);
void octaryn_client_camera_forward_vector(
    const octaryn_client_camera* camera,
    float* x,
    float* y,
    float* z);
int octaryn_client_camera_is_box_visible(
    const octaryn_client_camera* camera,
    float x,
    float y,
    float z,
    float width,
    float height,
    float depth);

#ifdef __cplusplus
}
#endif
