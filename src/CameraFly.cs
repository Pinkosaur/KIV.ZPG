using OpenTK.Mathematics;

namespace ZPG
{
    /// <summary>
    /// Free-fly camera controlled by translation and mouse-driven rotation.
    /// </summary>
    internal class CameraFly : Camera
    {
        /// <summary>
        /// Vertical field of view in radians.
        /// </summary>
        public float Fov { get; set; } = MathF.PI / 2;

        /// <summary>
        /// Per-frame angular velocity from input.
        /// </summary>
        public Vector2 AngularVelocity { get; set; }

        /// <summary>
        /// Current view direction placeholder.
        /// </summary>
        public Vector3 ViewingDirection { get; set; } = new Vector3(0f, 0f, 0f);

        /// <summary>
        /// Creates perspective projection for fly camera mode.
        /// </summary>
        /// <param name="aspectRatio">Viewport aspect ratio.</param>
        /// <returns>Perspective projection matrix.</returns>
        public override Matrix4 GetProjectionMatrix(float aspectRatio)
        {
            return Matrix4.CreatePerspectiveFieldOfView(Fov, aspectRatio, 0.1f, 100f);
        }

        /// <summary>
        /// Applies angular and linear velocity to camera transform.
        /// </summary>
        /// <param name="dt">Frame delta time in seconds.</param>
        public override void Update(float dt)
        {
            base.Update(dt);

            var rotation = Rotation;
            rotation.X -= AngularVelocity.Y * dt;
            rotation.Y -= AngularVelocity.X * dt;
            //rotation.X = MathF.Min(0, MathF.Max(-MathF.PI / 2 + 0.01f, rotation.X));
            Rotation = rotation;

            var position = Position;
            position.X += Velocity.X * dt;
            position.Y += Velocity.Y * dt;
            position.Z += Velocity.Z * dt;
            //position.Z = MathF.Max(1.0f, position.Z);
            Position = position;
        }

        /// <summary>
        /// Moves camera forward/backward in local forward direction.
        /// </summary>
        /// <param name="velocity">Movement speed.</param>
        /// <param name="dt">Frame delta time in seconds.</param>
        public void Advance(float velocity, float dt)
        {
            var position = Position;
            position.X -= velocity * MathF.Sin(Rotation.Y) * dt;
            position.Y += velocity * MathF.Sin(Rotation.X) * dt;
            position.Z -= velocity * MathF.Cos(Rotation.Y) * dt;
            Position = position;
        }

        /// <summary>
        /// Moves camera left/right in local right direction.
        /// </summary>
        /// <param name="velocity">Strafe speed.</param>
        /// <param name="dt">Frame delta time in seconds.</param>
        public void Strafe(float velocity, float dt)
        {
            var position = Position;
            position.X += velocity * MathF.Cos(Rotation.Y) * dt;
            position.Z -= velocity * MathF.Sin(Rotation.Y) * dt;
            Position = position;
        }

        /// <summary>
        /// Computes model transform from rotation and translation.
        /// </summary>
        /// <returns>Camera world transform matrix.</returns>
        protected override Matrix4 ComputeModelMatrix()
        {
            return /*Matrix4.CreateTranslation(-Position) * Matrix4.CreateTranslation(Position) 
                * */Matrix4.CreateRotationX(Rotation.X)
                * Matrix4.CreateRotationY(Rotation.Y)
                * Matrix4.CreateTranslation(Position);
        }
    }
}