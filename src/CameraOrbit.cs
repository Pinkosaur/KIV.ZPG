using OpenTK.Mathematics;
using System;

namespace ZPG
{
    /// <summary>
    /// Orbit camera that rotates around a center point with zoom control.
    /// </summary>
    internal class CameraOrbit : Camera
    {
        /// <summary>
        /// Vertical field of view in radians.
        /// </summary>
        public float Fov { get; set; } = MathF.PI / 2;

        /// <summary>
        /// Orbit pivot in world coordinates.
        /// </summary>
        public Vector3 Center { get; set; } = Vector3.Zero;

        /// <summary>
        /// Distance from camera to center.
        /// </summary>
        public float Distance { get; set; } = 5.0f;

        /// <summary>
        /// Orbit yaw/pitch angles in radians.
        /// </summary>
        public Vector2 OrbitalAngle { get; set; } = Vector2.Zero;

        /// <summary>
        /// Angular input velocity for orbit updates.
        /// </summary>
        public Vector2 AngularVelocity { get; set; }

        /// <summary>
        /// Gets view matrix looking from the current camera position to center.
        /// </summary>
        public override Matrix4 ViewMatrix =>
            Matrix4.LookAt(Position, Center, Vector3.UnitY);

        /// <summary>
        /// Creates perspective projection matrix for orbit mode.
        /// </summary>
        /// <param name="aspectRatio">Viewport aspect ratio.</param>
        /// <returns>Perspective projection matrix.</returns>
        public override Matrix4 GetProjectionMatrix(float aspectRatio)
        {
            return Matrix4.CreatePerspectiveFieldOfView(
                float.Pi / 2,
                aspectRatio,
                0.1f,
                20f
            );
        }

        /// <summary>
        /// Integrates orbit angles and distance from user input.
        /// </summary>
        /// <param name="dt">Frame delta time in seconds.</param>
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

        /// <summary>
        /// Recomputes world transform from current orbit parameters.
        /// </summary>
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