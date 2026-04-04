using OpenTK.Mathematics;

namespace ZPG
{
    internal class CameraFly : Camera
    {
        public float Fov { get; set; } = MathF.PI / 2;
        public Vector2 AngularVelocity { get; set; }
        public Vector3 ViewingDirection { get; set; } = new Vector3(0f, 0f, 0f);

        public override Matrix4 GetProjectionMatrix(float aspectRatio)
        {
            return Matrix4.CreatePerspectiveFieldOfView(Fov, aspectRatio, 0.1f, 100f);
        }

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

        public void Advance(float velocity, float dt)
        {
            var position = Position;
            position.X -= velocity * MathF.Sin(Rotation.Y) * dt;
            position.Y += velocity * MathF.Sin(Rotation.X) * dt;
            position.Z -= velocity * MathF.Cos(Rotation.Y) * dt;
            Position = position;
        }

        public void Strafe(float velocity, float dt)
        {
            var position = Position;
            position.X += velocity * MathF.Cos(Rotation.Y) * dt;
            position.Z -= velocity * MathF.Sin(Rotation.Y) * dt;
            Position = position;
        }

        protected override Matrix4 ComputeModelMatrix()
        {
            return /*Matrix4.CreateTranslation(-Position) * Matrix4.CreateTranslation(Position) 
                * */Matrix4.CreateRotationX(Rotation.X)
                * Matrix4.CreateRotationY(Rotation.Y)
                * Matrix4.CreateTranslation(Position);
        }
    }
}