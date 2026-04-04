using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZPG
{
    internal class Camera : SceneObject
    {
        public virtual Matrix4 GetProjectionMatrix(float aspectRatio)
        {
            return Matrix4.Identity;
        }

        public override void Update(float dt)
        {
            base.Update(dt);
        }
        public Vector3 Velocity { get; set; }

        public virtual Matrix4 ViewMatrix
        {
            get { return ModelMatrix.Inverted(); }
        }

        public void MoveTo(Vector3 position)
        {
            Position = position;
        }
    }
}
