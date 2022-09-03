using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Threads;

namespace SharpDetect.Plugins.LockSet
{
    public class Variable
    {
        public VariableState State { get; protected set; }
        protected IShadowThread CreatorThread { get; set; }
        protected HashSet<object>? CandidateLocks { get; set; }
        private static HashSet<object> EmptyLocks { get; } = new HashSet<object>();

        public Variable(IShadowThread thread)
        {
            CreatorThread = thread;
        }

        public bool TryUpdateRead(IShadowThread thread, HashSet<IShadowObject> currentLocks)
        {
            // Update variable state
            if (State == VariableState.Exclusive && thread != CreatorThread)
                State = VariableState.Shared;

            return CheckVariable(currentLocks);
        }

        public bool TryUpdateWrite(IShadowThread thread, HashSet<IShadowObject> currentLocks)
        {
            // Update variable state
            switch (State)
            {
                case VariableState.Virgin:
                case VariableState.Exclusive:
                    if (thread == CreatorThread)
                        State = VariableState.Exclusive;
                    else
                        State = VariableState.SharedModified;
                    break;
                case VariableState.Shared:
                    State = VariableState.SharedModified;
                    break;
            }

            return CheckVariable(currentLocks);
        }

        private bool CheckVariable(HashSet<IShadowObject> currentLocks)
        {
            // Variables in SharedModified state must be checked
            if (State == VariableState.SharedModified)
            {
                if (!LockRefinement(currentLocks))
                    return false;
            }

            return true;
        }

        private bool LockRefinement(HashSet<IShadowObject> currentLocks)
        {
            // If there are no locks taken the candidate locks are always empty
            if (currentLocks.Count == 0)
                CandidateLocks = EmptyLocks;
            // For the first time just copy locks
            else if (CandidateLocks == null)
                CandidateLocks = new HashSet<object>(currentLocks);
            // Check if refinement makes sense
            else if (CandidateLocks.Count != 0)
                CandidateLocks.IntersectWith(currentLocks);

            // There are no locks guarding access to the variable
            return CandidateLocks.Count != 0;
        }
    }
}
