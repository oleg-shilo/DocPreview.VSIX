[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://oleg-shilo.github.io/cs-script/Donation.html)

You can find the extension binaries on the Releases page: https://github.com/oleg-shilo/DocPreview.VSIX/releases

*This is a source code and defect-tracking repository only. For all binaries please visit the product [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=OlegShilo.DocPreview-2017) page.*

*This source code is for the Visual Studio 2017 (VS2017) version of the extension. The VS2015 source code is hosted in a separate [repository](https://docpreview.codeplex.com/)*

## Project Description

There are many products that assist with producing the API Documentation from XML code comments within C# source code. Some of these products are open source and some are proprietary but most of them are doing very good job for what they are designed to do. 

However, most of these products fall short of assisting with the actual development of the documentation: editing XML code comments as opposed to building the final documentation. This is because the majority of the tools are concentrating on the actual authoring of the final documentation and styling it according to one or another _Help format_.

DocPreview is different. It provides an instantaneous preview of a given single XML comments block for C#, C/C++ and VB.NET syntax. DocPreview assists with validating the user input for being compatible with the documentation authoring tools (e.g. Sandcastle). Thus DocPreview is concentrating purely on efficient editing of the documentation comments.

DocPreview deliberately doesn't strongly adhere to any of the API Documentation formats (e.g. HTML Help 1/2, MS Help Viewer, Open XML). Instead, it concerns only the validity of the documentation source (XML) and renders the preview in the custom format loosely based on one of the standard HTML Help formats.

DocPreview is reusing some ported/adjusted bits from ImmDoc.NET (https://immdocnet.codeplex.com/). It is rather an excellent HTML documentation-building tool, which is a lightweight and lightning-fast alternative to Sandcastle. Special thanks to Marek Stoj who is the author of ImmDoc.NET.  

## Using DocPreview

Using DocPreview is straightforward. Open the preview window from Menu->View->Other Windows. Now if you place the cursor inside of the XML code comments the documentation preview will be rendered when you press the refresh icon/button. Alternatively, you can tick the 'Auto refresh' checkbox  and the preview will be generated automatically.

_DocPreview supports all [recommended documentation tags](https://msdn.microsoft.com/en-us/library/5ast78ax.aspx) except `<include>`, which is not a subject of preview._

Below is the screenshot that illustrates the use of DocPreview:

![](https://github.com/oleg-shilo/DocPreview.VSIX/raw/master/DocPreview/DocPreview/Resources/preview.large.png)

## Limitations

DocPreview is designed to be light and it does not aim to build chm files or even to provide ultimately accurate preview rendering of the future documentation. Thus it is not a "compiler" but rather a "linter", which helps avoid misformatted or even invalid XML documentation content with a very short feedback loop. 

And you may also find that for some more advanced preview scenarios, you may need to go with the higher-grade licensed industrial tools. 

Please note that some of the newer XML Doc tags that have been introduced after the release of this extension will be ignored in the member documentation preview text. Let me know such cases and I will see what can be done. In many cases extending the tag support was relatively easy. Though in some others a significant investment is required (e.g. [#17](https://github.com/oleg-shilo/DocPreview.VSIX/issues/17)) thus I can use your help. But at least vote for the feature requests so I can make the decision to invest in it or not.  

### `<inheritdoc/>` Limitations
One of the XML documentation features requires special mentioning - `<inheritdoc/>`. It is a relatively new feature of Sandcastle (~2022). The idea is simple - you can define your XML documentation by pulling multiple XML fragments from other classes' documentation (or even different files). The benefits of these features are debatable as they lead to fragmented less maintainable documentation. Thus GhostDoc (as opposed to Sandcastle) implements documentation "inheritance" by removing this tag completely from he derived classes (on Ctrl+Shift+D) and replicating it with the  documentation base class documentation content. 

Nevertheless, some developers do rely on `<inheritdoc/>` so starting from v1.6 DocPreview supports previewing `<inheritdoc/>`. Though there are fundamental differences between previewing and compiling documentation: DocPreview analyzes complex and imperfect CodeDom and Sandcastle analyses perfect compiled assemblies). This difference leads to the `<inheritdoc/>` specific limitations:

`<inheritdoc/>` is not supported: 

- For non-C# syntax
- `<inheritdoc/>` references to the external assemblies cannot be resolved. DocPreview analyses only code but not assemblies.
- Types marked with a single `<inheritdoc/>` statement are not resolved for the type members. You need to either preview the base type or place `<inheritdoc/>` on the type members.


