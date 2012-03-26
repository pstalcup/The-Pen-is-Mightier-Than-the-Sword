using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace RTS_Scroll_Test
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        const int GRID_WIDTH = 25;
        const int GRID_HEIGHT = 25;
        const int GRID_PIXEL = 100;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Texture2D block;
        SpriteFont sf;

        Rectangle[,] grid;
        Color[,] colors;

        //randomization
        Random random;

        //screen stuff
        Rectangle screen;
        Rectangle maxSize;
        Rectangle zoomScreen;

        //scrolling
        Rectangle scrollUp;
        Rectangle scrollDown;
        Rectangle scrollLeft;
        Rectangle scrollRight;
        Boolean scrolledLast;
        int scrollSpeed;

        //zooming
        float zoom;
        float zoomFactor;

        //selection box
        Vector2 lastLeftClick;
        Rectangle selection;
        List<Unit> selected;

        //input variables
        KeyboardState lastKeyboard;
        MouseState lastMouse;

        //Units
        List<Unit> units;

        //frame rate stuff
        TimeSpan timeBetweenFrames;
        TimeSpan lastFrame;

        //BottomBar stuff
        Rectangle taskBar;

        //action box stuff

        Rectangle actionBox;
        Rectangle actionButton;
        Rectangle stopButton;
        Rectangle moveButton;

        enum MouseActionState { Normal, Stop, Action, Move };
        MouseActionState currentMouseActionState;

        //Party selestion stuff
        Rectangle partyBox;
        List<Rectangle> portraits;

        //Minimap stuff
        Rectangle minimapArea;
        Rectangle minimapOutline;
        Color[,] minimap;
        int minimapScale;

        //Loaded Textures
        Texture2D unitTexture;
        Texture2D stop;
        Texture2D move;
        Texture2D attack;

        //Player
        Team player;
        Team enemy;
        Team neutral;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);

            //big enough to account for a sizeable fullscreen resolution
            graphics.PreferredBackBufferHeight = 1200;
            graphics.PreferredBackBufferHeight = 800;

            graphics.SynchronizeWithVerticalRetrace = false;
            graphics.ApplyChanges();

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            IsFixedTimeStep = false;
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

            grid = new Rectangle[GRID_WIDTH, GRID_HEIGHT];
            colors = new Color[GRID_WIDTH, GRID_HEIGHT];
            minimap = new Color[GRID_WIDTH, GRID_HEIGHT];

            maxSize = new Rectangle(0, 0, GRID_PIXEL * GRID_WIDTH, GRID_PIXEL * GRID_HEIGHT);

            units = new List<Unit>();
            selected = new List<Unit>();
            portraits = new List<Rectangle>();

            random = new Random();

            player = new Team("Player", Color.Blue);
            enemy = new Team("Enemy", Color.Red);
            neutral = new Team("Neutral", Color.LightGray);

            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    grid[x, y] = new Rectangle(x * GRID_PIXEL, y * GRID_PIXEL, GRID_PIXEL, GRID_PIXEL);
                    colors[x, y] = y % 2 == x % 2 ? Color.Gray : Color.GhostWhite;
                    minimap[x, y] = Color.White;
                }
            }

            //CONSTANTS
            minimapScale = 4;
            zoom = 1.0f;
            zoomFactor = 0.01f;
            scrollSpeed = 5;
            timeBetweenFrames = new TimeSpan(TimeSpan.TicksPerSecond / 60);

            CalculateScreen();

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

            sf = Content.Load<SpriteFont>("font");
            unitTexture = Content.Load<Texture2D>("Unit");
            stop = Content.Load<Texture2D>("stop");
            move = Content.Load<Texture2D>("move");
            attack = Content.Load<Texture2D>("attack");

            block = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            block.SetData<Color>(Enumerable.Repeat(Color.White, 1).ToArray());
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

            TimeSpan currentFrame = gameTime.TotalGameTime;
            bool updateFrame = false;
            if ((currentFrame - lastFrame) > timeBetweenFrames)
            {
                lastFrame = currentFrame;
                updateFrame = true;
            }

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here

            MouseState currentMouse = Mouse.GetState();
            KeyboardState currentKeyboard = Keyboard.GetState();

            //generic mouse input

            int mx = currentMouse.X;
            int my = currentMouse.Y;

            int ax = (int)(mx / zoom + zoomScreen.X);
            int ay = (int)(my / zoom + zoomScreen.Y);

            //LEFTCLICKING

            if (minimapArea.Contains(mx, my) && currentMouse.LeftButton == ButtonState.Pressed)
            {
                int gx = (mx - minimapArea.X) / minimapScale;
                int gy = (my - minimapArea.Y) / minimapScale;

                int lx = screen.X;
                int ly = screen.Y;

                screen.X = (gx * GRID_PIXEL) - screen.Width / 2;
                screen.Y = (gy * GRID_PIXEL) - screen.Height / 2;

                ZoomScreen();

                if (!maxSize.Contains(zoomScreen))
                {
                    screen.X = lx;
                    screen.Y = ly;
                    ZoomScreen();
                }
            }
            else if (IsPressed(stopButton, currentMouse))
            {
                foreach (Unit u in units)
                {
                    u.Destination = u.Center;
                }
                currentMouseActionState = MouseActionState.Stop;
            }
            else if (IsPressed(moveButton, currentMouse))
            {
                currentMouseActionState = MouseActionState.Move;
            }
            else if (IsPressed(actionButton, currentMouse))
            {
                currentMouseActionState = MouseActionState.Action;
            }
            else if (screen.Contains(mx + screen.X, my + screen.Y))
            {
                if (currentMouse.LeftButton == ButtonState.Pressed)
                {
                    if (currentMouseActionState == MouseActionState.Action)
                    {
                        foreach (Unit u in units)
                        {
                            u.Action(ax, ay, units);
                        }
                    }
                    else if (currentMouseActionState == MouseActionState.Move)
                    {
                        Vector2 d = new Vector2(ax, ay);
                        foreach (Unit u in selected)
                        {
                            u.Destination = d;
                        }
                    }
                    else if (currentMouseActionState == MouseActionState.Stop)
                    {
                    }
                    else if (currentMouseActionState == MouseActionState.Normal)
                    {
                        if (lastMouse.LeftButton == ButtonState.Pressed)
                        {

                            int width = mx - ((int)lastLeftClick.X);
                            int height = my - ((int)lastLeftClick.Y);

                            selection = new Rectangle((int)lastLeftClick.X, (int)lastLeftClick.Y, width, height);
                        }
                        else
                        {
                            lastLeftClick = new Vector2(mx, my);
                            selection = new Rectangle((int)lastLeftClick.X, (int)lastLeftClick.Y, 0, 0);
                        }
                    }
                    currentMouseActionState = MouseActionState.Normal;
                }
                else
                {
                    if (lastMouse.LeftButton == ButtonState.Pressed)
                    {
                        Rectangle scaleSel = new Rectangle((int)(selection.X * zoom + zoomScreen.X), (int)(selection.Y * zoom + zoomScreen.Y), (int)(selection.Width * zoom), (int)(selection.Height * zoom));
                        if (!currentKeyboard.IsKeyDown(Keys.LeftShift) && !currentKeyboard.IsKeyDown(Keys.RightShift))
                        {
                            selected.Clear();
                        }

                        foreach (Unit u in player.Units)
                        {
                            if (scaleSel.Intersects(u.HardCollisionBox) && !selected.Contains(u) && selected.Count < portraits.Count)
                            {
                                selected.Add(u);
                            }
                        }
                        selection = new Rectangle(-1, -1, 0, 0);
                        lastLeftClick = new Vector2(-1, -1);
                    }
                }
            }

            //RIGHTCLICKING

            if (currentMouse.RightButton == ButtonState.Pressed)
            {
                if (lastMouse.RightButton == ButtonState.Released)
                {
                    Vector2 d = new Vector2(ax, ay);
                    foreach (Unit u in selected)
                    {
                        u.Destination = d;
                    }
                }
            }
            //screen scrolling logic
            if (scrolledLast || (currentMouse.X != lastMouse.X && currentMouse.Y != lastMouse.Y))
            {
                int dx = 0;
                int dy = 0;
                if (scrollDown.Contains(currentMouse.X, currentMouse.Y))
                {
                    dy = scrollSpeed;
                }
                if (scrollUp.Contains(currentMouse.X, currentMouse.Y))
                {
                    dy = -scrollSpeed;
                }
                if (scrollLeft.Contains(currentMouse.X, currentMouse.Y))
                {
                    dx = -scrollSpeed;
                }
                if (scrollRight.Contains(currentMouse.X, currentMouse.Y))
                {
                    dx = scrollSpeed;
                }
                if (dx != 0)
                {
                    scrolledLast = true;
                    screen.Offset(dx, 0);
                    ZoomScreen();
                    if (!maxSize.Contains(zoomScreen))
                    {
                        screen.Offset(-dx, 0);
                        ZoomScreen();
                    }
                }
                if (dy != 0)
                {
                    scrolledLast = true;
                    screen.Offset(0,dy);
                    ZoomScreen();
                    if (!maxSize.Contains(zoomScreen))
                    {
                        screen.Offset(0,-dy);
                        ZoomScreen();
                    }
                }
            }

            //generic keyboard input
            if (currentKeyboard.IsKeyDown(Keys.N) && lastKeyboard.IsKeyUp(Keys.N))
            {
                Unit toAdd;
                do
                {
                    int rx = random.Next(screen.X, screen.X + screen.Width);
                    int ry = random.Next(screen.Y, screen.Y + screen.Height);
                    toAdd = new Unit(rx, ry, 50, 50, unitTexture, unitTexture, attack);
                } while (!maxSize.Contains(toAdd.DrawArea));
                units.Add(toAdd);
                if (currentKeyboard.IsKeyDown(Keys.RightShift) || currentKeyboard.IsKeyDown(Keys.LeftShift))
                {
                    enemy.AddUnit(toAdd);
                }
                else
                {
                    player.AddUnit(toAdd);
                }
            }
            if (currentKeyboard.IsKeyDown(Keys.F) && lastKeyboard.IsKeyUp(Keys.F))
            {
                graphics.ToggleFullScreen();
                CalculateScreen();
            }
            if (currentKeyboard.IsKeyDown(Keys.S) && lastKeyboard.IsKeyUp(Keys.S))
            {
                Team temp = player;
                player = enemy;
                enemy = temp;
            }
            if (currentKeyboard.IsKeyDown(Keys.Escape) && lastKeyboard.IsKeyUp(Keys.Escape))
            {
                if (graphics.IsFullScreen)
                {
                    graphics.ToggleFullScreen();
                    CalculateScreen();
                }
            }

            lastKeyboard = currentKeyboard;
            lastMouse = currentMouse;

            if (updateFrame)
            {
                for (int x = 0; x < GRID_WIDTH; x++)
                {
                    for (int y = 0; y < GRID_HEIGHT; y++)
                    {
                        minimap[x, y] = Color.White;
                    }
                }

                foreach (Unit u in units)
                {
                    u.Update();
                    if (u.IsMoving())
                    {
                        foreach (Unit v in units)
                        {
                            if (u.ID != v.ID && u.CollidesWith(v))
                            {
                                //lets go on the assumption that something will only ever collide with something else if it is moving
                                u.UndoUpdate();
                            }
                        }
                    }
                    int gx = (int)(u.Center.X / GRID_PIXEL);
                    int gy = (int)(u.Center.Y / GRID_PIXEL);

                    minimap[gx, gy] = u.Team.TeamColor;
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
            GraphicsDevice.Clear(Color.White);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Matrix.CreateScale(new Vector3(zoom, zoom, 1)));

            //Draw the Background
            Rectangle toDraw;
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    if (zoomScreen.Intersects(grid[x, y]))
                    {
                        toDraw = grid[x, y];

                        toDraw.X -= zoomScreen.X;
                        toDraw.Y -= zoomScreen.Y;

                        spriteBatch.Draw(block, toDraw, colors[x, y]);
                        spriteBatch.DrawString(sf, "(" + x.ToString() + "," + y.ToString() + ")", new Vector2(toDraw.X, toDraw.Y), Color.Black);
                    }
                }
            }

            foreach (Unit u in units)
            {
                toDraw = u.DrawArea;
                toDraw.Offset(-zoomScreen.X, -zoomScreen.Y);
                spriteBatch.Draw(u.Texture, toDraw, u.Team.TeamColor);
                if (selected.Contains(u))
                {
                    OutlineRectangle(toDraw, Color.Blue, 1);
                }
            }

            spriteBatch.End();

            spriteBatch.Begin(); //unscaled

            DrawUI();

            spriteBatch.End();

            base.Draw(gameTime);
        }

        public void DrawUI()
        {

            Rectangle toDraw;

            Vector2 offset = sf.MeasureString(player.TeamName);

            toDraw = new Rectangle(10,10,(int)offset.X,(int)offset.Y);

            spriteBatch.Draw(block, toDraw, Color.White);
            toDraw.Offset(-1, -1);
            toDraw.Width += 1;
            toDraw.Height += 1;
            OutlineRectangle(toDraw, Color.Black, 1);

            spriteBatch.DrawString(sf, player.TeamName, new Vector2(10, 10), player.TeamColor);
            
            //render the bottom bar
            spriteBatch.Draw(block, taskBar, Color.DarkOliveGreen);
            OutlineRectangle(taskBar, Color.Black, 3);
            spriteBatch.Draw(block, actionBox, Color.White);
            OutlineRectangle(actionBox, Color.Black, 3);
            spriteBatch.Draw(block, partyBox, Color.White);
            OutlineRectangle(partyBox, Color.Black, 3);
            OutlineRectangle(minimapOutline, Color.Black, 2);

            //draw the buttons in the action box
            spriteBatch.Draw(stop, stopButton, Color.White);
            Color outline = Color.Black;
            if (currentMouseActionState == MouseActionState.Stop)
            {
                outline = Color.Blue;
            }
            else
            {
                outline = Color.Black;
            }
            OutlineRectangle(stopButton, outline, 1);
            spriteBatch.Draw(move, moveButton, Color.White);
            if (currentMouseActionState == MouseActionState.Move)
            {
                outline = Color.Blue;
            }
            else
            {
                outline = Color.Black;
            }
            OutlineRectangle(moveButton, outline, 1);
            spriteBatch.Draw(attack, actionButton, Color.White);
            if (currentMouseActionState == MouseActionState.Action)
            {
                outline = Color.Blue;
            }
            else
            {
                outline = Color.Black;
            }
            OutlineRectangle(actionButton, outline, 1);

            int mx = minimapArea.X;
            int my = minimapArea.Y;

            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    toDraw = new Rectangle(mx + (x * minimapScale), my + (y * minimapScale), minimapScale, minimapScale);
                    spriteBatch.Draw(block, toDraw, minimap[x, y]);
                }
            }

            for (int i = 0; i < selected.Count; i++)
            {
                spriteBatch.Draw(selected[i].Portrait, portraits[i], Color.White);
            }

            toDraw = new Rectangle(mx + minimapScale * zoomScreen.X / GRID_PIXEL, my + minimapScale * zoomScreen.Y / GRID_PIXEL, (minimapScale * zoomScreen.Width / GRID_PIXEL), (minimapScale * zoomScreen.Height / GRID_PIXEL));
            OutlineRectangle(toDraw, Color.Black, 1);

            if (lastLeftClick.X != -1)
            {
                OutlineRectangle(selection, Color.Blue, 1);
            }
        }

        public void OutlineRectangle(Rectangle r, Color c, int thick)
        {
            if (r.Width < 0)
            {
                r = new Rectangle(r.X + r.Width, r.Y, -r.Width, r.Height);
            }
            if (r.Height < 0)
            {
                r = new Rectangle(r.X, r.Y + r.Height, r.Width, -r.Height);
            }
            Rectangle topLine = new Rectangle(r.X, r.Y, r.Width, thick);
            Rectangle bottomLine = new Rectangle(r.X, r.Y + r.Height - thick, r.Width, thick);
            Rectangle leftLine = new Rectangle(r.X, r.Y, thick, r.Height);
            Rectangle rightLine = new Rectangle(r.X + r.Width - thick, r.Y, thick, r.Height);

            spriteBatch.Draw(block, topLine, c);
            spriteBatch.Draw(block, bottomLine, c);
            spriteBatch.Draw(block, leftLine, c);
            spriteBatch.Draw(block, rightLine, c);
        }
        public void OutlineRectangle(Rectangle r, Color c)
        {
            OutlineRectangle(r, c, 5);
        }
        void ZoomScreen()
        {
            zoomScreen = new Rectangle((int)(screen.X / zoom), (int)(screen.Y / zoom), (int)(screen.Width / zoom), (int)(screen.Height / zoom));
        }

        void CalculateScreen()
        {
            int newW = graphics.GraphicsDevice.PresentationParameters.BackBufferWidth;
            int newH = graphics.GraphicsDevice.PresentationParameters.BackBufferHeight;

            int scroll_box_size = 50;
            int taskbar_size = 150;

            screen = new Rectangle(0, 0, newW, newH - taskbar_size);

            taskBar = new Rectangle(0, newH - taskbar_size, newW, taskbar_size);
            actionBox = new Rectangle(taskBar.X + 10, taskBar.Y + 10, 130, 130);
            partyBox = new Rectangle(newW / 2 - 200, taskBar.Y + 10, 400, taskbar_size - 20);
            Console.WriteLine(partyBox);

            minimapArea = new Rectangle(newW - 25 - GRID_WIDTH * minimapScale, newH - GRID_HEIGHT * minimapScale - 25, GRID_WIDTH * minimapScale, GRID_HEIGHT * minimapScale);
            minimapOutline = new Rectangle(minimapArea.X - 2, minimapArea.Y - 2, minimapArea.Width + 4, minimapArea.Height + 4);

            newH -= taskbar_size;

            scrollUp = new Rectangle(0, 0, newW, scroll_box_size);
            scrollDown = new Rectangle(0, newH - scroll_box_size, newW, scroll_box_size);
            scrollLeft = new Rectangle(0, 0, scroll_box_size, newH);
            scrollRight = new Rectangle(newW - scroll_box_size, 0, scroll_box_size, newH);

            int portraitSize = 50;
            int portraitMargin = 5;

            int portraitsPerRow = partyBox.Width / (portraitSize + portraitMargin);
            int portraitMaxRows = partyBox.Height / (portraitSize + portraitMargin);

            int r = 0;
            int c = 0;

            portraits.Clear();

            for (int i = 0; i < portraitsPerRow * portraitMaxRows; i++)
            {
                Rectangle toDraw = new Rectangle(partyBox.X + portraitMargin + (portraitMargin + portraitSize) * c, partyBox.Y + portraitMargin + (portraitMargin + portraitSize) * r, portraitSize, portraitSize);
                c++;
                if (c == portraitsPerRow)
                {
                    c = 0;
                    r++;
                }
                portraits.Add(toDraw);
            }

            int buttonWidth = 30;
            int buttonBuffer = 10;

            moveButton = new Rectangle(actionBox.X + buttonBuffer, actionBox.Y + buttonBuffer, buttonWidth, buttonWidth);
            actionButton = new Rectangle(actionBox.X + actionBox.Width / 2 - buttonWidth / 2, actionBox.Y + buttonBuffer, buttonWidth, buttonWidth);
            stopButton = new Rectangle(actionBox.X + actionBox.Width - buttonBuffer - buttonWidth, actionBox.Y + 10, buttonWidth, buttonWidth);

            ZoomScreen();
        }
        public Boolean IsPressed(Rectangle button, MouseState ms)
        {
            return button.Contains(ms.X, ms.Y) && ms.LeftButton == ButtonState.Pressed;
        }
    }
}
/*
 * 
            if (currentMouse.LeftButton == ButtonState.Pressed)
            {
                //various buttons
                if (stopButton.Contains(currentMouse.X, currentMouse.Y))
                {
                    currentMouseActionState = MouseActionState.Stop;
                    foreach (Unit u in selected)
                    {
                        u.Destination = u.Center;
                    }
                }
                else if (actionButton.Contains(currentMouse.X, currentMouse.Y) && selected.Count > 0)
                {
                    currentMouseActionState = MouseActionState.Action;
                }
                else if (moveButton.Contains(currentMouse.X, currentMouse.Y) && selected.Count > 0)
                {
                    currentMouseActionState = MouseActionState.Move;
                }
                else if (minimapArea.Contains(currentMouse.X, currentMouse.Y) && selected.Count > 0)
                {
                    int gx = (currentMouse.X - minimapArea.X) / minimapScale;
                    int gy = (currentMouse.Y - minimapArea.Y) / minimapScale;

                    int lx = screen.X;
                    int ly = screen.Y;

                    screen.X = (gx * GRID_PIXEL) - screen.Width / 2;
                    screen.Y = (gy * GRID_PIXEL) - screen.Height / 2;

                    ZoomScreen();

                    if (!maxSize.Contains(zoomScreen))
                    {
                        screen.X = lx;
                        screen.Y = ly;
                        ZoomScreen();
                    }
                }
                else if(screen.Contains(currentMouse.X,currentMouse.Y))
                {

                    int ax = (int)(currentMouse.X * zoom + zoomScreen.X);
                    int ay = (int)(currentMouse.Y * zoom + zoomScreen.Y);

                    Console.WriteLine(currentMouseActionState +" "+ performedAction);

                    if (currentMouseActionState == MouseActionState.Action)
                    {
                        foreach (Unit u in selected)
                        {
                            u.Action(ax, ay, units);
                        }
                        performedAction = true;
                    }
                    else if (currentMouseActionState == MouseActionState.Move)
                    {
                        foreach (Unit u in selected)
                        {
                            u.Destination = new Vector2(ax, ay);
                        }
                        performedAction = true;
                    }
                    else
                    {
                        if (lastMouse.LeftButton == ButtonState.Pressed && !performedAction)
                        {
                            int width = currentMouse.X - ((int)lastLeftClick.X);
                            int height = currentMouse.Y - ((int)lastLeftClick.Y);

                            selection = new Rectangle((int)lastLeftClick.X, (int)lastLeftClick.Y, width, height);
                        }
                        else
                        {
                            performedAction = false;
                            lastLeftClick = new Vector2(currentMouse.X, currentMouse.Y);
                            selection = new Rectangle((int)lastLeftClick.X, (int)lastLeftClick.Y, 0, 0);
                        }
                    }
                    currentMouseActionState = MouseActionState.Normal;
                }
            }
            else
            {
                if (lastMouse.LeftButton == ButtonState.Pressed)
                {
                    if (!currentKeyboard.IsKeyDown(Keys.LeftShift) && !currentKeyboard.IsKeyDown(Keys.RightShift))
                    {
                        selected.Clear();
                    }

                    Rectangle offsetSelection = new Rectangle((int)(selection.X / zoom), (int)(selection.Y / zoom), (int)(selection.Width / zoom), (int)(selection.Height / zoom));
                    ZoomScreen();
                    offsetSelection.Offset(zoomScreen.X, zoomScreen.Y);

                    foreach (Unit u in units)
                    {
                        if (offsetSelection.Intersects(u.HardCollisionBox) && !selected.Contains(u))
                        {
                            selected.Add(u);
                        }
                    }
                    lastLeftClick = new Vector2(-1, -1);
                }

            }
            if (currentMouse.RightButton == ButtonState.Pressed && lastMouse.RightButton == ButtonState.Released)
            {
                ZoomScreen();

                int tx = currentMouse.X + zoomScreen.X;
                int ty = currentMouse.Y + zoomScreen.Y;

                if (minimapArea.Contains(currentMouse.X, currentMouse.Y))
                {
                    tx = (GRID_PIXEL * (currentMouse.X - minimapArea.X) / minimapScale) + GRID_PIXEL / 2;
                    ty = (GRID_PIXEL * (currentMouse.Y - minimapArea.Y) / minimapScale) + GRID_PIXEL / 2;
                }
                foreach (Unit u in selected)
                {
                    u.Destination = new Vector2(tx, ty);
                }
            }

            if (currentMouse.ScrollWheelValue != lastMouse.ScrollWheelValue)
            {
                float lZoom = zoom;
                zoom += zoomFactor * (currentMouse.ScrollWheelValue > lastMouse.ScrollWheelValue ? 1 : -1);
                ZoomScreen();
                if (!maxSize.Contains(zoomScreen) || zoom < 0.25f || zoom > 1.5f)
                {
                    zoom = lZoom;
                }
            }
 */
