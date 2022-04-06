# BeepDM

A Data Managment Engine and Library for Managing you Connection to Different DataSources 

  * This is a Engine That will people from regular clerk trying to make sense of his data from excel sheet or text file to a Data Analyst working to make decision.
  * Its based on Modular Design and Most of the Engine is expandable.
  * It support most of the major Databases's out of the box like oracle,sqlserver, mysql, .... etc.

The Directory Structure for Every Beep Data Management Project has the following Directories:

* 1- Addin            : Where you place all DLL that have "Addin" Interface (usercontrol, form,class,etc ..)
* 2- AI               : Where we store all AI scripts (user Later)
* 3- Config           : Where we store all ourt configuration files needed to run Beep DM correctly.
* 4- ConnectionDrivers: Store all Data source drivers (oracle ,sqlite,sqlserver, etc)
* 5- DataFiles        : Primary strorage for Data Files needed for Project
* 6- DataViews        : A DataView is a Fedrated View of Different Data Source entities, thats you can compile and manupilate. Json DataView File Stored Here.
* 7- Entities         : Temporary place to store Datasource Entities Descriptions.
* 8- GFX              : a place to Store Gfx and icons needed by Application.
* 9- LoadingExtensions: place to load all Classes that Implement "ILoadingExtension". This will allow you to add extra functionality and dynmicaly load other data                             classes in your Application not defined before.
* 10- Mapping         : place to Store Mapping Definitions for Datasource to other Datasource.
* 11- OtherDLL        : Place to Store other dll needed for your application.
* 12- ProjectClasses  : The primary Folder for Loading (DataSource Implementation,Addins,etc)
* 13- ProjectData     : Place to store files 
* 14- Scripts         : Place to store scripts and logs
* 15- WorkFlow        : place to store Workflow definitions

![Image of Yaktocat](https://github.com/fahadTheTechIdea/gfx/raw/master/BeepDMProjectStructure.png) 
![Image of Yaktocat](https://github.com/fahadTheTechIdea/gfx/blob/master/DataManagementEngine.png) 


#Lets get to the good part:


* Create New Data Source:
  * Create New Class Implementing "IDataSource".
  * Deploy new Class Dll to ConnectionDrivers Directory in the Application.
  * Run "Connection Drivers" Module from Addin Tree in Application. Add new Record and Select Your Class From "Class Handler" Column.
  * Or Update File "ConnectionConfig.json" in the Config Folder in Application Folder.
  
* Create New Addin:
  * Create usercontrol of Form Implementing "IDM_Addin" , then Add Generated dll to Addin Folder or ProjectClasses and it will show in Addin Tree in Application.
    
* Config Folder:
   * QueryList.json : Define all Queries types needed for getting meta data from any Datasource.
   * DriversDefinitions.json : Define Drivers for Data Source that have no Drivers (like WebApi).
   * ConnectionConfig.json : Define Drivers and DataSource Class that use them and other data (like icon).
   * DataTypeMapping.json :Define the DataType and Column Type Mapping for DataSource to Datasource.
   * DataConnections.json : Define Data Source Connections.

