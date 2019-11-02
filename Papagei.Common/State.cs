using System;

namespace Papagei
{
    /// <summary>
    /// States are the fundamental data management class. They 
    /// contain all of the synchronized information that an Entity needs to
    /// function. States have multiple sub-fields that are sent at different
    /// cadences, as follows:
    /// 
    ///    Mutable Data:
    ///       Sent whenever the state differs from the client's view.
    ///       Delta-encoded against the client's view.
    ///    
    ///    Controller Data:
    ///       Sent to the controller of the entity every update.
    ///       Not delta-encoded -- always sent full-encode.
    ///       
    ///    Immutable Data:
    ///       Sent only once at creation. Can not be changed after.
    ///       
    ///    Removal Data (Not currently implemented):
    ///       Sent when the state is removed. Arrives at the time of removal.
    ///       
    /// In order to register a State class, tag it with the
    /// [RegisterState] attribute. See Registry.cs for more information.
    /// </summary>
    public abstract class State : IPoolable<State>
    {
        // #region Pooling
        public IPool<State> Pool { get; set; }

        public virtual void Reset()
        {
            Flags = 0;
            HasControllerData = false;
            HasImmutableData = false;
        }
        // #endregion

        public const uint FLAGS_ALL = 0xFFFFFFFF; // All values different
        public const uint FLAGS_NONE = 0x00000000; // No values different

        public int TypeCode { get; set; }

        // #region Creation
        // #endregion

        //protected virtual void InitializeData() { }

        public abstract int FlagBits { get; }

        public uint Flags { get; set; }              // Synchronized
        public bool HasControllerData { get; set; } // Synchronized
        public bool HasImmutableData { get; set; }  // Synchronized
        public Tick RemovedTick { get; set; }       // Synchronized
        public Tick CommandAck { get; set; }        // Synchronized to Controller

        public abstract void ApplyMutableFrom(State source, uint flags);
        [Obsolete]
        public virtual void ApplyControllerFrom(State source) { }
        public abstract void ApplyImmutableFrom(State source);

        // Server
        public abstract uint CompareMutableData(State basis);
        public abstract bool IsControllerDataEqual(State basis);

        // Client
        [Obsolete]
        public virtual void ResetControllerData() { }
        // Client?
        public virtual void ApplyInterpolated(State first, State second, float t) { }

        public void OverwriteFrom(State source)
        {
            Flags = source.Flags;
            ApplyMutableFrom(source, FLAGS_ALL);
            ApplyControllerFrom(source);
            ApplyImmutableFrom(source);
            HasControllerData = source.HasControllerData;
            HasImmutableData = source.HasImmutableData;
        }
    }
}
