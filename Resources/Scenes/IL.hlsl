#include "Spaces/Euclidean.hlsl"
#include "Shapes/Euclidean.hlsl"

// Stack for intermediate results
#define MAX_STACK_SIZE 8

struct Operation{
    int opcode;
    float4x4 transform;
    float4 parameters;
    float4 albedo; // Material color
};

/*
Opcodes:
0: Union
1: Intersection
2: Invert

3: Nothing (Empty space)
4: All (Solid space)
5: Sphere
6: Plane
7: Cube

8: Smooth Union
*/

cbuffer SceneConstants : register(b0)
{
    float4x4 camTransform;
};

RWStructuredBuffer<Operation> operations : register(u1);

// struct PixelCtx{
//     float sdfStack[MAX_STACK_SIZE];
//     int sdfStackTop;

//     HitResult hitStack[MAX_STACK_SIZE];
//     int hitStackTop;

//     uint opCount;
// };

// void InitPixelCtx(inout PixelCtx ctx, uint3 DTid){
//     ctx.sdfStackTop = -1;
//     ctx.hitStackTop = -1;
    
//     uint stride;
//     operations.GetDimensions(ctx.opCount, stride);
// }

// #define PIXEL_CONTEXT PixelCtx
// #define PIXEL_CONTEXT_INIT InitPixelCtx

#define EXTRA_PARAMS_DECL , \
    inout float sdfStack[MAX_STACK_SIZE],\
    inout int sdfStackTop,\
    inout HitResult hitStack[MAX_STACK_SIZE],\
    inout int hitStackTop

#define EXTRA_PARAMS , sdfStack, sdfStackTop, hitStack, hitStackTop

#define EXTRA_PARAMS_INIT \
    int sdfStackTop = -1; \
    float sdfStack[MAX_STACK_SIZE] = (float[MAX_STACK_SIZE])0; \
    int hitStackTop = -1; \
    HitResult hitStack[MAX_STACK_SIZE] = (HitResult[MAX_STACK_SIZE])0;

#define pop(stack) (stack[stack##Top--])
#define push(stack, value) stack[++stack##Top] = value


float GetDistance(float3 pos EXTRA_PARAMS_DECL){
    uint opCount;
    uint stride;
    operations.GetDimensions(opCount, stride);

    for(uint i = 0; i < opCount; i++){
        Operation op = operations[i];

        // For SDF evaluation, we need to transform world pos to object local space
        // This requires the INVERSE of the transform matrix
        // For orthogonal matrices (rotation + translation), transpose = inverse
        float3 p = mul(float4(pos, 1.0f), op.transform).xyz;

        switch(op.opcode){
            case 0: // Union
            {
                float b = pop(sdfStack);
                float a = pop(sdfStack);
                push(sdfStack, UnionSdf(a, b));
                break;
            }
            
            case 1: // Intersection
            {
                float b = pop(sdfStack);
                float a = pop(sdfStack);
                push(sdfStack, IntersectionSdf(a, b));
                break;
            }
            
            case 2: // Invert
            {
                float a = pop(sdfStack);
                push(sdfStack, -a);
                break;
            }
            
            case 3: // Nothing
            {
                push(sdfStack, NoneSdf());
                break;
            }
            
            case 4: // All
            {
                push(sdfStack, AllSdf());
                break;
            }
            
            case 5: // Sphere
            {
                float3 center = op.parameters.xyz;
                float radius = op.parameters.w;
                push(sdfStack, SphereSdf(p, center, radius));
                break;
            }
            
            case 6: // Plane
            {
                float3 normal = normalize(op.parameters.xyz);
                float d = op.parameters.w;
                push(sdfStack, PlaneSdf(p, normal, d));
                break;
            }
            
            case 7: // Cube
            {
                float3 center = op.parameters.xyz;
                float size = op.parameters.w;
                float3 sizeVec = float3(size, size, size);
                push(sdfStack, CubeISdf(p, center, sizeVec));
                break;
            }

            case 8: // Smooth Union
            {
                float b = pop(sdfStack);
                float a = pop(sdfStack);
                float k = op.parameters.w;
                push(sdfStack, SmoothUnionSdf(a, b, k));
                break;
            }
        }
    }

    return pop(sdfStack);
}

HitResult GetHit(float3 pos EXTRA_PARAMS_DECL){
    uint opCount;
    uint stride;
    operations.GetDimensions(opCount, stride);

    for(uint i = 0; i < opCount; i++){
        Operation op = operations[i];
        
        // For SDF evaluation, we need to transform world pos to object local space
        // This requires the INVERSE of the transform matrix
        // For orthogonal matrices (rotation + translation), transpose = inverse
        float3 p = mul(float4(pos, 1.0f), op.transform).xyz;

        switch(op.opcode){
            case 0: // Union
            {
                HitResult b = pop(hitStack);
                HitResult a = pop(hitStack);
                push(hitStack, UnionHit(a, b));
                break;
            }

            case 1: // Intersection
            {
                HitResult b = pop(hitStack);
                HitResult a = pop(hitStack);
                push(hitStack, IntersectionHit(a, b));
                break;
            }

            case 2: // Invert
            {
                HitResult a = pop(hitStack);
                a.dist = -a.dist;
                a.normal = -a.normal;
                push(hitStack, a);
                break;
            }

            case 3: // Nothing
            {
                push(hitStack, NoneHit());
                break;
            }

            case 4: // All
            {
                push(hitStack, AllHit());
                break;
            }

            case 5: // Sphere
            {
                float3 center = op.parameters.xyz;
                float radius = op.parameters.w;
                push(hitStack, SphereHit(p, center, radius, op.albedo));
                break;
            }

            case 6: // Plane
            {
                float3 normal = normalize(op.parameters.xyz);
                float d = op.parameters.w;
                push(hitStack, PlaneHit(p, normal, d, op.albedo));
                break;
            }

            case 7: // Cube
            {
                float3 center = op.parameters.xyz;
                float size = op.parameters.w;
                float3 sizeVec = float3(size, size, size);
                push(hitStack, CubeIHit(p, center, sizeVec, op.albedo));
                break;
            }

            case 8: // Smooth Union
            {
                HitResult b = pop(hitStack);
                HitResult a = pop(hitStack);
                float k = op.parameters.w;
                // For smooth operations, use the closer surface's material
                push(hitStack, SmoothUnionHit(a, b, k));
                break;
            }
        }
    }

    return pop(hitStack);
}

// // Smooth operations for more organic shapes
// float SmoothUnionSdf(float a, float b, float k)
// {
//     float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
//     return lerp(b, a, h) - k * h * (1.0 - h);
// }

// float SmoothIntersectionSdf(float a, float b, float k)
// {
//     float h = clamp(0.5 - 0.5 * (b - a) / k, 0.0, 1.0);
//     return lerp(b, a, h) + k * h * (1.0 - h);
// }

// float SmoothSubtractionSdf(float a, float b, float k)
// {
//     float h = clamp(0.5 - 0.5 * (a + b) / k, 0.0, 1.0);
//     return lerp(a, -b, h) + k * h * (1.0 - h);
// }

// // Stack operations for distances
// void pushDistance(inout float distanceStack[MAX_STACK_SIZE], inout int stackTop, float value)
// {
//     if (stackTop < MAX_STACK_SIZE - 1)
//     {
//         stackTop++;
//         distanceStack[stackTop] = value;
//     }
// }

// float popDistance(inout float distanceStack[MAX_STACK_SIZE], inout int stackTop)
// {
//     if (stackTop >= 0)
//     {
//         float value = distanceStack[stackTop];
//         stackTop--;
//         return value;
//     }
//     return INFINITY;
// }

// // Stack operations for hit results
// void pushHit(inout HitResult hitStack[MAX_STACK_SIZE], inout int stackTop, HitResult hit)
// {
//     if (stackTop < MAX_STACK_SIZE - 1)
//     {
//         stackTop++;
//         hitStack[stackTop] = hit;
//     }
// }

// HitResult popHit(inout HitResult hitStack[MAX_STACK_SIZE], inout int stackTop)
// {
//     if (stackTop >= 0)
//     {
//         HitResult hit = hitStack[stackTop];
//         stackTop--;
//         return hit;
//     }
//     return NoneHit();
// }

// // Execute a single operation
// void executeOperation(Operation op, float3 p, inout float distanceStack[MAX_STACK_SIZE], inout int stackTop)
// {
//     // Transform point if needed
//     float3 transformedP = mul(float4(p, 1.0f), op.transform).xyz;
    
//     switch (op.opcode)
//     {
//         case 0: // Union
//         {
//             if (stackTop >= 1)
//             {
//                 float b = popDistance(distanceStack, stackTop);
//                 float a = popDistance(distanceStack, stackTop);
//                 pushDistance(distanceStack, stackTop, UnionSdf(a, b));
//             }
//             break;
//         }
        
//         case 1: // Intersection
//         {
//             if (stackTop >= 1)
//             {
//                 float b = popDistance(distanceStack, stackTop);
//                 float a = popDistance(distanceStack, stackTop);
//                 pushDistance(distanceStack, stackTop, IntersectionSdf(a, b));
//             }
//             break;
//         }
        
//         case 2: // Invert (Subtraction)
//         {
//             if (stackTop >= 0)
//             {
//                 float a = popDistance(distanceStack, stackTop);
//                 pushDistance(distanceStack, stackTop, -a);
//             }
//             break;
//         }
        
//         case 3: // Nothing (Empty space)
//         {
//             pushDistance(distanceStack, stackTop, NoneSdf());
//             break;
//         }
        
//         case 4: // All (Solid space)
//         {
//             pushDistance(distanceStack, stackTop, AllSdf());
//             break;
//         }
        
//         case 5: // Sphere
//         {
//             float3 center = op.parameters.xyz;
//             float radius = op.parameters.w;
//             pushDistance(distanceStack, stackTop, SphereSdf(transformedP, center, radius));
//             break;
//         }
        
//         case 6: // Plane
//         {
//             float3 normal = normalize(op.parameters.xyz);
//             float d = op.parameters.w;
//             pushDistance(distanceStack, stackTop, PlaneSdf(transformedP, normal, d));
//             break;
//         }
        
//         case 7: // Cube
//         {
//             float3 center = op.parameters.xyz;
//             float size = op.parameters.w;
//             float3 sizeVec = float3(size, size, size);
//             pushDistance(distanceStack, stackTop, CubeISdf(transformedP, center, sizeVec));
//             break;
//         }
        
//         case 8: // Smooth Union
//         {
//             if (stackTop >= 1)
//             {
//                 float b = popDistance(distanceStack, stackTop);
//                 float a = popDistance(distanceStack, stackTop);
//                 float k = op.parameters.w;
//                 pushDistance(distanceStack, stackTop, SmoothUnionSdf(a, b, k));
//             }
//             break;
//         }
        
//         case 9: // Smooth Intersection
//         {
//             if (stackTop >= 1)
//             {
//                 float b = popDistance(distanceStack, stackTop);
//                 float a = popDistance(distanceStack, stackTop);
//                 float k = op.parameters.w;
//                 pushDistance(distanceStack, stackTop, SmoothIntersectionSdf(a, b, k));
//             }
//             break;
//         }
        
//         case 10: // Smooth Subtraction
//         {
//             if (stackTop >= 1)
//             {
//                 float b = popDistance(distanceStack, stackTop);
//                 float a = popDistance(distanceStack, stackTop);
//                 float k = op.parameters.w;
//                 pushDistance(distanceStack, stackTop, SmoothSubtractionSdf(a, b, k));
//             }
//             break;
//         }
        
//         default:
//             // Unknown opcode, push infinity to avoid issues
//             pushDistance(distanceStack, stackTop, INFINITY);
//             break;
//     }
// }

// // Execute operations for hit results (for material information)
// void executeOperationHit(Operation op, float3 p, float4 defaultAlbedo, inout HitResult hitStack[MAX_STACK_SIZE], inout int stackTop)
// {
//     float3 transformedP = mul(float4(p, 1.0f), op.transform).xyz;
    
//     switch (op.opcode)
//     {
//         case 0: // Union
//         {
//             if (stackTop >= 1)
//             {
//                 HitResult b = popHit(hitStack, stackTop);
//                 HitResult a = popHit(hitStack, stackTop);
//                 pushHit(hitStack, stackTop, UnionHit(a, b));
//             }
//             break;
//         }
        
//         case 1: // Intersection
//         {
//             if (stackTop >= 1)
//             {
//                 HitResult b = popHit(hitStack, stackTop);
//                 HitResult a = popHit(hitStack, stackTop);
//                 pushHit(hitStack, stackTop, IntersectionHit(a, b));
//             }
//             break;
//         }
        
//         case 2: // Invert
//         {
//             if (stackTop >= 0)
//             {
//                 HitResult a = popHit(hitStack, stackTop);
//                 a.dist = -a.dist;
//                 a.normal = -a.normal;
//                 pushHit(hitStack, stackTop, a);
//             }
//             break;
//         }
        
//         case 3: // Nothing
//         {
//             pushHit(hitStack, stackTop, NoneHit());
//             break;
//         }
        
//         case 4: // All
//         {
//             pushHit(hitStack, stackTop, AllHit());
//             break;
//         }
        
//         case 5: // Sphere
//         {
//             float3 center = op.parameters.xyz;
//             float radius = op.parameters.w;
//             pushHit(hitStack, stackTop, SphereHit(transformedP, center, radius, op.albedo));
//             break;
//         }
        
//         case 6: // Plane
//         {
//             float3 normal = normalize(op.parameters.xyz);
//             float d = op.parameters.w;
//             pushHit(hitStack, stackTop, PlaneHit(transformedP, normal, d, op.albedo));
//             break;
//         }
        
//         case 7: // Cube
//         {
//             float3 center = op.parameters.xyz;
//             float size = op.parameters.w;
//             float3 sizeVec = float3(size, size, size);
//             pushHit(hitStack, stackTop, CubeIHit(transformedP, center, sizeVec, op.albedo));
//             break;
//         }
        
//         case 8: // Smooth Union
//         {
//             if (stackTop >= 1)
//             {
//                 HitResult b = popHit(hitStack, stackTop);
//                 HitResult a = popHit(hitStack, stackTop);
//                 // For smooth operations, use the closer surface's material
//                 pushHit(hitStack, stackTop, UnionHit(a, b));
//             }
//             break;
//         }
        
//         case 9: // Smooth Intersection
//         {
//             if (stackTop >= 1)
//             {
//                 HitResult b = popHit(hitStack, stackTop);
//                 HitResult a = popHit(hitStack, stackTop);
//                 pushHit(hitStack, stackTop, IntersectionHit(a, b));
//             }
//             break;
//         }
        
//         case 10: // Smooth Subtraction
//         {
//             if (stackTop >= 1)
//             {
//                 HitResult b = popHit(hitStack, stackTop);
//                 HitResult a = popHit(hitStack, stackTop);
//                 a.dist = -a.dist;
//                 a.normal = -a.normal;
//                 pushHit(hitStack, stackTop, IntersectionHit(a, b));
//             }
//             break;
//         }
        
//         default:
//             pushHit(hitStack, stackTop, NoneHit());
//             break;
//     }
// }

// // Scene SDF
// float GetDistance(float3 p, PixelCtx ctx)
// {
//     // Create local stacks for this pixel
//     float distanceStack[MAX_STACK_SIZE];
//     int stackTop = -1;
    
//     // Get the number of operations
//     uint numOperations;
//     uint stride;
//     operations.GetDimensions(numOperations, stride);
    
//     // Execute all operations in sequence
//     for (uint i = 0; i < numOperations; i++)
//     {
//         Operation op = operations[i];
//         executeOperation(op, p, distanceStack, stackTop);
//     }
    
//     // Return the final result from the stack
//     if (stackTop >= 0)
//     {
//         return popDistance(distanceStack, stackTop);
//     }
    
//     // Default to no geometry if stack is empty
//     return INFINITY;
// }

// // Optional: Function to get hit result with material information
// HitResult GetHit(float3 p, PixelCtx ctx, float4 defaultAlbedo = float4(1.0f, 1.0f, 1.0f, 1.0f))
// {
//     // Create local stacks for this pixel
//     HitResult hitStack[MAX_STACK_SIZE];
//     int stackTop = -1;
    
//     uint numOperations;
//     uint stride;
//     operations.GetDimensions(numOperations, stride);
    
//     // Execute all operations for hit results
//     for (uint i = 0; i < numOperations; i++)
//     {
//         Operation op = operations[i];
//         executeOperationHit(op, p, defaultAlbedo, hitStack, stackTop);
//     }
    
//     // Return the final result from the stack
//     if (stackTop >= 0)
//     {
//         return popHit(hitStack, stackTop);
//     }
    
//     // Default to no hit
//     return NoneHit();
// }

Transform GetTransform()
{
    return camTransform;
}

#include "RayMarchingCore.hlsl"
#include "Renderers/EuclideanLit.hlsl"
#include "RayMarchingMain.hlsl"