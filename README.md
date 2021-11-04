# Project Name

This repo contains the source code for process WSDL files that contains wsdl:imports, also xsd imports and includes. It merges WSDL file with all those references.

## Features

This project framework provides the following features:

* Detect/resolve/inline all wsdl:imports
* Detect/resolve/inline all xsd:imports in xml schemas
* Detect/resolve/inline all xsd:includes in xml schemas
* Merge all xml schemas with the same targetnamespace into a single schema
* The tool produces a single wsdl file to a path passed as a second parameter: **tool.exe "c:\foo\bar\in.wsdl" "d:\buzz\quix\out.wsdl"**
* The tool is able to resolve following types of references:
    * http/https absolute urls. Any non-200 response is a failure.
    * Absolute local filesystem location.
    * Relative local filesystem location. For the base location the tool uses current file location, **not root file location and not the location of the tool itself**.

## Getting Started

### Prerequisites


- OS: Windows, Linux, MacOS.
- Library version: .NET 5.0
- .NET Command Line

### Installation

When you have the .NET Command Line installed on your OS of choice, you can download the code and go to Microsoft.Azure.ApiManagement.WsdlProcessor.App directory. 

First, you will need to restore the packages:
	
	dotnet restore
	
This will restore all of the packages that are specified in the csproj file of the given project.

Compiling to IL is done using:
	
	dotnet build

This will drop a binary in `./bin/[configuration]/[net5.0]/[Microsoft.Azure.ApiManagement.WsdlProcessor.App.exe]` that you can just run.

You can run the binary in this way:
	
	c:\folder\Microsoft.Azure.ApiManagement.WsdlProcessor.App.exe "mywsdlfile.wsdl" "myoutputwsdlfile-processed.wsdl""
	

### Quickstart
(Add steps to get up and running quickly)

1. git clone https://github.com/Azure-Samples/api-management-schema-import.git
2. cd **api-management-schema-import**
3. 


## Demo

A demo app is included to show how to use the project.

To run the demo, follow these steps:

(Add steps to start up the demo)

1.
2.
3.

## Resources

(Any additional resources or related projects)

- Link to supporting information
- Link to similar sample
- ...
