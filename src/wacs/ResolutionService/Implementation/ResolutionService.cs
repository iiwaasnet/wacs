using System.Collections.Generic;
using wacs.core;
using wacs.Messaging;
using wacs.ResolutionService.Interface;

namespace wacs.ResolutionService.Implementation
{
    public class ResolutionService : IResolutionService
    {
        private readonly IMessageHub messageHub;
        private readonly IProcess self;

        public ResolutionService(IMessageHub messageHub)
        {
            this.messageHub = messageHub;
            self = CreateHostingProcessIdentity();
        }

        private IProcess CreateHostingProcessIdentity()
        {
            return new Process(UniqueIdGenerator.Generate(3));
        }

        public IEnumerable<IProcess> GetWorld()
        {
            throw new System.NotImplementedException();
        }

        public IProcess GetLocalProcess()
        {
            throw new System.NotImplementedException();
        }
    }
}