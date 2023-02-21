// -----------------------------------------------------------------------
// <copyright file="TimeSpanNanosecondsConverter.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using System;
using Newtonsoft.Json;

namespace Akka.Persistence.SqlServer.Tests.Internal
{
    internal class TimeSpanNanosecondsConverter : JsonConverter
    {
        private const int MiliSecondToNanoSecond = 1000000;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var timeSpan = value as TimeSpan?;
            if (timeSpan == null) return;

            writer.WriteValue((long)(timeSpan.Value.TotalMilliseconds * MiliSecondToNanoSecond));
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TimeSpan) || objectType == typeof(TimeSpan?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var valueInNanoSeconds = (long?)reader.Value;

            if (!valueInNanoSeconds.HasValue) return null;
            var miliSecondValue = valueInNanoSeconds.Value / MiliSecondToNanoSecond;

            return TimeSpan.FromMilliseconds(miliSecondValue);
        }
    }
}