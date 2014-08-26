using Accelerated_Delivery_Win;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.Constraints.SingleEntity;
using BEPUphysics.Constraints.TwoEntity.Motors;
using BEPUphysics.Entities;
using BEPUphysicsDemos.AlternateMovement.Character;
using BEPUutilities;
using ConversionHelper;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LD30
{
    public class Character
    {
        public readonly CharacterControllerInput Entity;
        public readonly Model Model;

        public float Angle { get; private set; }

        public bool DisplayingText { get; private set; }
        private string text;

        private Collidable lastGround = null;

        private Dictionary<PropInstance, Vector3> fadedProps = new Dictionary<PropInstance, Vector3>();

        public Character(Model m, CharacterControllerInput e)
        {
            Model = m;
            Entity = e;
            e.CharacterController.Body.Tag = this; // safe to set tag, not collisioninformation.tag
            //motor = new SingleEntityLinearMotor(e, e.Position);
            //motor.Settings.Mode = MotorMode.VelocityMotor;
        }

        public void Update(Microsoft.Xna.Framework.GameTime gameTime, bool editorOpen)
        {
            if(Entity.CharacterController.SupportFinder.HasSupport)
                lastGround = Entity.CharacterController.SupportFinder.SupportData.Value.SupportObject;

            if(Entity.CharacterController.Body.Position.Z < -1)
            {
                Entity.CharacterController.Body.Position = (lastGround as EntityCollidable).WorldTransform.Position + new Vector3(0, 0, 3.5f);
                Entity.CharacterController.Body.LinearVelocity = Vector3.Zero;
            }
            if(!editorOpen)
            {
                Entity.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
                Vector2 movementDir = Vector2.Zero;
                if(Input.KeyboardState.IsKeyDown(Keys.W) || Input.KeyboardState.IsKeyDown(Keys.Up))
                    movementDir += new Vector2(1, 0);
                if(Input.KeyboardState.IsKeyDown(Keys.S) || Input.KeyboardState.IsKeyDown(Keys.Down))
                    movementDir += new Vector2(-1, 0);
                if(Input.KeyboardState.IsKeyDown(Keys.A) || Input.KeyboardState.IsKeyDown(Keys.Left))
                    movementDir += new Vector2(0, 1);
                if(Input.KeyboardState.IsKeyDown(Keys.D) || Input.KeyboardState.IsKeyDown(Keys.Right))
                    movementDir += new Vector2(0, -1);
                if(movementDir != Vector2.Zero)
                {
                    movementDir.Normalize();
                    Angle = (float)Math.Acos(movementDir.X);
                    if(movementDir.Y < 0)
                        Angle = -Angle;
                }
#if DEBUG
                if(Input.CheckKeyboardJustPressed(Keys.G))
                    Entity.CharacterController.Body.IsAffectedByGravity = !Entity.CharacterController.Body.IsAffectedByGravity;
#endif
            }

            Vector3 dir = (Entity.CharacterController.Body.Position + Entity.CharacterController.Body.Height * Vector3.UnitZ) - Renderer.Camera.Position;
            float length = dir.Length();
            dir.Normalize();
            List<BEPUphysics.RayCastResult> results = new List<BEPUphysics.RayCastResult>();
            var count = 3;
            while(count > 0)
            {
                if(Entity.Space.RayCast(new Ray(Renderer.Camera.Position, dir), length, results))
                {
                    foreach(var result in results)
                    {
                        var instance = result.HitObject.Tag as PropInstance;
                        if(instance != null && instance.Alpha == 1 && !fadedProps.ContainsKey(instance))
                        {
                            instance.FadeOut(0.2f);
                            fadedProps.Add(instance, dir);
                        }
                    }
                }
                else if(count == 3)
                    break;
                results.Clear();
                dir = (Entity.CharacterController.Body.Position + Entity.CharacterController.Body.Height * Vector3.UnitZ - (3 - --count) * Vector3.UnitZ * 1.25f) - Renderer.Camera.Position;
                length = dir.Length();
                dir.Normalize();
            }
            int i = 0;
            BEPUphysics.RayCastResult r;
            while(i < fadedProps.Count)
            {
                var curr = fadedProps.ElementAt(i);
                if(!Entity.Space.RayCast(new Ray(Renderer.Camera.Position, curr.Value), e => (e as EntityCollidable).Entity.Tag == fadedProps.ElementAt(i).Key, out r))
                {
                    curr.Key.FadeIn();
                    fadedProps.Remove(curr.Key);
                    i--;
                }
                i++;
            }
        }

        public void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            if(!DisplayingText)
                return;

            Vector3 projection = Renderer.GraphicsDevice.Viewport.Project(Entity.CharacterController.Body.Position + new Vector3(0, 0, Entity.CharacterController.Body.Height), 
                MathConverter.Convert(Renderer.Camera.ProjectionMatrix),
                MathConverter.Convert(Renderer.Camera.ViewMatrix),
                MathConverter.Convert(Renderer.Camera.WorldMatrix));
            float offset = 15;
            Vector2 screenCoords = new Vector2(projection.X, projection.Y);

            screenCoords.X += offset;
            screenCoords.Y -= offset;

            RenderingDevice.SpriteBatch.Begin();
            RenderingDevice.SpriteBatch.DrawString(Program.Game.Loader.SmallerFont,
                text, screenCoords, Microsoft.Xna.Framework.Color.Black);
            RenderingDevice.SpriteBatch.End();
        }

        public void DisplayBridgeText(string w1, string w2)
        {
            text = string.Format("Bridge to {0}'s world", w2);
            DisplayingText = true;
        }

        public void StopDisplayingText()
        {
            DisplayingText = false;
        }
    }
}
