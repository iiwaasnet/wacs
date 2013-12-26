using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using wacs.Configuration;
using wacs.core;
using wacs.Paxos.Implementation;

namespace tests.Unit
{
    [TestFixture]
    public class SynodConfigurationProviderTests
    {
        [Test]
        public void TestAddExistingNodeToWorld_DoesntAddNodeToWorldAndNoWorldChangedEventRisenAndThrowsNoException()
        {
            var sameUri = "tcp://127.0.0.1:234";
            var joiningNode = new SynodConfigurationProvider.Endpoint(sameUri);
            var worldChangedFired = false;
            var synodChangedFired = false;

            var synodConfiguration = new SynodConfiguration
                                     {
                                         Nodes = new[]
                                                 {
                                                     new SynodConfigurationProvider.Endpoint(sameUri),
                                                     new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:235")
                                                 }
                                     };
            var synodConfigProvider = new SynodConfigurationProvider(synodConfiguration);

            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Nodes.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Nodes.Select(n => n.Address).OrderBy(a => a)));

            synodConfigProvider.AddNodeToWorld(joiningNode);
            synodConfigProvider.AddNodeToWorld(joiningNode);

            Assert.IsFalse(synodChangedFired);
            Assert.IsFalse(worldChangedFired);

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Nodes.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Nodes.Select(n => n.Address).OrderBy(a => a)));
        }

        [Test]
        public void TestAddNewNodeToWorld_RaisesWorldChangedEventAndAddsNodeToWorldOnly()
        {
            var joiningNode = new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:124");
            var worldChangedFired = false;
            var synodChangedFired = false;

            var synodConfiguration = new SynodConfiguration
                                     {
                                         Nodes = new[]
                                                 {
                                                     new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:121"),
                                                     new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:122")
                                                 }
                                     };
            var synodConfigProvider = new SynodConfigurationProvider(synodConfiguration);

            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Nodes.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Nodes.Select(n => n.Address).OrderBy(a => a)));

            synodConfigProvider.AddNodeToWorld(joiningNode);

            Assert.IsFalse(synodChangedFired);
            Assert.IsTrue(worldChangedFired);

            var world = synodConfiguration.Nodes.Concat(new[] {joiningNode});

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Nodes.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(world.Select(n => n.Address).OrderBy(a => a)));
        }

        [Test]
        [TestCase("tcp://127.0.0.1:234", "tcp://127.0.0.1:234")]
        [TestCase("tcp://127.0.0.1:234/", "tcp://127.0.0.1:234")]
        public void TestEndpointAddressIsNormalized(string inputUri, string normalizedUri)
        {
            Assert.AreEqual(normalizedUri, new SynodConfigurationProvider.Endpoint(inputUri).Address);
        }

        [Test]
        public void TestInitialConfigurationLoad_ReturnsSynodAndWorld()
        {
            var synodConfiguration = new SynodConfiguration
                                     {
                                         Nodes = new[]
                                                 {
                                                     new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:234"),
                                                     new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:235/")
                                                 }
                                     };
            var synodConfigProvider = new SynodConfigurationProvider(synodConfiguration);

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Nodes.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Nodes.Select(n => n.Address).OrderBy(a => a)));
        }

        [Test]
        public void TestRemovingNodeFromWorld_RemovesNodeFromWorldAndRaisesWorldChangedEvent()
        {
            var leavingNode = "tcp://127.0.0.1:236/".NormalizeEndpointAddress();

            var worldChangedFired = false;
            var synodChangedFired = false;

            var synodConfiguration = new SynodConfiguration
                                     {
                                         Nodes = new[]
                                                 {
                                                     new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:234"),
                                                     new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:235/")
                                                 }
                                     };
            var synodConfigProvider = new SynodConfigurationProvider(synodConfiguration);
            synodConfigProvider.AddNodeToWorld(new SynodConfigurationProvider.Endpoint(leavingNode));

            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Nodes.Select(n => n.Address).OrderBy(a => a)));
            var world = synodConfiguration.Nodes.Concat(new[] {new SynodConfigurationProvider.Endpoint(leavingNode)});
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(world.Select(n => n.Address).OrderBy(a => a)));

            synodConfigProvider.DetachNodeFromWorld(new SynodConfigurationProvider.Endpoint(leavingNode));

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Nodes.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Nodes.Select(n => n.Address).OrderBy(a => a)));
            Assert.IsTrue(worldChangedFired);
            Assert.IsFalse(synodChangedFired);
        }

        [Test]
        public void TestRemovingNodeInActiveSynodFromWorld_ThrowsException()
        {
            var uri1 = "tcp://127.0.0.1:234";

            var synodConfiguration = new SynodConfiguration
                                     {
                                         Nodes = new[]
                                                 {
                                                     new SynodConfigurationProvider.Endpoint(uri1),
                                                     new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:235/")
                                                 }
                                     };
            var synodConfigProvider = new SynodConfigurationProvider(synodConfiguration);

            Assert.Throws<Exception>(() => synodConfigProvider.DetachNodeFromWorld(new SynodConfigurationProvider.Endpoint(uri1)));
            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Nodes.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Nodes.Select(n => n.Address).OrderBy(a => a)));
        }

        [Test]
        public void TestActivatingNewSynod_RaisesSynodChangedEventAndChangesToNewSynod()
        {
            var worldChangedFired = false;
            var synodChangedFired = false;

            var synod = new SynodConfiguration
                        {
                            Nodes = new[]
                                    {
                                        new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:234"),
                                        new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:235")
                                    }
                        };

            var synodConfigProvider = new SynodConfigurationProvider(synod);
            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synod.Nodes.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synod.Nodes.Select(n => n.Address).OrderBy(a => a)));

            var newSynod = new[]
                           {
                               new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:236"),
                               new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:237")
                           };

            synodConfigProvider.ActivateNewSynod(newSynod);

            Assert.IsTrue(synodChangedFired);
            Assert.IsTrue(worldChangedFired);

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(newSynod.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(newSynod.Select(n => n.Address).OrderBy(a => a)));
        }

        [Test]
        public void TestActivatingNewSynod_RaisesWorldChangedEventRemovesOldSynodFromWorldAndAddsNewSynodToWorld()
        {
            var worldChangedFired = false;
            var synodChangedFired = false;

            var listenerNode = new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:230");
            var synod = new SynodConfiguration
                        {
                            Nodes = new[]
                                    {
                                        new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:234"),
                                        new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:235")
                                    }
                        };
            var world = synod.Nodes.Concat(new[] {listenerNode});

            var synodConfigProvider = new SynodConfigurationProvider(synod);

            synodConfigProvider.AddNodeToWorld(listenerNode);

            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synod.Nodes.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(world.Select(n => n.Address).OrderBy(a => a)));

            var newSynod = new[]
                           {
                               new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:236"),
                               new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:237")
                           };
            world = newSynod.Concat(new[] {listenerNode});
            synodConfigProvider.ActivateNewSynod(newSynod);

            Assert.IsTrue(synodChangedFired);
            Assert.IsTrue(worldChangedFired);

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(newSynod.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(world.Select(n => n.Address).OrderBy(a => a)));
        }

        [Test]
        public void TestCreatingSynodConfigurationProviderWithTwoSameNodesInConfig_ThrowsException()
        {
            var oneEndpoint = new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:234");
            var otherEndpoint = new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:234");

            var synodConfiguration = new SynodConfiguration
                                     {
                                         Nodes = new[]
                                                 {
                                                     oneEndpoint,
                                                     oneEndpoint,
                                                     otherEndpoint
                                                 }
                                     };
            Assert.Throws<ArgumentException>(() => new SynodConfigurationProvider(synodConfiguration));
        }

        [Test]
        public void TestActivatingNewSynodWithDuplicatedNodesInSynod_NoSynodAndWorldChangedAndExceptionIsThrown()
        {
            var worldChangedFired = false;
            var synodChangedFired = false;

            var synod = new SynodConfiguration
                        {
                            Nodes = new[]
                                    {
                                        new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:234"),
                                        new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:235")
                                    }
                        };

            var synodConfigProvider = new SynodConfigurationProvider(synod);
            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synod.Nodes.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synod.Nodes.Select(n => n.Address).OrderBy(a => a)));

            var newSynod = new[]
                           {
                               new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:236"),
                               new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:236")
                           };

            Assert.Throws<ArgumentException>(() => synodConfigProvider.ActivateNewSynod(newSynod));

            Assert.IsFalse(synodChangedFired);
            Assert.IsFalse(worldChangedFired);

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synod.Nodes.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synod.Nodes.Select(n => n.Address).OrderBy(a => a)));
        }

        [Test]
        public void TestSynodConfigurationProvider_ReturnsLocalInitializedNode()
        {
            var synodConfigProvider = new SynodConfigurationProvider(new SynodConfiguration {Nodes = Enumerable.Empty<INode>()});

            Assert.IsNotNull(synodConfigProvider.LocalNode);
            Assert.AreNotEqual(0, synodConfigProvider.LocalNode.Id);
        }

        [Test]
        public void TestSynodConfigurationProvider_CanBeInitializedWithEmptySynodAndCanAddNodesToWorld()
        {
            var worldChangedFired = false;
            var synodChangedFired = false;
            var config = new SynodConfiguration {Nodes = Enumerable.Empty<INode>()};

            var synodConfigProvider = new SynodConfigurationProvider(config);
            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            var node1 = new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:234/");
            var node2 = new SynodConfigurationProvider.Endpoint("tcp://127.0.0.1:235/");
            var world = new[]
                        {
                            node1,
                            node2
                        };
            synodConfigProvider.AddNodeToWorld(node1);
            synodConfigProvider.AddNodeToWorld(node2);

            Assert.IsTrue(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                             .SequenceEqual(world.Select(n => n.Address).OrderBy(a => a)));
            Assert.IsTrue(worldChangedFired);
            Assert.IsFalse(synodChangedFired);
        }
    }

    internal class SynodConfiguration : ISynodConfiguration
    {
        public IEnumerable<INode> Nodes { get; set; }
    }
}