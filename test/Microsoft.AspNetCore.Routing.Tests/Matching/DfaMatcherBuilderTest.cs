﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matching
{
    public class DfaMatcherBuilderTest
    {
        [Fact]
        public void BuildDfaTree_SingleEndpoint_Empty()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint = CreateEndpoint("/");
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Same(endpoint, Assert.Single(root.Matches));
            Assert.Null(root.Parameters);
            Assert.Empty(root.Literals);
        }

        [Fact]
        public void BuildDfaTree_SingleEndpoint_Literals()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint = CreateEndpoint("a/b/c");
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);
            Assert.Null(a.Parameters);

            next = Assert.Single(a.Literals);
            Assert.Equal("b", next.Key);

            var b = next.Value;
            Assert.Empty(b.Matches);
            Assert.Null(b.Parameters);

            next = Assert.Single(b.Literals);
            Assert.Equal("c", next.Key);

            var c = next.Value;
            Assert.Same(endpoint, Assert.Single(c.Matches));
            Assert.Null(c.Parameters);
            Assert.Empty(c.Literals);
        }

        [Fact]
        public void BuildDfaTree_SingleEndpoint_Parameters()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint = CreateEndpoint("{a}/{b}/{c}");
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Empty(root.Literals);

            var a = root.Parameters;
            Assert.Empty(a.Matches);
            Assert.Empty(a.Literals);

            var b = a.Parameters;
            Assert.Empty(b.Matches);
            Assert.Empty(b.Literals);

            var c = b.Parameters;
            Assert.Same(endpoint, Assert.Single(c.Matches));
            Assert.Null(c.Parameters);
            Assert.Empty(c.Literals);
        }

        [Fact]
        public void BuildDfaTree_SingleEndpoint_CatchAll()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint = CreateEndpoint("{a}/{*b}");
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Empty(root.Literals);

            var a = root.Parameters;

            // The catch all can match a path like '/a'
            Assert.Same(endpoint, Assert.Single(a.Matches));
            Assert.Empty(a.Literals);
            Assert.Null(a.Parameters);

            // Catch-all nodes include an extra transition that loops to process
            // extra segments.
            var catchAll = a.CatchAll;
            Assert.Same(endpoint, Assert.Single(catchAll.Matches));
            Assert.Empty(catchAll.Literals);
            Assert.Same(catchAll, catchAll.Parameters);
            Assert.Same(catchAll, catchAll.CatchAll);
        }

        [Fact]
        public void BuildDfaTree_SingleEndpoint_CatchAllAtRoot()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint = CreateEndpoint("{*a}");
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Same(endpoint, Assert.Single(root.Matches));
            Assert.Empty(root.Literals);

            // Catch-all nodes include an extra transition that loops to process
            // extra segments.
            var catchAll = root.CatchAll;
            Assert.Same(endpoint, Assert.Single(catchAll.Matches));
            Assert.Empty(catchAll.Literals);
            Assert.Same(catchAll, catchAll.Parameters);
        }

        [Fact]
        public void BuildDfaTree_MultipleEndpoint_LiteralAndLiteral()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint1 = CreateEndpoint("a/b1/c");
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("a/b2/c");
            builder.AddEndpoint(endpoint2);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);

            Assert.Equal(2, a.Literals.Count);

            var b1 = a.Literals["b1"];
            Assert.Empty(b1.Matches);
            Assert.Null(b1.Parameters);

            next = Assert.Single(b1.Literals);
            Assert.Equal("c", next.Key);

            var c1 = next.Value;
            Assert.Same(endpoint1, Assert.Single(c1.Matches));
            Assert.Null(c1.Parameters);
            Assert.Empty(c1.Literals);

            var b2 = a.Literals["b2"];
            Assert.Empty(b2.Matches);
            Assert.Null(b2.Parameters);

            next = Assert.Single(b2.Literals);
            Assert.Equal("c", next.Key);

            var c2 = next.Value;
            Assert.Same(endpoint2, Assert.Single(c2.Matches));
            Assert.Null(c2.Parameters);
            Assert.Empty(c2.Literals);
        }

        [Fact]
        public void BuildDfaTree_MultipleEndpoint_LiteralAndParameter()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint1 = CreateEndpoint("a/b/c");
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("a/{b}/c");
            builder.AddEndpoint(endpoint2);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);

            next = Assert.Single(a.Literals);
            Assert.Equal("b", next.Key);

            var b = next.Value;
            Assert.Empty(b.Matches);
            Assert.Null(b.Parameters);

            next = Assert.Single(b.Literals);
            Assert.Equal("c", next.Key);

            var c1 = next.Value;
            Assert.Collection(
                c1.Matches,
                e => Assert.Same(endpoint1, e),
                e => Assert.Same(endpoint2, e));
            Assert.Null(c1.Parameters);
            Assert.Empty(c1.Literals);

            var b2 = a.Parameters;
            Assert.Empty(b2.Matches);
            Assert.Null(b2.Parameters);

            next = Assert.Single(b2.Literals);
            Assert.Equal("c", next.Key);

            var c2 = next.Value;
            Assert.Same(endpoint2, Assert.Single(c2.Matches));
            Assert.Null(c2.Parameters);
            Assert.Empty(c2.Literals);
        }

        [Fact]
        public void BuildDfaTree_MultipleEndpoint_ParameterAndParameter()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint1 = CreateEndpoint("a/{b1}/c");
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("a/{b2}/c");
            builder.AddEndpoint(endpoint2);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);
            Assert.Empty(a.Literals);

            var b = a.Parameters;
            Assert.Empty(b.Matches);
            Assert.Null(b.Parameters);

            next = Assert.Single(b.Literals);
            Assert.Equal("c", next.Key);

            var c = next.Value;
            Assert.Collection(
                c.Matches,
                e => Assert.Same(endpoint1, e),
                e => Assert.Same(endpoint2, e));
            Assert.Null(c.Parameters);
            Assert.Empty(c.Literals);
        }

        [Fact]
        public void BuildDfaTree_MultipleEndpoint_LiteralAndCatchAll()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint1 = CreateEndpoint("a/b/c");
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("a/{*b}");
            builder.AddEndpoint(endpoint2);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Same(endpoint2, Assert.Single(a.Matches));

            next = Assert.Single(a.Literals);
            Assert.Equal("b", next.Key);

            var b1 = next.Value;
            Assert.Same(endpoint2, Assert.Single(a.Matches));
            Assert.Null(b1.Parameters);

            next = Assert.Single(b1.Literals);
            Assert.Equal("c", next.Key);

            var c1 = next.Value;
            Assert.Collection(
                c1.Matches,
                e => Assert.Same(endpoint1, e),
                e => Assert.Same(endpoint2, e));
            Assert.Null(c1.Parameters);
            Assert.Empty(c1.Literals);

            var catchAll = a.CatchAll;
            Assert.Same(endpoint2, Assert.Single(catchAll.Matches));
            Assert.Same(catchAll, catchAll.Parameters);
            Assert.Same(catchAll, catchAll.CatchAll);
        }

        [Fact]
        public void BuildDfaTree_MultipleEndpoint_ParameterAndCatchAll()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint1 = CreateEndpoint("a/{b}/c");
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("a/{*b}");
            builder.AddEndpoint(endpoint2);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Same(endpoint2, Assert.Single(a.Matches));
            Assert.Empty(a.Literals);

            var b1 = a.Parameters;
            Assert.Same(endpoint2, Assert.Single(a.Matches));
            Assert.Null(b1.Parameters);

            next = Assert.Single(b1.Literals);
            Assert.Equal("c", next.Key);

            var c1 = next.Value;
            Assert.Collection(
                c1.Matches,
                e => Assert.Same(endpoint1, e),
                e => Assert.Same(endpoint2, e));
            Assert.Null(c1.Parameters);
            Assert.Empty(c1.Literals);

            var catchAll = a.CatchAll;
            Assert.Same(endpoint2, Assert.Single(catchAll.Matches));
            Assert.Same(catchAll, catchAll.Parameters);
            Assert.Same(catchAll, catchAll.CatchAll);
        }

        [Fact]
        public void BuildDfaTree_WithPolicies()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder(new TestMetadata1MatcherPolicy(), new TestMetadata2MatcherPolicy());

            var endpoint1 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(0), new TestMetadata2(true), });
            builder.AddEndpoint(endpoint1);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);
            Assert.IsType<TestMetadata1MatcherPolicy>(a.NodeBuilder);
            Assert.Collection(
                a.PolicyEdges.OrderBy(e => e.Key),
                e => Assert.Equal(0, e.Key));

            var test1_0 = a.PolicyEdges[0];
            Assert.Empty(a.Matches);
            Assert.IsType<TestMetadata2MatcherPolicy>(test1_0.NodeBuilder);
            Assert.Collection(
                test1_0.PolicyEdges.OrderBy(e => e.Key),
                e => Assert.Equal(true, e.Key));

            var test2_true = test1_0.PolicyEdges[true];
            Assert.Same(endpoint1, Assert.Single(test2_true.Matches));
            Assert.Null(test2_true.NodeBuilder);
            Assert.Empty(test2_true.PolicyEdges);
        }

        [Fact]
        public void BuildDfaTree_WithPolicies_AndBranches()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder(new TestMetadata1MatcherPolicy(), new TestMetadata2MatcherPolicy());

            var endpoint1 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(0), new TestMetadata2(true), });
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(1), new TestMetadata2(true), });
            builder.AddEndpoint(endpoint2);

            var endpoint3 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(1), new TestMetadata2(false), });
            builder.AddEndpoint(endpoint3);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);
            Assert.IsType<TestMetadata1MatcherPolicy>(a.NodeBuilder);
            Assert.Collection(
                a.PolicyEdges.OrderBy(e => e.Key),
                e => Assert.Equal(0, e.Key),
                e => Assert.Equal(1, e.Key));

            var test1_0 = a.PolicyEdges[0];
            Assert.Empty(test1_0.Matches);
            Assert.IsType<TestMetadata2MatcherPolicy>(test1_0.NodeBuilder);
            Assert.Collection(
                test1_0.PolicyEdges.OrderBy(e => e.Key),
                e => Assert.Equal(true, e.Key));

            var test2_true = test1_0.PolicyEdges[true];
            Assert.Same(endpoint1, Assert.Single(test2_true.Matches));
            Assert.Null(test2_true.NodeBuilder);
            Assert.Empty(test2_true.PolicyEdges);

            var test1_1 = a.PolicyEdges[1];
            Assert.Empty(test1_1.Matches);
            Assert.IsType<TestMetadata2MatcherPolicy>(test1_1.NodeBuilder);
            Assert.Collection(
                test1_1.PolicyEdges.OrderBy(e => e.Key),
                e => Assert.Equal(false, e.Key),
                e => Assert.Equal(true, e.Key));

            test2_true = test1_1.PolicyEdges[true];
            Assert.Same(endpoint2, Assert.Single(test2_true.Matches));
            Assert.Null(test2_true.NodeBuilder);
            Assert.Empty(test2_true.PolicyEdges);

            var test2_false = test1_1.PolicyEdges[false];
            Assert.Same(endpoint3, Assert.Single(test2_false.Matches));
            Assert.Null(test2_false.NodeBuilder);
            Assert.Empty(test2_false.PolicyEdges);
        }

        [Fact]
        public void BuildDfaTree_WithPolicies_AndBranches_FirstPolicySkipped()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder(new TestMetadata1MatcherPolicy(), new TestMetadata2MatcherPolicy());

            var endpoint1 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata2(true), });
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata2(true), });
            builder.AddEndpoint(endpoint2);

            var endpoint3 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata2(false), });
            builder.AddEndpoint(endpoint3);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);
            Assert.IsType<TestMetadata2MatcherPolicy>(a.NodeBuilder);
            Assert.Collection(
                a.PolicyEdges.OrderBy(e => e.Key),
                e => Assert.Equal(false, e.Key),
                e => Assert.Equal(true, e.Key));

            var test2_true = a.PolicyEdges[true];
            Assert.Equal(new[] { endpoint1, endpoint2, }, test2_true.Matches);
            Assert.Null(test2_true.NodeBuilder);
            Assert.Empty(test2_true.PolicyEdges);

            var test2_false = a.PolicyEdges[false];
            Assert.Equal(new[] { endpoint3, }, test2_false.Matches);
            Assert.Null(test2_false.NodeBuilder);
            Assert.Empty(test2_false.PolicyEdges);
        }

        [Fact]
        public void BuildDfaTree_WithPolicies_AndBranches_SecondSkipped()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder(new TestMetadata1MatcherPolicy(), new TestMetadata2MatcherPolicy());

            var endpoint1 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(0), });
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(1), });
            builder.AddEndpoint(endpoint2);

            var endpoint3 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(1), });
            builder.AddEndpoint(endpoint3);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);
            Assert.IsType<TestMetadata1MatcherPolicy>(a.NodeBuilder);
            Assert.Collection(
                a.PolicyEdges.OrderBy(e => e.Key),
                e => Assert.Equal(0, e.Key),
                e => Assert.Equal(1, e.Key));

            var test1_0 = a.PolicyEdges[0];
            Assert.Equal(new[] { endpoint1, }, test1_0.Matches);
            Assert.Null(test1_0.NodeBuilder);
            Assert.Empty(test1_0.PolicyEdges);

            var test1_1 = a.PolicyEdges[1];
            Assert.Equal(new[] { endpoint2, endpoint3, }, test1_1.Matches);
            Assert.Null(test1_1.NodeBuilder);
            Assert.Empty(test1_1.PolicyEdges);
        }

        [Fact]
        public void BuildDfaTree_WithPolicies_AndBranches_BothPoliciesSkipped()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder(new TestMetadata1MatcherPolicy(), new TestMetadata2MatcherPolicy());

            var endpoint1 = CreateEndpoint("/a", metadata: new object[] { });
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("/a", metadata: new object[] { });
            builder.AddEndpoint(endpoint2);

            var endpoint3 = CreateEndpoint("/a", metadata: new object[] { });
            builder.AddEndpoint(endpoint3);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Equal(new[] { endpoint1, endpoint2, endpoint3, }, a.Matches);
            Assert.Null(a.NodeBuilder);
            Assert.Empty(a.PolicyEdges);
        }

        [Fact]
        public void CreateCandidate_JustLiterals()
        {
            // Arrange
            var endpoint = CreateEndpoint("/a/b/c");

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(endpoint, score: 0);

            // Assert
            Assert.Equal(Candidate.CandidateFlags.None, candidate.Flags);
            Assert.Empty(candidate.Slots);
            Assert.Empty(candidate.Captures);
            Assert.Equal(default, candidate.CatchAll);
            Assert.Empty(candidate.ComplexSegments);
            Assert.Empty(candidate.MatchProcessors);
        }

        [Fact]
        public void CreateCandidate_Parameters()
        {
            // Arrange
            var endpoint = CreateEndpoint("/{a}/{b}/{c}");

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(endpoint, score: 0);

            // Assert
            Assert.Equal(Candidate.CandidateFlags.HasCaptures, candidate.Flags);
            Assert.Equal(3, candidate.Slots.Length);
            Assert.Collection(
                candidate.Captures,
                c => Assert.Equal(("a", 0, 0), c),
                c => Assert.Equal(("b", 1, 1), c),
                c => Assert.Equal(("c", 2, 2), c));
            Assert.Equal(default, candidate.CatchAll);
            Assert.Empty(candidate.ComplexSegments);
            Assert.Empty(candidate.MatchProcessors);
        }

        [Fact]
        public void CreateCandidate_Parameters_WithDefaults()
        {
            // Arrange
            var endpoint = CreateEndpoint("/{a=aa}/{b=bb}/{c=cc}");

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(endpoint, score: 0);

            // Assert
            Assert.Equal(
                Candidate.CandidateFlags.HasDefaults | Candidate.CandidateFlags.HasCaptures,
                candidate.Flags);
            Assert.Collection(
                candidate.Slots,
                s => Assert.Equal(new KeyValuePair<string, object>("a", "aa"), s),
                s => Assert.Equal(new KeyValuePair<string, object>("b", "bb"), s),
                s => Assert.Equal(new KeyValuePair<string, object>("c", "cc"), s));
            Assert.Collection(
                candidate.Captures,
                c => Assert.Equal(("a", 0, 0), c),
                c => Assert.Equal(("b", 1, 1), c),
                c => Assert.Equal(("c", 2, 2), c));
            Assert.Equal(default, candidate.CatchAll);
            Assert.Empty(candidate.ComplexSegments);
            Assert.Empty(candidate.MatchProcessors);
        }

        [Fact]
        public void CreateCandidate_Parameters_CatchAll()
        {
            // Arrange
            var endpoint = CreateEndpoint("/{a}/{b}/{*c=cc}");

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(endpoint, score: 0);

            // Assert
            Assert.Equal(
                Candidate.CandidateFlags.HasDefaults |
                    Candidate.CandidateFlags.HasCaptures |
                    Candidate.CandidateFlags.HasCatchAll,
                candidate.Flags);
            Assert.Collection(
                candidate.Slots,
                s => Assert.Equal(new KeyValuePair<string, object>("c", "cc"), s),
                s => Assert.Equal(new KeyValuePair<string, object>(null, null), s),
                s => Assert.Equal(new KeyValuePair<string, object>(null, null), s));
            Assert.Collection(
                candidate.Captures,
                c => Assert.Equal(("a", 0, 1), c),
                c => Assert.Equal(("b", 1, 2), c));
            Assert.Equal(("c", 2, 0), candidate.CatchAll);
            Assert.Empty(candidate.ComplexSegments);
            Assert.Empty(candidate.MatchProcessors);
        }

        // Defaults are processed first, which affects the slot ordering.
        [Fact]
        public void CreateCandidate_Parameters_OutOfLineDefaults()
        {
            // Arrange
            var endpoint = CreateEndpoint("/{a}/{b}/{c=cc}", new { a = "aa", d = "dd", });

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(endpoint, score: 0);

            // Assert
            Assert.Equal(
                Candidate.CandidateFlags.HasDefaults | Candidate.CandidateFlags.HasCaptures,
                candidate.Flags);
            Assert.Collection(
                candidate.Slots,
                s => Assert.Equal(new KeyValuePair<string, object>("a", "aa"), s),
                s => Assert.Equal(new KeyValuePair<string, object>("d", "dd"), s),
                s => Assert.Equal(new KeyValuePair<string, object>("c", "cc"), s),
                s => Assert.Equal(new KeyValuePair<string, object>(null, null), s));
            Assert.Collection(
                candidate.Captures,
                c => Assert.Equal(("a", 0, 0), c),
                c => Assert.Equal(("b", 1, 3), c),
                c => Assert.Equal(("c", 2, 2), c));
            Assert.Equal(default, candidate.CatchAll);
            Assert.Empty(candidate.ComplexSegments);
            Assert.Empty(candidate.MatchProcessors);
        }

        [Fact]
        public void CreateCandidate_Parameters_ComplexSegments()
        {
            // Arrange
            var endpoint = CreateEndpoint("/{a}-{b=bb}/{c}");

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(endpoint, score: 0);

            // Assert
            Assert.Equal(
                Candidate.CandidateFlags.HasDefaults |
                    Candidate.CandidateFlags.HasCaptures |
                    Candidate.CandidateFlags.HasComplexSegments,
                candidate.Flags);
            Assert.Collection(
                candidate.Slots,
                s => Assert.Equal(new KeyValuePair<string, object>("b", "bb"), s),
                s => Assert.Equal(new KeyValuePair<string, object>(null, null), s));
            Assert.Collection(
                candidate.Captures,
                c => Assert.Equal(("c", 1, 1), c));
            Assert.Equal(default, candidate.CatchAll);
            Assert.Collection(
                candidate.ComplexSegments,
                s => Assert.Equal(0, s.segmentIndex));
            Assert.Empty(candidate.MatchProcessors);
        }

        [Fact]
        public void CreateCandidate_MatchProcessors()
        {
            // Arrange
            var endpoint = CreateEndpoint("/a/b/c", constraints: new { a = new IntRouteConstraint(), });

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(endpoint, score: 0);

            // Assert
            Assert.Equal( Candidate.CandidateFlags.HasMatchProcessors, candidate.Flags);
            Assert.Empty(candidate.Slots);
            Assert.Empty(candidate.Captures);
            Assert.Equal(default, candidate.CatchAll);
            Assert.Empty(candidate.ComplexSegments);
            Assert.Single(candidate.MatchProcessors);
        }
        
        [Fact]
        public void CreateCandidates_CreatesScoresCorrectly()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/a/b/c", constraints: new { a = new IntRouteConstraint(), }, metadata: new object[] { new TestMetadata1(), new TestMetadata2(), }),
                CreateEndpoint("/a/b/c", constraints: new { a = new AlphaRouteConstraint(), }, metadata: new object[] { new TestMetadata1(), new TestMetadata2(), }),
                CreateEndpoint("/a/b/c", constraints: new { a = new IntRouteConstraint(), }, metadata: new object[] { new TestMetadata1(), }),
                CreateEndpoint("/a/b/c", constraints: new { a = new IntRouteConstraint(), }, metadata: new object[] { new TestMetadata2(), }),
                CreateEndpoint("/a/b/c", constraints: new { }, metadata: new object[] { }),
                CreateEndpoint("/a/b/c", constraints: new { }, metadata: new object[] { }),
            };

            var builder = CreateDfaMatcherBuilder(new TestMetadata1MatcherPolicy(), new TestMetadata2MatcherPolicy());

            // Act
            var candidates = builder.CreateCandidates(endpoints);

            // Assert
            Assert.Collection(
                candidates,
                c => Assert.Equal(0, c.Score),
                c => Assert.Equal(0, c.Score),
                c => Assert.Equal(1, c.Score),
                c => Assert.Equal(2, c.Score),
                c => Assert.Equal(3, c.Score),
                c => Assert.Equal(3, c.Score));
        }

        private static DfaMatcherBuilder CreateDfaMatcherBuilder(params MatcherPolicy[] policies)
        {
            var dataSource = new CompositeEndpointDataSource(Array.Empty<EndpointDataSource>());
            return new DfaMatcherBuilder(
                Mock.Of<MatchProcessorFactory>(),
                Mock.Of<EndpointSelector>(),
                policies);
        }

        private MatcherEndpoint CreateEndpoint(
            string template,
            object defaults = null,
            object constraints = null,
            params object[] metadata)
        {
            return new MatcherEndpoint(
                MatcherEndpoint.EmptyInvoker,
                RoutePatternFactory.Parse(template, new RouteValueDictionary(defaults), new RouteValueDictionary(constraints)),
                0,
                new EndpointMetadataCollection(metadata),
                "test");
        }

        private class TestMetadata1
        {
            public TestMetadata1()
            {
            }

            public TestMetadata1(int state)
            {
                State = state;
            }

            public int State { get; set; }
        }

        private class TestMetadata1MatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, INodeBuilderPolicy
        {
            public override int Order => 100;

            public IComparer<Endpoint> Comparer => EndpointMetadataComparer<TestMetadata1>.Default;

            public bool AppliesToNode(IReadOnlyList<Endpoint> endpoints)
            {
                return endpoints.Any(e => e.Metadata.GetMetadata<TestMetadata1>() != null);
            }

            public PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges)
            {
                throw new NotImplementedException();
            }

            public IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints)
            {
                return endpoints
                    .GroupBy(e => e.Metadata.GetMetadata<TestMetadata1>().State)
                    .Select(g => new PolicyNodeEdge(g.Key, g.ToArray()))
                    .ToArray();
            }
        }

        private class TestMetadata2
        {
            public TestMetadata2()
            {
            }

            public TestMetadata2(bool state)
            {
                State = state;
            }

            public bool State { get; set; }
        }

        private class TestMetadata2MatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, INodeBuilderPolicy
        {
            public override int Order => 101;

            public IComparer<Endpoint> Comparer => EndpointMetadataComparer<TestMetadata2>.Default;

            public bool AppliesToNode(IReadOnlyList<Endpoint> endpoints)
            {
                return endpoints.Any(e => e.Metadata.GetMetadata<TestMetadata2>() != null);
            }

            public PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges)
            {
                throw new NotImplementedException();
            }

            public IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints)
            {
                return endpoints
                    .GroupBy(e => e.Metadata.GetMetadata<TestMetadata2>().State)
                    .Select(g => new PolicyNodeEdge(g.Key, g.ToArray()))
                    .ToArray();
            }
        }
    }
}
