using SharpDX;

namespace GpuHlslRayMarchingTest;

public class OrbitCamera{
    private Scene scene;

    //"interesting" fields
    private Vector2 camRotation;
    private float logCamDistance = 1.3f;
    private System.Drawing.Point lastMousePosition;

    private float CamDistance => (float)Math.Exp(logCamDistance);

    private readonly string cameraTransformName;

    public OrbitCamera(Scene scene, string cameraTransformName = "camTransform"){
        this.scene = scene;
        this.cameraTransformName = cameraTransformName;

        scene.writeSceneConstants += WriteCameraConstants;
        Form1.Instance.MouseMove += MouseMove;
        Form1.Instance.MouseWheel += MouseWheel;
    }

    private void MouseMove(object? sender, MouseEventArgs e)
    {
        if (scene.enabled && Control.MouseButtons == MouseButtons.Left)
        {
            var deltaX = e.X - lastMousePosition.X;
            var deltaY = e.Y - lastMousePosition.Y;

            camRotation.X += deltaY * 0.01f; // Adjust sensitivity as needed
            camRotation.Y += deltaX * 0.01f; // Adjust sensitivity as needed
        }
        lastMousePosition = e.Location;
    }

    private void MouseWheel(object? sender, MouseEventArgs e)
    {
        if (scene.enabled)
        {
            logCamDistance += e.Delta * 0.001f; // Adjust sensitivity as needed
        }
    }

    private void WriteCameraConstants(ConstantBufferWriter writer)
    {
        //setup camera view matrix
        Matrix cameraView = Matrix.Identity;
        cameraView *= Matrix.Translation(0, 0, -CamDistance);
        cameraView *= Matrix.RotationX(camRotation.X);
        cameraView *= Matrix.RotationY(camRotation.Y);

        //cameraView.Transpose();

        writer.Write(cameraTransformName, cameraView);
    }
}