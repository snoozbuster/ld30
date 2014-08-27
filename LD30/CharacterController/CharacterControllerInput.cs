using BEPUphysics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using ConversionHelper;
using BEPUutilities;
using LD30;
using Accelerated_Delivery_Win;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.CollisionRuleManagement;

namespace BEPUphysicsDemos.AlternateMovement.Character
{
    /// <summary>
    /// Handles input and movement of a character in the game.
    /// Acts as a simple 'front end' for the bookkeeping and math of the character controller.
    /// </summary>
    public class CharacterControllerInput
    {
        /// <summary>
        /// Gets the camera to use for input.
        /// </summary>
        public Camera Camera { get; private set; }

        /// <summary>
        /// Physics representation of the character.
        /// </summary>
        public CharacterController CharacterController;

        /// <summary>
        /// Gets the camera control scheme used by the character input.
        /// </summary>
        public CharacterCameraControlScheme CameraControlScheme { get; private set; }

        /// <summary>
        /// Gets whether the character controller's input management is being used.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Owning space of the character.
        /// </summary>
        public Space Space { get; private set; }

        /// <summary>
        /// Constructs the character and internal physics character controller.
        /// </summary>
        /// <param name="owningSpace">Space to add the character to.</param>
        /// <param name="camera">Camera to attach to the character.</param>
        /// <param name="game">The running game.</param>
        public CharacterControllerInput(Space owningSpace, Camera camera, BaseGame game)
        {
            CharacterController = new CharacterController(new Vector3(World.MaxSideLength / 2, World.MaxSideLength / 2, 5), 2.7f, 2, .4f, 15);
            Camera = camera;
            CameraControlScheme = new CharacterCameraControlScheme(CharacterController, camera, game);
            Space = owningSpace;
        }

        /// <summary>
        /// Gives the character control over the Camera and movement input.
        /// </summary>
        public void Activate()
        {
            if (!IsActive)
            {
                IsActive = true;
                Space.Add(CharacterController);
                //Offset the character start position from the camera to make sure the camera doesn't shift upward discontinuously.
                //CharacterController.Body.Position = Camera.Position - new Vector3(0, 0, CameraControlScheme.StandingCameraOffset);
                Camera.Position = CharacterController.Body.Position + new Vector3(-10, -10, 10);
            }
        }

        /// <summary>
        /// Returns input control to the Camera.
        /// </summary>
        public void Deactivate()
        {
            if (IsActive)
            {
                IsActive = false;
                Space.Remove(CharacterController);
            }
        }


        /// <summary>
        /// Handles the input and movement of the character.
        /// </summary>
        /// <param name="dt">Time since last frame in simulation seconds.</param>
        /// <param name="previousKeyboardInput">The last frame's keyboard state.</param>
        /// <param name="keyboardInput">The current frame's keyboard state.</param>
        /// <param name="previousGamePadInput">The last frame's gamepad state.</param>
        /// <param name="gamePadInput">The current frame's keyboard state.</param>
        public void Update(float dt)
        {
            if(IsActive)
            {
                //Note that the character controller's update method is not called here; this is because it is handled within its owning space.
                //This method's job is simply to tell the character to move around.

                CameraControlScheme.Update(dt);

                Vector2 totalMovement = Vector2.Zero;

                if(Input.ControlScheme == ControlScheme.Keyboard)
                {
                    //Collect the movement impulses.

                    if(Input.KeyboardState.IsKeyDown(Keys.W) || Input.KeyboardState.IsKeyDown(Keys.Up))
                        totalMovement += new Vector2(1, 0);
                    if(Input.KeyboardState.IsKeyDown(Keys.S) || Input.KeyboardState.IsKeyDown(Keys.Down))
                        totalMovement += new Vector2(-1, 0);
                    if(Input.KeyboardState.IsKeyDown(Keys.A) || Input.KeyboardState.IsKeyDown(Keys.Left))
                        totalMovement += new Vector2(0, 1);
                    if(Input.KeyboardState.IsKeyDown(Keys.D) || Input.KeyboardState.IsKeyDown(Keys.Right))
                        totalMovement += new Vector2(0, -1);
                    if(totalMovement == Vector2.Zero)
                        CharacterController.HorizontalMotionConstraint.MovementDirection = Vector2.Zero;
                    else
                    {
                        Vector3 v = Microsoft.Xna.Framework.Vector3.Transform(new Vector3(Vector2.Normalize(totalMovement), 0), Camera.Rotation);
                        CharacterController.HorizontalMotionConstraint.MovementDirection = new Vector2(v.X, v.Y);
                    }

                    CharacterController.StanceManager.DesiredStance = Stance.Standing;
                    //CharacterController.StanceManager.DesiredStance = Input.KeyboardState.IsKeyDown(Keys.LeftShift) ? Stance.Crouching : Stance.Standing;

                    //Jumping
                    if(Input.KeyboardLastFrame.IsKeyUp(Keys.Space) && Input.KeyboardState.IsKeyDown(Keys.Space))
                    {
                        CharacterController.Jump();
                    }
                }
                CharacterController.ViewDirection = -Camera.ViewMatrix.Forward;
            }
        }

        bool RayCastFilter(BroadPhaseEntry entry)
        {
            return entry != CharacterController.Body.CollisionInformation && entry.CollisionRules.Personal <= CollisionRule.Normal;
        }
    }
}