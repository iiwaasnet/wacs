using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using tests.Unit.Helpers;
using wacs.Configuration;
using wacs.Paxos.Implementation;

namespace tests.Unit
{
    [TestFixture]
    public class SynodConfigurationProviderTests
    {
        private SynodConfiguration CreateEmptySynodConfiguration()
        {
            var localNode = new Endpoint("tcp://127.0.0.1:3030");
            return new SynodConfiguration
                   {
                       LocalNode = localNode,
                       Members = Enumerable.Empty<IEndpoint>()
                   };
        }

        private SynodConfiguration CreateSynodConfigurationWithLocalNode(IEnumerable<IEndpoint> synod)
        {
            var localNode = new Endpoint("tcp://127.0.0.1:3030");
            return new SynodConfiguration
                   {
                       LocalNode = localNode,
                       Members = synod.Concat(new[] {localNode})
                   };
        }

        [Test]
        public void TestAddExistingNodeToWorld_DoesntAddNodeToWorldAndNoWorldChangedEventRisenAndThrowsNoException()
        {
            var joiningNode = new Endpoint("tcp://192.168.0.1:3030");
            var worldChangedFired = false;
            var synodChangedFired = false;

            var synodConfiguration = CreateSynodConfigurationWithLocalNode(new []{joiningNode});

            var synodConfigProvider = new SynodConfigurationProvider(synodConfiguration);

            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Members.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Members.Select(n => n.Address).OrderBy(a => a)));

            synodConfigProvider.AddNodeToWorld(joiningNode);
            synodConfigProvider.AddNodeToWorld(joiningNode);

            Assert.IsFalse(synodChangedFired);
            Assert.IsFalse(worldChangedFired);

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Members.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Members.Select(n => n.Address).OrderBy(a => a)));
        }

        [Test]
        public void TestAddNewNodeToWorld_RaisesWorldChangedEventAndAddsNodeToWorldOnly()
        {
            var joiningNode = new Endpoint("tcp://192.169.0.1:3030");
            var worldChangedFired = false;
            var synodChangedFired = false;

            var synodConfiguration = CreateSynodConfigurationWithLocalNode(new[] {new Endpoint("tpc://192.168.0.2:3030")});
            var synodConfigProvider = new SynodConfigurationProvider(synodConfiguration);

            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Members.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Members.Select(n => n.Address).OrderBy(a => a)));

            synodConfigProvider.AddNodeToWorld(joiningNode);

            Assert.IsFalse(synodChangedFired);
            Assert.IsTrue(worldChangedFired);

            var world = synodConfiguration.Members.Concat(new[] {joiningNode});

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Members.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(world.Select(n => n.Address).OrderBy(a => a)));
        }

        [Test]
        [TestCase("tcp://127.0.0.1:234", "tcp://127.0.0.1:234")]
        [TestCase("tcp://127.0.0.1:234/", "tcp://127.0.0.1:234")]
        public void TestEndpointAddressIsNormalized(string inputUri, string normalizedUri)
        {
            Assert.AreEqual(normalizedUri, new Endpoint(inputUri).Address);
        }

        [Test]
        public void TestInitialConfigurationLoad_ReturnsSynodAndWorld()
        {
            var synodConfiguration = CreateSynodConfigurationWithLocalNode(new[] {new Endpoint("tcp://192.168.0.1:303")});
            var synodConfigProvider = new SynodConfigurationProvider(synodConfiguration);

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Members.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Members.Select(n => n.Address).OrderBy(a => a)));
        }

        [Test]
        public void TestRemovingNodeFromWorld_RemovesNodeFromWorldAndRaisesWorldChangedEvent()
        {
            var leavingNode = new Endpoint("tcp://192.168.0.2:3030");

            var worldChangedFired = false;
            var synodChangedFired = false;

            var synodConfiguration = CreateSynodConfigurationWithLocalNode(new[]
                                                                           {
                                                                               new Endpoint("tcp://192.16.0.1:3030")
                                                                           });
            var synodConfigProvider = new SynodConfigurationProvider(synodConfiguration);
            synodConfigProvider.AddNodeToWorld(new Endpoint(leavingNode));

            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Members.Select(n => n.Address).OrderBy(a => a)));
            var world = synodConfiguration.Members.Concat(new[] {new Endpoint(leavingNode)});
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(world.Select(n => n.Address).OrderBy(a => a)));

            synodConfigProvider.DetachNodeFromWorld(leavingNode);

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Members.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Members.Select(n => n.Address).OrderBy(a => a)));
            Assert.IsTrue(worldChangedFired);
            Assert.IsFalse(synodChangedFired);
        }

        [Test]
        public void TestRemovingNodeInActiveSynodFromWorld_ThrowsException()
        {
            var leavingNode = new Endpoint("tcp://192.168.0.2:3030");

            var synodConfiguration = CreateSynodConfigurationWithLocalNode(new[] {leavingNode});
            var synodConfigProvider = new SynodConfigurationProvider(synodConfiguration);

            Assert.Throws<Exception>(() => synodConfigProvider.DetachNodeFromWorld(leavingNode));
            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Members.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synodConfiguration.Members.Select(n => n.Address).OrderBy(a => a)));
        }

        [Test]
        public void TestActivatingNewSynod_RaisesSynodChangedEventAndChangesToNewSynod()
        {
            var worldChangedFired = false;
            var synodChangedFired = false;

            var synod = CreateSynodConfigurationWithLocalNode(new[] {new Endpoint("tcp://192.168.0.1:3031")});

            var synodConfigProvider = new SynodConfigurationProvider(synod);
            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synod.Members.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synod.Members.Select(n => n.Address).OrderBy(a => a)));

            var newSynod = new[]
                           {
                               new Endpoint("tcp://192.168.0.1:3030"),
                               new Endpoint("tcp://192.168.0.2:3030")
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

            var listenerNode = new Endpoint("tcp://192.168.0.1:3030");
            var synod = CreateSynodConfigurationWithLocalNode(new[] {new Endpoint("tcp://192.168.0.3:3030"),});
            var world = synod.Members.Concat(new[] {listenerNode});

            var synodConfigProvider = new SynodConfigurationProvider(synod);

            synodConfigProvider.AddNodeToWorld(listenerNode);

            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synod.Members.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(world.Select(n => n.Address).OrderBy(a => a)));

            var newSynod = new[]
                           {
                               new Endpoint("tcp://192.168.0.4:3030"),
                               new Endpoint("tcp://192.168.0.5:3030")
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
            var synodConfiguration = CreateSynodConfigurationWithLocalNode(Enumerable.Repeat(new Endpoint("tcp://192.168.0.3:3030"), 2));
            Assert.Throws<ArgumentException>(() => new SynodConfigurationProvider(synodConfiguration));
        }

        [Test]
        public void TestActivatingNewSynodWithDuplicatedNodesInSynod_NoSynodAndWorldChangedAndExceptionIsThrown()
        {
            var worldChangedFired = false;
            var synodChangedFired = false;

            var synod = CreateSynodConfigurationWithLocalNode(new[] {new Endpoint("tcp://192.168.0.1:3031")});

            var synodConfigProvider = new SynodConfigurationProvider(synod);
            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synod.Members.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synod.Members.Select(n => n.Address).OrderBy(a => a)));

            var newSynod = Enumerable.Repeat(new Endpoint("tcp://192.168.0.1:3032"), 2);

            Assert.Throws<ArgumentException>(() => synodConfigProvider.ActivateNewSynod(newSynod));

            Assert.IsFalse(synodChangedFired);
            Assert.IsFalse(worldChangedFired);

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synod.Members.Select(n => n.Address).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                           .SequenceEqual(synod.Members.Select(n => n.Address).OrderBy(a => a)));
        }

        [Test]
        public void TestSynodConfigurationProvider_ReturnsLocalInitializedNode()
        {
            var synodConfigProvider = new SynodConfigurationProvider(CreateEmptySynodConfiguration());

            Assert.IsNotNull(synodConfigProvider.LocalNode);
            Assert.AreNotEqual(0, synodConfigProvider.LocalNode.Id);
        }

        [Test]
        public void TestSynodConfigurationProvider_CanBeInitializedWithEmptySynodAndCanAddNodesToWorld()
        {
            var worldChangedFired = false;
            var synodChangedFired = false;
            var config = CreateEmptySynodConfiguration();

            var synodConfigProvider = new SynodConfigurationProvider(config);
            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            var node = new Endpoint("tcp://192.168.0.2:3030");
            var world = new[]
                        {
                            node,
                            config.LocalNode
                        };
            synodConfigProvider.AddNodeToWorld(node);

            Assert.IsTrue(synodConfigProvider.World.Select(n => n.Address).OrderBy(a => a)
                                             .SequenceEqual(world.Select(n => n.Address).OrderBy(a => a)));
            Assert.IsTrue(worldChangedFired);
            Assert.IsFalse(synodChangedFired);
        }

        [Test]
        public void TestInitializingSynodConfigurationProviderWithSynodWithoutLocalNode_ThrowsException()
        {
            var synodConfig = new SynodConfiguration
                              {
                                  LocalNode = new Endpoint("tcp://127.0.0.1:30303"),
                                  Members = new[] {new Endpoint("tcp://192.168.0.2:3030")}
                              };

            Assert.Throws<Exception>(() => new SynodConfigurationProvider(synodConfig));
        }
    }
}