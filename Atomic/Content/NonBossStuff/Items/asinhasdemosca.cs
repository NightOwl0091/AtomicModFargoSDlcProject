using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Atomic.Content.NonBossStuff.Items
{
    public class Asinhas_De_Mosca : ModItem
    {
        // Caminho da textura (sem o .png)
        public override string Texture => "Atomic/Assets/Sprites/Vanilla Addons/Items/asinhasdemosca";

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(platinum: 1) + Item.sellPrice(gold: 91) + Item.sellPrice(silver: 50) + Item.sellPrice(copper: 50);
            Item.rare = ItemRarityID.Expert;

            // AQUI ESTÁ A CORREÇÃO: Substitui o nome da classe por um nome com espaços
            Item.SetNameOverride("Asinhas De Mosca");
        }
    }
}