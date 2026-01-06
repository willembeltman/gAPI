using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gAPI.AutoCrud
{
    [Generator]
    public class Generator : IIncrementalGenerator
    {
        //public void Initialize(IncrementalGeneratorInitializationContext context)
        //{
        //    context.RegisterSourceOutput(context.CompilationProvider, (spc, compilation) =>
        //    {
        //        //#if DEBUG
        //        //                    if (!Debugger.IsAttached)
        //        //                    {
        //        //                        Debugger.Launch(); // Triggert dialoog om te attachen
        //        //                    }
        //        //#endif

        //        try
        //        {
        //            // Zoek alle handlers op: overerven van gAPI.Interfaces.IHandler<TEntity, TDto, TKey>
        //            // Zoek alle handlers op: overerven van gAPI.AutoMapper.CustomMapping<TEntity, TDto>

        //            // Zoek alle paren TEntity, TDto
        //            // Check dat ze er allemaal zijn

        //            // Tot hier pak ik het op

        //        }
        //        catch (Exception ex)
        //        {
        //            ShowError(ex.ToString(), spc);
        //            //throw;
        //        }
        //    });
        //}
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {


            var handlers = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (n, _) => n is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax,
                    static (ctx, _) => GetHandlerInfo(ctx))
                .Where(static x => x != null)
                .Select(static (x, _) => x!)
                .Collect();

            var mappers = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (n, _) => n is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax,
                    static (ctx, _) => GetMapperInfo(ctx))
                .Where(static x => x != null)
                .Select(static (x, _) => x!)
                .Collect();

            var pairs = handlers.Combine(mappers)
                .SelectMany(static (x, _) =>
                {
                    var result = new List<CrudContract>();

                    foreach (var h in x.Left)
                        foreach (var m in x.Right)
                        {
                            var pair = MatchPairs(h, m);
                            if (pair != null)
                                result.Add(pair.Value);
                        }

                    return result;
                });

            context.RegisterSourceOutput(pairs, static (spc, pair) =>
            {
                EmitCrud(pair, spc);
            });
        }


        static (INamedTypeSymbol handler, INamedTypeSymbol entity, INamedTypeSymbol dto, ITypeSymbol key)? GetHandlerInfo(GeneratorSyntaxContext ctx)
        {
            var symbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) as INamedTypeSymbol;
            if (symbol == null) return null;

            var iface = symbol.AllInterfaces.FirstOrDefault(i =>
                i.Name == "IHandler" && i.TypeArguments.Length == 3);

            if (iface == null) return null;

            return (symbol,
                    (INamedTypeSymbol)iface.TypeArguments[0],
                    (INamedTypeSymbol)iface.TypeArguments[1],
                    iface.TypeArguments[2]);
        }

        static (INamedTypeSymbol mapper, INamedTypeSymbol entity, INamedTypeSymbol dto)? GetMapperInfo(GeneratorSyntaxContext ctx)
        {
            var symbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) as INamedTypeSymbol;
            if (symbol == null) return null;

            var baseType = symbol.BaseType;
            while (baseType != null)
            {
                if (baseType.Name == "CustomMapping" && baseType.TypeArguments.Length == 2)
                {
                    return (symbol,
                        (INamedTypeSymbol)baseType.TypeArguments[0],
                        (INamedTypeSymbol)baseType.TypeArguments[1]);
                }
                baseType = baseType.BaseType;
            }
            return null;
        }

        private static CrudContract? MatchPairs(
            (INamedTypeSymbol handler, INamedTypeSymbol entity, INamedTypeSymbol dto, ITypeSymbol key)? h,
            (INamedTypeSymbol mapper, INamedTypeSymbol entity, INamedTypeSymbol dto)? m)
        {
            if (!SymbolEqualityComparer.Default.Equals(h.Value.entity, m.Value.entity)) return null;
            if (!SymbolEqualityComparer.Default.Equals(h.Value.dto, m.Value.dto)) return null;

            return new CrudContract(h.Value.entity, h.Value.dto, h.Value.key, h.Value.handler, m.Value.mapper);
        }

        static void EmitCrud(CrudContract c, SourceProductionContext spc)
        {
            var ns = c.Entity.ContainingNamespace.ToDisplayString();
            var name = c.Entity.Name;

            var src = $@"";

            spc.AddSource($"CrudServices\\{name}Service.g.cs", SourceText.From(src, Encoding.UTF8));
            spc.AddSource($"MappingExtentions\\{name}MappingExtentions.g.cs", SourceText.From(src, Encoding.UTF8));
        }


        public void ShowError(Exception exception, SourceProductionContext CurrentSpc)
        {
            ShowError(exception.Message, CurrentSpc);
        }

        public void ShowError(string errorMessage, SourceProductionContext CurrentSpc)
        {
            //throw new Exception(errorMessage); // Helps while debugging
            var sourceCode = $"#error gAPI.AutoHub: {errorMessage.Replace("\r", "").Replace("\n", " ")}";
            CurrentSpc.AddSource("Gapi_Error.AutoHub.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
        }

    }
    internal struct CrudContract
    {
        public CrudContract(
        INamedTypeSymbol Entity,
        INamedTypeSymbol Dto,
        ITypeSymbol Key,
        INamedTypeSymbol Handler,
        INamedTypeSymbol Mapper)
        {
            this.Entity = Entity;
            this.Dto = Dto;
            this.Key = Key;
            this.Handler = Handler;
            this.Mapper = Mapper;
        }

        public INamedTypeSymbol Entity { get; }
        public INamedTypeSymbol Dto { get; }
        public ITypeSymbol Key { get; }
        public INamedTypeSymbol Handler { get; }
        public INamedTypeSymbol Mapper { get; }
    }
}