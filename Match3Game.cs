using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace GameForest_Test_Task
{
    public class Match3Game : Game
    {
        private enum CurrentScreenE
        {
            MainMenuScreen,
            GameScreen,
        };

        private enum GameStatusE
        {
            GameRunning,
            GameStoped,
        };

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        CurrentScreenE curScreen;

        MouseState lastState;

        GameStatusE gameState;
        GameTime gameStartTime;
        GameField field;

        private delegate void updater(GameTime gameTime);
        private delegate void drawer(GameTime gameTime);

        private string[] requiredTextures;
        private Dictionary<string, Texture2D> textures;
        private Texture2D[] blocksTextures;

        private const int FIELD_SIZE = 8;

        private int FIELD_SHIFT_BY_X;
        private int FIELD_SHIFT_BY_Y;
        private int BLOCK_TEXTURE_SIZE;

        private int curSelectedItemIdx;

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
                "circle",
                "diamond",
                "hex",
                "square",
                "star",
                "triangle",
                "circleHl",
                "diamondHl",
                "hexHl",
                "squareHl",
                "starHl",
                "triangleHl",
            };

            textures = new Dictionary<string, Texture2D>();

            field = new GameField(FIELD_SIZE);

            blocksTextures = new Texture2D[(int)GameField.BlockTypeE.BlocksCount * 2];

            curSelectedItemIdx = -1;
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

            string[] blocksTexturesNames = new string[] {
                "circle",
                "diamond",
                "hex",
                "square",
                "star",
                "triangle",
            };

            int blocksCount = (int)GameField.BlockTypeE.BlocksCount;

            for (int i = 0; i < blocksCount; ++i)
            {
                blocksTextures[i] = textures[blocksTexturesNames[i]];
                blocksTextures[i + blocksCount] = textures[blocksTexturesNames[i] + "Hl"];
            }

            BLOCK_TEXTURE_SIZE = blocksTextures[0].Width;
            FIELD_SHIFT_BY_X = (GraphicsDevice.Viewport.Width - FIELD_SIZE * BLOCK_TEXTURE_SIZE) / 2;
            FIELD_SHIFT_BY_Y = (GraphicsDevice.Viewport.Height - FIELD_SIZE * BLOCK_TEXTURE_SIZE) / 2;
        }

        protected override void UnloadContent()
        {}


        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            updater[] updaters = { this.updateMainMenuScreen, this.updateGameScreen };

            updaters[(int)this.curScreen](gameTime);

            lastState = Mouse.GetState();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

            drawer[] drawers = { this.drawMainMenuScreen, this.drawGameScreen };

            drawers[(int)curScreen](gameTime);

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void updateMainMenuScreen(GameTime gameTime)
        {
            Texture2D playBtn = textures["playBtn"];

            int btnCenterX = (GraphicsDevice.Viewport.Width - playBtn.Width) / 2;
            int btnCenterY = (GraphicsDevice.Viewport.Height - playBtn.Height) / 2;

            Point mp = getMousePosition();
            Rectangle btnRect = new Rectangle(btnCenterX, btnCenterY, playBtn.Width, playBtn.Height);

            if (btnRect.Contains(mp) && leftKeyClick())
            {
                curScreen = CurrentScreenE.GameScreen;
                gameState = GameStatusE.GameRunning;
                gameStartTime = gameTime;

                field.Init();
            }
        }

        private void drawMainMenuScreen(GameTime gameTime)
        {
            Texture2D playBtn = textures["playBtn"];

            int btnCenterX = (GraphicsDevice.Viewport.Width - playBtn.Width) / 2;
            int btnCenterY = (GraphicsDevice.Viewport.Height - playBtn.Height) / 2;

            Point mp = getMousePosition();
            Rectangle btnRect = new Rectangle(btnCenterX, btnCenterY, playBtn.Width, playBtn.Height);

            Texture2D btnToDraw = playBtn;

            if (btnRect.Contains(mp))
            {
                btnToDraw = Mouse.GetState().LeftButton == ButtonState.Pressed ? textures["playBtnClicked"] : textures["playBtnHov"];
            }

            spriteBatch.Draw(btnToDraw, new Vector2(btnCenterX, btnCenterY), Color.White);
        }

        private void updateGameScreen(GameTime gameTime)
        {
            int gameFieldSideLength = FIELD_SIZE * BLOCK_TEXTURE_SIZE;
            Rectangle gameFieldRect = new Rectangle(FIELD_SHIFT_BY_X, FIELD_SHIFT_BY_Y, gameFieldSideLength, gameFieldSideLength);
            Point mp = getMousePosition();

            if (gameFieldRect.Contains(mp) && leftKeyClick())
            {
                curSelectedItemIdx = (mp.Y - FIELD_SHIFT_BY_Y) / BLOCK_TEXTURE_SIZE * FIELD_SIZE + (mp.X - FIELD_SHIFT_BY_X) / BLOCK_TEXTURE_SIZE;
            }
        }

        private void drawGameScreen(GameTime gameTime)
        {
            int blocksCount = (int)GameField.BlockTypeE.BlocksCount;

            for (int i = 0; i < FIELD_SIZE; ++i)
            {
                for (int j = 0; j < FIELD_SIZE; ++j)
                {
                    int idx1d = i * FIELD_SIZE + j;

                    Texture2D blockTexture = blocksTextures[(int)field.field[idx1d] + (idx1d == curSelectedItemIdx ? blocksCount : 0)];

                    int x = FIELD_SHIFT_BY_X + j * BLOCK_TEXTURE_SIZE;
                    int y = FIELD_SHIFT_BY_Y + i * BLOCK_TEXTURE_SIZE;

                    spriteBatch.Draw(blockTexture, new Vector2(x, y), Color.White);
                }
            }
        }

        private Point getMousePosition()
        {
            return Mouse.GetState().Position;
        }

        private bool leftKeyClick()
        {
            return Mouse.GetState().LeftButton == ButtonState.Released && lastState.LeftButton == ButtonState.Pressed;
        }
    }
}
