#include "Resources/RayMarchingCore3D.hlsl"
#include "Resources/DistanceEstimators.hlsl"

float TableLike(float3 s, float radius, float thickness, float height, float baseRadius, float baseThickness, float supportThickness){
    float main = 1E100;
    
    //table top
    Add(main, SlicedSphereI(s, float3(0, height, 0), radius, thickness));

    //table support
    float support = 1E100;
    Add(support, Capsule(s, float3(0, height, 0), float3(0, 0, 0), supportThickness));
    Intersect(support, PlaneSigned(s, float3(0, height, 0), float3(0, -1, 0)));
    Intersect(support, PlaneSigned(s, float3(0, 0, 0), float3(0, 1, 0)));
    Add(support, SlicedSphereI(s, float3(0, baseThickness, 0), baseRadius, baseThickness));

    Add(main, support);

    return main;
}

float TableLike(float3 s, float3 pos, float radius, float thickness, float height, float baseRadius, float baseThickness, float supportThickness){
    return TableLike(s - pos, radius, thickness, height, baseRadius, baseThickness, supportThickness);
}

float SampleDistance(float3 s)
{
    float main = 1E100;

    //base room
    Add(main, CubeI(s, float3(-0.1, 1-0.1, -0.1), float3(2.1, 1.1, 2.1)));
    Intersect(main, -CubeI(s, float3(0.1, 1+0.1, 0.1), float3(2.1, 1.1, 2.1)));


    //table
    Add(main, TableLike(s, float3(0, 0, 0), 0.5, 0.02, 0.76, 0.2, 0.05, 0.05));

    //chairs
    Add(main, TableLike(s, float3(-0.5, 0, 0), 0.2, 0.02, 0.43, 0.15, 0.02, 0.03));
    Add(main, TableLike(s, float3( 0.2, 0, 0.3), 0.2, 0.02, 0.43, 0.15, 0.02, 0.03));
    Add(main, TableLike(s, float3(0.3, 0, -0.4), 0.2, 0.02, 0.43, 0.15, 0.02, 0.03));

    return main;
}