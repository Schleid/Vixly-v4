using System.Reflection;
using System.Runtime.CompilerServices;
using Vixly.Platform.Application.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Architecture.Tests;

// =========================================================
// FILE: UseCaseRulesTests.cs
//
// PURPOSE:
// Enforces the three-files-per-use-case rules of Global
// Standard 5.2 on every Application assembly.
//
// RESPONSIBILITIES:
// - Require Request and Response beside every UseCase
// - Require sealed public types
// - Require init-only Request properties and private-init
//   Response properties
// - Require Response.Failure to take an Error (a code), never
//   a sentence (Global Standard 5.5)
// - Require a typed authorization context first and a
//   cancellation token last on ExecuteAsync
// - Require constructor dependencies to be null-checked
//
// IMPORTANT NOTES:
// - Vacuous until the first use case exists; the scan-reach
//   test proves the assemblies are actually inspected
// - Null-check enforcement is behavioral: each constructor is
//   called with a null for every parameter in turn
// =========================================================
public sealed class UseCaseRulesTests
{
    private static readonly Assembly[] ApplicationAssemblies =
    {
        typeof(PlatformAuthorizationContext).Assembly
    };

    private static IEnumerable<Type> UseCases()
    {
        return ApplicationAssemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.Name.EndsWith("UseCase", StringComparison.Ordinal));
    }

    [Fact]
    public void ApplicationAssemblies_AreInspected()
    {
        Assert.Equal("Vixly.Platform.Application", ApplicationAssemblies[0].GetName().Name);
    }

    [Fact]
    public void EveryUseCase_HasRequestAndResponseInTheSameNamespace()
    {
        foreach (Type useCase in UseCases())
        {
            string stem = useCase.Name[..^"UseCase".Length];

            Type? request = useCase.Assembly.GetType($"{useCase.Namespace}.{stem}Request");
            Type? response = useCase.Assembly.GetType($"{useCase.Namespace}.{stem}Response");

            Assert.True(request is not null, $"{useCase.FullName}: {stem}Request is missing.");
            Assert.True(response is not null, $"{useCase.FullName}: {stem}Response is missing.");
            Assert.True(useCase is { IsSealed: true, IsPublic: true }, $"{useCase.FullName} must be public sealed.");
            Assert.True(request is { IsSealed: true, IsPublic: true }, $"{request!.FullName} must be public sealed.");
            Assert.True(response is { IsSealed: true, IsPublic: true }, $"{response!.FullName} must be public sealed.");
        }
    }

    [Fact]
    public void Requests_AreInitOnly_AndResponsesArePrivateInit()
    {
        foreach (Type useCase in UseCases())
        {
            string stem = useCase.Name[..^"UseCase".Length];
            Type request = useCase.Assembly.GetType($"{useCase.Namespace}.{stem}Request")!;
            Type response = useCase.Assembly.GetType($"{useCase.Namespace}.{stem}Response")!;

            foreach (PropertyInfo property in request.GetProperties())
            {
                MethodInfo? setter = property.SetMethod;

                Assert.True(
                    setter is null || IsInitOnly(setter),
                    $"{request.FullName}.{property.Name} must be '{{ get; init; }}'.");
            }

            foreach (PropertyInfo property in response.GetProperties())
            {
                Assert.True(
                    property.GetSetMethod(nonPublic: false) is null,
                    $"{response.FullName}.{property.Name} must be '{{ get; private init; }}'.");
            }

            Assert.NotNull(response.GetMethod("Success", BindingFlags.Public | BindingFlags.Static));

            MethodInfo? failure = response.GetMethod("Failure", BindingFlags.Public | BindingFlags.Static);

            Assert.NotNull(failure);
            Assert.True(
                failure!.GetParameters().Length == 1
                && failure.GetParameters()[0].ParameterType == typeof(Error),
                $"{response.FullName}.Failure must take a single Error, not text.");
        }
    }

    [Fact]
    public void ExecuteAsync_TakesAuthorizationContextFirstAndCancellationTokenLast()
    {
        foreach (Type useCase in UseCases())
        {
            MethodInfo[] methods = useCase
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => method.Name == "ExecuteAsync")
                .ToArray();

            Assert.True(methods.Length == 1, $"{useCase.FullName} must expose exactly one ExecuteAsync.");

            ParameterInfo[] parameters = methods[0].GetParameters();

            Assert.True(parameters.Length == 3, $"{useCase.FullName}.ExecuteAsync takes (context, request, token).");
            Assert.EndsWith("AuthorizationContext", parameters[0].ParameterType.Name);
            Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
        }
    }

    [Fact]
    public void EveryUseCase_NullChecksEachConstructorDependency()
    {
        foreach (Type useCase in UseCases())
        {
            foreach (ConstructorInfo constructor in useCase.GetConstructors())
            {
                ParameterInfo[] parameters = constructor.GetParameters();

                for (int index = 0; index < parameters.Length; index++)
                {
                    object?[] arguments = parameters
                        .Select((parameter, position) =>
                            position == index ? null : CreateStub(parameter.ParameterType))
                        .ToArray();

                    TargetInvocationException exception =
                        Assert.Throws<TargetInvocationException>(() => constructor.Invoke(arguments));

                    Assert.IsType<ArgumentNullException>(exception.InnerException);
                }
            }
        }
    }

    private static bool IsInitOnly(MethodInfo setter)
    {
        return setter.ReturnParameter
            .GetRequiredCustomModifiers()
            .Contains(typeof(IsExternalInit));
    }

    private static object CreateStub(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type)!;
        }

        if (type.IsInterface)
        {
            return typeof(DispatchProxy)
                .GetMethod(nameof(DispatchProxy.Create))!
                .MakeGenericMethod(type, typeof(StubProxy))
                .Invoke(null, null)!;
        }

        return RuntimeHelpers.GetUninitializedObject(type);
    }

    public class StubProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return null;
        }
    }
}
