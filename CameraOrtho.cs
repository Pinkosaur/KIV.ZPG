using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZPG
{
    internal class CameraOrtho : Camera
    {
        public override Matrix4 GetProjectionMatrix(float aspectRatio)  
        {
            return Matrix4.CreateOrthographic(2*aspectRatio, 2, -10, 10);
        }

        public float AngularZVelocity { get; set; }

        public override void Update(float dt)
        {
            Rotation += new Vector3(0, 0, AngularZVelocity * dt);
            var forward = new Vector3(-MathF.Sin(Rotation.Z), MathF.Cos(Rotation.Z),0);
            var right = new Vector3(forward.Y, -forward.X,0);

            Position += right * Velocity.X * dt;
            Position += forward * Velocity.Y * dt;
        }

        public override void Draw()
        {
            base.Draw();
    
            /*GL.Begin(PrimitiveType.LineLoop);
            GL.Vertex3(-0.01f, -0.01f, 0);
            GL.Vertex3(0.01f, -0.01f, 0);
            GL.Vertex3(0, 0.01f, 0);

            GL.End();*/

        }
    }
}
