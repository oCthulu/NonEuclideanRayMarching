# Stack-Based IL Interpreter for Ray Marching

This system implements a stack-based Intermediate Language (IL) interpreter for building complex ray marched scenes dynamically. The interpreter processes a sequence of operations that build up the scene using a stack-based approach.

## How It Works

### Stack-Based Execution
The interpreter uses two stacks:
- **Distance Stack**: Stores distance field values
- **Hit Result Stack**: Stores hit information including materials and normals

Operations are executed in sequence, consuming operands from the stack and pushing results back onto the stack.

### Operation Format
Each operation contains:
```csharp
struct Operation {
    int opcode;          // Operation type
    float4x4 transform;  // Transformation matrix
    float4 parameters;   // Operation-specific parameters
    float4 albedo;       // Material color
}
```

## Available Operations

### Boolean Operations
- **0: Union** - Combines two shapes (min operation)
- **1: Intersection** - Intersects two shapes (max operation) 
- **2: Invert** - Inverts a shape (negation)

### Primitive Shapes
- **3: Nothing** - Empty space (infinite distance)
- **4: All** - Solid space (negative infinite distance)
- **5: Sphere** - Sphere primitive
  - `parameters.xyz`: center position
  - `parameters.w`: radius
- **6: Plane** - Infinite plane
  - `parameters.xyz`: normal vector
  - `parameters.w`: distance from origin
- **7: Cube** - Axis-aligned box
  - `parameters.xyz`: center position
  - `parameters.w`: size (half-extents)

### Smooth Operations
- **8: Smooth Union** - Smooth blending of two shapes
- **9: Smooth Intersection** - Smooth intersection
- **10: Smooth Subtraction** - Smooth subtraction
  - `parameters.w`: smoothing factor (higher = smoother)

## Usage Examples

### Basic Scene Creation
```csharp
// Create a simple scene with two spheres
var operations = new ILBuilder()
    .Sphere(Vector3.Zero, 1.0f, new Vector4(1, 0, 0, 1))     // Red sphere
    .Sphere(new Vector3(2, 0, 0), 0.8f, new Vector4(0, 1, 0, 1))  // Green sphere
    .Union()  // Combine them
    .ToArray();

form.UpdateScene(operations);
```

### Boolean Operations
```csharp
// Create a sphere with a cube subtracted from it
var operations = new ILBuilder()
    .Sphere(Vector3.Zero, 1.5f, new Vector4(1, 0, 0, 1))    // Red sphere
    .Cube(Vector3.Zero, 1.0f, new Vector4(0, 0, 1, 1))      // Blue cube
    .Invert()         // Invert the cube (make it a hole)
    .Intersection()   // Subtract cube from sphere
    .ToArray();
```

### Smooth Blending
```csharp
// Create smoothly blended spheres
var operations = new ILBuilder()
    .Sphere(new Vector3(-0.5f, 0, 0), 1.0f, new Vector4(1, 0, 0, 1))
    .Sphere(new Vector3(0.5f, 0, 0), 1.0f, new Vector4(0, 1, 0, 1))
    .SmoothUnion(0.3f)  // Smooth blend with 0.3 units of smoothing
    .ToArray();
```

### Transformations
```csharp
// Create a rotated and translated cube
var transform = ILBuilder.CreateRotation(Vector3.UnitY, MathUtil.PiOverFour) * 
                ILBuilder.CreateTranslation(new Vector3(2, 0, 0));

var operations = new ILBuilder()
    .Cube(Vector3.Zero, 1.0f, new Vector4(1, 1, 0, 1), transform)
    .ToArray();
```

## Interactive Controls

- **1**: Load example scene 1 (simple union)
- **2**: Load example scene 2 (boolean operations)
- **3**: Load example scene 3 (smooth blending)
- **R**: Reload shader (useful for development)
- **Mouse**: Rotate camera
- **Mouse Wheel**: Zoom in/out

## Technical Details

### Stack Management
The HLSL implementation uses fixed-size arrays for the stacks:
```hlsl
#define MAX_STACK_SIZE 32
static float distanceStack[MAX_STACK_SIZE];
static HitResult hitStack[MAX_STACK_SIZE];
```

### Memory Layout
Operations are stored in a structured buffer and bound to register `u1`:
```hlsl
RWStructuredBuffer<Operation> operations : register(u1);
```

### Performance Considerations
- Maximum stack depth is 32 operations
- All operations are executed sequentially for each pixel
- Complex scenes may impact performance
- Consider LOD systems for distant objects

## Extending the System

### Adding New Primitives
1. Add a new opcode to the enum
2. Implement the SDF function
3. Add the case to `executeOperation` and `executeOperationHit`
4. Add a helper method to `ILBuilder`

### Adding New Operations
1. Define the new operation's behavior
2. Implement stack manipulation logic
3. Update both distance and hit result execution paths

### Custom Transforms
Use the transform matrix in operations to:
- Translate objects
- Rotate objects
- Scale objects (non-uniform scaling possible)
- Apply complex transformations

This system provides a flexible foundation for building complex ray marched scenes dynamically, making it easier to create procedural content, animations, and interactive scenes.
