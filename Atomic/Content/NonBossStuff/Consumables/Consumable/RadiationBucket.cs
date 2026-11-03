using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent.Creative;
using ModLiquidLib.ModLoader;
using ModLiquidLib.ID;
using Atomic.Content.NonBossStuff.Systems.Biomes.Atomic;

namespace Atomic.Content.NonBossStuff.Consumables.Consumables
{
    public class RadiationBucket : ModItem
    {
        public override string Texture => "Atomic/Assets/Sprites/Vanilla Addons/Items/RadiationBucket";

        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsLavaImmuneRegardlessOfRarity[Type] = true;
            ItemID.Sets.AlsoABuildingItem[Type] = true;
            ItemID.Sets.DuplicationMenuToolsFilter[Type] = true;

            // REGISTRO OFICIAL: Diz à biblioteca que este item é o balde do nosso líquido.
            // Isso permite que o jogo saiba que este balde contém Radiação.
            LiquidID_TLmod.Sets.CreateLiquidBucketItem[LiquidLoader.LiquidType<RadiationLiquid>()] = Type;

            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 5;
        }

        public override void SetDefaults()
        {
            Item.width = 20; Item.height = 24;
            Item.maxStack = Item.CommonMaxStack;
            Item.useTurn = true; Item.autoReuse = true;
            Item.useAnimation = 15; Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
        }

        public override void HoldItem(Player player)
        {
            if (!player.JustDroppedAnItem)
            {
                if (player.whoAmI != Main.myPlayer) return;
                if (player.noBuilding || !(player.position.X / 16f - (float)Player.tileRangeX - (float)Item.tileBoost <= (float)Player.tileTargetX) ||
                    !((player.position.X + (float)player.width) / 16f + (float)Player.tileRangeX + (float)Item.tileBoost - 1f >= (float)Player.tileTargetX) ||
                    !(player.position.Y / 16f - (float)Player.tileRangeY - (float)Item.tileBoost <= (float)Player.tileTargetY) ||
                    !((player.position.Y + (float)player.height) / 16f + (float)Player.tileRangeY + (float)Item.tileBoost - 2f >= (float)Player.tileTargetY))
                { return; }

                if (!Main.GamepadDisableCursorItemIcon)
                {
                    player.cursorItemIconEnabled = true;
                    Main.ItemIconCacheUpdate(Item.type);
                }

                if (!player.ItemTimeIsZero || player.itemAnimation <= 0 || !player.controlUseItem) return;

                Tile tile = Main.tile[Player.tileTargetX, Player.tileTargetY];
                if (tile.LiquidAmount >= 200) return;

                if (tile.HasUnactuatedTile)
                {
                    bool[] tileSolid = Main.tileSolid;
                    if (tileSolid[tile.TileType])
                    {
                        bool[] tileSolidTop = Main.tileSolidTop;
                        if (!tileSolidTop[tile.TileType])
                        {
                            if (tile.TileType != TileID.Grate) return;
                        }
                    }
                }

                // OBTÉM O ID DO LÍQUIDO NA HORA EXATA (evita o bug de virar água)
                int bucketLiquidType = LiquidLoader.LiquidType<RadiationLiquid>();

                if (tile.LiquidAmount != 0)
                {
                    if (tile.LiquidType != bucketLiquidType) return;
                }

                SoundEngine.PlaySound(SoundID.SplashWeak, player.position);
                tile.LiquidType = (byte)bucketLiquidType;
                tile.LiquidAmount = byte.MaxValue;
                WorldGen.SquareTileFrame(Player.tileTargetX, Player.tileTargetY);

                Item.stack--;
                player.PutItemInInventoryFromItemUsage(ItemID.EmptyBucket, player.selectedItem);
                player.ApplyItemTime(Item);

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    NetMessage.sendWater(Player.tileTargetX, Player.tileTargetY);
                }
            }
        }
    }
}