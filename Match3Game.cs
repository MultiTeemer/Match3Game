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


        private string[] requiredTextures;
        private Dictionary<string, Texture2D> textures;

        public Match3Game()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            curScreen = CurrentScreenE.MainMenuScreen;

            this.IsMouseVisible = true;

            requiredTextures = new string[] {
                "playBtn",
                "playBtnHov",
                "playBtnClicked",
            };

            textures = new Dictionary<string, Texture2D>();
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            for (int i = 0; i < requiredTextures.Length; ++i)
            {
                textures.Add(requiredTextures[i], Content.Load<Texture2D>("graphics/" + requiredTextures[i]));
            }
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
            Texture2D playBtn = textures["playBtn"];

                    spriteBatch.Draw(btnToDraw, new Vector2(btnCenterX, btnCenterY), Color.White);

                    break;
                case CurrentScreenE.GameScreen:

                    break;
            }

            spriteBatch.End();
            Texture2D playBtn = textures["playBtn"];
                btnToDraw = Mouse.GetState().LeftButton == ButtonState.Pressed ? textures["playBtnClicked"] : textures["playBtnHov"];

            base.Draw(gameTime);
        }

        private Point getMousePosition()
        {
            return Mouse.GetState().Position;
        }
    }
}
