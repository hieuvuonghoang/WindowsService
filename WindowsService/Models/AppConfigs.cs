using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsService
{
    public class AppConfigs : ConfigurationSection
    {
        [ConfigurationProperty("FileConfigs")]
        public FileConfigs FileConfigs
        {
            get
            {
                return (FileConfigs)this["FileConfigs"];
            }
            set
            {
                value = (FileConfigs)this["FileConfigs"];
            }
        }
    }

    public class FileConfigs : ConfigurationElement
    {
        [ConfigurationProperty("FileName", DefaultValue = "StoreMaxId.txt", IsRequired = true)]
        public string FileName
        {
            get
            {
                return (string)this["FileName"];
            }
            set
            {
                value = (string)this["FileName"];
            }
        }
        [ConfigurationProperty("Dir", IsRequired = true)]
        public string Dir
        {
            get
            {
                return (string)this["Dir"];
            }
            set
            {
                value = (string)this["Dir"];
            }
        }
    }
}
