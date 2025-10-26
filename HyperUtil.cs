using SceneBuilding;
using SharpDX;

namespace GpuHlslRayMarchingTest;
public static class HyperUtil{
    public static readonly Vector4 Origin = new Vector4(0, 0, 0, 1);
    public static Vector4 ToHyperbolic(Vector3 v){
        float w = (float)Math.Sqrt(1 + v.LengthSquared());
        return new Vector4(v, w);
    }


    public static Matrix TranslationX(float distance)
    {
        Matrix translation = new Matrix
        (
            MathF.Cosh(distance), 0, 0, MathF.Sinh(distance),
            0, 1, 0, 0,
            0, 0, 1, 0,
            MathF.Sinh(distance), 0, 0, MathF.Cosh(distance)
        );
        return translation;
    }

    public static Matrix TranslationY(float distance)
    {
        Matrix translation = new Matrix
        (
            1, 0, 0, 0,
            0, MathF.Cosh(distance), 0, MathF.Sinh(distance),
            0, 0, 1, 0,
            0, MathF.Sinh(distance), 0, MathF.Cosh(distance)
        );
        return translation;
    }

    public static Matrix TranslationZ(float distance)
    {
        Matrix translation = new Matrix
        (
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, MathF.Cosh(distance), MathF.Sinh(distance),
            0, 0, MathF.Sinh(distance), MathF.Cosh(distance)
        );
        return translation;
    }


    public static float HyperTriSideLength(float oppAngle, float adjAngle1, float adjAngle2)
    {
        return MathF.Acosh(
            (MathF.Cos(oppAngle) + MathF.Cos(adjAngle1) * MathF.Cos(adjAngle2)) /
            (MathF.Sin(adjAngle1) * MathF.Sin(adjAngle2))
        );
    }

    public static ObjectBuilder NGonPrism(int sides, float sideDist, float thickness, Vector4 albedo, Matrix baseTransform)
    {
        var prism = new Intersection();

        for (int i = 0; i < sides; i++)
        {
            float ang = 2 * MathF.PI / sides * i;

            prism.Add(new TransformH(
                TranslationZ(-sideDist) * Matrix.RotationY(ang) * baseTransform,
                new PlaneH(new Vector4(0, 0, 1, 0), 0, albedo)
            ));
        }

        prism.Add(
            new TransformH(
                baseTransform,
                new Intersection(
                    new PlaneH(new Vector4(0, 1, 0, 0), 0, albedo),
                    new PlaneH(new Vector4(0, -1, 0, 0), 0.1f, albedo)
                )
            )
        );

        return prism;
    }

    public static ObjectBuilder Tiling(int sides, int shapesPerVertex, int depth, Func<Matrix, ObjectBuilder> newObj, float epsilon = 0.01f){
        var group = new Union();
        float centralAngle = 2 * MathF.PI / sides;
        float interiorAngle = 2 * MathF.PI / shapesPerVertex;

        float sideDist = 2 * HyperTriSideLength(
            interiorAngle / 2,
            centralAngle / 2,
            MathF.PI / 2
        );

        List<Vector4> foundPositions = new();
        List<Matrix> transformsToProcess = new();

        transformsToProcess.Add(Matrix.Identity);
        foundPositions.Add(Origin);
        group.Add(newObj(Matrix.Identity));

        for(int d = 0; d < depth; d++){
            List<Matrix> newTransforms = new();

            foreach(var parentTransform in transformsToProcess){
                for(int i = 0; i < sides; i++){
                    float ang = i * centralAngle;
                    if(sides % 2 == 1){
                        ang += centralAngle / 2;
                    }
                    var translation = TranslationZ(sideDist) * Matrix.RotationY(ang);
                    var newTransform = parentTransform * translation;

                    var pos = Vector4.Transform(Origin, newTransform);

                    bool alreadyExists = false;
                    foreach(var existingPos in foundPositions){
                        if(Vector4.DistanceSquared(existingPos, pos) < epsilon){
                            alreadyExists = true;
                            break;
                        }
                    }

                    if(!alreadyExists){
                        foundPositions.Add(pos);
                        newTransforms.Add(newTransform);
                        group.Add(newObj(newTransform));
                    }
                }
            }

            transformsToProcess = newTransforms;
        }

        return group;
    }

    public static ObjectBuilder Tiling(int sides, int shapesPerVertex, int depth, float thickness, float padding, Vector4 albedo, float epsilon = 0.01f){
        float centralAngle = 2 * MathF.PI / sides;
        float interiorAngle = 2 * MathF.PI / shapesPerVertex;

        float sideDist = HyperTriSideLength(
            interiorAngle / 2,
            centralAngle / 2,
            MathF.PI / 2
        );

        return Tiling(sides, shapesPerVertex, depth, (baseTransform) => NGonPrism(
            sides,
            sideDist - padding,
            thickness,
            albedo,
            baseTransform
        ), epsilon);
    }

    /// <summary>
    /// Converts a light-like normal (tangent vector) to a space-like normal for hyperbolic planes.
    /// For a light-like vector (x, y, z, 0), creates a space-like vector (x, y, z, w) where w > ||(x,y,z)||
    /// </summary>
    public static Vector4 ToSpaceNormal(Vector4 lightLikeNormal)
    {
        // Calculate the magnitude of the spatial part
        float spatialMagnitude = MathF.Sqrt(
            lightLikeNormal.X * lightLikeNormal.X + 
            lightLikeNormal.Y * lightLikeNormal.Y + 
            lightLikeNormal.Z * lightLikeNormal.Z
        );
        
        // Set w component to be slightly larger than spatial magnitude to ensure space-like
        // This makes the Minkowski norm x² + y² + z² - w² < 0
        float wComponent = spatialMagnitude + 0.1f;
        
        return new Vector4(lightLikeNormal.X, lightLikeNormal.Y, lightLikeNormal.Z, wComponent);
    }

    public static Vector4 SpaceNormal(float x, float y, float z)
    {
        return ToSpaceNormal(new Vector4(x, y, z, 0));
    }
}