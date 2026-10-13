using System.IO;
using System.Xml;
using UnityEditor.Android;

namespace WifiAR.EditorTools
{
    /// <summary>
    /// Adds the permissions the app needs to the generated Gradle project, so we don't have to
    /// maintain a full custom AndroidManifest.xml that must track Unity's own template.
    /// </summary>
    public sealed class AndroidManifestPostprocessor : IPostGenerateGradleAndroidProject
    {
        const string AndroidNamespace = "http://schemas.android.com/apk/res/android";

        static readonly string[] Permissions =
        {
            "android.permission.ACCESS_WIFI_STATE"
        };

        public int callbackOrder => 100;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            var document = new XmlDocument();
            document.Load(manifestPath);
            XmlElement manifest = document.DocumentElement;

            bool changed = false;
            foreach (string permission in Permissions)
            {
                if (HasPermission(manifest, permission)) continue;

                XmlElement element = document.CreateElement("uses-permission");
                XmlAttribute name = document.CreateAttribute("android", "name", AndroidNamespace);
                name.Value = permission;
                element.Attributes.Append(name);
                manifest.AppendChild(element);
                changed = true;
            }

            if (changed) document.Save(manifestPath);
        }

        static bool HasPermission(XmlElement manifest, string permission)
        {
            foreach (XmlNode node in manifest.ChildNodes)
            {
                if (node is XmlElement element &&
                    element.Name == "uses-permission" &&
                    element.GetAttribute("name", AndroidNamespace) == permission)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
