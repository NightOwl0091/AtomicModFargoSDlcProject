using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using Terraria.GameContent;

namespace Atomic.Assets.AnotherModAssets.Assets.Content.Projectiles.Masomode
{
    public class TargetingReticle : ModProjectile
    {
        // Caminho da textura ajustado para a sua pasta
        public override string Texture => "Atomic/Assets/AnotherModAssets/Assets/Content/Projectiles/Masomode/TargetingReticle";

        public override void SetDefaults()
        {
            Projectile.width = 110;
            Projectile.height = 110;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.penetrate = -1;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.timeLeft = 90;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override void AI()
        {
            // Substituição do FargoSoulsUtil.NPCExists por código nativo do Terraria
            int npcIndex = (int)Projectile.ai[0];
            NPC npc = null;

            if (npcIndex >= 0 && npcIndex < Main.maxNPCs)
            {
                NPC tempNpc = Main.npc[npcIndex];
                if (tempNpc.active && !tempNpc.friendly && tempNpc.life > 0)
                {
                    npc = tempNpc;
                }
            }

            // Verifica se o NPC e o jogador alvo existem
            if (npc != null && npc.target >= 0 && npc.target < Main.maxPlayers && Main.player[npc.target].active && !Main.player[npc.target].dead)
            {
                Projectile.alpha -= 4;
                if (Projectile.alpha < 0) //fade in
                    Projectile.alpha = 0;

                int modifier = Math.Min(60, 90 - Projectile.timeLeft);

                Projectile.scale = 4f - 3f / 60f * modifier; //start big, shrink down

                Projectile.Center = npc.Center;
                Projectile.velocity = Main.player[npc.target].Center - Projectile.Center;
                Projectile.velocity = Projectile.velocity / 60 * modifier; //move from npc to player
                Projectile.rotation = (float)Math.PI * 2 / 30 * modifier * Math.Sign(Projectile.velocity.X);
            }
            else
            {
                Projectile.Kill();
            }

            if (Projectile.timeLeft % 20 == 0)
            {
                if (!Main.dedServ)
                    SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/ReticleBeep"), Projectile.Center);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
                SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/ReticleLockOn"), Projectile.Center);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 128) * (1f - Projectile.alpha / 255f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture2D13 = TextureAssets.Projectile[Projectile.type].Value;
            int num156 = TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type];
            int y3 = num156 * Projectile.frame;
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;

            Main.EntitySpriteDraw(
                texture2D13,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                new Microsoft.Xna.Framework.Rectangle?(rectangle),
                Projectile.GetAlpha(lightColor),
                Projectile.rotation,
                origin2,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }
    }
}