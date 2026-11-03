using Atomic.Content.NonBossStuff.Systems.Biomes.Atomic; // Caminho corrigido para o novo Sistema
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
using Terraria.WorldBuilding;

// Namespace corrigido para bater com a pasta: Content/NonBossStuff/Consumables/Consumable
namespace Atomic.Content.NonBossStuff.Consumables.Consumable
{
    public class Fatman : ModItem
    {
        public override string Texture => "Atomic/Assets/Sprites/Vanilla Addons/UsableConsumables/Fatman";

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 40;
            Item.useAnimation = 40;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.consumable = true;
            Item.maxStack = 9999;
            Item.DamageType = DamageClass.Default;
            Item.damage = 900;
            Item.knockBack = 8f;
            Item.rare = ItemRarityID.Purple;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<FatmanProj>();
            Item.shootSpeed = 0f;
            Item.value = Item.buyPrice(gold: 50);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            // CORREÇÃO EXTRA: Evita que o jogo trave (crash) se o mod Fargowiltas não estiver instalado
            if (ModContent.TryFind<ModItem>("Fargowiltas", "CityBuster", out ModItem cityBuster))
            {
                recipe.AddIngredient(cityBuster.Type, 35);
            }
            else
            {
                // Se não tiver Fargos, usa um item substituto para a receita não quebrar (ex: 35 Dynamites)
                recipe.AddIngredient(ItemID.Dynamite, 35);
            }

            recipe.AddIngredient(ItemID.FragmentSolar, 50);
            recipe.AddIngredient(ItemID.FragmentVortex, 50);
            recipe.AddIngredient(ItemID.FragmentNebula, 50);
            recipe.AddIngredient(ItemID.FragmentStardust, 50);
            recipe.AddTile(TileID.LunarCraftingStation);
            recipe.Register();
        }
    }

    public class FatmanProj : ModProjectile
    {
        public override string Texture => "Atomic/Assets/Sprites/Vanilla Addons/UsableConsumables/Fatman";

        private const float BaseSpeed = 4.2f;
        private const float MaxSpeed = 18f;
        private const float Acceleration = 0.15f;

        private int _timer;
        private int _playerChaseTimer;
        private int _dropTimer;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Default;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.damage = 900;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(IEntitySource source)
        {
            _timer = 0;
            _playerChaseTimer = 0;
            _dropTimer = 0;
            Projectile.velocity = new Vector2(0, -3f);
            Projectile.rotation = 0f;
        }

        public override void AI()
        {
            _timer++;

            if (_timer < 90)
            {
                Projectile.velocity = new Vector2(0, -3f);
                Projectile.rotation = 0f;
                SpawnParticles();
                Lighting.AddLight(Projectile.Center, 2f, 2f, 2f);

                if (_timer % 12 == 0 && Vector2.Distance(Projectile.Center, Main.LocalPlayer.Center) < 1500f)
                {
                    SoundEngine.PlaySound(SoundID.Item13.WithVolumeScale(0.7f).WithPitchOffset(-0.4f), Projectile.Center);
                }
                return;
            }

            if (_timer == 90)
            {
                ModContent.GetInstance<FatmanScreenFlash>().StartFlash();
            }

            if (_timer < 210)
            {
                Projectile.velocity = new Vector2(0, -3f);
                Projectile.rotation = 0f;
                SpawnParticles();
                Lighting.AddLight(Projectile.Center, 3f, 3f, 3f);
                return;
            }

            if (Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
            {
                Projectile.Kill();
                return;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.active && !p.dead && Projectile.Hitbox.Intersects(p.Hitbox))
                {
                    Projectile.Kill();
                    return;
                }
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n.active && !n.dontTakeDamage && !n.immortal &&
                   (Projectile.Hitbox.Intersects(n.Hitbox) || Vector2.Distance(Projectile.Center, n.Center) < (Projectile.width / 2f) + (n.width / 2f)))
                {
                    Projectile.Kill();
                    return;
                }
            }

            _dropTimer++;
            if (_dropTimer >= 10)
            {
                _dropTimer = 0;

                int[] explosiveTypes = new int[]
                {
                    ProjectileID.Grenade, ProjectileID.StickyGrenade, ProjectileID.BouncyGrenade,
                    ProjectileID.Beenade, ProjectileID.Bomb, ProjectileID.StickyBomb,
                    ProjectileID.BouncyBomb, ProjectileID.Dynamite, ProjectileID.StickyDynamite,
                    ProjectileID.BouncyDynamite, ProjectileID.BombFish
                };

                int chosenProjectile = explosiveTypes[Main.rand.Next(explosiveTypes.Length)];

                int bomb = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), Projectile.Center,
                    new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(2f, 6f)),
                    chosenProjectile, 300, 8f, Projectile.owner
                );

                Main.projectile[bomb].friendly = true;
                Main.projectile[bomb].hostile = false;
                Main.projectile[bomb].timeLeft = 180;
            }

            if (_timer % 15 == 0 && Vector2.Distance(Projectile.Center, Main.LocalPlayer.Center) < 1500f)
            {
                SoundStyle fargoHover = SoundID.Item14;
                if (ModLoader.TryGetMod("FargowiltasSouls", out Mod fargoMod))
                {
                    string soundPath = "FargowiltasSouls/Assets/Sounds/Explosion";
                    if (ModContent.HasAsset(soundPath))
                    {
                        fargoHover = new SoundStyle(soundPath).WithVolumeScale(0.4f).WithPitchOffset(0.5f);
                    }
                }
                SoundEngine.PlaySound(fargoHover, Projectile.Center);
            }

            Entity target = FindClosestLifeform(3000f);
            bool isOnScreen = Projectile.Hitbox.Intersects(new Rectangle((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth, Main.screenHeight));

            float guidedSpeed = BaseSpeed;

            if (!isOnScreen)
            {
                guidedSpeed = 1f;
            }
            else if (target is Player)
            {
                _playerChaseTimer++;
                if (_playerChaseTimer > 120)
                {
                    guidedSpeed = MathHelper.Clamp(BaseSpeed + (_playerChaseTimer - 120) * Acceleration, BaseSpeed, MaxSpeed);
                }
            }
            else
            {
                _playerChaseTimer = 0;
                guidedSpeed = BaseSpeed;
            }

            if (target != null)
            {
                Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction * guidedSpeed, 0.05f);
                Projectile.rotation = (target.Center - Projectile.Center).ToRotation() - MathHelper.PiOver2;
            }
            else
            {
                if (Projectile.velocity.Length() < guidedSpeed)
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * guidedSpeed;

                Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            }

            Lighting.AddLight(Projectile.Center, 3f, 3f, 3f);
        }

        private void SpawnParticles()
        {
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.GreenTorch);
                dust.velocity = Projectile.velocity * 0.5f;
                dust.scale = 1.8f;
                dust.noGravity = true;
            }
            if (Main.rand.NextBool(3))
            {
                Dust fire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Frost);
                fire.velocity = Projectile.velocity * 0.2f;
                fire.scale = 2f;
                fire.noGravity = true;
            }

            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(8f, 8f);
                Dust glow = Dust.NewDustPerfect(Projectile.Center + offset, DustID.WhiteTorch, Vector2.Zero, 0, Color.White, 2f);
                glow.noGravity = true;
            }
        }

        private Entity FindClosestLifeform(float maxDetectDistance)
        {
            Entity closestTarget = null;
            float sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;

            foreach (var target in Main.ActiveNPCs)
            {
                if (!target.dontTakeDamage && !target.immortal)
                {
                    float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);
                    if (sqrDistanceToTarget < sqrMaxDetectDistance)
                    {
                        sqrMaxDetectDistance = sqrDistanceToTarget;
                        closestTarget = target;
                    }
                }
            }

            foreach (var player in Main.ActivePlayers)
            {
                if (player.active && !player.dead && !player.ghost)
                {
                    float sqrDistanceToTarget = Vector2.DistanceSquared(player.Center, Projectile.Center);
                    if (sqrDistanceToTarget < sqrMaxDetectDistance)
                    {
                        sqrMaxDetectDistance = sqrDistanceToTarget;
                        closestTarget = player;
                    }
                }
            }

            return closestTarget;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            SpriteEffects effects = _timer < 210 ? SpriteEffects.FlipVertically : SpriteEffects.None;

            Color outlineColor = Color.Cyan;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < 24; i++)
            {
                Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi / 24f * i) * 6f;
                Main.EntitySpriteDraw(
                    texture, drawPos + offset, null, outlineColor, Projectile.rotation, origin, Projectile.scale, effects, 0f
                );
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                texture, drawPos, null, lightColor, Projectile.rotation, origin, Projectile.scale, effects, 0f
            );

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundStyle explosionSound = SoundID.Item14;
            if (ModLoader.TryGetMod("FargowiltasSouls", out Mod fargoMod))
            {
                string soundPath = "FargowiltasSouls/Assets/Sounds/Explosion";
                if (ModContent.HasAsset(soundPath))
                {
                    explosionSound = new SoundStyle(soundPath);
                }
            }
            SoundEngine.PlaySound(explosionSound, Projectile.Center);

            var spreadSys = ModContent.GetInstance<RadiationSpreadSystem>();

            PunchCameraModifier modifier = new PunchCameraModifier(Projectile.Center, (Main.rand.NextFloat() * ((float)Math.PI * 2f)).ToRotationVector2(), 40f, 20f, 60, 2000f, "Fatman");
            Main.instance.CameraModifiers.Add(modifier);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int blockRadius = 75;
                int wallRadius = 90;

                Vector2 center = Projectile.Center;
                int centerX = (int)(center.X / 16f);
                int centerY = (int)(center.Y / 16f);

                // Criação da cratera normal (destrói tudo, sem colocar radiação)
                for (int x = centerX - blockRadius; x <= centerX + blockRadius; x++)
                {
                    for (int y = centerY - blockRadius; y <= centerY + blockRadius; y++)
                    {
                        if (x >= 0 && x < Main.maxTilesX && y >= 0 && y < Main.maxTilesY)
                        {
                            float distSq = (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY);

                            if (distSq < wallRadius * wallRadius)
                            {
                                WorldGen.KillWall(x, y, false);
                            }

                            if (distSq < blockRadius * blockRadius)
                            {
                                Tile tile = Main.tile[x, y];
                                if (tile.HasTile)
                                {
                                    if (tile.TileType == TileID.LihzahrdBrick ||
                                        tile.TileType == TileID.Containers ||
                                        tile.TileType == TileID.Containers2 ||
                                        tile.TileType == TileID.DemonAltar)
                                        continue;

                                    WorldGen.KillTile(x, y, false, false, true);
                                }
                            }
                        }
                    }
                }

                // LIBERAÇÃO DA RADIAÇÃO: só na PRIMEIRA bomba de cada mundo
                if (!spreadSys.FirstExplosionDone)
                {
                    spreadSys.FirstExplosionDone = true; // marca ANTES de gerar, para nunca repetir

                    // Sorteia um local no subterrâneo (entre a camada de rocha e o inferno)
                    int veinX = WorldGen.genRand.Next(100, Main.maxTilesX - 100);
                    int veinY = WorldGen.genRand.Next((int)Main.rockLayer, Main.maxTilesY - 200);

                    // Tamanho do veio
                    int veinSize = WorldGen.genRand.Next(8, 15);

                    // Gera o veio de radiação
                    WorldGen.OreRunner(veinX, veinY, veinSize, veinSize, (ushort)ModContent.TileType<NuclearTile>());

                    if (Main.netMode == NetmodeID.Server)
                    {
                        // Multiplayer: sincroniza o veio e avisa todos os jogadores
                        NetMessage.SendTileSquare(-1, veinX, veinY, veinSize * 2 + 4);
                        Terraria.Chat.ChatHelper.BroadcastChatMessage(
                            NetworkText.FromLiteral("A explosão despertou uma fonte de radiação no subterrâneo!"),
                            new Color(50, 255, 50));
                    }
                    else
                    {
                        Main.NewText("A explosão despertou uma fonte de radiação no subterrâneo!", new Color(50, 255, 50));
                    }
                }
            }

            for (int i = 0; i < 25; i++)
            {
                Vector2 spawnPos = Projectile.Center + Main.rand.NextVector2Circular(500f, 500f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPos, Vector2.Zero, ProjectileID.RocketIV, 500, 10f, Projectile.owner);
            }

            for (int i = 0; i < 800; i++)
            {
                Vector2 speed = Main.rand.NextVector2CircularEdge(35f, 35f);
                int dustType = DustID.Torch;
                if (i % 3 == 0) dustType = DustID.GreenTorch;
                else if (i % 3 == 1) dustType = DustID.Frost;

                Dust d = Dust.NewDustPerfect(Projectile.Center, dustType, speed, 0, default, 3f);
                d.noGravity = true;
            }

            float explosionRadius = 300f;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.immortal && !npc.dontTakeDamage)
                {
                    if (Vector2.Distance(npc.Center, Projectile.Center) < explosionRadius)
                        npc.SimpleStrikeNPC(900, 1, false, 10f, null, false, 0f, true);
                }
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && !player.ghost)
                {
                    if (Vector2.Distance(player.Center, Projectile.Center) < explosionRadius)
                    {
                        int savedDefense = player.statDefense;
                        player.statDefense *= 0;

                        PlayerDeathReason reason = PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral($"{player.name} foi detonado pela Ogiva Fatman."));

                        player.Hurt(reason, 900, 1, false, false, -1, false);

                        player.statDefense += savedDefense;
                    }
                }
            }
        }
    }

    public class FatmanScreenFlash : ModSystem
    {
        private int _flashTimer;
        private const int MaxFlashTime = 120;

        public void StartFlash()
        {
            _flashTimer = MaxFlashTime;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "Atomic: Fatman Flash",
                    delegate
                    {
                        if (_flashTimer > 0)
                        {
                            _flashTimer--;

                            float progress = 1f - ((float)_flashTimer / MaxFlashTime);

                            float width;
                            float opacity = 1f;

                            if (progress < 0.3f)
                            {
                                float expandProgress = progress / 0.3f;
                                width = MathHelper.Lerp(10f, Main.screenWidth, expandProgress);
                            }
                            else if (progress < 0.7f)
                            {
                                width = Main.screenWidth;
                            }
                            else
                            {
                                width = Main.screenWidth;
                                opacity = 1f - ((progress - 0.7f) / 0.3f);
                            }

                            int rectX = (int)(Main.screenWidth * 0.5f - width * 0.5f);
                            int rectY = 0;
                            int rectHeight = Main.screenHeight;

                            Main.spriteBatch.Draw(
                                TextureAssets.MagicPixel.Value,
                                new Rectangle(rectX, rectY, (int)width, rectHeight),
                                Color.White * opacity
                            );
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }
}