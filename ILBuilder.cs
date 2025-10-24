using System;
using System.Collections.Generic;
using SharpDX;

namespace GpuHlslRayMarchingTest
{
    public struct IlOperation
    {
        public int Opcode;
        public Matrix Transform;
        public Vector4 Parameters;
        public Vector4 Albedo;
    }
    /// <summary>
    /// Helper class for building IL operations for the stack-based ray marching interpreter
    /// </summary>
    public class ILBuilder
    {
        private List<IlOperation> operations = new List<IlOperation>();


        // Basic operations
        public ILBuilder Union() 
        { 
            operations.Add(new IlOperation { Opcode = 0 }); 
            return this; 
        }

        public ILBuilder Intersection() 
        { 
            operations.Add(new IlOperation { Opcode = 1 }); 
            return this; 
        }

        public ILBuilder Invert() 
        { 
            operations.Add(new IlOperation { Opcode = 2 }); 
            return this; 
        }

        public ILBuilder Nothing() 
        { 
            operations.Add(new IlOperation { Opcode = 3 }); 
            return this; 
        }

        public ILBuilder All() 
        { 
            operations.Add(new IlOperation { Opcode = 4 }); 
            return this; 
        }

        // Shapes
        public ILBuilder Sphere(Vector3 center, float radius, Vector4? albedo = null, Matrix? transform = null)
        {
            operations.Add(new IlOperation 
            { 
                Opcode = 5,
                Transform = transform ?? Matrix.Identity,
                Parameters = new Vector4(center.X, center.Y, center.Z, radius),
                Albedo = albedo ?? Vector4.One
            });
            return this;
        }

        public ILBuilder Plane(Vector3 normal, float distance, Vector4? albedo = null, Matrix? transform = null)
        {
            normal = Vector3.Normalize(normal);
            operations.Add(new IlOperation 
            { 
                Opcode = 6,
                Transform = transform ?? Matrix.Identity,
                Parameters = new Vector4(normal.X, normal.Y, normal.Z, distance),
                Albedo = albedo ?? Vector4.One
            });
            return this;
        }

        public ILBuilder Cube(Vector3 center, float size, Vector4? albedo = null, Matrix? transform = null)
        {
            operations.Add(new IlOperation 
            { 
                Opcode = 7,
                Transform = transform ?? Matrix.Identity,
                Parameters = new Vector4(center.X, center.Y, center.Z, size),
                Albedo = albedo ?? Vector4.One
            });
            return this;
        }

        // Smooth operations
        public ILBuilder SmoothUnion(float smoothness = 0.1f) 
        { 
            operations.Add(new IlOperation 
            { 
                Opcode = 8,
                Parameters = new Vector4(0, 0, 0, smoothness)
            }); 
            return this; 
        }

        public ILBuilder SmoothIntersection(float smoothness = 0.1f) 
        { 
            operations.Add(new IlOperation 
            { 
                Opcode = 9,
                Parameters = new Vector4(0, 0, 0, smoothness)
            }); 
            return this; 
        }

        public ILBuilder SmoothSubtraction(float smoothness = 0.1f) 
        { 
            operations.Add(new IlOperation 
            { 
                Opcode = 10,
                Parameters = new Vector4(0, 0, 0, smoothness)
            }); 
            return this; 
        }

        // Utility methods
        public ILBuilder Clear()
        {
            operations.Clear();
            return this;
        }

        public IlOperation[] ToArray()
        {
            return operations.ToArray();
        }

        public int Count => operations.Count;

        // Example scene builders
        public static IlOperation[] CreateSimpleScene()
        {
            return new ILBuilder()
                .Sphere(Vector3.Zero, 1.0f, new Vector4(1, 0, 0, 1))  // Red sphere
                .Sphere(new Vector3(2, 0, 0), 0.8f, new Vector4(0, 1, 0, 1))  // Green sphere
                .Union()  // Combine the spheres
                .ToArray();
        }

        public static IlOperation[] CreateBooleanScene()
        {
            return new ILBuilder()
                .Sphere(Vector3.Zero, 1.5f, new Vector4(1, 0, 0, 1))  // Red sphere
                .Cube(new Vector3(0.5f, 0, 0), 1.0f, new Vector4(0, 0, 1, 1))  // Blue cube
                .Intersection()  // Intersect sphere and cube
                .Sphere(new Vector3(-0.5f, 0, 0), 0.8f, new Vector4(0, 1, 0, 1))  // Green sphere
                .Invert()  // Invert green sphere (make it a hole)
                .Intersection()  // Subtract from the sphere-cube intersection
                .ToArray();
        }

        public static IlOperation[] CreateSmoothScene()
        {
            return new ILBuilder()
                .Sphere(new Vector3(-0.5f, 0, 0), 1.0f, new Vector4(1, 0, 0, 1))  // Red sphere
                .Sphere(new Vector3(0.5f, 0, 0), 1.0f, new Vector4(0, 1, 0, 1))   // Green sphere
                .SmoothUnion(0.3f)  // Smooth blend between spheres
                .Plane(new Vector3(0, 1, 0), 1.0f, new Vector4(0.5f, 0.5f, 0.5f, 1))  // Gray floor
                .Union()  // Add floor
                .ToArray();
        }

        // Transform helpers
        public static Matrix CreateTranslation(Vector3 translation)
        {
            return Matrix.Translation(translation);
        }

        public static Matrix CreateRotation(Vector3 axis, float angle)
        {
            return Matrix.RotationAxis(Vector3.Normalize(axis), angle);
        }

        public static Matrix CreateScale(float scale)
        {
            return Matrix.Scaling(scale);
        }

        public static Matrix CreateScale(Vector3 scale)
        {
            return Matrix.Scaling(scale);
        }
    }
}
