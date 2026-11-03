using Atomic.Content.NonBossStuff.Consumables.Edible;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Atomic.Content.NonBossStuff.Systems.Sfxs
{
    public class SfxParticlesShroom : ModSystem
    {
        private int stepTimer;
        private int jumpTimer;

        public override void PostUpdateEverything()
        {
            Player player = Main.LocalPlayer;
            if (player.active && !player.dead)
            {
                // Verifica se o jogador está no chão e em cima de um cogumelo
                bool onRedShroom = false;
                bool onBlueShroom = false;
                Point feetTile = player.Bottom.ToTileCoordinates();

                if (WorldGen.InWorld(feetTile.X, feetTile.Y))
                {
                    Tile tile = Main.tile[feetTile.X, feetTile.Y];
                    if (tile.HasTile)
                    {
                        if (tile.TileType == ModContent.TileType<GiantRedMushroomTile>()) onRedShroom = true;
                        else if (tile.TileType == ModContent.TileType<GiantBlueMushroomTile>()) onBlueShroom = true;
                    }
                }

                // --- COGUMELO VERMELHO (Passos + Partículas de Cura) ---
                if (onRedShroom && player.velocity.Y == 0 && Math.Abs(player.velocity.X) > 0.1f)
                {
                    stepTimer++;
                    // Toca um som suave a cada 20 frames enquanto anda
                    if (stepTimer >= 20)
                    {
                        stepTimer = 0;
                        SoundEngine.PlaySound(SoundID.Item3 with { Volume = 0.3f, Pitch = 0.5f }, player.Center); // Substitua por som do Fargo se desejar
                    }

                    // Partículas de cura subindo
                    if (Main.rand.NextBool(3))
                    {
                        Dust d = Dust.NewDustDirect(player.Bottom, player.width, 10, DustID.HealingPlus);
                        d.velocity.Y = -1f;
                        d.noGravity = true;
                    }
                }
                else if (!onRedShroom) stepTimer = 0;

                // --- COGUMELO AZUL (Pulo + Partículas Psicodélicas) ---
                if (onBlueShroom && player.velocity.Y != 0 && player.justJumped)
                {
                    jumpTimer = 10; // Cooldown para não tocar o som toda hora que pular
                    SoundEngine.PlaySound(SoundID.Item168 with { Volume = 0.8f }, player.Center); // Som do Pula-Pula
                }

                if (jumpTimer > 0) jumpTimer--;

                // Partículas contínuas no cogumelo azul
                if (onBlueShroom && jumpTimer == 0 && Main.rand.NextBool(4))
                {
                    Dust d = Dust.NewDustDirect(player.Bottom, player.width, 10, DustID.BlueTorch);
                    d.velocity = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, 0f));
                    d.noGravity = true;
                }
            }
        }
    }
}