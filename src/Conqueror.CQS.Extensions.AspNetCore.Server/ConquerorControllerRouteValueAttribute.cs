// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Conqueror.CQS.Extensions.AspNetCore.Server
{
    // NOTE: must be public for ASP Core to detect the attribute dynamically
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ConquerorControllerRouteValueAttribute : RouteValueAttribute
    {
        public ConquerorControllerRouteValueAttribute(string value)
            : base("controller", value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}
