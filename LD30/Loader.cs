using Accelerated_Delivery_Win;
using BEPUphysics.CollisionShapes;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections;
using System.Collections.Generic;

namespace LD30
{
    public class Loader : IEnumerable<float>
    {
        private ContentManager content;
        private int loadedItems;
        private int totalItems;

        #region staples
        public Texture2D halfBlack;
        public Texture2D Instructions_Xbox;
        public Texture2D Instructions_PC;
        public Texture2D mainMenuLogo;
        public Texture2D mainMenuBackground;
        public Texture2D EmptyTex;
        #endregion

        #region font
        public SpriteFont SmallerFont;
        public SpriteFont Font;
        public SpriteFont BiggerFont;
        #endregion

        #region Buttons
        public Sprite resumeButton;
        public Sprite startButton;
        public Sprite quitButton;
        public Sprite mainMenuButton;
        public Sprite yesButton;
        public Sprite noButton;
        public Sprite pauseQuitButton;
        public Sprite instructionsButton;

        public Texture2D worldSelectButton;
        public Texture2D editorUI;
        #endregion

        #region models
        public Prop bridge;
        public Model player;
        #endregion

        #region props
        public PropCategory GeneralCategory = new PropCategory("General");
        public PropCategory InteriorCategory = new PropCategory("Interior");
        public PropCategory ExteriorCategory = new PropCategory("Exterior");
        public PropCategory DecorationCategory = new PropCategory("Decoration");
        public PropCategory StatueCategory = new PropCategory("Statues");
        #endregion

        public Loader(ContentManager content)
        {
            this.content = content;
        }

        public IEnumerator<float> GetEnumerator()
        {
            totalItems = 3 + 9 + 2 + 39;

            #region Font
            SmallerFont = content.Load<SpriteFont>("font/smallfont");
            yield return progress();
            Font = content.Load<SpriteFont>("font/font");
            yield return progress();
            BiggerFont = content.Load<SpriteFont>("font/bigfont");
            yield return progress();
            #endregion

            #region gui
            EmptyTex = new Texture2D(RenderingDevice.GraphicsDevice, 1, 1);
            EmptyTex.SetData(new[] { Color.White });
            yield return progress();
            halfBlack = new Texture2D(RenderingDevice.GraphicsDevice, 1, 1);
            halfBlack.SetData(new[] { new Color(0, 0, 0, 178) }); //set the color data on the texture
            yield return progress();
            RenderTarget2D target = new RenderTarget2D(RenderingDevice.GraphicsDevice, 450, 120);
            int border = 7;
            RenderingDevice.GraphicsDevice.SetRenderTarget(target);
            RenderingDevice.GraphicsDevice.Clear(Color.SlateGray);
            RenderingDevice.SpriteBatch.Begin();
            RenderingDevice.SpriteBatch.Draw(EmptyTex, new Rectangle(border, border, 450 - border * 2, 120 - border * 2), Color.LightGray);
            RenderingDevice.SpriteBatch.End();
            RenderingDevice.GraphicsDevice.SetRenderTarget(null);
            worldSelectButton = (Texture2D)target;
            yield return progress();

            Texture2D buttonsTex = content.Load<Texture2D>("gui/buttons");
            Rectangle buttonRect = new Rectangle(0, 0, 210, 51);
            resumeButton = new Sprite(delegate { return buttonsTex; }, new Vector2(RenderingDevice.Width * 0.23f, RenderingDevice.Height * 0.75f), buttonRect, Sprite.RenderPoint.UpLeft);
            mainMenuButton = new Sprite(delegate { return buttonsTex; }, new Vector2((RenderingDevice.Width * 0.415f), (RenderingDevice.Height * 0.75f)), new Rectangle(0, buttonRect.Height, buttonRect.Width, buttonRect.Height), Sprite.RenderPoint.UpLeft);
            pauseQuitButton = new Sprite(delegate { return buttonsTex; }, new Vector2((RenderingDevice.Width * 0.6f), (RenderingDevice.Height * 0.75f)), new Rectangle(0, buttonRect.Height * 3, buttonRect.Width, buttonRect.Height), Sprite.RenderPoint.UpLeft);
            instructionsButton = new Sprite(delegate { return buttonsTex; }, new Vector2(RenderingDevice.Width * 0.415f, RenderingDevice.Height * 0.75f), new Rectangle(buttonRect.Width, buttonRect.Height * 2, buttonRect.Width, buttonRect.Height), Sprite.RenderPoint.UpLeft);
            quitButton = new Sprite(delegate { return buttonsTex; }, new Vector2(RenderingDevice.Width * 0.6f, RenderingDevice.Height * 0.75f), new Rectangle(0, buttonRect.Height * 3, buttonRect.Width, buttonRect.Height), Sprite.RenderPoint.UpLeft);
            startButton = new Sprite(delegate { return buttonsTex; }, new Vector2(RenderingDevice.Width * 0.23f, RenderingDevice.Height * 0.75f), new Rectangle(0, buttonRect.Height * 2, buttonRect.Width, buttonRect.Height), Sprite.RenderPoint.UpLeft);
            yesButton = new Sprite(delegate { return buttonsTex; }, new Vector2(RenderingDevice.Width * 0.315f, RenderingDevice.Height * 0.65f), new Rectangle(buttonRect.Width, 0, buttonRect.Width, buttonRect.Height), Sprite.RenderPoint.UpLeft);
            noButton = new Sprite(delegate { return buttonsTex; }, new Vector2(RenderingDevice.Width * 0.515f, RenderingDevice.Height * 0.65f), new Rectangle(buttonRect.Width, buttonRect.Height, buttonRect.Width, buttonRect.Height), Sprite.RenderPoint.UpLeft);
            yield return progress();
            mainMenuBackground = content.Load<Texture2D>("gui/background");
            yield return progress();
            mainMenuLogo = content.Load<Texture2D>("gui/logo");
            yield return progress();

            Instructions_Xbox = content.Load<Texture2D>("gui/instructions_xbox");
            yield return progress();
            Instructions_PC = content.Load<Texture2D>("gui/instructions_pc");
            yield return progress();

            editorUI = content.Load<Texture2D>("gui/editor");
            yield return progress();
            #endregion

            #region props
            GeneralCategory.Add(new Prop(content.Load<Model>("models/general/cube"), new Vector3(1, 1, 1), true, true, true, true, "Cube", "The basic building block of all life.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f) * s, s.X, s.Y, s.Z)));
            yield return progress();
            GeneralCategory.Add(new Prop(content.Load<Model>("models/general/incline"), new Vector3(2, 2, 1), false, false, false, true, "Stairs", "NOW you're going places. Can't be scaled.",
                (p, s) => { BEPUutilities.Vector3[] v; int[] i; ModelDataExtractor.GetVerticesAndIndicesFromModel(content.Load<Model>("models/general/incline"), out v, out i); Entity e = new MobileMesh(v, i, BEPUutilities.AffineTransform.Identity, MobileMeshSolidity.Counterclockwise); e.Position = p + new Vector3(1, 1, 0.5f); return e; }));
            yield return progress();
            GeneralCategory.Add(new Prop(content.Load<Model>("models/general/inclinecorner"), new Vector3(2, 2, 1), false, false, false, true, "Corner Stairs", "For those hard-to-reach spots. Can't be scaled.",
                (p, s) => { BEPUutilities.Vector3[] v; int[] i; ModelDataExtractor.GetVerticesAndIndicesFromModel(content.Load<Model>("models/general/inclinecorner"), out v, out i); Entity e = new MobileMesh(v, i, BEPUutilities.AffineTransform.Identity, MobileMeshSolidity.Solid); e.Position = p + new Vector3(1, 1, 0.5f); return e; }));
            yield return progress();
            GeneralCategory.Add(new Prop(content.Load<Model>("models/general/littlebridge"), new Vector3(3, 1, 1), false, false, true, false, "Bridge", "Somewhat more stylish than cubes.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(1.5f, 0.5f, 0.5f) * s, s.X * 3, s.Y, s.Z)));
            yield return progress();
            GeneralCategory.Add(new Prop(content.Load<Model>("models/general/pyramid"), new Vector3(2, 2, 1), false, false, false, true, "Pyramid", "I can't fathom what this would be useful for.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Cone(p + new Vector3(1, 1, 0.5f) * s, s.Z, (s.X + s.Y) / 2 * 1.2f)));
            yield return progress();
            GeneralCategory.Add(new Prop(content.Load<Model>("models/general/sphere"), new Vector3(2, 2, 2), false, false, false, true, "Sphere", "The roundest of them all.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Sphere(p + new Vector3(1, 1, 1) * s, (s.X + s.Y + s.Z) / 3)));
            yield return progress();
            InteriorCategory.Add(new Prop(content.Load<Model>("models/interior/chair"), new Vector3(1, 1, 2), false, false, false, true, "Chair", "A small chair.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f, 0.5f, 1) * s, s.X * 0.9f, s.Y * 0.9f, s.Z * 1.5f)));
            yield return progress();
            InteriorCategory.Add(new Prop(content.Load<Model>("models/interior/crate"), new Vector3(1, 1, 1), false, false, false, true, "Crate", "What does it hold?",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f) * s, s.X * 0.8f, s.Y * 0.8f, s.Z * 0.8f)));
            yield return progress();
            InteriorCategory.Add(new Prop(content.Load<Model>("models/interior/table"), new Vector3(2, 2, 1), true, false, false, true, "Table", "Sturdy multipurpose table. You can put things on it.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(1, 1, 0.5f) * s, s.X * 2, s.Y * 2, s.Z)));
            yield return progress();
            InteriorCategory.Add(new Prop(content.Load<Model>("models/interior/teapot"), new Vector3(1, 1, 1), false, false, false, true, "Teapot", "Short and stout.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f) * s, s.X * 0.75f, s.Y * 0.4f, s.Z * 0.55f)));
            yield return progress();
            InteriorCategory.Add(new Prop(content.Load<Model>("models/interior/nightstand"), new Vector3(1, 1, 1), true, false, false, true, "Nightstand", "For keeping things in and on.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f, 0.5f, 1) * s, s.X * 0.9f, s.Y * 0.85f, s.Z)));
            yield return progress();
            InteriorCategory.Add(new Prop(content.Load<Model>("models/interior/fireplace"), new Vector3(4, 2, 3), false, false, false, true, "Fireplace", "Glorious and warm.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(2, 1, 1.5f) * s, s.X * 3.6f, s.Y * 1.7f, s.Z * 2.5f)));
            yield return progress();
            InteriorCategory.Add(new Prop(content.Load<Model>("models/interior/plate"), new Vector3(1, 1, 1), false, false, false, true, "Plate", "Have your potato and eat it too.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f) * s, s.X, s.Y, s.Z * 0.15f)));
            yield return progress();
            ExteriorCategory.Add(new Prop(content.Load<Model>("models/exterior/rock"), new Vector3(2, 2, 1), false, false, false, true, "Rock", "Just an ordinary boulder.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(1, 1, 0.5f) * s, s.X * 1.4f, s.Y * 1.3f, s.Z * 0.7f)));
            yield return progress();
            ExteriorCategory.Add(new Prop(content.Load<Model>("models/exterior/tree"), new Vector3(2, 2, 5), false, false, false, true, "Tree", "It's working on its impression of the Empire State Building.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(1, 1, 2.5f) * s, s.X * 2, s.Y * 2, s.Z * 5)));
            yield return progress();
            ExteriorCategory.Add(new Prop(content.Load<Model>("models/exterior/bush"), new Vector3(2, 2, 2), false, false, false, true, "Bush", "It is adamant that you refer to it as George.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(1, 1, 1) * s, s.X * 2, s.Y * 2, s.Z * 1.7f)));
            yield return progress();
            ExteriorCategory.Add(new Prop(content.Load<Model>("models/exterior/grass"), new Vector3(2, 2, 1), false, false, false, true, "Grass", "A small patch of grass.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(1, 1, 0.5f) * s, s.X * 2, s.Y * 2, s.Z * 0.3f)));
            yield return progress();
            ExteriorCategory.Add(new Prop(content.Load<Model>("models/exterior/plant"), new Vector3(1, 1, 1), false, false, false, true, "Plant", "A sprig of something.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f) * s, s.X * 0.6f, s.Y * 0.3f, s.Z * 1.1f)));
            yield return progress();
            ExteriorCategory.Add(new Prop(content.Load<Model>("models/exterior/barrel"), new Vector3(2, 2, 3), false, false, false, true, "Barrel", "A nice wooden barrel.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Cylinder(p + new Vector3(1, 1, 1.5f) * s, s.Z * 1.5f, (s.X + s.Y) / 2)));
            yield return progress();
            ExteriorCategory.Add(new Prop(content.Load<Model>("models/exterior/fence"), new Vector3(2, 1, 2), false, false, false, true, "Fence", "Keeps people out.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(1, 0.5f, 1) * s, s.X * 2, s.Y * 0.5f, s.Z * 2.4f)));
            yield return progress();
            ExteriorCategory.Add(new Prop(content.Load<Model>("models/exterior/ironfence"), new Vector3(2, 1, 2), false, false, false, true, "Iron Fence", "Keeps STRONG people out.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(1, 0.5f, 1) * s, s.X * 2, s.Y * 0.3f, s.Z * 2.3f)));
            yield return progress();
            ExteriorCategory.Add(new Prop(content.Load<Model>("models/exterior/mushroom"), new Vector3(2, 2, 3), false, false, false, true, "Mushroom", "It's huge!",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(1, 1, 1.5f) * s, s.X * 2, s.Y * 2, s.Z * 2.9f)));
            yield return progress();
            ExteriorCategory.Add(new Prop(content.Load<Model>("models/exterior/pump"), new Vector3(1, 2, 2), false, false, false, true, "Pump", "Rusted shut.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f, 1, 1) * s, s.X * 0.5f, s.Y * 1.4f, s.Z * 2.1f)));
            yield return progress();
            DecorationCategory.Add(new Prop(content.Load<Model>("models/decoration/banner"), new Vector3(1, 1, 2), false, false, true, false, "Banner", "The standard of great hall everywhere.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f, 0.5f, 1) * s, s.X * 1, s.Y * 0.3f, s.Z * 1.5f)));
            yield return progress();
            DecorationCategory.Add(new Prop(content.Load<Model>("models/decoration/checkerboard"), new Vector3(1, 1, 1), false, false, false, true, "Checkerboard", "You can also use it for chess.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f) * s, s.X, s.Y, s.Z * 0.1f)));
            yield return progress();
            DecorationCategory.Add(new Prop(content.Load<Model>("models/decoration/flamingo"), new Vector3(1, 3, 3), false, false, false, true, "Flamingo", "Not just in pink anymore!",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f, 1.5f, 1.5f) * s, s.X, s.Y * 2.5f, s.Z * 3)));
            yield return progress();
            DecorationCategory.Add(new Prop(content.Load<Model>("models/decoration/gnome"), new Vector3(1, 1, 2), false, false, false, true, "Gnome", "Worth millions.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f, 0.5f, 1) * s, s.X * 0.8f, s.Y * 0.3f, s.Z * 1.4f)));
            yield return progress();
            DecorationCategory.Add(new Prop(content.Load<Model>("models/decoration/potato"), new Vector3(1, 1, 1), false, false, false, true, "Potato", "Potatoes gonna potate. #LudumSpud #SpudumDare",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f) * s, s.X * 0.5f, s.Y * 0.25f, s.Z * 0.25f)));
            yield return progress();
            DecorationCategory.Add(new Prop(content.Load<Model>("models/decoration/sconce"), new Vector3(1, 1, 1), false, false, true, false, "Wall Sconce", "Torches not included.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f) * s, s.X * 0.5f, s.Y * 0.6f, s.Z * 0.9f)));
            yield return progress();
            DecorationCategory.Add(new Prop(content.Load<Model>("models/decoration/trafficcone"), new Vector3(1, 1, 2), false, false, false, true, "Traffic Cone", "You'll have no trouble with traffic now!",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f, 0.5f, 1) * s, s.X, s.Y, s.Z * 1.4f)));
            yield return progress();
            StatueCategory.Add(new Prop(content.Load<Model>("models/statues/abduction"), new Vector3(1, 1, 2), false, false, false, true, "UFO Statue", "Moo.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f, 0.5f, 1) * s, s.X * 0.9f, s.Y * 0.9f, s.Z * 1.45f)));
            yield return progress();
            StatueCategory.Add(new Prop(content.Load<Model>("models/statues/parabox"), new Vector3(1, 1, 2), false, false, false, true, "Rectangular Man", "He looks very at home.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f, 0.5f, 1) * s, s.X * 0.9f, s.Y * 0.5f, s.Z * 1.7f)));
            yield return progress();
            StatueCategory.Add(new Prop(content.Load<Model>("models/statues/travesty"), new Vector3(1, 1, 2), false, false, false, true, "Flat Man", "Give him a sword and he'll be set!",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f, 0.5f, 1) * s, s.X * 0.9f, s.Y * 0.9f, s.Z * 1.55f)));
            yield return progress();
            StatueCategory.Add(new Prop(content.Load<Model>("models/statues/wip"), new Vector3(1, 1, 1), false, false, false, true, "Anvil", "They say this particular anvil floats.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f) * s, s.X * 0.9f, s.Y * 0.9f, s.Z * 0.5f)));
            yield return progress();
            StatueCategory.Add(new Prop(content.Load<Model>("models/statues/ld30"), new Vector3(2, 2, 3), false, false, false, true, "Ludum Dare 30", "Commemorative statue. Get one before they're gone!",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(1, 1, 1.5f) * s, s.X * 2, s.Y * 2, s.Z * 2.9f)));
            yield return progress();
            StatueCategory.Add(new Prop(content.Load<Model>("models/statues/obelisk"), new Vector3(1, 1, 2), false, false, false, true, "Obelisk", "Mystical artifact. Possibly from another planet.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(0.5f, 0.5f, 1) * s, s.X * 0.7f, s.Y * 0.7f, s.Z * 1.95f)));
            yield return progress();
            StatueCategory.Add(new Prop(content.Load<Model>("models/statues/gargoyle"), new Vector3(3, 1, 2), false, false, true, false, "Gargoyle", "Stone cold protector.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(1.5f, 0.5f, 1) * s, s.X * 3f, s.Y, s.Z * 2)));
            yield return progress();
            StatueCategory.Add(new Prop(content.Load<Model>("models/statues/heatsink"), new Vector3(2, 2, 1), false, false, false, true, "Heatsink", "Incidentally, it looks nothing like a sink.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(1, 1, 0.5f) * s, s.X * 2, s.Y * 2, s.Z)));
            yield return progress();
            StatueCategory.Add(new Prop(content.Load<Model>("models/statues/kerath"), new Vector3(2, 2, 3), false, false, false, true, "Kerath's Arch", "Brought to you by the letter V.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(1, 1, 1.5f) * s, s.X * 1.8f, s.Y * 1.8f, s.Z * 2.4f)));
            yield return progress();
            StatueCategory.Add(new Prop(content.Load<Model>("models/statues/lion"), new Vector3(2, 2, 1), false, false, false, true, "Lion", "I'm not lion, it's no sphinx.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p + new Vector3(1, 1, 0.5f) * s, s.X * 1.6f, s.Y * 2, s.Z * 1)));
            yield return progress();
            #endregion

            #region general models
            bridge = new Prop(content.Load<Model>("models/bridge"), new Vector3(10, 2, 4), false, false, true, false, "Bridge", "You aren't supposed to see this.",
                (p, s) => { BEPUutilities.Vector3[] v; int[] i; ModelDataExtractor.GetVerticesAndIndicesFromModel(content.Load<Model>("models/bridge"), out v, out i); Entity e = new MobileMesh(v, i, BEPUutilities.AffineTransform.Identity, MobileMeshSolidity.Solid); e.Position = p; return e; });
            yield return progress();
            player = content.Load<Model>("models/player");
            foreach(ModelMesh mesh in player.Meshes)
                foreach(ModelMeshPart part in mesh.MeshParts)
                    part.Effect = RenderingDevice.Shader;

            foreach(PropCategory category in PropCategory.Categories)
                foreach(Prop prop in category.Props)
                    foreach(ModelMesh mesh in prop.Model.Meshes)
                        foreach(ModelMeshPart part in mesh.MeshParts)
                            part.Effect = RenderingDevice.Shader;
            yield return progress();
            #endregion
        }

        float progress()
        {
            ++loadedItems;
            return (float)loadedItems / totalItems;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
