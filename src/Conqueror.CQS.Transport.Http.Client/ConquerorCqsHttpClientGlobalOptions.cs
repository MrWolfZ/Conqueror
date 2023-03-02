using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace Conqueror.CQS.Transport.Http.Client;

public sealed class ConquerorCqsHttpClientGlobalOptions
{
    internal ConquerorCqsHttpClientGlobalOptions(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    public IHttpCommandPathConvention? CommandPathConvention { get; set; }

    public IHttpQueryPathConvention? QueryPathConvention { get; set; }

    internal HttpClient? GlobalHttpClient { get; private set; }

    internal Dictionary<Type, HttpClient>? CommandHttpClients { get; private set; }

    internal Dictionary<Type, HttpClient>? QueryHttpClients { get; private set; }

    internal Dictionary<Assembly, HttpClient>? AssemblyClients { get; private set; }

    public ConquerorCqsHttpClientGlobalOptions UseHttpClientForCommand<T>(HttpClient httpClient)
        where T : notnull
    {
        CommandHttpClients ??= new();

        CommandHttpClients[typeof(T)] = httpClient;

        return this;
    }

    public ConquerorCqsHttpClientGlobalOptions UseHttpClientForQuery<T>(HttpClient httpClient)
        where T : notnull
    {
        QueryHttpClients ??= new();

        QueryHttpClients[typeof(T)] = httpClient;

        return this;
    }

    public ConquerorCqsHttpClientGlobalOptions UseHttpClientForTypesFromAssembly(Assembly assembly, HttpClient httpClient)
    {
        AssemblyClients ??= new();

        AssemblyClients[assembly] = httpClient;

        return this;
    }

    public ConquerorCqsHttpClientGlobalOptions UseHttpClient(HttpClient httpClient)
    {
        GlobalHttpClient = httpClient;
        return this;
    }
}
