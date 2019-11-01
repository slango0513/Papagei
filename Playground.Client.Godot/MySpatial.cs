using Godot;

namespace Playground.Client
{
    public class MySpatial : Spatial
    {
        [Export]
        private readonly Spatial exportSpatial;

        public override void _Ready()
        {
            //exportSpatial = this;
        }

        public override void _Process(float delta)
        {
            //const float speed = 10f;

            //if (Input.IsActionPressed("ui_up"))
            //{
            //    exportSpatial.Translate(Vector3.Forward * speed * delta);
            //}
            //if (Input.IsActionPressed("ui_down"))
            //{
            //    exportSpatial.Translate(Vector3.Back * speed * delta);
            //}
            //if (Input.IsActionPressed("ui_left"))
            //{
            //    exportSpatial.Translate(Vector3.Left * speed * delta);
            //}
            //if (Input.IsActionPressed("ui_right"))
            //{
            //    exportSpatial.Translate(Vector3.Right * speed * delta);
            //}
        }
    }
}
