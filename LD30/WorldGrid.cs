using Accelerated_Delivery_Win;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUutilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LD30
{
    public class WorldGrid
    {
        private List<World> connectedWorlds = new List<World>();
        private List<BaseModel> bridges = new List<BaseModel>();

        private Queue<World> readyWorlds = new Queue<World>();
        private Queue<int> linkedWorlds = new Queue<int>();
        private object queueLock = new object();

        public World Host { get { return connectedWorlds[0]; } }
        public World[] Worlds { get { return connectedWorlds.ToArray(); } }
        public BaseModel[] Bridges { get { return bridges.ToArray(); } }

        Character character;

        public WorldGrid(World host, Character character)
        {
            this.character = character;
            connectedWorlds.Add(host);

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
                        Vector3 dir = randDir();
                        int index = r.Next(0, connectedWorlds.Count);
                        World w = new World(p, connectedWorlds[index].WorldPosition + dir * World.MaxSideLength * 2 - dir * 3);
                        readyWorlds.Enqueue(w);
                        linkedWorlds.Enqueue(index);
                    }
                }
                parsedPastes.AddRange(newPastes);
            }
        }

        private BaseModel makeBridge(World w1, World w2)
        {
            // todo: calculate bridge pos/rotation
            BaseModel m = new BaseModel(delegate { return Program.Game.Loader.bridge; },
                false, null, new Microsoft.Xna.Framework.Vector3());
            m.Ent.CollisionInformation.Events.DetectingInitialCollision += (sender, other, pair) => {
                EntityCollidable otherEntity = other as EntityCollidable;
                if(otherEntity == null || otherEntity.Entity == null)
                    return;
                if(otherEntity.Tag == character)
                {
                    if(!character.DisplayingText)
                        character.DisplayBridgeText(w1.OwnerName, w2.OwnerName);
                }
            };
            m.Ent.CollisionInformation.Events.CollisionEnded += (sender, other, pair) => {
                EntityCollidable otherEntity = other as EntityCollidable;
                if(otherEntity == null || otherEntity.Entity == null)
                    return;
                if(otherEntity.Tag == character)
                    character.StopDisplayingText();
            };
            return m;
        }
    }
}
