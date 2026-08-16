#ifndef LIGHT_TOWER_FIRST_PERSON_PROXIMITY_CLIP_INCLUDED
#define LIGHT_TOWER_FIRST_PERSON_PROXIMITY_CLIP_INCLUDED

float4 _LightTowerFirstPersonClipCenterRadius;
float _LightTowerFirstPersonClipEnabled;

inline void LightTowerApplyFirstPersonClip(float3 positionWS)
{
    if (_LightTowerFirstPersonClipEnabled < 0.5)
        return;

    float3 cameraOffset = positionWS - _LightTowerFirstPersonClipCenterRadius.xyz;
    float radius = max(0.001, _LightTowerFirstPersonClipCenterRadius.w);
    clip(dot(cameraOffset, cameraOffset) - radius * radius);
}

#endif
