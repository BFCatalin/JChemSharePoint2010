using ChemAxon.JChemSharePoint.Library.Data;
using ChemAxon.JChemSharePoint.Library.Data.JChemMetaField;
using ChemAxon.JChemSharePoint.Library.Data.SharePoint.IO;
using ChemAxon.JChemSharePoint.UI.WebServices.Clients;
using HtmlAgilityPack;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace EmptyStructureFinder
{
    public class LibraryChecker
    {
        public object JChemTrace { get; private set; }

        public void Check(string url)
        {
            using (var site = new SPSite(url))
            {
                ChekWeb(site.RootWeb);
                foreach (SPWeb web in site.RootWeb.Webs)
                {
                    try
                    {
                        ChekWeb(web);
                    }
                    catch (Exception ex)
                    {
                        ConsoleEx.WriteLine(ConsoleColor.Red, "Error while checking web '{0}': {1}", web.Url, ex.Message);
                    }
                }
            }
        }

        private void ChekWeb(SPWeb web)
        {
            ConsoleEx.WriteLine(ConsoleColor.Green, "Checking '{0}'", web.Title);
            using (WebStructureOutput webZipOutput = new WebStructureOutput(web.Title))
            {
                foreach (SPList list in web.Lists)
                {
                    if (list.Fields.ContainsField("JChemMetaField"))
                    {
                        ConsoleEx.WriteLine(string.Format("Checking '{0}' for empty structures...", list.Title));
                        ConsoleEx.WriteLine("\tJChem structures field found.");

                        bool runCompleteFieldCheck;
                        var metaData = CheckMetaFieldReferences(web, list, webZipOutput, out runCompleteFieldCheck);

                        if (runCompleteFieldCheck)
                        {
                            RunCheckOnAllFields(web, list, metaData, webZipOutput);
                        }
                    }

                }
                webZipOutput.Save();
            }
        }

        public void Check(IEnumerable<string> urlEnum)
        {
            foreach (var url in urlEnum)
            {
                Check(url);
            }
        }

        private void RunCheckOnAllFields(SPWeb web, SPList list, JChemMetaFieldCollector metaData, WebStructureOutput webZipOutput)
        {
            List<SPField> fields = new List<SPField>();
            foreach (SPField field in list.Fields)
            {
                if (field.Type == SPFieldType.Note && !field.Hidden)
                {
                    fields.Add(field);
                }
            }
            foreach (SPListItem listItem in list.Items)
            {
                foreach (var field in fields)
                {
                    CheckListItemField(web, list, listItem, field.Id, metaData, webZipOutput);
                }
            }
        }

        private JChemMetaFieldCollector CheckMetaFieldReferences(SPWeb web, SPList list, WebStructureOutput webZipOutput, out bool runCompleteFieldCheck)
        {
            runCompleteFieldCheck = false;
            JChemMetaFieldCollector metaData = null;
            foreach (SPListItem listItem in list.Items)
            {
                var data = listItem["JChemMetaField"] as string;
                if (data != null)
                {
                    metaData = DeserializeMetaData(data, listItem.Name);
                    foreach (var fieldId in metaData.Keys)
                    {
                        if (list.Fields.Contains(fieldId))
                        {
                            CheckListItemField(web, list, listItem, fieldId, metaData, webZipOutput);
                        }
                        else
                        {
                            ConsoleEx.WriteLine(ConsoleColor.DarkYellow, "\tMeta filed contains invalid field references. Complete list ({0}/{1}) fields check will be performed.", web.Title, list.Title);
                            runCompleteFieldCheck = true;
                        }
                    }
                }
            }

            return metaData;
        }

        private void CheckListItemField(SPWeb web, SPList list, SPListItem listItem, Guid fieldId, JChemMetaFieldCollector metaData, WebStructureOutput webZipOutput)
        {
            try
            {
                JChemContextInfo ctx = new JChemContextInfo(
                     SPSecurity.AuthenticationMode != System.Web.Configuration.AuthenticationMode.Windows,
                web.CurrentUser.LoginName,
                Convert.ToBase64String(web.CurrentUser.UserToken.BinaryToken),
                Guid.NewGuid(),
                web.Site.ID,
                web.ID,
                web.Locale,
                web.Locale,
                Guid.NewGuid());
                StructureServiceClient client = new StructureServiceClient(ctx, SPServiceContext.GetContext(web.Site));

                var fieldContent = listItem[fieldId] as string;
                if (!string.IsNullOrEmpty(fieldContent))
                {
                    var fieldContentDocument = new HtmlDocument();
                    fieldContentDocument.LoadHtml(fieldContent);

                    HtmlNodeCollection structureHtmlNodesWithIdOrURL =
                        fieldContentDocument.DocumentNode.SelectNodes("//img[contains(@src,'/_vti_bin/JChem/CxnWebGet.svc/GetStructureImageFromSession') or contains(@src,'/_vti_bin/JChem/CxnWebGet.svc/GetErrorImage')]");
                    if (structureHtmlNodesWithIdOrURL != null)
                    {
                        ConsoleEx.WriteLine(ConsoleColor.DarkRed, "\tEmpty structure found in '{0}/{1}/{2}'", web.Title, list.Title, listItem.Name);
                        foreach (var entry in metaData)
                        {
                            var structures = JChemMetaFieldDataProvider.GetStructures(entry.Value[FieldPropertyCollector.SructuresProperty]);
                            foreach (var structure in structures)
                            {
                                if (!structure.StructureString.StartsWith("error", StringComparison.InvariantCultureIgnoreCase))
                                    try
                                    {
                                        var image = client.GetStructureImage(structure.StructureString, structure.Format.ToString(), 200, 200, false);
                                        using (var imageStream = new MemoryStream())
                                        {
                                            image.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
                                            imageStream.Position = 0;
                                            webZipOutput.AddStructure(list.Title, listItem.Name, listItem.ID, structure.Id.ToString(), imageStream.ToArray());
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        ConsoleEx.WriteLine(ConsoleColor.Red, "\tCannot render structure: {0}", ex.Message);
                                        Environment.Exit(1);
                                    }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteLine("\tCannot determine content of column '{0}'. Reason: {1}", list.Fields[fieldId].Title, ex.Message);
            }
        }

        private JChemMetaFieldCollector DeserializeMetaData(string serializedData, string listItemName)
        {
            JChemMetaFieldCollector returnValue = null;
            if (string.IsNullOrEmpty(serializedData))
                return null;
            XmlSerializer serializer = new XmlSerializer(typeof(JChemMetaFieldCollector));
            try
            {
                returnValue = serializer.Deserialize(new StringReader(serializedData)) as JChemMetaFieldCollector;
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteLine(ConsoleColor.DarkRed, "Cannot deserialize meta data for '{0}'.", listItemName);
            }
            return returnValue;
        }

        private void WriteToZip()
        {
            using (var memoryStream = new MemoryStream())
            {
                using (ZipOutputStream output = new ZipOutputStream(memoryStream))
                {
                    ZipEntryFactory fact = new ZipEntryFactory();
                    var dir = fact.MakeDirectoryEntry("");

                }

            }
        }
    }
}
