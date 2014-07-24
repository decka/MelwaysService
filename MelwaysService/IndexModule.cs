using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Xml.Serialization;
using Nancy.ModelBinding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace MelwaysService
{
    using Nancy;

    public class IndexModule : NancyModule
    {
        public IndexModule()
        {
            Get["/"] = (parameters) => View["Index"];

            Post["/address/", true] = async (parameters, ct) =>
            {
                var model = this.Bind<AddressModel>();
                var address = model.Address;

                const string googleURL = @"https://maps.googleapis.com/maps/api/geocode/json?address=";
                const string melwaysURL = @"http://online.melway.com.au/melway/php/MelwayMapRefInfo.php?directory=Mel&cntlng={0}&cntlat={1}&scale=20000";

                var client = new HttpClient();
                var response = await client.GetAsync(googleURL + address);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                var deserialized = JsonConvert.DeserializeObject<RootObject>(content);

                var results = new List<MelwayResults>();

                foreach (var location in deserialized.results)
                {
                    response = await client.GetAsync(String.Format(melwaysURL, location.geometry.location.lng, location.geometry.location.lat));
                    response.EnsureSuccessStatusCode();
                    content = await response.Content.ReadAsStringAsync();

                    var xmlS = new XmlSerializer(typeof(Markers));
                    using (TextReader reader = new StringReader(content))
                    {
                        var markers = (Markers)xmlS.Deserialize(reader);
                        results.Add(new MelwayResults() { Address = location.formatted_address, MelwaysReference = markers.marker.Skip(1).First().COL1 + markers.marker.Skip(1).First().COL2 });
                    }
                }
                return JsonConvert.SerializeObject(results);
            };
        }
    }

    public class AddressModel
    {
        public string Address { get; set; }
    }

    #region Google API Class Structure
    public class AddressComponent
    {
        public string long_name { get; set; }
        public string short_name { get; set; }
        public List<string> types { get; set; }
    }

    public class Location
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Northeast
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Southwest
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Viewport
    {
        public Northeast northeast { get; set; }
        public Southwest southwest { get; set; }
    }

    public class Geometry
    {
        public Location location { get; set; }
        public string location_type { get; set; }
        public Viewport viewport { get; set; }
    }

    public class Result
    {
        public List<AddressComponent> address_components { get; set; }
        public string formatted_address { get; set; }
        public Geometry geometry { get; set; }
        public List<string> types { get; set; }
    }

    public class RootObject
    {
        public List<Result> results { get; set; }
        public string status { get; set; }
    }
    #endregion

    #region Melways API Class structure

    public class Marker
    {
        [XmlAttribute]
        public string COL1 { get; set; }
        [XmlAttribute]
        public string COL2 { get; set; }
        [XmlAttribute]
        public string COL3 { get; set; }
    }

    [XmlRoot("markers", Namespace = "", IsNullable = false)]
    public class Markers
    {
        [XmlElement("marker")]
        public Marker[] marker { get; set; }
    }

    #endregion

    #region Results

    public class MelwayResults
    {
        public string Address { get; set; }
        public string MelwaysReference { get; set; }
    }
    #endregion
}