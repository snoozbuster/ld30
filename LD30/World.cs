using Accelerated_Delivery_Win;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionShapes;
using BEPUphysics.Entities.Prefabs;
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
    public class World : ISpaceObject
    {
        public const int MaxSideLength = 32;
        public const int MaxHeight = 8;

        public readonly string OwnerName;
        public List<PropInstance> Objects { get { return new List<PropInstance>(objects); } }
        private List<PropInstance> objects;

        //private CompoundBody terrain;

        public Vector3 WorldPosition { get; private set; }

        public PropInstance[, ,] Grid { get { return grid; } }
        private PropInstance[, ,] grid = new PropInstance[MaxSideLength, MaxSideLength, MaxHeight];

        public World(string name, string data, Vector3 worldPos)
        {
            WorldPosition = worldPos;
            OwnerName = name;
            objects = new List<PropInstance>();
            createWorld(new BinaryReader(new MemoryStream(Convert.FromBase64String(data))));
        }

        public World(string name, string data)
            : this(name, data, Vector3.Zero)
        { }

        public World(string name)
        {
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
            WorldPosition = worldPos;
            OwnerName = paste.paste_title;
            objects = new List<PropInstance>();
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

            float h = 0.3f; // smoothness constant; higher values are more smooth (0-1), due to post-generation smoothing the initial terrain needs to be very rough
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

            //beginCompounding();
            for(int z = 0; z < World.MaxHeight; z++) // although it's slower to put this on the outside, it allows "grouping" of "layers", should it be needed
                for(int i = 0; i < World.MaxSideLength; i++)
                    for(int j = 0; j < World.MaxSideLength; j++)
                        if(z < baseTerrainArray[i, j])
                            AddObject(Prop.PropAssociations[0].CreateInstance(new BEPUutilities.Vector3(i, j, z) + WorldPosition,
                                Vector3.One, 0, new Microsoft.Xna.Framework.Color((z + 1) * (255 / World.MaxHeight), (z + 1) * (255 / World.MaxHeight), (z + 1) * (255 / World.MaxHeight)), true, this), i, j, z);
            //terrain = endCompounding();
            for(int i = 0; i < World.MaxSideLength; i++)
                for(int j = 0; j < World.MaxSideLength; j++)
                {
                    // generate things after terrain is filled in
                    int z = baseTerrainArray[i, j];
                    // chance to generate something
                    if(z != 0)
                        switch(r.Next(0, 60))
                        {
                            default: break;
                            case 4:
                            case 8: if(z != 7)
                                {
                                    Vector3 vscale = Vector3.One * (r.Next(5) == 0 ? 2 : 1);
                                    if(ValidPosition(Program.Game.Loader.ExteriorCategory.Props[0].Dimensions, vscale, i, j, z))
                                        AddObject(Program.Game.Loader.ExteriorCategory.Props[0].CreateInstance(new BEPUutilities.Vector3(i, j, z) + WorldPosition,
                                            vscale, MathHelper.PiOver2 * r.Next(0, 4), Microsoft.Xna.Framework.Color.DarkGray, true, this), i, j, z);
                                }
                                break;
                            case 12: if(z < 3)
                                    if(ValidPosition(Program.Game.Loader.ExteriorCategory.Props[1].Dimensions, i, j, z))
                                        AddObject(Program.Game.Loader.ExteriorCategory.Props[1].CreateInstance(new BEPUutilities.Vector3(i, j, z) + WorldPosition,
                                            Vector3.One, 0, Microsoft.Xna.Framework.Color.ForestGreen, true, this), i, j, z);
                                break;
                        }
                }
        }

        //private bool compounding = false;
        //private int firstNonCubeIndex = 0;
        //private void beginCompounding()
        //{
        //    compounding = true;
        //}

        //private CompoundBody endCompounding()
        //{
        //    compounding = false;
        //    firstNonCubeIndex = objects.Count + 1;
        //    return new CompoundBody(objects.Select(i => new CompoundShapeEntry(i.Entity.CollisionInformation.Shape)).ToList()) { Tag = objects[0] };
        //}

        public void AddObject(PropInstance instance, int i, int j, int z)
        {
            objects.Add(instance);
            fillGrid(instance, instance.CorrectedDimensions, instance.CorrectedScale, i, j, z);
            if(Space != null)// && !compounding)
            {
                Space.Add(instance.Entity);
                Renderer.Add(instance);
            }
        }

        public void RemoveObject(PropInstance instance)
        {
            objects.Remove(instance);
            emptyGrid(instance.CorrectedDimensions, instance.CorrectedScale, (int)instance.Position.X, (int)instance.Position.Y, (int)instance.Position.Z);
            if(Space != null)
            {
                Space.Remove(instance.Entity);
                Renderer.Remove(instance);
            }
        }

        private void emptyGrid(Microsoft.Xna.Framework.Vector3 dim, Microsoft.Xna.Framework.Vector3 scale, int i, int j, int z)
        {
            for(int ii = i; ii < dim.X * scale.X + i; ii++)
                for(int jj = j; jj < dim.Y * scale.Y + j; jj++)
                    for(int zz = z; zz < dim.Z * scale.Z + z; zz++)
                        grid[ii, jj, zz] = null;
        }

        public bool GridOpen(Vector3 dim, int i, int j, int z)
        {
            return GridOpen(dim, Vector3.One, i, j, z);
        }

        public bool HasSupport(Vector3 dim, int i, int j, int z)
        {
            return HasSupport(dim, Vector3.One, i, j, z);
        }

        public bool HasSupport(Vector3 dim, Vector3 scale, int i, int j, int z)
        {
            return HasSupport(dim, scale, -Vector3.UnitZ, i, j, z);
        }

        public bool HasSupport(Vector3 dim, Vector3 scale, Vector3 dir, int i, int j, int z)
        {
            if(i + scale.X * dim.X > World.MaxSideLength || j + dim.Y * scale.Y > World.MaxSideLength || z + dim.Z * scale.Z > World.MaxHeight)
                return false;
            if(i < 0 || j < 0 || z < 0)
                return false;

            try
            {
                // this loop uses the contents of dir to dynamically "select" two loops to run.
                // I can't imagine what weird havoc it would wreak if you fed it a non-basis vector
                for(int ii = i; ii < (dir.X == 0 ? scale.X * dim.X + i : i+1); ii++)
                    for(int jj = j; jj < (dir.Y == 0 ? dim.Y * scale.Y + j : j+1); jj++)
                        for(int zz = z; zz < (dir.Z == 0 ? dim.Z * scale.Z + z : z+1); zz++)
                            if((grid[ii + (int)dir.X, jj + (int)dir.Y, zz + (int)dir.Z] == null) || !(grid[ii + (int)dir.X, jj + (int)dir.Y, zz + (int)dir.Z].BaseProp.IsGround || grid[ii + (int)dir.X, jj + (int)dir.Y, zz + (int)dir.Z].BaseProp.IsWall))
                                return false;
            }
            catch(IndexOutOfRangeException)
            {
                // walked off the edge of the grid, no space
                // GridOpen should catch this if used in tandem
                return false;
            }
            return true;
        }

        public bool ValidPosition(PropInstance instance, int i, int j, int z)
        {
            return ValidPosition(instance.CorrectedDimensions, instance.CorrectedScale, i, j, z);
        }

        public bool ValidPosition(Vector3 dim, int i, int j, int z)
        {
            return ValidPosition(dim, Vector3.One, i, j, z);
        }

        public bool ValidPosition(Vector3 dim, Vector3 scale, int i, int j, int z)
        {
            return GridOpen(dim, scale, i, j, z) && HasSupport(dim, scale, i, j, z);
        }

        public bool GridOpen(Vector3 dim, Vector3 scale, int i, int j, int z)
        {
            if(i + scale.X * dim.X > World.MaxSideLength || j + dim.Y * scale.Y > World.MaxSideLength || z + dim.Z * scale.Z > World.MaxHeight)
                return false;
            if(i < 0 || j < 0 || z < 0)
                return false;

            for(int ii = i; ii < scale.X * dim.X + i; ii++)
                for(int jj = j; jj < dim.Y * scale.Y + j; jj++)
                    for(int zz = z; zz < dim.Z * scale.Z + z; zz++)
                        if(grid[ii, jj, zz] != null)
                            return false;
            return true;
        }

        private void fillGrid(PropInstance instance, Vector3 dim, Vector3 scale, int i, int j, int z)
        {
            for(int ii = i; ii < dim.X * scale.X + i; ii++)
                for(int jj = j; jj < dim.Y * scale.Y + j; jj++)
                    for(int zz = z; zz < dim.Z * scale.Z + z; zz++)
                        grid[ii, jj, zz] = instance;
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

            Func<int, int, int> transform = (x, y) =>
            {
                // we're inside the polygon if we can draw a line to any corner and get exactly 1 intersection
                // 0 or 2 mean we're outside
                // this function doesn't work perfectly; none of the points on y=x are "inside", and sometimes it
                // misses points in the upper-right of the polygon
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
                return (int)MathHelper.Clamp(baseTerrainArray[x, y] + (int)(-Math.Pow(2, smallestDistance - 2)), 0, World.MaxHeight);
            };
            int[,] output = new int[size, size];
            for(int i = 0; i < size; i++)
                for(int j = 0; j < size; j++)
                    output[i, j] = transform(i, j);
            return output;
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
            //beginCompounding();
            for(int i = 0; i < num; i++)
            {
                var instance = reader.ReadPropInstance(this);
                if(WorldPosition != Vector3.Zero)
                    instance.Immobile = true;
                //if(instance.BaseProp.ID != 0)
                //{
                //    firstNonCubeIndex--;
                //    terrain = endCompounding();
                //}
                AddObject(instance, (int)instance.Position.X, (int)instance.Position.Y, (int)instance.Position.Z);
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

        public void OnAdditionToSpace(Space newSpace)
        {
            foreach(PropInstance p in objects)
                newSpace.Add(p.Entity);
        }

        public void OnRemovalFromSpace(Space oldSpace)
        {
            foreach(PropInstance p in objects)
                oldSpace.Remove(p.Entity);
        }

        public Space Space { get; set; }

        public object Tag { get; set; }
    }
}
