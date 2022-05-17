using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace OWSShared.Implementations
{
    public class WritableOptions<T> : IWritableOptions<T> where T : class, new()
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IOptionsMonitor<T> _options;
        private readonly IConfigurationRoot _configuration;
        private readonly string _section;
        private readonly string _file;
        public WritableOptions(
            IWebHostEnvironment environment,
            IOptionsMonitor<T> options,
            IConfigurationRoot configuration,
            string section,
            string file)
        {
            _environment = environment;
            _options = options;
            _configuration = configuration;
            _section = section;
            _file = file;
        }
        public T Value => _options.CurrentValue;
        public T Get(string name) => _options.Get(name);
        public void Update(Action<T> applyChanges)
        {
            var fileProvider = _environment.ContentRootFileProvider;
            var fileInfo = fileProvider.GetFileInfo(_file);
            var physicalPath = fileInfo.PhysicalPath;
            string jsonText = File.ReadAllText(physicalPath);
            var jObject = JsonSerializer.Deserialize<dynamic>(jsonText);
            JsonElement section = new JsonElement();
            jObject.TryGetProperty(_section, out section);

            var sectionObject = JsonSerializer.Deserialize<T>(section.ToString());

            applyChanges(sectionObject);

            //Temporary fix until .NET 6 when we can use the new JsonObject
            Dictionary<string, object> obj = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonText);
            obj[_section] = sectionObject;

            File.WriteAllText(physicalPath, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
            _configuration.Reload();
        }
    }
}
