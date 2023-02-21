// -----------------------------------------------------------------------
// <copyright file="JsonVersionConverter.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using System;
using Newtonsoft.Json;

namespace Akka.Persistence.SqlServer.Tests.Internal
{
    internal class JsonVersionConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var strVal = reader.Value as string;
            if (strVal == null)
            {
                var valueType = reader.Value == null ? "<null>" : reader.Value.GetType().FullName;
                throw new InvalidOperationException(
                    $"Cannot deserialize value of type '{valueType}' to '{objectType.FullName}' ");
            }

            return Version.Parse(strVal);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Version);
        }
    }
}