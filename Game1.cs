using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Text.Json;

namespace yur;

public class RectangleData
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
public class RectanglesFile
{
    public List<RectangleData> Rectangles { get; set; }
}

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    Texture2D picture;

    Texture2D backgroundTexture;
    Texture2D backgroundTexture2;
    Background background;
    Background background2;

    Texture2D patrollerTexture;
    // Lista för alla fiender
    private List<Enemy> enemies = new();

    private List<IGameObject> gameObjects = new();

    private List<Rectangle> platforms = new();
    private Texture2D pixel;
    private float gravity = 1200f;
    public int groundy;

    private bool DoubleJumpPrimed;

    Player player = new();

    private float worldwidth = 3000f;
    private Vector2 cameraPosition;

    public int playerdeathcount = 0;
    private SpriteFont font;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 1000;
        _graphics.PreferredBackBufferHeight = 700;
        _graphics.ApplyChanges();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        var viewport = GraphicsDevice.Viewport;
        pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.Azure });
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        picture = Content.Load<Texture2D>("mushroom sprite (2)");

        //bakgrundsgrejs
        backgroundTexture = Content.Load<Texture2D>("background1");
        backgroundTexture2 = Content.Load<Texture2D>("trees");
        background = new Background(backgroundTexture, 0.1f);
        background2 = new Background(backgroundTexture2, 0.3f);

        player.Position = new Vector2(
            100f,
            viewport.Height / 2f - picture.Height / 2f
        );

        groundy = viewport.Height - picture.Height;

        string json = System.IO.File.ReadAllText("data.json");
        RectanglesFile data = JsonSerializer.Deserialize<RectanglesFile>(json);

        if (data != null)
        {
            for (int i = 0; i < data.Rectangles.Count; i++)
            {
                RectangleData rect = data.Rectangles[i];
                platforms.Add(new Rectangle(rect.X, rect.Y, rect.Width, rect.Height));
            }
        }

        platforms.Add(new Rectangle(
                -5000,           
                groundy,        
                (int)worldwidth * 3,  
                100              
            ));

        cameraPosition = Vector2.Zero;

        patrollerTexture = Content.Load<Texture2D>("patroller");
        enemies.Add(EnemyFactory.CreateChaser(new Vector2(600, 400), patrollerTexture, player));
        enemies.Add(EnemyFactory.CreatePatroller(new Vector2(1000, 400), patrollerTexture, 900, 1100));
        font = Content.Load<SpriteFont>("MyFont");

        
        gameObjects.Add(new PowerUp(new Vector2(800, 400), PowerUpType.JumpBoost, 30, 10f));
        gameObjects.Add(new Teleporter(new Vector2(600, 400), new Vector2(1500, 300), 50, 50));
        gameObjects.Add(new PowerUp(new Vector2(900, 500), PowerUpType.DoubleJump, 50, 15f));
        gameObjects.Add(new PowerUp(new Vector2(300, 300), PowerUpType.Invincibility, 50, 10f));

        Random rand = new Random();
        bool validPos = false;
        while(!validPos)
        {
            int posX = rand.Next(0, 990); // lite under skärmens max
            int posY = rand.Next(0, 690);
            validPos = true;
            foreach(var rect in platforms)
            {
                if(posX == rect.X && posY == rect.Y)
                {
                    validPos = false; //fortsätter while loopen
                    break;
                }
            }

            if (validPos)
            {
                gameObjects.Add(new Teleporter(new Vector2(300, 500), new Vector2(posX, posY), 50, 50));
            }
        }

        }

    protected override void Update(GameTime gameTime)
{


    var viewport = GraphicsDevice.Viewport;
    var keyboard = Keyboard.GetState();
    if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
        Keyboard.GetState().IsKeyDown(Keys.Escape))
        Exit();

    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

    var playerposition = player.Position;
    var playervelocity = player.Velocity;

    //var previousPosition = playerposition;

    if (keyboard.IsKeyDown(Keys.A))
    {
        playerposition.X -= player.Speed * dt;
    }
    if (keyboard.IsKeyDown(Keys.D))
    {
        playerposition.X += player.Speed * dt;
    }

    
    if (keyboard.IsKeyDown(Keys.Space) && player.IsOnGround || keyboard.IsKeyDown(Keys.Space) && player.HasDoubleJump)
    {
        if(keyboard.IsKeyDown(Keys.Space) && !player.IsOnGround && player.HasDoubleJump && DoubleJumpPrimed == true)
        {
            DoubleJumpPrimed = false;
        }
        if(keyboard.IsKeyDown(Keys.Space) && player.IsOnGround)
        {
            DoubleJumpPrimed = true;
        }
        playervelocity.Y = player.JumpSpeed;
        player.IsOnGround = false;
    }
   

    float targetCamX = playerposition.X - viewport.Width / 2f;

    playervelocity.Y += gravity * dt;
    playerposition += playervelocity * dt;

    Rectangle playerHitbox = new Rectangle(
        (int)playerposition.X,
        (int)playerposition.Y,
        picture.Width,
        picture.Height
    );
    player.IsOnGround = false;

    foreach (var platform in platforms)
    {
        if (playerHitbox.Intersects(platform))
        {
            float feet = playerposition.Y + picture.Height;
            
            if (playervelocity.Y >= 0 && Math.Abs(feet - platform.Top) < 20)
            {
                playerposition.Y = platform.Top - picture.Height;
                playervelocity.Y = 0;
                player.IsOnGround = true;
            }
        }
    }

    player.Position = playerposition;
    player.Velocity = playervelocity;

    // spelarens hitbox
    playerHitbox = new Rectangle(
        (int)player.Position.X,
        (int)player.Position.Y,
        picture.Width,
        picture.Height
    );


    foreach (var obj in gameObjects)
    {
        obj.Update(dt, playerHitbox, ref player);
    }


    if (player.Position.Y >= groundy)
    {
        player.Position = new Vector2(player.Position.X, groundy);
        player.Velocity = new Vector2(player.Velocity.X, 0);
        player.IsOnGround = true;
    }


    player.Position = new Vector2(
        MathHelper.Clamp(player.Position.X, -worldwidth - picture.Width, worldwidth - picture.Width),
        player.Position.Y
    );

    cameraPosition.X = targetCamX;

    // Kör genom alla fiender i listan
    foreach (var enemy in enemies)
    {
        enemy.Update(dt, platforms, gravity, player);
    }

    // Tittar efter en collision och isf lägger till
    foreach (var enemy in enemies)
    {
        if (playerHitbox.Intersects(enemy.Hitbox) && !player.IsInvincible)
        {
            playerdeathcount += 1;
        }
    }

    foreach (var obj in gameObjects)
    {
        if(obj is PowerUp powerUp && powerUp.IsActive) //om obj (vilket är en IGameObject) är en PowerUp, vid namn powerUp och är aktiv.
        {
            powerUp.Duration -= dt;
            if(powerUp.Duration <= 0)
            {
                powerUp.IsActive = false;
            }
        }
    } //Listan gameobjects är en IGameObject men datan vi matar in är en PowerUp, så vi kollar om obj är en PowerUp och sparar den som powerUp för att komma åt Duration.

    player.Update(dt);

    base.Update(gameTime);
}

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();

        var playerScreenPosition = new Vector2(
            player.Position.X - cameraPosition.X,
            player.Position.Y - cameraPosition.Y
        );

        Vector2 backgroundPosition = new Vector2(-cameraPosition.X * background.Speed - 200, 0);
        Vector2 backgroundPosition_2 = new Vector2(-cameraPosition.X * background.Speed - 400, 0);
        Vector2 backgroundPosition2 = new Vector2(-cameraPosition.X * background2.Speed - 200, -400);

        _spriteBatch.Draw(backgroundTexture, backgroundPosition, null, Color.White, 0f, new Vector2(0, 0), 2f, SpriteEffects.None, 0f);
        _spriteBatch.Draw(backgroundTexture, backgroundPosition_2, null, Color.White, 0f, new Vector2(0, 0), 2f, SpriteEffects.None, 0f);
        _spriteBatch.Draw(backgroundTexture2, backgroundPosition2, null, Color.White, 0f, new Vector2(0, 0), 3f, SpriteEffects.None, 0f);
        _spriteBatch.Draw(picture, playerScreenPosition, null, Color.White, 0f, new Vector2(0, 0), 1f, SpriteEffects.None, 0f);

        foreach (var platform in platforms)
        {
            var screenRect = new Rectangle(
                (int)(platform.X - cameraPosition.X),
                platform.Y,
                platform.Width,
                platform.Height
            );
            _spriteBatch.Draw(pixel, screenRect, Color.Azure);
        }

        // gå genom alla fiender och rita 
        foreach (var enemy in enemies)
        {
            enemy.Draw(_spriteBatch, cameraPosition);
        }
        _spriteBatch.DrawString(font, "Player Deaths: " + playerdeathcount.ToString(), new Vector2(100, 80), Color.White);

        foreach (var obj in gameObjects)
        {
            obj.Draw(_spriteBatch, cameraPosition, pixel);
        }


        _spriteBatch.End();

        base.Draw(gameTime);
    }
}