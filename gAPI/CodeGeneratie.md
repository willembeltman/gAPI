🔹 3 manieren om C#-code te genereren

1. Pre-compile time – Reflection tools

Je kunt tijdens de build-fase een tool laten draaien die via reflection metadata uit bestaande assemblies leest.
Op basis daarvan genereer je automatisch nieuwe C#-bestanden met klassen, methoden of eigenschappen.
Ideaal voor het aanmaken van DTO’s, API-clients of andere afgeleide types.

2. Run-time – Roslyn Scripting API

Met de Roslyn Scripting API kun je C#-code tijdens runtime compileren en uitvoeren.
Je definieert codefragmenten als string, compileert ze on the fly, en voert ze direct uit — zonder aparte build-stap.
Perfect voor dynamische scenario’s, zoals gebruikersscripts of runtime codegeneratie.

3. Compile-time – Roslyn Source Generators

Source Generators draaien tijdens de compilatie zelf, binnen Roslyn.
Ze analyseren je code en voegen automatisch nieuwe C#-bestanden toe aan de build.
Ideaal om boilerplate te elimineren en patronen te automatiseren, zonder runtime overhead.