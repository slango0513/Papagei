using Godot;

namespace Playground.Client
{
    public class ControlledEntitySpatial : Spatial
    {
        public ClientControlledEntity Entity { get; set; }

        public override void _Ready()
        {
            //var camera = GetNode<Camera>("Camera");
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

            Entity.Up = Input.IsKeyPressed((int)KeyList.S);
            Entity.Down = Input.IsKeyPressed((int)KeyList.W);
            Entity.Right = Input.IsKeyPressed((int)KeyList.D);
            Entity.Left = Input.IsKeyPressed((int)KeyList.A);
        }
    }
}
