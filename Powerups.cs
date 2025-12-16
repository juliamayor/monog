using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace yur;

public enum PowerUpType
{
    JumpBoost,
    SpeedBoost,
    DoubleJump,
    Invincibility
}

public class PowerUp : IGameObject
{
    public Vector2 Position { get; set; }
    public Rectangle Hitbox { get; set; }
    public bool IsActive { get; set; } = true;
    public PowerUpType Type { get; set; }
    public float Duration { get; set; } = 7f; // Sekunder
    
    private float _bobTimer = 0f;
    private float _bobOffset = 0f;
    private float _rotationTimer = 0f;
    
    public PowerUp(Vector2 position, PowerUpType type, int size = 30, float duration = 10f)
    {
        Position = position;
        Type = type;
        Duration = duration;
        Hitbox = new Rectangle(
            (int)position.X,
            (int)position.Y,
            size,
            size
        );
    }
    
    public void Update(float dt, Rectangle playerHitbox, ref Player player)
    {
        if (!IsActive) return;
        
        // Animation för att få snurra
        _bobTimer += dt * 2f;
        _bobOffset = (float)Math.Sin(_bobTimer) * 8f;
        _rotationTimer += dt;
        
        // Kolla kollision
        if (playerHitbox.Intersects(Hitbox))
        {
            ApplyPowerUp(ref player);
            IsActive = false;
        }
    }
    
    private void ApplyPowerUp(ref Player player)
    {
        switch (Type)
        {
            case PowerUpType.JumpBoost:
                player.AddPowerUp(PowerUpType.JumpBoost, Duration);
                player.JumpSpeed = -1000f; // Normallt är det -800f
                break;
                
            case PowerUpType.SpeedBoost:
                player.AddPowerUp(PowerUpType.SpeedBoost, Duration);
                player.Speed = 500f; // Normallt är det 350f
                break;
                
            case PowerUpType.DoubleJump:
                player.AddPowerUp(PowerUpType.DoubleJump, Duration);
                player.HasDoubleJump = true;
                break;
                
            case PowerUpType.Invincibility:
                player.AddPowerUp(PowerUpType.Invincibility, Duration);
                player.IsInvincible = true;
                break;
        }
    }
    
    public void Draw(SpriteBatch spriteBatch, Vector2 cameraPosition, Texture2D pixel)
    {
        if (!IsActive) return;
        
        var screenPos = new Vector2(
            Position.X - cameraPosition.X + Hitbox.Width / 2f,
            Position.Y - cameraPosition.Y + Hitbox.Height / 2f + _bobOffset
        );
        
        var color = Type switch
        {
            PowerUpType.JumpBoost => Color.Cyan,
            PowerUpType.SpeedBoost => Color.Yellow,
            PowerUpType.DoubleJump => Color.Purple,
            PowerUpType.Invincibility => Color.White,
            _ => Color.Green
        };
        
        // animation, roterande romb
        var rect = new Rectangle(0, 0, Hitbox.Width, Hitbox.Height);
        var origin = new Vector2(Hitbox.Width / 2f, Hitbox.Height / 2f);
        
        spriteBatch.Draw(
            pixel,
            screenPos,
            rect,
            color,
            _rotationTimer * 2f,
            origin,
            1f,
            SpriteEffects.None,
            0f
        );
    }
}