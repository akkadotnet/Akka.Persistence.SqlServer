using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Akka.Persistence.SqlServer.Tests.Internal
{
    internal class DockerJsonSerializer
    {
        private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new JsonConverter[]
            {
                new JsonIso8601AndUnixEpochDateConverter(),
                new JsonVersionConverter(),
                new StringEnumConverter(),
                new TimeSpanSecondsConverter(),
                new TimeSpanNanosecondsConverter()
            }
        };

        public DockerJsonSerializer()
        {
        }

        public T DeserializeObject<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, this._settings);
        }

        public string SerializeObject<T>(T value)
        {
            return JsonConvert.SerializeObject(value, this._settings);
        }
    }
}
