using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Chat;
using Atomic.Content.NonBossStuff.Systems.Biomes.Atomic;
using Atomic.Content.NonBossStuff.Items;
using Terraria.GameContent.ItemDropRules;
using Atomic.Assets.AnotherModAssets.Assets.Content.Projectiles.Masomode;

namespace Atomic.Content.NonBossStuff.Systems.Biomes.Atomic.Inimigos
{
    [AutoloadBossHead]
    public class OrdinaryNpc1 : ModNPC
    {
        public override string Texture => "Atomic/Assets/Sprites/ModBiomes/Inimigos/OrdinaryNpc1";

        // Só é boss depois de irritada (angerState 3)
        public bool isBoss => angerState == 3;

        // Acuada pelo Coração Vil: metade do tamanho, 75% menos vida, dropa o Emaranhado de Pernas de Mosca
        public bool cowed = false;

        private const string AtkTexturePath = "Atomic/Assets/Sprites/ModBiomes/Inimigos/OrdinaryNpc1Atk";
        private const string AtkHitTexturePath = "Atomic/Assets/Sprites/ModBiomes/Inimigos/OrdinaryNpc1AtkOnhit";
        private const string HitTexturePath = "Atomic/Assets/Sprites/ModBiomes/Inimigos/OrdinaryNpc1_Onhit";

        // O nome do inimigo continua o original. "Mosca Raivosa" aparece só no BossChecklist.
        public override LocalizedText DisplayName => Language.GetOrRegister("Mods.Atomic.NPCs.OrdinaryNpc1.DisplayName", () => "Moscália Bostoleira");

        private static readonly int[] IdleFrameX = new int[] { 0, 66, 132, 198 };
        private static readonly int[] AtkFrameX = new int[] { 2, 64, 126, 188, 250, 312, 374 };

        private Vector2[] oldPositions = new Vector2[6];
        private float[] oldRotations = new float[6];
        private int flashTimer = 0;
        private int stunTimer = 0;
        private int currentFrame = 0;

        private bool seenPlayer = false;
        private int irritationTimer = 0;
        private int chaseTimer = 0;
        private int currentSpeedStage = 0;
        private static readonly float[] SpeedThresholds = { 7f, 11f, 15f, 19f, 23f };

        private float fadeOpacity = 0f;
        private int stageCooldown = 0;

        // 0 = normal, 1 = tremendo, 2 = explodindo de raiva, 3 = irritada (boss)
        private int angerState = 0;
        private int angerTimer = 0;

        private const float FleeRange = 700f;      // dentro disso ela foge do jogador com o Coração Vil
        private const float CatchRange = 350f;     // perto assim conta como "perseguição"
        private const int ChaseTicksNeeded = 240;  // ~4 segundos de perseguição para ela ficar acuada
        private const float CowedScale = 0.5f;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 4;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
        }

        public override void SetDefaults()
        {
            NPC.width = 50;
            NPC.height = 40;
            NPC.damage = 50;
            NPC.defense = 40;
            NPC.lifeMax = 5000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 1f;
            NPC.noGravity = true;
            NPC.noTileCollide = false;
            NPC.value = Item.buyPrice(gold: 10);
            NPC.aiStyle = -1;
            NPC.npcSlots = 1f;

            NPC.boss = false;
            fadeOpacity = 0f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            NPC.boss = false;

            NPC.position.Y -= 150f;
            NPC.netUpdate = true;

            for (int i = 0; i < oldPositions.Length; i++)
            {
                oldPositions[i] = NPC.Center;
                oldRotations[i] = NPC.rotation;
            }
        }

        public override void OnKill()
        {
            // Só conta como "Mosca Raivosa derrotada" se ela estava na fase boss
            if (isBoss && Main.netMode != NetmodeID.MultiplayerClient)
            {
                AngryFlyDownedSystem.downedAngryFly = true;

                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.WorldData);
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)angerState);
            writer.Write(seenPlayer);
            writer.Write(cowed);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            angerState = reader.ReadByte();
            seenPlayer = reader.ReadBoolean();
            cowed = reader.ReadBoolean();
        }

        public override void BossHeadSlot(ref int index)
        {
            if (!isBoss)
                index = -1;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            flashTimer = 42;
            if (angerState == 0) stunTimer = 30;
            currentSpeedStage = 0;

            for (int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Blood, NPC.velocity.X * 0.5f, NPC.velocity.Y * 0.5f);
                d.noGravity = true;
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (NPC.ai[0] == 0f)
            {
                NPC.frame.Width = 64;
                NPC.frame.Height = 62;
            }
            else
            {
                NPC.frame.Width = 60;
                NPC.frame.Height = 60;
            }
            NPC.frame.Y = 0;

            NPC.frameCounter++;
            if (NPC.frameCounter >= 4)
            {
                NPC.frameCounter = 0;
                currentFrame++;
            }

            if (NPC.ai[0] == 0f)
            {
                if (currentFrame >= IdleFrameX.Length) currentFrame = 0;
                NPC.frame.X = IdleFrameX[currentFrame];
            }
            else
            {
                if (currentFrame >= AtkFrameX.Length) currentFrame = 0;
                NPC.frame.X = AtkFrameX[currentFrame];
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            Vector2 direction = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY);

            target.velocity.X = direction.X * 12f;
            target.velocity.Y = -6f;

            NPC.velocity = -direction * 15f;
            if (angerState == 0) stunTimer = 30;
            currentSpeedStage = 0;
        }

        public override void AI()
        {
            if (flashTimer > 0) flashTimer--;
            if (stunTimer > 0) stunTimer--;
            if (stageCooldown > 0) stageCooldown--;

            NPC.boss = isBoss;

            Lighting.AddLight(NPC.Center, 1.0f, 0.2f, 0.1f);

            if (fadeOpacity < 1f)
            {
                fadeOpacity += 0.03f;
                if (fadeOpacity > 1f) fadeOpacity = 1f;
            }

            // Acuada: encolhe até metade do tamanho mantendo o centro no lugar
            // (roda em todos os clientes, pois "cowed" é sincronizado)
            if (cowed && angerState == 0)
            {
                NPC.scale = MathHelper.Lerp(NPC.scale, CowedScale, 0.05f);
                if (NPC.scale < CowedScale + 0.01f) NPC.scale = CowedScale;

                Vector2 center = NPC.Center;
                NPC.width = (int)(50 * NPC.scale);
                NPC.height = (int)(40 * NPC.scale);
                NPC.Center = center;
            }

            Player player = Main.player[NPC.target];

            if (!player.active || player.dead)
            {
                NPC.TargetClosest(false);
                player = Main.player[NPC.target];
                if (!player.active || player.dead)
                {
                    NPC.velocity.Y -= 0.1f;
                    if (NPC.timeLeft > 10) NPC.timeLeft = 10;
                    return;
                }
            }

            float distance = Vector2.Distance(NPC.Center, player.Center);

            // Com o Coração Vil equipado ela tem medo: foge em vez de atacar e nunca se irrita
            bool afraid = angerState == 0 && player.GetModPlayer<VileHeartPlayer>().vileHeart;

            // Direção do sprite: fugindo, olha para onde está indo (com folga para não piscar);
            // nos outros casos, olha para o jogador
            if (afraid && distance < FleeRange)
            {
                if (System.Math.Abs(NPC.velocity.X) > 0.5f)
                    NPC.direction = NPC.velocity.X > 0f ? 1 : -1;
            }
            else
            {
                NPC.direction = NPC.Center.X < player.Center.X ? 1 : -1;
            }
            NPC.spriteDirection = NPC.direction;

            if (!seenPlayer && distance < 250f && fadeOpacity > 0.5f)
            {
                seenPlayer = true;
                NPC.netUpdate = true;
            }

            if (angerState == 0 && stunTimer <= 0)
            {
                if (afraid)
                {
                    irritationTimer = 0;
                    FleeFromPlayer(player, distance);
                }
                else
                {
                    chaseTimer = 0;

                    if (distance > 400f && seenPlayer)
                    {
                        irritationTimer++;
                        if (irritationTimer > 180 && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            angerState = 1;
                            cowed = false;
                            angerTimer = 0;
                            irritationTimer = 0;

                            SpawnReticle();

                            NPC.netUpdate = true;
                        }
                    }
                    else irritationTimer = 0;

                    if (seenPlayer)
                    {
                        NPC.ai[0] = 1f;
                        Vector2 direction = player.Center - NPC.Center;
                        direction.Normalize();
                        NPC.velocity = Vector2.Lerp(NPC.velocity, direction * 15f, 0.03f);
                        NPC.rotation = (float)System.Math.Atan2(NPC.velocity.Y, System.Math.Abs(NPC.velocity.X));

                        if (currentSpeedStage < 5 && NPC.velocity.Length() >= SpeedThresholds[currentSpeedStage] && stageCooldown <= 0)
                        {
                            currentSpeedStage++;
                            SoundEngine.PlaySound(SoundID.Item14 with { Pitch = 0.1f * currentSpeedStage, Volume = 0.6f }, NPC.Center);
                            for (int i = 0; i < 15; i++)
                            {
                                Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.TerraBlade);
                                d.velocity = -NPC.velocity * 0.3f + Main.rand.NextVector2Circular(5f, 5f);
                                d.noGravity = true;
                            }
                            stageCooldown = 8;
                        }
                    }
                    else
                    {
                        IdleHover(player);
                    }
                }
            }
            else if (angerState == 1)
            {
                angerTimer++;
                NPC.velocity *= 0.8f;
                NPC.rotation += 0.1f;

                if (angerTimer >= 90 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    angerState = 2;
                    angerTimer = 0;

                    if (Main.netMode == NetmodeID.Server)
                        ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("Cuidado, a mosca se irritou com suas tentativas de evadir seus ataques!"), Color.Red);
                    else
                        Main.NewText("Cuidado, a mosca se irritou com suas tentativas de evadir seus ataques!", Color.Red);

                    NPC.netUpdate = true;
                }
            }
            else if (angerState == 2)
            {
                angerTimer++;
                NPC.velocity *= 0.9f;
                NPC.rotation += 0.3f;

                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Shadowflame);
                    d.velocity = Main.rand.NextVector2Circular(8f, 8f);
                    d.noGravity = true;
                }

                if (angerTimer >= 60 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    angerState = 3; // a partir daqui isBoss == true
                    currentSpeedStage = 0;
                    angerTimer = 0;

                    NPC.lifeMax *= 5;
                    NPC.life = NPC.lifeMax;
                    NPC.netUpdate = true;

                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    for (int i = 0; i < 50; i++)
                    {
                        Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Shadowflame);
                        d.velocity = Main.rand.NextVector2CircularEdge(12f, 12f);
                        d.noGravity = true;
                        d.scale = 2f;
                    }
                }
            }
            else if (angerState == 3)
            {
                if (NPC.scale < 2f)
                {
                    NPC.scale += 0.015f;
                    if (NPC.scale > 2f) NPC.scale = 2f;

                    NPC.width = (int)(50 * NPC.scale);
                    NPC.height = (int)(40 * NPC.scale);
                }

                NPC.ai[0] = 1f;
                Vector2 direction = player.Center - NPC.Center;
                direction.Normalize();

                NPC.velocity = Vector2.Lerp(NPC.velocity, direction * 25f, 0.12f);
                NPC.rotation = (float)System.Math.Atan2(NPC.velocity.Y, System.Math.Abs(NPC.velocity.X));

                if (currentSpeedStage < 5 && NPC.velocity.Length() >= SpeedThresholds[currentSpeedStage] && stageCooldown <= 0)
                {
                    currentSpeedStage++;
                    SoundEngine.PlaySound(SoundID.Item14 with { Pitch = 0.1f * currentSpeedStage, Volume = 0.6f }, NPC.Center);
                    for (int i = 0; i < 15; i++)
                    {
                        Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.TerraBlade);
                        d.velocity = -NPC.velocity * 0.3f + Main.rand.NextVector2Circular(5f, 5f);
                        d.noGravity = true;
                    }
                    stageCooldown = 8;
                }
            }

            if (seenPlayer && NPC.velocity.Length() > SpeedThresholds[0] && fadeOpacity >= 1f)
            {
                for (int i = oldPositions.Length - 1; i > 0; i--)
                {
                    oldPositions[i] = oldPositions[i - 1];
                    oldRotations[i] = oldRotations[i - 1];
                }
                oldPositions[0] = NPC.Center;
                oldRotations[0] = NPC.rotation;
            }
            else
            {
                for (int i = 0; i < oldPositions.Length; i++)
                {
                    oldPositions[i] = NPC.Center;
                    oldRotations[i] = NPC.rotation;
                }
            }
        }

        // Voo parado em volta do jogador (comportamento "calmo")
        private void IdleHover(Player player)
        {
            NPC.ai[0] = 0f;
            float xOffset = (float)System.Math.Sin(NPC.ai[1]) * 350f;
            float yOffset = -200f + (float)System.Math.Cos(NPC.ai[1]) * 100f;
            Vector2 targetPos = player.Center + new Vector2(xOffset, yOffset);
            Vector2 direction = targetPos - NPC.Center;
            if (direction.Length() > 10f) NPC.velocity = Vector2.Lerp(NPC.velocity, direction.SafeNormalize(Vector2.Zero) * 6f, 0.05f);
            NPC.ai[1] += 0.025f;
            NPC.rotation = (float)System.Math.Sin(NPC.ai[1] * 0.5f) * 0.15f;
        }

        // Foge de quem está com o Coração Vil. Se o jogador a mantiver encurralada por um tempo, ela fica acuada.
        private void FleeFromPlayer(Player player, float distance)
        {
            if (distance < FleeRange)
            {
                NPC.ai[0] = 1f;
                Vector2 away = (NPC.Center - player.Center).SafeNormalize(Vector2.UnitY);
                NPC.velocity = Vector2.Lerp(NPC.velocity, away * 11f, 0.1f);

                // O sprite original olha para a esquerda e é espelhado quando direction == 1,
                // então a inclinação precisa ser multiplicada pela direção para acompanhar o espelhamento
                NPC.rotation = (float)System.Math.Atan2(NPC.velocity.Y, System.Math.Abs(NPC.velocity.X)) * NPC.direction;

                if (distance < CatchRange) chaseTimer++;
                else if (chaseTimer > 0) chaseTimer--;

                if (!cowed && chaseTimer >= ChaseTicksNeeded && Main.netMode != NetmodeID.MultiplayerClient)
                    BecomeCowed();
            }
            else
            {
                if (chaseTimer > 0) chaseTimer--;
                IdleHover(player);
            }
        }

        private void BecomeCowed()
        {
            cowed = true;

            // Perde 75% da vida atual
            NPC.life = System.Math.Max(1, (int)(NPC.life * 0.25f));
            NPC.netUpdate = true;

            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Smoke);
                d.velocity = Main.rand.NextVector2Circular(4f, 4f);
                d.noGravity = true;
            }
        }

        private void SpawnReticle()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                NPC.Center,
                Vector2.Zero,
                ModContent.ProjectileType<TargetingReticle>(),
                0,
                0f,
                Main.myPlayer,
                NPC.whoAmI
            );
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D currentMainTexture;
            Texture2D currentHitTexture;
            Vector2 drawOrigin;

            if (NPC.ai[0] == 0f)
            {
                currentMainTexture = TextureAssets.Npc[Type].Value;
                currentHitTexture = ModContent.Request<Texture2D>(HitTexturePath).Value;
                drawOrigin = new Vector2(32f, 31f);
            }
            else
            {
                currentMainTexture = ModContent.Request<Texture2D>(AtkTexturePath).Value;
                currentHitTexture = ModContent.Request<Texture2D>(AtkHitTexturePath).Value;
                drawOrigin = new Vector2(30f, 30f);
            }

            SpriteEffects effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            for (int k = oldPositions.Length - 1; k >= 0; k--)
            {
                Vector2 drawPos = oldPositions[k] - screenPos;
                Color trailColor = drawColor * ((float)(oldPositions.Length - k) / oldPositions.Length) * 0.15f * fadeOpacity;
                spriteBatch.Draw(currentMainTexture, drawPos, NPC.frame, trailColor, oldRotations[k], drawOrigin, NPC.scale, effects, 0f);
            }

            Vector2 drawPosMain = NPC.Center - screenPos;

            if (angerState == 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float angle = (float)System.Math.PI * 2f / 4f * i + NPC.rotation;
                    Vector2 offset = new Vector2(40f, 0).RotatedBy(angle);

                    spriteBatch.Draw(currentMainTexture, drawPosMain + offset, NPC.frame, drawColor * 0.4f * fadeOpacity, NPC.rotation, drawOrigin, NPC.scale, effects, 0f);
                }
            }

            Color finalColor = drawColor * fadeOpacity;

            if (flashTimer > 0)
            {
                spriteBatch.Draw(currentHitTexture, drawPosMain, NPC.frame, finalColor, NPC.rotation, drawOrigin, NPC.scale, effects, 0f);
            }
            else
            {
                spriteBatch.Draw(currentMainTexture, drawPosMain, NPC.frame, finalColor, NPC.rotation, drawOrigin, NPC.scale, effects, 0f);
            }

            return false;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!isBoss) return;

            float healthPercent = (float)NPC.life / NPC.lifeMax;
            Vector2 pos = NPC.Top - screenPos - new Vector2(0, 20);
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            int barWidth = 50;
            int barHeight = 6;

            spriteBatch.Draw(pixel, new Rectangle((int)pos.X - (barWidth / 2), (int)pos.Y, barWidth, barHeight), Color.Black * 0.8f * fadeOpacity);

            int fillWidth = (int)((barWidth - 2) * healthPercent);
            if (fillWidth > 0)
            {
                spriteBatch.Draw(pixel, new Rectangle((int)pos.X - (barWidth / 2) + 1, (int)pos.Y + 1, fillWidth, barHeight - 2), Color.Red * fadeOpacity);
            }

            spriteBatch.Draw(pixel, new Rectangle((int)pos.X - (barWidth / 2), (int)pos.Y, barWidth, 1), Color.White * 0.5f * fadeOpacity);
            spriteBatch.Draw(pixel, new Rectangle((int)pos.X - (barWidth / 2), (int)pos.Y + barHeight - 1, barWidth, 1), Color.White * 0.5f * fadeOpacity);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.PoopBlock, 1, 1, 3));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Asinhas_De_Mosca>(), 2, 1, 2));

            // Coração Vil: só na fase boss e só uma vez por mundo (ver VileHeartDropCondition)
            npcLoot.Add(ItemDropRule.ByCondition(new VileHeartDropCondition(), ModContent.ItemType<VileHeart>(), 1, 1, 1));

            // Emaranhado de Pernas de Mosca: só de uma mosca acuada pelo Coração Vil
            npcLoot.Add(ItemDropRule.ByCondition(new CowedFlyDropCondition(), ModContent.ItemType<EmaranhadoDePernasDeMosca>(), 1, 1, 1));
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Player.InModBiome<RadiationBiome>() && !NPC.AnyNPCs(Type))
            {
                return 5f;
            }
            return 0f;
        }
    }
}