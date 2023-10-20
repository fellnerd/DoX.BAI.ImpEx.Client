using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using Microsoft.Win32;

namespace DoX.BAI.ImpEx.Client
{
    public class BAIClientSettingsProvider : SettingsProvider
    {

        const String SETTINGSROOT = "BAIClientSettings";
        private XmlDocument _SettingsXML = null;

        public override void Initialize(String name, NameValueCollection col)
        {
            base.Initialize(this.ApplicationName, col);
        }

        public override String ApplicationName
        {
            get
            {
                return BAIClient.Instance.ClientName;
            }
            set { }
        }

        public virtual String GetAppSettingsPath()
        {
            // --- Alle Benutzer haben die gleichen Settings
            String path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Dorner Electronic\\BAI Client\\");

            return path;
        }

        public virtual String GetAppSettingsFilename()
        {
            return ApplicationName + ".settings";
        }

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection propvals)
        {
            foreach (SettingsPropertyValue propval in propvals)
            {
                SetValue(propval);
            }

            DirectoryInfo di = new DirectoryInfo(GetAppSettingsPath());
            if (!di.Exists)
                di.Create();

            SettingsXML.Save(Path.Combine(GetAppSettingsPath(), GetAppSettingsFilename()));
        }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection props)
        {
            SettingsPropertyValueCollection values = new SettingsPropertyValueCollection();

            foreach (SettingsProperty setting in props)
            {

                SettingsPropertyValue value = new SettingsPropertyValue(setting);
                value.IsDirty = false;
                value.SerializedValue = GetValue(setting);
                values.Add(value);
            }
            return values;
        }

        private XmlDocument SettingsXML
        {
            get
            {
                if (_SettingsXML == null)
                {
                    _SettingsXML = new XmlDocument();

                    try
                    {
                        _SettingsXML.Load(Path.Combine(GetAppSettingsPath(), GetAppSettingsFilename()));
                    }
                    catch (Exception)
                    {
                        XmlDeclaration dec = _SettingsXML.CreateXmlDeclaration("1.0", "utf-8", String.Empty);
                        _SettingsXML.AppendChild(dec);

                        XmlNode nodeRoot;

                        nodeRoot = _SettingsXML.CreateNode(XmlNodeType.Element, SETTINGSROOT, "");
                        _SettingsXML.AppendChild(nodeRoot);
                    }
                }

                return _SettingsXML;
            }
        }

        private String GetValue(SettingsProperty setting)
        {
            String ret = "";

            try
            {
                ret = SettingsXML.SelectSingleNode(SETTINGSROOT + "/" + setting.Name).InnerText;
            }
            catch (Exception)
            {
                if ((setting.DefaultValue != null))
                    ret = setting.DefaultValue.ToString();
                else
                    ret = "";
            }

            return ret;
        }

        private void SetValue(SettingsPropertyValue propVal)
        {

            XmlElement settingNode = null;

            try
            {
                settingNode = (XmlElement)SettingsXML.SelectSingleNode(SETTINGSROOT + "/" + propVal.Name);
            }
            catch (Exception)
            {
                settingNode = null;
            }

            // --- Existiert der Node?
            if ((settingNode != null))
            {
                settingNode.InnerText = propVal.SerializedValue.ToString();
            }
            else
            {
                // --- Wert als Element speichern
                settingNode = SettingsXML.CreateElement(propVal.Name);
                settingNode.InnerText = propVal.SerializedValue.ToString();
                SettingsXML.SelectSingleNode(SETTINGSROOT).AppendChild(settingNode);
            }
        }

    }
}
