using Atomic.Content.NonBossStuff.PlayerGetBuffs;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Localization;
using Microsoft.Xna.Framework;

namespace Atomic.Content.NonBossStuff.Consumables.Edible
{
    // ========== SEMENTES ==========
    public class RedMushroomSeed : ModItem
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.CrimsonSeeds;

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.CrimsonSeeds);
            Item.SetNameOverride("Semente de Amanita Muscaria");
            Item.createTile = ModContent.TileType<GiantRedMushroomTile>();
            Item.placeStyle = 0;
            Item.consumable = true;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(copper: 20);
        }
    }

    public class BlueMushroomSeed : ModItem
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.MushroomGrassSeeds;

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.MushroomGrassSeeds);
            Item.SetNameOverride("Semente de Psilocybe Cubensis");
            Item.createTile = ModContent.TileType<GiantBlueMushroomTile>();
            Item.placeStyle = 0;
            Item.consumable = true;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(copper: 20);
        }
    }

    // ========== TILES 4x4 (BLOCOS) ==========
    public class GiantRedMushroomTile : ModTile
    {
        public override string Texture => "Atomic/Assets/Sprites/Vanilla Addons/Positional/RedMushroom";
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileSolidTop[Type] = true;

            AddMapEntry(new Color(200, 50, 50), Language.GetOrRegister("Mods.Atomic.Tiles.GiantRedMushroomTile.MapEntry", () => "Amanita Muscaria Gigante"));

            TileObjectData.newTile.Width = 4;
            TileObjectData.newTile.Height = 4;
            TileObjectData.newTile.Origin = new Point16(1, 3);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16, 16 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.addTile(Type);
        }
        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            // Referência atualizada para AmanitaMuscaria
            Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 64, 64, ModContent.ItemType<AmanitaMuscaria>(), Main.rand.Next(1, 4));
        }
    }

    public class GiantBlueMushroomTile : ModTile
    {
        public override string Texture => "Atomic/Assets/Sprites/Vanilla Addons/Positional/BlueMushroom";
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileSolidTop[Type] = true;
            Main.tileBouncy[Type] = true;

            AddMapEntry(new Color(50, 50, 200), Language.GetOrRegister("Mods.Atomic.Tiles.GiantBlueMushroomTile.MapEntry", () => "Psilocybe Cubensis Gigante"));

            TileObjectData.newTile.Width = 4;
            TileObjectData.newTile.Height = 4;
            TileObjectData.newTile.Origin = new Point16(1, 3);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16, 16 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.addTile(Type);
        }
        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            // Referência atualizada para PsilocybeCubensis
            Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 64, 64, ModContent.ItemType<PsilocybeCubensis>(), Main.rand.Next(1, 4));
        }
    }

    // ========== CONSUMÍVEIS ==========
    // Classe renomeada de GiantBMushConsumable para PsilocybeCubensis
    public class PsilocybeCubensis : ModItem
    {
        public override string Texture => "Atomic/Assets/Sprites/Vanilla Addons/EdibleConsumables/GiantBMushConsumable";

        public override void SetDefaults()
        {
            Item.width = 20; Item.height = 20;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useTime = 17; Item.useAnimation = 17;
            Item.UseSound = SoundID.Item2;
            Item.value = Item.sellPrice(silver: 50);
            Item.rare = ItemRarityID.Blue;

            Item.SetNameOverride("Psilocybe Cubensis");
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "BlueMushTooltip", "Deixa você drogado por 1 minuto"));
        }
        public override bool? UseItem(Player player)
        {
            player.AddBuff(ModContent.BuffType<DruggedBuff>(), 3600);
            return true;
        }
    }

    // Classe renomeada de GiantRMushConsumable para AmanitaMuscaria
    public class AmanitaMuscaria : ModItem
    {
        public override string Texture => "Atomic/Assets/Sprites/Vanilla Addons/EdibleConsumables/GiantRMushConsumable";

        public override void SetDefaults()
        {
            Item.width = 20; Item.height = 20;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useTime = 17; Item.useAnimation = 17;
            Item.UseSound = SoundID.Item2;
            Item.value = Item.sellPrice(silver: 50);
            Item.rare = ItemRarityID.Red;

            Item.SetNameOverride("Amanita Muscaria");
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "RedMushTooltip", "Concede voo infinito e regeneração por 8 segundos"));
        }
        public override bool? UseItem(Player player)
        {
            player.AddBuff(ModContent.BuffType<RedMushroomFlightBuff>(), 480);
            player.AddBuff(ModContent.BuffType<RedMushroomRegenBuff>(), 480);
            return true;
        }
    }

    // ========== LOJA DA DRÍADE ==========
    public class DryadMushroomShop : GlobalNPC
    {
        public override void ModifyShop(NPCShop shop)
        {
            if (shop.NpcType == NPCID.Dryad && NPC.downedPlantBoss)
            {
                shop.Add<RedMushroomSeed>();
                shop.Add<BlueMushroomSeed>();
            }
        }
    }
}