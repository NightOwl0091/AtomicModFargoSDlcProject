using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Atomic.Content.NonBossStuff.Items;
using Atomic.Content.NonBossStuff.Systems.Biomes.Atomic.Inimigos;

namespace Atomic.Content.NonBossStuff.Systems.Biomes.Atomic
{
    public class AngryFlyDownedSystem : ModSystem
    {
        public static bool downedAngryFly = false;

        // Posição na lista do BossChecklist (Pilares = 14, Senhor da Lua = 15).
        // Para testar se a entrada está sendo registrada, troque temporariamente por 0.5f.
        private const float ChecklistWeight = 17.2f;

        public override void OnWorldLoad()
        {
            downedAngryFly = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (downedAngryFly)
                tag["downedAngryFly"] = true;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            downedAngryFly = tag.GetBool("downedAngryFly");
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(downedAngryFly);
        }

        public override void NetReceive(BinaryReader reader)
        {
            downedAngryFly = reader.ReadBoolean();
        }

        // Integração com o BossChecklist
        public override void PostSetupContent()
        {
            if (!ModLoader.TryGetMod("BossChecklist", out Mod bossChecklist))
                return;

            bossChecklist.Call(
                "LogMiniBoss",
                Mod,                                   // precisa ser o Mod, não o ModSystem (this)
                "AngryFly",                            // chave interna da entrada
                ChecklistWeight,
                (Func<bool>)(() => downedAngryFly),    // condição de derrota
                ModContent.NPCType<OrdinaryNpc1>(),
                new Dictionary<string, object>()
                {
                    // Nome mostrado só no BossChecklist; o nome do NPC continua Moscália Bostoleira
                    ["displayName"] = Language.GetOrRegister("Mods.Atomic.BossChecklist.AngryFly.DisplayName", () => "Mosca Raivosa"),
                    ["spawnInfo"] = Language.GetOrRegister("Mods.Atomic.BossChecklist.AngryFly.SpawnInfo", () =>
                        "Spawna naturalmente no Bioma de Radiação. Se o jogador ficar fugindo dela por tempo demais, ela se irrita e vira a Mosca Raivosa."),
                    ["collectibles"] = new List<int>() { ModContent.ItemType<VileHeart>() }
                }
            );
        }
    }
}