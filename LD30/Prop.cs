using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.Entities;
using ConversionHelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LD30
{
    public class Prop
    {
        public static Dictionary<int, Prop> PropAssociations { get; private set; }
        private static int currentId = 0;
        static Prop() { PropAssociations = new Dictionary<int, Prop>(); }

        public readonly Model Model;
        public readonly string Description;
        public readonly string Name;
        public readonly int ID;
        public readonly bool IsGround;
        public readonly bool IsWall;
        public readonly bool CanPlaceOnWall;
        public readonly bool CanPlaceOnGround;
        public PropCategory Category { get; internal set; }
        public Vector3 Dimensions { get; private set; }
        public readonly Func<Vector3, Vector3, Entity> EntityCreator;

        public readonly Func<bool> UnlockCriteria;

        public bool Unlocked { get; private set; }

        public Prop(Model model, Vector3 dimensions, bool isGround, bool isWall, bool wall, bool ground, string name, string desc, Func<Vector3, Vector3, Entity> creator, Func<bool> unlock = null)
        {
            ID = currentId++;
            Name = name;
            Model = model;
            Description = desc;
            Dimensions = dimensions;
            CanPlaceOnWall = wall;
            CanPlaceOnGround = ground;
            IsWall = isWall;
            EntityCreator = creator;

            IsGround = isGround;

            if(unlock == null)
                UnlockCriteria = () => { return true; };
            else
                UnlockCriteria = unlock;
            Unlocked = UnlockCriteria();

            PropAssociations.Add(ID, this);
        }

        public PropInstance CreateInstance(Vector3 position, Vector3 scale, float rotation, Color color, bool immobile, World w)
        {
            return new PropInstance(scale, position, rotation, color, EntityCreator(position + (w != null ? MathConverter.Convert(w.WorldPosition) : Vector3.Zero), scale), immobile, this, w);
        }

        public static PropInstance CreateInstance(int ID, Vector3 position, Vector3 scale, float rotation, Color color, bool immobile, World w)
        {
            if(!PropAssociations.ContainsKey(ID))
                throw new ArgumentException("No such prop ID.", "ID");

            return PropAssociations[ID].CreateInstance(position, scale, rotation, color, immobile, w);
        }
    }
}
