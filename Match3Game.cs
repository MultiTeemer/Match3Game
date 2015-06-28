using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameForest_Test_Task
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Match3Game : Game
    {
        private enum CurrentScreenE
        {
            MainMenuScreen,
            GameScreen,
        };
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        CurrentScreenE curScreen;

        private Texture2D playBtn;
        private Texture2D playBtnHov;
        private Texture2D playBtnClicked;

        public Match3Game()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            curScreen = CurrentScreenE.MainMenuScreen;

            this.IsMouseVisible = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            playBtn = Content.Load<Texture2D>("graphics/playBtn");
            playBtnHov = Content.Load<Texture2D>("graphics/playBtnHov");
            playBtnClicked = Content.Load<Texture2D>("graphics/playBtnClicked");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
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
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            switch (curScreen)
            {
                case CurrentScreenE.MainMenuScreen:
                    int btnCenterX = (GraphicsDevice.Viewport.Width - playBtn.Width) / 2;
                    int btnCenterY = (GraphicsDevice.Viewport.Height - playBtn.Height) / 2;

                    Point mp = getMousePosition();
                    Rectangle btnRect = new Rectangle(btnCenterX, btnCenterY, playBtn.Width, playBtn.Height);

                    Texture2D btnToDraw = playBtn;

                    if (btnRect.Contains(mp))
                    {
                        btnToDraw = Mouse.GetState().LeftButton == ButtonState.Pressed ? playBtnClicked : playBtnHov;
                    }

                    spriteBatch.Draw(btnToDraw, new Vector2(btnCenterX, btnCenterY), Color.White);

                    break;
                case CurrentScreenE.GameScreen:

                    break;
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private Point getMousePosition()
        {
            return Mouse.GetState().Position;
        }
    }
}
