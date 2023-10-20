using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace Foundry.Docgen
{
    public class FoundryDocgenWindow : EditorWindow
    {
        [MenuItem("Foundry/Documentation/Docgen Helper")]
        public static void OpenWindow()
        {
            var window = GetWindow<FoundryDocgenWindow>("Foundry Docgen", true);
            window.Show();
        }

        private void CreateGUI()
        {
            DrawPackageList(rootVisualElement);
        }

        private async void DrawPackageList(VisualElement root)
        {
            var packagesRoot = new ScrollView();
            packagesRoot.style.backgroundColor = Color.clear;
            root.Add(packagesRoot);
            var placeholder = new Label("Loading local packages...");
            packagesRoot.Add(placeholder);
            
            var request = Client.List();
            // Wait for the request to complete
            while (!request.IsCompleted)
                await Task.Delay(100);
            
            packagesRoot.Remove(placeholder);

            var packages = request.Result;
            foreach (var package in packages)
            {
                if (package.source != PackageSource.Local)
                    continue;

                var packageFoldout = new Foldout();
                packageFoldout.value = false;
                packageFoldout.text = $"{package.name}";
                packagesRoot.Add(packageFoldout);
                
                packageFoldout.contentContainer.Add(new Label("path: " + package.resolvedPath.Replace("\\", "\\\\")));
                DrawDocgenUI(packageFoldout.contentContainer, package.resolvedPath);
                
                
            }
        }

        private void DrawDocgenUI(VisualElement root, string path)
        {
            var assetPath = Path.Join(path, "Documentation", "FoundryDocgenConfig.json");
            if (File.Exists(assetPath))
                DrawGenerateDocsUI(root, path);
            else
                DrawCreateDocsUI(root, path);
        }

        private void DrawCreateDocsUI(VisualElement root, string path)
        {
            var createDocsRoot = new Box();
            createDocsRoot.style.backgroundColor = Color.clear;
            root.Add(createDocsRoot);

            var message = new Label("Documentation config not found, would you like to create one?");
            createDocsRoot.Add(message);

            var docfxOutputPath = new TextField("DocFx Output Path");
            docfxOutputPath.value = "docfx_output";
            createDocsRoot.Add(docfxOutputPath);
            
            var docfxConfigPath = new TextField("DocFx Config Path");
            docfxConfigPath.value = "docfx.json";
            createDocsRoot.Add(docfxConfigPath);
            
            var createTemplateFiles = new Toggle("Create Template Files");
            createTemplateFiles.value = true;
            createTemplateFiles.tooltip = "Create a set of standard folders and markdown files";
            createDocsRoot.Add(createTemplateFiles);
            
            var createButton = new Button(() =>
            {
                var config = FoundryDocgenConfig.Create(Path.Join(path, "Documentation", "FoundryDocgenConfig.json"));
                config.DocfxOutputPath = docfxOutputPath.value;
                config.DocfxConfigPath = docfxConfigPath.value;
                
                Directory.CreateDirectory(Path.Join(path, "Documentation"));
                config.Save();
                root.Remove(createDocsRoot);
                
                var docPath = Path.Join(path, "Documentation");
                var docfxConfigFullPath = Path.Join(docPath, docfxConfigPath.value);
                if (!Directory.Exists(docfxConfigFullPath))
                    CreateTemplateDocFxConfig(docfxConfigFullPath);
                if(createTemplateFiles.value)
                    CreateTemplateDocs(path);
                DrawDocgenUI(root, path);
            });
            createButton.text = "Create Docs";
            createDocsRoot.Add(createButton);
        }
        
        private void DrawGenerateDocsUI(VisualElement root, string path)
        {
            var assetPath = Path.Join(path, "Documentation", "FoundryDocgenConfig.json");
            var asset = FoundryDocgenConfig.load(assetPath);
            var generateDocsRoot = new Box();
            generateDocsRoot.style.backgroundColor = Color.clear;
            root.Add(generateDocsRoot);
            
            var message = new Label("Docgen Settings:");
            generateDocsRoot.Add(message);

            var property = asset;
            
            var docfxOutputPath = new TextField("DocFx Output Path");
            docfxOutputPath.value = property.DocfxOutputPath;
            docfxOutputPath.RegisterValueChangedCallback(evt =>
            {
                property.DocfxOutputPath = evt.newValue;
                property.Save();
            });
            generateDocsRoot.Add(docfxOutputPath);
            
            var docfxConfigPath = new TextField("DocFx Config Path");
            docfxConfigPath.value = property.DocfxConfigPath;
            docfxConfigPath.RegisterValueChangedCallback(evt =>
            {
                property.DocfxConfigPath = evt.newValue;
                property.Save();
            });
            generateDocsRoot.Add(docfxConfigPath);
            
            var docfxTemplatePath = new TextField("DocFx Template Path");
            docfxTemplatePath.value = property.DocfxTemplate;
            docfxTemplatePath.RegisterValueChangedCallback(evt =>
            {
                property.DocfxTemplate = evt.newValue;
                property.Save();
            });
            generateDocsRoot.Add(docfxTemplatePath);

            var docfxList = new ListView(asset.DocfxMetadata, -1, () =>
            {
                var elem = new Box();
                elem.style.flexDirection = FlexDirection.Row;
                return elem;
            }, (elem, index) =>
            {
                var pair = asset.DocfxMetadata[index];
                elem.Clear();
                
                var key = new TextField();
                key.style.minWidth = 100;
                elem.Add(key);
                elem.Add(new Label("="));
                var value = new TextField();
                value.style.flexGrow = 1;
                elem.Add(value);
                
                key.value = pair.Key;
                key.RegisterValueChangedCallback(evt =>
                {
                    pair.Key = evt.newValue;
                    asset.Save();
                });
                
                value.value = pair.Value;
                value.RegisterValueChangedCallback(evt =>
                {
                    pair.Value = evt.newValue;
                    asset.Save();
                });
            });
            docfxList.showFoldoutHeader = true;
            docfxList.headerTitle = "DocFx Metadata";
            docfxList.reorderMode = ListViewReorderMode.Animated;
            docfxList.showAddRemoveFooter = true;
            docfxList.itemsAdded += i =>
            {
                asset.Save();
            };
            docfxList.itemsRemoved += i =>
            {
                asset.Save();
            };
            generateDocsRoot.Add(docfxList);
            
            
            
            generateDocsRoot.Add(new ToolbarSpacer());
            generateDocsRoot.Add(new Label("Actions:"));
            var serveToggle = new Toggle("Preview Docs");
            serveToggle.value = false;
            serveToggle.tooltip = "Start a local server to preview the docs when they are generated";
            generateDocsRoot.Add(serveToggle);
            
            var generateButton = new Button(() =>
            {
                GenerateDocs(path, asset, true);
            });
            generateButton.text = "Generate Docs";
            generateDocsRoot.Add(generateButton);
            var previewButton = new Button(() =>
            {
                ServeDocs(path, asset);
            });
            previewButton.text = "Preview Docs";
            generateDocsRoot.Add(previewButton);
        }

        [Serializable]
        struct AsmdefJson
        {
            public string name;
        }

        private List<string> GetCsprojForPackage(string path)
        {
            List<string> csproj = new();
            List<string> asmdefNames = new();
            foreach (var asmdef in Directory.GetFiles(path, "*.asmdef", SearchOption.AllDirectories))
            {
                var asmdefAsset = JsonUtility.FromJson<AsmdefJson>(File.ReadAllText(asmdef));
                asmdefNames.Add(asmdefAsset.name);
            }

            foreach (var asmdefName in asmdefNames)
            {
                var csprojPath = Path.Join(Application.dataPath, "..", asmdefName + ".csproj");
                if (File.Exists(csprojPath))
                    csproj.Add(csprojPath);
            }

            return csproj;
        }

        private Process _compileProcess;
        private Process _serveProcess;

        private void GenerateDocs(string path, FoundryDocgenConfig config, bool serve = false)
        {
            if(_compileProcess?.HasExited ?? false)
                _compileProcess.Kill();
            var docsPath = Path.Join(path, "Documentation");
                
            // Copy the artifacts we need into here
            Directory.CreateDirectory(Path.Join(docsPath, "Temp"));
            foreach (var csproj in GetCsprojForPackage(path))
                File.Copy(csproj, Path.Join(docsPath,  "Temp",  Path.GetFileName(csproj)), true);
            
            var configPath = Path.Join(docsPath, config.DocfxConfigPath);
            var outputPath = Path.Join(docsPath, config.DocfxOutputPath);

            string metadata = "";
            foreach (var data in config.DocfxMetadata)
            {
                metadata += $"-m \"{data.Key}\"=\"{data.Value}\" ";
            }
            
            var templateFlag = string.IsNullOrEmpty(config.DocfxTemplate) ? "" : $"-t {config.DocfxTemplate}";
            var serveFlag = serve ? "--serve" : "";
            if (serve && (!_serveProcess?.HasExited ?? false))
                _serveProcess.Kill();

            var processInfo = new ProcessStartInfo("docfx");
            processInfo.Arguments = $" \"{configPath}\" -o \"{outputPath}\" -o \"{outputPath}\" {templateFlag} {metadata} {serveFlag}";
            processInfo.WorkingDirectory = docsPath;

            Debug.Log("Generating docs for " + path + " with command: docfx " + processInfo.Arguments);
            _compileProcess = Process.Start(processInfo);
        }

        private void ServeDocs(string path, FoundryDocgenConfig config)
        {
            if(!_serveProcess?.HasExited ?? false)
                _serveProcess.Kill();
            
            var docsPath = Path.Join(path, "Documentation");
            var processInfo = new ProcessStartInfo("docfx");
            
            var outputPath = Path.Join(docsPath, config.DocfxOutputPath);

            processInfo.Arguments = $"serve \"{outputPath}\" --open-browser";
            processInfo.WorkingDirectory = docsPath;

            Debug.Log("Generating docs for " + path + " with command: docfx " + processInfo.Arguments);
            _serveProcess = Process.Start(processInfo);
        }

        private void CreateTemplateDocs(string path)
        {
            var docPath = Path.Join(path, "Documentation");
            Directory.CreateDirectory(Path.Join(docPath, "Api"));
            Directory.CreateDirectory(Path.Join(docPath, "Manual"));
            Directory.CreateDirectory(Path.Join(docPath, "Media"));
            File.WriteAllText(Path.Join(docPath, "Api", "index.md"), "# API Reference");
            
            // Create the suggested docs structure from https://docs.unity3d.com/Manual/cus-document.html
            File.WriteAllText(Path.Join(docPath, "index.md"), @"# Documentation
## Overview
Provide a brief, high-level explanation of the package.

## Package Contents
Include the location of important files you want the user to know about. For example, if this is a sample package containing textures, models, and materials separated by sample group, you might want to provide the folder location of each group.

## Installation instructions
You can point to the official Package Manager installation instructions, but if you have any special installation requirements, such as installing samples, add them here.

## Requirements
This is a good place to add hardware or software requirements, including which versions of the Unity Editor this package is compatible with.

## Limitations
If your package has any known limitations, you can list them here. If not, or if the limitations are trivial, exclude this section.

## Workflows
Include a list of steps that the user can easily follow that demonstrates how to use the feature. You can include screenshots to help describe how to use the feature.

## Advanced topics
This is where you can provide detailed information about what you are providing to users. This is ideal if you don’t want to overwhelm the user with too much information up front.

## Reference
If you have a user interface with a lot of properties, you can provide the details in a reference section. Using tables is a good way to provide quick access to specific property descriptions.

## Samples
For packages that include sample files, you can include detailed information on how the user can use these sample files in their projects and scenes.

## Tutorials
If you want to provide walkthroughs for complicated procedures, you can also add them here. Use step-by-step instructions and include images if they can help the user understand.
");
            File.WriteAllText(Path.Join(docPath, "toc.yml"), "### YamlMime:TableOfContent\nitems:\n- name: Home\n  href: index.md\n- name: Manual\n  href: Manual/\n- name: Api\n  href: Api/");
            File.WriteAllText(Path.Join(docPath, ".gitignore"), "Temp\n");
        }
        
        private void CreateTemplateDocFxConfig(string path)
        {
            var json = @"{
    ""metadata"": [
       {
           ""src"": [
               {
                   ""src"" : ""temp"",
                   ""files"": [
                       ""**.csproj""
                   ]
               }
           ],
           ""dest"": ""Api""
       }
    ],
    ""build"": {
       ""content"": [
         {
           ""files"": [
             ""toc.yml"",
             ""index.md""
           ]
         },
         {
           ""files"": [
             ""api/toc.yml"",
             ""api/**/*.yml"",
             ""api/**/*.md""
           ]
         },
         {
           ""files"": [
             ""manual/**/*.md"",
             ""manual/**/*.yml""
           ]
         }
       ],
       ""resource"": [
           {
               ""files"": [
                   ""Media/**""
               ]
           }
       ],
       ""overwrite"": """",
       ""dest"": """",
       ""globalMetadataFiles"": [],
       ""template"": [
         ""default"", 
         ""modern""
       ],
      ""postProcessors"": [],
      ""keepFileLink"": false,
      ""disableGitFeatures"": false
    }
}
";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
        }
    }
}
