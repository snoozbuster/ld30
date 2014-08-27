using Accelerated_Delivery_Win;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUutilities;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace LD30
{
    public class WorldGrid : ISpaceObject
    {
        private List<World> connectedWorlds = new List<World>();
        private List<PropInstance> bridges = new List<PropInstance>();

        private List<Vector3> usedPoints = new List<Vector3>();
        private Queue<string> localWorlds = new Queue<string>();
        private Queue<World> readyWorlds = new Queue<World>();
        private Queue<int> linkedWorlds = new Queue<int>();
        private object queueLock = new object();
        private int removedWorlds = 0;

        public World Host { get { return connectedWorlds[0]; } }
        public World[] Worlds { get { return connectedWorlds.ToArray(); } }
        public PropInstance[] Bridges { get { return bridges.ToArray(); } }

        Character character;

        private int worldAddTime;
        private float timer;
        private Random r = new Random();

        public WorldGrid(World host, Character character)
        {
            this.character = character;
            connectedWorlds.Add(host);

            List<string> local = new List<string>();
            foreach(string name in Directory.GetFiles(Program.SavePath, "*.wld", SearchOption.TopDirectoryOnly))
                if(Path.GetFileNameWithoutExtension(name) != host.OwnerName)
                    local.Add(name);
            shuffle(local);
            foreach(string s in local)
                localWorlds.Enqueue(s);

            worldAddTime = r.Next(1, 4) * 60;
            usedPoints.Add(new Vector3());

            Thread t = new Thread(new ThreadStart(threadedWorldDownload));
            t.IsBackground = true;
            t.Name = "world download thread";
            t.Start();
        }

        public void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if(timer > worldAddTime 
#if DEBUG
                || Input.CheckKeyboardJustPressed(Microsoft.Xna.Framework.Input.Keys.H)
#endif
                )
            {
                timer = 0;
                TryAddWorld();
                worldAddTime = r.Next(1, 4) * 60;
            }
        }

        /// <summary>
        /// Tries to add a world that has been downloaded and queued.
        /// </summary>
        public void TryAddWorld()
        {
            World newWorld = null;
            int index = 0;
            if(!Monitor.TryEnter(queueLock, TimeSpan.FromSeconds(1))) // this is on the main thread, we do not want to block for long
            {
                return;
            }
            try
            {
                if(readyWorlds.Count > 0)
                {
                    newWorld = readyWorlds.Dequeue();
                    index = linkedWorlds.Dequeue();
                }
            }
            finally
            {
                Monitor.Exit(queueLock);
            }
            if(newWorld != null)
            {
                bool success;
                PropInstance bridge = makeBridge(connectedWorlds[index - removedWorlds], newWorld, out success);
                if(success)
                {
                    connectedWorlds.Add(newWorld);
                    bridges.Add(bridge);
                    Host.Space.Add(newWorld);
                    Host.Space.Add(bridge.Entity);
                    bridge.FadeIn();
                    bridge.Alpha = 0;
                    Renderer.Add(bridge);
                    foreach(PropInstance i in newWorld.Objects)
                    {
                        i.Alpha = 0;
                        i.FadeIn();
                        Renderer.Add(i);
                    }
                }
                else
                {
                    removedWorlds++;
                    //// try to put it back
                    //if(!Monitor.TryEnter(queueLock, TimeSpan.FromSeconds(1))) // this is on the main thread, we do not want to block for long
                    //{
                    //    return;
                    //}
                    //try
                    //{
                    //    readyWorlds.Enqueue(newWorld);
                    //    linkedWorlds.Enqueue(index);
                    //}
                    //finally
                    //{
                    //    Monitor.Exit(queueLock);
                    //}
                }
            }
        }

        // called on a background thread and perpetually downloads and parses pastes
        private void threadedWorldDownload()
        {
            Random r = new Random();
            Func<Vector3> randDir = () => {
                switch(r.Next(0, 4))
                {
                    case 0: return new Vector3(0, 1, 0);
                    case 1: return new Vector3(1, 0, 0);
                    case 2: return new Vector3(-1, 0, 0);
                    case 3: return new Vector3(0, -1, 0);
                    default: throw new InvalidOperationException("help");
                }
            };
            Action readyLocalWorld = () => {
                Vector3 worldPos;
                int index;
                do
                {
                    Vector3 dir = randDir();
                    index = r.Next(0, connectedWorlds.Count + readyWorlds.Count);
                    worldPos = (index >= connectedWorlds.Count ? readyWorlds.ElementAt(index - connectedWorlds.Count) : connectedWorlds[index]).WorldPosition + dir * (World.MaxSideLength + 10);
                } while(usedPoints.Contains(worldPos));
                usedPoints.Add(worldPos);

                string filename = localWorlds.Dequeue();
                if(File.Exists(filename))
                {
                    readyWorlds.Enqueue(new World(Path.GetFileNameWithoutExtension(filename), File.ReadAllText(filename), worldPos));
                    linkedWorlds.Enqueue(index);
                }
            };
            List<paste> parsedPastes = new List<paste>();
            //Thread.Sleep(60000);
            while(true)
            {
                paste[] pastes = OnlineHandler.DownloadWorlds(); // may sleep for 100s an infinite number of times
                paste[] newPastes = pastes.Except(parsedPastes).ToArray();
                shuffle(newPastes);
                lock(queueLock)
                {
                    if(newPastes.Count() == 0 && localWorlds.Count > 0)
                        readyLocalWorld();
                    else foreach(paste p in newPastes)
                    {
                        if(readyWorlds.Select(d => p.paste_title == d.OwnerName).Contains(true) || 
                            localWorlds.Select(d => p.paste_title == Path.GetFileNameWithoutExtension(d)).Contains(true) ||
                            connectedWorlds[0].OwnerName == p.paste_title) // prevent your own world(s) from being added, as well as previously added worlds
                            continue;

                        Vector3 worldPos;
                        int index;
                        // sometimes include a local world
                        if(localWorlds.Count > 0 && r.Next(0, 4) == 0)
                            readyLocalWorld();

                        do
                        {
                            Vector3 dir = randDir();
                            index = r.Next(0, connectedWorlds.Count + readyWorlds.Count);
                            worldPos = (index >= connectedWorlds.Count ? readyWorlds.ElementAt(index - connectedWorlds.Count) : connectedWorlds[index]).WorldPosition + dir * (World.MaxSideLength + 10);
                        } while(usedPoints.Contains(worldPos));
                        usedPoints.Add(worldPos);

                        World w = new World(p, worldPos);
                        readyWorlds.Enqueue(w);
                        linkedWorlds.Enqueue(index);
                    }
                }
                parsedPastes.AddRange(newPastes);
                Thread.Sleep(300000); // five minutes before we check again
            }
        }

        private void shuffle<T>(IList<T> a)
        {
            int count = 100;
            while(count > 0 && a.Count > 0)
            {
                int index = r.Next(0, a.Count);
                T temp = a[0];
                a[0] = a[index];
                a[index] = temp;
                count--;
            }
        }

        private PropInstance makeBridge(World w1, World w2, out bool success)
        {
            PropInstance[, ,] grid1 = w1.Grid;
            PropInstance[, ,] grid2 = w2.Grid;
            Vector3 dir = w2.WorldPosition - w1.WorldPosition;
            dir.Normalize();
            Vector3 pos = Vector3.Zero;
            success = false;
            World baseWorld = dir.X > 0 || dir.Y > 0 ? w2 : w1;
            World otherWorld = baseWorld == w1 ? w2 : w1;
            // start at the center, move backwards a square, than forwards two squares, and so on
            // to keep the bridge as near to the center as possible; also, start with z near the upper limit
            // to keep the bridge as high as possible
            int offset = 1;
            for(int z = 2; z >= 0 && !success; z--)
                for(int x = (int)baseWorld.WorldPosition.X + (int)Math.Abs(dir.Y) * (World.MaxSideLength / 2 - 2); x < (int)baseWorld.WorldPosition.X + (int)Math.Abs(dir.X) + (int)Math.Abs(dir.Y) * World.MaxSideLength && !success; x += dir.Y != 0 ? offset : 1)
                    for(int y = (int)baseWorld.WorldPosition.Y + (int)Math.Abs(dir.X) * (World.MaxSideLength / 2 - 2); y < (int)baseWorld.WorldPosition.Y + (int)Math.Abs(dir.Y) + (int)Math.Abs(dir.X) * World.MaxSideLength; y += dir.X != 0 ? offset : 1)
                    {
                        // x, y in world coordinates near baseWorld; convert to grid coordinates for baseWorld
                        // baseWorld is chosen to make this easy
                        int baseWorldgridX, baseWorldgridY;
                        baseWorldgridX = x - (int)baseWorld.WorldPosition.X;
                        baseWorldgridY = y - (int)baseWorld.WorldPosition.Y;
                        // convert to grid coordinates for other world, keeping in mind the bridge is 10 units
                        int otherWorldGridX, otherWorldGridY;
                        otherWorldGridX = x - (int)Math.Abs(dir.X) * 10 - (int)otherWorld.WorldPosition.X;
                        otherWorldGridY = y - (int)Math.Abs(dir.Y) * 10 - (int)otherWorld.WorldPosition.Y;

                        // check for support only using the top two (bridge edge is 2x2, but skip the lower half)
                        if(baseWorld.HasSupport(new Vector3(1 + Math.Abs(dir.Y), 1 + Math.Abs(dir.X), 1), Vector3.One, dir.Abs(), baseWorldgridX, baseWorldgridY, z) &&
                           otherWorld.HasSupport(new Vector3(1 + Math.Abs(dir.Y), 1 + Math.Abs(dir.X), 1), Vector3.One, -dir.Abs(), otherWorldGridX, otherWorldGridY, z))
                        {
                            // found! convert to world pos (unlike most props, the bridge uses world coords, not grid coords)
                            // it turns out this process is way easier than I expected
                            success = true;
                            pos = new Vector3(x - 5 * Math.Abs(dir.X), y - 5 * Math.Abs(dir.Y), z - 0.1f) - w1.WorldPosition;
                            break;
                        }
                        // one step back, two steps forward
                        //offset = -offset;
                        //offset += Math.Sign(offset);
                    }
            if(!success)
                return null;

            PropInstance m = Program.Game.Loader.bridge.CreateInstance(pos, Vector3.One, 0, new Microsoft.Xna.Framework.Color(0.8f, 0.8f, 0.8f), true, w1);
            if(dir.Y != 0)
                m.Entity.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.PiOver2);
            m.Entity.CollisionInformation.Events.DetectingInitialCollision += (sender, other, pair) => {
                EntityCollidable otherEntity = other as EntityCollidable;
                if(otherEntity == null || otherEntity.Entity == null)
                    return;
                if(otherEntity.Entity.Tag == character)
                {
                    if(!character.DisplayingText)
                        character.DisplayBridgeText(w1.OwnerName, w2.OwnerName);
                }
            };
            m.Entity.CollisionInformation.Events.CollisionEnded += (sender, other, pair) =>
            {
                EntityCollidable otherEntity = other as EntityCollidable;
                if(otherEntity == null || otherEntity.Entity == null)
                    return;
                if(otherEntity.Entity.Tag == character)
                    character.StopDisplayingText();
            };
            return m;
        }

        public void OnAdditionToSpace(Space newSpace)
        {
            foreach(World w in connectedWorlds)
            {
                newSpace.Add(w);
                foreach(PropInstance i in w.Objects)
                    Renderer.Add(i);
            }
            foreach(PropInstance b in bridges)
            {
                newSpace.Add(b.Entity);
                Renderer.Add(b);
            }
        }

        public void OnRemovalFromSpace(Space oldSpace)
        {
            foreach(World w in connectedWorlds)
            {
                oldSpace.Remove(w);
                foreach(PropInstance i in w.Objects)
                    Renderer.Remove(i);
            }
            foreach(PropInstance b in bridges)
            {
                oldSpace.Remove(b.Entity);
                Renderer.Remove(b);
            }
        }

        public Space Space { get; set; }

        public object Tag { get; set; }
    }
}
