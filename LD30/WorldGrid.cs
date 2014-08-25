using Accelerated_Delivery_Win;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUutilities;
using Microsoft.Xna.Framework.Graphics;
using System;
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

        public World Host { get { return connectedWorlds[0]; } }
        public World[] Worlds { get { return connectedWorlds.ToArray(); } }
        public PropInstance[] Bridges { get { return bridges.ToArray(); } }

        Character character;

        public WorldGrid(World host, Character character)
        {
            this.character = character;
            connectedWorlds.Add(host);

            List<string> local = new List<string>();
            foreach(string name in Directory.GetFiles(Program.SavePath, "*.wld", SearchOption.TopDirectoryOnly))
                if(Path.GetFileNameWithoutExtension(name) != host.OwnerName)
                    local.Add(name);
            int count = 100;
            Random r = new Random();
            while(count > 0 && local.Count > 0)
            {
                int index = r.Next(0, local.Count);
                string temp = local[0];
                local[0] = local[index];
                local[index] = local[0];
                count--;
            }
            foreach(string s in local)
                localWorlds.Enqueue(s);

            Thread t = new Thread(new ThreadStart(threadedWorldDownload));
            t.IsBackground = true;
            t.Name = "world download thread";
            t.Start();
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
                connectedWorlds.Add(newWorld);
                bridges.Add(makeBridge(connectedWorlds[index], newWorld));
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
            List<paste> parsedPastes = new List<paste>();
            while(true)
            {
                Thread.Sleep(300000); // five minutes before we check again
                paste[] pastes = OnlineHandler.DownloadWorlds(); // may sleep for 100s an infinite number of times
                IEnumerable<paste> newPastes = pastes.Except(parsedPastes);
                lock(queueLock)
                {
                    foreach(paste p in newPastes)
                    {
                        if(readyWorlds.Select(d => p.paste_title == d.OwnerName).Contains(true))
                            continue;

                        Vector3 worldPos;
                        int index;
                        // sometimes include a local world
                        if(localWorlds.Count > 0 && r.Next(0, 4) == 0)
                        {
                            string filename = localWorlds.Dequeue();
                            do
                            {
                                Vector3 dir = randDir();
                                index = r.Next(0, connectedWorlds.Count);
                                worldPos = connectedWorlds[index].WorldPosition + dir * World.MaxSideLength * 2 - dir * 3;
                            } while(usedPoints.Contains(worldPos));

                            readyWorlds.Enqueue(new World(Path.GetFileNameWithoutExtension(filename), File.ReadAllText(filename), worldPos));
                            linkedWorlds.Enqueue(index);
                        }

                        do
                        {
                            Vector3 dir = randDir();
                            index = r.Next(0, connectedWorlds.Count);
                            worldPos = connectedWorlds[index].WorldPosition + dir * World.MaxSideLength * 2 - dir * 3;
                        }while(usedPoints.Contains(worldPos));

                        World w = new World(p, worldPos);
                        readyWorlds.Enqueue(w);
                        linkedWorlds.Enqueue(index);
                    }
                }
                parsedPastes.AddRange(newPastes);
            }
        }

        private PropInstance makeBridge(World w1, World w2)
        {
            // todo: calculate bridge pos/rotation
            PropInstance m = Program.Game.Loader.bridge.CreateInstance(new Microsoft.Xna.Framework.Vector3(), Vector3.One, 0, new Microsoft.Xna.Framework.Color(0.8f, 0.8f, 0.8f), true, w1);
            m.Entity.CollisionInformation.Events.DetectingInitialCollision += (sender, other, pair) => {
                EntityCollidable otherEntity = other as EntityCollidable;
                if(otherEntity == null || otherEntity.Entity == null)
                    return;
                if(otherEntity.Tag == character)
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
                if(otherEntity.Tag == character)
                    character.StopDisplayingText();
            };
            return m;
        }

        public void Draw(Camera camera)
        {
            foreach(World w in connectedWorlds)
                w.Draw();
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
