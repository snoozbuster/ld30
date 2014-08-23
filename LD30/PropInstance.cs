using BEPUphysics.Entities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LD30
{
    public class PropInstance
    {
        public readonly Prop BaseProp;
        public readonly World ContainingWorld;
        public Vector3 Scale { get; private set; }
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        public float RotationAngle { get; private set; }
        public Color Color { get; private set; }
        public Entity Entity { get; private set; }

        public PropInstance(Vector3 scale, Vector3 position, float rotation, Color color, Entity entity, Prop baseProp, World w)
        {
            Scale = scale;
            Position = position;
            RotationAngle = rotation;
            Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rotation);
            Color = color;
            Entity = entity;
            Entity.Orientation = Rotation;
            BaseProp = baseProp;
            ContainingWorld = w;
        }


    }
}
