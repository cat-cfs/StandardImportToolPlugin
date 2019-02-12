# StandardImportToolPlugin
wrapper for Operational-Scale CBM-CFS3 standard import tool DLL, and sample import code.  

Buid instructions (visual studio 2017)
  * Install the Operational-Scale CBM-CF3 Toolbox
  
  * Add references to the following toolbox dll files:
      * CBMSIT.dll
      * StringUtilities.dll
      * Interfaces.dll
      * WoodStockImportTool.dll
      * Global.dll
 
The DLL files can be found in your installation directory. For example
here:  
`C:\Program Files (x86)\Operational-Scale CBM-CFS3\CBMSIT.dll`

tutorial6.xls which is referenced in example code is found by default in this directory:
`C:\Program Files (x86)\Operational-Scale CBM-CFS3\Tutorials\Tutorial 6`

## command line usage

A quick example for importing the included [tutorial6.json](https://github.com/cat-cfs/StandardImportToolPlugin/blob/master/tutorial6.json) configuration file.  The output path, as specified in the config file itself will be created as a valid CBM-CFS3 project

`StandardImportToolPlugin.exe -c "tutorial6.json"`

## other uses

The file [ExampleMethods.cs](https://github.com/cat-cfs/StandardImportToolPlugin/blob/master/ExampleMethods.cs) has 3 usage examples for importing SIT file directly in C# code

  * [Method 1](https://github.com/cat-cfs/StandardImportToolPlugin/blob/master/ExampleMethods.cs#L146) - Import an SIT project from an SIT formatted database or excel spreadsheet (for example tutorial6.xlsx) 
  * [Method 2](https://github.com/cat-cfs/StandardImportToolPlugin/blob/master/ExampleMethods.cs#L88) - Import an SIT project by building up a dataset manually with code.
  * [Method 3](https://github.com/cat-cfs/StandardImportToolPlugin/blob/master/ExampleMethods.cs#L19) - Import via json object
