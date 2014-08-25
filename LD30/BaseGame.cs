using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Accelerated_Delivery_Win;
using Microsoft.Win32;
using System.IO;
using BEPUphysicsDemos.AlternateMovement.Character;

namespace LD30
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class BaseGame : Game
    {
        public GraphicsDeviceManager Graphics;
        private LoadingScreen loadingScreen;

        public bool Loading { get; private set; }
        private bool locked = false;
        private bool beenDrawn = false;
        private Texture2D loadingSplash;
        public Loader Loader { get; private set; }

        public SoundEffectInstance BGM;

        public WorldGrid WorldGrid { get; private set; }
        private Editor editor;

        private Character character;
        private Camera camera;

        public BaseGame()
        {
            Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            GameManager.FirstStageInitialization(this, Program.Cutter);
            Loading = true;
            Microsoft.Win32.SystemEvents.SessionSwitch += new Microsoft.Win32.SessionSwitchEventHandler(SystemEvents_SessionSwitch);

            Graphics.PreferredBackBufferHeight = 720;
            Graphics.PreferredBackBufferWidth = 1280;
            if(Graphics.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
                Graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Graphics.ApplyChanges();

            Input.SetOptions(new WindowsOptions(), new XboxOptions());

            base.Initialize();

            Resources.Initialize(Content);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            RenderingDevice.Initialize(Graphics, Program.Cutter, GameManager.Space, Content.Load<Effect>("shaders/shadowmap"));
            Renderer.Initialize(Graphics, this, GameManager.Space, Content.Load<Effect>("shaders/shadowmap"));
            //Program.Initialize(GraphicsDevice);
            //MyExtensions.Initialize(GraphicsDevice);
            loadingScreen = new LoadingScreen(Content, GraphicsDevice);
            loadingSplash = Content.Load<Texture2D>("gui/loading");

            SoundEffect e = Content.Load<SoundEffect>("music/main");
            BGM = e.CreateInstance();
            BGM.IsLooped = true;
            BGM.Play();

            GameManager.Initialize(null, Content.Load<SpriteFont>("font/font"), null);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if((!IsActive && Loader != null) || locked)
            {
                base.Update(gameTime);
                return;
            }
            
            Input.Update(gameTime, false);
            MediaSystem.Update(gameTime, Program.Game.IsActive);
            
            if(Loading)
            {
                IsFixedTimeStep = true;
                Loader l = loadingScreen.Update(gameTime);
                if(l != null)
                {
                    IsFixedTimeStep = false;
                    Loader = l;
                    loadingScreen = null;
                    Loading = false;
                    MenuHandler.Create(Loader);
                    IsMouseVisible = true;
                }
            }
            else
            {
                GameState statePrior = GameManager.State;
                MenuHandler.Update(gameTime);
                bool stateChanged = GameManager.State != statePrior;

                if(GameManager.State == GameState.Running)
                {
                    //IsMouseVisible = false;
                    if((Input.CheckKeyboardJustPressed(Keys.Escape)) && !stateChanged)
                    {
                        //MediaSystem.PlaySoundEffect(SFXOptions.Pause);
                        GameManager.State = GameState.Paused;
                        IsMouseVisible = true;
                    }
                    else
                    {
                        if(editor.IsOpen)
                            editor.Update(gameTime);
                        else if(Input.CheckMouseJustClicked(2))
                        {
                            editor.Open();
                            // force character to stop moving
                            character.Entity.CharacterController.HorizontalMotionConstraint.MovementDirection = Vector2.Zero;
                        }
                        character.Update(gameTime, editor.IsOpen);
                        if(!editor.IsOpen)
                            camera.Update(gameTime);
                        GameManager.Space.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
                    }
                }
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            if(!beenDrawn)
            {
                MediaSystem.LoadSoundEffects(Content);
                beenDrawn = true;
            }

            GraphicsDevice.Clear(Color.LightGray);

            if(Loading)
            {
                RenderingDevice.SpriteBatch.Begin();
                RenderingDevice.SpriteBatch.Draw(loadingSplash, new Vector2(RenderingDevice.Width * 0.5f, RenderingDevice.Height * 0.5f), null, Color.White, 0, new Vector2(loadingSplash.Width, loadingSplash.Height) * 0.5f, 1, SpriteEffects.None, 0);
                RenderingDevice.SpriteBatch.End();
                loadingScreen.Draw();
            }
            else if(GameManager.State != GameState.Ending)
                MenuHandler.Draw(gameTime);

            if(GameManager.State == GameState.Running)
                DrawScene(gameTime);

            base.Draw(gameTime);
        }

        public void DrawScene(GameTime gameTime)
        {
            Renderer.Draw();
            character.Draw(gameTime);
            editor.Draw(gameTime);
            WorldGrid.Draw(camera);
        }

        public void Start(string path, bool created)
        {
            camera = new Camera(this, character);
            character = new Character(Loader.player, new CharacterControllerInput(GameManager.Space, camera, this));
            camera.Character = character;
            Renderer.Camera = camera;

            World w;
            if(!created)
                w = World.FromFile(path);
            else
                w = new World(Path.GetFileNameWithoutExtension(path));

            WorldGrid = new WorldGrid(w, character);
            editor = new Editor(w, character);
            GameManager.State = GameState.Running;

            GameManager.Space.Add(WorldGrid);
            character.Entity.Activate();
        }

        public void End()
        {
            if(WorldGrid != null)
            {
                GameManager.Space.Remove(WorldGrid);
                character.Entity.Deactivate();
                WorldGrid.Host.SaveToFile();
            }
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            if(WorldGrid != null)
            {
                WorldGrid.Host.SaveToFile();
                //OnlineHandler.UploadWorld(WorldGrid.Host);
            }
            base.OnExiting(sender, args);
        }
        
#if WINDOWS
        protected override void OnActivated(object sender, EventArgs args)
        {
            if(GameManager.PreviousState == GameState.Running && GameManager.State != GameState.Ending)
                GameManager.State = GameState.Running;
            BGM.Resume();
            BGM.Volume = 1;

            base.OnActivated(sender, args);
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            if(GameManager.State == GameState.Running && GameManager.State != GameState.Ending)
                GameManager.State = GameState.Paused;
            BGM.Pause();

            base.OnDeactivated(sender, args);
        }
        protected void SystemEvents_SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
        {
            if(e.Reason == SessionSwitchReason.SessionLock)
            {
                OnDeactivated(sender, e);
                locked = true;
            }
            else if(e.Reason == SessionSwitchReason.SessionUnlock)
            {
                OnActivated(sender, e);
                locked = false;
            }
        }
#endif
    }
}
