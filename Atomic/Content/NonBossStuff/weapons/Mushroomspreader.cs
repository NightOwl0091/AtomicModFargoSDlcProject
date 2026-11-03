using Atomic.Content.NonBossStuff.Systems.Biomes.Atomic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace Atomic.Content.NonBossStuff.weapons
{
    public class MushroomSpreader : ModItem
    {
        public override string Texture => "Atomic/Assets/Sprites/Vanilla Addons/Weapons/MushroomSpreader";
        public override LocalizedText DisplayName => Language.GetOrRegister("Mods.Atomic.Items.MushroomSpreader.DisplayName", () => "Espalhador de Cogumelo");

        private int insultTimer;

        private readonly string[] insults = {
            "Você não tem uma arma melhorzinha não?",
            "Estou desperdiçando meu poder na sua mão...",
            "Puxa o gatilho de uma vez, covarde.",
            "Eu já purifiquei mundos inteiros, e você me usa pra matar esquilos?",
            "Aquele Mutante teria coragem de atirar.",
            "Mais radiação? Que tédio...",
            "Se não vai atirar, me devolve pro baú.",
            "Você está me encarando? Que nojento.",
            "Minha paciência tem limites, humano.",
            "Você é péssimo com armas de fogo, sabia?",
            "Desiste logo, seu inútil.",
            "Meu deus, quanto tempo você vai levar?",
            "Eu já vi slimes mais intimidadores que você.",
            "Para de tremer e atira logo!",
            "Se você morrer, eu vou pro próximo. Não sinto nada por você.",
            "Você acha que isso é uma estratégia? É patético."
        };

        private readonly string[] soundPaths = {
            "Atomic/Assets/Music/SFX/gunsound-1",
            "Atomic/Assets/Music/SFX/gunsound-2",
            "Atomic/Assets/Music/SFX/gunsound-3"
        };

        public override void SetDefaults()
        {
            Item.width = 30; Item.height = 30;
            Item.DamageType = DamageClass.Ranged;
            Item.damage = 110;
            Item.knockBack = 1.5f;
            Item.crit = 4;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 9; Item.useAnimation = 9;
            Item.useAmmo = ItemID.GlowingMushroom;
            Item.shoot = ModContent.ProjectileType<MushroomSpreaderProjectile>();
            Item.shootSpeed = 11f;
            Item.noMelee = true;
            Item.value = Item.buyPrice(gold: 50);
            Item.rare = ItemRarityID.Red;
            Item.UseSound = SoundID.Item34;
            Item.autoReuse = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine line = new TooltipLine(Mod, "MushroomSpreaderTooltip", "Ela pode cansar de você e ir embora, mas ela sempre volta. Purifica radiação.");
            line.OverrideColor = new Color(150, 255, 150);
            tooltips.Add(line);
        }

        public override Vector2? HoldoutOffset() => new Vector2(-4f, 0f);

        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<MushroomBoomerang>()] == 0 &&
                   player.ownedProjectileCounts[ModContent.ProjectileType<MushroomChargingProjectile>()] == 0;
        }

        public override bool CanConsumeAmmo(Item ammo, Player player) => true;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            MushroomPlayer mp = player.GetModPlayer<MushroomPlayer>();
            mp.shots++;

            if (mp.shots >= MushroomPlayer.ShotsToThrow)
            {
                mp.shots = 0;
                Projectile.NewProjectile(source, player.Center, Vector2.Zero, ModContent.ProjectileType<MushroomChargingProjectile>(), damage, 6f, player.whoAmI);
                SoundEngine.PlaySound(SoundID.Item1, player.Center);
                return false;
            }

            Vector2 muzzle = position + velocity.SafeNormalize(Vector2.UnitX * player.direction) * 40f;
            if (Collision.CanHit(position, 0, 0, muzzle, 0, 0)) position = muzzle;

            Vector2 vel = velocity.RotatedByRandom(MathHelper.ToRadians(5f)) * Main.rand.NextFloat(0.85f, 1.1f);
            Projectile.NewProjectile(source, position, vel, type, damage, knockback, player.whoAmI);

            insultTimer = Main.rand.Next(240, 480);

            return false;
        }

        public override void UpdateInventory(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return;

            if (player.HeldItem.type == Type)
            {
                if (player.itemAnimation == 0)
                {
                    insultTimer--;

                    if (insultTimer <= 0)
                    {
                        string msg = insults[Main.rand.Next(insults.Length)];
                        string soundPath = soundPaths[Main.rand.Next(soundPaths.Length)];

                        CombatText.NewText(player.getRect(), new Color(150, 255, 150), msg, true);
                        SoundEngine.PlaySound(new SoundStyle(soundPath) with { Volume = 0.8f, PitchVariance = 0.1f }, player.Center);

                        insultTimer = Main.rand.Next(240, 480);
                    }
                }
            }
            else
            {
                insultTimer = Main.rand.Next(240, 480);
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Autohammer, 1);
            recipe.AddIngredient(ItemID.ShroomiteBar, 100);
            recipe.AddIngredient(ItemID.GlowingMushroom, 900);
            recipe.AddIngredient(ItemID.StrangeGlowingMushroom, 50);
            recipe.AddIngredient(ItemID.Mushroom, 20);
            recipe.AddIngredient(ItemID.LunarBar, 100);
            if (ModLoader.TryGetMod("Fargowiltas", out Mod fargoMutant))
            {
                string[] truffleNames = { "Truffle", "CapturedTruffle", "TruffleCaptured" };
                foreach (string name in truffleNames)
                {
                    if (fargoMutant.TryFind(name, out ModItem capturedTruffle))
                    {
                        recipe.AddIngredient(capturedTruffle.Type, 1);
                        break;
                    }
                }
            }
            recipe.AddTile(TileID.LunarCraftingStation);
            recipe.Register();
        }
    }

    public class MushroomChargingProjectile : ModProjectile
    {
        public override string Texture => "Atomic/Assets/Sprites/Vanilla Addons/Weapons/MushroomSpreader";

        public override void SetDefaults()
        {
            Projectile.width = 40; Projectile.height = 40;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead) { Projectile.Kill(); return; }

            Projectile.Center = owner.Center + new Vector2(0, -40);
            Projectile.rotation += 0.3f;
            Projectile.velocity = Vector2.Zero;

            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GlowingMushroom, Main.rand.NextVector2Circular(2f, 2f), 0, default, 1.5f);
                d.noGravity = true;
            }

            if (Projectile.timeLeft == 1)
            {
                Vector2 dir = (Main.MouseWorld - owner.Center).SafeNormalize(Vector2.UnitX * owner.direction);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.Center, dir * 15f, ModContent.ProjectileType<MushroomBoomerang>(), Projectile.damage, 6f, Projectile.owner);
            }
        }
    }

    public class MushroomBoomerang : ModProjectile
    {
        public override string Texture => "Atomic/Assets/Sprites/Vanilla Addons/Weapons/MushroomSpreader";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 15;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40; Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.ai[0] = 0f;
            Projectile.localAI[0] = 0f;
        }

        public override bool? CanCutTiles() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawOrigin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
            for (int k = 0; k < Projectile.oldPos.Length; k++)
            {
                Vector2 drawPos = (Projectile.oldPos[k] - Main.screenPosition) + drawOrigin + new Vector2(0f, Projectile.gfxOffY);
                Color color = Projectile.GetAlpha(lightColor) * ((float)(Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length);
                Main.EntitySpriteDraw(texture, drawPos, null, color, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0);
            }
            return true;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead) { Projectile.Kill(); return; }

            Projectile.rotation += 0.5f;

            if (Projectile.ai[0] == 0f)
            {
                Projectile.localAI[0]++;
                NPC target = null;
                float maxDetectDistance = 1500f;
                float sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;

                for (int k = 0; k < Main.maxNPCs; k++)
                {
                    NPC npc = Main.npc[k];
                    if (npc.active && npc.boss && npc.CanBeChasedBy(this))
                    {
                        float sqrDistanceToTarget = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                        if (sqrDistanceToTarget < sqrMaxDetectDistance)
                        {
                            sqrMaxDetectDistance = sqrDistanceToTarget;
                            target = npc;
                        }
                    }
                }

                if (target == null)
                {
                    for (int k = 0; k < Main.maxNPCs; k++)
                    {
                        NPC npc = Main.npc[k];
                        if (npc.CanBeChasedBy(this))
                        {
                            float sqrDistanceToTarget = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                            if (sqrDistanceToTarget < sqrMaxDetectDistance)
                            {
                                sqrMaxDetectDistance = sqrDistanceToTarget;
                                target = npc;
                            }
                        }
                    }
                }

                if (target != null)
                {
                    Vector2 direction = target.Center - Projectile.Center;
                    direction.Normalize();
                    direction *= 16f;
                    Projectile.velocity = (Projectile.velocity * 10f + direction) / 11f;
                }
                else
                {
                    Projectile.velocity *= 0.97f;
                }

                if (Projectile.localAI[0] > 60 || Projectile.velocity.Length() < 2f)
                {
                    Projectile.ai[0] = 1f;
                    Projectile.netUpdate = true;
                }
            }
            else
            {
                Vector2 direction = owner.Center - Projectile.Center;
                direction.Normalize();
                direction *= 18f;
                Projectile.velocity = (Projectile.velocity * 5f + direction) / 6f;

                if (Vector2.Distance(Projectile.Center, owner.Center) < 40f)
                {
                    Projectile.Kill();
                }
            }

            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GlowingMushroom, Projectile.velocity * 0.2f, 0, default, 1.5f);
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Vector2.Distance(Projectile.Center, Main.player[Projectile.owner].Center) > 100f)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<SupernovaBoomerangBlastProjectile>(), Projectile.damage * 8, 10f, Projectile.owner);
            }
        }
    }

    public class MushroomSpreaderProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SlimeGun;
        private const int Lifetime = 45;

        public static readonly int[] Debuffs =
        {
            BuffID.Poisoned, BuffID.OnFire, BuffID.OnFire3, BuffID.Frostburn, BuffID.Frostburn2,
            BuffID.CursedInferno, BuffID.ShadowFlame, BuffID.Venom, BuffID.Ichor, BuffID.Confused,
            BuffID.Slow, BuffID.Midas, BuffID.BetsysCurse, BuffID.Oiled, BuffID.Electrified, BuffID.Daybreak
        };

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20; Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 3;
            Projectile.timeLeft = Lifetime;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override bool? CanCutTiles() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawOrigin = new Vector2(texture.Width * 0.5f, Projectile.height * 0.5f);
            for (int k = 0; k < Projectile.oldPos.Length; k++)
            {
                Vector2 drawPos = (Projectile.oldPos[k] - Main.screenPosition) + drawOrigin + new Vector2(0f, Projectile.gfxOffY);
                Color color = Projectile.GetAlpha(lightColor) * ((float)(Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length);
                Main.EntitySpriteDraw(texture, drawPos, null, color, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0);
            }
            return true;
        }

        public override void AI()
        {
            float progress = 1f - Projectile.timeLeft / (float)Lifetime;
            Projectile.velocity *= 0.975f;
            int size = (int)(20 + 30 * progress);
            Projectile.Resize(size, size);

            float maxDetectDistance = 800f;
            NPC closestTarget = null;
            float sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;

            for (int k = 0; k < Main.maxNPCs; k++)
            {
                NPC target = Main.npc[k];
                if (target.CanBeChasedBy(this))
                {
                    float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);
                    if (sqrDistanceToTarget < sqrMaxDetectDistance)
                    {
                        sqrMaxDetectDistance = sqrDistanceToTarget;
                        closestTarget = target;
                    }
                }
            }

            if (closestTarget != null)
            {
                Vector2 direction = closestTarget.Center - Projectile.Center;
                direction.Normalize();
                direction *= 8f;
                Projectile.velocity = (Projectile.velocity * 20f + direction) / 21f;
            }

            if (progress > 0.05f)
            {
                for (int i = 0; i < 3; i++)
                {
                    int dustType = Main.rand.NextBool(3) ? DustID.GlowingMushroom : (Main.rand.NextBool(2) ? DustID.BlueTorch : DustID.RedTorch);
                    Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dustType, Projectile.velocity.X * 0.3f, Projectile.velocity.Y * 0.3f, 100, default, 1f + progress * 1.6f);
                    d.noGravity = true;
                    d.velocity *= 0.6f;
                }
            }
            Lighting.AddLight(Projectile.Center, 0.15f, 0.25f, 0.7f * (1f - progress) + 0.2f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            foreach (int buff in Debuffs)
            {
                target.buffImmune[buff] = false;
                target.AddBuff(buff, 60 * 10);
            }

            ApplyFargoDebuffs(target, 120);

            if (ModLoader.TryGetMod("FargowiltasSouls", out Mod fargoMod))
            {
                if (fargoMod.TryFind("MutantPresence", out ModBuff fargoBuff)) target.AddBuff(fargoBuff.Type, 180);
            }
        }

        public static void ApplyFargoDebuffs(NPC target, int duration)
        {
            if (ModLoader.TryGetMod("FargowiltasSouls", out Mod fargoMod))
            {
                string[] debuffNames = {
                    "MutantFangBuff",
                    "LethargicBuff",
                    "ClippedWingsBuff",
                    "MutantNibbleBuff"
                };

                foreach (string debuffName in debuffNames)
                {
                    if (fargoMod.TryFind<ModBuff>(debuffName, out ModBuff fargoBuff))
                    {
                        target.AddBuff(fargoBuff.Type, duration);
                    }
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => true;

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int radius = 20;
                int centerX = (int)(Projectile.Center.X / 16f);
                int centerY = (int)(Projectile.Center.Y / 16f);

                if (WorldGen.InWorld(centerX, centerY, radius + 10))
                {
                    bool changed = false;
                    for (int x = centerX - radius; x <= centerX + radius; x++)
                    {
                        for (int y = centerY - radius; y <= centerY + radius; y++)
                        {
                            if (!WorldGen.InWorld(x, y)) continue;
                            Tile tile = Main.tile[x, y];
                            if (!tile.HasTile) continue;

                            if (tile.TileType == ModContent.TileType<AtomicFloorGrass>())
                            {
                                WorldGen.KillTile(x, y, false, false, true);
                                changed = true;
                                continue;
                            }

                            int newType = GetPurifiedEquivalent(tile.TileType);
                            if (newType != -1)
                            {
                                tile.TileType = (ushort)newType;
                                WorldGen.SquareTileFrame(x, y, true);
                                changed = true;
                            }
                        }
                    }
                    if (changed && Main.netMode == NetmodeID.Server)
                        NetMessage.SendTileSquare(-1, centerX, centerY, radius * 2 + 1);
                }
            }
            for (int i = 0; i < 5; i++)
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GlowingMushroom, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 100, default, 1.5f);
        }

        private int GetPurifiedEquivalent(ushort originalType)
        {
            if (originalType == ModContent.TileType<NuclearTile>()) return TileID.Tungsten;
            if (originalType == ModContent.TileType<AtomicStone>()) return TileID.Stone;
            if (originalType == ModContent.TileType<AtomicDirtTile>()) return TileID.Dirt;
            if (originalType == ModContent.TileType<AtomicSand>()) return TileID.Sand;
            if (originalType == ModContent.TileType<AtomicVines>()) return TileID.Vines;
            return -1;
        }
    }

    public class MushroomPlayer : ModPlayer
    {
        public const int ShotsToThrow = 75;
        public int shots;
        private int idle;
        public override void PostUpdate()
        {
            if (Player.itemAnimation > 0) idle = 0;
            else if (++idle > 15) shots = 0;
        }
    }

    public class SupernovaBoomerangBlastProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SlimeGun;

        private static readonly SoundStyle ExplosionSound = new SoundStyle("Atomic/Assets/AnotherModAssets/Assets/Sounds/Weapons/DiffractorEnd") with { Volume = 1f, Pitch = 0f };

        private static Asset<Texture2D> bloomAsset;
        private static Asset<Texture2D> ringAsset;
        private static Asset<Texture2D> smokeAsset;
        private static Asset<Texture2D> blobGlowAsset;
        private static Asset<Texture2D> bloomFlareAsset;
        private static Asset<Texture2D> scorchAsset;
        private static Asset<Texture2D> bloomParticleAsset;

        public override void Load()
        {
            bloomAsset = ModContent.Request<Texture2D>("Atomic/Assets/AnotherModAssets/Assets/ExtraTextures/AdditiveTextures/Bloom");
            ringAsset = ModContent.Request<Texture2D>("Atomic/Assets/AnotherModAssets/Assets/ExtraTextures/AdditiveTextures/HardEdgeRing");
            smokeAsset = ModContent.Request<Texture2D>("Atomic/Assets/AnotherModAssets/Assets/ExtraTextures/AdditiveTextures/Smoke");
            blobGlowAsset = ModContent.Request<Texture2D>("Atomic/Assets/AnotherModAssets/Assets/ExtraTextures/AdditiveTextures/BlobGlow");
            bloomFlareAsset = ModContent.Request<Texture2D>("Atomic/Assets/AnotherModAssets/Assets/ExtraTextures/AdditiveTextures/BloomFlare");
            scorchAsset = ModContent.Request<Texture2D>("Atomic/Assets/AnotherModAssets/Assets/ExtraTextures/AdditiveTextures/Scorch");
            bloomParticleAsset = ModContent.Request<Texture2D>("Atomic/Assets/AnotherModAssets/Assets/ExtraTextures/AdditiveTextures/BloomParticle");
        }

        public override void SetDefaults()
        {
            Projectile.width = 1200; Projectile.height = 1200;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanCutTiles() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            if (bloomAsset == null || ringAsset == null || smokeAsset == null ||
                blobGlowAsset == null || bloomFlareAsset == null || scorchAsset == null || bloomParticleAsset == null) return false;

            Texture2D bloom = bloomAsset.Value;
            Texture2D ring = ringAsset.Value;
            Texture2D smoke = smokeAsset.Value;
            Texture2D blobGlow = blobGlowAsset.Value;
            Texture2D bloomFlare = bloomFlareAsset.Value;
            Texture2D scorch = scorchAsset.Value;
            Texture2D bloomParticle = bloomParticleAsset.Value;

            float progress = 1f - (Projectile.timeLeft / 120f);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            float bloomScale = 0.5f + progress * 2.5f;
            Main.spriteBatch.Draw(bloom, drawPos, null, Color.White * (1f - progress), 0f, bloom.Size() / 2f, bloomScale, SpriteEffects.None, 0f);

            float ringScale = progress * 5f;
            Main.spriteBatch.Draw(ring, drawPos, null, Color.Cyan * (1f - progress), 0f, ring.Size() / 2f, ringScale, SpriteEffects.None, 0f);

            Main.spriteBatch.Draw(smoke, drawPos, null, Color.Gray * 0.4f * (1f - progress), 0f, smoke.Size() / 2f, 4f, SpriteEffects.None, 0f);

            Main.spriteBatch.Draw(blobGlow, drawPos, null, Color.LightBlue * 0.5f * (1f - progress), 0f, blobGlow.Size() / 2f, 3f, SpriteEffects.None, 0f);

            Main.spriteBatch.Draw(bloomFlare, drawPos, null, Color.Orange * 0.8f * (1f - progress), 0f, bloomFlare.Size() / 2f, 2f + progress * 2f, SpriteEffects.None, 0f);

            for (int i = 0; i < 10; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(100f, 100f);
                float scale = Main.rand.NextFloat(0.5f, 1.5f);
                Main.spriteBatch.Draw(scorch, drawPos + offset, null, Color.OrangeRed * 0.6f * (1f - progress), 0f, scorch.Size() / 2f, scale, SpriteEffects.None, 0f);
            }

            for (int i = 0; i < 15; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(150f, 150f);
                float scale = Main.rand.NextFloat(0.3f, 0.8f);
                Main.spriteBatch.Draw(bloomParticle, drawPos + offset, null, Color.White * 0.4f * (1f - progress), 0f, bloomParticle.Size() / 2f, scale, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        public override void OnSpawn(IEntitySource source)
        {
            PunchCameraModifier modifier = new PunchCameraModifier(Projectile.Center, (Main.rand.NextFloat() * ((float)Math.PI * 2f)).ToRotationVector2(), 40f, 20f, 60, 2000f, "AtomicSupernova");
            Main.instance.CameraModifiers.Add(modifier);

            SoundEngine.PlaySound(ExplosionSound, Projectile.Center);
            ModContent.GetInstance<SupernovaScreenEffect>().TriggerFlash();

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 10; i++)
                {
                    Vector2 spawnPos = Projectile.Center + Main.rand.NextVector2Circular(300f, 300f);
                    int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<WofBlastParticle>(), 0, 0f, Projectile.owner);
                    Main.projectile[p].scale = Main.rand.NextFloat(1.5f, 3f);
                }
            }

            for (int i = 0; i < 400; i++)
            {
                float angle = MathHelper.TwoPi * i / 400;
                Vector2 speed = angle.ToRotationVector2() * Main.rand.NextFloat(15f, 50f);
                int dustType = Main.rand.NextBool(3) ? DustID.GlowingMushroom : (Main.rand.NextBool(2) ? DustID.BlueTorch : DustID.RedTorch);
                Dust d = Dust.NewDustPerfect(Projectile.Center, dustType, speed, 0, default, Main.rand.NextFloat(2f, 5f));
                d.noGravity = true; d.fadeIn = 1.2f;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (ModLoader.TryGetMod("FargowiltasSouls", out Mod fargoMod))
                {
                    string[] fargoProjectileNames = { "MutantDeathray2", "MutantGuardian", "MutantMark1", "MutantReticle2" };

                    for (int i = 0; i < 6; i++)
                    {
                        string projName = fargoProjectileNames[Main.rand.Next(fargoProjectileNames.Length)];

                        if (fargoMod.TryFind<ModProjectile>(projName, out ModProjectile fargoProj))
                        {
                            Vector2 spawnPos = Projectile.Center + Main.rand.NextVector2Circular(250f, 250f);
                            Vector2 velocity = Main.rand.NextVector2CircularEdge(12f, 12f);

                            int pIndex = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPos, velocity, fargoProj.Type, Projectile.damage / 2, 10f, Projectile.owner);

                            Main.projectile[pIndex].friendly = true;
                            Main.projectile[pIndex].hostile = false;
                            Main.projectile[pIndex].timeLeft = 60;
                        }
                    }
                }
            }
        }

        public override void AI()
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(Projectile.width / 2f, Projectile.height / 2f);
                Vector2 vel = (Projectile.Center - (Projectile.Center + offset)) * 0.05f;
                Dust d = Dust.NewDustPerfect(Projectile.Center + offset, DustID.GlowingMushroom, vel, 0, default, 2.5f);
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            foreach (int buff in MushroomSpreaderProjectile.Debuffs) { target.buffImmune[buff] = false; target.AddBuff(buff, 60 * 10); }

            MushroomSpreaderProjectile.ApplyFargoDebuffs(target, 180);

            if (ModLoader.TryGetMod("FargowiltasSouls", out Mod fargoMod)) { if (fargoMod.TryFind("MutantPresence", out ModBuff fargoBuff)) target.AddBuff(fargoBuff.Type, 180); }
        }
    }

    public class SupernovaScreenEffect : ModSystem
    {
        private static int FlashTimer = 0;
        private const int MaxFlashTime = 60;

        public void TriggerFlash()
        {
            FlashTimer = MaxFlashTime;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (FlashTimer > 0)
            {
                FlashTimer--;
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "Atomic: Supernova Screen Flash",
                    delegate
                    {
                        if (FlashTimer > 0)
                        {
                            float progress = 1f - ((float)FlashTimer / MaxFlashTime);
                            float alpha = 1f - progress;

                            Main.spriteBatch.End();
                            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

                            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Cyan * alpha);

                            Texture2D bloom = ModContent.Request<Texture2D>("Atomic/Assets/AnotherModAssets/Assets/ExtraTextures/AdditiveTextures/Bloom").Value;
                            Vector2 screenCenter = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);

                            float scale = MathHelper.Lerp(0.1f, 5f, progress);

                            Main.spriteBatch.Draw(bloom, screenCenter, null, Color.White * alpha, 0f, bloom.Size() / 2f, scale, SpriteEffects.None, 0f);

                            Main.spriteBatch.End();
                            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }

    public class GlowingMushroomAmmoGlobalItem : GlobalItem
    {
        public override void SetDefaults(Item entity)
        {
            if (entity.type == ItemID.GlowingMushroom)
                entity.ammo = ItemID.GlowingMushroom;
        }

        public override void PickAmmo(Item weapon, Item ammo, Player player, ref int type, ref float speed, ref StatModifier damage, ref float knockback)
        {
            if (weapon.type == ModContent.ItemType<MushroomSpreader>() && ammo.type == ItemID.GlowingMushroom)
                type = ModContent.ProjectileType<MushroomSpreaderProjectile>();
        }
    }

    public class FargoProjectileSafety : GlobalProjectile
    {
        public override bool CanHitPlayer(Projectile projectile, Player target)
        {
            string[] fargoProjectileNames = { "MutantDeathray2", "MutantGuardian", "MutantMark1", "MutantReticle2" };

            foreach (string name in fargoProjectileNames)
            {
                if (projectile.ModProjectile != null && projectile.ModProjectile.Name == name)
                {
                    return false;
                }
            }
            return true;
        }
    }
}