using BEPUphysics;
using BEPUphysics.Entities;
using ConversionHelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Accelerated_Delivery_Win;
using BEPUphysics.Entities.Prefabs;

namespace LD30
{
    delegate void RenderInformationDelegate(out Matrix world, out Color col, out float transparency);

    static class Renderer
    {
        public static Camera Camera { get; set; }
        public static GraphicsDeviceManager GDM { get; private set; }
        public static GraphicsDevice GraphicsDevice { get { return GDM.GraphicsDevice; } }
        public static bool HiDef { get; private set; }

        /// <summary>
        /// Gets the screen's aspect ratio. Same as graphics.GraphicsDevice.Viewport.AspectRatio.
        /// </summary>
        public static float AspectRatio { get { return GraphicsDevice.Viewport.AspectRatio; } }

        private static Space Space;
        private static Effect shader;

        private static Dictionary<PropInstance, bool> models = new Dictionary<PropInstance, bool>();
        private static List<ModelData> multiMeshModel = new List<ModelData>();
        //private static List<PropInstance> cubes = new List<PropInstance>();
        //private static Dictionary<CompoundBody, InstancedData> compoundBodies = new Dictionary<CompoundBody, InstancedData>();

        //private class InstancedData { Vector3 pos; Color col; public InstancedData(Vector3 p, Color c) { pos = p; col = c; } }

        //private static DynamicVertexBuffer instanceVertexBuffer;

        public static void Initialize(GraphicsDeviceManager gdm, BaseGame g, Space s, Effect sdr)
        {
#if DEBUG
            effect = new BasicEffect(gdm.GraphicsDevice);
            effect.VertexColorEnabled = true;
            effect.LightingEnabled = false;
#endif

            Space = s;
            GDM = gdm;
            OnGDMCreation(sdr);
            shader = sdr;
            HiDef = GDM.GraphicsProfile == GraphicsProfile.HiDef;
        }

        public static void Clear()
        {
            models.Clear();
            multiMeshModel.Clear();
        }

        public static void Add(PropInstance prop)
        {
            if(models.ContainsKey(prop))
            {
                models[prop] = true;
                //if(prop.BaseProp.ID == 0)
                //    cubes.Add(prop);
                return;
            }

            foreach(ModelMesh mesh in prop.BaseProp.Model.Meshes)
                for(int i = 0; i < mesh.Effects.Count; i++)
                    if(mesh.Effects[i] is BasicEffect)
                        foreach(ModelMeshPart meshPart in mesh.MeshParts)
                            meshPart.Effect = shader.Clone();
            models.Add(prop, true);
            //if(prop.BaseProp.ID == 0)
            //    cubes.Add(prop);
        }

        public static void Add(Model model, RenderInformationDelegate getData, bool transparent = false)
        {
            foreach(ModelMesh mesh in model.Meshes)
                for(int i = 0; i < mesh.Effects.Count; i++)
                {
                    if(mesh.Effects[i] is BasicEffect)
                        foreach(ModelMeshPart meshPart in mesh.MeshParts)
                            meshPart.Effect = shader.Clone();
                }
            multiMeshModel.Add(new ModelData(model, getData, transparent));
        }

        //public static void Add(CompoundBody body)
        //{
        //    if(!compoundBodies.ContainsKey(body))
        //        compoundBodies.Add(body, body.CollisionInformation.Children.Select(c => MathConverter.Convert(c.CollisionInformation.WorldTransform.Matrix)).ToArray());
        //}

        //public static void Remove(CompoundBody body)
        //{
        //    if(compoundBodies.ContainsKey(body))
        //        compoundBodies.Remove(body);
        //}

        public static void Remove(PropInstance model)
        {
            if(models.ContainsKey(model))
                models[model] = false;
            //if(model.BaseProp.ID == 0)
            //    cubes.Remove(model);
        }

        public static void Remove(Model model)
        {
            foreach(ModelData m in multiMeshModel)
                if(m.Model == model)
                    m.Active = false;
        }

        public static void Draw()
        {
            GraphicsDevice.Clear(Color.LightSlateGray);
            draw(null, MathConverter.Convert(Camera.ViewMatrix));
        }

        private static void draw(Plane? clipPlane, Matrix view)
        {
            List<PropInstance> transparentMeshes = new List<PropInstance>();

            setForTextured();
            foreach(KeyValuePair<PropInstance, bool> m in models)
                if(m.Value)
                {
                    if(!m.Key.Transparent)
                    {
                        //if(m.Key.BaseProp.ID != 0)
                            drawMesh(m.Key.BaseProp.Model.Meshes[0], m.Key, "ShadowedScene", clipPlane, view);
                    }
                    else
                        transparentMeshes.Add(m.Key);
                }
            foreach(ModelData m in multiMeshModel)
                if(m.Active && !m.Transparent)
                    drawMultiMesh(m, clipPlane, view);
            drawCharacter(Camera.Character, clipPlane, view);
            //foreach(CompoundBody instances in compoundBodies.Keys)
            //    drawCompoundBody(Prop.PropAssociations[0].Model.Meshes[0], instances, clipPlane, view);

#if DEBUG
            drawAxes();
            if(Camera.Debug)
                foreach(Entity e in Space.Entities)
                    e.CollisionInformation.BoundingBox.Draw();
#endif

            setForTransparency();
            transparentMeshes.Sort(new Comparison<PropInstance>(sortGlassList));
            foreach(PropInstance m in transparentMeshes)
                drawMesh(m.BaseProp.Model.Meshes[0], m, "ShadowedScene", clipPlane, view);
            foreach(ModelData m in multiMeshModel)
                if(m.Active && m.Transparent)
                    drawMultiMesh(m, clipPlane, view);
        }

        private static int sortGlassList(PropInstance x, PropInstance y)
        {
            Vector3 pos1, pos2;
            pos1 = x.BaseProp.Model.Meshes[0].ParentBone.Transform.Translation;
            pos2 = y.BaseProp.Model.Meshes[0].ParentBone.Transform.Translation;
            float pos1Distance = Vector3.Distance(pos1, RenderingDevice.Camera.Position);
            float pos2Distance = Vector3.Distance(pos2, RenderingDevice.Camera.Position);
            return pos2Distance.CompareTo(pos1Distance);
        }

        private static void drawCharacter(Character c, Plane? clipPlane, Matrix view)
        {
            foreach(ModelMesh mesh in c.Model.Meshes)
            {
                foreach(Effect currentEffect in mesh.Effects)
                {
                    currentEffect.CurrentTechnique = currentEffect.Techniques["ShadowedScene"];

                    //currentEffect.Parameters["Texture"].SetValue(m.Textures[i++]);

                    currentEffect.Parameters["xCamerasViewProjection"].SetValue(view * MathConverter.Convert(Camera.ProjectionMatrix));
                    currentEffect.Parameters["xWorld"].SetValue(mesh.ParentBone.Transform * Matrix.CreateRotationY(c.Angle) * Matrix.CreateTranslation(new Vector3(0, 1, 0)) * MathConverter.Convert(c.Entity.CharacterController.Body.WorldTransform));// * Camera.World);
                    currentEffect.Parameters["xColor"].SetValue(new Vector4(0.8f) { Z = 1 });
                    currentEffect.Parameters["xLightPos"].SetValue(new Microsoft.Xna.Framework.Vector3(0, 0, 10));
                    currentEffect.Parameters["xLightPower"].SetValue(0.4f);
                    currentEffect.Parameters["xAmbient"].SetValue(0.7f);
                    currentEffect.Parameters["xLightDir"].SetValue(-Vector3.UnitZ);

                    if(clipPlane.HasValue)
                    {
                        currentEffect.Parameters["xEnableClipping"].SetValue(true);
                        currentEffect.Parameters["xClipPlane"].SetValue(new Vector4(clipPlane.Value.Normal, clipPlane.Value.D));
                    }
                    else
                        currentEffect.Parameters["xEnableClipping"].SetValue(false);
                }
                mesh.Draw();
            }
        }

        private static void drawMultiMesh(ModelData m, Plane? clipPlane, Matrix view)
        {
            Matrix world;
            Color color;
            float transparency;
            m.GetData(out world, out color, out transparency);
            //int i = 0;
            foreach(ModelMesh mesh in m.Model.Meshes)
            {
                foreach(Effect currentEffect in mesh.Effects)
                {
                    currentEffect.CurrentTechnique = currentEffect.Techniques["ShadowedScene"];

                    //currentEffect.Parameters["Texture"].SetValue(m.Textures[i++]);

                    currentEffect.Parameters["xCamerasViewProjection"].SetValue(view * MathConverter.Convert(Camera.ProjectionMatrix));
                    currentEffect.Parameters["xWorld"].SetValue(mesh.ParentBone.Transform * world);// * Camera.World);
                    currentEffect.Parameters["xColor"].SetValue(color.ToVector4());
                    currentEffect.Parameters["xLightPos"].SetValue(new Microsoft.Xna.Framework.Vector3(0, 0, 10));
                    currentEffect.Parameters["xLightPower"].SetValue(0.4f);
                    currentEffect.Parameters["xAmbient"].SetValue(0.7f);
                    currentEffect.Parameters["xLightDir"].SetValue(-Vector3.UnitZ);

                    if(clipPlane.HasValue)
                    {
                        currentEffect.Parameters["xEnableClipping"].SetValue(true);
                        currentEffect.Parameters["xClipPlane"].SetValue(new Vector4(clipPlane.Value.Normal, clipPlane.Value.D));
                    }
                    else
                        currentEffect.Parameters["xEnableClipping"].SetValue(false);
                    if(m.Transparent)
                    {
                        currentEffect.Parameters["xEnableCustomAlpha"].SetValue(true);
                        currentEffect.Parameters["xCustomAlpha"].SetValue(transparency);
                    }
                    else
                        currentEffect.Parameters["xEnableCustomAlpha"].SetValue(false);
                }
                mesh.Draw();
            }
        }

        //static VertexDeclaration instanceVertexDeclaration = new VertexDeclaration
        //(
        //    new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 1),
        //    new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.Color, 0)
        //);  

        //private static void drawCompoundBody(ModelMesh mesh, CompoundBody body, Plane? clipPlane, Matrix view)
        //{
        //    Matrix[] instances = compoundBodies[body];
        //    if((instanceVertexBuffer == null) || (instances.Length > instanceVertexBuffer.VertexCount))
        //    {
        //        if(instanceVertexBuffer != null)
        //            instanceVertexBuffer.Dispose();

        //        instanceVertexBuffer = new DynamicVertexBuffer(GraphicsDevice, instanceVertexDeclaration,
        //                                                       instances.Length, BufferUsage.WriteOnly);
        //    }

        //    // Transfer the latest instance transform matrices into the instanceVertexBuffer.
        //    instanceVertexBuffer.SetData(instances, 0, instances.Length, SetDataOptions.Discard);

        //    foreach(ModelMeshPart meshPart in mesh.MeshParts)
        //    {
        //        // Tell the GPU to read from both the model vertex buffer plus our instanceVertexBuffer.
        //        GraphicsDevice.SetVertexBuffers(
        //            new VertexBufferBinding(meshPart.VertexBuffer, meshPart.VertexOffset, 0),
        //            new VertexBufferBinding(instanceVertexBuffer, 0, 1)
        //        );

        //        GraphicsDevice.Indices = meshPart.IndexBuffer;

        //        // Set up the instance rendering effect.
        //        Effect currentEffect = meshPart.Effect;
        //        currentEffect.CurrentTechnique = currentEffect.Techniques["ShadowedSceneInstanced"];
        //        currentEffect.Parameters["xCamerasViewProjection"].SetValue(view * MathConverter.Convert(Camera.ProjectionMatrix));
        //        currentEffect.Parameters["xWorld"].SetValue(mesh.ParentBone.Transform * MathConverter.Convert(body.WorldTransform));// * Camera.World);
        //        currentEffect.Parameters["xColor"].SetValue((body.Tag as PropInstance).Color.ToVector4());
        //        currentEffect.Parameters["xLightPos"].SetValue(new Microsoft.Xna.Framework.Vector3(0, 0, 10));
        //        currentEffect.Parameters["xLightPower"].SetValue(0.4f);
        //        currentEffect.Parameters["xAmbient"].SetValue(0.7f);
        //        currentEffect.Parameters["xLightDir"].SetValue(-Vector3.UnitZ);

        //        if(clipPlane.HasValue)
        //        {
        //            currentEffect.Parameters["xEnableClipping"].SetValue(true);
        //            currentEffect.Parameters["xClipPlane"].SetValue(new Vector4(clipPlane.Value.Normal, clipPlane.Value.D));
        //        }
        //        else
        //            currentEffect.Parameters["xEnableClipping"].SetValue(false);

        //        // Draw all the instance copies in a single call.
        //        foreach(EffectPass pass in effect.CurrentTechnique.Passes)
        //        {
        //            pass.Apply();

        //            GraphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0,
        //                                                    meshPart.NumVertices, meshPart.StartIndex,
        //                                                    meshPart.PrimitiveCount, instances.Length);
        //        }
        //    }
        //}

        private static void drawMesh(ModelMesh mesh, PropInstance prop, string tech, Plane? clipPlane, Matrix view)
        {
            Matrix entityWorld = ConversionHelper.MathConverter.Convert(prop.Entity.CollisionInformation.WorldTransform.Matrix);
            foreach(Effect currentEffect in mesh.Effects)
            {
                currentEffect.CurrentTechnique = currentEffect.Techniques[tech];

                currentEffect.Parameters["xColor"].SetValue(prop.Color.ToVector4());

                currentEffect.Parameters["xCamerasViewProjection"].SetValue(view * MathConverter.Convert(Camera.ProjectionMatrix));
                currentEffect.Parameters["xWorld"].SetValue(mesh.ParentBone.Transform * (prop.Transparent ? Matrix.CreateScale(0.99f) : Matrix.Identity) * Matrix.CreateScale(prop.Scale) *  entityWorld);// * Camera.World);
                currentEffect.Parameters["xLightPos"].SetValue(new Microsoft.Xna.Framework.Vector3(0, 0, 10));
                currentEffect.Parameters["xLightPower"].SetValue(0.4f);
                currentEffect.Parameters["xAmbient"].SetValue(0.7f);
                currentEffect.Parameters["xLightDir"].SetValue(-Vector3.UnitZ);

                if(clipPlane.HasValue)
                {
                    currentEffect.Parameters["xEnableClipping"].SetValue(true);
                    currentEffect.Parameters["xClipPlane"].SetValue(new Vector4(clipPlane.Value.Normal, clipPlane.Value.D));
                }
                else
                    currentEffect.Parameters["xEnableClipping"].SetValue(false);
                if(prop.Transparent)
                {
                    currentEffect.Parameters["xEnableCustomAlpha"].SetValue(true);
                    currentEffect.Parameters["xCustomAlpha"].SetValue(prop.Alpha);
                }
                else
                    currentEffect.Parameters["xEnableCustomAlpha"].SetValue(false);
            }
            mesh.Draw();
        }

        private static void setForTextured()
        {
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
            GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
        }

        private static void setForTransparency()
        {
            GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        }

        private class ModelData
        {
            public Model Model;
            public RenderInformationDelegate GetData;
            public bool Active;
            public bool Transparent;

            public ModelData(Model m, RenderInformationDelegate getData, bool transparent)
            {
                Active = true;
                Model = m;
                Transparent = transparent;
                GetData = getData;
            }
        }

        /// <summary>
        /// Be careful when you use this.
        /// </summary>
        public static void OnGDMCreation(Effect shader)
        {
#if DEBUG
            vertices = new VertexPositionColor[6];
            vertices[0] = new VertexPositionColor(new Vector3(0, 0, 0), Color.Red);
            vertices[1] = new VertexPositionColor(new Vector3(10000, 0, 0), Color.Red);
            vertices[2] = new VertexPositionColor(new Vector3(0, 0, 0), Color.Green);
            vertices[3] = new VertexPositionColor(new Vector3(0, 10000, 0), Color.Green);
            vertices[4] = new VertexPositionColor(new Vector3(0, 0, 0), Color.Blue);
            vertices[5] = new VertexPositionColor(new Vector3(0, 0, 10000), Color.Blue);
            xyz = new BasicEffect(GraphicsDevice);
            vertexBuff = new VertexBuffer(GraphicsDevice, VertexPositionColor.VertexDeclaration, vertices.Length, BufferUsage.None);
#endif
        }
        
#if DEBUG
        static VertexPositionColor[] verts = new VertexPositionColor[8];
        static BasicEffect effect;

        static int[] indices = new int[]
        {
            0, 1,
            1, 2,
            2, 3,
            3, 0,
            0, 4,
            1, 5,
            2, 6,
            3, 7,
            4, 5,
            5, 6,
            6, 7,
            7, 4,
        };

        public static void Draw(this BEPUutilities.BoundingBox box)
        {
            if(RenderingDevice.HiDef)
            {
                effect.View = MathConverter.Convert(Renderer.Camera.ViewMatrix);
                effect.Projection = MathConverter.Convert(Renderer.Camera.ProjectionMatrix);

                BEPUutilities.Vector3[] corners = box.GetCorners();
                for(int i = 0; i < 8; i++)
                {
                    verts[i].Position = corners[i];
                    verts[i].Color = Color.Goldenrod;
                }

                foreach(EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Renderer.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.LineList, verts, 0, 8, indices, 0, indices.Length / 2);
                }
            }
        }
#endif

        #region Debug - Axes
#if DEBUG
        /// <summary>
        /// This is for drawing axes. Handy.
        /// </summary>
        private static VertexPositionColor[] vertices;
        /// <summary>
        /// This is for drawing axes. Handy.
        /// </summary>
        private static VertexBuffer vertexBuff;
        /// <summary>
        /// This is for drawing axes. Handy.
        /// </summary>
        private static BasicEffect xyz;

        private static void drawAxes()
        {
            GraphicsDevice.SetVertexBuffer(vertexBuff);
            xyz.VertexColorEnabled = true;
            xyz.World = Matrix.Identity;
            xyz.View = MathConverter.Convert(Camera.ViewMatrix);
            xyz.Projection = MathConverter.Convert(Camera.ProjectionMatrix);
            xyz.TextureEnabled = false;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            foreach(EffectPass pass in xyz.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(
                    PrimitiveType.LineList, vertices, 0, 3);
            }
        }
#endif
        #endregion
    }
}
