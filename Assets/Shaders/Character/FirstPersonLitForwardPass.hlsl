#ifndef LIGHT_TOWER_FIRST_PERSON_LIT_FORWARD_PASS_INCLUDED
#define LIGHT_TOWER_FIRST_PERSON_LIT_FORWARD_PASS_INCLUDED

#define LitPassFragment LightTowerOriginalLitPassFragment
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
#undef LitPassFragment

#include "Assets/Shaders/Character/FirstPersonProximityClip.hlsl"

void LightTowerFirstPersonLitPassFragment(
    Varyings input,
    out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    LightTowerApplyFirstPersonClip(input.positionWS);
    LightTowerOriginalLitPassFragment(
        input,
        outColor
#ifdef _WRITE_RENDERING_LAYERS
        , outRenderingLayers
#endif
    );
}

#endif
