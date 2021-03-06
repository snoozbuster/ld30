﻿using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.Entities;
using ConversionHelper;
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
        public Vector3 Scale { get { return scale; } set { scale = value; if(Entity != null && Entity.Space != null) { var s = Entity.Space; s.Remove(Entity); Entity = BaseProp.EntityCreator(Position, value); Entity.Orientation = Rotation; Entity.Tag = Entity.CollisionInformation.Tag = this; s.Add(Entity); } } } // can't scale entities
        private Vector3 scale;
        public Vector3 InitialEntityTranslation { get; private set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; private set; }
        public float RotationAngle { get { return rotation; } set { rotation = value; Entity.Orientation = Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rotation); } }
        private float rotation;
        public Color Color { get; set; }
        public Entity Entity { get; private set; }
        public bool Immobile { get; set; }

        public Vector3 CorrectedDimensions { get { if(rotation == MathHelper.PiOver2 || rotation == 3 * MathHelper.PiOver2) return new Vector3(BaseProp.Dimensions.Y, BaseProp.Dimensions.X, BaseProp.Dimensions.Z); return BaseProp.Dimensions; } }
        public Vector3 CorrectedScale { get { if(rotation == MathHelper.PiOver2 || rotation == 3 * MathHelper.PiOver2) return new Vector3(Scale.Y, Scale.X, Scale.Z); return Scale; } }

        public bool Transparent { get; set; }
        public float Alpha { get; set; }

        protected const float deltaA = 0.07f;
        protected int fadeDirection = 0;
        protected float minAlpha;

        public PropInstance(Vector3 scale, Vector3 position, float rotation, Color color, Entity entity, bool immobile, Prop baseProp, World w)
        {
            Color = color;
            BaseProp = baseProp;
            ContainingWorld = w;
            Alpha = 1;
            Immobile = immobile;
            this.scale = scale;
            Position = position;
            Entity = entity;
            RotationAngle = rotation;
            Entity.Orientation = Rotation;
            if(ContainingWorld != null)
            {
                Entity.Position = position + CorrectedDimensions * CorrectedScale * 0.5f;
                if(Entity.Tag != null && Entity.Tag is BEPUutilities.Vector3)
                {
                    InitialEntityTranslation = (BEPUutilities.Vector3)Entity.Tag;
                    Entity.Position += MathConverter.Convert(Vector3.Transform(InitialEntityTranslation * scale, Entity.Orientation));
                }
                if(Entity is BEPUphysics.Entities.Prefabs.Box)
                    Entity.Position = Entity.Position - BEPUutilities.Vector3.UnitZ * (CorrectedDimensions.Z - (Entity as BEPUphysics.Entities.Prefabs.Box).Length) * 0.5f;
                Entity.Position += ContainingWorld.WorldPosition;
            }
            Entity.CollisionInformation.Tag = this;
            Entity.Tag = this;
        }

        // must be added to world and space
        public void FadeIn()
        {
            Transparent = true;
            if(fadeDirection == 0 && Entity.Space != null)
                Entity.Space.DuringForcesUpdateables.Starting += fade;
            fadeDirection = 1;
            minAlpha = 0;
        }

        // removes self at 0 transparency
        public void FadeOut(float minAlpha = 0)
        {
            Transparent = true;
            if(fadeDirection == 0 && Entity.Space != null)
                Entity.Space.DuringForcesUpdateables.Starting += fade;
            fadeDirection = -1;
            this.minAlpha = minAlpha;
        }

        private void fade()
        {
            try
            {
                Alpha += fadeDirection * deltaA;
                Alpha = MathHelper.Clamp(Alpha, minAlpha, 1);
                if(Alpha == minAlpha || Alpha == 1)
                {
                    Entity.Space.DuringForcesUpdateables.Starting -= fade;
                    fadeDirection = 0;
                    if(Alpha == 1)
                        Transparent = false;
                    if(Alpha == 0)
                        ContainingWorld.RemoveObject(this);
                }
            }
            catch { }
        }
    }
}
