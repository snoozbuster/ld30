using Accelerated_Delivery_Win;
using BEPUphysics;
using BEPUutilities;
using ConversionHelper;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace LD30
{
    public class World
    {
        public const int MaxSideLength = 32;
        public const int MaxHeight = 7;

        public readonly string OwnerName;
        public List<PropInstance> Objects { get { return new List<PropInstance>(objects); } }
        private List<PropInstance> objects;

        public Vector3 WorldPosition { get; private set; }

        private Texture2D polygonTex;

        private World(string name, string data)
        {
            effect = new BasicEffect(RenderingDevice.GraphicsDevice);
            foreach(ModelMesh mesh in Prop.PropAssociations[0].Model.Meshes)
                foreach(ModelMeshPart part in mesh.MeshParts)
                    part.Effect = effect;

            WorldPosition = Vector3.Zero;
            OwnerName = name;
            objects = new List<PropInstance>();
            createWorld(new BinaryReader(new MemoryStream(Convert.FromBase64String(data))));
        }

        public World(string name)
        {
            effect = new BasicEffect(RenderingDevice.GraphicsDevice);
            foreach(ModelMesh mesh in Prop.PropAssociations[0].Model.Meshes)
                foreach(ModelMeshPart part in mesh.MeshParts)
                    part.Effect = effect;

            OwnerName = name;
            objects = new List<PropInstance>();
            WorldPosition = Vector3.Zero;
            createWorld();
        }

        /// <summary>
        /// Warning: may throw exceptions
        /// </summary>
        /// <param name="paste"></param>
        public World(paste paste, Vector3 worldPos)
        {
            effect = new BasicEffect(RenderingDevice.GraphicsDevice);
            foreach(ModelMesh mesh in Prop.PropAssociations[0].Model.Meshes)
                foreach(ModelMeshPart part in mesh.MeshParts)
                    part.Effect = effect;

            WorldPosition = worldPos;
            OwnerName = paste.paste_title;
            string data = null;
            using(WebClient c = new WebClient())
                data = c.DownloadString("http://pastebin.com/raw.php?i=" + paste.paste_key);
            createWorld(new BinaryReader(new MemoryStream(Convert.FromBase64String(data))));
        }

        private void createWorld()
        {
            // algorithm based on http://gameprogrammer.com/fractal.html
            Random r = new Random();
            int size = World.MaxSideLength;
            // make size the next highest power of 2
            int subSize = size;
            size += 1; // algorithm works best at 2^n+1

            float h = 0.4f; // smoothness constant; higher values are more smooth (0-1)
            float ratio = (float)Math.Pow(2, -h);
            int scale = (int)(World.MaxHeight * ratio + 0.5f);

            int[,] baseTerrainArray = new int[size,size];
            // normally, we would seed the corners. I want them zero, so forego seeding
            int stride = subSize / 2;
            while(stride != 0)
            {
                for(int i = stride; i < subSize; i += stride)
                    for(int j = stride; j < subSize; j += stride)
                        baseTerrainArray[i, j] = (int)(scale * r.NextDouble() + 
                                                       avgSquareValues(i, j, stride, baseTerrainArray) + 0.5f);

                // diamonds are harder due to wrapping
                bool oddline = false;
                for(int i = 0; i < subSize; i += stride)
                {
                    oddline = !oddline;
                    for(int j = 0; j < subSize; j += stride)
                    {
                        if(oddline && j == 0)
                            continue;

                        baseTerrainArray[i, j] = (int)(scale * r.NextDouble() + avgDiamondValues(i, j, stride, size, subSize, baseTerrainArray) + 0.5f);

                        // optionally, wrap terrain here by copying to array's other edges
                        //if(i == 0)
                        //    baseTerrainArray[subSize, j] = baseTerrainArray[i, j];
                        //if(j == 0)
                        //    baseTerrainArray[i, subSize] = baseTerrainArray[i, j];
                    }
                }

                // reduce random number range
                scale = (int)(scale * ratio + 0.5f); // add 0.5f to round; ratio will always be between 0 and 1 so this will not exceed World.MaxHeight
                stride >>= 1;
            }

            baseTerrainArray = mask(baseTerrainArray, size, r);
            baseTerrainArray = smooth(baseTerrainArray, size, 2);

            for(int i = 0; i < World.MaxSideLength; i++)
                for(int j = 0; j < World.MaxSideLength; j++)
                    for(int z = 0; z < baseTerrainArray[i, j]; z++)
                        objects.Add(Prop.PropAssociations[0].CreateInstance(new BEPUutilities.Vector3(i + 0.5f, j + 0.5f, z + 0.5f) + WorldPosition,
                            Vector3.One, 0, new Microsoft.Xna.Framework.Color(z * (255 / World.MaxHeight), z * (255 / World.MaxHeight), z * (255 / World.MaxHeight)), this));
        }

        private int[,] mask(int[,] baseTerrainArray, int size, Random r)
        {
            // distorts a circle to create a island-esque mask
            float h = 0.2f;
            int newsize = 32;
            int[] offsets = new int[newsize];
            float ratio = (float)Math.Pow(2, -h);
            int scale = (int)(4 * ratio + 0.5f);
            int subSize = newsize;
            newsize += 1; // algorithm works best at 2^n+1
            int stride = subSize / 2;
            while(stride > 0)
            {
                for(int i = stride; i < subSize; i += stride)
                    offsets[i] = (int)(scale * (r.NextDouble() - 0.5f) * 2 + (offsets[i - stride] + offsets[i + stride - 1]) / 2);

                stride >>= 1;
            }

            Vector2[] points = new Vector2[subSize];
            for(int i = 0; i < points.Length; i++)
            {
                // using a 32-gon; so each interior angle is 2*pi/32 rad
                Quaternion q = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (2 * MathHelper.Pi / subSize) * i);
                Vector3 temp = Microsoft.Xna.Framework.Vector3.Transform(new Microsoft.Xna.Framework.Vector3((-size + 1) / 2, 0, 0), q);
                float mag = temp.Length();
                points[i] = new Vector2(temp.X, temp.Y) * (mag + offsets[i]) / mag; // "add" offset to magnitude
                points[i].X = (int)(MathHelper.Clamp((int)(points[i].X + 0.5f), (-size+1) / 2, (size-2) / 2)); // round to indices
                points[i].Y = (int)(MathHelper.Clamp((int)(points[i].Y + 0.5f), (-size+1) / 2, (size-2) / 2));
            }
            RenderTarget2D target = new RenderTarget2D(RenderingDevice.GraphicsDevice, size, size);
            RenderingDevice.GraphicsDevice.SetRenderTarget(target);
            RenderingDevice.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.White);
            RenderingDevice.SpriteBatch.Begin();

            Func<int, int, int> transform = (x, y) =>
            {
                // we're inside the polygon if we can draw a line to any corner and get exactly 1 intersection
                // 0 or 2 mean we're outside
                int intersections = 0;
                Vector2 point = new Vector2(x - (size - 1) / 2, y - (size - 1) / 2);
                Vector2 ul = new Vector2(-size + 1, -size + 1) - point;
                Func<Vector2, Vector2, float> miniCross = (v1, v2) => {
                    return v1.X * v2.Y - v1.Y * v2.X;
                };
                if(ul == point)
                    intersections = 2;

                for(int i = 0; i < points.Length; i++)
                {
                    if(intersections == 2)
                        break;
                    Vector2 q = points[i];
                    Vector2 s = points[i + 1 == points.Length ? 0 : i + 1] - q;
                    float cross = miniCross(ul, s);
                    if(cross != 0)
                    {
                        float t = miniCross(q - point, s) / cross;
                        float u = miniCross(q - point, ul) / cross;
                        if(t >= 0 && t <= 1 && u >= 0 && u <= 1)
                            intersections++;
                    }
                    else if(miniCross(q - point, ul) == 0)
                    {
                        float test1, test2;
                        Vector2 temp = q - point;
                        Vector2.Dot(ref temp, ref ul, out test1);
                        temp = point - q;
                        Vector2.Dot(ref temp, ref s, out test2);
                        if((test1 >= 0 && test1 <= ul.LengthSquared()) || (test2 > 0 && test2 <= s.LengthSquared()))
                            intersections++;
                    }
                }
                if(intersections == 1)
                {
                    RenderingDevice.SpriteBatch.Draw(Program.Game.Loader.EmptyTex, new Microsoft.Xna.Framework.Rectangle(x, y, 1, 1), Microsoft.Xna.Framework.Color.Green);
                    return baseTerrainArray[x, y]; // inside polygon
                }

                float smallestDistance = 1000000000;
                Vector2 closestPoint;
                foreach(Vector2 v in points)
                {
                    float distance = (v - point).LengthSquared();
                    if(distance < smallestDistance)
                    {
                        smallestDistance = distance;
                        closestPoint = v;
                    }
                }
                smallestDistance = (float)Math.Sqrt(smallestDistance);
                RenderingDevice.SpriteBatch.Draw(Program.Game.Loader.EmptyTex, new Microsoft.Xna.Framework.Rectangle(x, y, 1, 1), Microsoft.Xna.Framework.Color.Red);
                return (int)MathHelper.Clamp(baseTerrainArray[x, y] + (int)(-Math.Pow(2, smallestDistance - 2)), 0, World.MaxHeight);
            };
            polygonTex = (Texture2D)target;
            int[,] output = new int[size, size];
            for(int i = 0; i < size; i++)
                for(int j = 0; j < size; j++)
                    output[i, j] = transform(i, j);
            for(int i = 0; i < points.Length; i++)
            {
                Vector2 c = new Vector2(size / 2, size / 2);
                Vector2 q = points[i];
                Vector2 s = points[i + 1 == points.Length ? 0 : i + 1];
                drawLine(q + c, s + c);
            }
            RenderingDevice.SpriteBatch.End();
            RenderingDevice.GraphicsDevice.SetRenderTarget(null);
            return output;
        }
        
        /// <summary>
        /// used for debug polygon drawing
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        protected void drawLine(Vector2 point1, Vector2 point2)
        {
            float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            float length = (point1 - point2).Length();

            RenderingDevice.SpriteBatch.Draw(Program.Game.Loader.EmptyTex, point1, null, Microsoft.Xna.Framework.Color.Black,
                       angle, Vector2.Zero, new Vector2(length, 1.5f), SpriteEffects.None, 0);
        }

        private int avgSquareValues(int i, int j, int stride, int[,] array)
        {
            return (array[i - stride, j - stride] +
                    array[i - stride, j + stride] +
                    array[i + stride, j - stride] +
                    array[i + stride, j + stride]) / 4;
        }

        private int avgDiamondValues(int i, int j, int stride, int size, int subSize, int[,] array)
        {
            // support wrapping
            if(i == 0)
                return (array[i, j - stride] +
                        array[i, j + stride] +
                        array[subSize - stride, j] +
                        array[i + stride, j]) / 4;
            else if(i == size - 1)
                return (array[i, j - stride] +
                        array[i, j + stride] +
                        array[i - stride, j] +
                        array[0 + stride, j]) / 4;
            else if(j == 0)
                return (array[i - stride, j] +
                        array[i + stride, j] +
                        array[i, j + stride] +
                        array[i, subSize - stride]) / 4;
            else if(j == size - 1)
                return (array[i - stride, j] +
                        array[i + stride, j] +
                        array[i, j - stride] +
                        array[i, 0 + stride]) / 4;
            else
                return (array[i - stride, j] +
                        array[i + stride, j] +
                        array[i, j - stride] +
                        array[i, j + stride]) / 4;
        }

        private int[,] smooth(int[,] input, int size, int radius)
        {
            Func<int, int> wrap = i => {
                if(i < 0)
                    return size + i;
                if(i >= size)
                    return i - size;
                return i;
            };
            int[,] output = new int[size, size];
            for(int i = 0; i < size; i++)
                for(int j = 0; j < size; j++)
                {
                    int total = 0;
                    for(int ii = i - radius; ii < i + radius; ii++)
                        for(int jj = j - radius; jj < j + radius; jj++)
                            total += input[wrap(ii), wrap(jj)];
                    output[i, j] = (int)((float)total / ((radius * 2 + 1) * (radius * 2 + 1)) + 0.5f);
                }
            return output;
        }

        private void createWorld(BinaryReader reader)
        {
            int num = reader.ReadInt32();
            for(int i = 0; i < num; i++)
                objects.Add(reader.ReadPropInstance(this));
        }

        BasicEffect effect;

        public void AddToSpace(Space s)
        {
            foreach(PropInstance p in objects)
                s.Add(p.Entity);
        }

        public void Draw(Camera c)
        {
            RenderingDevice.GraphicsDevice.BlendState = BlendState.Opaque;
            RenderingDevice.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            RenderingDevice.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            RenderingDevice.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
            RenderingDevice.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
            foreach(PropInstance p in objects)
            {
                effect.LightingEnabled = true;
                effect.AmbientLightColor = p.Color.ToVector3();
                effect.View = MathConverter.Convert(c.ViewMatrix);
                effect.Projection = MathConverter.Convert(c.ProjectionMatrix);
                effect.DirectionalLight0.Direction = -Vector3.UnitZ;

                foreach(ModelMesh mesh in p.BaseProp.Model.Meshes)
                {
                    effect.World = MathConverter.Convert(MathConverter.Convert(mesh.ParentBone.Transform) * c.WorldMatrix * p.Entity.WorldTransform);
                    mesh.Draw();
                }
            }

            if(polygonTex != null)
            {
                RenderingDevice.SpriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, null, null);
                RenderingDevice.SpriteBatch.Draw(polygonTex, new Microsoft.Xna.Framework.Rectangle(0, 0, 75, 75), Microsoft.Xna.Framework.Color.White);
                RenderingDevice.SpriteBatch.End();
            }
        }

        public string SaveToFile()
        {
            using(MemoryStream stream = new MemoryStream())
                using(BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(objects.Count);
                    foreach(PropInstance p in objects)
                        writer.Write(p);
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] temp = new byte[stream.Length];
                    stream.Read(temp, 0, temp.Length);
                    string data = Convert.ToBase64String(temp);
                    string path = Program.SavePath;
                    path = path + OwnerName + ".wld";
                    using(StreamWriter w = new StreamWriter(path))
                        w.Write(data);
                    return path;
                }
        }

        public static World FromFile(string path)
        {
            using(StreamReader reader = new StreamReader(path))
                return new World(Path.GetFileNameWithoutExtension(path), reader.ReadToEnd());
        }
    }
}
