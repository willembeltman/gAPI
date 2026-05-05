namespace gAPI.Fabric.Models;

public class Screen(ScrollWindow[] windows)
{
    public ScrollWindow[] Windows { get; } = windows;
    public ScrollWindow Selected { get; private set; } = windows.First();
    int oldindex = -1;

    public void Render(bool force)
    {
        if (Console.WindowWidth < 10) return;

        if (force)
        {
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Clear();
        }

        var different = false;
        foreach (var window in Windows)
        {
            if (window.Render(force))
            {
                different = true;
            }
        }

        var index = Array.IndexOf(Windows, Selected);
        if (oldindex != index)
        {
            oldindex = index;
            different = true;
        }

        if (different || force)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.SetCursorPosition(
                Selected.X,
                Selected.Y + Selected.Height - 1);
        }
    }

    public void SelectNext()
    {
        var index = Array.IndexOf(Windows, Selected);
        index++;
        if (index >= Windows.Length)
            index = 0;
        Selected = Windows[index];
    }

    public void Down()
    {
        Selected.Down();
    }

    public void Up()
    {
        Selected.Up();
    }
}