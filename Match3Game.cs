using System;
using System.Collections.Generic;

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

        private bool mouseBlocked;

        private SwapAnimation? curSwapAnimation;

        public Match3Game()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            curScreen = CurrentScreenE.MainMenuScreen;

            IsMouseVisible = true;

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

            mouseBlocked = false;

            curSwapAnimation = null;
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
            if (mouseBlocked) return;

            int gameFieldSideLength = FIELD_SIZE * BLOCK_TEXTURE_SIZE;
            Rectangle gameFieldRect = new Rectangle(FIELD_SHIFT_BY_X, FIELD_SHIFT_BY_Y, gameFieldSideLength, gameFieldSideLength);
            Point mp = getMousePosition();

            if (leftKeyClick())
            {
                if (gameFieldRect.Contains(mp))
                {
                    int selectedItemIdx = (mp.Y - FIELD_SHIFT_BY_Y) / BLOCK_TEXTURE_SIZE * FIELD_SIZE + (mp.X - FIELD_SHIFT_BY_X) / BLOCK_TEXTURE_SIZE;

                    if (curSelectedItemIdx == -1)
                    {
                        curSelectedItemIdx = selectedItemIdx;
                    }
                    else
                    {
                        Point p1 = blockIdToCoords(curSelectedItemIdx);
                        Point p2 = blockIdToCoords(selectedItemIdx);

                        int dx = p2.X - p1.X;
                        int dy = p2.Y - p1.Y;

                        if (Math.Abs(dx) == 1 && dy == 0 || dx == 0 && Math.Abs(dy) == 1)
                        {
                            mouseBlocked = true;

                            float d = SwapAnimation.DURATION;
                            Vector2 shift = new Vector2(dx * BLOCK_TEXTURE_SIZE / d, dy * BLOCK_TEXTURE_SIZE / d);

                            curSwapAnimation = new SwapAnimation(curSelectedItemIdx, selectedItemIdx, shift);
                        }

                        curSelectedItemIdx = -1;
                    }
                }
                else
                {
                    curSelectedItemIdx = -1;
                }
            }
        }

        private void drawGameScreen(GameTime gameTime)
        {
            if (curSwapAnimation != null)
                drawGameScreenAnimation(gameTime);

            for (int i = 0; i < FIELD_SIZE; ++i)
            {
                for (int j = 0; j < FIELD_SIZE; ++j)
                {
                    int idx1d = i * FIELD_SIZE + j;

                    if (curSwapAnimation != null)
                    {
                        if (idx1d == curSwapAnimation.Value.block1Idx || idx1d == curSwapAnimation.Value.block2Idx)
                            continue;
                    }

                    Texture2D blockTexture = getBlockTextureById(idx1d);

                    int x = FIELD_SHIFT_BY_X + j * BLOCK_TEXTURE_SIZE;
                    int y = FIELD_SHIFT_BY_Y + i * BLOCK_TEXTURE_SIZE;

                    spriteBatch.Draw(blockTexture, new Vector2(x, y), Color.White);
                }
            }
        }

        private void drawGameScreenAnimation(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            SwapAnimation copy = curSwapAnimation.Value;

            copy.elapsed += dt;

            curSwapAnimation = copy;

            if (curSwapAnimation.Value.elapsed >= SwapAnimation.DURATION)
            {
                field.Swap(curSwapAnimation.Value.block1Idx, curSwapAnimation.Value.block2Idx);

                curSwapAnimation = null;
                mouseBlocked = false;

                return;
            }

            int id1 = curSwapAnimation.Value.block1Idx;
            int id2 = curSwapAnimation.Value.block2Idx;

            Point p1 = blockIdToCoords(id1);
            Point p2 = blockIdToCoords(id2);

            float elapsed = (float)curSwapAnimation.Value.elapsed;

            float x1 = FIELD_SHIFT_BY_X + p1.X * BLOCK_TEXTURE_SIZE + elapsed * curSwapAnimation.Value.shift.X;
            float y1 = FIELD_SHIFT_BY_Y + p1.Y * BLOCK_TEXTURE_SIZE + elapsed * curSwapAnimation.Value.shift.Y;

            float x2 = FIELD_SHIFT_BY_X + p2.X * BLOCK_TEXTURE_SIZE - elapsed * curSwapAnimation.Value.shift.X;
            float y2 = FIELD_SHIFT_BY_Y + p2.Y * BLOCK_TEXTURE_SIZE - elapsed * curSwapAnimation.Value.shift.Y;

            spriteBatch.Draw(getBlockTextureById(id1), new Vector2(x1, y1), Color.White);
            spriteBatch.Draw(getBlockTextureById(id2), new Vector2(x2, y2), Color.White);
        }

        private Point getMousePosition()
        {
            return Mouse.GetState().Position;
        }

        private Point blockIdToCoords(int id)
        {
            return new Point(id % FIELD_SIZE, id / FIELD_SIZE);
        }

        private Texture2D getBlockTextureById(int id)
        {
            return blocksTextures[(int)field.field[id] + (id == curSelectedItemIdx ? (int)GameField.BlockTypeE.BlocksCount : 0)];
        }

        private bool leftKeyClick()
        {
            return Mouse.GetState().LeftButton == ButtonState.Released && lastState.LeftButton == ButtonState.Pressed;
        }
    }
}
