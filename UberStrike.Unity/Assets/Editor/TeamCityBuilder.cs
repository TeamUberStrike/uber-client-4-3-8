using System.Text;
using System.Xml;
using Cmune.DataCenter.Common.Entities;
using UnityEngine;

public class TeamCityBuilder
{
    #region Build Routines

    public static void BuildWebPlayer()
    {
        SceneExporter.BuildWebPlayer(false, false);
        WriteConfigurationXml(ChannelType.WebPortal, SceneExporter.WebPlayerFolder + "/" + ApplicationDataManager.HeaderFilename + ".xml");
    }

    public static void BuildWindowsStandalone()
    {
        SceneExporter.BuildWindowsStandalone(false);
        WriteConfigurationXml(ChannelType.WindowsStandalone, SceneExporter.WindowsStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + "_Data/" + ApplicationDataManager.StandaloneFilename + ".xml");
    }

    public static void BuildMacAppStoreStandalone()
    {
        SceneExporter.BuildMacAppStoreStandalone();
        WriteConfigurationXml(ChannelType.MacAppStore, SceneExporter.MacAppStoreStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + ".app/Contents/Data/" + ApplicationDataManager.StandaloneFilename + ".xml");
    }

    public static void BuildOsxStandalone()
    {
        SceneExporter.BuildOsxStandalone();
        WriteConfigurationXml(ChannelType.OSXStandalone, SceneExporter.OsxStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + ".app/Contents/Data/" + ApplicationDataManager.StandaloneFilename + ".xml");
    }

    #endregion

    #region XML configuration

    public static void WriteConfigurationXml(ChannelType channel, string configurationXmlFilePath)
    {
        if (!string.IsNullOrEmpty(configurationXmlFilePath) &&
        configurationXmlFilePath.ToLower().Contains(".xml"))
        {
            XmlWriter xml = XmlWriter.Create(configurationXmlFilePath, new XmlWriterSettings() { Indent = true, NewLineOnAttributes = true, Encoding = Encoding.ASCII });

            try
            {
                xml.WriteStartDocument();

                xml.WriteStartElement("UberStrike");
                xml.WriteStartElement("Application");

                xml.WriteAttributeString("BuildType", "@BuildType");
                xml.WriteAttributeString("DebugLevel", "@DebugLevel");
                xml.WriteAttributeString("Version", "@Version");
                xml.WriteAttributeString("WebServiceBaseUrl", "@WebServiceUrl");
                xml.WriteAttributeString("ContentBaseUrl", "@ContentBaseUrl");
                xml.WriteAttributeString("ChannelType", channel.ToString());

                xml.WriteEndElement();
                xml.WriteEndElement();

                xml.WriteEndDocument();
            }
            finally
            {
                xml.Flush();
                xml.Close();
            }
        }
        else
        {
            Debug.LogError("Invalid url supplied for the Configuration XML file.\n'" + configurationXmlFilePath + "'");
        }
    }

    #endregion
}