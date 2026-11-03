using Terraria;
using Terraria.ModLoader;
using Atomic.Content.NonBossStuff.Systems.Biomes.Atomic; // Necessário para achar a RadiationDebuff

namespace Atomic.Content.NonBossStuff.PlayerGetBuffs
{
    public class NuclearEffects : ModPlayer
    {
        public override void PostUpdateEquips()
        {
            // Verifica se o jogador está com o debuff de Radiação
            if (Player.HasBuff(ModContent.BuffType<RadiationDebuff>()))
            {
                // Exemplo de mecânica: A radiação te deixa mais forte, mas mais vulnerável.
                // Sinta-se livre para alterar esses valores ou adicionar novos efeitos!

                Player.GetDamage(DamageClass.Generic) += 0.15f; // +15% de dano
                Player.statDefense -= 10; // -10 de defesa
            }
        }

        public override void ResetEffects()
        {
            // Se você adicionar variáveis para resetar a cada frame, coloque aqui.
        }
    }
}