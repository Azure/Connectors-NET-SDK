# Generated scalar contract fixture

`scalarunionfixture.swagger.json` is the synthetic schema used for the scalar-map
work in [NodeJS PR 113](https://github.com/Azure/Connectors-NodeJS-SDK/pull/113).
It contains no live connection or customer data. `ScalarunionfixtureExtensions.cs`
is the complete generated C# client, compiled only into this test project against
the real SDK. Never edit the generated file by hand.

Use the compiler built from the generator revision linked in the SDK pull request:

```powershell
LogicAppsCompiler.exe <existing-output-directory> unused --directClient --language=csharp --connectors=scalarunionfixture --swagger-input-directory=<this-directory>
```

Copy only `ScalarunionfixtureExtensions.cs` from that output directory. The input
SHA-256 is `661295288720799FA674E573EEC075001D503376AE8A9F6B392141CC62F4D0D3`.
The SDK pull request records the pinned generator revision, output hash, live
refresh scope, and its upstream merge gate.

`ScalarUnionContractTests` exercises the generated request through the real SDK's
injected HTTP transport and deserializes its response. The POST request includes
both schema-required maps and asserts that a declared null map entry stays in the
captured JSON. It also checks object,
array, undeclared-null, fractional-integer, and default-value rejection. The
paired generator tests compile positive consumers and verify that structured
assignments fail compilation. Open SQL and Fin & Ops maps are separate controls:
those payloads must keep accepting nested JSON, not become scalar-only.
