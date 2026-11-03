using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Atomic.Content.NonBossStuff.Systems.Biomes.Atomic; // Puxando a radiação da pasta correta

namespace Atomic.Content.NonBossStuff.Systems.Npcs
{
    public class DryadSystem : ModSystem
    {
        public override void PostUpdateEverything()
        {
            if (Main.LocalPlayer.talkNPC != -1 && Main.npc[Main.LocalPlayer.talkNPC].type == NPCID.Dryad)
            {
                if (!string.IsNullOrEmpty(Main.npcChatText) && Main.npcChatText.Contains("%"))
                {
                    if (!Main.npcChatText.Contains("radiação"))
                    {
                        var spreadSys = ModContent.GetInstance<RadiationSpreadSystem>();
                        if (spreadSys != null && spreadSys.RadiationPercentage > 0f)
                        {
                            float percent = spreadSys.RadiationPercentage * 100f;
                            Main.npcChatText += $"\n\nA radiação já cobriu {percent:F2}% do mundo.";
                        }
                    }
                }
            }
        }
    }
}