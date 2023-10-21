using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Foundry.Docgen
{
    [Serializable]
    public class FoundryDocgenConfig
    {
        public string DocfxOutputPath;
        public string DocfxConfigPath;
        public string DocfxTemplate;
        
        [Serializable]
        public class MetadataPair
        {
            public string Key = "";
            public string Value = "";
        }
        
        public List<MetadataPair> DocfxMetadata = new();

        private string _path;
        
        public static FoundryDocgenConfig Load(string path)
        {
            var asset = JsonUtility.FromJson<FoundryDocgenConfig>(File.ReadAllText(path));
            asset._path = path;
            return asset;
        }
        
        public static FoundryDocgenConfig Create(string path)
        {
            var asset = new FoundryDocgenConfig();
            asset._path = path;
            return asset;
        }

        public void Save()
        {
            File.WriteAllText(_path, JsonUtility.ToJson(this, true));
            
            var metadataPath = Path.Join(Path.GetDirectoryName(_path), Path.GetDirectoryName(DocfxConfigPath), "metadata.json");
            Dictionary<string, string> metadata = new();
            foreach (var pair in DocfxMetadata)
            {
                metadata.Add(pair.Key, pair.Value);
            }
            
            File.WriteAllText(metadataPath, JsonConvert.SerializeObject(metadata));
        }
    }
}
