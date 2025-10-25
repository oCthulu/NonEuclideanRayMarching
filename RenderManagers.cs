using SharpDX;

public class LitRenderManager
{
    public LitRenderManager(Scene scene, Vector3 lightDirection)
    {
        scene.writeSceneConstants += (writer) =>
        {
            writer.Write("lightDirection", new Vector4(lightDirection, 0));
        };
    }
}