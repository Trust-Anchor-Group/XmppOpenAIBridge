# XmppOpenAIBridge

Provides a bridge between the XMPP Instant Messaging protocol, and OpenAI. The solution
also provides integration with Markdown, allowing for easy use of image and text generation
services provided by OpenAI in Markdown-based content.

## Projects

The solution contains the following C# projects:

| Project                       | Framework         | Description |
|:------------------------------|:------------------|:------------|
| `TAG.Content.Markdown.OpenAI` | .NET Standard 2.0 | Integrates OpenAI services into Markdown, permitting the easy integration of generated text and images based on textual descriptions into Markdown-based content, such as web pages, [wiki content](https://lab.tagroot.io/Documentation/Index.md) or posts and replies in [the community](https://lab.tagroot.io/Community/Index.md). |
| `TAG.Networking.OpenAI`       | .NET Standard 2.0 | Class library for communicating with OpenAI services via the [OpenAI API](https://platform.openai.com/overview). |
| `TAG.Networking.OpenAI.Test`  | .NET 6.0          | Unit tests for the `TAG.Networking.OpenAI` library. |
| `TAG.Things.OpenAI`           | .NET Standard 2.0 | Publishes harmonized interfaces for administering access to OpenAI. The harmonized nodes allow for custom bridging between users of the XMPP protocol and services published by the OpenAI API, such as chatting and image generation. |

## Nugets

The following nugets external are used. They faciliate common programming tasks, and
enables the libraries to be hosted on an [IoT Gateway](https://github.com/PeterWaher/IoTGateway).
This includes hosting the bridge on the [TAG Neuron](https://lab.tagroot.io/Documentation/Index.md).
They can also be used standalone.

| Nuget                                                                              | Description |
|:-----------------------------------------------------------------------------------|:------------|
| [Waher.Content](https://www.nuget.org/packages/Waher.Content/)                     | Pluggable architecture for accessing, encoding and decoding Internet Content. |
| [Waher.Content.Markdown](https://www.nuget.org/packages/Waher.Content.Markdown/)   | An extensible Markdown-engine that parses Markdown, and converts it to various presentation or content formats. |
| [Waher.Content.Xml](https://www.nuget.org/packages/Waher.Content.Xml/)             | Helps with encoding and decoding of XML (and derivatives, such as XHTML). |
| [Waher.Events](https://www.nuget.org/packages/Waher.Events/)                       | An extensible architecture for event logging in the application. |
| [Waher.IoTGateway](https://www.nuget.org/packages/Waher.IoTGateway/)               | Contains the [IoT Gateway](https://github.com/PeterWaher/IoTGateway) hosting environment. |
| [Waher.Networking](https://www.nuget.org/packages/Waher.Networking/)               | Tools for working with communication, including troubleshooting. |
| [Waher.Runtime.Cache](https://www.nuget.org/packages/Waher.Runtime.Cache/)         | Helps with in-memory caching and memory management. |
| [Waher.Runtime.Temporary](https://www.nuget.org/packages/Waher.Runtime.Temporary/) | Library that helps with the management of temporary streams and files. |
| [Waher.Runtime.Timing](https://www.nuget.org/packages/Waher.Runtime.Timing/)       | Helps scheduling future tasks and events in an application. |
| [Waher.Things](https://www.nuget.org/packages/Waher.Things/)                       | Basic architecture enabling the harmonization of things across technology boundaries. |
| [Waher.Things.Xmpp](https://www.nuget.org/packages/Waher.Things.Xmpp/)             | Harmonized extensions for XMPP-based communication and extensions. |

The Unit Tests further use the following libraries:

| Nuget                                                                                            | Description |
|:-------------------------------------------------------------------------------------------------|:------------|
| [Waher.Content.Images](https://www.nuget.org/packages/Waher.Content.Images/)                     | Contains encoders and decoders of images. |
| [Waher.Events.Console](https://www.nuget.org/packages/Waher.Events.Console/)                     | Outputs events logged to the console output. |
| [Waher.Persistence](https://www.nuget.org/packages/Waher.Persistence/)                           | Abstraction layer for object databases. |
| [Waher.Persistence.Files](https://www.nuget.org/packages/Waher.Persistence.Files/)               | An encrypted object database stored as local files. |
| [Waher.Runtime.Inventory](https://www.nuget.org/packages/Waher.Runtime.Inventory/)               | Maintains an inventory of type definitions in the runtime environment, and permits easy instantiation of suitable classes, and inversion of control (IoC). |
| [Waher.Runtime.Inventory.Loader](https://www.nuget.org/packages/Waher.Runtime.Inventory.Loader/) | Permits the inventory and seamless integration of classes defined in all available assemblies. |
| [Waher.Runtime.Settings](https://www.nuget.org/packages/Waher.Runtime.Settings/)                 | Provides easy access to persistent settings. |

## Installable Package

The `TAG.Content.Markdown.OpenAI` project has been made into a package that can be downloaded and installed on any 
[TAG Neuron](https://lab.tagroot.io/Documentation/Index.md), or run on any [IoT Gateway](https://github.com/PeterWaher/IoTGateway).
To create a package, that can be distributed or installed, you begin by creating a *manifest file*. The
`TAG.Content.Markdown.OpenAI` project has a manifest file called `TAG.Content.Markdown.OpenAI.manifest`. It defines the
assemblies and content files included in the package. You then use the `Waher.Utility.Install` and `Waher.Utility.Sign` command-line
tools in the [IoT Gateway](https://github.com/PeterWaher/IoTGateway) repository, to create a package file and cryptographically
sign it for secure distribution across the Neuron network.

The XMPP/OpenAI Bridge is published as a package on TAG Neurons. If your neuron is connected to this network, you can install the
package using the following information:

| Package information ||
|:-----------------|:----|
| Package          | `TAG.XmppOpenAIBridge.package` |
| Installation key | `XGyd1kOAZX3KMhpKLDJ0swJ0Bxwg1lF6Z/DgRScfo/Ys0dxfr4u7U/ofd4zjL00jpi5MJAOIpISAa4982aef95d5daae27ccbbe3f12c38ac` |

## Building, Compiling & Debugging

The repository assumes you have the [IoT Gateway](https://github.com/PeterWaher/IoTGateway) repository cloned in a folder called
`C:\My Projects\IoT Gateway`, and that this repository is placed in `C:\My Projects\XmppOpenAIBridge`. You can place the
repositories in different folders, but you need to update the build events accordingly. To run the application, you select the
`TAG.Content.Markdown.OpenAI` project as your stardup project. It will execute the console version of the
[IoT Gateway](https://github.com/PeterWaher/IoTGateway), and make sure the compiled files of the `XmppOpenAIBridge` solution
is run with it.