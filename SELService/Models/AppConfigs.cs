using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SELService
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
        [ConfigurationProperty("WebServiceConfigs")]
        public WebServiceConfigs WebServiceConfigs
        {
            get
            {
                return (WebServiceConfigs)this["WebServiceConfigs"];
            }
            set
            {
                value = (WebServiceConfigs)this["WebServiceConfigs"];
            }
        }
        [ConfigurationProperty("PageConfigs")]
        public PageConfigs PageConfigs
        {
            get
            {
                return (PageConfigs)this["PageConfigs"];
            }
            set
            {
                value = (PageConfigs)this["PageConfigs"];
            }
        }
        [ConfigurationProperty("DonViConfigs")]
        public DonViConfigs DonViConfigs
        {
            get
            {
                return (DonViConfigs)this["DonViConfigs"];
            }
            set
            {
                value = (DonViConfigs)this["DonViConfigs"];
            }
        }
        [ConfigurationProperty("ServiceConfigs")]
        public ServiceConfigs ServiceConfigs
        {
            get
            {
                return (ServiceConfigs)this["ServiceConfigs"];
            }
            set
            {
                value = (ServiceConfigs)this["ServiceConfigs"];
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

    public class WebServiceConfigs : ConfigurationElement
    {
        [ConfigurationProperty("URI", IsRequired = true)]
        public string URI
        {
            get
            {
                return (string)this["URI"];
            }
            set
            {
                value = (string)this["URI"];
            }
        }
        [ConfigurationProperty("APIKey", IsRequired = true)]
        public string APIKey
        {
            get
            {
                return (string)this["APIKey"];
            }
            set
            {
                value = (string)this["APIKey"];
            }
        }
    }

    public class PageConfigs : ConfigurationElement
    {
        [ConfigurationProperty("MaxRowInPage", IsRequired = true)]
        public int MaxRowInPage
        {
            get
            {
                return (int)this["MaxRowInPage"];
            }
            set
            {
                value = (int)this["MaxRowInPage"];
            }
        }
    }

    public class DonViConfigs : ConfigurationElement
    {
        [ConfigurationProperty("MaDVQL", IsRequired = true)]
        public string MaDVQL
        {
            get
            {
                return (string)this["MaDVQL"];
            }
            set
            {
                value = (string)this["MaDVQL"];
            }
        }
        [ConfigurationProperty("TenDVQL", IsRequired = true)]
        public string TenDVQL
        {
            get
            {
                return (string)this["TenDVQL"];
            }
            set
            {
                value = (string)this["TenDVQL"];
            }
        }
    }

    public class ServiceConfigs : ConfigurationElement
    {
        [ConfigurationProperty("RefreshTime", IsRequired = true)]
        public int RefreshTime
        {
            get
            {
                return (int)this["RefreshTime"];
            }
            set
            {
                value = (int)this["RefreshTime"];
            }
        }
    }
}
