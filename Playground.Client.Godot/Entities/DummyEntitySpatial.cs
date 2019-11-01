using Godot;

namespace Playground.Client
{
    public class DummyEntitySpatial : Spatial
    {
        public ClientDummyEntity Entity { get; set; }

        public override void _Ready()
        {
            Entity.Frozen += () => { Visible = false; };
            Entity.Unfrozen += () => { Visible = true; };
        }

        public override void _Process(float delta)
        {
            if (Entity != default)
            {
                var state = Entity.State;
                Translation = new Vector3(state.X, state.Z, state.Y);
            }
        }
    }
}
