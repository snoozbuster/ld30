using BEPUutilities;
using ConversionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LD30
{
    public class Camera
    {
        public Matrix WorldMatrix { get { return Matrix.Identity; } }
        public Matrix ViewMatrix { get; private set; }
        public Matrix ProjectionMatrix { get; private set; }

        public Matrix WorldViewProj { get { return WorldMatrix * ViewMatrix * ProjectionMatrix; } }

        public Vector3 Position { get; private set; }

        private Character character;

        public Camera(BaseGame g, Character tracking)
        {
            character = tracking;
            Position = character.Entity.Position + new Vector3(-10, -10, 10);
            ProjectionMatrix = MathConverter.Convert(Microsoft.Xna.Framework.Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, g.GraphicsDevice.Viewport.AspectRatio, .1f, 10000));
        }

        public void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            Position = character.Entity.Position + new Vector3(-10, -10, 10);

            // Compute view matrix
            ViewMatrix = MathConverter.Convert(Microsoft.Xna.Framework.Matrix.CreateLookAt(this.Position,
                                                                                           character.Entity.Position,
                                                                                           Vector3.UnitZ));
        }
    }
}
