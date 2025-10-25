using System.Runtime.InteropServices;
using SharpDX;

namespace GpuHlslRayMarchingTest;

public class OrbitCamera{
    protected Scene scene;

    //"interesting" fields
    protected Vector2 camRotation;
    protected float logCamDistance = 1.3f;
    protected System.Drawing.Point lastMousePosition;

    protected float CamDistance => (float)Math.Exp(logCamDistance);

    protected readonly string cameraTransformName;

    public OrbitCamera(Scene scene, string cameraTransformName = "camTransform"){
        this.scene = scene;
        this.cameraTransformName = cameraTransformName;

        scene.writeSceneConstants += WriteCameraConstants;
        scene.OnEnabled += OnEnabled;
        scene.OnDisabled += OnDisabled;

        Form1.Instance.MouseMove += MouseMove;
        Form1.Instance.MouseWheel += MouseWheel;

    }

    private void OnEnabled()
    {
        Cursor.Show();
    }

    private void OnDisabled()
    {
        Cursor.Hide();
    }

    private void MouseMove(object? sender, MouseEventArgs e)
    {
        if (scene.Enabled && Control.MouseButtons == MouseButtons.Left)
        {
            var deltaX = e.X - lastMousePosition.X;
            var deltaY = e.Y - lastMousePosition.Y;

            camRotation.X += deltaY * 0.01f;
            camRotation.Y += deltaX * 0.01f;
        }
        lastMousePosition = e.Location;
    }

    private void MouseWheel(object? sender, MouseEventArgs e)
    {
        if (scene.Enabled)
        {
            logCamDistance += e.Delta * 0.001f;
        }
    }


    protected virtual void WriteCameraConstants(ConstantBufferWriter writer)
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

public class OrbitCameraH : OrbitCamera{
    public OrbitCameraH(Scene scene, string cameraTransformName = "camTransform") : base(scene, cameraTransformName)
    {
    }
    protected override void WriteCameraConstants(ConstantBufferWriter writer)
    {
        Matrix cameraView = Matrix.Identity;
        cameraView *= HyperUtil.TranslationZ(-CamDistance);
        cameraView *= Matrix.RotationX(camRotation.X);
        cameraView *= Matrix.RotationY(camRotation.Y);

        //cameraView.Transpose();

        writer.Write(cameraTransformName, cameraView);
    }
}

public class FirstPersonCameraH{
    protected Scene scene;
    protected Matrix baseTransform;
    protected float pitch;
    protected readonly string cameraTransformName;
    protected System.Drawing.Point lastMousePosition;

    protected float speed;
    protected float scale;

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);

    public FirstPersonCameraH(Scene scene, float speed, float scale, string cameraTransformName = "camTransform"){
        this.scene = scene;
        this.cameraTransformName = cameraTransformName;
        this.speed = speed;
        this.scale = scale;

        baseTransform = Matrix.Identity;

        scene.writeSceneConstants += WriteCameraConstants;
        scene.OnEnabled += OnEnabled;
        scene.OnDisabled += OnDisabled;

        Form1.Instance.MouseMove += MouseMove;
        Form1.Instance.OnRender += OnRender;
    }

    private bool IsKeyPressed(Keys key)
    {
        return (GetAsyncKeyState((int)key) & 0x8000) != 0;
    }

    private void OnEnabled()
    {
        Cursor.Hide();
        // Center the cursor in the form
        var centerPoint = new System.Drawing.Point(Form1.Instance.Width / 2, Form1.Instance.Height / 2);
        Cursor.Position = Form1.Instance.PointToScreen(centerPoint);
        lastMousePosition = centerPoint;
    }

    private void OnDisabled()
    {
        Cursor.Show();
    }

    private void MouseMove(object? sender, MouseEventArgs e)
    {
        if (scene.Enabled)
        {
            var deltaX = e.X - lastMousePosition.X;
            var deltaY = e.Y - lastMousePosition.Y;

            pitch += deltaY * 0.01f; 
            baseTransform = Matrix.RotationY(deltaX * 0.01f) * baseTransform;

            // Reset cursor to center to prevent hitting screen edges
            var centerPoint = new System.Drawing.Point(Form1.Instance.Width / 2, Form1.Instance.Height / 2);
            Cursor.Position = Form1.Instance.PointToScreen(centerPoint);
            lastMousePosition = centerPoint;
        }
        else{
            lastMousePosition = e.Location;
        }
    }

    private void OnRender()
    {
        if (!scene.Enabled) return;

        // Clamp pitch to avoid flipping
        pitch = MathUtil.Clamp(pitch, -MathF.PI / 2, MathF.PI / 2);

        if(IsKeyPressed(Keys.W))
        {
            baseTransform = HyperUtil.TranslationZ(speed * scale * Form1.Instance.DeltaTime) * baseTransform;
        }
        if(IsKeyPressed(Keys.S))
        {
            baseTransform = HyperUtil.TranslationZ(-speed * scale * Form1.Instance.DeltaTime) * baseTransform;
        }
        if(IsKeyPressed(Keys.A))
        {
            baseTransform = HyperUtil.TranslationX(-speed * scale * Form1.Instance.DeltaTime) * baseTransform;
        }
        if(IsKeyPressed(Keys.D))
        {
            baseTransform = HyperUtil.TranslationX(speed * scale * Form1.Instance.DeltaTime) * baseTransform;
        }

        
    }

    protected virtual void WriteCameraConstants(ConstantBufferWriter writer)
    {
        writer.Write(cameraTransformName, Matrix.RotationX(pitch) * HyperUtil.TranslationY(scale) * baseTransform);
    }
}