using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace Atomic.Content.NonBossStuff.PlayerGetBuffs
{
    public class MushroomSideEffects : ModPlayer
    {
        // Efeito de regeneração ao ficar perto do cogumelo vermelho
        public override void PostUpdateEquips()
        {
            bool nearRedMushroom = false;
            Point playerTile = Player.Center.ToTileCoordinates();

            for (int i = playerTile.X - 5; i <= playerTile.X + 5; i++)
            {
                for (int j = playerTile.Y - 5; j <= playerTile.Y + 5; j++)
                {
                    if (!WorldGen.InWorld(i, j)) continue;
                    Tile tile = Main.tile[i, j];
                    if (tile.HasTile && tile.TileType == ModContent.TileType<Consumables.Edible.GiantRedMushroomTile>())
                    {
                        nearRedMushroom = true;
                        break;
                    }
                }
                if (nearRedMushroom) break;
            }

            if (nearRedMushroom)
            {
                Player.lifeRegen += 2;
                Player.lifeRegenTime += 30;
            }
        }

        // NOVO: Salto e som no cogumelo azul
        public override void PostUpdate()
        {
            // Verifica o bloco exato abaixo dos pés do jogador
            Point feetTile = Player.Bottom.ToTileCoordinates();
            if (!WorldGen.InWorld(feetTile.X, feetTile.Y + 1)) return;

            Tile tileBelow = Main.tile[feetTile.X, feetTile.Y + 1];
            if (tileBelow.HasTile && tileBelow.TileType == ModContent.TileType<Consumables.Edible.GiantBlueMushroomTile>())
            {
                // Se o jogador estiver no chão (sem velocidade vertical), aplica o pulo
                if (Player.velocity.Y == 0f)
                {
                    Player.velocity.Y = -12f; // Força do pulo (ajuste como quiser)
                    // Toca o som de quicar (Item56 é o som de "Minecart bounces")
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item56 with { Pitch = -0.2f }, Player.Center);

                    // Partículas de "pula-pula"
                    for (int i = 0; i < 10; i++)
                    {
                        Dust d = Dust.NewDustDirect(Player.Bottom, Player.width, 10, DustID.BlueTorch);
                        d.velocity = new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-3f, 0f));
                        d.noGravity = true;
                    }
                }
            }
        }
    }

    // ==========================================
    // BUFF DE REGENERAÇÃO (Cogumelo Vermelho)
    // ==========================================
    public class RedMushroomRegenBuff : ModBuff
    {
        public override string Texture => "Terraria/Images/Buff_" + BuffID.Regeneration;

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.lifeRegen += 4;
            player.lifeRegenTime += 60;
            if (Main.rand.NextBool(5))
            {
                Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.HealingPlus);
                d.noGravity = true;
                d.velocity *= 0.2f;
            }
        }
    }

    // ==========================================
    // BUFF DE VOO (Cogumelo Vermelho)
    // ==========================================
    public class RedMushroomFlightBuff : ModBuff
    {
        public override string Texture => "Terraria/Images/Buff_" + BuffID.Poisoned;

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.wingTimeMax = 100;
            player.wingTime = 100;
            player.armorEffectDrawShadow = true;
            if (Main.rand.NextBool(5))
            {
                Dust dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.RedTorch);
                dust.noGravity = true;
                dust.velocity *= 0.5f;
            }
        }
    }

    // ==========================================
    // BUFF DROGADO (Cogumelo Azul)
    // ==========================================
    public class DruggedBuff : ModBuff
    {
        public override string Texture => "Terraria/Images/Buff_" + BuffID.Poisoned;

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = false;
        }
    }

    // ==========================================
    // SISTEMA DE TELA: TONTURA
    // ==========================================
    public class DruggedScreenEffectSystem : ModSystem
    {
        public override void ModifyScreenPosition()
        {
            Player player = Main.LocalPlayer;
            if (player.active && !player.dead && player.HasBuff(ModContent.BuffType<DruggedBuff>()))
            {
                float time = Main.GlobalTimeWrappedHourly;
                Main.screenPosition.X += (float)Math.Sin(time * 6f) * 30f;
                Main.screenPosition.Y += (float)Math.Cos(time * 5f) * 30f;
            }
        }

        public override void ModifyTransformMatrix(ref SpriteViewMatrix Transform)
        {
            Player player = Main.LocalPlayer;
            if (player.active && !player.dead && player.HasBuff(ModContent.BuffType<DruggedBuff>()))
            {
                float time = Main.GlobalTimeWrappedHourly;
                float pulseZoom = 1f + 0.08f * (float)Math.Sin(time * 4f);
                Transform.Zoom = new Vector2(pulseZoom, pulseZoom);
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "Atomic: Drugged Overlay",
                    delegate
                    {
                        Player player = Main.LocalPlayer;
                        if (player.active && !player.dead && player.HasBuff(ModContent.BuffType<DruggedBuff>()))
                        {
                            float pulse = 0.25f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f);
                            Color overlayColor = new Color(120, 0, 150) * pulse;
                            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), overlayColor);
                        }
                        return true;
                    }, InterfaceScaleType.UI)
                );
            }
        }
    }
}