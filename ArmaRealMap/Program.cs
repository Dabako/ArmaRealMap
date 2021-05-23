﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using ArmaRealMap.Buildings;
using ArmaRealMap.ElevationModel;
using ArmaRealMap.Geometries;
using ArmaRealMap.GroundTextureDetails;
using ArmaRealMap.Libraries;
using ArmaRealMap.Osm;
using ArmaRealMap.Roads;
using ClipperLib;
using Microsoft.Win32;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using OsmSharp;
using OsmSharp.Complete;
using OsmSharp.Db;
using OsmSharp.Db.Impl;
using OsmSharp.Geo;
using OsmSharp.Streams;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ArmaRealMap
{
    class Program
    {
        static void Main(string[] args)
        {
            /*var clipper = new ClipperOffset();
            clipper.AddPath(new List<IntPoint>()
            {
                new IntPoint(0,0),
                new IntPoint(100,100),
                new IntPoint(200,100),
            }, JoinType.jtSquare, EndType.etOpenSquare);
            var tree = new List<List<IntPoint>>();
            clipper.Execute(ref tree, 1);*/
            /*
            var img = (Image<Rgba32>)Image.Load(@"C:\Users\Julien\source\repos\jetelain\Mali\mapdata\MALI_HM.png");
            var img2 = (Image<Rgba32>)Image.Load(@"C:\Users\Julien\source\repos\jetelain\Mali\mapdata\MALI_HM_CPLT.png");

            var grid = new ElevationGrid(new MapInfos()
            {
                CellSize = 5,
                Size = 8192,
                StartPointUTM = new CoordinateSharp.UniversalTransverseMercator("31N", 200000, 0)
            });

            var minElevation = 1f;
            var maxElevation = 300f;

            for(int x = 0; x< img.Width; ++x)
            {
                for (int y = 0; y < img.Height; ++y)
                {
                    var c2 = img2[x, y];
                    bool isCloseWater = c2.R > 250 && c2.G < 128 && c2.B < 128;
                    bool isWater = c2.B > 250 && c2.G < 128 && c2.R < 128;
                    var theory = minElevation + (img[x, y].R * maxElevation / 255);
                    if ( isWater)
                    {
                        grid.elevationGrid[x, img.Height - y - 1] = -1;
                    }
                    else if(isCloseWater && theory < 1)
                    {
                        grid.elevationGrid[x, img.Height - y - 1] = 1;
                    }
                    else
                    {
                        grid.elevationGrid[x, img.Height - y - 1] = theory;
                    }
                }
            }

            for (int x = 1; x < img.Width-1; ++x)
            {
                for (int y = 1; y < img.Height-1; ++y)
                {
                    var c2 = img2[x, y];
                    bool isCloseWater = c2.R > 250 && c2.G < 128 && c2.B < 128;
                    bool isWater = c2.B > 250 && c2.G < 128 && c2.R < 128;
                    if (isWater || isCloseWater)
                    {
                        var a = grid.elevationGrid[x-1, img.Height - y - 1];
                        var b = grid.elevationGrid[x+1, img.Height - y - 1];
                        var c = grid.elevationGrid[x, img.Height - y - 1];
                        var d = grid.elevationGrid[x, img.Height - y - 2];
                        var e = grid.elevationGrid[x, img.Height - y];
                        grid.elevationGrid[x, img.Height - y - 1] = (a+b+c+d+e)/5f;
                    }
                }
            }

            grid.SavePreviewToPng(@"C:\Users\Julien\source\repos\jetelain\Mali\mapdata\mali_hm_preview.png");
            grid.SaveToAsc(@"C:\Users\Julien\source\repos\jetelain\Mali\mapdata\mali.asc");*/

            EnsureProjectDrive();

            Console.Title = "ArmaRealMap";

            var config = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));

            Trace.Listeners.Clear();

            Trace.Listeners.Add(new TextWriterTraceListener(@"arm.log"));

            Trace.WriteLine("----------------------------------------------------------------------------------------------------");

            var olibs = new ObjectLibraries();
            olibs.Load(config);
            File.WriteAllText(Path.Combine(config.Target?.Terrain ?? string.Empty, "library.sqf"), olibs.TerrainBuilder.GetAllSqf());

            //GDTConfigBuilder.PrepareGDT(config);

            var data = new MapData();

            data.Config = config;

            var area = MapInfos.Create(config);

            data.MapInfos = area;

            data.Elevation = ElevationGridBuilder.LoadOrGenerateElevationGrid(config, area);

            //SatelliteRawImage(config, area);

            BuildLand(config, data, area, olibs);

            Trace.WriteLine("----------------------------------------------------------------------------------------------------");
            Trace.Flush();
        }

#pragma warning disable CA1416 // Valider la compatibilité de la plateforme
        private static void EnsureProjectDrive()
        {
            if (!Directory.Exists("P:\\"))
            {
                Console.WriteLine("Mount project drive");
                string path = "";
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 233800"))
                {
                    if (key != null)
                    {
                        path = (key.GetValue("InstallLocation") as string) ?? path;
                    }
                }
                if (!string.IsNullOrEmpty(path))
                {
                    var processs = Process.Start(Path.Combine(path, @"WorkDrive\WorkDrive.exe"), "/Mount /y");
                    processs.WaitForExit();
                }
            }
        }
#pragma warning restore CA1416 // Valider la compatibilité de la plateforme


        private static void SatelliteRawImage(Config config, MapInfos area)
        {
            var rawSat = Path.Combine(config.Target?.Terrain ?? string.Empty, "sat-raw.png");
            if (!File.Exists(rawSat))
            {
                SatelliteImageBuilder.BuildSatImage(area, rawSat);
            }
        }
        private static void BuildLand(Config config, MapData data, MapInfos area, ObjectLibraries olibs)
        {
            var usedObjects = new HashSet<string>();
            using (var fileStream = File.OpenRead(config.OSM))
            {
                Console.WriteLine("Load OSM data...");
                var source = new PBFOsmStreamSource(fileStream);
                var db = new SnapshotDb(new MemorySnapshotDb(source));

                Console.WriteLine("Crop OSM data...");
                OsmStreamSource filtered = CropDataToArea(area, source);

                //RenderCitiesNames(config, area, filtered);

                RoadsBuilder.Roads(data, filtered, db, config);

                var shapes = OsmCategorizer.GetShapes(db, filtered);

                //ExportForestAsShapeFile(area, toRender);

                //new FillShapeWithObjects(area, olibs.Libraries.FirstOrDefault(l => l.Category == ObjectCategory.ForestTree))
                //    .MakeForest(shapes, filtered, db);

                //PlaceIsolatedTrees(area, olibs, filtered);

                BuildingsBuilder.PlaceBuildings(data, olibs, shapes);

                //MakeLakeDeeper(data, shapes);

                //DrawShapes(area, shapes);
            }


            var libs = olibs.TerrainBuilder.Libraries.Where(l => usedObjects.Any(o => l.Template.Any(t => t.Name == o))).Distinct().ToList();
            File.WriteAllLines("required_tml.txt", libs.Select(t => t.Name));
        }

        private static void RenderCitiesNames(Config config, MapInfos area, OsmStreamSource filtered)
        {
            var places = filtered.Where(o => o.Type == OsmGeoType.Node && o.Tags.ContainsKey("place")).ToList();

            var id = 0;
            var sb = new StringBuilder();
            foreach (OsmSharp.Node place in places)
            {
                var kind = ToArmaKind(place.Tags.GetValue("place"));
                if (kind != null)
                {
                    var name = place.Tags.GetValue("name");
                    var pos = area.LatLngToTerrainPoint(place);

                    if (area.IsInside(pos))
                    {
                        sb.AppendLine(FormattableString.Invariant($@"class Item{id}
{{
    name = ""{name}"";
	position[]={{{pos.X:0.00},{pos.Y:0.00}}};
	type=""{kind}"";
	radiusA=500;
	radiusB=500;
	angle=0;
}};"));
                        id++;
                    }
                }
            }

            File.WriteAllText(Path.Combine(config.Target?.Config ?? string.Empty, "names.hpp"), sb.ToString());
        }

        private static string ToArmaKind(string place)
        {
            switch(place)
            {
                case "city": return "NameCityCapital";
                case "town": return "NameCity";
                case "village": return "NameVillage";
                case "hamlet": return "NameLocal";
            }
            return null;
        }

        private static OsmStreamSource CropDataToArea(MapInfos area, PBFOsmStreamSource source)
        {
            var left = (float)Math.Min(area.SouthWest.Longitude.ToDouble(), area.NorthWest.Longitude.ToDouble());
            var top = (float)Math.Max(area.NorthEast.Latitude.ToDouble(), area.NorthWest.Latitude.ToDouble());
            var right = (float)Math.Max(area.SouthEast.Longitude.ToDouble(), area.NorthEast.Longitude.ToDouble());
            var bottom = (float)Math.Min(area.SouthEast.Latitude.ToDouble(), area.SouthWest.Latitude.ToDouble());
            return source.FilterBox(left, top, right, bottom, true);
        }

        private static void ExportForestAsShapeFile(MapInfos area, List<OsmShape> toRender)
        {
            var forest = toRender.Where(f => f.Category == OsmShapeCategory.Forest).ToList();
            var attributesTable = new AttributesTable();
            var features = forest.SelectMany(f => GeometryHelper.LatLngToTerrainPolygon(area, f.Geometry)).Select(f => new Feature(f, attributesTable)).ToList();
            var header = ShapefileDataWriter.GetHeader(features.First(), features.Count);
            var shapeWriter = new ShapefileDataWriter("forest.shp", new GeometryFactory())
            {
                Header = header
            };
            shapeWriter.Write(features);
        }

        private static IEnumerable<LineString> ToLineString(MapInfos area, Geometry geometry)
        {
            if (geometry.OgcGeometryType == OgcGeometryType.LineString)
            {
                var points = area.LatLngToTerrainPoints(((LineString)geometry).Coordinates).Where(p => area.IsInside(p)).ToList();
                if (points.Count > 1)
                {
                    return new[] { new LineString(points.Select(p => new NetTopologySuite.Geometries.Coordinate(p.X + 200000, p.Y)).ToArray())};
                }
                return new LineString[0];
            }
            throw new ArgumentException(geometry.OgcGeometryType.ToString());
        }





        private static void PlaceIsolatedTrees(MapInfos area, ObjectLibraries olibs, OsmStreamSource filtered)
        {
            var candidates = olibs.Libraries.Where(l => l.Category == ObjectCategory.IsolatedTree).SelectMany(l => l.Objects).ToList();
            var result = new StringBuilder();

            var trees = filtered.Where(o => o.Type == OsmGeoType.Node && OsmCategorizer.Get(o.Tags, "natural") == "tree").ToList();
            foreach (Node tree in trees)
            {
                var pos = area.LatLngToTerrainPoint(tree);  
                if (area.IsInside(pos))
                {
                    var random = new Random((int)Math.Truncate(pos.X + pos.Y));
                    var obj = candidates[random.Next(0, candidates.Count)];
                    result.Append(new TerrainObject(obj, pos, (float)(random.NextDouble() * 360.0)).ToString(area));
                    result.AppendLine();
                }
            }
            File.WriteAllText("trees.txt", result.ToString());
        }


        private static void DrawShapes(MapInfos area, List<OsmShape> toRender)
        {
            var shapes = toRender.Count;
            var report = new ProgressReport("DrawShapes", shapes);
            using (var img = new Image<Rgb24>(area.Size * area.CellSize, area.Size * area.CellSize, TerrainMaterial.GrassShort.Color))
            {
                foreach (var item in toRender.OrderByDescending(e => e.Category.GroundTexturePriority))
                {
                    OsmDrawHelper.Draw(area, img, new SolidBrush(item.Category.GroundTextureColorCode), item);
                    report.ReportOneDone();
                }
                report.TaskDone();
                Console.WriteLine("SavePNG");
                img.Save("osm4.png");
            }
        }


    }
}
