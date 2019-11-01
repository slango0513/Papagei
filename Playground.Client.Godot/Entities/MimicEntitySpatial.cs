using Godot;

namespace Playground.Client
{
    public class MimicEntitySpatial : Spatial
    {
        public ClientMimicEntity Entity { get; set; }

        public override void _Ready()
        {
            Entity.Shutdown += () => { QueueFree(); };
            Entity.Frozen += () => { Visible = false; };
            Entity.Unfrozen += () => { Visible = true; };
        }

        public override void _Process(float delta)
        {
            if (Entity != default)
            {
                if (ClientUtils.DoSmoothing)
                {
                    // state = Entity.GetSmoothedState(Time.time - Time.fixedTime);
                    var pos = ClientUtils.GetSmoothedPosition(ClientUtils.GetAuthState(Entity), ClientUtils.GetNextState(Entity));
                    Translation = new Vector3(pos.X, 0, pos.Y);
                }
                else
                {
                    var state = Entity.State;
                    Translation = new Vector3(state.X, 0, state.Y);
                }
            }
        }
    }
}
