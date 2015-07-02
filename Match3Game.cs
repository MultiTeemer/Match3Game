using System;
using System.Linq;
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

        GameField field;

        private delegate void updater(GameTime gameTime);
        private delegate void drawer(GameTime gameTime);

        private string[] requiredTextures;
        private Dictionary<string, Texture2D> textures;
        private Texture2D[] blocksTextures;
        private SpriteFont common;

        private const int FIELD_SIZE = 8;
        private const int GAME_DURATION = 60;
        private const float SWAP_ANIM_DURATION = 200;
        private const float BLOCK_DROP_DOWN_VELOCITY = 5e-3f;

        private int FIELD_SHIFT_BY_X;
        private int FIELD_SHIFT_BY_Y;
        private int BLOCK_TEXTURE_SIZE;

        private int curSelectedItemIdx;

        private bool animationRunning;

        private List<MoveAnimation> runningAnimations;

        private struct Turn
        {
            public int blockIdx1;
            public int blockIdx2;

            public Turn(int id1, int id2)
            {
                blockIdx1 = id1;
                blockIdx2 = id2;
            }
        };

        private Turn? previousTurn;

        private int score;
        private int gameTimeMillisecondsElapsed;

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

            runningAnimations = new List<MoveAnimation>();
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

            common = Content.Load<SpriteFont>("fonts/CommonFont");
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
                gameTimeMillisecondsElapsed = 0;

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
            animationRunning = runningAnimations.Count > 0;

            if (animationRunning || gameTimeMillisecondsElapsed / 1000 >= GAME_DURATION)
                return;

            updateField();

            if (previousTurn != null)
            {
                addSwapAnimation(previousTurn.Value.blockIdx1, previousTurn.Value.blockIdx2);

                previousTurn = null;
            }

            int gameFieldSideLength = FIELD_SIZE * BLOCK_TEXTURE_SIZE;
            Rectangle gameFieldRect = new Rectangle(FIELD_SHIFT_BY_X, FIELD_SHIFT_BY_Y, gameFieldSideLength, gameFieldSideLength);
            Point mp = getMousePosition();

            if (leftKeyClick())
            {
                if (gameFieldRect.Contains(mp))
                {
                    int selectedItemIdx = (mp.Y - FIELD_SHIFT_BY_Y) / BLOCK_TEXTURE_SIZE * FIELD_SIZE + (mp.X - FIELD_SHIFT_BY_X) / BLOCK_TEXTURE_SIZE;

                    if (curSelectedItemIdx != -1 && canSwap(curSelectedItemIdx, selectedItemIdx))
                    {
                        previousTurn = new Turn(curSelectedItemIdx, selectedItemIdx);

                        addSwapAnimation(curSelectedItemIdx, selectedItemIdx);
                    }

                    curSelectedItemIdx = curSelectedItemIdx == -1 ? selectedItemIdx : -1;
                }
                else
                {
                    curSelectedItemIdx = -1;
                }
            }
        }

        private void addSwapAnimation(int id1, int id2)
        {
            Vector2 p1 = blockIdToTableCoords(id1);
            Vector2 p2 = blockIdToTableCoords(id2);

            int dx = (int)(p2.X - p1.X);
            int dy = (int)(p2.Y - p1.Y);

            Vector2 shift = new Vector2(dx * BLOCK_TEXTURE_SIZE / SWAP_ANIM_DURATION, dy * BLOCK_TEXTURE_SIZE / SWAP_ANIM_DURATION);

            MoveAnimation block1Movement = new MoveAnimation(
                getBlockCoords(id1),
                shift,
                SWAP_ANIM_DURATION,
                field.Get(id1),
                blockIdToTableCoords(id2)
            );

            MoveAnimation block2Movement = new MoveAnimation(
                getBlockCoords(id2),
                -shift,
                SWAP_ANIM_DURATION,
                field.Get(id2),
                blockIdToTableCoords(id1)
            );

            runningAnimations.Add(block1Movement);
            runningAnimations.Add(block2Movement);

            field.SetEmpty(id1);
            field.SetEmpty(id2);
        }

        private bool canSwap(int id1, int id2)
        {
            Vector2 p1 = blockIdToTableCoords(id1);
            Vector2 p2 = blockIdToTableCoords(id2);

            int dx = (int)Math.Abs(p2.X - p1.X);
            int dy = (int)Math.Abs(p2.Y - p1.Y);

            return dx * dy == 0 && (dx != 0 || dy != 0);
        }

        private bool gameEnded()
        {
            return gameTimeMillisecondsElapsed / 1000 >= GAME_DURATION;
        }

        private int countBlocks(GameField.BlockTypeE type, Vector2 start, Vector2 shift)
        {
            int counter = 0;
            Vector2 pos = start + shift;
            bool sameBlock = type == field.Get(tableCoordsToBlockId(pos));

            while (sameBlock)
            {
                ++counter;

                pos += shift;

                sameBlock = type == field.Get(tableCoordsToBlockId(pos));
            }

            return counter;
        }

        private void destroyOneBlock(int idx)
        {
            field.SetEmpty(idx);

            score += 25;
        }

        private void destroyBlocks(GameField.BlockTypeE type, Vector2 start, Vector2 shift)
        {
            Vector2 pos = start + shift;
            bool sameBlock = type == field.Get(tableCoordsToBlockId(pos));

            while (sameBlock)
            {
                int idx = tableCoordsToBlockId(pos);

                destroyOneBlock(idx);

                pos += shift;

                sameBlock = type == field.Get(tableCoordsToBlockId(pos));
            }
        }

        private void destroyChains()
        {
            for (int i = 0; i < FIELD_SIZE; ++i)
            {
                for (int j = 0; j < FIELD_SIZE; ++j)
                {
                    int idx = tableCoordsToBlockId(j, i);
                    GameField.BlockTypeE type = field.Get(idx);
                    Vector2 start = new Vector2(j, i);
                    bool blocksDestroyed = false;

                    if (type == GameField.BlockTypeE.Empty) continue;

                    int horCount = countBlocks(type, start, new Vector2(-1, 0)) + 1 + countBlocks(type, start, new Vector2(1, 0));
                    int verCount = countBlocks(type, start, new Vector2(0, -1)) + 1 + countBlocks(type, start, new Vector2(0, 1));

                    if (horCount >= 3)
                    {
                        blocksDestroyed = true;

                        destroyBlocks(type, start, new Vector2(-1, 0));
                        destroyBlocks(type, start, new Vector2(1, 0));
                    }

                    if (verCount >= 3)
                    {
                        blocksDestroyed = true;

                        destroyBlocks(type, start, new Vector2(0, -1));
                        destroyBlocks(type, start, new Vector2(0, 1));
                    }

                    if (blocksDestroyed)
                    {
                        destroyOneBlock(idx);
                    }
                }
            }

            if (previousTurn != null)
            {
                int id1 = previousTurn.Value.blockIdx1;
                int id2 = previousTurn.Value.blockIdx2;

                if (field.IsEmpty(id1) || field.IsEmpty(id2))
                {
                    previousTurn = null;
                }
            }
        }

        private void createDropDownAnimations()
        {
            for (int i = 0; i < FIELD_SIZE; ++i)
            {
                int firstEmptyRow = -1;

                for (int j = FIELD_SIZE - 1; j >= 0 && firstEmptyRow == -1; --j)
                {
                    if (field.IsEmpty(tableCoordsToBlockId(i, j)))
                    {
                        firstEmptyRow = j;
                    }
                }

                if (firstEmptyRow != -1)
                {
                    int[] fallHeight = new int[FIELD_SIZE];
                    Vector2 shift = new Vector2(0, BLOCK_TEXTURE_SIZE * BLOCK_DROP_DOWN_VELOCITY);

                    fallHeight[firstEmptyRow] = 1;

                    for (int j = firstEmptyRow - 1; j >= 0; --j)
                    {
                        int idx = tableCoordsToBlockId(i, j);
                        bool isEmpty = field.IsEmpty(idx);

                        fallHeight[j] = fallHeight[j + 1] + Convert.ToInt32(field.IsEmpty(idx));

                        if (!isEmpty)
                        {
                            Vector2 destination = new Vector2(i, j + fallHeight[j]);

                            runningAnimations.Add(createOneDropDownAnimation(getBlockCoords(idx), destination, field.Get(idx)));

                            field.SetEmpty(idx);
                        }
                    }

                    int newBlocksCount = fallHeight[0];
                    float startY = FIELD_SHIFT_BY_Y - BLOCK_TEXTURE_SIZE;
                    float startX = getBlockCoords(tableCoordsToBlockId(i, 0)).X;

                    for (int j = 0; j < newBlocksCount; ++j)
                    {
                        Vector2 start = new Vector2(startX, startY);
                        Vector2 destination = new Vector2(i, newBlocksCount - j - 1);

                        runningAnimations.Add(createOneDropDownAnimation(start, destination, GameField.RandomType()));

                        startY -= BLOCK_TEXTURE_SIZE;
                    }
                }
            }
        }

        private MoveAnimation createOneDropDownAnimation(Vector2 start, Vector2 destination, GameField.BlockTypeE type)
        {
            int idx = tableCoordsToBlockId(destination);
            float duration = Math.Abs(getBlockCoords(idx).Y - start.Y) / BLOCK_TEXTURE_SIZE / BLOCK_DROP_DOWN_VELOCITY;
            Vector2 shift = new Vector2(0, BLOCK_TEXTURE_SIZE * BLOCK_DROP_DOWN_VELOCITY);

            return new MoveAnimation(start, shift, duration, type, destination);
        }

        private void updateField()
        {
            destroyChains();
            createDropDownAnimations();
        }

        private void drawGameScreen(GameTime gameTime)
        {
            drawGameScreenGameRunning(gameTime);

            if (runningAnimations.Count > 0)
                drawGameScreenAnimation(gameTime);

            if (gameEnded())
                drawGameScreenGameOver(gameTime);
        }

        private void drawGameScreenGameRunning(GameTime gameTime)
        {
            for (int i = 0; i < FIELD_SIZE; ++i)
            {
                for (int j = 0; j < FIELD_SIZE; ++j)
                {
                    int idx = tableCoordsToBlockId(j, i);

                    if (field.IsEmpty(idx)) continue;

                    Texture2D blockTexture = getBlockTextureById(idx);

                    int x = FIELD_SHIFT_BY_X + j * BLOCK_TEXTURE_SIZE;
                    int y = FIELD_SHIFT_BY_Y + i * BLOCK_TEXTURE_SIZE;

                    spriteBatch.Draw(blockTexture, new Vector2(x, y), Color.White);
                }
            }

            if (!gameEnded())
            {
                gameTimeMillisecondsElapsed += (int)gameTime.ElapsedGameTime.TotalMilliseconds;
            }

            spriteBatch.DrawString(common, "Timer: 0:" + Convert.ToString(60 - gameTimeMillisecondsElapsed / 1000), new Vector2(0, 0), Color.White);
            spriteBatch.DrawString(common, "Score: " + Convert.ToString(score), new Vector2(0, 30), Color.White);
        }

        private void drawGameScreenAnimation(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            List<MoveAnimation> animations = new List<MoveAnimation>();
            List<MoveAnimation>.Enumerator i = runningAnimations.GetEnumerator();

            while (i.MoveNext())
            {
                MoveAnimation curr = i.Current;

                curr.timeElapsed += dt;

                if (curr.timeElapsed < curr.duration)
                {
                    animations.Add(curr);
                }
                else
                {
                    field.Set(tableCoordsToBlockId(curr.destination), curr.type);
                }

                float x = curr.start.X + curr.timeElapsed * curr.shift.X;
                float y = curr.start.Y + curr.timeElapsed * curr.shift.Y;

                spriteBatch.Draw(getBlockTextureByType(curr.type), new Vector2(x, y), Color.White);
            }

            runningAnimations = animations;
        }

        private void drawGameScreenGameOver(GameTime gameTime)
        {
            int w = GraphicsDevice.Viewport.Width;
            int h = GraphicsDevice.Viewport.Height;

            Texture2D dialog = new Texture2D(GraphicsDevice, 400, 300);

            Color[] dialogColorMap = Enumerable.Repeat(Color.BlueViolet, dialog.Height * dialog.Width).ToArray(); ;

            dialog.SetData(dialogColorMap);

            string msg1 = "Game Over!";
            string msg2 = "Your score: " + Convert.ToString(score) + " pts";

            Vector2 fontMeasure1 = common.MeasureString(msg1);
            Vector2 fontMeasure2 = common.MeasureString(msg2);

            spriteBatch.Draw(dialog, new Vector2((w - dialog.Width) / 2, (h - dialog.Height) / 2), Color.White);
            spriteBatch.DrawString(common, msg1, new Vector2((w - fontMeasure1.X) / 2, h / 3 - fontMeasure1.Y), Color.White);
            spriteBatch.DrawString(common, msg2, new Vector2((w - fontMeasure2.X) / 2, h / 2 - fontMeasure2.Y / 2), Color.White);
        }

        private Point getMousePosition()
        {
            return Mouse.GetState().Position;
        }

        private Vector2 getBlockCoords(int id)
        {
            Vector2 tableCoords = blockIdToTableCoords(id);

            float x = FIELD_SHIFT_BY_X + tableCoords.X * BLOCK_TEXTURE_SIZE;
            float y = FIELD_SHIFT_BY_Y + tableCoords.Y * BLOCK_TEXTURE_SIZE;

            return new Vector2(x, y);
        }

        private Vector2 blockIdToTableCoords(int id)
        {
            return new Vector2(id % FIELD_SIZE, id / FIELD_SIZE);
        }

        private int tableCoordsToBlockId(int col, int row)
        {
            return FIELD_SIZE * row + col;
        }

        private int tableCoordsToBlockId(Vector2 pos)
        {
            return FIELD_SIZE * (int)pos.Y + (int)pos.X;
        }

        private Texture2D getBlockTextureById(int id)
        {
            return blocksTextures[(int)field.field[id] + (id == curSelectedItemIdx ? (int)GameField.BlockTypeE.BlocksCount : 0)];
        }

        private Texture2D getBlockTextureByType(GameField.BlockTypeE type)
        {
            return blocksTextures[(int)type];
        }

        private bool leftKeyClick()
        {
            return Mouse.GetState().LeftButton == ButtonState.Released && lastState.LeftButton == ButtonState.Pressed;
        }
    }
}
