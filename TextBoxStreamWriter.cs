using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Controls;

public class TextBoxStreamWriter : TextWriter
{
    private TextBox tbox;
    private Thread spinningThread;
    private bool isSpinning = false;
    private bool textUpdated = false;
    private bool textAppended = false;
    private readonly object lockObject = new object();


    public TextBoxStreamWriter(TextBox output)
    {
        tbox = output;
        tbox.Text = "";
        StartSpinning();
    }

    ~TextBoxStreamWriter()
    {
        StopSpinning();
    }

    public override void WriteLine(string value)
    {
        Write(value + "\n");
    }

    public override void Write(string value)
    {
        while (!textUpdated)
        {
            lock (lockObject)
            {
                textUpdated = true;
                
                tbox.Dispatcher.Invoke(() =>
                {
                    if (!textAppended)
                    {
                        tbox.Text = TrimLastChar(tbox.Text);
                    }
                    textAppended = true;

                    appendWithCarriageReturn(value);
                    tbox.CaretIndex = tbox.Text.Length;
                    tbox.ScrollToEnd();
                });
            }

        }

        textUpdated = false;
    }

    public override Encoding Encoding
    {
        get { return Encoding.Unicode; }
    }


    public void appendWithCarriageReturn(string value)
    {
        string[] lines = value.Split('\r');

        int i = 0;

        if (lines.Length == 1)
        {
            tbox.Text += value;
            return;
        }

        if (lines[0].Length == 0)
        {
            CarriageReturn();
            i = 1;
        }

        for (; i < lines.Length; i++)
        {
            tbox.Text += lines[i];

            if (i != lines.Length - 1)
                CarriageReturn();
        }

    }

    public void CarriageReturn()
    {
        string[] lines = tbox.Text.Split('\n');
        lines[lines.Length - 1] = "";
        tbox.Text = string.Join("\n", lines);
    }

    public void RedirectStandardOutput()
    {
        // Redirect the standard output to the custom TextBoxStreamWriter
        Console.SetOut(this);
        Console.SetError(this);
    }

    public void StartSpinning()
    {
        isSpinning = true;
        spinningThread = new Thread(new ThreadStart(SpinningTask));
        spinningThread.IsBackground = true;
        spinningThread.Start();
    }

    public void StopSpinning()
    {
        isSpinning = false;
        //if (spinningThread != null && spinningThread.IsAlive)
            //spinningThread.Join();
    }

    private String TrimLastChar(String s)
    {
        if (!string.IsNullOrEmpty(s))
        {
            return s.Remove(s.Length - 1);
        }

        return s;
    }

    private void SpinningTask() 
    {
        try
        {
            char[] spinningChars = { '|', '/', '―', '\\' };
            int currentIndex = 0;

            while (isSpinning)
            {
                lock (lockObject)
                {
                    tbox.Dispatcher.Invoke(() =>
                    {
                        if (!textAppended)
                        {
                            tbox.Text = TrimLastChar(tbox.Text);
                        }
                        else
                        {
                            textAppended = false;
                        }

                        tbox.AppendText(spinningChars[currentIndex].ToString());
                        tbox.CaretIndex = tbox.Text.Length;
                        tbox.ScrollToEnd();
                    });

                    currentIndex = (currentIndex + 1) % spinningChars.Length;
                }

                Thread.Sleep(250);
            }
        }
        catch(System.Threading.Tasks.TaskCanceledException _) {}
    }
}