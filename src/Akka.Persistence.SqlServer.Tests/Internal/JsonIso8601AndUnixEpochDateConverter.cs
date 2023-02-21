// -----------------------------------------------------------------------
// <copyright file="JsonIso8601AndUnixEpochDateConverter.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Reflection;
using Newtonsoft.Json;

namespace Akka.Persistence.SqlServer.Tests.Internal
{
    internal class JsonIso8601AndUnixEpochDateConverter : JsonConverter
    {
        private static readonly DateTime UnixEpochBase = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var isNullableType = objectType.GetTypeInfo().IsGenericType &&
                                 objectType.GetGenericTypeDefinition() == typeof(Nullable<>);
            var value = reader.Value;

            DateTime result;
            if (value is DateTime)
                result = (DateTime)value;
            else if (value is string)
                // ISO 8601 String
                result = DateTime.Parse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            else if (value is long)
                // UNIX epoch timestamp (in seconds)
                result = UnixEpochBase.AddSeconds((long)value);
            else
                throw new NotImplementedException(
                    $"Deserializing {value.GetType().FullName} back to {objectType.FullName} is not handled.");

            if (isNullableType && result == default) return null; // do not set result on DateTime? field

            return result;
        }
    }
}