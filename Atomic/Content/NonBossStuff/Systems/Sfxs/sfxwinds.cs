using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Atomic.Content.NonBossStuff.Systems.Sfxs
{
    public static class MushroomSfx
    {
        private const float MaxSwayPixels = 7f;
        private const float WindInfluence = 1.4f;

        public static bool DrawSway(int i, int j, SpriteBatch spriteBatch, int type)
        {
            Tile tile = Main.tile[i, j];
            if (!tile.HasTile) return true;

            Texture2D tex = TextureAssets.Tile[type].Value;

            // Stride = Tamanho da coordenada (16) + Padding (2)
            int stride = 18;
            int row = Math.Clamp(tile.TileFrameY / stride, 0, 3); // 0 = topo ... 3 = base

            // Calcula o 'i' base (o bloco mais à esquerda do cogumelo 4x4) para balançar tudo junto
            int baseI = i - (tile.TileFrameX / stride);

            float wind = Main.windSpeedCurrent * WindInfluence;
            float gentle = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.6f + baseI * 0.4f) * 0.35f;
            float sway = wind + gentle;

            // 0 na base, 1 no topo; ao quadrado para curvar mais perto do chapéu
            float height = (3 - row) / 3f;
            float offsetX = sway * height * height * MaxSwayPixels;

            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange, Main.offScreenRange);
            Vector2 pos = new Vector2(i * 16, j * 16) - Main.screenPosition + zero + new Vector2(offsetX, 0f);

            Rectangle src = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16);
            spriteBatch.Draw(tex, pos, src, Lighting.GetColor(i, j), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            return false;
        }

        public static void SpawnSpores(int i, int j, int dustType)
        {
            if (!Main.rand.NextBool(15)) return; // Chance de spawnar

            // Spawna no topo do cogumelo
            Vector2 pos = new Vector2(i * 16 + Main.rand.Next(8, 24), j * 16 + Main.rand.Next(0, 16));
            Vector2 vel = new Vector2(Main.windSpeedCurrent * 3.5f + Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(-0.7f, -0.1f));

            Dust d = Dust.NewDustPerfect(pos, dustType, vel, 120, default, Main.rand.NextFloat(0.8f, 1.3f));
            d.noGravity = true;
            d.fadeIn = 1.1f;
        }
    }
}