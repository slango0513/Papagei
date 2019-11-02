using Papagei;

namespace Playground
{
    public class GameCommand : Command
    {
        public override void Reset()
        {
            base.Reset();

            SetData(false, false, false, false, false);
        }

        public bool Up { get; set; }
        public bool Down { get; set; }
        public bool Left { get; set; }
        public bool Right { get; set; }
        public bool Action { get; set; }

        public void SetData(bool up, bool down, bool left, bool right, bool action)
        {
            Up = up;
            Down = down;
            Left = left;
            Right = right;
            Action = action;
        }

        public override void SetDataFrom(Command other)
        {
            var _other = (GameCommand)other;
            SetData(_other.Up, _other.Down, _other.Left, _other.Right, _other.Action);
        }
    }
}
