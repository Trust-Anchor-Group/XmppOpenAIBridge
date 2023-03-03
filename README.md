# XmppOpenAIBridge

Provides a bridge between the XMPP Instant Messaging protocol, and OpenAI.

The solution contains the following C# projects:

| Project                      | Framework         | Description |
|:-----------------------------|:------------------|:------------|
| `TAG.Networking.OpenAI`      | .NET Standard 2.0 | Class library for communicating with OpenAI services via the [OpenAI API](https://platform.openai.com/overview). |
| `TAG.Networking.OpenAI.Test` | .NET 6.0          | Unit tests for the `TAG.Networking.OpenAI` library. |
| `TAG.Things.OpenAI`          | .NET Standard 2.0 | Publishes harmonized interfaces for administering access to OpenAI. |

The following nugets are used, enabling the libraries to be hosted on an [IoT Gateway](https://github.com/PeterWaher/IoTGateway).
This includes hosting the bridge on the [TAG Neuron](https://lab.tagroot.io/Documentation/Index.md). They can also be used
standalone.

| Nuget                                                                  | Description |
|:-----------------------------------------------------------------------|:------------|
| [Waher.Content](https://www.nuget.org/packages/Waher.Content/)         | Provides a pluggable architecture for accessing, encoding and decoding Internet Content. |
| [Waher.Events](https://www.nuget.org/packages/Waher.Events/)           | Provides an extensible architecture for event logging in the application. |
| [Waher.Networking](https://www.nuget.org/packages/Waher.Networking/)   | Provides tools for working with communication, including troubleshooting. |
| [Waher.Things](https://www.nuget.org/packages/Waher.Things/)           | Provides a basic architecture enabling the harmonization of things across technology boundaries. |
| [Waher.Things.Xmpp](https://www.nuget.org/packages/Waher.Things.Xmpp/) | Provides harmonized extensions for XMPP-based communication and extensions. |
