using Accelerated_Delivery_Win;
using BEPUphysics.Entities;
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
        public Texture2D pressStart;
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
        #endregion

        #region models
        public Model bridge;
        #endregion

        #region props
        public PropCategory GeneralCatergory = new PropCategory("General");
        #endregion

        public Loader(ContentManager content)
        {
            this.content = content;
        }

        public IEnumerator<float> GetEnumerator()
        {
            totalItems = 3 + 9 + 1 + 1;

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

            pressStart = content.Load<Texture2D>("gui/start");
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
            #endregion

            #region general models
            bridge = content.Load<Model>("models/bridge");
            yield return progress();
            #endregion

            #region props
            GeneralCatergory.Add(new Prop(content.Load<Model>("models/general/cube"), "Cube", "The basic building block of all life.",
                (p, s) => new BEPUphysics.Entities.Prefabs.Box(p, s.X, s.Y, s.Z)));
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
