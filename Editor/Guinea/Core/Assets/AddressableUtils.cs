using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System.Xml.Serialization;
using System.IO;

namespace Guinea.Core
{
    public static partial class AddressableUtils
    {
        public static void AssetAsAddressable(AddressableAssetSettings settings, Object o, string groupName, string address=null)
        {
            var group = settings.FindGroup(groupName);
            if (group is null)
                group = settings.CreateGroup(groupName, false, false, true, null, typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
 
            var assetpath = AssetDatabase.GetAssetPath(o);
            var guid = AssetDatabase.AssetPathToGUID(assetpath);
 
            var e = settings.CreateOrMoveEntry(guid, group, false, false);
            var entriesAdded = new List<AddressableAssetEntry> {e};
            if(!string.IsNullOrEmpty(address))
            {
                e.address = address;
            }
 
            group.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, false, true);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true, false);
        }

        public static void BuildAddressableFromConfig(AddressableAssetSettings settings, string configPath)
        {
            TextAsset textAsset = EditorGUIUtility.Load(configPath) as TextAsset;
            Debug.Assert(textAsset!=null, $"Could not load {typeof(Addressableconfig)} from \"{configPath}\"");
            XmlSerializer serializer = new XmlSerializer(typeof(Addressableconfig));
            Addressableconfig config;
            using (StringReader reader = new StringReader(textAsset.text))
            {
                config = serializer.Deserialize(reader) as Addressableconfig;
            }

            string profileId = settings.profileSettings.GetProfileId(config.ProfileName);
            if (string.IsNullOrEmpty(profileId))
            {
                profileId = settings.profileSettings.AddProfile(config.ProfileName, settings.profileSettings.GetProfileId("Default"));
            }

            settings.profileSettings.SetValue(profileId, AddressableAssetSettings.kLocalBuildPath, config.Localpath.BuildPath);
            settings.profileSettings.SetValue(profileId, AddressableAssetSettings.kLocalLoadPath, config.Localpath.LoadPath);
            settings.profileSettings.SetValue(profileId, AddressableAssetSettings.kRemoteBuildPath, config.Remotepath.BuildPath);
            settings.profileSettings.SetValue(profileId, AddressableAssetSettings.kRemoteLoadPath, config.Remotepath.LoadPath);
            settings.activeProfileId = profileId;

            foreach (Group group in config.Group)
            {
                foreach (string file in Directory.EnumerateFiles(Path.Combine("Assets", group.Path), group.Regex, SearchOption.AllDirectories))
                {
                    Object asset = AssetDatabase.LoadMainAssetAtPath(file);
                    AssetAsAddressable(settings, asset, group.Name, Path.GetFileNameWithoutExtension(file));
                }


                AddressableAssetGroup groupSettings = settings.FindGroup(group.Name);
                UnityEngine.Debug.Assert(groupSettings != null, $"Group {group.Name} has not been created. This can be caused by Directory.EnumerateFiles returned Empty");
                switch (group.Type)
                {
                    case "remote":
                        {
                            groupSettings.GetSchema<BundledAssetGroupSchema>().BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
                            groupSettings.GetSchema<BundledAssetGroupSchema>().LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
                            break;
                        }
                    default:
                        {
                            groupSettings.GetSchema<BundledAssetGroupSchema>().BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                            groupSettings.GetSchema<BundledAssetGroupSchema>().LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
                            break;
                        }
                }
            }
            AddressableAssetSettings.BuildPlayerContent();
        }

        [MenuItem("AddressableUtils/BuildFromPreviousBuild")]
        public static void BuildFromPreviousBuild()
        {
            string contentStatePath = ContentUpdateScript.GetContentStateDataPath(false);
            ContentUpdateScript.BuildContentUpdate(AddressableAssetSettingsDefaultObject.Settings, contentStatePath);
        }

        [MenuItem("AddressableUtils/BuildFromConfig")]
        public static void BuildFromConfig()
        {
            AssetDatabase.Refresh();
            BuildAddressableFromConfig(AddressableAssetSettingsDefaultObject.Settings, "AddressableConfig.xml");
        }

        [XmlRoot(ElementName = "localpath")]
        public class Localpath
        {

            [XmlAttribute(AttributeName = "buildPath")]
            public string BuildPath { get; set; }

            [XmlAttribute(AttributeName = "loadPath")]
            public string LoadPath { get; set; }
        }

        [XmlRoot(ElementName = "remotepath")]
        public class Remotepath
        {

            [XmlAttribute(AttributeName = "buildPath")]
            public string BuildPath { get; set; }

            [XmlAttribute(AttributeName = "loadPath")]
            public string LoadPath { get; set; }
        }

        [XmlRoot(ElementName = "group")]
        public class Group
        {

            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }

            [XmlAttribute(AttributeName = "type")]
            public string Type { get; set; }

            [XmlAttribute(AttributeName = "path")]
            public string Path { get; set; }

            [XmlAttribute(AttributeName = "regex")]
            public string Regex { get; set; }

            [XmlAttribute(AttributeName = "namedBy")]
            public string NamedBy { get; set; }
        }

        [XmlRoot(ElementName = "addressableconfig")]
        public class Addressableconfig
        {

            [XmlElement(ElementName = "localpath")]
            public Localpath Localpath { get; set; }

            [XmlElement(ElementName = "remotepath")]
            public Remotepath Remotepath { get; set; }

            [XmlElement(ElementName = "group")]
            public List<Group> Group { get; set; }

            [XmlAttribute(AttributeName = "profileName")]
            public string ProfileName { get; set; }
        }
    }
}