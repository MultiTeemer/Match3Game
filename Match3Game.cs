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
        private enum CurrentScreen
        {
            MainMenuScreen,
            GameScreen,
        };

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        CurrentScreen curScreen;

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
        private const float DESTROY_ANIMATION_DURATION = 500;
        private const float BLOCK_DROP_DOWN_VELOCITY = 5e-3f;

        private int FIELD_SHIFT_BY_X;
        private int FIELD_SHIFT_BY_Y;
        private int BLOCK_TEXTURE_SIZE;

        private bool animationRunning;

        private List<Animation> runningAnimations;

        private GameInfo info;

        public Match3Game()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            curScreen = CurrentScreen.MainMenuScreen;

            IsMouseVisible = true;

            requiredTextures = new string[] {
                "playBtn",
                "playBtnHov",
                "playBtnClicked",
                "okBtn",
                "okBtnHov",
                "okBtnClicked",
                "gameOverDlg",
            };

            textures = new Dictionary<string, Texture2D>();

            field = new GameField(FIELD_SIZE);

            blocksTextures = new Texture2D[(int)GameField.BlockType.BlocksCount * 3];

            runningAnimations = new List<Animation>();
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
                textures.Add(requiredTextures[i], loadTexture(requiredTextures[i]));
            }

            string[] blocksTexturesNames = new string[] {
                "circle",
                "diamond",
                "hex",
                "square",
                "star",
                "triangle",
            };

            int blocksCount = (int)GameField.BlockType.BlocksCount;

            for (int i = 0; i < blocksCount; ++i)
            {
                textures.Add(blocksTexturesNames[i], loadTexture(blocksTexturesNames[i]));
                textures.Add(blocksTexturesNames[i] + "Hl", loadTexture(blocksTexturesNames[i] + "Hl"));
                textures.Add(blocksTexturesNames[i] + "Dstrd", loadTexture(blocksTexturesNames[i] + "Dstrd"));

                blocksTextures[i] = textures[blocksTexturesNames[i]];
                blocksTextures[i + blocksCount] = textures[blocksTexturesNames[i] + "Hl"];
                blocksTextures[i + blocksCount * 2] = textures[blocksTexturesNames[i] + "Dstrd"];
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

            Point mp = getMousePosition();
            Rectangle btnRect = getPlayBtnRect();

            if (btnRect.Contains(mp) && leftKeyClick())
            {
                curScreen = CurrentScreen.GameScreen;
                info = new GameInfo();

                field.Init();
            }
        }

        private void drawMainMenuScreen(GameTime gameTime)
        {
            Texture2D playBtn = textures["playBtn"];

            Point mp = getMousePosition();
            Rectangle btnRect = getPlayBtnRect();

            Texture2D btnToDraw = playBtn;

            if (btnRect.Contains(mp))
            {
                btnToDraw = Mouse.GetState().LeftButton == ButtonState.Pressed ? textures["playBtnClicked"] : textures["playBtnHov"];
            }

            spriteBatch.Draw(btnToDraw, btnRect, Color.White);
        }

        private void updateGameScreen(GameTime gameTime)
        {
            animationRunning = runningAnimations.Count > 0;
            Point mp = getMousePosition();

            if (gameEnded())
            {
                Rectangle okBtnRect = getOkBtnRect();

                if (okBtnRect.Contains(mp) && leftKeyClick())
                {
                    curScreen = CurrentScreen.MainMenuScreen;
                }
            }
            else if (!animationRunning)
            {
                updateGame();
            }
        }

        private void updateGame()
        {
            updateField();

            if (info.previousTurn != null)
            {
                addSwapAnimation(info.previousTurn.Value.block1, info.previousTurn.Value.block2);

                info.previousTurn = null;
            }

            int gameFieldSideLength = FIELD_SIZE * BLOCK_TEXTURE_SIZE;
            Point mp = getMousePosition();
            Rectangle gameFieldRect = new Rectangle(FIELD_SHIFT_BY_X, FIELD_SHIFT_BY_Y, gameFieldSideLength, gameFieldSideLength);

            if (leftKeyClick())
            {
                if (gameFieldRect.Contains(mp))
                {
                    int selectedCol = (mp.X - FIELD_SHIFT_BY_X) / BLOCK_TEXTURE_SIZE;
                    int selectedRow = (mp.Y - FIELD_SHIFT_BY_Y) / BLOCK_TEXTURE_SIZE;
                    TableCoords selectedBlock = new TableCoords(selectedCol, selectedRow);

                    if (info.blockSelected())
                    {
                        if (canSwap(info.curSelectedBlock.Value, selectedBlock))
                        {
                            info.previousTurn = new Turn(info.curSelectedBlock.Value, selectedBlock);

                            addSwapAnimation(info.curSelectedBlock.Value, selectedBlock);
                        }

                        info.dropSelection();
                    }
                    else
                    {
                        info.curSelectedBlock = selectedBlock;
                    }
                }
                else
                {
                    info.dropSelection();
                }
            }
        }

        private void addSwapAnimation(TableCoords block1, TableCoords block2)
        {
            int dcol = block2.col - block1.col;
            int drow = block2.row - block1.row;

            Vector2 shift = new Vector2(dcol * BLOCK_TEXTURE_SIZE / SWAP_ANIM_DURATION, drow * BLOCK_TEXTURE_SIZE / SWAP_ANIM_DURATION);

            MoveAnimation block1Movement = new MoveAnimation(
                getBlockCoords(block1),
                shift,
                SWAP_ANIM_DURATION,
                field.Get(block1),
                block2
            );

            MoveAnimation block2Movement = new MoveAnimation(
                getBlockCoords(block2),
                -shift,
                SWAP_ANIM_DURATION,
                field.Get(block2),
                block1
            );

            runningAnimations.Add(block1Movement);
            runningAnimations.Add(block2Movement);

            field.SetEmpty(block1);
            field.SetEmpty(block2);
        }

        private bool canSwap(TableCoords block1, TableCoords block2)
        {
            int dcol = Math.Abs(block2.col - block1.col);
            int drow = Math.Abs(block2.row - block1.row);

            return dcol * drow == 0 && (dcol == 1 || drow == 1);
        }

        private bool gameEnded()
        {
            return info.gameTimeMillisecondsElapsed / 1000 >= GAME_DURATION;
        }

        private int countBlocks(GameField.BlockType type, TableCoords start, TableCoords shift)
        {
            int counter = 0;
            TableCoords pos = start + shift;

            while (type == field.Get(pos))
            {
                ++counter;

                pos += shift;
            }

            return counter;
        }

        private void destroyOneBlock(TableCoords block)
        {
            info.score += 25;

            runningAnimations.Add(new DestroyAnimation(DESTROY_ANIMATION_DURATION, block, field.Get(block)));

            field.SetEmpty(block);
        }

        private void destroyChains()
        {
            bool[,] destroyed = new bool[FIELD_SIZE, FIELD_SIZE];

            for (int i = 0; i < FIELD_SIZE; ++i)
            {
                GameField.BlockType[] row = new GameField.BlockType[FIELD_SIZE];
                GameField.BlockType[] col = new GameField.BlockType[FIELD_SIZE];

                for (int j = 0; j < FIELD_SIZE; ++j)
                {
                    col[j] = field.Get(i, j);
                    row[j] = field.Get(j, i);
                }

                for (int j = 0; j < FIELD_SIZE - 2; ++j)
                {
                    if (col[j] != GameField.BlockType.Empty && col[j] == col[j + 1] && col[j] == col[j + 2])
                    {
                        for (int k = j; k < FIELD_SIZE && col[k] == col[j]; ++k)
                        {
                            destroyed[i, k] = true;
                        }
                    }

                    if (row[j] != GameField.BlockType.Empty && row[j] == row[j + 1] && row[j] == row[j + 2])
                    {
                        for (int k = j; k < FIELD_SIZE && row[k] == row[j]; ++k)
                        {
                            destroyed[k, i] = true;
                        }
                    }
                }
            }

            for (int i = 0; i < FIELD_SIZE; ++i)
            {
                for (int j = 0; j < FIELD_SIZE; ++j)
                {
                    if (destroyed[i, j])
                    {
                        destroyOneBlock(new TableCoords(i, j));
                    }
                }
            }

            if (
                info.turnMade()
                && (
                    field.IsEmpty(info.previousTurn.Value.block1)
                    || field.IsEmpty(info.previousTurn.Value.block2)
                    )
            )
            {
                info.previousTurn = null;
            }
        }

        private void createDropDownAnimations()
        {
            for (int i = 0; i < FIELD_SIZE; ++i)
            {
                int firstEmptyRow = -1;

                for (int j = FIELD_SIZE - 1; j >= 0 && firstEmptyRow == -1; --j)
                {
                    if (field.IsEmpty(new TableCoords(i, j)))
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
                        TableCoords pos = new TableCoords(i, j);
                        bool isEmpty = field.IsEmpty(pos);

                        fallHeight[j] = fallHeight[j + 1] + Convert.ToInt32(isEmpty);

                        if (!isEmpty)
                        {
                            TableCoords destination = new TableCoords(i, j + fallHeight[j]);

                            runningAnimations.Add(createOneDropDownAnimation(getBlockCoords(pos), destination, field.Get(pos)));

                            field.SetEmpty(pos);
                        }
                    }

                    int newBlocksCount = fallHeight[0];
                    float startY = FIELD_SHIFT_BY_Y - BLOCK_TEXTURE_SIZE;
                    float startX = getBlockCoords(new TableCoords(i, 0)).X;

                    for (int j = 0; j < newBlocksCount; ++j)
                    {
                        Vector2 start = new Vector2(startX, startY);
                        TableCoords destination = new TableCoords(i, newBlocksCount - j - 1);

                        runningAnimations.Add(createOneDropDownAnimation(start, destination, GameField.RandomType()));

                        startY -= BLOCK_TEXTURE_SIZE;
                    }
                }
            }
        }

        private MoveAnimation createOneDropDownAnimation(Vector2 start, TableCoords destination, GameField.BlockType type)
        {
            float duration = Math.Abs(getBlockCoords(destination).Y - start.Y) / BLOCK_TEXTURE_SIZE / BLOCK_DROP_DOWN_VELOCITY;
            Vector2 shift = new Vector2(0, BLOCK_TEXTURE_SIZE * BLOCK_DROP_DOWN_VELOCITY);

            return new MoveAnimation(start, shift, duration, type, destination);
        }

        private void updateField()
        {
            destroyChains();

            if (runningAnimations.Count == 0) // there is no destroy animations
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
                    TableCoords pos = new TableCoords(j, i);

                    if (field.IsEmpty(pos)) continue;

                    int typeIdx = (int)field.Get(pos);

                    if (isBlockSelected(pos))
                    {
                        typeIdx += (int)GameField.BlockType.BlocksCount;
                    }

                    Texture2D blockTexture = blocksTextures[typeIdx];

                    int x = FIELD_SHIFT_BY_X + j * BLOCK_TEXTURE_SIZE;
                    int y = FIELD_SHIFT_BY_Y + i * BLOCK_TEXTURE_SIZE;

                    spriteBatch.Draw(blockTexture, new Vector2(x, y), Color.White);
                }
            }

            if (!gameEnded())
            {
                info.gameTimeMillisecondsElapsed += (int)gameTime.ElapsedGameTime.TotalMilliseconds;
            }

            int timeRest = 60 - info.gameTimeMillisecondsElapsed / 1000;
            string leftSide = Convert.ToString(timeRest / 60);
            string rightSide = Convert.ToString(timeRest % 60);

            if (rightSide.Length < 2)
                rightSide = "0" + rightSide;

            spriteBatch.DrawString(common, "Timer: " + leftSide + ":" + rightSide, new Vector2(0, 0), Color.White);
            spriteBatch.DrawString(common, "Score: " + Convert.ToString(info.score), new Vector2(0, 30), Color.White);
        }

        private void drawGameScreenAnimation(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            List<Animation> animations = new List<Animation>();
            List<Animation>.Enumerator i = runningAnimations.GetEnumerator();

            while (i.MoveNext())
            {
                Animation curr = i.Current;

                curr.timeElapsed += dt;

                if (curr.timeElapsed < curr.duration)
                {
                    animations.Add(curr);
                }

                if (curr is MoveAnimation)
                {
                    MoveAnimation anim = (MoveAnimation)curr;

                    float x = anim.start.X + curr.timeElapsed * anim.shift.X;
                    float y = anim.start.Y + curr.timeElapsed * anim.shift.Y;

                    spriteBatch.Draw(blocksTextures[(int)anim.type], new Vector2(x, y), Color.White);

                    if (anim.Ended())
                    {
                        field.Set(anim.destination, anim.type);
                    }
                }
                else if (curr is DestroyAnimation)
                {
                    DestroyAnimation anim = (DestroyAnimation)curr;

                    Texture2D texture = blocksTextures[(int)anim.type + 2 * (int)GameField.BlockType.BlocksCount];
                    float alpha = 1 - anim.timeElapsed / anim.duration;

                    spriteBatch.Draw(texture, getBlockCoords(anim.block), Color.White * alpha);
                }
            }

            runningAnimations = animations;
        }

        private void drawGameScreenGameOver(GameTime gameTime)
        {
            int w = GraphicsDevice.Viewport.Width;
            int h = GraphicsDevice.Viewport.Height;

            Texture2D dialog = textures["gameOverDlg"];
            Texture2D okBtn = textures["okBtn"];

            Point mp = getMousePosition();
            Rectangle okBtnRect = getOkBtnRect();

            if (okBtnRect.Contains(mp))
            {
                okBtn = Mouse.GetState().LeftButton == ButtonState.Pressed ? textures["okBtnClicked"] : textures["okBtnHov"];
            }

            string msg1 = "Game Over!";
            string msg2 = "Your score: " + Convert.ToString(info.score) + " pts";

            Vector2 fm1 = common.MeasureString(msg1);
            Vector2 fm2 = common.MeasureString(msg2);

            spriteBatch.Draw(dialog, new Vector2((w - dialog.Width) / 2, (h - dialog.Height) / 2), Color.White);
            spriteBatch.DrawString(common, msg1, new Vector2((w - fm1.X) / 2, (h - dialog.Height) / 2 + fm1.Y), Color.White);
            spriteBatch.DrawString(common, msg2, new Vector2((w - fm2.X) / 2, h / 2 - fm2.Y * 2), Color.White);
            spriteBatch.Draw(okBtn, okBtnRect, Color.White);
        }

        private Texture2D createRectangle(int width, int height, Color color)
        {
            Texture2D rect = new Texture2D(GraphicsDevice, width, height);

            Color[] colorMap = Enumerable.Repeat(color, height * width).ToArray();

            rect.SetData(colorMap);

            return rect;
        }

        private Point getMousePosition()
        {
            return Mouse.GetState().Position;
        }

        private Vector2 getBlockCoords(TableCoords pos)
        {
            float x = FIELD_SHIFT_BY_X + pos.col * BLOCK_TEXTURE_SIZE;
            float y = FIELD_SHIFT_BY_Y + pos.row * BLOCK_TEXTURE_SIZE;

            return new Vector2(x, y);
        }

        private bool leftKeyClick()
        {
            return Mouse.GetState().LeftButton == ButtonState.Released && lastState.LeftButton == ButtonState.Pressed;
        }

        private bool isBlockSelected(TableCoords pos)
        {
            return info.blockSelected() && info.curSelectedBlock.Value == pos;
        }

        private Rectangle getPlayBtnRect()
        {
            Texture2D playBtn = textures["playBtn"];

            int btnCenterX = (GraphicsDevice.Viewport.Width - playBtn.Width) / 2;
            int btnCenterY = (GraphicsDevice.Viewport.Height - playBtn.Height) / 2;

            return new Rectangle(btnCenterX, btnCenterY, playBtn.Width, playBtn.Height);
        }

        private Rectangle getOkBtnRect()
        {
            Texture2D okBtn = textures["okBtn"];

            int okBtnCenterX = (GraphicsDevice.Viewport.Width - okBtn.Width) / 2;
            int okBtnCenterY = (GraphicsDevice.Viewport.Height + okBtn.Height) / 2;

            return new Rectangle(okBtnCenterX, okBtnCenterY, okBtn.Width, okBtn.Height);
        }

        private Texture2D loadTexture(string name)
        {
            return Content.Load<Texture2D>("graphics/" + name);
        }
    }
}
