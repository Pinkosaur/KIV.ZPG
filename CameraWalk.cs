using OpenTK.Mathematics;
using System;

namespace ZPG
{
    /// <summary>
    /// Represents a camera that simulates walking on a terrain by adjusting its Y-coordinate
    /// based on the underlying 3D geometry using barycentric interpolation. Integrates basic momentum and physics.
    /// </summary>
    internal class CameraWalk : Camera
    {
        private float _eyeLevel = 1.8f;
        private float _normalEyeLevel = 1.8f;
        private float _crouchingEyeLevel = 1f;
        private bool _crouching = false;
        private float _speedMultiplier = 1f;

        /// <summary>
        /// Gets or sets the field of view (FOV) of the camera in radians.
        /// </summary>
        public float Fov { get; set; } = MathF.PI / 2;

        /// <summary>
        /// Gets or sets the angular velocity of the camera for view rotation.
        /// </summary>
        public Vector2 AngularVelocity { get; set; }

        /// <summary>
        /// Gets or sets the current viewing direction vector of the camera.
        /// </summary>
        public Vector3 ViewingDirection { get; set; } = new Vector3(0f, 0f, 0f);

        private float[,] _heightMap;
        private int _terrainWidth;
        private int _terrainHeight;

        private float _mapMaxX = 0;
        private float _mapMaxZ = 0;

        private bool _isJumping = false;
        private bool _isFalling = false;
        
        /// <summary>
        /// Dedicated vector to track the camera's momentum while detached from the terrain.
        /// </summary>
        private Vector3 _airborneVelocity = Vector3.Zero;

        /// <summary>
        /// Precomputes the heightmap grid from the provided terrain geometry to allow fast height lookups.
        /// </summary>
        public void ComputeHeightMap(VertexNormal[] terrainVertices, MeshPart[] terrainMeshParts, int width, int height)
        {
            if (terrainVertices == null || terrainMeshParts == null)
                return;

            List<Triangle> t = new List<Triangle>();
            foreach (var part in terrainMeshParts)
                foreach (var tri in part.Triangles)
                    t.Add(tri);

            Triangle[] terrainTriangles = t.ToArray();

            _terrainWidth = width;
            _terrainHeight = height;
            _heightMap = new float[_terrainHeight, _terrainWidth]; // [z, x]
            _mapMaxX = width / 4f - 2f;
            _mapMaxZ = height / 4f - 2f;

            for (int i = 0; i < terrainTriangles.Length; i++)
            {
                Vector3 p0 = terrainVertices[terrainTriangles[i].i0].Position;
                Vector3 p1 = terrainVertices[terrainTriangles[i].i1].Position;
                Vector3 p2 = terrainVertices[terrainTriangles[i].i2].Position;

                // Restrict bounds calculation strictly to world-space boundaries
                int minX = (int)MathF.Max(-(width / 2f), MathF.Floor(MathF.Min(p0.X, MathF.Min(p1.X, p2.X))));
                int maxX = (int)MathF.Min( width / 2f - 1, MathF.Ceiling(MathF.Max(p0.X, MathF.Max(p1.X, p2.X))));
                int minZ = (int)MathF.Max(-(height / 2f), MathF.Floor(MathF.Min(p0.Z, MathF.Min(p1.Z, p2.Z))));
                int maxZ = (int)MathF.Min( height / 2f - 1, MathF.Ceiling(MathF.Max(p0.Z, MathF.Max(p1.Z, p2.Z))));

                for (int x = minX; x <= maxX; x++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        float determinant = (p1.Z - p2.Z) * (p0.X - p2.X) + (p2.X - p1.X) * (p0.Z - p2.Z);
                        if (MathF.Abs(determinant) < 0.0001f)
                            continue;

                        float w1 = ((p1.Z - p2.Z) * (x - p2.X) + (p2.X - p1.X) * (z - p2.Z)) / determinant;
                        float w2 = ((p2.Z - p0.Z) * (x - p2.X) + (p0.X - p2.X) * (z - p2.Z)) / determinant;
                        float w3 = 1.0f - w1 - w2;

                        if (w1 >= 0 && w2 >= 0 && w3 >= 0)
                        {
                            int mapX = x + width / 2;
                            int mapZ = z + height / 2;

                            if ((uint)mapX < (uint)width && (uint)mapZ < (uint)height)
                                _heightMap[mapZ, mapX] = w1 * p0.Y + w2 * p1.Y + w3 * p2.Y;
                        }
                    }
                }
            }
        }

        private void UpdatePosition(Vector3 newPosition)
        {
            if (newPosition.X > -_mapMaxX && newPosition.X < _mapMaxX && newPosition.Z > -_mapMaxZ && newPosition.Z < _mapMaxZ)
            {
                Position = newPosition;
                return;
            }
            if (newPosition.X < -_mapMaxX)
            {
                newPosition.X = -_mapMaxX;
            }
            else if (newPosition.X > _mapMaxX)
            {
                newPosition.X = _mapMaxX;
            }
            if (newPosition.Z < -_mapMaxZ)
            {
                newPosition.Z = -_mapMaxZ;
            }
            else if (newPosition.Z > _mapMaxZ)
            {
                newPosition.Z = _mapMaxZ;
            }
            newPosition.Y = ComputeY(newPosition.X, newPosition.Z) + _eyeLevel; // Ensure the camera stays grounded to the terrain when clamping to edges
            Position = newPosition;
            return;
        }

        /// <summary>
        /// Generates the perspective projection matrix for this camera.
        /// </summary>
        /// <param name="aspectRatio">The aspect ratio of the active viewport.</param>
        /// <returns>The computed perspective projection matrix.</returns>
        public override Matrix4 GetProjectionMatrix(float aspectRatio)
        {
            return Matrix4.CreatePerspectiveFieldOfView(Fov, aspectRatio, 0.1f, 100f);
        }

        /// <summary>
        /// Integrates requested movement vectors, handles gravity, applies mouse rotation, and resolves terrain collisions.
        /// </summary>
        /// <param name="dt">The elapsed time since the last frame.</param>
        public override void Update(float dt)
        {
            base.Update(dt);

            var rotation = Rotation;
            rotation.X -= AngularVelocity.Y * dt; rotation.X = MathHelper.Clamp(rotation.X, -MathF.PI / 2 + 0.01f, MathF.PI / 2 - 0.01f);
            rotation.Y -= AngularVelocity.X * dt;
            Rotation = rotation;

            var position = Position;

            if (_isJumping || _isFalling)
            {
                // Apply gravity to the airborne momentum
                _airborneVelocity.Y -= 9.81f * dt;
                
                // Integrate velocity into position
                position += _airborneVelocity * dt;

                // Check for collision with the terrain
                float terrainY = ComputeY(position.X, position.Z) + _eyeLevel;
                
                // Camera only snaps to the ground if traveling downwards and crosses the height threshold
                if (position.Y <= terrainY /*&& _airborneVelocity.Y <= 0*/)
                {
                    position.Y = terrainY;
                    _isJumping = false;
                    _isFalling = false;
                    _airborneVelocity = Vector3.Zero;
                }

                UpdatePosition(position);
            }
            else
            {
                // Grounded movement integration
                if (Velocity.LengthSquared > 0.0001f)
                {
                    Vector3 nextPosition = position + Velocity * dt;
                    nextPosition.Y = ComputeY(nextPosition.X, nextPosition.Z) + _eyeLevel;

                    int slopeState = EvalSlope(position - new Vector3(0, _eyeLevel, 0), nextPosition);

                    if (slopeState == -1) 
                    {
                        // The slope is too steep downwards; initiate a fall
                        _isFalling = true;
                        
                        // Transfer the accumulated running velocity into the airborne state to preserve horizontal inertia
                        _airborneVelocity = Velocity;
                        position += Velocity * dt;
                        UpdatePosition(position);
                    }
                    else if (slopeState == 1)
                    {
                        nextPosition -= Velocity * dt * 0.6f; // Reduce horizontal speed by 60% on moderately steep slopes
                        UpdatePosition(nextPosition);
                    } 
                    else if (slopeState == 2) 
                    {
                        // The slope is too steep upwards; block movement completely
                        UpdatePosition(position);
                    }
                    else 
                    {
                        // Standard flat movement
                        UpdatePosition(nextPosition);
                    }
                }
                else
                {
                    // Ensure the camera stays anchored to the terrain even when no input is provided
                    position.Y = ComputeY(position.X, position.Z) + _eyeLevel;
                    UpdatePosition(position);
                }
            }
        }
 
        /// <summary>
        /// Initiates a jump maneuver. Captures the provided velocity vector to retain horizontal momentum in the air.
        /// </summary>
        public void Jump(Vector3 velocity)
        {
            if (!_isJumping && !_isFalling)
            {
                _isJumping = true;
                _airborneVelocity = velocity;
            }
        }

        /// <summary>
        /// Accumulates forward/backward velocity along the local Z axis relative to camera rotation.
        /// </summary>
        /// <param name="speed">The scalar speed of movement.</param>
        /// <param name="dt">The elapsed time since the last frame. Maintained for interface compatibility.</param>
        public void Advance(float speed, float dt)
        {
            AdvanceDirectional(speed, 0f);
        }

        public void Run(float speed, float dt)
        {
            if (speed <= 0)
            {
                Advance(speed, dt); return;
            }
            if (_crouching) ToggleCrouch();
            Advance(2 * speed, dt);
        }

        /// <summary>
        /// Accumulates sideways velocity along the local X axis relative to camera rotation.
        /// </summary>
        /// <param name="speed">The scalar speed of movement.</param>
        /// <param name="dt">The elapsed time since the last frame. Maintained for interface compatibility.</param>
        public void Strafe(float speed, float dt)
        {
            // Strafing is simply advancing with a -90 degree offset
            AdvanceDirectional(speed, -MathF.PI / 2f);
        }

        /// <summary>
        /// Internal helper that calculates a velocity vector based on a directional offset and appends it to the active momentum.
        /// </summary>
        private void AdvanceDirectional(float speed, float angleOffset)
        {
            // Ignore ground movement input if the camera is currently in the air
            if (_isJumping || _isFalling) return;

            float angle = Rotation.Y + angleOffset;
            var v = Velocity;
            
            // Accumulate velocity in meters per second. 
            // We intentionally avoid multiplying by dt here, as dt integration happens centrally in the Update method.
            v.X += -speed * MathF.Sin(angle);
            v.Z += -speed * MathF.Cos(angle);
            
            Velocity = v;
        }

        public void ToggleCrouch()
        {
            Vector3 pos = Position;
            if (_crouching)
            {
                _eyeLevel = _normalEyeLevel;
                _crouching = false;
                _speedMultiplier = 1f;
                pos.Y += _normalEyeLevel - _crouchingEyeLevel;
            }
            else
            {
                _eyeLevel = _crouchingEyeLevel;
                _crouching = true;
                _speedMultiplier = .7f;
                pos.Y -= _normalEyeLevel - _crouchingEyeLevel;
            }
            UpdatePosition(pos);
        }

        /// <summary>
        /// Computes the final model matrix using the camera's scale, rotation, and translation variables.
        /// </summary>
        /// <returns>The affine transformation matrix representing the camera.</returns>
        protected override Matrix4 ComputeModelMatrix()
        {
            return Matrix4.CreateRotationX(Rotation.X)
                * Matrix4.CreateRotationY(Rotation.Y)
                * Matrix4.CreateTranslation(Position);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        /// <summary>
        /// Looks up the terrain height at the specified world X/Z coordinates using interpolated heightmap data.
        /// </summary>
        private float ComputeY(float x, float z)
        {
            if (_heightMap == null)
                return Position.Y - _eyeLevel;

            // Convert world coordinates to grid coordinates.
            float gx = _terrainWidth  * 0.5f - 0.5f + x;
            float gz = _terrainHeight * 0.5f - 0.5f + z;

            int ix = (int)MathF.Floor(gx);
            int iz = (int)MathF.Floor(gz);

            // Need a full cell, so the last valid cell starts at width-2 / height-2.
            if ((uint)ix >= (uint)(_terrainWidth - 1) || (uint)iz >= (uint)(_terrainHeight - 1))
                return Position.Y - _eyeLevel;

            float tx = gx - ix;
            float tz = gz - iz;

            float h00 = _heightMap[iz, ix];
            float h10 = _heightMap[iz, ix + 1];
            float h01 = _heightMap[iz + 1, ix];
            float h11 = _heightMap[iz + 1, ix + 1];

            float hx0 = Lerp(h00, h10, tx);
            float hx1 = Lerp(h01, h11, tx);
            return Lerp(hx0, hx1, tz);
        }

        /// <summary>
        /// Evaluates the angle of the terrain between two positions to determine if it is navigable.
        /// </summary>
        private int EvalSlope(Vector3 position1, Vector3 position2)
        {
            Vector2 normalizedFlattenedDir = new Vector2(position2.X - position1.X, position2.Z - position1.Z).Normalized() * .3f; // 30 cm in viewing direction
            Vector3 normalizedPosition2 = new Vector3(
                position1.X + normalizedFlattenedDir.X, 
                ComputeY(position1.X + normalizedFlattenedDir.X, position1.Z + normalizedFlattenedDir.Y), 
                position1.Z + normalizedFlattenedDir.Y
            );
            
            float deltaY = normalizedPosition2.Y - position1.Y; // positive in an upwards slope, negative in a downwards slope
            float horizontalDistance = MathF.Sqrt((normalizedPosition2.X - position1.X) * (normalizedPosition2.X - position1.X) + (normalizedPosition2.Z - position1.Z) * (normalizedPosition2.Z - position1.Z));
            float slopeAngle = MathF.Atan2(deltaY, horizontalDistance);
            
            if (slopeAngle > MathF.PI / 3) return 2; // 2 = too steep to walk up
            else if (slopeAngle > 2f * MathF.PI / 9) return 1; // 1 = moderately steep
            else if (slopeAngle < -MathF.PI / 3) return -1; // -1 = falling off ledge
            
            return 0; // 0 = normal slope, safe to traverse
        }          
    }
}