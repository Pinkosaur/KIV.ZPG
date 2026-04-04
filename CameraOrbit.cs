using OpenTK.Mathematics;
using System;

namespace ZPG
{
    internal class CameraOrbit : Camera
    {
        public override Matrix4 GetProjectionMatrix(float aspectRatio)
        {
            return Matrix4.CreatePerspectiveFieldOfView(
                float.Pi / 2,
                aspectRatio,
                0.1f,
                20f
            );
        }

        public float Fov { get; set; } = MathF.PI / 2;

        public Vector3 Center { get; set; } = Vector3.Zero;
        public float Distance { get; set; } = 5.0f;

        public Vector2 OrbitalAngle { get; set; } = Vector2.Zero;

        public Vector2 AngularVelocity { get; set; }

        public override Matrix4 ViewMatrix =>
            Matrix4.LookAt(Position, Center, Vector3.UnitY);

        public override void Update(float dt)
        {
            base.Update(dt);

            OrbitalAngle += AngularVelocity * dt;

            OrbitalAngle = new Vector2(
                OrbitalAngle.X,
                MathHelper.Clamp(OrbitalAngle.Y, 0, MathF.PI / 2 - 0.001f)
            );

            Distance = MathHelper.Clamp(Distance + Velocity.Z * dt, 1f, 18f);

            UpdateWorldTransform();
        }

        private void UpdateWorldTransform()
        {
            float yaw = OrbitalAngle.X;
            float pitch = OrbitalAngle.Y;

            Vector3 offset = new Vector3(
                Distance * MathF.Cos(pitch) * MathF.Sin(yaw),
                Distance * MathF.Sin(pitch),
                Distance * MathF.Cos(pitch) * MathF.Cos(yaw)
            );

            Vector3 eye = Center + offset;

            Vector3 forward = Vector3.Normalize(Center - eye);

            float cameraPitch = MathF.Asin(forward.Y);
            float cameraYaw = -MathF.Atan2(forward.X, forward.Z);

            Rotation = new Vector3(cameraPitch, cameraYaw, 0.0f);
            Position = eye;
        }
    }
}