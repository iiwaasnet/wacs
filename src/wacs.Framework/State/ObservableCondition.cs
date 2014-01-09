﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;

namespace wacs.Framework.State
{
    public class ObservableCondition : IObservableCondition
    {
        private readonly Func<bool> condition;
        private readonly EventWaitHandle waitHandle;

        public ObservableCondition(Func<bool> condition, IEnumerable<IChangeNotifiable> members)
        {
            Contract.Requires(members != null && members.All(m => m != null));

            this.condition = condition;
            waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            BindEventHandlers(members);
        }

        private void BindEventHandlers(IEnumerable<IChangeNotifiable> members)
        {
            foreach (var changeNotifiable in members)
            {
                changeNotifiable.Changed += OnNotifiableChanged;
            }
        }

        private void OnNotifiableChanged()
        {
            if (condition())
            {
                waitHandle.Set();
            }
            else
            {
                waitHandle.Reset();
            }
        }

        public WaitHandle Waitable
        {
            get { return waitHandle; }
        }
    }
}