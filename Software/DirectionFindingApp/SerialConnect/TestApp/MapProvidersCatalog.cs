using System;
using System.Collections.Generic;
using System.Linq;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.Projections;

namespace TestApp
{
    public sealed class MapProviderDescriptor
    {
        public required string Id { get; init; }
        public required string DisplayName { get; init; }
        public required GMapProvider Provider { get; init; }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public static class MapProvidersCatalog
    {
        private static readonly IReadOnlyList<MapProviderDescriptor> Providers = new List<MapProviderDescriptor>
        {
            new() { Id = "OpenStreetMap", DisplayName = "OpenStreetMap", Provider = OpenStreetMapTileProvider.Instance },
            new() { Id = "OpenStreetMap_HOT", DisplayName = "OpenStreetMap HOT", Provider = OpenStreetMapHotTileProvider.Instance },
            new() { Id = "Esri_WorldTopoMap", DisplayName = "Esri World Topo", Provider = EsriWorldTopoTileProvider.Instance },
            new() { Id = "OpenTopoMap", DisplayName = "OpenTopoMap", Provider = OpenTopoMapTileProvider.Instance },
            new() { Id = "Esri_WorldImagery", DisplayName = "Esri World Imagery", Provider = EsriWorldImageryTileProvider.Instance },
            new() { Id = "TopPlusOpen_Color", DisplayName = "TopPlusOpen Color", Provider = TopPlusOpenColorTileProvider.Instance },
        };

        public static IReadOnlyList<MapProviderDescriptor> GetAll()
        {
            return Providers;
        }

        public static MapProviderDescriptor GetDefault()
        {
            return Providers[0];
        }

        public static MapProviderDescriptor Resolve(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return GetDefault();
            }

            return Providers.FirstOrDefault(provider => string.Equals(provider.Id, id, StringComparison.OrdinalIgnoreCase))
                ?? GetDefault();
        }
    }

    internal abstract class TemplateMapProviderBase : GMapProvider
    {
        private readonly GMapProvider[] _overlays;

        protected TemplateMapProviderBase()
        {
            _overlays = new GMapProvider[] { this };
        }

        public override PureProjection Projection => MercatorProjection.Instance;

        public override GMapProvider[] Overlays => _overlays;

        protected abstract string UrlTemplate { get; }

        protected virtual string[] Servers => new[] { string.Empty };

        public override PureImage GetTileImage(GPoint pos, int zoom)
        {
            string url = MakeTileImageUrl(pos, zoom);
            return GetTileImageUsingHttp(url);
        }

        protected virtual string MakeTileImageUrl(GPoint pos, int zoom)
        {
            string[] servers = Servers.Length == 0 ? new[] { string.Empty } : Servers;
            string server = servers[GetServerNum(pos, servers.Length)];

            return UrlTemplate
                .Replace("{s}", server, StringComparison.Ordinal)
                .Replace("{z}", zoom.ToString(), StringComparison.Ordinal)
                .Replace("{x}", pos.X.ToString(), StringComparison.Ordinal)
                .Replace("{y}", pos.Y.ToString(), StringComparison.Ordinal);
        }
    }

    internal sealed class OpenStreetMapTileProvider : TemplateMapProviderBase
    {
        public static readonly OpenStreetMapTileProvider Instance = new();

        public override Guid Id => new("b2d7b878-45bc-4d19-b111-70dc7d58a001");

        public override string Name => "OpenStreetMap";

        protected override string UrlTemplate => "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png";

        protected override string[] Servers => new[] { "a", "b", "c" };

        private OpenStreetMapTileProvider()
        {
        }
    }

    internal sealed class OpenStreetMapHotTileProvider : TemplateMapProviderBase
    {
        public static readonly OpenStreetMapHotTileProvider Instance = new();

        public override Guid Id => new("f766b9fa-e44e-4ae7-b4f7-fc3f13051002");

        public override string Name => "OpenStreetMap_HOT";

        protected override string UrlTemplate => "https://{s}.tile.openstreetmap.fr/hot/{z}/{x}/{y}.png";

        protected override string[] Servers => new[] { "a", "b", "c" };

        private OpenStreetMapHotTileProvider()
        {
        }
    }

    internal sealed class EsriWorldTopoTileProvider : TemplateMapProviderBase
    {
        public static readonly EsriWorldTopoTileProvider Instance = new();

        public override Guid Id => new("be9e09de-8f51-46fd-8410-b999c9d53003");

        public override string Name => "Esri_WorldTopoMap";

        protected override string UrlTemplate => "https://server.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{z}/{y}/{x}.png";

        private EsriWorldTopoTileProvider()
        {
        }
    }

    internal sealed class OpenTopoMapTileProvider : TemplateMapProviderBase
    {
        public static readonly OpenTopoMapTileProvider Instance = new();

        public override Guid Id => new("59205b7f-0592-4f6b-b3d7-aa87a7d14004");

        public override string Name => "OpenTopoMap";

        protected override string UrlTemplate => "https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png";

        protected override string[] Servers => new[] { "a", "b", "c" };

        private OpenTopoMapTileProvider()
        {
        }
    }

    internal sealed class EsriWorldImageryTileProvider : TemplateMapProviderBase
    {
        public static readonly EsriWorldImageryTileProvider Instance = new();

        public override Guid Id => new("0ea7dc57-d311-4f3e-9c36-5eeed8fb5005");

        public override string Name => "Esri_WorldImagery";

        protected override string UrlTemplate => "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}.png";

        private EsriWorldImageryTileProvider()
        {
        }
    }

    internal sealed class TopPlusOpenColorTileProvider : TemplateMapProviderBase
    {
        public static readonly TopPlusOpenColorTileProvider Instance = new();

        public override Guid Id => new("8edfd02d-c686-4f16-9eca-4dcf7f6bd006");

        public override string Name => "TopPlusOpen_Color";

        protected override string UrlTemplate => "https://sgx.geodatenzentrum.de/wmts_topplus_open/tile/1.0.0/web/default/WEBMERCATOR/{z}/{y}/{x}.png";

        private TopPlusOpenColorTileProvider()
        {
        }
    }
}