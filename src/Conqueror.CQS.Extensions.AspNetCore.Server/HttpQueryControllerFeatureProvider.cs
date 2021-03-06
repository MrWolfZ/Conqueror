// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Conqueror.CQS.Extensions.AspNetCore.Server
{
    internal sealed class HttpQueryControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly DynamicQueryControllerFactory controllerFactory;
        private readonly IReadOnlyCollection<QueryHandlerMetadata> metadata;

        public HttpQueryControllerFeatureProvider(DynamicQueryControllerFactory controllerFactory, IEnumerable<QueryHandlerMetadata> metadata)
        {
            this.controllerFactory = controllerFactory;
            this.metadata = metadata.ToList();
        }

        public void PopulateFeature(
            IEnumerable<ApplicationPart> parts,
            ControllerFeature feature)
        {
            foreach (var query in GetHttpQueries())
            {
                var controllerType = controllerFactory.Create(query, query.QueryType.GetCustomAttribute<HttpQueryAttribute>()!).GetTypeInfo();

                if (!feature.Controllers.Contains(controllerType))
                {
                    feature.Controllers.Add(controllerType);
                }
            }
        }

        private IEnumerable<QueryHandlerMetadata> GetHttpQueries() => metadata.Where(m => m.QueryType.GetCustomAttributes(typeof(HttpQueryAttribute), true).Any());
    }
}
