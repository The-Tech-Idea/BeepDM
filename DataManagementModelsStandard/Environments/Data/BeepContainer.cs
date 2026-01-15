
using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Environments
{
    public class BeepContainer:IBeepContainer

    {
        public BeepContainer()
        {

        }
        public BeepContainer(string containername)
        {
            GuidID = new Guid().ToString();

            ContainerName = containername;
        }
        public BeepContainer(string containername, IBeepService beepService)
        {
            GuidID = new Guid().ToString();
            ContainerName = containername;
            BeepService = beepService;
        }
        public BeepContainer(string containername, IBeepService beepService, string guidID)
        {
            GuidID = guidID;
            ContainerName = containername;
            BeepService = beepService;
        }
        public BeepContainer(string containername, IBeepService beepService, string guidID, string owner)
        {
            GuidID = guidID;
            ContainerName = containername;
            BeepService = beepService;
            Owner = owner;
        }
        public BeepContainer(string containername, IBeepService beepService, string guidID, string owner, string owneremail)
        {
            GuidID = guidID;
            ContainerName = containername;
            BeepService = beepService;
            Owner = owner;
            OwnerEmail = owneremail;
        }
        public BeepContainer(string containername, IBeepService beepService, string guidID, string owner, string owneremail, string ownerGuidID)
        {
            GuidID = guidID;
            ContainerName = containername;
            BeepService = beepService;
            Owner = owner;
            OwnerEmail = owneremail;
            OwnerGuidID = ownerGuidID;
        }
        public BeepContainer(string containername, IBeepService beepService, string guidID, string owner, string owneremail, string ownerGuidID, int ownerID)
        {
            GuidID = guidID;
            ContainerName = containername;
            BeepService = beepService;
            Owner = owner;
            OwnerEmail = owneremail;
            OwnerGuidID = ownerGuidID;
            OwnerID = ownerID;
        }
        public bool IsContainerActive { get; set; } = false;
        public bool IsContainerLoaded { get; set; } = false;
        public bool IsContainerCreated { get; set; } = false;
        public bool IsPrimary { get; set; } = false;    
        public string ContainerName { get; set; }
        public IBeepService BeepService { get; set; }
        public string GuidID { get; set; }
        public List<string> Users { get; set; } = new List<string>();
        public string AdminUserID { get; set; } = string.Empty;
        public List<string> Groups { get; set; } = new List<string>();
        public List<string> Roles { get; set; } = new List<string>();
        public List<string> Privileges { get; set; } = new List<string>();
        public List<string> Products { get; set; }=new List<string>();
        public List<string> Modules { get; set; } = new List<string>();
        public List<string> Services { get; set; } = new List<string>();
        public List<string> Assemblies { get; set; } = new List<string>();
        public List<string> Configurations { get; set; } = new List<string>();
        public List<string> DataSources { get; set; } = new List<string>();
        public List<string> DataModels { get; set; } = new List<string>();
        public List<string> DataEntities { get; set; } = new List<string>();
        public List<string> DataViews { get; set; } = new List<string>();
        public List<string> DataTransformations { get; set; } = new List<string>();
        public List<string> DataExports { get; set; } = new List<string>();
        public List<string> DataImports { get; set; } = new List<string>();
        public List<string> DataConnections { get; set; } = new List<string>();
        public List<string> DataConnectionsConfigurations { get; set; } = new List<string>();
        public string Owner { get; set; }
        public string OwnerEmail { get; set; }
        public string OwnerGuidID { get; set; }
        public int OwnerID { get; set; }

        
        public int ContainerID { get; set; }
        public string ContainerFolderPath { get; set; }
        public string ContainerUrlPath { get; set; }
        public string SecretKey { get; set; }
        public string TokenKey { get; set; }
        public bool IsAdmin { get; set; }=false;
        public bool isActive { get; set; } = true;
       

    }
}
