using gAPI.Fabric.Server.Enums;
using System.Drawing;

namespace gAPI.Fabric.Server.Models;

public class ScrollWindow(string title, Point point, Size size, HorizontalAlign align = HorizontalAlign.Left) : IConsole
{
    ColorLine[] Items = [];
    string[] oldItems = [];
    int topIndex = 0;
    int oldTopIndex = 0;

    public void SetItems(ColorLine[] items)
    {
        Items = items;
        Dirty = true;
    }

    public int X => point.X * Console.WindowWidth / 120;
    public int Y => point.Y * Console.WindowHeight / 30;
    public int Width => size.Width * Console.WindowWidth / 120;
    public int Height => size.Height * Console.WindowHeight / 30;

    public ColorLine[] RealLines => [.. Items.SelectMany(a => GetLineBreaked(a))];

    private IEnumerable<ColorLine> GetLineBreaked(ColorLine value2)
    {
        var lines = value2.Text.Split(Environment.NewLine);
        foreach (var value in lines)
        {
            var pos = 0;
            var lineWidth = Width - 2;
            while (pos <= value.Length)
            {
                var end = Math.Min(pos + lineWidth, value.Length - pos);
                yield return new ColorLine() { Text = value.Substring(pos, end), Color = value2.Color };
                pos += lineWidth;
            }
        }
    }

    public bool Dirty { get; set; }

    public bool Render(bool force)
    {
        if (Console.WindowWidth < 10) return false;
        if (!force && !Dirty && !IsDifferent()) return false;

        Console.BackgroundColor = ConsoleColor.DarkBlue; ;
        Console.SetCursorPosition(X, Y);
        Console.WriteLine(FillString(title, Width, HorizontalAlign.Center));

        var max = Y + Height;
        var i = topIndex;

        var y2 = Y + 1;
        while (y2 < max)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.SetCursorPosition(X, y2);
            var item = i < RealLines.Length ? RealLines[i] : null;
            if (item != null)
            {
                Console.ForegroundColor = item.Color;
            }
            Console.WriteLine(FillString(item?.Text ?? "", Width, align));
            i++;
            y2++;
        }
        DrawVerticalScrollbar(X, Y, Width, Height);
        Dirty = false;

        return true;
    }


    private bool IsDifferent()
    {
        var differnt = false;
        if (oldItems.Length != Items.Length)
        {
            oldItems = new string[Items.Length];
        }
        for (int i2 = 0; i2 < Items.Length; i2++)
        {
            if (oldItems[i2] != Items[i2].Text)
            {
                differnt = true;
                oldItems[i2] = Items[i2].Text;
            }
        }
        if (oldTopIndex != topIndex)
        {
            oldTopIndex = topIndex;
            differnt = true;
        }
        return differnt;
    }
    private void DrawVerticalScrollbar(int x, int y, int width, int height)
    {
        if (Items.Length <= 0)
            return;

        int contentHeight = height - 1; // title excluded
        int totalItems = RealLines.Length;

        if (totalItems <= contentHeight)
            return; // no need for scrollbar

        int scrollbarX = x + width - 1;
        int scrollbarY = y + 1;

        double visibleRatio = (double)contentHeight / totalItems;
        double thumbHeightExact = visibleRatio * contentHeight;

        // We use double resolution (halve blokken)
        int virtualHeight = contentHeight * 2;
        int thumbHeight = (int)Math.Max(2, thumbHeightExact * 2);

        double scrollRatio = (double)topIndex / (totalItems - contentHeight);
        int thumbTop = (int)(scrollRatio * (virtualHeight - thumbHeight));

        for (int i = 0; i < contentHeight; i++)
        {
            Console.SetCursorPosition(scrollbarX, scrollbarY + i);
            Console.ForegroundColor = ConsoleColor.Gray;

            int cellTop = i * 2;
            int cellBottom = cellTop + 2;

            bool upperFilled = cellTop >= thumbTop && cellTop < thumbTop + thumbHeight;
            bool lowerFilled = cellBottom > thumbTop && cellBottom <= thumbTop + thumbHeight;

            char c = '│';

            if (upperFilled && lowerFilled)
                c = '█';
            else if (upperFilled)
                c = '▀';
            else if (lowerFilled)
                c = '▄';

            Console.Write(c);
        }
    }

    public static string FillString(string text, int width, HorizontalAlign align)
    {
        text ??= string.Empty;

        if (width <= 0)
            return string.Empty;

        if (text.Length >= width)
            return text.Substring(0, width);

        int totalPadding = width - text.Length;

        return align switch
        {
            HorizontalAlign.Left =>
                text + new string(' ', totalPadding),

            HorizontalAlign.Right =>
                new string(' ', totalPadding) + text,

            HorizontalAlign.Center =>
                new string(' ', totalPadding / 2) +
                text +
                new string(' ', totalPadding - (totalPadding / 2)),

            _ => text
        };
    }


    public void WriteLine(string? line = null, ConsoleColor color = ConsoleColor.White)
    {
        if (Console.WindowWidth < 10)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(line);
            return;
        }

        Items = [.. Items, new() { Text = line ?? "", Color = color }];
        //Items.Add(new() { Text = line ?? "", Color = color });

        if (RealLines.Length > Height - 1)
        {
            topIndex = RealLines.Length - Height + 1;
        }
        Dirty = true;
    }

    public void Down()
    {
        if (topIndex < RealLines.Length - Height + 1)
        {
            topIndex++;
            Dirty = true;
        }
    }

    public void Up()
    {
        if (topIndex > 0)
        {
            topIndex--;
            Dirty = true;
        }
    }
}
public class ColorLine
{
    public string Text { get; set; } = string.Empty;
    public ConsoleColor Color { get; set; } = ConsoleColor.White;
}