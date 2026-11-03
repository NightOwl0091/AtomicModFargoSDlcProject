using Terraria;
using Terraria.ModLoader;
using Atomic.Content.NonBossStuff.Systems.Biomes.Atomic; // Caminho completo e correto

namespace Atomic
{
    public class Atomic : Mod
    {
        // Hook para o BTitles: verifica se o jogador está no bioma de Radiação
        public string BTitlesHook_BiomeChecker(Player player)
        {
            // Agora podemos usar o nome simples 'RadiationBiome' porque o using foi adicionado
            if (player.InModBiome<RadiationBiome>())
            {
                return "RadiationBiome"; // Chave única para o bioma
            }
            return "";
        }

        // Hook para o BTitles: fornece os dados visuais do título
        public dynamic BTitlesHook_GetBiome(int index)
        {
            if (index == 0)
            {
                return new
                {
                    Key = "RadiationBiome",          // Deve ser igual ao retornado no BiomeChecker
                    Title = "Radiação",              // Texto que aparecerá
                    SubTitle = "Atomic",             // Subtítulo opcional
                    TitleColor = Microsoft.Xna.Framework.Color.LimeGreen, // Cor do texto
                    TitleStroke = Microsoft.Xna.Framework.Color.Black,     // Cor do contorno
                };
            }
            return null;
        }
    }
}