using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;
using Atomic.Content.NonBossStuff.Systems.Biomes.Atomic.Inimigos;

namespace Atomic.Content.NonBossStuff.Items
{
    public class EmaranhadoDePernasDeMosca : ModItem
    {
        public override string Texture => "Atomic/Assets/Sprites/Vanilla Addons/Items/edpm";

        public override LocalizedText DisplayName => Language.GetOrRegister("Mods.Atomic.Items.EmaranhadoDePernasDeMosca.DisplayName", () => "Emaranhado de Pernas de Mosca");

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;
            Item.accessory = true;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(gold: 5);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            TangleSpeedPlayer modPlayer = player.GetModPlayer<TangleSpeedPlayer>();
            modPlayer.tangle = true;

            // Ghosting: rastro de imagens do jogador quando ele já pegou embalo
            if (!hideVisual && modPlayer.momentum > 0.1f)
                player.armorEffectDrawShadow = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "EdpmSpeed", "A velocidade de movimento aumenta gradativamente"));
            TooltipLine desc = new TooltipLine(Mod, "EdpmDesc", "Elas tem medo de você");
            desc.OverrideColor = new Color(140, 15, 40);
            tooltips.Add(desc);
        }
    }

    // Velocidade progressiva: quanto mais tempo correndo na mesma direção, mais rápido.
    // A cada 20% de embalo ele ganha um pico de +5 de velocidade, com a explosão de partículas e o som do dash da mosca.
    public class TangleSpeedPlayer : ModPlayer
    {
        public bool tangle;       // acessório equipado
        public float momentum;    // 0 a 1

        // 90 mph convertidos para pixels/tick (fórmula do Stopwatch: mph = px/tick * 216000 / 42240)
        public const float MaxSpeed = 90f * 42240f / 216000f; // = 17.6

        private const float BuildPerTick = 1f / 300f;  // ~5 segundos correndo para chegar ao máximo
        private const float DecayPerTick = 1f / 40f;   // perde o embalo rápido ao parar
        private const float BurstSpeed = 5f;           // tamanho de cada pico

        private int lastStage;
        private int lastDir;

        public override void ResetEffects()
        {
            tangle = false;
        }

        // Depois de todos os bônus vanilla: aqui o teto de corrida sobe junto com o embalo
        public override void PostUpdateRunSpeeds()
        {
            if (!tangle)
                return;

            float baseline = MathHelper.Max(Player.maxRunSpeed, Player.accRunSpeed);
            float cap = MathHelper.Lerp(baseline, MaxSpeed, momentum);

            Player.maxRunSpeed = cap;
            Player.accRunSpeed = cap;
            Player.runAcceleration *= 1f + momentum; // acelera mais rápido para acompanhar o teto
        }

        public override void PostUpdate()
        {
            if (!tangle)
            {
                momentum = 0f;
                lastStage = 0;
                lastDir = 0;
                return;
            }

            int dir = 0;
            if (Player.controlRight && !Player.controlLeft) dir = 1;
            else if (Player.controlLeft && !Player.controlRight) dir = -1;

            // Inverter a direção quebra boa parte do embalo
            if (dir == 0)
                lastDir = 0;
            else
            {
                if (lastDir != 0 && dir != lastDir)
                    momentum *= 0.25f;
                lastDir = dir;
            }

            // Só ganha embalo se estiver realmente se movendo na direção pressionada (parede zera o ganho)
            bool running = dir != 0 && System.Math.Sign(Player.velocity.X) == dir && System.Math.Abs(Player.velocity.X) > 1f;

            if (running)
                momentum = MathHelper.Min(1f, momentum + BuildPerTick);
            else
                momentum = MathHelper.Max(0f, momentum - DecayPerTick);

            // Um pico a cada 20% de embalo (5 picos até o máximo)
            int stage = (int)(momentum * 5f);
            if (running && stage > lastStage)
                Burst(stage, dir);
            lastStage = stage;

            // Limite duro: nunca passa de 90 mph na horizontal
            if (Player.whoAmI == Main.myPlayer)
                Player.velocity.X = MathHelper.Clamp(Player.velocity.X, -MaxSpeed, MaxSpeed);
        }

        private void Burst(int stage, int dir)
        {
            // Só o dono do personagem aplica o impulso (os outros recebem a velocidade pela rede)
            if (Player.whoAmI == Main.myPlayer)
                Player.velocity.X = MathHelper.Clamp(Player.velocity.X + BurstSpeed * dir, -MaxSpeed, MaxSpeed);

            if (Main.dedServ)
                return;

            // Mesmo som e mesma explosão de partículas dos dashes da mosca
            SoundEngine.PlaySound(SoundID.Item14 with { Pitch = 0.1f * stage, Volume = 0.6f }, Player.Center);
            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.TerraBlade);
                d.velocity = -Player.velocity * 0.3f + Main.rand.NextVector2Circular(5f, 5f);
                d.noGravity = true;
            }
        }
    }

    // Só dropa de uma mosca acuada pelo Coração Vil (e que ainda não virou boss)
    public class CowedFlyDropCondition : IItemDropRuleCondition
    {
        public bool CanDrop(DropAttemptInfo info)
        {
            return info.npc?.ModNPC is OrdinaryNpc1 fly && fly.cowed && !fly.isBoss;
        }

        public bool CanShowItemDropInUI() => true;

        public string GetConditionDescription() => "Dropa de uma mosca acuada pelo Coração Vil";
    }
}