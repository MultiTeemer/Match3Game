﻿using System;
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
        private const float SWAP_ANIM_DURATION = 200;
        private const float BLOCK_DROP_DOWN_VELOCITY = 5e-3f;

        private int FIELD_SHIFT_BY_X;
        private int FIELD_SHIFT_BY_Y;
        private int BLOCK_TEXTURE_SIZE;

        private int curSelectedItemIdx;

        private bool animationRunning;

        private List<MoveAnimation> curAnimations;
        private bool[] inAnimation;

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

            animationRunning = false;

            curAnimations = new List<MoveAnimation>();
            inAnimation = new bool[FIELD_SIZE * FIELD_SIZE];
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
            animationRunning = curAnimations.Count > 0;

            if (animationRunning) return;

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
                id1,
                shift,
                SWAP_ANIM_DURATION,
                field.Get(id1),
                blockIdToTableCoords(id2)
            );

            MoveAnimation block2Movement = new MoveAnimation(
                id2,
                -shift,
                SWAP_ANIM_DURATION,
                field.Get(id2),
                blockIdToTableCoords(id1)
            );

            inAnimation[id1] = true;
            inAnimation[id2] = true;

            curAnimations.Add(block1Movement);
            curAnimations.Add(block2Movement);
        }

        private bool canSwap(int id1, int id2)
        {
            Vector2 p1 = blockIdToTableCoords(id1);
            Vector2 p2 = blockIdToTableCoords(id2);

            int dx = (int)Math.Abs(p2.X - p1.X);
            int dy = (int)Math.Abs(p2.Y - p1.Y);

            return dx * dy == 0 && (dx != 0 || dy != 0);
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

            // add to score
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

                    if (horCount >= 3)
                    {
                        blocksDestroyed = true;

                        destroyBlocks(type, start, new Vector2(-1, 0));
                        destroyBlocks(type, start, new Vector2(1, 0));
                    }

                    int verCount = countBlocks(type, start, new Vector2(0, -1)) + 1 + countBlocks(type, start, new Vector2(0, 1));

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

                    fallHeight[firstEmptyRow] = 1;

                    for (int j = firstEmptyRow - 1; j >= 0; --j)
                    {
                        int idx = tableCoordsToBlockId(i, j);
                        bool isEmpty = field.IsEmpty(idx);

                        fallHeight[j] = fallHeight[j + 1] + Convert.ToInt32(field.IsEmpty(idx));

                        if (!isEmpty)
                        {
                            Vector2 shift = new Vector2(0, BLOCK_TEXTURE_SIZE * BLOCK_DROP_DOWN_VELOCITY);
                            Vector2 destination = new Vector2(i, j + fallHeight[j]);
                            float duration = fallHeight[j] / BLOCK_DROP_DOWN_VELOCITY;

                            MoveAnimation anim = new MoveAnimation(idx, shift, duration, field.Get(idx), destination);

                            curAnimations.Add(anim);
                            field.SetEmpty(idx);

                            inAnimation[idx] = true;
                        }
                    }
                }
            }
        }

        private void updateField()
        {
            destroyChains();
            createDropDownAnimations();
        }

        private void drawGameScreen(GameTime gameTime)
        {
            if (animationRunning)
                drawGameScreenAnimation(gameTime);

            for (int i = 0; i < FIELD_SIZE; ++i)
            {
                for (int j = 0; j < FIELD_SIZE; ++j)
                {
                    int idx = tableCoordsToBlockId(j, i);

                    if (field.Get(idx) == GameField.BlockTypeE.Empty) continue; // remove later

                    if (inAnimation[idx]) continue;

                    Texture2D blockTexture = getBlockTextureById(idx);

                    int x = FIELD_SHIFT_BY_X + j * BLOCK_TEXTURE_SIZE;
                    int y = FIELD_SHIFT_BY_Y + i * BLOCK_TEXTURE_SIZE;

                    spriteBatch.Draw(blockTexture, new Vector2(x, y), Color.White);
                }
            }
        }

        private void drawGameScreenAnimation(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            List<MoveAnimation> animations = new List<MoveAnimation>();
            List<MoveAnimation>.Enumerator i = curAnimations.GetEnumerator();

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
                    int idx = tableCoordsToBlockId(curr.destination);
                    inAnimation[idx] = false;

                    field.Set(idx, curr.type);
                }

                Vector2 tableCoords = blockIdToTableCoords(curr.blockId);

                float x = FIELD_SHIFT_BY_X + tableCoords.X * BLOCK_TEXTURE_SIZE + curr.timeElapsed * curr.shift.X;
                float y = FIELD_SHIFT_BY_Y + tableCoords.Y * BLOCK_TEXTURE_SIZE + curr.timeElapsed * curr.shift.Y;

                spriteBatch.Draw(getBlockTextureByType(curr.type), new Vector2(x, y), Color.White);
            }

            curAnimations = animations;
        }

        private Point getMousePosition()
        {
            return Mouse.GetState().Position;
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
