using Accelerated_Delivery_Win;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using ConversionHelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LD30
{
    public class Editor
    {
        public World EditableWorld { get; private set; }
        private Character character; 

        public bool IsOpen { get; private set; }

        private EditorState state; 
        private enum EditorState { Placing, Menu }

        private PropInstance placingObject;
        private PropInstance hoveredObject;
        private Model cube;
        private bool validLocation;
        private bool collidingWithCharacter;

        private Vector3 currGridPos;

        private readonly Color[] palette = new Color[] { new Color(214, 0, 55), new Color(0, 215, 55),
            new Color(0, 55, 214), new Color(108, 226, 255), new Color(226, 226, 109), new Color(226, 109, 226),
            new Color(131, 78, 38), new Color(219, 117, 0), new Color(13, 13, 13), new Color(78, 78, 78), new Color(147, 148, 147),
            new Color(221, 223, 221) };
        private const int red = 0;
        private const int green = 1;
        private const int blue = 2;
        private const int cyan = 3;
        private const int yellow = 4;
        private const int pink = 5;
        private const int brown = 6;
        private const int orange = 7;
        private const int black = 8;
        private const int darkGrey = 9;
        private const int lightGrey = 10;
        private const int white = 11;

        private Sprite background;
        private Sprite[] tabs = new Sprite[5];
        private Sprite[][] props = new Sprite[5][];
        private Sprite[] colors = new Sprite[12];
        private Texture2D[][] thumbnails = new Texture2D[5][];

        private int selectedColor = lightGrey;
        private int hoveredColor = -1;
        private int selectedTab;
        private int hoveredTab = -1;
        private int selectedProp = -1;
        private int hoveredProp = -1;
        private bool holdingButton;

        private Color selectTint = Color.DarkRed;
        private Color hoverTint = Color.Goldenrod;

        private Rectangle innerPropWidth = new Rectangle(0, 0, 110 / 2, 110 / 2);
        private Rectangle innerColorWidth = new Rectangle(0, 0, 52 / 2 + 2, 52 / 2);
        private Rectangle textRectangle;

        private float currentScaleFactor = 1;

        private HelpfulTextBox text;

        public Editor(World w, Character c)
        {
            character = c;
            EditableWorld = w;
            cube = Prop.PropAssociations[0].Model;
            state = EditorState.Menu;

            Texture2D ui = Program.Game.Loader.editorUI;
            Rectangle tabRect = new Rectangle(0, 0, 77, 23);
            Rectangle scaleUpRect = new Rectangle(0, 23, 23, 35);
            Rectangle scaleDownRect = new Rectangle(23, 23, 24, 36);
            Rectangle propRect = new Rectangle(77, 0, 61, 62);
            Rectangle rotationRect = new Rectangle(77 + 61, 0, 62, 67);
            Rectangle colorRect = new Rectangle(77 + 62 + 61, 0, 33, 31);
            Rectangle backgroundRect = new Rectangle(0, 67, 465, 414);

            background = new Sprite(() => ui, new Vector2(RenderingDevice.Width * 0.6f, RenderingDevice.Height * 0.352f - backgroundRect.Height / 2), backgroundRect, Sprite.RenderPoint.UpLeft);
            Vector2 backgroundPos = background.UpperLeft;
            for(int i = 0; i < tabs.Length; i++)
            {
                tabs[i] = new Sprite(() => ui, backgroundPos + new Vector2(59f / 2 + (tabRect.Width + 10 / 2) * i, 54 / 2), tabRect, Sprite.RenderPoint.UpLeft);
                props[i] = new Sprite[10];
                for(int j = 0; j < props[i].Length; j++)
                    props[i][j] = new Sprite(() => ui, backgroundPos + new Vector2(96 / 2 + (propRect.Width + 30 / 2) * (j % 5), 145f / 2 + (propRect.Height + 30 / 2) * (j / 5)), propRect, Sprite.RenderPoint.UpLeft);
            }
            for(int i = 0; i < colors.Length; i++)
                colors[i] = new Sprite(() => ui, backgroundPos + new Vector2(460 / 2 + (colorRect.Width + 40 / 2) * (i % 4), 484 / 2 + (colorRect.Height + 40 / 2) * (i / 4)), colorRect, Sprite.RenderPoint.UpLeft);

            Matrix viewProj = Matrix.CreateLookAt(new Vector3(5), Vector3.Zero, Vector3.UnitZ) * Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, (float)innerPropWidth.Width / innerPropWidth.Height, 1, 10);
            for(int i = 0; i < 5; i++)
            {
                thumbnails[i] = new Texture2D[10];
                var props = PropCategory.Categories[i].Props;
                for(int j = 0; j < props.Count; j++)
                {
                    RenderTarget2D temp = new RenderTarget2D(Renderer.GraphicsDevice, innerPropWidth.Width, innerPropWidth.Height);
                    Renderer.GraphicsDevice.SetRenderTarget(temp);
                    Renderer.GraphicsDevice.Clear(Color.DarkSlateGray);
                    ModelMesh mesh = props[j].Model.Meshes[0];
                    string tech = "ShadowedScene";
                    foreach(Effect currentEffect in mesh.Effects)
                    {
                        currentEffect.CurrentTechnique = currentEffect.Techniques[tech];

                        currentEffect.Parameters["xColor"].SetValue(new Vector4(0.8f, 0.8f, 0.8f, 1));

                        currentEffect.Parameters["xCamerasViewProjection"].SetValue(viewProj);
                        currentEffect.Parameters["xWorld"].SetValue(mesh.ParentBone.Transform * Matrix.CreateScale(2));// * Camera.World);
                        currentEffect.Parameters["xLightPos"].SetValue(new Microsoft.Xna.Framework.Vector3(0, 0, 10));
                        currentEffect.Parameters["xLightPower"].SetValue(0.5f);
                        currentEffect.Parameters["xAmbient"].SetValue(0.75f);
                        currentEffect.Parameters["xLightDir"].SetValue(-Vector3.UnitZ);

                        currentEffect.Parameters["xEnableClipping"].SetValue(false);
                        currentEffect.Parameters["xEnableCustomAlpha"].SetValue(false);
                    }
                    mesh.Draw();
                    Renderer.GraphicsDevice.SetRenderTarget(null);
                    thumbnails[i][j] = temp;
                }
            }

            textRectangle = new Rectangle(80 / 2 + (int)background.UpperLeft.X, 488 / 2 + (int)background.UpperLeft.Y,
                360 / 2, 248 / 2);
            text = new HelpfulTextBox(textRectangle, () => Program.Game.Loader.SmallerFont);
            text.SetTextColor(Color.Black);
            textRectangle.Inflate(3, 3);
        }

        public void Open()
        {
            IsOpen = true;
        }

        public void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            if(!IsOpen)
                return;

            if(state == EditorState.Menu)
            {
                RenderingDevice.SpriteBatch.Begin();
                background.Draw();
                RenderingDevice.SpriteBatch.Draw(Program.Game.Loader.EmptyTex, textRectangle, Color.LightSlateGray * 0.3f);
                for(int i = 0; i < tabs.Length; i++)
                {
                    if(i == selectedTab)
                    {
                        tabs[i].Draw(selectTint);
                        for(int j = 0; j < props[i].Length; j++)
                        {
                            if(thumbnails[i][j] != null)
                                RenderingDevice.SpriteBatch.Draw(thumbnails[i][j],
                                    new Rectangle((int)props[i][j].Center.X - innerPropWidth.Width / 2, (int)props[i][j].Center.Y - innerPropWidth.Height / 2, innerPropWidth.Width, innerPropWidth.Height),
                                    Color.White);
                            props[i][j].Draw(thumbnails[i][j] != null ? (j == selectedProp ? selectTint : (j == hoveredProp ? hoverTint : Color.White)) : Color.Gray);
                            if(j == hoveredProp)
                                text.Draw(PropCategory.Categories[i].Props[j].Name + "\n\n" + PropCategory.Categories[i].Props[j].Description);
                        }
                    }
                    else
                        tabs[i].Draw(i == hoveredTab ? hoverTint : Color.White);
                    RenderingDevice.SpriteBatch.DrawString(Program.Game.Loader.SmallerFont, PropCategory.Categories[i].Name,
                        tabs[i].Center + new Vector2(0, 3), Color.Black, 0, Program.Game.Loader.SmallerFont.MeasureString(PropCategory.Categories[i].Name) * 0.5f,
                        0.7f, SpriteEffects.None, 0);
                }
                for(int i = 0; i < colors.Length; i++)
                {
                    RenderingDevice.SpriteBatch.Draw(Program.Game.Loader.EmptyTex,
                        new Rectangle((int)colors[i].Center.X - innerColorWidth.Width / 2, (int)colors[i].Center.Y - innerColorWidth.Height / 2, innerColorWidth.Width, innerColorWidth.Height),
                        palette[i]);
                    colors[i].Draw(i == selectedColor ? selectTint : (i == hoveredColor ? hoverTint : Color.White));
                }
                RenderingDevice.SpriteBatch.End();
            }
        }

        public void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            if(!IsOpen)
                return;

            if(state == EditorState.Placing)
            {
                if(Input.CheckKeyboardJustPressed(Microsoft.Xna.Framework.Input.Keys.R))
                {
                    placingObject.RotationAngle = placingObject.RotationAngle + MathHelper.PiOver2;
                    if(placingObject.RotationAngle >= MathHelper.Pi)
                        placingObject.RotationAngle = 0;
                }
                if(Input.MouseState.ScrollWheelValue > Input.MouseLastFrame.ScrollWheelValue)
                {
                    Vector3 newScale = nextScale(placingObject.BaseProp.Dimensions, ref currentScaleFactor);
                    if(newScale != placingObject.Scale)
                    {
                        placingObject.Scale = newScale;
                        setEntityProps();
                    }
                }
                else if(Input.MouseState.ScrollWheelValue < Input.MouseLastFrame.ScrollWheelValue)
                {
                    Vector3 newScale = prevScale(placingObject.BaseProp.Dimensions, ref currentScaleFactor);
                    if(newScale != placingObject.Scale)
                    {
                        placingObject.Scale = newScale;
                        setEntityProps();
                    }
                }

                Vector3 near = RenderingDevice.GraphicsDevice.Viewport.Unproject(new Vector3(Input.MouseState.X, Input.MouseState.Y, 0),
                    MathConverter.Convert(Renderer.Camera.ProjectionMatrix),
                    MathConverter.Convert(Renderer.Camera.ViewMatrix),
                    MathConverter.Convert(Renderer.Camera.WorldMatrix));
                Vector3 far = RenderingDevice.GraphicsDevice.Viewport.Unproject(new Vector3(Input.MouseState.X, Input.MouseState.Y, 1),
                    MathConverter.Convert(Renderer.Camera.ProjectionMatrix),
                    MathConverter.Convert(Renderer.Camera.ViewMatrix),
                    MathConverter.Convert(Renderer.Camera.WorldMatrix));

                Vector3 forward = far - near;
                float distance = forward.Length();
                forward.Normalize();

                List<BEPUphysics.RayCastResult> results = new List<BEPUphysics.RayCastResult>();
                if(EditableWorld.Space.RayCast(new BEPUutilities.Ray(Renderer.Camera.Position, forward), distance, results))
                {
                    results.Sort(new Comparison<BEPUphysics.RayCastResult>((x, y) =>
                    {
                        float d1 = (Renderer.Camera.Position - x.HitData.Location).LengthSquared();
                        float d2 = (Renderer.Camera.Position - y.HitData.Location).LengthSquared();
                        return d1.CompareTo(d2);
                    }));
                    foreach(var result in results)
                    {
                        var instance = result.HitObject.Tag as PropInstance;
                        if(instance != null && instance != placingObject && (instance.BaseProp.IsGround || instance.BaseProp.IsWall))
                        {
                            Vector3 temp;
                            if(placingObject.BaseProp.CanPlaceOnWall && instance.BaseProp.IsWall && result.HitData.Normal != BEPUutilities.Vector3.UnitZ)
                            {
                                temp = instance.Position + MathConverter.Convert(result.HitData.Normal) * Math.Abs(Vector3.Dot(result.HitData.Normal, placingObject.CorrectedDimensions * placingObject.Scale));
                                if(EditableWorld.GridOpen(placingObject.CorrectedDimensions, placingObject.CorrectedScale, (int)temp.X, (int)temp.Y, (int)temp.Z))
                                {
                                    currGridPos = temp;
                                    placingObject.Position = currGridPos;
                                    placingObject.Entity.Position = placingObject.BaseProp.EntityCreator(currGridPos, placingObject.CorrectedScale).Position; // this is so wasteful
                                    validLocation = EditableWorld.HasSupport(placingObject.CorrectedDimensions, placingObject.CorrectedScale,
                                        -result.HitData.Normal, (int)currGridPos.X, (int)currGridPos.Y, (int)currGridPos.Z);
                                    if(validLocation)
                                        break;
                                }
                            }
                            if(placingObject.BaseProp.CanPlaceOnGround && instance.BaseProp.IsGround && result.HitData.Normal == BEPUutilities.Vector3.UnitZ)
                            {
                                currGridPos = instance.Position + Vector3.UnitZ;
                                placingObject.Position = currGridPos;
                                placingObject.Entity.Position = placingObject.BaseProp.EntityCreator(currGridPos, placingObject.CorrectedScale).Position; // this is so wasteful
                                validLocation = placingObject.BaseProp.CanPlaceOnWall ? EditableWorld.GridOpen(placingObject.CorrectedDimensions, placingObject.CorrectedScale, (int)currGridPos.X, (int)currGridPos.Y, (int)currGridPos.Z) :
                                                                                       EditableWorld.ValidPosition(placingObject, (int)currGridPos.X, (int)currGridPos.Y, (int)currGridPos.Z);
                                break;
                            }
                        }
                    }
                }
                else
                    validLocation = false;
                if(Input.CheckMouseJustClicked(Program.Game.IsActive) && validLocation && !collidingWithCharacter)
                    endPlace(true);
                else if(Input.CheckMouseJustClicked(2))
                    endPlace(false);
            }
            else
            {
                if(Input.CheckMouseJustClicked(2))
                {
                    IsOpen = false;
                    if(hoveredObject != null)
                    {
                        hoveredObject.Transparent = false;
                        hoveredObject.Alpha = 1;
                        hoveredObject = null;
                    }
                    return;
                }

                hoveredColor = hoveredProp = hoveredTab = -1;
                for(int i = 0; i < tabs.Length; i++)
                    if(Input.CheckMouseWithinCoords(tabs[i]))
                    {
                        hoveredTab = i;
                        if(Input.CheckMouseJustClicked(Program.Game.IsActive))
                        {
                            selectedTab = i;
                            MediaSystem.PlaySoundEffect(SFXOptions.Button_Press);
                        }
                        break; // no other possible selections
                    }
                if(hoveredTab == -1)
                    for(int j = 0; j < props[selectedTab].Length; j++)
                    {
                        if(thumbnails[selectedTab][j] == null)
                            break; // out of valid selectors
                        if(Input.CheckMouseWithinCoords(props[selectedTab][j]))
                        {
                            hoveredProp = j;
                            if(Input.CheckMouseJustClicked(Program.Game.IsActive))
                            {
                                selectedProp = j;
                                holdingButton = true;
                                MediaSystem.PlaySoundEffect(SFXOptions.Button_Press);
                            }
                            else if(Input.MouseState.LeftButton == ButtonState.Released && Input.MouseLastFrame.LeftButton == ButtonState.Pressed && holdingButton)
                            {
                                selectedProp = -1;
                                holdingButton = false;
                                MediaSystem.PlaySoundEffect(SFXOptions.Button_Release);
                                character.Entity.CameraControlScheme.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
                                character.Entity.CameraControlScheme.Camera.Update(gameTime);
                                beginPlace(PropCategory.Categories[selectedTab].Props[j].CreateInstance(new Vector3(),
                                    Vector3.One, 0, palette[selectedColor], false, EditableWorld));
                            }
                            else if(Input.MouseState.LeftButton == ButtonState.Released)
                                holdingButton = false;
                            break; // no other possible selections
                        }
                    }
                if(hoveredTab == -1 && hoveredProp == -1)
                    for(int i = 0; i < colors.Length; i++)
                        if(Input.CheckMouseWithinCoords(colors[i]))
                        {
                            hoveredColor = i;
                            if(Input.CheckMouseJustClicked(Program.Game.IsActive))
                            {
                                selectedColor = i;
                                MediaSystem.PlaySoundEffect(SFXOptions.Button_Press);
                            }
                            break;
                        }
                if(!Input.CheckMouseWithinCoords(background))
                {
                    Vector3 near = RenderingDevice.GraphicsDevice.Viewport.Unproject(new Vector3(Input.MouseState.X, Input.MouseState.Y, 0),
                        MathConverter.Convert(Renderer.Camera.ProjectionMatrix),
                        MathConverter.Convert(Renderer.Camera.ViewMatrix),
                        MathConverter.Convert(Renderer.Camera.WorldMatrix));
                    Vector3 far = RenderingDevice.GraphicsDevice.Viewport.Unproject(new Vector3(Input.MouseState.X, Input.MouseState.Y, 1),
                        MathConverter.Convert(Renderer.Camera.ProjectionMatrix),
                        MathConverter.Convert(Renderer.Camera.ViewMatrix),
                        MathConverter.Convert(Renderer.Camera.WorldMatrix));

                    Vector3 forward = far - near;
                    float distance = forward.Length();
                    forward.Normalize();

                    BEPUphysics.RayCastResult result;
                    if(EditableWorld.Space.RayCast(new BEPUutilities.Ray(Renderer.Camera.Position, forward), out result))
                    {
                        PropInstance instance = result.HitObject.Tag as PropInstance;
                        if(instance != null && !instance.Immobile)
                        {
                            if(hoveredObject != null)
                            {
                                hoveredObject.Transparent = false;
                                hoveredObject.Alpha = 1;
                            }
                            hoveredObject = instance;
                            hoveredObject.Transparent = true;
                            hoveredObject.Alpha = 0.3f;
                            if(Input.CheckMouseJustClicked(Program.Game.IsActive))
                            {
                                EditableWorld.RemoveObject(hoveredObject);
                                beginPlace(hoveredObject);
                                hoveredObject = null;
                            }
                        }
                        else if(hoveredObject != null && hoveredObject != instance)
                        {
                            hoveredObject.Transparent = false;
                            hoveredObject.Alpha = 1;
                            hoveredObject = null;
                        }
                    }
                    else if(hoveredObject != null)
                    {
                        hoveredObject.Transparent = false;
                        hoveredObject.Alpha = 1;
                        hoveredObject = null;
                    }
                }
            }
        }

        private void beginPlace(PropInstance i)
        {
            state = EditorState.Placing;
            placingObject = i;
            placingObject.Transparent = true;
            placingObject.Alpha = 0.3f;
            // generate collision detection, but do not collide
            setEntityProps();
            EditableWorld.Space.Add(i.Entity);
            if(i.BaseProp.ID != 0) // cubes are weird to render
                Renderer.Add(i);
            Renderer.Add(cube, getRenderData, true);
        }

        void Events_CollisionEnded(EntityCollidable sender, Collidable other, CollidablePairHandler pair)
        {
            if((other as EntityCollidable) != null && (other as EntityCollidable).Entity.Tag == character)
                collidingWithCharacter = false;
        }

        void Events_DetectingInitialCollision(EntityCollidable sender, Collidable other, CollidablePairHandler pair)
        {
            if((other as EntityCollidable) != null && (other as EntityCollidable).Entity.Tag == character)
                collidingWithCharacter = true;
        }

        private Vector3 nextScale(Vector3 dim, ref float currentScaleFactor)
        {
            float tempScaleFactor = currentScaleFactor + 0.25f;
            do
            {
                Vector3 temp = dim * tempScaleFactor;
                if(temp.X / (int)temp.X == 1 && temp.Y / (int)temp.Y == 1 && temp.Z / (int)temp.Z == 1)
                {
                    currentScaleFactor = tempScaleFactor;
                    tempScaleFactor = 5.01f;
                }
                else
                    tempScaleFactor += 0.25f;
            } while(tempScaleFactor <= 5);
            return dim * currentScaleFactor;
        }

        private Vector3 prevScale(Vector3 dim, ref float currentScaleFactor)
        {
            float tempScaleFactor = currentScaleFactor - 0.25f;
            do
            {
                Vector3 temp = dim * tempScaleFactor;
                if(temp.X / (int)temp.X == 1 && temp.Y / (int)temp.Y == 1 && temp.Z / (int)temp.Z == 1)
                {
                    currentScaleFactor = tempScaleFactor;
                    return dim * currentScaleFactor;
                }
                else
                    tempScaleFactor -= 0.25f;
            } while(tempScaleFactor >= 1);
            currentScaleFactor = 1;
            return dim * currentScaleFactor;
        }

        private void setEntityProps()
        {
            placingObject.Entity.CollisionInformation.CollisionRules.Personal = CollisionRule.NoSolver;
            placingObject.Entity.CollisionInformation.Events.DetectingInitialCollision += Events_DetectingInitialCollision;
            placingObject.Entity.CollisionInformation.Events.CollisionEnded += Events_CollisionEnded;
        }

        private void endPlace(bool actuallyPlace)
        {
            float rot = placingObject.RotationAngle;
            Vector3 scale = placingObject.Scale;
            Prop p = placingObject.BaseProp;

            state = EditorState.Menu;
            EditableWorld.Space.Remove(placingObject.Entity);
            Renderer.Remove(placingObject);
            Renderer.Remove(cube);
            placingObject.Entity.CollisionInformation.CollisionRules.Personal = CollisionRule.Defer;
            placingObject.Entity.CollisionInformation.Events.DetectingInitialCollision -= Events_DetectingInitialCollision;
            placingObject.Entity.CollisionInformation.Events.CollisionEnded -= Events_CollisionEnded;
            placingObject.Transparent = false;
            placingObject.Alpha = 1;
            if(actuallyPlace)
                EditableWorld.AddObject(placingObject, (int)currGridPos.X, (int)currGridPos.Y, (int)currGridPos.Z);
            placingObject = null;
            if(actuallyPlace && (Input.KeyboardState.IsKeyDown(Keys.LeftShift) || Input.KeyboardState.IsKeyDown(Keys.RightShift)))
                beginPlace(p.CreateInstance(new Vector3(), scale, rot, palette[selectedColor], false, EditableWorld));
            else
                currentScaleFactor = 1;
        }

        private void getRenderData(out Matrix world, out Color color, out float transparency)
        {
            // extra scale term to prevent clipping when aligned on edges
            world = Matrix.CreateScale(0.99f) * Matrix.CreateScale(placingObject.BaseProp.Dimensions) * Matrix.CreateScale(placingObject.Scale) * MathConverter.Convert(placingObject.Entity.WorldTransform);
            color = validLocation && !collidingWithCharacter ? Color.Green : Color.Red;
            transparency = 0.5f;
            color *= transparency;
        }
    }
}
