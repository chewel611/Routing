// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Routing.LinkGeneration
{
    public class SingleRouteWithParametersBenchmark
    {
        private TreeRouter _treeRouter;
        private readonly LinkGenerator _linkGenerator;

        public SingleRouteWithParametersBenchmark()
        {
            var services = CreateBasicServices();

            // Build Endpoint
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<EndpointDataSource>(
                    new DefaultEndpointDataSource(
                        new MatcherEndpoint(
                            MatcherEndpoint.EmptyInvoker,
                            RoutePatternFactory.Parse(
                                "Customers/Details/{category}/{region}/{id}",
                                defaults: new { controller = "Customers", action = "Details" },
                                constraints: null),
                            order: 0,
                            metadata: new EndpointMetadataCollection(
                                new RouteValuesAddressMetadata(
                                    name: string.Empty,
                                    requiredValues: new RouteValueDictionary(
                                        new { controller = "Customers", action = "Details" }))),
                            displayName: string.Empty))));

            var serviceProvider = services.BuildServiceProvider();

            // Build TreeRouter
            var treeRouteBuilder = serviceProvider.GetRequiredService<TreeRouteBuilder>();
            treeRouteBuilder.MapOutbound(
                NullRouter.Instance,
                new RouteTemplate(
                    RoutePatternFactory.Parse(
                        "Customers/Details/{category}/{region}/{id}",
                        defaults: new { controller = "Customers", action = "Details" },
                        constraints: null)),
                requiredLinkValues: new RouteValueDictionary(new { controller = "Customers", action = "Details" }),
                routeName: string.Empty,
                order: 0);
            _treeRouter = treeRouteBuilder.Build();

            _linkGenerator = serviceProvider.GetRequiredService<LinkGenerator>();
        }

        [Benchmark]
        public void UsingTreeRouter()
        {
            var virtualPathData = _treeRouter.GetVirtualPath(new VirtualPathContext(
                new DefaultHttpContext(),
                ambientValues: new RouteValueDictionary(),
                values: new RouteValueDictionary(
                    new
                    {
                        controller = "Customers",
                        action = "Details",
                        category = "Administration",
                        region = "US",
                        id = 10
                    })));

            Validate(virtualPathData?.VirtualPath);
        }

        [Benchmark]
        public void UsingEndpointRouting()
        {
            var actualUrl = _linkGenerator.GetLink(
                new DefaultHttpContext(),
                values: new RouteValueDictionary(
                    new
                    {
                        controller = "Customers",
                        action = "Details",
                        category = "Administration",
                        region = "US",
                        id = 10
                    }));

            Validate(actualUrl);
        }

        private void Validate(string actualUrl)
        {
            var expectedUrl = "/Customers/Details/Administration/US/10";
            if (actualUrl != expectedUrl)
            {
                throw new InvalidOperationException($"Expected: {expectedUrl}, Actual: {actualUrl}");
            }
        }

        private IServiceCollection CreateBasicServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            services.AddLogging();
            services.AddOptions();
            services.AddRouting();
            return services;
        }
    }
}
