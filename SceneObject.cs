using OpenTK.Mathematics;

namespace ZPG
{
    public class SceneObject : IDisposable
    {

        public Material Material { get; set; } = new Material();
        protected Vector3 _position;
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

        protected Vector3 _scale = Vector3.One;
        public Vector3 Scale
        {
            get { return _scale; }
            set
            {
                _scale = value;
                _isModelMatrixDirty = true;
            }
        }

        protected Vector3 _rotation;
        public Vector3 Rotation
        {
            get { return _rotation; }
            set
            {
                _rotation = value;
                _isModelMatrixDirty = true;
            }
        }

        protected bool _isModelMatrixDirty = true; 
        protected Matrix4 _modelMatrix;
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

        protected virtual Matrix4 ComputeModelMatrix()
        {
            return Matrix4.CreateScale(_scale) * Matrix4.CreateRotationX(_rotation.X) * Matrix4.CreateRotationY(_rotation.Y) * Matrix4.CreateRotationZ(_rotation.Z) * Matrix4.CreateTranslation(_position);
        }

        public virtual void Update(float dt)
        {

        }

        public virtual void Draw()
        {

        }

        public virtual void Dispose()
        {
            
        }
    }
}
