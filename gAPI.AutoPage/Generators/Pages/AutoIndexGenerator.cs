using gAPI.AutoPage.Interfaces;
using gAPI.AutoPage.Models;
using gAPI.AutoPage.Models.Configs;
using gAPI.AutoPage.SimpleRazorCompiler;
using System;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoPage.Generators.Pages
{
    public class AutoIndexGenerator : BaseGenerator, IPageIndex
    {
        public AutoIndexGenerator(
            Generator generator,
            string key, 
            AutoPageGenerator[] autoPageGenerators,
            PageConfig config)
        {
            Context = generator;

            Generator = new IndexGenerator(
                key, 
                autoPageGenerators.Select(a => a.Generator), 
                this,
                config.Pages_Destination.Directory,
                config.Pages_Destination.Namespace);

            FileName = $"{Name}.g.cs";
        }

        public Generator Context { get; }
        public IndexGenerator Generator { get; }

        public override string Namespace => Generator.Namespace;
        public override string Directory => Generator.Directory;
        public override string Name => Generator.Name;
        public string Route => Generator.Route;
        public string? Title => Generator.Title;
        public IEnumerable<IPage> Pages => Generator.Pages;

        internal void GenerateCode()
        {
            Generator.GenerateCode();

            var razorCode = GetRazorNamespacesCode() + "\r\n" + Generator.Code;
            Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace, Name, Context.SharedReferences.AllComponents);
        }
    }
}