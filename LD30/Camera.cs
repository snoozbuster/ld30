using Accelerated_Delivery_Win;
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

        public Vector3 Position { get; set; }
        public Vector3 Offset { get; private set; }
        public Vector3 Forward { get { return Matrix.Invert(ViewMatrix).Forward; } }

        public Character Character { get; set; }

#if DEBUG
        public bool Debug { get; private set; }
#endif 

        public Camera(BaseGame g, Character tracking)
        {
            Offset = new Vector3(-10, -10, 10);
            //Character = tracking;
            //Position = Character.Entity.CharacterController.Body.Position + new Vector3(-10, -10, 10);
            ProjectionMatrix = MathConverter.Convert(Microsoft.Xna.Framework.Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, g.GraphicsDevice.Viewport.AspectRatio, .1f, 10000));
        }

        public void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            //Position = Character.Entity.CharacterController.Body.Position + new Vector3(-10, -10, 10);

            // Compute view matrix
            ViewMatrix = MathConverter.Convert(Microsoft.Xna.Framework.Matrix.CreateLookAt(this.Position,
                                                                                           Character.Entity.CharacterController.Body.Position,
                                                                                           Vector3.UnitZ));

#if DEBUG
            if(Input.CheckKeyboardJustPressed(Microsoft.Xna.Framework.Input.Keys.I))
                Debug = !Debug;
            if(Debug && Input.CheckKeyboardJustPressed(Microsoft.Xna.Framework.Input.Keys.K))
                Offset = Microsoft.Xna.Framework.Vector3.Transform(Offset, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.PiOver2));
#endif
        }
    }
}
