using Atomic.Content.NonBossStuff.Systems.Biomes.Atomic.Inimigos;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Utilities;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Atomic.Content.NonBossStuff.Items
{
    public class VileHeart : ModItem
    {
        public override string Texture => "Atomic/Assets/Sprites/Vanilla Addons/Items/VileHeart";

        public override LocalizedText DisplayName => Language.GetOrRegister("Mods.Atomic.Items.VileHeart.DisplayName", () => "Coração Vil");

        // Duração de um batimento em ticks (50 = ~72 bpm). Ajuste para casar com o ritmo do seu HeartBeating.wav.
        public const int BeatPeriodTicks = 50;

        public override void SetDefaults()
        {
            Item.width = 64;   // acompanha o sprite de 64x64
            Item.height = 64;
            Item.maxStack = 1;
            Item.uniqueStack = true;       // o jogador só consegue carregar um
            Item.accessory = true;         // equipável
            Item.rare = ItemRarityID.Master;

            // Item.value é inteiro, então o teto de venda fica em ~429 platinum (int.MaxValue / 5).
            Item.value = int.MaxValue;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.statLifeMax2 += 200;

            VileHeartPlayer modPlayer = player.GetModPlayer<VileHeartPlayer>();
            modPlayer.vileHeart = true;
            if (!hideVisual)
                modPlayer.heartVisual = true;
        }

        // Equipado no slot de vanity: só o visual, sem os efeitos
        public override void UpdateVanity(Player player)
        {
            player.GetModPlayer<VileHeartPlayer>().heartVisual = true;
        }

        // Escala do batimento com o "tum-tum" (pulso forte + pulso fraco) e uma pausa. phase vai de 0 a 1.
        public static float BeatScale(float phase)
        {
            float lub = (float)System.Math.Exp(-System.Math.Pow((phase - 0.10f) / 0.06f, 2));
            float dub = (float)System.Math.Exp(-System.Math.Pow((phase - 0.32f) / 0.05f, 2));

            float beat = lub + 0.65f * dub;
            return 0.94f + 0.16f * beat; // varia entre ~0.94 (relaxado) e ~1.10 (contraído)
        }

        // Inventário, hotbar e baús
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            float phase = (Main.GameUpdateCount % BeatPeriodTicks) / (float)BeatPeriodTicks;
            Texture2D texture = TextureAssets.Item[Type].Value;
            spriteBatch.Draw(texture, position, frame, drawColor, 0f, origin, scale * BeatScale(phase), SpriteEffects.None, 0f);
            return false;
        }

        // Item solto no chão
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            float phase = (Main.GameUpdateCount % BeatPeriodTicks) / (float)BeatPeriodTicks;
            scale *= BeatScale(phase);
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "VileHeartLife", "+200 de vida máxima"));
            tooltips.Add(new TooltipLine(Mod, "VileHeartRegen", "Abaixo de 30% de vida, regenera +4 de vida por segundo"));
            tooltips.Add(new TooltipLine(Mod, "VileHeartBeat", "Você consegue ouvir um coração batendo..."));

            TooltipLine line = new TooltipLine(Mod, "VileHeartTooltip", "Cheira a inseto");
            line.OverrideColor = new Color(180, 20, 20);
            tooltips.Add(line);
        }
    }

    // Efeitos do Coração Vil: regeneração, som, batimento visual e sangue
    public class VileHeartPlayer : ModPlayer
    {
        public bool vileHeart;     // efeitos ativos (acessório equipado)
        public bool heartVisual;   // desenha o coração no peito
        public float heartPhase;   // fase do batimento (0 a 1)

        private const float MaxPitch = 0.35f; // aceleração do som com vida baixa (em oitavas)

        private SlotId heartSlot;
        private float intensity; // 0 = calmo, 1 = vida baixa

        private static readonly SoundStyle HeartBeatSound = new SoundStyle("Atomic/Assets/Music/HeartBeating")
        {
            IsLooped = true,
            Volume = 1f,
            MaxInstances = 1,
            SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
        };

        private bool LowLife => Player.statLife < Player.statLifeMax2 * 0.3f;

        public override void ResetEffects()
        {
            vileHeart = false;
            heartVisual = false;
        }

        public override void UpdateLifeRegen()
        {
            // lifeRegen é em "meios pontos" por segundo: 8 = +4 de vida por segundo
            if (vileHeart && LowLife)
                Player.lifeRegen += 8;
        }

        public override void PostUpdate()
        {
            if (Main.dedServ)
                return;

            // Suaviza a transição entre o batimento calmo e o intenso
            float target = (vileHeart && LowLife) ? 1f : 0f;
            intensity = MathHelper.Lerp(intensity, target, 0.08f);

            // A fase do batimento visual acompanha a velocidade do som (pitch em oitavas -> 2^pitch)
            float speed = (float)System.Math.Pow(2.0, MaxPitch * intensity);
            float prevPhase = heartPhase;
            heartPhase = (heartPhase + speed / VileHeart.BeatPeriodTicks) % 1f;

            if (heartVisual && !Player.dead)
                SpawnBlood(prevPhase);

            // O som só toca para o jogador local
            if (Player.whoAmI == Main.myPlayer && vileHeart && !Player.dead && !SoundEngine.TryGetActiveSound(heartSlot, out _))
                heartSlot = SoundEngine.PlaySound(HeartBeatSound, Player.Center, UpdateHeartSound);
        }

        // Chamado todo tick enquanto o som toca; retornar false encerra o som
        private bool UpdateHeartSound(ActiveSound sound)
        {
            if (!vileHeart || !Player.active || Player.dead)
                return false;

            sound.Position = Player.Center;
            sound.Volume = MathHelper.Lerp(0.5f, 1f, intensity);  // mais alto com vida baixa
            sound.Pitch = MathHelper.Lerp(0f, MaxPitch, intensity); // e mais acelerado
            return true;
        }

        private void SpawnBlood(float prevPhase)
        {
            // Pinga sangue do peito a cada batimento forte (mais gotas com vida baixa)
            bool beatNow = prevPhase < 0.10f && heartPhase >= 0.10f;
            if (beatNow)
            {
                int count = LowLife ? 3 : 1;
                for (int i = 0; i < count; i++)
                {
                    Vector2 pos = Player.Center + new Vector2(Main.rand.NextFloat(-3f, 3f), -2f);
                    Vector2 vel = new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-1.5f, 0f));
                    BloodStainSystem.SpawnDrop(pos, vel);
                }
            }

            // Rastro de sangue por onde o jogador passa
            if (Player.velocity.Length() > 1.5f && Main.GameUpdateCount % 6 == 0)
            {
                Vector2 pos = Player.Center + new Vector2(Main.rand.NextFloat(-6f, 6f), Player.height / 4f);
                BloodStainSystem.SpawnDrop(pos, Player.velocity * 0.1f);
            }
        }
    }

    // Gotas de sangue que caem, batem no chão e viram manchas que ficam no mapa
    public class BloodStainSystem : ModSystem
    {
        private class Drop
        {
            public Vector2 pos;
            public Vector2 vel;
            public int life;
        }

        private class Stain
        {
            public Vector2 pos;  // centro da base da mancha, em coordenadas de mundo
            public int width;
            public int seed;
            public int age;
        }

        private static readonly List<Drop> drops = new List<Drop>();
        private static readonly List<Stain> stains = new List<Stain>();

        private const int MaxDrops = 150;
        private const int MaxStains = 300;               // passou disso, a mais antiga some
        private const int StainLifetime = 60 * 60 * 8;   // 8 minutos até sumir de vez
        private const int FadeTicks = 60 * 10;           // some aos poucos nos últimos 10 segundos

        public static void SpawnDrop(Vector2 position, Vector2 velocity)
        {
            if (Main.dedServ || drops.Count >= MaxDrops)
                return;

            drops.Add(new Drop { pos = position, vel = velocity });
        }

        public override void OnWorldUnload()
        {
            drops.Clear();
            stains.Clear();
        }

        public override void PostUpdateEverything()
        {
            if (Main.dedServ || Main.gamePaused)
                return;

            for (int i = drops.Count - 1; i >= 0; i--)
            {
                Drop d = drops[i];
                d.life++;
                d.vel.Y = MathHelper.Min(d.vel.Y + 0.25f, 10f);
                d.pos += d.vel;

                if (Collision.SolidCollision(d.pos, 2, 2))
                {
                    // Volta um passo e desce até encostar na superfície
                    Vector2 p = d.pos - d.vel;
                    int guard = 0;
                    while (!Collision.SolidCollision(p + new Vector2(0f, 1f), 2, 2) && guard++ < 16)
                        p.Y += 1f;

                    AddStain(p + new Vector2(1f, 2f));
                    drops.RemoveAt(i);
                    continue;
                }

                if (d.life > 600)
                    drops.RemoveAt(i);
            }

            for (int i = stains.Count - 1; i >= 0; i--)
            {
                stains[i].age++;
                if (stains[i].age >= StainLifetime)
                    stains.RemoveAt(i);
            }
        }

        private static void AddStain(Vector2 pos)
        {
            if (stains.Count >= MaxStains)
                stains.RemoveAt(0);

            stains.Add(new Stain
            {
                pos = pos,
                width = Main.rand.Next(4, 10),
                seed = Main.rand.Next(1000)
            });
        }

        private static Color Lit(Color baseColor, Color light)
        {
            return new Color(baseColor.R * light.R / 255, baseColor.G * light.G / 255, baseColor.B * light.B / 255);
        }

        public override void PostDrawTiles()
        {
            if (stains.Count == 0 && drops.Count == 0)
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D pixel = TextureAssets.MagicPixel.Value;

            foreach (Stain s in stains)
            {
                Vector2 sp = s.pos - Main.screenPosition;
                if (sp.X < -32f || sp.Y < -32f || sp.X > Main.screenWidth + 32f || sp.Y > Main.screenHeight + 32f)
                    continue;

                float fade = s.age > StainLifetime - FadeTicks ? (StainLifetime - s.age) / (float)FadeTicks : 1f;
                Color light = Lighting.GetColor((int)(s.pos.X / 16f), (int)((s.pos.Y - 2f) / 16f));
                Color color = Lit(new Color(125, 10, 16), light) * fade;

                // Mancha principal achatada no chão + um respingo menor em cima
                Main.spriteBatch.Draw(pixel, new Rectangle((int)sp.X - s.width / 2, (int)sp.Y - 2, s.width, 2), color);
                Main.spriteBatch.Draw(pixel, new Rectangle((int)sp.X - s.width / 4 + (s.seed % 3 - 1), (int)sp.Y - 3, System.Math.Max(2, s.width / 2), 1), color);
            }

            foreach (Drop d in drops)
            {
                Vector2 sp = d.pos - Main.screenPosition;
                Color light = Lighting.GetColor((int)(d.pos.X / 16f), (int)(d.pos.Y / 16f));
                Main.spriteBatch.Draw(pixel, new Rectangle((int)sp.X, (int)sp.Y, 2, 2), Lit(new Color(170, 15, 20), light));
            }

            Main.spriteBatch.End();
        }
    }

    // Desenha o coração de 14x16 no peito do personagem, pulsando.
    // Fica por cima de armaduras e acessórios, mas por baixo dos braços.
    public class VileHeartChestLayer : PlayerDrawLayer
    {
        private const string TexturePath = "Atomic/Assets/Sprites/Vanilla Addons/Items/woreheart";

        // Ajuste fino da posição no peito, relativo ao centro do corpo. X é espelhado conforme a direção do jogador.
        private static readonly Vector2 ChestOffset = new Vector2(0f, -2f);

        // Logo antes da camada do braço que fica na frente do item (ArmOverItem):
        // já passou por corpo, armaduras e acessórios, então o coração fica por cima deles,
        // e o braço desenhado depois fica por cima do coração.
        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.ArmOverItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            return !p.dead && !p.invis && p.GetModPlayer<VileHeartPlayer>().heartVisual;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            if (drawInfo.shadow != 0f)
                return;

            Player p = drawInfo.drawPlayer;
            VileHeartPlayer modPlayer = p.GetModPlayer<VileHeartPlayer>();
            Texture2D texture = ModContent.Request<Texture2D>(TexturePath, AssetRequestMode.ImmediateLoad).Value;

            // Acompanha o balanço do corpo na animação de andar
            int frameIndex = (p.bodyFrame.Y / p.bodyFrame.Height) % Main.OffsetsPlayerHeadgear.Length;
            Vector2 bob = Main.OffsetsPlayerHeadgear[frameIndex];
            bob.Y *= p.gravDir;

            Vector2 offset = new Vector2(ChestOffset.X * p.direction, ChestOffset.Y * p.gravDir);
            Vector2 pos = drawInfo.Position - Main.screenPosition + new Vector2(p.width / 2f, p.height / 2f) + offset + bob;
            pos = new Vector2((int)pos.X, (int)pos.Y);

            float scale = VileHeart.BeatScale(modPlayer.heartPhase);
            Color color = Color.Lerp(drawInfo.colorArmorBody, Color.White, 0.35f);

            drawInfo.DrawDataCache.Add(new DrawData(texture, pos, null, color, 0f, texture.Size() / 2f, scale, SpriteEffects.None, 0));
        }
    }

    // Só dropa da Moscália na fase boss (Mosca Raivosa) e se o mundo ainda não teve o drop
    public class VileHeartDropCondition : IItemDropRuleCondition
    {
        public bool CanDrop(DropAttemptInfo info)
        {
            if (info.npc?.ModNPC is OrdinaryNpc1 fly && fly.isBoss)
                return !VileHeartSystem.droppedVileHeart;

            return false;
        }

        public bool CanShowItemDropInUI() => true;

        public string GetConditionDescription() => "Dropa apenas da Mosca Raivosa (único por mundo)";
    }

    // Detecta quando o item realmente foi dropado (só servidor/singleplayer decide)
    public class VileHeartGlobalItem : GlobalItem
    {
        public override void OnSpawn(Item item, IEntitySource source)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            if (item.type == ModContent.ItemType<VileHeart>() && source is EntitySource_Loot)
            {
                VileHeartSystem.droppedVileHeart = true;

                if (Main.netMode == NetmodeID.Server)
                {
                    Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("A mosca dropou algo raro e único!"), Color.Gold);
                    NetMessage.SendData(MessageID.WorldData);
                }
                else
                {
                    Main.NewText("A mosca dropou algo raro e único!", Color.Gold);
                }
            }
        }
    }

    public class VileHeartSystem : ModSystem
    {
        public static bool droppedVileHeart = false;

        public override void OnWorldLoad()
        {
            droppedVileHeart = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (droppedVileHeart)
                tag["droppedVileHeart"] = true;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            droppedVileHeart = tag.GetBool("droppedVileHeart");
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(droppedVileHeart);
        }

        public override void NetReceive(BinaryReader reader)
        {
            droppedVileHeart = reader.ReadBoolean();
        }
    }
}