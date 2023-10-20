# Getting Started

Foundry docgen uses a combination of docfx and editor ui to help create documentation for a Unity Package

## Prerequisites

Install DocFx using the instructions here: https://dotnet.github.io/docfx/tutorial/docfx_getting_started.html

## Setup

To set up a Package to use Foundry Docgen, open Foundry > Documentation > Docgen Helper from the main menu, this will 
show you a list of every local package you have installed. 

If you have not set up that package yet you'll be presented with some options to help you get started. 
DocFx input path and DocFx config path can just be left as default unless you want to move them somewhere else.

I highly suggest keeping `Create Template Files` enabled, this will copy the DocsTemplate directory from this package to 
your package, with a template docfx.json, .gitignore, and docs structure. It also sets some example DocFx metadata to add 
in an icon and set the package name.

Once done, you can then press the Generate Docs button to generate docs, and Preview Docs to preview them.

## Troubleshooting

If you need to look at the logs from DocFx, you can enable the `Auto Preview Docs On Generation` option, this will force
the console window to stay open after generation, allowing you to see any errors that may have occurred.