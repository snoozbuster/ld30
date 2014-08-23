using Accelerated_Delivery_Win;
using BEPUphysics.Constraints.SingleEntity;
using BEPUphysics.Constraints.TwoEntity.Motors;
using BEPUphysics.Entities;
using BEPUutilities;
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
        public readonly Entity Entity;
        public readonly Model Model;

        private bool inAir = false;

        public bool DisplayingText { get; private set; }
        private string text;

        public Character(Model m, Entity e)
        {
            Model = m;
            Entity = e;

            //motor = new SingleEntityLinearMotor(e, e.Position);
            //motor.Settings.Mode = MotorMode.VelocityMotor;
        }

        public void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            Vector2 movementDir = Vector2.Zero;
            if(Input.ControlScheme == ControlScheme.Keyboard)
            {
                if(Input.KeyboardState.IsKeyDown(Keys.W) || Input.KeyboardState.IsKeyDown(Keys.Up))
                    movementDir += new Vector2(0, 1);
                if(Input.KeyboardState.IsKeyDown(Keys.S) || Input.KeyboardState.IsKeyDown(Keys.Down))
                    movementDir += new Vector2(0, -1);
                if(Input.KeyboardState.IsKeyDown(Keys.A) || Input.KeyboardState.IsKeyDown(Keys.Left))
                    movementDir += new Vector2(-1, 0);
                if(Input.KeyboardState.IsKeyDown(Keys.D) || Input.KeyboardState.IsKeyDown(Keys.Right))
                    movementDir += new Vector2(1, 0);
            }
            else
            {
                movementDir = Input.CurrentPad.ThumbSticks.Left;
            }
            Entity.LinearVelocity = new Vector3(movementDir, Entity.LinearVelocity.Z);
        }

        public void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            if(!DisplayingText)
                return;


        }

        public void DisplayBridgeText(string w1, string w2)
        {
            text = string.Format("Bridge between {0}'s world and {1}'s world", w1, w2);
            DisplayingText = true;
        }

        public void StopDisplayingText()
        {
            DisplayingText = false;
        }
    }
}
