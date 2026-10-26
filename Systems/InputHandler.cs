using System;
using System.Windows.Forms;

namespace TypingDead.Systems
{
    public enum GameAction
    {
        None,
        MoveUp,
        MoveDown,
        Backspace,
        Pause,
        Tutorial,
        Submit,
        Restart,
        Letter
    }

    public struct InputCommand
    {
        public GameAction Action;
        public char Letter;

        public InputCommand(GameAction action)
        {
            Action = action;
            Letter = '\0';
        }

        public InputCommand(GameAction action, char letter)
        {
            Action = action;
            Letter = letter;
        }
    }

    /// <summary>
    /// Converts WinForms keys into small, game-specific commands. Typing is
    /// captured from the form's command-key pipeline, so no TextBox is needed.
    /// </summary>
    public static class InputHandler
    {
        public static InputCommand MapGameKey(Keys key)
        {
            if (key >= Keys.A && key <= Keys.Z)
            {
                return new InputCommand(GameAction.Letter, (char)('A' + (key - Keys.A)));
            }

            switch (key)
            {
                case Keys.Up: return new InputCommand(GameAction.MoveUp);
                case Keys.Down: return new InputCommand(GameAction.MoveDown);
                case Keys.Back: return new InputCommand(GameAction.Backspace);
                case Keys.Escape: return new InputCommand(GameAction.Pause);
                case Keys.P: return new InputCommand(GameAction.Letter, 'P');
                case Keys.F1: return new InputCommand(GameAction.Tutorial);
                case Keys.Enter: return new InputCommand(GameAction.Submit);
                case Keys.Space: return new InputCommand(GameAction.Letter, ' ');
                case Keys.R: return new InputCommand(GameAction.Restart);
                default: return new InputCommand(GameAction.None);
            }
        }
    }
}
