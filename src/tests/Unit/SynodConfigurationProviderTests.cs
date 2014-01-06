using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using wacs.Configuration;
using wacs.Rsm.Implementation;

namespace tests.Unit
{
    [TestFixture]
    public class SynodConfigurationProviderTests
    {
        private Topology CreateEmptySynodConfiguration()
        {
            var localNode = CreateLocalNode();
            return new Topology
                   {
                       LocalNode = localNode,
                       Synod = new Synod {Members = Enumerable.Empty<INode>()}
                   };
        }

        private Topology CreateSynodConfigurationWithLocalNode(IEnumerable<INode> synod)
        {
            var localNode = CreateLocalNode();
            return new Topology
                   {
                       LocalNode = localNode,
                       Synod = new Synod {Members = synod.Concat(new[] {localNode})}
                   };
        }

        [Test]
        public void TestAddExistingNodeToWorld_DoesntAddNodeToWorldAndNoWorldChangedEventRisenAndThrowsNoException()
        {
            var joiningNode = CreateNode("tcp://192.168.0.3");
            var worldChangedFired = false;
            var synodChangedFired = false;

            var synodConfig = CreateSynodConfigurationWithLocalNode(new[] {joiningNode});

            var synodConfigProvider = new SynodConfigurationProvider(synodConfig);

            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synodConfig.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synodConfig.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));

            synodConfigProvider.AddNodeToWorld(joiningNode);
            synodConfigProvider.AddNodeToWorld(joiningNode);

            Assert.IsFalse(synodChangedFired);
            Assert.IsFalse(worldChangedFired);

            Assert.True(synodConfigProvider.Synod.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synodConfig.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synodConfig.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));
        }

        [Test]
        public void TestAddNewNodeToWorld_RaisesWorldChangedEventAndAddsNodeToWorldOnly()
        {
            var joiningNode = CreateNode("tcp://192.169.0.1");
            var worldChangedFired = false;
            var synodChangedFired = false;

            var synodConfig = CreateSynodConfigurationWithLocalNode(new[] {CreateNode("tpc://192.168.0.2")});
            var synodConfigProvider = new SynodConfigurationProvider(synodConfig);

            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synodConfig.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synodConfig.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));

            synodConfigProvider.AddNodeToWorld(joiningNode);

            Assert.IsFalse(synodChangedFired);
            Assert.IsTrue(worldChangedFired);

            var world = synodConfig.Synod.Members.Concat(new[] {joiningNode});

            Assert.True(synodConfigProvider.Synod.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synodConfig.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(world.Select(n => n.BaseAddress).OrderBy(a => a)));
        }

        [Test]
        [TestCase("tcp://127.0.0.1", "tcp://127.0.0.1")]
        [TestCase("tcp://127.0.0.1/", "tcp://127.0.0.1")]
        public void TestEndpointAddressIsNormalized(string inputUri, string normalizedUri)
        {
            Assert.AreEqual(normalizedUri, new Node(inputUri, 3030, 4030).BaseAddress);
        }

        [Test]
        public void TestInitialConfigurationLoad_ReturnsSynodAndWorld()
        {
            var synodConfig = CreateSynodConfigurationWithLocalNode(new[] {CreateNode("tcp://192.168.0.1")});
            var synodConfigProvider = new SynodConfigurationProvider(synodConfig);

            Assert.True(synodConfigProvider.Synod.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synodConfig.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synodConfig.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));
        }

        [Test]
        public void TestRemovingNodeFromWorld_RemovesNodeFromWorldAndRaisesWorldChangedEvent()
        {
            var leavingNode = CreateNode("tcp://192.168.0.2");

            var worldChangedFired = false;
            var synodChangedFired = false;

            var synodConfig = CreateSynodConfigurationWithLocalNode(new[]
                                                                    {
                                                                        CreateNode("tcp://192.16.0.1")
                                                                    });
            var synodConfigProvider = new SynodConfigurationProvider(synodConfig);
            synodConfigProvider.AddNodeToWorld(new Node(leavingNode));

            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synodConfig.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));
            var world = synodConfig.Synod.Members.Concat(new[] {new Node(leavingNode)});
            Assert.True(synodConfigProvider.World.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(world.Select(n => n.BaseAddress).OrderBy(a => a)));

            synodConfigProvider.DetachNodeFromWorld(leavingNode);

            Assert.True(synodConfigProvider.Synod.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synodConfig.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synodConfig.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));
            Assert.IsTrue(worldChangedFired);
            Assert.IsFalse(synodChangedFired);
        }

        [Test]
        public void TestRemovingNodeInActiveSynodFromWorld_ThrowsException()
        {
            var leavingNode = CreateNode("tcp://192.168.0.2");

            var synodConfig = CreateSynodConfigurationWithLocalNode(new[] {leavingNode});
            var synodConfigProvider = new SynodConfigurationProvider(synodConfig);

            Assert.Throws<Exception>(() => synodConfigProvider.DetachNodeFromWorld(leavingNode));
            Assert.True(synodConfigProvider.Synod.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synodConfig.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synodConfig.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));
        }

        [Test]
        public void TestActivatingNewSynod_RaisesSynodChangedEventAndChangesToNewSynod()
        {
            var worldChangedFired = false;
            var synodChangedFired = false;

            var synod = CreateSynodConfigurationWithLocalNode(new[] {CreateNode("tcp://192.168.0.1")});

            var synodConfigProvider = new SynodConfigurationProvider(synod);
            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synod.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synod.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));

            var newSynod = new[]
                           {
                               CreateLocalNode(),
                               CreateNode("tcp://192.168.0.2")
                           };

            synodConfigProvider.ActivateNewSynod(newSynod);

            Assert.IsTrue(synodChangedFired);
            Assert.IsTrue(worldChangedFired);

            Assert.True(synodConfigProvider.Synod.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(newSynod.Select(n => n.BaseAddress).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(newSynod.Select(n => n.BaseAddress).OrderBy(a => a)));
        }

        [Test]
        public void TestActivatingNewSynod_RaisesWorldChangedEventRemovesOldSynodFromWorldAndAddsNewSynodToWorld()
        {
            var worldChangedFired = false;
            var synodChangedFired = false;

            var listenerNode = CreateNode("tcp://192.168.0.2");
            var synod = CreateSynodConfigurationWithLocalNode(new[] {CreateNode("tcp://192.168.0.3")});
            var world = synod.Synod.Members.Concat(new[] {listenerNode});

            var synodConfigProvider = new SynodConfigurationProvider(synod);

            synodConfigProvider.AddNodeToWorld(listenerNode);

            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synod.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(world.Select(n => n.BaseAddress).OrderBy(a => a)));

            var newSynod = new[]
                           {
                               CreateNode("tcp://192.168.0.4"),
                               CreateNode("tcp://192.168.0.5")
                           };
            world = newSynod.Concat(new[] {listenerNode});
            synodConfigProvider.ActivateNewSynod(newSynod);

            Assert.IsTrue(synodChangedFired);
            Assert.IsTrue(worldChangedFired);

            Assert.True(synodConfigProvider.Synod.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(newSynod.Select(n => n.BaseAddress).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(world.Select(n => n.BaseAddress).OrderBy(a => a)));
        }

        [Test]
        public void TestCreatingSynodConfigurationProviderWithTwoSameNodesInConfig_ThrowsException()
        {
            var synodConfiguration = CreateSynodConfigurationWithLocalNode(Enumerable.Repeat(CreateNode("tcp://192.168.0.3"), 2));
            Assert.Throws<ArgumentException>(() => new SynodConfigurationProvider(synodConfiguration));
        }

        [Test]
        public void TestActivatingNewSynodWithDuplicatedNodesInSynod_NoSynodAndWorldChangedAndExceptionIsThrown()
        {
            var worldChangedFired = false;
            var synodChangedFired = false;

            var synod = CreateSynodConfigurationWithLocalNode(new[] {CreateNode("tcp://192.168.0.3")});

            var synodConfigProvider = new SynodConfigurationProvider(synod);
            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synod.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synod.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));

            var newSynod = Enumerable.Repeat(CreateNode("tcp://192.168.0.1"), 2);

            Assert.Throws<ArgumentException>(() => synodConfigProvider.ActivateNewSynod(newSynod));

            Assert.IsFalse(synodChangedFired);
            Assert.IsFalse(worldChangedFired);

            Assert.True(synodConfigProvider.Synod.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synod.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));
            Assert.True(synodConfigProvider.World.Select(n => n.BaseAddress).OrderBy(a => a)
                                           .SequenceEqual(synod.Synod.Members.Select(n => n.BaseAddress).OrderBy(a => a)));
        }

        [Test]
        public void TestSynodConfigurationProvider_ReturnsLocalInitializedNode()
        {
            var synodConfigProvider = new SynodConfigurationProvider(CreateEmptySynodConfiguration());

            Assert.IsNotNull(synodConfigProvider.LocalProcess);
            Assert.AreNotEqual(0, synodConfigProvider.LocalProcess.Id);
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

            var node = CreateNode("tcp://192.168.0.2");
            var world = new[]
                        {
                            node,
                            config.LocalNode
                        };
            synodConfigProvider.AddNodeToWorld(node);

            Assert.IsTrue(synodConfigProvider.World.Select(n => n.BaseAddress).OrderBy(a => a)
                                             .SequenceEqual(world.Select(n => n.BaseAddress).OrderBy(a => a)));
            Assert.IsTrue(worldChangedFired);
            Assert.IsFalse(synodChangedFired);
        }

        [Test]
        public void TestInitializingSynodConfigurationProviderWithSynodWithoutLocalNode_ThrowsException()
        {
            var synodConfig = new Topology
                              {
                                  LocalNode = CreateLocalNode(),
                                  Synod = new Synod {Members = new[] {CreateNode("tcp://192.168.0.2")}}
                              };

            Assert.Throws<Exception>(() => new SynodConfigurationProvider(synodConfig));
        }

        private static Node CreateLocalNode()
        {
            return CreateNode("tcp://127.0.0.1");
        }

        private static Node CreateNode(string baseAddress)
        {
            return new Node(baseAddress, 3030, 4030);
        }
    }
}