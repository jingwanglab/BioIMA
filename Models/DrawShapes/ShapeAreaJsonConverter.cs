using wpf522.Expends;
using wpf522.Models.Enums;
using Interface.Expends;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace wpf522.Models.DrawShapes
{
    public class ShapeAreaJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.Equals(typeof(ShapeArea));
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            var shapeType = obj["ShapeType"]?.To<ShapeType>();
            if (shapeType == ShapeType.Box)
            {
                return JsonConvert.DeserializeObject<ShapeBox>(obj.ToString());
            }
            else if (shapeType == ShapeType.Polygon)
            {
                return JsonConvert.DeserializeObject<ShapePolygon>(obj.ToString());
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public override bool CanWrite => false;
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

