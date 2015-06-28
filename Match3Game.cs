using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameForest_Test_Task
{
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

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            playBtn = Content.Load<Texture2D>("graphics/playBtn");
            playBtnHov = Content.Load<Texture2D>("graphics/playBtnHov");
            playBtnClicked = Content.Load<Texture2D>("graphics/playBtnClicked");
        }

        protected override void UnloadContent()
        {}


        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();


            base.Update(gameTime);
        }

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
