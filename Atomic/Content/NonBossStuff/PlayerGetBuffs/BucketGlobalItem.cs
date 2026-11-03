using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ModLiquidLib.ModLoader;
using Atomic.Content.NonBossStuff.Consumables.Consumables;
using Atomic.Content.NonBossStuff.Systems.Biomes.Atomic;

namespace Atomic.Content.NonBossStuff.PlayerGetBuffs
{
    public class BucketGlobalItem : GlobalItem
    {
        public override bool? UseItem(Item item, Player player)
        {
            if (item.type == ItemID.EmptyBucket)
            {
                Point targetTile = Main.MouseWorld.ToTileCoordinates();
                if (WorldGen.InWorld(targetTile.X, targetTile.Y))
                {
                    Tile tile = Main.tile[targetTile.X, targetTile.Y];

                    // OBTÉM O ID OFICIAL DO LÍQUIDO
                    int radType = LiquidLoader.LiquidType<RadiationLiquid>();

                    if (tile.LiquidAmount > 0 && tile.LiquidType == radType)
                    {
                        player.QuickSpawnItem(player.GetSource_ItemUse(item), ModContent.ItemType<RadiationBucket>());

                        tile.LiquidAmount = 0;
                        WorldGen.SquareTileFrame(targetTile.X, targetTile.Y, true);

                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendTileSquare(-1, targetTile.X, targetTile.Y, 1);

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Splash, player.Center);

                        item.stack--;
                        if (item.stack <= 0) item.TurnToAir();
                        return true;
                    }
                }
            }
            return null;
        }
    }
}