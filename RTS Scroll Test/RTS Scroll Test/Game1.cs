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
        const int GRID_WIDTH = 100;
        const int GRID_HEIGHT = 100;
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

        //Loaded Textures
        Texture2D unitTexture;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);

            //big enough to account for a sizeable fullscreen resolution
            graphics.PreferredBackBufferHeight = 1200;
            graphics.PreferredBackBufferHeight = 800;

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            IsFixedTimeStep = false;
        }

        void ZoomScreen()
        {
            zoomScreen = new Rectangle((int)(screen.X / zoom), (int)(screen.Y / zoom), (int)(screen.Width / zoom), (int)(screen.Height / zoom));
        }

        void CalculateScreen()
        {
            int newW = graphics.GraphicsDevice.PresentationParameters.BackBufferWidth;
            int newH = graphics.GraphicsDevice.PresentationParameters.BackBufferHeight;

            screen = new Rectangle(0, 0, newW, newH);

            int scroll_box_size = 100;

            scrollUp = new Rectangle(0, 0, newW, scroll_box_size);
            scrollDown = new Rectangle(0, newH - scroll_box_size, newW, scroll_box_size);
            scrollLeft = new Rectangle(0, 0, scroll_box_size, newH);
            scrollRight = new Rectangle(newW - scroll_box_size, 0, scroll_box_size, newH);
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

            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    grid[x, y] = new Rectangle(x * GRID_PIXEL, y * GRID_PIXEL, GRID_PIXEL, GRID_PIXEL);
                    colors[x, y] = y % 2 == x % 2 ? Color.Gray : Color.GhostWhite;
                }
            }

            maxSize = new Rectangle(0, 0, GRID_PIXEL * GRID_WIDTH, GRID_PIXEL * GRID_HEIGHT);

            zoom = 1.0f;
            zoomFactor = 0.01f;

            scrollSpeed = 5;

            units = new List<Unit>();
            selected = new List<Unit>();

            random = new Random();

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

            block = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            Color[] data = new Color[1];
            data[0] = Color.White;
            block.SetData<Color>(data);


            // TODO: use this.Content to load your game content here
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
            if (gameTime.IsRunningSlowly)
            {
                Console.WriteLine("LAGGING");
            }

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here

            MouseState currentMouse = Mouse.GetState();
            KeyboardState currentKeyboard = Keyboard.GetState();

            //generic mouse input
            if (currentMouse.LeftButton == ButtonState.Pressed)
            {
                if (lastMouse.LeftButton == ButtonState.Pressed)
                {
                    int width = currentMouse.X - ((int)lastLeftClick.X);
                    int height = currentMouse.Y - ((int)lastLeftClick.Y);

                    selection = new Rectangle((int)lastLeftClick.X, (int)lastLeftClick.Y, width, height);
                }
                else
                {
                    lastLeftClick = new Vector2(currentMouse.X, currentMouse.Y);
                    selection = new Rectangle((int)lastLeftClick.X, (int)lastLeftClick.Y, 0, 0);
                }
            }
            else
            {
                if (lastMouse.LeftButton == ButtonState.Pressed)
                {
                    selected.Clear();

                    Rectangle offsetSelection = new Rectangle((int)(selection.X / zoom), (int)(selection.Y / zoom), (int)(selection.Width / zoom), (int)(selection.Height / zoom));
                    ZoomScreen();
                    offsetSelection.Offset(zoomScreen.X, zoomScreen.Y);

                    foreach (Unit u in units)
                    {
                        if (offsetSelection.Intersects(u.HardCollisionBox))
                        {
                            selected.Add(u);
                        }
                    }
                    lastLeftClick = new Vector2(-1, -1);
                }
            }
            if (currentMouse.RightButton == ButtonState.Pressed && lastMouse.RightButton == ButtonState.Released)
            {
                foreach (Unit u in units)
                {
                    u.Destination = new Vector2(currentMouse.X, currentMouse.Y);
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
                if (dx != 0 || dy != 0)
                {
                    scrolledLast = true;
                    screen.Offset(dx, dy);
                    ZoomScreen();
                    if (!maxSize.Contains(zoomScreen))
                    {
                        screen.Offset(-dx, -dy);
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
                    toAdd = new Unit(random.Next(screen.X, screen.X + screen.Width), random.Next(screen.Y, screen.Y + screen.Height), 50, 50, unitTexture);
                } while (!maxSize.Contains(toAdd.DrawArea));
                units.Add(toAdd);
            }
            if (currentKeyboard.IsKeyDown(Keys.F) && lastKeyboard.IsKeyUp(Keys.F))
            {
                graphics.ToggleFullScreen();
                CalculateScreen();
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

            foreach(Unit u in units)
            {
                u.Update();
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
            
            for (int x = screen.X/GRID_PIXEL; x <= (screen.X+screen.Width)/GRID_PIXEL; x++)
            {
                for (int y = screen.Y / GRID_PIXEL; y <= (screen.Y+screen.Height)/GRID_PIXEL; y++)
                {
                    if (zoomScreen.Intersects(grid[x, y]))
                    {
                        Rectangle toDraw = grid[x, y];

                        toDraw.X -= zoomScreen.X;
                        toDraw.Y -= zoomScreen.Y;

                        spriteBatch.Draw(block, toDraw, colors[x, y]);
                        spriteBatch.DrawString(sf, "(" + x.ToString() + "," + y.ToString() + ")", new Vector2(toDraw.X, toDraw.Y), Color.Black);
                    }
                }
            }
             
            //Draw the Units
            Console.WriteLine(units.Count);
            foreach (Unit u in units)
            {
                Rectangle toDraw = u.DrawArea;
                toDraw.Offset(-zoomScreen.X, -zoomScreen.Y);
                spriteBatch.Draw(u.Texture, toDraw, Color.White);
                if (selected.Contains(u))
                {
                    OutlineRectangle(toDraw, Color.Blue,1);
                }
            }
            
            //spriteBatch.End();

            //spriteBatch.Begin();
            if (lastLeftClick.X != -1)
            {
                OutlineRectangle(selection, Color.Blue, 2);
            }
            //OutlineRectangle(scrollUp, Color.Green);
            //OutlineRectangle(scrollDown, Color.Green);
            //OutlineRectangle(scrollLeft, Color.Green);
            //OutlineRectangle(scrollRight, Color.Green);

            spriteBatch.End();

            base.Draw(gameTime);
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
    }
}
