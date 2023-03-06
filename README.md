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

## Configuring bridges to OpenAI

To create a bridge to OpenAI, the first step is to create a Bridge *node* on the gateway or Neuron. Once the package is installed,
you can do this using, for instance, the *Simple IoT Client*, available in the [IoT Gateway](https://github.com/PeterWaher/IoTGateway)
repository. Follow these steps:

1. As an administrator of the Gateway, make sure your XMPP Address (JID) is on the list to receive notifications from the
gateway. This way, the gateway knows you're an administrator.

2. From your XMPP Client (for example, the *Simple IoT Client*), add your Gateway as a contact, and subscribe to its presence.

3. Once you have an approved subscription, expand the contact, then expand the `MeteringTopology` source node, followed by
the `Root` node.

4. On the `Root` node, add one or more `XMPP Broker` nodes. Each `XMPP Broker` node, creates a separate XMPP connection. You can
point this to the same Neuron, if you're hosting the bridge on a Neuron. If you're hosting the bridge on another type of Gateway,
you need to point it to a Neuron, or some other XMPP Server. The `XMPP Broker` node will maintain a separate XMPP connection to this
Broker, and will receive its own XMPP Address (or JID). The bridge you're creating will be reachable on this JID.

	There are three tabs you need to fill in: On the `IP` Tab, you fill in information about the XMPP Server (or Neuron) you wish
	to connect to. On the `XMPP` tab, you fill in information about the account you will use. On the `Roster` tab, you optionally
	enter a regular expression that will be used to automatically accept presence subscription requests, if they come from JIDs
	matching this expression. (If you don't want automatic presence subscription acceptance, just leave the corresponding regular 
	expression empty.)

5. Once you have an XMPP connection for the bridge (i.e. the `XMPP Broker` node is created and works), you can add an OpenAI
extension node to the `XMPP Broker` node. You select either a `ChatGPT-XMPP Bridge` node or a `DALL-E XMPP Bridge` node, depending
on what type of bridge you want the connection to represent.

	**Note**: It is important to provide the Open AI extension node with a proper identity (ID property). This identity is used in 
	the Markdown integrations, to select the proper gateway to use when converting Markdown code blocks into presentable content.

	**Note 2**: When creating a `ChatGPT-XMPP Bridge` node, make sure to provide proper *instructions* in the corresponding field.
	These instructions are human-readable text that describes the role OpenAI has in the chat. This is the only mechanism available
	to customize ChatGPT.

Once you have completed these steps, you can access the bridge, wither by chatting with them, using the JIDs you've defined above,
or through Markdown, referencing the extensions defined.

![Example configuration](Images/ExampleConfiguration.png)

## Integration with Markdown

Once the package is installed, the OpenAI bridges will also be available via [Markdown](https://lab.tagroot.io/Markdown.md),
as [code block constructs](https://lab.tagroot.io/Markdown.md#codeBlocks). The `TAG.Content.Markdown.AI` projects defines two
code block extensions: One for ChatGPT, and one for DALL-E. They use bridges defined earlier, so it is important to define them,
even if you don't plan on providing XMPP-bridges to OpenAI.

To add text generated by ChatGPT to Markdown content, add a code block as follows:

	```chatgpt,ChatGPT:Text example
	What do you know about the XMPP protocol?
	```

The first `chatgpt` identifies the ChatGPT Code block Markdown extension defined in `ChatGptCodeBlock.cs` in the
`TAG.Content.Markdown.OpenAI` project. The second parameter (after the comma) `ChatGPT` refers to a Node with the `ID` property
set to this value. The text after the colon `:` is the title of the code block. (For text generation, this title is not shown).

For showing an image, generated by DALL-E, a similar construct can be used:

	```dalle512,DallE:A blue dragon on skates
	A blue dragon on skates
	```

The first `dalle512` identifies the DALL-E Code block Markdown extension defined in `DallECodeBlock.cs` in the same project. It
also defines the size of images generated (512x512). (There are three sizes supported by DALL-E: 256x256, 512x512 and 1024x1024).
The second parameter (after the comma) identifies the corresponding node to use. It must have its `ID` property set to the same
value. The text after the colon `:` is the title of the code block, and will be shown below the image.

**Note**: It is the node defined above, that contains the API Key. This key is necessary for making the call. You can use
different nodes in different content, if you wish to use different API Keys (and therefore distribute associated costs, depending
on type of content.)

## Trying the XMPP Bridges

You can try the bridges without installing the package, by connecting and chatting with the following JIDs:

| Open OpenAI-bridges available ||
| JID | Description              |
|:---------------------------|:------------------------------------------------|
| `chatgpt@lab.tagroot.io`   | ChatGPT-XMPP bridge with minimal instruction.   |
| `dalle256@lab.tagroot.io`  | DALL-E-XMPP bridge generating 256x256 images.   |
| `dalle512@lab.tagroot.io`  | DALL-E-XMPP bridge generating 512x512 images.   |
| `dalle1024@lab.tagroot.io` | DALL-E-XMPP bridge generating 1024x1024 images. |

If you use an XMPP Client / Chat Client, that can scan QR codes, you can also scan the following codes to interact with the
above bridges:

![chatgpt@lab.tagroot.io](https://lab.tagroot.io/QR/xmpp:chatgpt@lab.tagroot.io)

![dalle256@lab.tagroot.io](https://lab.tagroot.io/QR/xmpp:dalle256@lab.tagroot.io)

![dalle512@lab.tagroot.io](https://lab.tagroot.io/QR/xmpp:dalle512@lab.tagroot.io)

![dalle1024@lab.tagroot.io](https://lab.tagroot.io/QR/xmpp:dalle1024@lab.tagroot.io)
