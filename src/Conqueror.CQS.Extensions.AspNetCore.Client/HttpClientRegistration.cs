﻿using System;
using System.Net.Http;

namespace Conqueror.CQS.Extensions.AspNetCore.Client
{
    internal sealed class HttpClientRegistration
    {
        public HttpClientRegistration(Type handlerType)
        {
            HandlerType = handlerType;
        }

        public Type HandlerType { get; }
        
        public Action<ConquerorCqsHttpClientOptions>? ConfigurationAction { get; init; }
        
        public Func<IServiceProvider, HttpClient>? HttpClientFactory { get; init; }
        
        public Func<IServiceProvider, Uri>? BaseAddressFactory { get; init; }
    }
}