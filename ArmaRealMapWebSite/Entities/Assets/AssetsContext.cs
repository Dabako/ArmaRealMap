﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using ArmaRealMapWebSite.Entities.Assets;
using ArmaRealMap.Core.ObjectLibraries;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArmaRealMapWebSite.Entities.Assets
{
    public class AssetsContext : DbContext
    {
        public AssetsContext(DbContextOptions<AssetsContext> options)
            : base(options)
        {
        }

        public DbSet<Asset> Assets { get; set; }
        public DbSet<AssetPreview> AssetPreviews { get; set; }
        public DbSet<GameMod> GameMods { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Asset>().ToTable(nameof(Asset));
            modelBuilder.Entity<AssetPreview>().ToTable(nameof(AssetPreview));
            modelBuilder.Entity<GameMod>().ToTable(nameof(GameMod));

            modelBuilder.Entity<ObjectLibrary>().ToTable(nameof(ObjectLibrary));
            modelBuilder.Entity<ObjectLibraryAsset>().ToTable(nameof(ObjectLibraryAsset));
        }

        private static readonly Regex TextLine = new Regex(@"\[([a-zA-Z/0-9\.\-]+),""([^""]+)"",""([^""]+)"",\[\[([0-9\.\-]+),([0-9\.\-]+),([0-9\.\-]+)\],\[([0-9\.\-]+),([0-9\.\-]+),([0-9\.\-]+)\],([0-9\.\-]+)\]\]", RegexOptions.Compiled);

        internal void LoadFromXData()
        {
            //Load("JBAD", "e1.txt", TerrainRegion.Sahel | TerrainRegion.NearEast, AssetCategory.Structure);
            //Load("ARM", "e2.txt", TerrainRegion.Sahel, AssetCategory.Building);
            //Load("Base game", "e3.txt", TerrainRegion.Unknown, AssetCategory.Rock);

            //var files = Directory.GetFiles(@"xdata", "*.json");
            //var assets = Assets.ToList();
            //foreach (var json in files)
            //{
            //    var jlib = JsonSerializer.Deserialize<JsonObjectLibrary>(File.ReadAllText(json), options);

            //    Add(new ObjectLibrary()
            //    {
            //        Density = jlib.Density,
            //        ObjectCategory = jlib.Category,
            //        Probability = 1,
            //        TerrainRegion = jlib.Terrain ?? TerrainRegion.Unknown,
            //        Assets = jlib.Objects.Where(o => assets.Any(a => string.Equals(a.Name,o.Name, StringComparison.OrdinalIgnoreCase))).Select(o => new ObjectLibraryAsset()
            //        {
            //            Probability = o.PlacementProbability,
            //            MaxZ = o.MaxZ,
            //            MinZ = o.MinZ,
            //            PlacementRadius = o.PlacementRadius,
            //            ReservedRadius = o.ReservedRadius,
            //            Asset = assets.OrderBy(a => a.AssetID).Last(a => string.Equals(a.Name, o.Name, StringComparison.OrdinalIgnoreCase))
            //        }).ToList()
            //    }); 
            //}
            //SaveChanges();
        }



        private static readonly JsonSerializerOptions options = new JsonSerializerOptions
        {
            Converters ={
                new JsonStringEnumConverter()
            },
            WriteIndented = true
        };

        private void Load(string gameModName, string file, TerrainRegion region, AssetCategory def)
        {
            var name = Path.Combine("xdata", file);
            if (!File.Exists(name))
            {
                return;
            }

            var gameMod = GameMods.FirstOrDefault(n => n.Name == gameModName);
            if (gameMod == null)
            {
                gameMod = new GameMod() { Name = gameModName };
                GameMods.Add(gameMod);
                SaveChanges();
            }

            foreach (var line in File.ReadAllLines(name))
            {
                if (line.Contains("_ruins_", StringComparison.OrdinalIgnoreCase) || line.Contains("_damaged_", StringComparison.OrdinalIgnoreCase)) continue;
                
                var match = TextLine.Match(line);
                if (match.Success)
                {
                    var model = match.Groups[3].Value;

                    byte[] thumbData, jpegData;
                    GetPreviews(Path.Combine("xdata", match.Groups[1].Value + ".png"), out thumbData, out jpegData);

                    var existing = Assets.FirstOrDefault(a => a.ModelPath == model);
                    if (existing == null)
                    {
                        var minX = float.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
                        var minY = float.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture);
                        var minZ = float.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture);
                        var maxX = float.Parse(match.Groups[7].Value, CultureInfo.InvariantCulture);
                        var maxY = float.Parse(match.Groups[8].Value, CultureInfo.InvariantCulture);
                        var maxZ = float.Parse(match.Groups[9].Value, CultureInfo.InvariantCulture);
                        var asset = new Asset()
                        {
                            GameMod = gameMod,
                            ClassName = match.Groups[2].Value,
                            ModelPath = model,
                            MinX = minX,
                            MinY = minY,
                            MinZ = minZ,
                            MaxX = maxX,
                            MaxY = maxY,
                            MaxZ = maxZ,
                            BoundingSphereDiameter = float.Parse(match.Groups[10].Value, CultureInfo.InvariantCulture),
                            Width = maxX - minX,
                            Depth = maxY - minY,
                            Height = maxZ - minZ,
                            CX = 0,
                            CY = 0,
                            CZ = 0,
                            TerrainRegions = region,
                            AssetCategory = GuessCategory(match.Groups[3].Value, def),
                            Name = Path.GetFileNameWithoutExtension(match.Groups[3].Value.Replace('\\', Path.DirectorySeparatorChar)),
                            Previews = new List<AssetPreview>()
                            {
                                new AssetPreview() { Data = jpegData, Width = 1920, Height = 1080 },
                                new AssetPreview() { Data = thumbData, Width = 480, Height = 270 }
                            }
                        };
                        Assets.Add(asset);
                    }
                    else
                    {
                        UpdatePreview(jpegData, 1920, existing);
                        UpdatePreview(thumbData, 480, existing);
                    }
                }
            }
            SaveChanges();
        }

        private void UpdatePreview(byte[] data, int width, Asset existing)
        {
            var preview = AssetPreviews.First(a => a.AssetID == existing.AssetID && a.Width == width);
            preview.Data = data;
            Update(preview);
        }

        private static void GetPreviews(string shot, out byte[] thumbData, out byte[] jpegData)
        {
            using (var thumb = Image.Load(shot))
            {
                using (var stream = new MemoryStream())
                {
                    thumb.SaveAsJpeg(stream);
                    jpegData = stream.ToArray();
                }
                thumb.Mutate(i => i.Resize(480, 270));
                using (var stream = new MemoryStream())
                {
                    thumb.SaveAsPng(stream);
                    thumbData = stream.ToArray();
                }
            }
        }

        private AssetCategory GuessCategory(string value, AssetCategory def)
        {
            if (value.Contains("Houses", StringComparison.OrdinalIgnoreCase))
            {
                return AssetCategory.Building;
            }
            if (value.Contains("structures_", StringComparison.OrdinalIgnoreCase))
            {
                return AssetCategory.Structure;
            }
            if (value.Contains("rocks_", StringComparison.OrdinalIgnoreCase))
            {
                return AssetCategory.Rock;
            }
            if (value.Contains("clutter", StringComparison.OrdinalIgnoreCase))
            {
                return AssetCategory.Clutter;
            }
            if (value.Contains("bush", StringComparison.OrdinalIgnoreCase) || value.Contains("shrub", StringComparison.OrdinalIgnoreCase))
            {
                return AssetCategory.Bush;
            }
            if (value.Contains("tree", StringComparison.OrdinalIgnoreCase))
            {
                return AssetCategory.Tree;
            }
            return def;
        }

        public DbSet<ArmaRealMapWebSite.Entities.Assets.ObjectLibrary> ObjectLibrary { get; set; }

        public DbSet<ArmaRealMapWebSite.Entities.Assets.ObjectLibraryAsset> ObjectLibraryAsset { get; set; }
    }
}