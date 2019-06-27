using ChemAxon.JChemSharePoint.Model.Data.JChemMetaField;
using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JChemSharePointEmptyStructureFinder
{
    public class LibraryChecker
    {
        public void Check(string url)
        {
            using(var site = new SPSite(url))
            {
                using(var web = site.OpenWeb())
                {
                    foreach(SPList list in web.Lists)
                    {
                        Console.WriteLine(string.Format("Checking '{0}' for structures...", list.Title));
                        if (list.Fields.ContainsField("JChemMetaField"))
                        {
                            Console.WriteLine("\tJChem structures field found.");
                        }
                        
                    }
                }
            }
        }

        public void Check(IEnumerable<string> urlEnum)
        {
            foreach(var url in urlEnum)
            {
                Check(url);
            }
        }
    }
}
