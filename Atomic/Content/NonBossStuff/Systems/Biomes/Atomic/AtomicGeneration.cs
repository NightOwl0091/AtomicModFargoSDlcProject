using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ObjectData;
using Terraria.WorldBuilding;
using Terraria.ModLoader.IO;
using Terraria.Audio;
using Terraria.GameContent.Liquid;
using Terraria.Enums;
using ModLiquidLib.ModLoader;
using Atomic.Content.NonBossStuff.Systems.Configs;

namespace Atomic.Content.NonBossStuff.Systems.Biomes.Atomic
{
    // ==============================================================================
    // 1. BIOMA E CONTAGEM DE TILES
    // ==============================================================================
    public class RadiationBiome : ModBiome
    {
        public override LocalizedText DisplayName => Language.GetOrRegister("Mods.Atomic.Biomes.RadiationBiome.DisplayName", () => "Radiação");
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Music/radiation_theme");
        public override SceneEffectPriority Priority => SceneEffectPriority.BiomeMedium;

        public override void SetStaticDefaults()
        {
            MusicID.Sets.SkipsVolumeRemap[Music] = true;
        }

        public override bool IsBiomeActive(Player player)
        {
            var tileCountSystem = ModContent.GetInstance<RadiationTileCountSystem>();
            return tileCountSystem.RadiationTileCount >= 300;
        }
    }

    public class RadiationTileCountSystem : ModSystem
    {
        public int RadiationTileCount { get; private set; }

        public override void TileCountsAvailable(ReadOnlySpan<int> tileCounts)
        {
            int dirtCount = tileCounts[ModContent.TileType<AtomicDirtTile>()];
            int nuclearCount = tileCounts[ModContent.TileType<NuclearTile>()];
            int rockCount = tileCounts[ModContent.TileType<AtomicStone>()];
            int sandCount = tileCounts[ModContent.TileType<AtomicSand>()];
            int vineCount = tileCounts[ModContent.TileType<AtomicVines>()];
            int grassCount = tileCounts[ModContent.TileType<RadioactiveGrass>()];
            int floorGrassCount = tileCounts[ModContent.TileType<AtomicFloorGrass>()];

            RadiationTileCount = dirtCount + nuclearCount + rockCount + sandCount + vineCount + grassCount + floorGrassCount;
        }
    }

    // ==============================================================================
    // 2. O LÍQUIDO (ModLiquidLib)
    // ==============================================================================
    public class RadiationLiquid : ModLiquid
    {
        public override string Texture => "Atomic/Assets/Sprites/Liquids/RadiationWaterStyle_Liquid";
        public override string BlockTexture => "Atomic/Assets/Sprites/Liquids/RadiationWaterStyle_Liquid_Block";
        public override string SlopeTexture => "Atomic/Assets/Sprites/Liquids/RadiationWaterStyle_Liquid_Slope";

        public override void SetStaticDefaults()
        {
            LiquidRenderer.VISCOSITY_MASK[Type] = 220;
            LiquidRenderer.DEFAULT_OPACITY[Type] = 1f;
            LiquidRenderer.WATERFALL_LENGTH[Type] = 15;
            SlopeOpacity = 1f;
            LiquidfallOpacityMultiplier = 0.8f;
            WaterRippleMultiplier = 0.1f;

            SplashDustType = DustID.GreenTorch;
            SplashSound = SoundID.SplashWeak;

            FallDelay = 3;
            ChecksForDrowning = true;
            AllowEmitBreathBubbles = false;

            PlayerMovementMultiplier = 0.15f;
            StopWatchMPHMultiplier = PlayerMovementMultiplier;
            NPCMovementMultiplierDefault = PlayerMovementMultiplier;
            ProjectileMovementMultiplier = PlayerMovementMultiplier;

            UsesLavaCollisionForWet = false;
            ExtinguishesOnFireDebuffs = false;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 0.15f; g = 1.0f; b = 0.15f;
        }
    }

    // ==============================================================================
    // 3. O MOTOR DE ESPALHAMENTO (RADIAÇÃO, GRAMA E MATINHO)
    // ==============================================================================
    public class RadiationSpreadSystem : ModSystem
    {
        public bool FirstExplosionDone = false;
        public bool HasScannedWorld = false;

        public List<Point> RadiationTiles = new List<Point>();
        public readonly HashSet<Point> radiationSet = new HashSet<Point>();
        public int NuclearTileCount = 0;
        public float RadiationPercentage = 0f;

        private int scanX = 0;
        private int scanY = 0;
        private bool explosionHandled = false;

        public void RegisterTile(int x, int y)
        {
            var p = new Point(x, y);
            if (radiationSet.Add(p)) RadiationTiles.Add(p);
        }

        public void ResetRadiation()
        {
            FirstExplosionDone = false;
            HasScannedWorld = false;
            explosionHandled = false;
            RadiationTiles.Clear();
            radiationSet.Clear();
            NuclearTileCount = 0;
            RadiationPercentage = 0f;
        }

        public override void OnWorldLoad()
        {
            ResetRadiation();
            scanX = 0;
            scanY = 0;
        }

        public override void PostUpdateWorld()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            if (FirstExplosionDone && !explosionHandled)
            {
                explosionHandled = true;
                HasScannedWorld = false;
                scanX = 0;
                scanY = 0;
            }
            else if (!FirstExplosionDone)
            {
                explosionHandled = false;
            }

            if (!HasScannedWorld)
            {
                int tilesPerTick = 50000;
                for (int i = 0; i < tilesPerTick; i++)
                {
                    scanX++;
                    if (scanX >= Main.maxTilesX) { scanX = 0; scanY++; }
                    if (scanY >= Main.maxTilesY)
                    {
                        HasScannedWorld = true;
                        NuclearTileCount = RadiationTiles.Count;
                        RadiationPercentage = (float)NuclearTileCount / (Main.maxTilesX * Main.maxTilesY);
                        break;
                    }
                    if (WorldGen.InWorld(scanX, scanY))
                    {
                        Tile tile = Main.tile[scanX, scanY];
                        if (tile.HasTile && IsAnyRadioactiveBlock(tile.TileType)) RegisterTile(scanX, scanY);
                    }
                }
                return;
            }

            if (RadiationTiles.Count == 0) return;

            // --- ESPALHAMENTO DA RADIAÇÃO ---
            int speed = 0;
            switch (AtomicConfig.Instance.RadiationSpreadSpeed)
            {
                case SpreadSpeed.Desligado: speed = 0; break;
                case SpreadSpeed.Baixo: speed = 5; break;
                case SpreadSpeed.Medio: speed = 50; break;
                case SpreadSpeed.Rapido: speed = 1000; break;
            }

            if (speed > 0)
            {
                for (int i = 0; i < speed; i++)
                {
                    Point p = RadiationTiles[WorldGen.genRand.Next(RadiationTiles.Count)];
                    TrySpreadRadiation(p.X, p.Y);
                }
            }

            // --- ESPALHAMENTO DA GRAMA SÓLIDA ---
            int grassSpeed = 0;
            switch (AtomicConfig.Instance.GrassSpreadSpeed)
            {
                case SpreadSpeed.Desligado: grassSpeed = 0; break;
                case SpreadSpeed.Baixo: grassSpeed = 5; break;
                case SpreadSpeed.Medio: grassSpeed = 50; break;
                case SpreadSpeed.Rapido: grassSpeed = 1000; break;
            }

            if (grassSpeed > 0)
            {
                for (int i = 0; i < grassSpeed; i++)
                {
                    Point p = RadiationTiles[WorldGen.genRand.Next(RadiationTiles.Count)];
                    TrySpreadGrass(p.X, p.Y);
                }
            }

            // --- ESPALHAMENTO DO MATINHO ---
            int tuftSpeed = 0;
            switch (AtomicConfig.Instance.TuftSpreadSpeed)
            {
                case SpreadSpeed.Desligado: tuftSpeed = 0; break;
                case SpreadSpeed.Baixo: tuftSpeed = 5; break;
                case SpreadSpeed.Medio: tuftSpeed = 50; break;
                case SpreadSpeed.Rapido: tuftSpeed = 1000; break;
            }

            if (tuftSpeed > 0)
            {
                for (int i = 0; i < tuftSpeed; i++)
                {
                    Point p = RadiationTiles[WorldGen.genRand.Next(RadiationTiles.Count)];
                    TrySpreadTuft(p.X, p.Y);
                }
            }
        }

        private void TrySpreadRadiation(int x, int y)
        {
            if (!WorldGen.InWorld(x, y)) return;
            Tile tile = Main.tile[x, y];
            if (!tile.HasTile || !IsAnyRadioactiveBlock(tile.TileType)) return;

            for (int i = 0; i < 4; i++)
            {
                int nx = x + (i == 0 ? 1 : i == 1 ? -1 : 0);
                int ny = y + (i == 2 ? 1 : i == 3 ? -1 : 0);

                if (!WorldGen.InWorld(nx, ny)) continue;
                Tile nTile = Main.tile[nx, ny];
                if (!nTile.HasTile) continue;

                int newType = GetRadioactiveEquivalent(nTile.TileType);
                if (newType != 0)
                {
                    nTile.TileType = (ushort)newType;
                    WorldGen.SquareTileFrame(nx, ny, true);
                    RegisterTile(nx, ny);
                }
            }
        }

        private void TrySpreadGrass(int x, int y)
        {
            if (!WorldGen.InWorld(x, y)) return;
            Tile tile = Main.tile[x, y];
            if (!tile.HasTile || tile.TileType != ModContent.TileType<AtomicDirtTile>()) return;

            if (!Main.tile[x, y - 1].HasTile)
            {
                tile.TileType = (ushort)ModContent.TileType<RadioactiveGrass>();
                WorldGen.SquareTileFrame(x, y, true);
            }
        }

        private void TrySpreadTuft(int x, int y)
        {
            if (!WorldGen.InWorld(x, y)) return;
            Tile tile = Main.tile[x, y];
            if (!tile.HasTile) return;

            // Se for um bloco de grama sólida, tenta crescer um matinho em cima
            if (tile.TileType == ModContent.TileType<RadioactiveGrass>())
            {
                if (!Main.tile[x, y - 1].HasTile)
                {
                    WorldGen.PlaceTile(x, y - 1, ModContent.TileType<AtomicFloorGrass>(), mute: true, forced: true, plr: -1, style: GetRandomGrassStyle());
                }
            }
            // Se já for um matinho, tenta espalhar para os lados (onde houver grama sólida embaixo)
            else if (tile.TileType == ModContent.TileType<AtomicFloorGrass>())
            {
                for (int i = 0; i < 2; i++)
                {
                    int nx = x + (i == 0 ? 1 : -1);
                    if (!WorldGen.InWorld(nx, y)) continue;

                    if (!Main.tile[nx, y].HasTile && WorldGen.InWorld(nx, y + 1))
                    {
                        Tile below = Main.tile[nx, y + 1];
                        if (below.HasTile && below.TileType == ModContent.TileType<RadioactiveGrass>())
                        {
                            WorldGen.PlaceTile(nx, y, ModContent.TileType<AtomicFloorGrass>(), mute: true, forced: true, plr: -1, style: GetRandomGrassStyle());
                        }
                    }
                }
            }
        }

        // FUNÇÃO DE PESO: 10% de chance de ser cogumelo (frame 0), 90% de chance de ser mato (frames 1 a 5)
        public static int GetRandomGrassStyle()
        {
            if (Main.rand.Next(100) < 10) return 0; // Cogumelo
            return Main.rand.Next(1, 6); // Mato (1, 2, 3, 4 ou 5)
        }

        private int GetRadioactiveEquivalent(ushort originalType)
        {
            if (originalType == TileID.Dirt || originalType == TileID.Grass || originalType == TileID.JungleGrass || originalType == TileID.MushroomGrass)
                return ModContent.TileType<AtomicDirtTile>();

            if (originalType == TileID.Stone || originalType == TileID.Ebonstone || originalType == TileID.Crimstone || originalType == TileID.Pearlstone || originalType == TileID.ClayBlock)
                return ModContent.TileType<AtomicStone>();

            if (originalType == TileID.Sand || originalType == TileID.Ebonsand || originalType == TileID.Crimsand || originalType == TileID.Pearlsand)
                return ModContent.TileType<AtomicSand>();

            if (originalType == TileID.Vines || originalType == TileID.VineFlowers || originalType == TileID.JungleVines || originalType == TileID.HallowedVines)
                return ModContent.TileType<AtomicVines>();

            return 0;
        }

        public bool IsAnyRadioactiveBlock(int type)
        {
            return type == ModContent.TileType<NuclearTile>() ||
                   type == ModContent.TileType<AtomicDirtTile>() ||
                   type == ModContent.TileType<AtomicStone>() ||
                   type == ModContent.TileType<AtomicSand>() ||
                   type == ModContent.TileType<AtomicVines>() ||
                   type == ModContent.TileType<RadioactiveGrass>() ||
                   type == ModContent.TileType<AtomicFloorGrass>();
        }

        public override void SaveWorldData(TagCompound tag) { tag["FirstExplosionDone"] = FirstExplosionDone; }
        public override void LoadWorldData(TagCompound tag) { FirstExplosionDone = tag.GetBool("FirstExplosionDone"); }
    }

    // ==============================================================================
    // 4. OS TILES (BLOCOS SÓLIDOS E MATINHOS)
    // ==============================================================================
    public abstract class GrassGrowing : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileCut[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileNoFail[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileSolid[Type] = false;

            TileID.Sets.Grass[Type] = true;
            TileID.Sets.SwaysInWindBasic[Type] = true;
            TileID.Sets.BreakableWhenPlacing[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, 1, 0);
            TileObjectData.newTile.CoordinateHeights = new[] { 16 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.StyleWrapLimit = 6;
            TileObjectData.newTile.DrawYOffset = -2; // CORREÇÃO: Puxa o sprite 1 pixel para cima
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(50, 255, 50), CreateMapEntryName());
            DustType = DustID.GrassBlades;
        }

        public override bool KillSound(int i, int j, bool fail)
        {
            if (!fail) SoundEngine.PlaySound(SoundID.Grass with { PitchVariance = 0.2f }, new Vector2(i * 16, j * 16));
            return false;
        }

        public override void RandomUpdate(int i, int j)
        {
            if (AtomicConfig.Instance.TuftSpreadSpeed == SpreadSpeed.Desligado) return;

            if (Main.rand.NextBool(4))
            {
                int targetX = i + (Main.rand.NextBool() ? -1 : 1);
                if (WorldGen.InWorld(targetX, j) && !Main.tile[targetX, j].HasTile)
                {
                    Tile tileBelow = Main.tile[targetX, j + 1];
                    if (tileBelow.HasTile && tileBelow.TileType == ModContent.TileType<RadioactiveGrass>())
                    {
                        WorldGen.PlaceTile(targetX, j, Type, mute: true, forced: true, plr: -1, style: RadiationSpreadSystem.GetRandomGrassStyle());
                    }
                }
            }
        }
    }

    public class AtomicFloorGrass : GrassGrowing
    {
        public override string Texture => "Atomic/Assets/Sprites/ModBiomes/Radiations/Blocks/Tiles/Atomicfloorgrass";

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            float pulse = 0.8f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.3f;
            r = 0.05f; g = 0.6f * pulse; b = 0.05f;
        }
    }

    public class AtomicDirtTile : ModTile
    {
        public override string Texture => "Atomic/Assets/Sprites/ModBiomes/Radiations/Blocks/Tiles/AtomicDirtRevampFull";

        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileFrameImportant[Type] = false;
            Main.tileMergeDirt[Type] = true;
            Main.tileMerge[Type][Type] = true;
            Main.tileMerge[Type][TileID.Dirt] = true;
            Main.tileMerge[TileID.Dirt][Type] = true;
            Main.tileMerge[Type][TileID.Grass] = true;
            Main.tileMerge[TileID.Grass][Type] = true;
            AddMapEntry(new Color(80, 60, 40), CreateMapEntryName());
            DustType = DustID.Dirt;
            RegisterItemDrop(ModContent.ItemType<AtomicDirtBlock>());
        }

        public override void RandomUpdate(int i, int j)
        {
            if (AtomicConfig.Instance.GrassSpreadSpeed == SpreadSpeed.Desligado) return;

            if (Main.rand.NextBool(2) && !Main.tile[i, j - 1].HasTile)
            {
                Main.tile[i, j].TileType = (ushort)ModContent.TileType<RadioactiveGrass>();
                WorldGen.SquareTileFrame(i, j, true);
            }
        }
    }

    public class RadioactiveGrass : ModTile
    {
        public override string Texture => "Atomic/Assets/Sprites/ModBiomes/Radiations/Blocks/Tiles/radiograss";

        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileFrameImportant[Type] = false;
            Main.tileMergeDirt[Type] = true;
            Main.tileMerge[Type][Type] = true;
            Main.tileMerge[Type][ModContent.TileType<AtomicDirtTile>()] = true;
            Main.tileMerge[ModContent.TileType<AtomicDirtTile>()][Type] = true;
            RegisterItemDrop(ModContent.ItemType<AtomicDirtBlock>());
            AddMapEntry(new Color(50, 255, 50), CreateMapEntryName());
            DustType = DustID.GrassBlades;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            float pulse = 0.8f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.3f;
            r = 0.05f; g = 0.6f * pulse; b = 0.05f;
        }

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            Tile tile = Main.tile[i, j];
            bool IsSolid(int x, int y)
            {
                if (!WorldGen.InWorld(x, y)) return false;
                Tile t = Main.tile[x, y];
                return t.HasTile && (t.TileType == Type || t.TileType == ModContent.TileType<AtomicDirtTile>());
            }

            bool up = IsSolid(i, j - 1);
            bool down = IsSolid(i, j + 1);
            bool left = IsSolid(i - 1, j);
            bool right = IsSolid(i + 1, j);

            int frameX = 1; int frameY = 1;
            if (!up) frameY = 0; else if (!down) frameY = 2; else frameY = 1;

            if (frameY == 0) { if (!left) frameX = 0; else if (!right) frameX = 2; else frameX = 1; }
            else if (frameY == 2) { if (!left) frameX = 0; else if (!right) frameX = 2; else frameX = 1; }
            else { if (!left && !right) frameX = 1; else if (!left) frameX = 0; else if (!right) frameX = 2; else frameX = 1; }

            tile.TileFrameX = (short)(frameX * 18);
            tile.TileFrameY = (short)(frameY * 18);
            return false;
        }

        public override void RandomUpdate(int i, int j)
        {
            if (AtomicConfig.Instance.TuftSpreadSpeed == SpreadSpeed.Desligado) return;

            if (!Main.tile[i, j - 1].HasTile && Main.rand.NextBool(3))
            {
                int style = RadiationSpreadSystem.GetRandomGrassStyle();
                bool placed = WorldGen.PlaceTile(i, j - 1, ModContent.TileType<AtomicFloorGrass>(), mute: true, forced: true, plr: -1, style: style);
                if (placed) WorldGen.SquareTileFrame(i, j - 1, true);
            }
        }
    }

    public class AtomicStone : ModTile
    {
        public override string Texture => "Atomic/Assets/Sprites/ModBiomes/Radiations/Blocks/Tiles/AtomicStone";
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true; Main.tileMergeDirt[Type] = true; Main.tileBlockLight[Type] = true; Main.tileLighted[Type] = true; Main.tileMerge[Type][Type] = true;
            DustType = DustID.GreenTorch; AddMapEntry(new Color(50, 255, 50), CreateMapEntryName()); MineResist = 2f;
            RegisterItemDrop(ModContent.ItemType<AtomicStoneItem>());
        }
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            float pulse = 0.8f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.3f;
            r = 0.05f; g = 0.8f * pulse; b = 0.05f;
        }
    }

    public class AtomicSand : ModTile
    {
        public override string Texture => "Atomic/Assets/Sprites/ModBiomes/Radiations/Blocks/Tiles/AtomicSand";
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true; Main.tileMergeDirt[Type] = true; Main.tileBlockLight[Type] = true; Main.tileLighted[Type] = true; Main.tileMerge[Type][Type] = true; Main.tileSand[Type] = true;
            DustType = DustID.Sand; AddMapEntry(new Color(50, 255, 50), CreateMapEntryName()); MineResist = 1f;
            RegisterItemDrop(ModContent.ItemType<AtomicSandItem>());
        }
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            float pulse = 0.8f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.3f;
            r = 0.05f; g = 0.8f * pulse; b = 0.05f;
        }
    }

    public class AtomicVines : ModTile
    {
        public override string Texture => "Atomic/Assets/Sprites/ModBiomes/Radiations/Blocks/Tiles/AtomicVines";
        public override void SetStaticDefaults()
        {
            Main.tileCut[Type] = true; Main.tileBlockLight[Type] = true; Main.tileLighted[Type] = true; Main.tileMerge[Type][Type] = true;
            TileID.Sets.Grass[Type] = true; Main.tileAxe[Type] = true; TileID.Sets.IsVine[Type] = true; TileID.Sets.BreakableWhenPlacing[Type] = true; TileID.Sets.VineThreads[Type] = true;
            DustType = DustID.GrassBlades; AddMapEntry(new Color(50, 255, 50), CreateMapEntryName());
        }
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            float pulse = 0.8f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.3f;
            r = 0.05f; g = 0.6f * pulse; b = 0.05f;
        }
        public override bool KillSound(int i, int j, bool fail)
        {
            if (!fail) SoundEngine.PlaySound(SoundID.Grass with { PitchVariance = 0.2f }, new Vector2(i * 16, j * 16));
            return false;
        }
        public override void RandomUpdate(int i, int j)
        {
            if (WorldGen.InWorld(i, j + 1) && !Main.tile[i, j + 1].HasTile)
            {
                if (Main.rand.NextBool(6))
                {
                    WorldGen.PlaceTile(i, j + 1, Type, mute: true, forced: true);
                    WorldGen.SquareTileFrame(i, j + 1, true);
                    var sys = ModContent.GetInstance<RadiationSpreadSystem>();
                    if (sys != null) sys.RegisterTile(i, j + 1);
                }
            }
        }
    }

    public class NuclearTile : ModTile
    {
        public override string Texture => "Atomic/Assets/Sprites/ModBiomes/Radiations/Blocks/Tiles/NuclearOreTile";
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true; Main.tileMergeDirt[Type] = true; Main.tileBlockLight[Type] = true; Main.tileLighted[Type] = true; Main.tileFrameImportant[Type] = false; Main.tileMerge[Type][Type] = true;
            DustType = DustID.GreenTorch; AddMapEntry(new Color(50, 255, 50), CreateMapEntryName()); MineResist = 2f;
            RegisterItemDrop(ModContent.ItemType<NuclearItem>());
        }
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            float pulse = 0.8f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.3f;
            r = 0.1f; g = 1.2f * pulse; b = 0.1f;
        }
    }

    // ==============================================================================
    // 5. ITENS DOS BLOCOS
    // ==============================================================================
    public class AtomicDirtBlock : ModItem
    {
        public override string Texture => "Atomic/Assets/Sprites/ModBiomes/Radiations/Blocks/Ordinary/AtomicDirtBlock";
        public override void SetDefaults()
        {
            Item.width = 16; Item.height = 16; Item.maxStack = 9999; Item.useTurn = true; Item.autoReuse = true;
            Item.useAnimation = 15; Item.useTime = 10; Item.useStyle = ItemUseStyleID.Swing; Item.consumable = true;
            Item.createTile = ModContent.TileType<AtomicDirtTile>();
            Item.SetNameOverride("Terra Atômica");
        }
    }

    public class AtomicStoneItem : ModItem
    {
        public override string Texture => "Atomic/Assets/Sprites/ModBiomes/Radiations/Blocks/Ordinary/AtomicStoneBlock";
        public override void SetDefaults()
        {
            Item.width = 16; Item.height = 16; Item.maxStack = 9999; Item.useTurn = true; Item.autoReuse = true;
            Item.useAnimation = 15; Item.useTime = 10; Item.useStyle = ItemUseStyleID.Swing; Item.consumable = true;
            Item.createTile = ModContent.TileType<AtomicStone>();
            Item.SetNameOverride("Pedra Atômica");
        }
    }

    public class AtomicSandItem : ModItem
    {
        public override string Texture => "Atomic/Assets/Sprites/ModBiomes/Radiations/Blocks/Ordinary/AtomicSandBlock";
        public override void SetDefaults()
        {
            Item.width = 16; Item.height = 16; Item.maxStack = 9999; Item.useTurn = true; Item.autoReuse = true;
            Item.useAnimation = 15; Item.useTime = 10; Item.useStyle = ItemUseStyleID.Swing; Item.consumable = true;
            Item.createTile = ModContent.TileType<AtomicSand>();
            Item.SetNameOverride("Areia Atômica");
        }
    }

    public class NuclearItem : ModItem
    {
        public override string Texture => "Atomic/Assets/Sprites/ModBiomes/Radiations/Blocks/Ores/NuclearOreItem";
        public override void SetDefaults()
        {
            Item.width = 16; Item.height = 16; Item.maxStack = 9999; Item.useTurn = true; Item.autoReuse = true; Item.useAnimation = 15; Item.useTime = 10; Item.useStyle = ItemUseStyleID.Swing; Item.consumable = true;
            Item.createTile = ModContent.TileType<NuclearTile>();
            Item.SetNameOverride("Minério Nuclear");
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine line = new TooltipLine(Mod, "RadiationTooltip", "Você sente sua mão derreter ao segurar isso");
            line.OverrideColor = new Color(50, 255, 50);
            tooltips.Add(line);
        }
    }

    // ==============================================================================
    // 6. DEBUFF E LÓGICA DO JOGADOR
    // ==============================================================================
    public class RadiationDebuff : ModBuff
    {
        public override string Texture => "Terraria/Images/Buff_20";
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true; Main.pvpBuff[Type] = true; Main.buffNoSave[Type] = false;
        }
        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            buffName = "Radiação"; tip = ""; rare = 0;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            RadiationPlayer modPlayer = player.GetModPlayer<RadiationPlayer>();
            int damage = 10 + (modPlayer.radiationTime / 60);
            player.lifeRegen -= damage * 2;
        }
    }

    public class RadiationPlayer : ModPlayer
    {
        public float radiationLevel;
        public int radiationTime;

        public override void PostUpdate()
        {
            Point feetTile = Player.Bottom.ToTileCoordinates();
            Point centerTile = Player.Center.ToTileCoordinates();
            bool isTouchingNuclear = false;

            if (WorldGen.InWorld(feetTile.X, feetTile.Y) && Main.tile[feetTile.X, feetTile.Y].HasTile && IsRadioactiveTile(Main.tile[feetTile.X, feetTile.Y].TileType)) isTouchingNuclear = true;
            if (WorldGen.InWorld(centerTile.X, centerTile.Y) && Main.tile[centerTile.X, centerTile.Y].HasTile && IsRadioactiveTile(Main.tile[centerTile.X, centerTile.Y].TileType)) isTouchingNuclear = true;

            if (isTouchingNuclear && !Player.fireWalk) Player.AddBuff(BuffID.OnFire, 180);

            bool standingOnNuclear = false;
            for (int x = feetTile.X - 1; x <= feetTile.X + 1; x++)
            {
                for (int y = feetTile.Y - 1; y <= feetTile.Y + 2; y++)
                {
                    if (WorldGen.InWorld(x, y))
                    {
                        Tile tile = Main.tile[x, y];
                        if (tile.HasTile && IsRadioactiveTile(tile.TileType)) { standingOnNuclear = true; break; }
                    }
                }
                if (standingOnNuclear) break;
            }

            if (standingOnNuclear)
            {
                radiationLevel += 0.004f;
                if (radiationLevel >= 1f) { radiationLevel = 1f; Player.AddBuff(ModContent.BuffType<RadiationDebuff>(), 300); }
            }
            else
            {
                radiationLevel -= 0.02f;
                if (radiationLevel < 0f) radiationLevel = 0f;
            }

            if (Player.HasBuff(ModContent.BuffType<RadiationDebuff>()))
            {
                radiationTime++;
                Lighting.AddLight(Player.Center, 0.5f, 2.0f, 0.5f);
                if (Main.rand.NextBool(3))
                {
                    Dust dust = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.GreenTorch);
                    dust.velocity *= 0.2f; dust.noGravity = true;
                }
            }
            else radiationTime = 0;
        }

        private bool IsRadioactiveTile(int type)
        {
            return type == ModContent.TileType<NuclearTile>() ||
                   type == ModContent.TileType<AtomicDirtTile>() ||
                   type == ModContent.TileType<AtomicStone>() ||
                   type == ModContent.TileType<AtomicSand>() ||
                   type == ModContent.TileType<AtomicVines>() ||
                   type == ModContent.TileType<RadioactiveGrass>() ||
                   type == ModContent.TileType<AtomicFloorGrass>();
        }
    }

    // ==============================================================================
    // 7. GLOBAL CLASSES E COMANDO
    // ==============================================================================
    public class RadiationGlobalNPC : GlobalNPC
    {
        public override void GetChat(NPC npc, ref string chat)
        {
            if (npc.type == NPCID.Dryad)
            {
                string lowerChat = chat.ToLower();
                if (lowerChat.Contains("carmesim") || lowerChat.Contains("corrupção") || lowerChat.Contains("sagrado") || lowerChat.Contains("crimson") || lowerChat.Contains("corruption") || lowerChat.Contains("hallow") || lowerChat.Contains("status"))
                {
                    var sys = ModContent.GetInstance<RadiationSpreadSystem>();
                    if (sys != null && sys.RadiationPercentage > 0f)
                    {
                        float percent = sys.RadiationPercentage * 100f;
                        chat += $"\n\nA radiação já cobriu {percent:F2}% do mundo.";
                    }
                }
            }
        }
    }

    public class RadiationGlobalTile : GlobalTile
    {
        public override void PlaceInWorld(int i, int j, int type, Item item)
        {
            var sys = ModContent.GetInstance<RadiationSpreadSystem>();
            if (sys != null && sys.IsAnyRadioactiveBlock(type))
            {
                sys.RegisterTile(i, j);
            }
        }
    }

    public class RadiationCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;
        public override string Command => "resetrad";
        public override string Usage => "/resetrad";
        public override string Description => "Reseta a radiação do mundo para você testar a bomba novamente.";
        public override void Action(CommandCaller caller, string input, string[] args)
        {
            var sys = ModContent.GetInstance<RadiationSpreadSystem>();
            sys.ResetRadiation();
            Main.NewText("Radiação resetada! O próximo Fatman vai criar radiação.", Color.LimeGreen);
        }
    }

    // ==============================================================================
    // 8. VISUAL (GLOW E HUD)
    // ==============================================================================
    public class RadiationGlowLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.ArmorLongCoat);
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.drawPlayer.GetModPlayer<RadiationPlayer>().radiationTime > 0;
        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            foreach (DrawData data in drawInfo.DrawDataCache)
            {
                DrawData greenData = data;
                greenData.color = Color.LimeGreen * 0.7f;
                greenData.Draw(Main.spriteBatch);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }

    public class RadiationUI : ModSystem
    {
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int resourceBars = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
            if (resourceBars != -1)
            {
                layers.Insert(resourceBars + 1, new LegacyGameInterfaceLayer("Atomic: Radiation Bar",
                    delegate
                    {
                        Player player = Main.LocalPlayer;
                        RadiationPlayer modPlayer = player.GetModPlayer<RadiationPlayer>();
                        if (modPlayer.radiationLevel > 0f)
                        {
                            Vector2 pos = player.Top - Main.screenPosition - new Vector2(0, 30);
                            Texture2D pixel = TextureAssets.MagicPixel.Value;
                            Main.spriteBatch.Draw(pixel, new Rectangle((int)pos.X - 20, (int)pos.Y, 40, 6), Color.Black * 0.7f);
                            int width = (int)(40 * modPlayer.radiationLevel);
                            Main.spriteBatch.Draw(pixel, new Rectangle((int)pos.X - 20, (int)pos.Y, width, 6), Color.LimeGreen);
                            Main.spriteBatch.Draw(pixel, new Rectangle((int)pos.X - 21, (int)pos.Y - 1, 42, 1), Color.White * 0.5f);
                            Main.spriteBatch.Draw(pixel, new Rectangle((int)pos.X - 21, (int)pos.Y + 6, 42, 1), Color.White * 0.5f);
                        }
                        return true;
                    }, InterfaceScaleType.Game)
                );
            }
        }
    }
}