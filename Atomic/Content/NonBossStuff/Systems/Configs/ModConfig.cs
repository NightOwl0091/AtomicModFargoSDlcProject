using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace Atomic.Content.NonBossStuff.Systems.Configs
{
    public enum SpreadSpeed
    {
        Desligado,
        Baixo,
        Medio,
        Rapido
    }

    public class AtomicConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;
        public static AtomicConfig Instance => ModContent.GetInstance<AtomicConfig>();

        [Header("ModTester")]

        [DefaultValue(SpreadSpeed.Medio)]
        public SpreadSpeed RadiationSpreadSpeed { get; set; }

        [DefaultValue(SpreadSpeed.Medio)]
        public SpreadSpeed GrassSpreadSpeed { get; set; }

        [DefaultValue(SpreadSpeed.Medio)]
        public SpreadSpeed TuftSpreadSpeed { get; set; }
    }
}