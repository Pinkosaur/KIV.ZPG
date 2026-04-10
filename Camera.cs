using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZPG
{
    /// <summary>
    /// Base camera abstraction providing projection and view behavior.
    /// </summary>
    internal class Camera : SceneObject
    {
        /// <summary>
        /// Linear camera velocity in local update logic.
        /// </summary>
        public Vector3 Velocity { get; set; }

        /// <summary>
        /// Gets the view matrix generated from the inverse model transform.
        /// </summary>
        public virtual Matrix4 ViewMatrix
        {
            get { return ModelMatrix.Inverted(); }
        }

        /// <summary>
        /// Builds the camera projection matrix.
        /// </summary>
        /// <param name="aspectRatio">Viewport aspect ratio.</param>
        /// <returns>Projection matrix.</returns>
        public virtual Matrix4 GetProjectionMatrix(float aspectRatio)
        {
            return Matrix4.Identity;
        }

        /// <summary>
        /// Updates camera internals for the frame.
        /// </summary>
        /// <param name="dt">Frame delta time in seconds.</param>
        public override void Update(float dt)
        {
            base.Update(dt);
        }

        /// <summary>
        /// Moves the camera to a world-space position.
        /// </summary>
        /// <param name="position">Target camera position.</param>
        public void MoveTo(Vector3 position)
        {
            Position = position;
        }
    }
}
