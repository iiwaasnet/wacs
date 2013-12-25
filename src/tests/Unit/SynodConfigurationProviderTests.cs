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
        public void TestAddExistingNodeToWorld_DoesntAddNodeToWorldAndNoWorldChangedEventRisen()
        {
            var uri = "tcp://127.0.0.1:234".NormalizeEndpointAddress();
            var joiningUri = uri;
            var worldChangedFired = false;
            var synodChangedFired = false;

            var synodConfiguration = new SynodConfiguration
                                     {
                                         Nodes = new[]
                                                 {
                                                     new SynodConfigurationProvider.Endpoint(uri)
                                                 }
                                     };
            var synodConfigProvider = new SynodConfigurationProvider(synodConfiguration);

            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).SequenceEqual(synodConfiguration.Nodes.Select(n => n.Address)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).SequenceEqual(synodConfiguration.Nodes.Select(n => n.Address)));

            synodConfigProvider.AddNodeToWorld(new SynodConfigurationProvider.Endpoint(joiningUri));

            Assert.IsFalse(synodChangedFired);
            Assert.IsFalse(worldChangedFired);

            Assert.AreEqual(synodConfiguration.Nodes.Count(), synodConfigProvider.World.Count());
            Assert.AreEqual(synodConfiguration.Nodes.Count(), synodConfigProvider.Synod.Count());
        }

        [Test]
        public void TestAddNewNodeToWorld_RaisesWorldChangedEventAndAddsNodeToWorldOnly()
        {
            var uri = "tcp://127.0.0.1:234".NormalizeEndpointAddress();
            var joiningUri = "tcp://127.0.0.1:235/".NormalizeEndpointAddress();
            var worldChangedFired = false;
            var synodChangedFired = false;

            var synodConfiguration = new SynodConfiguration
                                     {
                                         Nodes = new[]
                                                 {
                                                     new SynodConfigurationProvider.Endpoint(uri)
                                                 }
                                     };
            var synodConfigProvider = new SynodConfigurationProvider(synodConfiguration);

            synodConfigProvider.WorldChanged += () => { worldChangedFired = true; };
            synodConfigProvider.SynodChanged += () => { synodChangedFired = true; };

            Assert.True(synodConfigProvider.Synod.Select(n => n.Address).SequenceEqual(synodConfiguration.Nodes.Select(n => n.Address)));
            Assert.True(synodConfigProvider.World.Select(n => n.Address).SequenceEqual(synodConfiguration.Nodes.Select(n => n.Address)));

            synodConfigProvider.AddNodeToWorld(new SynodConfigurationProvider.Endpoint(joiningUri));

            Assert.IsFalse(synodChangedFired);
            Assert.IsTrue(worldChangedFired);

            Assert.IsTrue(synodConfigProvider.World.Any(n => n.Address == joiningUri));
            Assert.IsFalse(synodConfigProvider.Synod.Any(n => n.Address == joiningUri));
        }

        [Test]
        [TestCase("tcp://127.0.0.1:234", "tcp://127.0.0.1:234")]
        [TestCase("tcp://127.0.0.1:234/", "tcp://127.0.0.1:234")]
        public void TestEndpointAddressIsNormalized(string inputUri, string normalizedUri)
        {
            Assert.AreEqual(normalizedUri, new SynodConfigurationProvider.Endpoint(inputUri).Address);
        }

        [Test]
        public void TestInitialConfiguratioLoad_ReturnsSynodAndWorld()
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
            // world should have more nodes, than synod
        }

        [Test]
        public void TestCreatingSynodConfigurationProviderWithTwoSameNodesInConfig_AddsNonDuplicatedNodes()
        {
        }
    }

    internal class SynodConfiguration : ISynodConfiguration
    {
        public IEnumerable<INode> Nodes { get; set; }
    }
}