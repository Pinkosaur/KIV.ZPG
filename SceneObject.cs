using OpenTK.Mathematics;

namespace ZPG
{
    /// <summary>
    /// Base transformable object with cached model matrix computation.
    /// </summary>
    public class SceneObject : IDisposable
    {
        protected Vector3 _position;
        protected Vector3 _scale = Vector3.One;
        protected Vector3 _rotation;
        protected bool _isModelMatrixDirty = true;
        protected Matrix4 _modelMatrix;

        /// <summary>
        /// Material applied when rendering this object.
        /// </summary>
        public Material Material { get; set; } = new Material();

        /// <summary>
        /// World-space translation.
        /// </summary>
        public Vector3 Position
        {
            get {
                return _position;
            }
            set
            {
                _position = value;
                _isModelMatrixDirty = true;
            }
        }

        /// <summary>
        /// World-space non-uniform scale.
        /// </summary>
        public Vector3 Scale
        {
            get { return _scale; }
            set
            {
                _scale = value;
                _isModelMatrixDirty = true;
            }
        }

        /// <summary>
        /// Euler rotation in radians.
        /// </summary>
        public Vector3 Rotation
        {
            get { return _rotation; }
            set
            {
                _rotation = value;
                _isModelMatrixDirty = true;
            }
        }

        /// <summary>
        /// Cached model matrix, recomputed lazily when transforms change.
        /// </summary>
        public Matrix4 ModelMatrix
        {
            get
            {
                if (_isModelMatrixDirty)
                {
                    _isModelMatrixDirty = false;
                    _modelMatrix = ComputeModelMatrix();
                }
                return _modelMatrix;
            }
        }

        /// <summary>
        /// Computes the model matrix from scale, rotation and translation.
        /// </summary>
        /// <returns>Affine model transform matrix.</returns>
        protected virtual Matrix4 ComputeModelMatrix()
        {
            return Matrix4.CreateScale(_scale) * Matrix4.CreateRotationX(_rotation.X) * Matrix4.CreateRotationY(_rotation.Y) * Matrix4.CreateRotationZ(_rotation.Z) * Matrix4.CreateTranslation(_position);
        }

        /// <summary>
        /// Updates object state for the current frame.
        /// </summary>
        /// <param name="dt">Frame delta time in seconds.</param>
        public virtual void Update(float dt)
        {

        }

        /// <summary>
        /// Draws the object.
        /// </summary>
        public virtual void Draw()
        {

        }

        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            
        }
    }
}
