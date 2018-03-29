﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Reflection;
using NBitcoin.JsonConverters;
using System.Globalization;
using Common.CLightning;

namespace Common.JsonConverters
{
    public class LightMoneyJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(LightMoneyJsonConverter).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        Type longType = typeof(long).GetTypeInfo();
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                return reader.TokenType == JsonToken.Null ? null :
                    reader.TokenType == JsonToken.Integer ?
                                                longType.IsAssignableFrom(reader.ValueType) ? new LightMoney((long)reader.Value)
                                                                                            : new LightMoney(long.MaxValue) :
                    reader.TokenType == JsonToken.String ? new LightMoney(long.Parse((string)reader.Value, CultureInfo.InvariantCulture)) 
                    : null;
            }
            catch (InvalidCastException)
            {
                throw new JsonObjectException("Money amount should be in millisatoshi", reader);
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((LightMoney)value).MilliSatoshi);
        }
    }
}
